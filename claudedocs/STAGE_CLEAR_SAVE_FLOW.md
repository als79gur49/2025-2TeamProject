# 스테이지 클리어 정보 저장 흐름 상세 분석

## 📋 목차
1. [전체 아키텍처](#1-전체-아키텍처)
2. [저장 데이터 구조](#2-저장-데이터-구조)
3. [전체 호출 흐름](#3-전체-호출-흐름)
4. [핵심 객체 및 함수](#4-핵심-객체-및-함수)
5. [자동 저장 설정](#5-자동-저장-설정)
6. [저장 파일 구조](#6-저장-파일-구조)

---

## 1. 전체 아키텍처

### 1.1 계층 구조

```
┌──────────────────────────────────────────────────────────┐
│ Game Layer (게임 로직)                                   │
│                                                           │
│  GameOutcomeManager                                      │
│  - OnVictory 이벤트 발생 (적 본진 파괴)                 │
│  - OnDefeat 이벤트 발생 (아군 본진 파괴)                │
│                                                           │
└─────────────────┬────────────────────────────────────────┘
                  │ 이벤트 구독
┌─────────────────▼────────────────────────────────────────┐
│ UI/System Layer (UI 및 게임 시스템)                      │
│                                                           │
│  - 승리 이벤트 수신                                       │
│  - CompleteStage(stageId, score, statistics) 호출       │
│                                                           │
└─────────────────┬────────────────────────────────────────┘
                  │ 데이터 전달
┌─────────────────▼────────────────────────────────────────┐
│ Manager Layer (진행도 관리)                              │
│                                                           │
│  StageProgressManager                                    │
│  - PlaySession 생성 및 업데이트                          │
│  - StageRecord 업데이트 (최고 기록, 상태)                │
│  - 통계 누적 (Statistics)                                │
│  - 이벤트 발행 (OnStageCompleted, OnProgressUpdated)     │
│  - SaveProgress() 호출                                   │
│                                                           │
└─────────────────┬────────────────────────────────────────┘
                  │ 저장 요청
┌─────────────────▼────────────────────────────────────────┐
│ Adapter Layer (데이터 변환 및 조정)                      │
│                                                           │
│  SaveDataAdapter                                         │
│  - CollectStageProgress() - 메모리 데이터 수집           │
│  - SaveSpecific(SaveFileType.StageProgress) 호출        │
│                                                           │
└─────────────────┬────────────────────────────────────────┘
                  │ 직렬화 및 저장
┌─────────────────▼────────────────────────────────────────┐
│ Storage Layer (파일 시스템)                              │
│                                                           │
│  SaveGameManager                                         │
│  - JSON 직렬화 (JsonUtility.ToJson)                     │
│  - 파일 저장 (Application.persistentDataPath)           │
│  - OnDataSaved 이벤트 발행                               │
│                                                           │
└──────────────────────────────────────────────────────────┘
```

### 1.2 주요 컴포넌트 관계

```
GameOutcomeManager (이벤트 발생)
         │
         ├─ OnVictory Event
         │       │
         │       └─> UI/Game System (이벤트 구독)
         │                   │
         │                   └─> StageProgressManager.CompleteStage()
         │
StageProgressManager (데이터 관리)
         │
         ├─ progressData (메모리 저장)
         │   └─ Dictionary<string, StageRecord> stageRecords
         │
         ├─ autoSaveEnabled (자동 저장 설정)
         │
         └─> SaveDataAdapter.SaveSpecific()
                     │
                     └─> SaveGameManager.SaveToFile()
                                 │
                                 └─> stage_progress.json
```

---

## 2. 저장 데이터 구조

### 2.1 PlaySession (개별 플레이 세션)
**파일**: `Assets/Script/SaveSystem/Data/StageProgressData.cs` (line 25-40)

```csharp
public class PlaySession
{
    public DateTime startTime;          // 플레이 시작 시간
    public DateTime endTime;            // 플레이 종료 시간
    public int score;                   // 획득 점수
    public int stars;                   // 획득 별 개수 (0-3)
    public float playTime;              // 실제 플레이 시간 (초)
    public bool completed;              // 클리어 여부
    public Dictionary<string, int> statistics;  // 세션별 통계
}
```

**statistics 포함 정보**:
- `enemies_defeated`: 처치한 적 수
- `damage_dealt`: 입힌 데미지
- `damage_taken`: 입은 데미지
- 기타 커스텀 통계

### 2.2 StageRecord (스테이지별 누적 데이터)
**파일**: `Assets/Script/SaveSystem/Data/StageProgressData.cs` (line 47-99)

```csharp
public class StageRecord
{
    public string stageId;                      // 스테이지 고유 ID
    public StageState state;                    // Locked/Unlocked/InProgress/Cleared/Perfect
    public int bestScore;                       // 최고 점수
    public int bestStars;                       // 최고 별 개수 (0-3)
    public List<PlaySession> playSessions;      // 모든 플레이 세션 기록
    public DateTime firstUnlockDate;            // 첫 해금 시간
    public DateTime firstClearDate;             // 첫 클리어 시간
    public DateTime lastPlayDate;               // 마지막 플레이 시간
    public Dictionary<string, object> metadata; // 확장 메타데이터
    public StageStatistics statistics;          // 누적 통계
}
```

**StageState 열거형**:
- `Locked = 0`: 잠김
- `Unlocked = 1`: 해금됨
- `InProgress = 2`: 진행 중
- `Cleared = 3`: 클리어 (별 3개 미만)
- `Perfect = 4`: 퍼펙트 클리어 (별 3개)

### 2.3 StageStatistics (스테이지 누적 통계)
**파일**: `Assets/Script/SaveSystem/Data/StageProgressData.cs` (line 105-118)

```csharp
public class StageStatistics
{
    public int totalEnemiesDefeated;    // 처치한 적 총 수
    public int totalDamageDealt;        // 입힌 데미지 총합
    public int totalDamageTaken;        // 입은 데미지 총합
    public int perfectClearCount;       // 노데미지 클리어 횟수
    public float bestClearTime;         // 최고 클리어 시간
    public Dictionary<string, int> customStats;  // 커스텀 통계
}
```

### 2.4 StageProgressData (전체 진행도)
**파일**: `Assets/Script/SaveSystem/Data/StageProgressData.cs` (line 127-165)

```csharp
public class StageProgressData
{
    public Dictionary<string, StageRecord> stageRecords;  // 모든 스테이지 기록
    public GlobalStatistics globalStats;                  // 전역 통계
    public DateTime lastModified;                         // 마지막 수정 시간
}
```

---

## 3. 전체 호출 흐름

### 3.1 시퀀스 다이어그램

```
┌────────────────┐  ┌──────────────┐  ┌─────────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ Game System    │  │ UI/System    │  │ StageProgressMgr    │  │ SaveDataAdapter │  │ SaveGameManager │
└────────┬───────┘  └──────┬───────┘  └──────────┬──────────┘  └────────┬────────┘  └────────┬────────┘
         │                 │                      │                      │                    │
    (1) 게임 클리어       │                      │                      │                    │
         │                 │                      │                      │                    │
    OnVictory() ──────────>│                      │                      │                    │
         │                 │                      │                      │                    │
         │           CompleteStage(stageId, score, statistics)           │                    │
         │                 │─────────────────────>│                      │                    │
         │                 │                      │                      │                    │
         │                 │              (2) PlaySession 생성 및 업데이트                     │
         │                 │                      │                      │                    │
         │                 │              (3) 별(stars) 계산              │                    │
         │                 │                      │                      │                    │
         │                 │              (4) StageRecord 업데이트        │                    │
         │                 │                      │                      │                    │
         │                 │              (5) 통계 누적                   │                    │
         │                 │                      │                      │                    │
         │                 │              (6) 이벤트 발행                 │                    │
         │                 │<─────────────────────│                      │                    │
         │      OnStageCompleted(stageId, score, stars)                  │                    │
         │                 │                      │                      │                    │
         │                 │              (7) SaveProgress()             │                    │
         │                 │                      │                      │                    │
         │                 │                      │ SaveSpecific(StageProgress)                │
         │                 │                      │─────────────────────>│                    │
         │                 │                      │                      │                    │
         │                 │                      │      CollectStageProgress()               │
         │                 │                      │<─────────────────────│                    │
         │                 │                      │                      │                    │
         │                 │                      │      GetProgressData()                    │
         │                 │                      │─────────────────────>│                    │
         │                 │                      │                      │                    │
         │                 │                      │  StageProgressData   │                    │
         │                 │                      │<─────────────────────│                    │
         │                 │                      │                      │                    │
         │                 │                      │                   SaveToFile()            │
         │                 │                      │                      │───────────────────>│
         │                 │                      │                      │                    │
         │                 │                      │                      │    (8) JSON 직렬화 │
         │                 │                      │                      │                    │
         │                 │                      │                      │    (9) 파일 저장   │
         │                 │                      │                      │                    │
         │                 │                      │                      │  OnDataSaved()     │
         │                 │                      │                      │<───────────────────│
         │                 │                      │                      │                    │
         │                 │                      │ HandleDataSaved()    │                    │
         │                 │                      │<─────────────────────│                    │
         │                 │                      │                      │                    │
```

### 3.2 상세 호출 스택

```
1. GameOutcomeManager.OnEnemyBaseDeath()
   └─> OnVictory?.Invoke() [이벤트 발생] (line 172)

2. UI/Game System [이벤트 구독자]
   └─> StageProgressManager.CompleteStage(stageId, score, statistics)

3. StageProgressManager.CompleteStage() (line 144-227)
   │
   ├─ [Step 1] PlaySession 업데이트 (line 152-161)
   │   ├─ currentSession.endTime = DateTime.Now
   │   ├─ currentSession.score = score
   │   ├─ currentSession.playTime = (endTime - startTime).TotalSeconds
   │   ├─ currentSession.completed = true
   │   └─ currentSession.statistics = statistics
   │
   ├─ [Step 2] 별(stars) 계산 (line 172)
   │   └─ int stars = stageData.CalculateStars(score)
   │
   ├─ [Step 3] StageRecord 가져오기/생성 (line 176)
   │   └─ StageRecord record = GetOrCreateRecord(stageId)
   │
   ├─ [Step 4] PlaySession 추가 (line 180)
   │   └─ record.playSessions.Add(currentSession)
   │
   ├─ [Step 5] 최고 기록 업데이트 (line 183-188)
   │   ├─ if (score > record.bestScore)
   │   │   ├─ record.bestScore = score
   │   │   ├─ record.bestStars = stars
   │   │   └─ OnStageScoreUpdated?.Invoke(stageId, score, stars)
   │   │
   │   └─ 최초 클리어인 경우
   │       ├─ record.firstClearDate = DateTime.Now
   │       └─ record.state = (stars >= 3) ? Perfect : Cleared
   │
   ├─ [Step 6] 상태 업데이트 (line 191-197)
   │   ├─ record.state = (stars >= 3) ? Perfect : Cleared
   │   └─ record.lastPlayDate = DateTime.Now
   │
   ├─ [Step 7] 통계 누적 (line 200)
   │   └─ UpdateStatistics(record, currentSession)
   │       ├─ record.statistics.totalEnemiesDefeated += enemies
   │       ├─ record.statistics.totalDamageDealt += damageDealt
   │       ├─ record.statistics.totalDamageTaken += damageTaken
   │       └─ if (damageTaken == 0) perfectClearCount++
   │
   ├─ [Step 8] 전역 진행도 업데이트 (line 203)
   │   └─ UpdateGlobalProgress()
   │       ├─ 총 클리어 스테이지 수 재계산
   │       ├─ 총 별 개수 재계산
   │       ├─ 총 점수 재계산
   │       └─ 챕터별 진행도 업데이트
   │
   ├─ [Step 9] 다음 스테이지 자동 해금 (line 206)
   │   └─ CheckAndUnlockNextStages()
   │       └─ UnlockCondition 체크 후 자동 해금
   │
   ├─ [Step 10] 보상 처리 (line 210)
   │   └─ ProcessRewards(rewards)
   │       └─ 코인, 경험치, 아이템 배분
   │
   ├─ [Step 11] 이벤트 발행 (line 213-215)
   │   ├─ OnStageCompleted?.Invoke(stageId, score, stars)
   │   ├─ OnStageStateChanged?.Invoke(stageId, record.state)
   │   └─ OnProgressUpdated?.Invoke(progressData)
   │
   └─ [Step 12] 자동 저장 (line 218-221)
       └─ if (autoSaveEnabled) SaveProgress()

4. StageProgressManager.SaveProgress() (line 461-482)
   │
   ├─ progressData.lastModified = DateTime.Now (line 472)
   │
   └─ saveAdapter.SaveSpecific(SaveFileType.StageProgress) (line 474)

5. SaveDataAdapter.SaveSpecific(SaveFileType.StageProgress) (line 164-190)
   │
   ├─ [데이터 수집] CollectStageProgress() (line 421-432)
   │   └─> stageProgressMgr.GetProgressData()
   │       └─> return progressData (메모리의 StageProgressData)
   │
   └─ [저장 요청] saveManager.SaveToFile() (line 181-182)
       └─> SaveGameManager.SaveToFile(
             "stage_progress.json",
             stageProgress,
             SaveFileType.StageProgress
           )

6. SaveGameManager.SaveToFile() (SaveGameManager.cs)
   │
   ├─ [JSON 직렬화]
   │   └─ string json = JsonUtility.ToJson(data, prettyPrint: true)
   │
   ├─ [파일 저장]
   │   ├─ string path = Application.persistentDataPath + "/stage_progress.json"
   │   └─ File.WriteAllText(path, json)
   │
   └─ [이벤트 발행]
       └─ OnDataSaved?.Invoke(SaveFileType.StageProgress)

7. SaveDataAdapter.HandleDataSaved(SaveFileType.StageProgress) (line 595-613)
   └─> Debug.Log("[SaveDataAdapter] StageProgress saved successfully")
```

---

## 4. 핵심 객체 및 함수

### 4.1 StageProgressManager

**파일**: `Assets/Script/Managers/StageProgressManager.cs`

| 메서드 | 라인 | 역할 | 파라미터 | 반환 |
|--------|------|------|----------|------|
| `CompleteStage()` | 144-227 | 스테이지 클리어 처리 | stageId, score, statistics | void |
| `StartStage()` | 229-257 | 스테이지 시작 (PlaySession 생성) | stageId | PlaySession |
| `UnlockStage()` | 259-291 | 스테이지 해금 | stageId, save=true | bool |
| `SaveProgress()` | 461-482 | 진행도 저장 | - | void |
| `LoadProgress()` | 487-509 | 진행도 로드 | - | void |
| `GetOrCreateRecord()` | 528-548 | StageRecord 조회/생성 | stageId | StageRecord |
| `UpdateStatistics()` | 312-360 | 통계 누적 업데이트 | record, session | void |
| `UpdateGlobalProgress()` | 362-425 | 전역 진행도 업데이트 | - | void |
| `CheckAndUnlockNextStages()` | 427-459 | 다음 스테이지 자동 해금 | - | void |
| `ProcessRewards()` | (미구현) | 보상 처리 | rewards | void |

### 4.2 SaveDataAdapter

**파일**: `Assets/Script/SaveSystem/Core/SaveDataAdapter.cs`

| 메서드 | 라인 | 역할 | 파라미터 | 반환 |
|--------|------|------|----------|------|
| `SaveSpecific()` | 164-190 | 특정 타입만 저장 | SaveFileType | void |
| `LoadSpecific()` | 192-218 | 특정 타입만 로드 | SaveFileType | void |
| `QuickSave()` | 114-137 | 모든 데이터 일괄 저장 | - | void |
| `QuickLoad()` | 139-162 | 모든 데이터 일괄 로드 | - | void |
| `CollectStageProgress()` | 421-432 | StageProgressManager에서 데이터 수집 | - | StageProgressData |
| `ApplyStageProgress()` | 434-443 | 로드된 데이터를 StageProgressManager에 적용 | StageProgressData | void |

### 4.3 SaveGameManager

**파일**: `Assets/Script/SaveSystem/Core/SaveGameManager.cs`

| 메서드 | 역할 | 파라미터 | 반환 |
|--------|------|----------|------|
| `SaveToFile()` | JSON 파일로 저장 | fileName, data, fileType | void |
| `LoadData<T>()` | JSON 파일에서 로드 | fileName, fileType | T |
| `DeleteSaveFile()` | 저장 파일 삭제 | fileName | bool |
| `SaveExists()` | 저장 파일 존재 확인 | fileName | bool |

### 4.4 GameOutcomeManager

**파일**: `Assets/Script/Game/Services/GameOutcomeManager.cs`

| 이벤트 | 라인 | 발생 조건 |
|--------|------|-----------|
| `OnVictory` | 172 | 적팀 본진이 파괴될 때 |
| `OnDefeat` | 177 | 아군 본진이 파괴될 때 |

---

## 5. 자동 저장 설정

### 5.1 설정 필드
**파일**: `StageProgressManager.cs`

```csharp
[Header("Auto Save Settings")]
[SerializeField]
private bool autoSaveEnabled = true;              // Line 24 - 자동 저장 활성화

[SerializeField]
private float autoSaveInterval = 60f;             // Line 27 - 주기적 저장 간격 (초)

[SerializeField]
private bool debugMode = false;                   // Line 30 - 디버그 로그
```

### 5.2 자동 저장 발생 시점

| 발생 시점 | 트리거 | 라인 | 코드 |
|----------|--------|------|------|
| **스테이지 클리어** | CompleteStage() 호출 후 | 218-221 | `if (autoSaveEnabled) SaveProgress()` |
| **주기적 저장** | Update() 60초마다 | 569-578 | `if (Time.time >= lastAutoSaveTime + autoSaveInterval)` |
| **스테이지 해금** | UnlockStage() 호출 시 | 248-251 | `if (save) SaveProgress()` |
| **수동 저장** | 직접 SaveProgress() 호출 | - | 언제든지 호출 가능 |

### 5.3 주기적 자동 저장 로직
**파일**: `StageProgressManager.cs` (line 569-578)

```csharp
private void Update()
{
    if (autoSaveEnabled && Time.time >= lastAutoSaveTime + autoSaveInterval)
    {
        SaveProgress();
        lastAutoSaveTime = Time.time;

        if (debugMode)
            Debug.Log($"[StageProgressManager] Auto-save triggered at {Time.time}");
    }
}
```

---

## 6. 저장 파일 구조

### 6.1 저장 위치
- **파일명**: `stage_progress.json`
- **경로**: `Application.persistentDataPath/stage_progress.json`
- **플랫폼별 경로**:
  - Windows: `%userprofile%/AppData/LocalLow/CompanyName/GameName/stage_progress.json`
  - macOS: `~/Library/Application Support/CompanyName/GameName/stage_progress.json`
  - Android: `/data/data/com.CompanyName.GameName/files/stage_progress.json`
  - iOS: `/var/mobile/Containers/Data/Application/APP_ID/Documents/stage_progress.json`

### 6.2 JSON 구조 예시

```json
{
  "stageRecords": {
    "chapter1_stage1": {
      "stageId": "chapter1_stage1",
      "state": 4,
      "bestScore": 15000,
      "bestStars": 3,
      "playSessions": [
        {
          "startTime": "2025-11-09T10:30:00.000Z",
          "endTime": "2025-11-09T10:45:30.500Z",
          "score": 15000,
          "stars": 3,
          "playTime": 930.5,
          "completed": true,
          "statistics": {
            "enemies_defeated": 42,
            "damage_dealt": 5000,
            "damage_taken": 0,
            "towers_destroyed": 5,
            "units_produced": 30
          }
        },
        {
          "startTime": "2025-11-09T11:00:00.000Z",
          "endTime": "2025-11-09T11:12:15.200Z",
          "score": 12500,
          "stars": 2,
          "playTime": 735.2,
          "completed": true,
          "statistics": {
            "enemies_defeated": 38,
            "damage_dealt": 4200,
            "damage_taken": 150,
            "towers_destroyed": 4,
            "units_produced": 25
          }
        }
      ],
      "firstUnlockDate": "2025-11-09T10:00:00.000Z",
      "firstClearDate": "2025-11-09T10:45:30.500Z",
      "lastPlayDate": "2025-11-09T11:12:15.200Z",
      "metadata": {
        "difficulty": "normal",
        "custom_flag": true
      },
      "statistics": {
        "totalEnemiesDefeated": 80,
        "totalDamageDealt": 9200,
        "totalDamageTaken": 150,
        "perfectClearCount": 1,
        "bestClearTime": 735.2,
        "customStats": {
          "total_towers_destroyed": 9,
          "total_units_produced": 55
        }
      }
    },
    "chapter1_stage2": {
      "stageId": "chapter1_stage2",
      "state": 1,
      "bestScore": 0,
      "bestStars": 0,
      "playSessions": [],
      "firstUnlockDate": "2025-11-09T10:45:30.500Z",
      "firstClearDate": "0001-01-01T00:00:00.000Z",
      "lastPlayDate": "0001-01-01T00:00:00.000Z",
      "metadata": {},
      "statistics": {
        "totalEnemiesDefeated": 0,
        "totalDamageDealt": 0,
        "totalDamageTaken": 0,
        "perfectClearCount": 0,
        "bestClearTime": 0,
        "customStats": {}
      }
    }
  },
  "globalStats": {
    "totalStagesUnlocked": 5,
    "totalStagesCleared": 1,
    "totalStarCount": 3,
    "totalScore": 15000,
    "totalPlayTime": 1665.7,
    "perfectClearCount": 1,
    "lastPlayDate": "2025-11-09T11:12:15.200Z"
  },
  "lastModified": "2025-11-09T11:12:15.200Z"
}
```

### 6.3 StageState 값 매핑

```json
{
  "state": 0  // Locked (잠김)
  "state": 1  // Unlocked (해금됨)
  "state": 2  // InProgress (진행 중)
  "state": 3  // Cleared (클리어, 별 3개 미만)
  "state": 4  // Perfect (퍼펙트, 별 3개)
}
```

---

## 7. 사용 예시

### 7.1 게임 클리어 시 저장 흐름

```csharp
// 게임 종료 시스템에서 승리 이벤트 구독
void OnEnable()
{
    if (ServiceLocator.IsRegistered<GameOutcomeManager>())
    {
        var outcomeManager = ServiceLocator.Get<GameOutcomeManager>();
        outcomeManager.OnVictory += HandleVictory;
    }
}

// 승리 시 호출
private void HandleVictory()
{
    // 1. 점수 및 통계 계산
    int finalScore = CalculateFinalScore();
    var statistics = new Dictionary<string, int>
    {
        { "enemies_defeated", enemyKillCount },
        { "damage_dealt", totalDamageDealt },
        { "damage_taken", totalDamageTaken },
        { "towers_destroyed", towersDestroyed }
    };

    // 2. StageProgressManager에 클리어 정보 전달
    if (ServiceLocator.IsRegistered<StageProgressManager>())
    {
        var progressManager = ServiceLocator.Get<StageProgressManager>();
        progressManager.CompleteStage(currentStageId, finalScore, statistics);

        // 3. 자동 저장이 활성화되어 있으면 자동으로 저장됨
        // 비활성화되어 있으면 수동 저장 필요
        // progressManager.SaveProgress();
    }
}
```

### 7.2 수동 저장

```csharp
// 특정 시점에 수동으로 저장
if (ServiceLocator.IsRegistered<StageProgressManager>())
{
    var progressManager = ServiceLocator.Get<StageProgressManager>();
    progressManager.SaveProgress();
}

// 또는 SaveDataAdapter를 통해 전체 저장
if (ServiceLocator.IsRegistered<SaveDataAdapter>())
{
    var saveAdapter = ServiceLocator.Get<SaveDataAdapter>();
    saveAdapter.QuickSave(); // 모든 데이터 저장
}
```

### 7.3 진행도 로드

```csharp
// 게임 시작 시 진행도 로드
if (ServiceLocator.IsRegistered<StageProgressManager>())
{
    var progressManager = ServiceLocator.Get<StageProgressManager>();
    progressManager.LoadProgress();

    // 특정 스테이지 정보 조회
    var stageInfo = progressManager.GetStageInfo("chapter1_stage1");
    Debug.Log($"Best Score: {stageInfo.bestScore}, Stars: {stageInfo.bestStars}");
}
```

---

## 8. 이벤트 시스템

### 8.1 StageProgressManager 이벤트

**파일**: `StageProgressManager.cs` (line 32-39)

```csharp
// 스테이지 완료 시
public event Action<string, int, int> OnStageCompleted;              // (stageId, score, stars)

// 스테이지 상태 변경 시
public event Action<string, StageState> OnStageStateChanged;         // (stageId, newState)

// 진행도 업데이트 시
public event Action<StageProgressData> OnProgressUpdated;            // (progressData)

// 스테이지 해금 시
public event Action<string> OnStageUnlocked;                         // (stageId)

// 최고 점수 갱신 시
public event Action<string, int, int> OnStageScoreUpdated;          // (stageId, newScore, newStars)

// 전체 진행도 변경 시
public event Action<float> OnOverallProgressChanged;                 // (progressPercentage)
```

### 8.2 이벤트 구독 예시

```csharp
void Start()
{
    if (ServiceLocator.IsRegistered<StageProgressManager>())
    {
        var progressManager = ServiceLocator.Get<StageProgressManager>();

        // 스테이지 완료 이벤트 구독
        progressManager.OnStageCompleted += HandleStageCompleted;

        // 진행도 업데이트 이벤트 구독
        progressManager.OnProgressUpdated += HandleProgressUpdated;
    }
}

private void HandleStageCompleted(string stageId, int score, int stars)
{
    Debug.Log($"Stage {stageId} completed! Score: {score}, Stars: {stars}");

    // UI 업데이트
    UpdateStageUI(stageId, score, stars);

    // 결과 화면 표시
    ShowResultScreen(score, stars);
}

private void HandleProgressUpdated(StageProgressData progressData)
{
    Debug.Log($"Progress updated! Total cleared: {progressData.globalStats.totalStagesCleared}");

    // 전체 진행도 UI 업데이트
    UpdateProgressUI(progressData);
}

void OnDestroy()
{
    if (ServiceLocator.IsRegistered<StageProgressManager>())
    {
        var progressManager = ServiceLocator.Get<StageProgressManager>();

        // 이벤트 구독 해제
        progressManager.OnStageCompleted -= HandleStageCompleted;
        progressManager.OnProgressUpdated -= HandleProgressUpdated;
    }
}
```

---

## 9. 디버그 및 검증

### 9.1 디버그 로그 활성화

```csharp
// Inspector에서 설정
[SerializeField]
private bool debugMode = true;  // StageProgressManager에서 활성화

// 또는 런타임에 설정
if (ServiceLocator.IsRegistered<StageProgressManager>())
{
    var progressManager = ServiceLocator.Get<StageProgressManager>();
    // debugMode는 private이므로 Inspector에서만 설정 가능
}
```

### 9.2 저장 파일 검증

```csharp
// 저장 파일 존재 확인
string filePath = Path.Combine(Application.persistentDataPath, "stage_progress.json");
bool fileExists = File.Exists(filePath);
Debug.Log($"Save file exists: {fileExists}");

// 저장된 JSON 내용 확인
if (fileExists)
{
    string json = File.ReadAllText(filePath);
    Debug.Log($"Saved JSON:\n{json}");
}
```

### 9.3 진행도 데이터 검증

```csharp
if (ServiceLocator.IsRegistered<StageProgressManager>())
{
    var progressManager = ServiceLocator.Get<StageProgressManager>();
    var progressData = progressManager.GetProgressData();

    // 모든 스테이지 기록 출력
    foreach (var kvp in progressData.stageRecords)
    {
        string stageId = kvp.Key;
        StageRecord record = kvp.Value;

        Debug.Log($"[{stageId}] State: {record.state}, Best: {record.bestScore} ({record.bestStars}★)");
        Debug.Log($"  Play count: {record.playSessions.Count}");
        Debug.Log($"  Total enemies defeated: {record.statistics.totalEnemiesDefeated}");
    }

    // 전역 통계 출력
    Debug.Log($"Global Stats:");
    Debug.Log($"  Total Unlocked: {progressData.globalStats.totalStagesUnlocked}");
    Debug.Log($"  Total Cleared: {progressData.globalStats.totalStagesCleared}");
    Debug.Log($"  Total Stars: {progressData.globalStats.totalStarCount}");
    Debug.Log($"  Total Score: {progressData.globalStats.totalScore}");
}
```

---

## 10. 주요 참조 파일

| 파일 | 역할 |
|------|------|
| `Assets/Script/Managers/StageProgressManager.cs` | 스테이지 진행도 관리 및 클리어 처리 |
| `Assets/Script/SaveSystem/Core/SaveDataAdapter.cs` | 저장 데이터 변환 및 조정 |
| `Assets/Script/SaveSystem/Core/SaveGameManager.cs` | 파일 시스템 저장/로드 |
| `Assets/Script/SaveSystem/Data/StageProgressData.cs` | 진행도 데이터 구조 정의 |
| `Assets/Script/Game/Services/GameOutcomeManager.cs` | 게임 결과 감지 및 이벤트 발행 |
| `Assets/Script/Data/StageDataSO.cs` | 스테이지 메타 데이터 (별 계산 등) |

---

**문서 작성일**: 2025-11-09
**버전**: 1.0
**작성자**: Claude Code Analysis
