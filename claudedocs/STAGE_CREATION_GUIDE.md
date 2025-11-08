# Stage Creation Guide

Unity Editor에서 StageDataSO ScriptableObject를 생성하는 가이드입니다.

## 빠른 시작

### 1. 기본 스테이지 생성

**Unity Editor 단계**:
1. Project 창에서 `Assets/ScriptableObjects/Stages/` 폴더 생성
2. 우클릭 → Create → Game → Stage Data
3. 파일명: `StageData_Chapter1_Stage1.asset`

### 2. 필수 설정

**Stage Identification**:
- **Stage Id**: `chapter1_stage1` (자동 생성됨)
- **Chapter Id**: `chapter1`
- **Stage Number**: `1`

**Stage Information**:
- **Display Name**: `Tutorial Stage`
- **Description**: `Learn the basics...`
- **Stage Type**: `Tutorial` (자동 해금됨)

**Stage Properties**:
- **Difficulty**: `1` (1-10)
- **Is Hidden**: `false`

**Score System** (기본값 사용 가능):
- **Max Score**: `10000`
- **Star Thresholds**: `[3000, 6000, 9000]` (1성, 2성, 3성)
- **Use Rank System**: `true`
- **S Rank Score**: `9500`

**Rewards** (기본값 사용 가능):
- **Base Coin**: `100`
- **Base Exp**: `50`
- **First Clear Bonus**: `500`

## 테스트 스테이지 예제

### 예제 1: Tutorial Stage (chapter1_stage1)

```
Stage Identification:
  Stage Id: chapter1_stage1
  Chapter Id: chapter1
  Stage Number: 1

Stage Information:
  Display Name: Tutorial Stage
  Description: Learn the basic mechanics
  Stage Type: Tutorial

Stage Properties:
  Difficulty: 1
  Is Hidden: false

Unlock Conditions: (비어있음 - 튜토리얼은 자동 해금)

Score System:
  Max Score: 10000
  Star Thresholds: [3000, 6000, 9000]
  S Rank Score: 9500
  A Rank Score: 8000
  B Rank Score: 6000
  C Rank Score: 4000

Rewards:
  Base Coin: 100
  Base Exp: 50
  First Clear Bonus: 500
```

**생성 방법**:
1. Create → Game → Stage Data
2. 이름: `StageData_Chapter1_Stage1`
3. 위 값들 입력
4. Unlock Conditions는 비워둠 (튜토리얼)

---

### 예제 2: First Normal Stage (chapter1_stage2)

```
Stage Identification:
  Stage Id: chapter1_stage2
  Chapter Id: chapter1
  Stage Number: 2

Stage Information:
  Display Name: Forest Path
  Description: Clear the forest enemies
  Stage Type: Normal

Stage Properties:
  Difficulty: 2
  Is Hidden: false

Unlock Conditions:
  [0] StageClearCondition:
    - Target Stage Id: chapter1_stage1
    - Minimum Stars: 0 (클리어만 하면 됨)

Score System: (기본값)
Rewards: (기본값)
```

**UnlockCondition 생성 방법**:
1. Assets/ScriptableObjects/Conditions/ 폴더 생성
2. 우클릭 → Create → Game → Unlock Conditions → Stage Clear
3. 이름: `Condition_ClearStage1`
4. Target Stage Id: `chapter1_stage1`
5. Minimum Stars: `0`
6. StageData의 Unlock Conditions 리스트에 드래그

---

### 예제 3: Boss Stage with Complex Unlock (chapter1_stage5)

```
Stage Identification:
  Stage Id: chapter1_stage5
  Chapter Id: chapter1
  Stage Number: 5

Stage Information:
  Display Name: Forest Boss
  Description: Defeat the forest guardian
  Stage Type: Boss

Stage Properties:
  Difficulty: 5
  Is Hidden: false

Unlock Conditions:
  [0] AndCondition:
    Conditions:
      - StageClearCondition (chapter1_stage4, 최소 2성)
      - ChapterProgressCondition (chapter1, 60% 진행)

Score System:
  Max Score: 20000 (보스는 더 높은 점수)
  Star Thresholds: [6000, 12000, 18000]

Rewards:
  Base Coin: 500
  Base Exp: 200
  First Clear Bonus: 2000
```

**Complex UnlockCondition 생성**:

1. **StageClearCondition 생성**:
   - Create → Game → Unlock Conditions → Stage Clear
   - 이름: `Condition_ClearStage4_2Stars`
   - Target Stage Id: `chapter1_stage4`
   - Minimum Stars: `2`

2. **ChapterProgressCondition 생성**:
   - Create → Game → Unlock Conditions → Chapter Progress
   - 이름: `Condition_Chapter1_60Percent`
   - Chapter Id: `chapter1`
   - Required Progress: `0.6`

3. **AndCondition 생성**:
   - Create → Game → Unlock Conditions → Logic → AND
   - 이름: `Condition_BossUnlock`
   - Conditions 리스트에 위 2개 조건 드래그

4. **StageData에 적용**:
   - StageData의 Unlock Conditions에 AndCondition 드래그

---

### 예제 4: Secret Stage with Stats Requirement (chapter1_secret1)

```
Stage Identification:
  Stage Id: chapter1_secret1
  Chapter Id: chapter1
  Stage Number: 99

Stage Information:
  Display Name: Hidden Grove
  Description: A secret place discovered by true explorers
  Stage Type: Secret

Stage Properties:
  Difficulty: 6
  Is Hidden: true

Unlock Conditions:
  [0] GlobalStatsCondition:
    - Minimum Total Stars: 15 (챕터 1의 모든 스테이지에서 3성씩 필요)
    - Minimum Total Stages Cleared: 5

Score System: (기본값)
Rewards:
  Base Coin: 1000
  First Clear Bonus: 5000
  First Clear Items: ["secret_token", "rare_card"]
```

**GlobalStatsCondition 생성**:
1. Create → Game → Unlock Conditions → Global Stats
2. 이름: `Condition_Chapter1_AllThreeStars`
3. Minimum Total Stars: `15` (5개 스테이지 × 3성)
4. Minimum Total Stages Cleared: `5`

---

## 권장 스테이지 구조

### Chapter 1 (Tutorial + Basic)
- `chapter1_stage1`: Tutorial (튜토리얼)
- `chapter1_stage2`: Normal (stage1 클리어)
- `chapter1_stage3`: Normal (stage2 클리어)
- `chapter1_stage4`: Normal (stage3 클리어, 2성 필요)
- `chapter1_stage5`: Boss (stage4 2성 + chapter 60%)
- `chapter1_secret1`: Secret (전체 15성)

### Chapter 2 (Advanced)
- `chapter2_stage1`: Normal (chapter1_stage5 클리어)
- `chapter2_stage2`: Normal (chapter2_stage1 클리어)
- ...

---

## UnlockCondition 종류

### 1. StageClearCondition
```
용도: 특정 스테이지 클리어 필요
설정:
  - Target Stage Id: "chapter1_stage1"
  - Minimum Stars: 0 (클리어만) | 1 | 2 | 3
  - Required Perfect: false (노데미지 필요 시 true)
```

### 2. ChapterProgressCondition
```
용도: 챕터 진행도 필요
설정:
  - Chapter Id: "chapter1"
  - Required Progress: 0.5 (50%)
  - Minimum Stages Cleared: 3
  - Minimum Total Stars: 9
```

### 3. GlobalStatsCondition
```
용도: 전체 통계 조건
설정:
  - Minimum Total Stages Unlocked: 10
  - Minimum Total Stages Cleared: 5
  - Minimum Total Stars: 20
  - Minimum Total Play Time: 3600 (1시간)
```

### 4. StageStatsCondition
```
용도: 특정 스테이지의 상세 통계
설정:
  - Target Stage Id: "chapter1_stage3"
  - Minimum Score: 8000
  - Maximum Clear Time: 120.0 (2분 이내)
  - Minimum Perfect Clears: 1 (노데미지 1회)
```

### 5. AndCondition
```
용도: 모든 조건을 만족해야 함
설정:
  - Conditions: [조건1, 조건2, ...]
```

### 6. OrCondition
```
용도: 하나 이상의 조건을 만족하면 됨
설정:
  - Conditions: [조건1, 조건2, ...]
  - Required Condition Count: 1 (최소 만족 개수)
```

---

## StageProgressManager 설정

StageProgressManager가 스테이지 데이터를 인식하려면:

1. **Hierarchy에서 StageProgressManager GameObject 생성** (없는 경우):
   - GameObject → Create Empty
   - 이름: StageProgressManager
   - Add Component → StageProgressManager

2. **Stage Database 설정**:
   - Inspector에서 Stage Database 리스트 확장
   - 생성한 모든 StageDataSO를 드래그하여 추가
   - Size를 늘려서 여러 스테이지 추가 가능

3. **Settings 확인**:
   - Auto Save Enabled: true
   - Auto Save Interval: 60 (60초마다 자동 저장)
   - Debug Mode: true (테스트 시)

---

## 디버그 및 테스트

### 에디터 메뉴 사용

StageProgressManager에 우클릭 메뉴가 있습니다:

- **Reset Progress**: 진행도 초기화
- **Unlock All Stages**: 모든 스테이지 해금
- **Print Progress Stats**: 현재 통계 출력

### 코드에서 테스트

```csharp
// 스테이지 해금 테스트
var manager = StageProgressManager.Instance;
manager.UnlockStage("chapter1_stage2");

// 스테이지 클리어 테스트
var statistics = new Dictionary<string, int>
{
    ["enemies_defeated"] = 10,
    ["damage_taken"] = 50
};
manager.StartStage("chapter1_stage1");
manager.CompleteStage("chapter1_stage1", 8500, statistics);

// 진행도 확인
bool isUnlocked = manager.IsStageUnlocked("chapter1_stage2");
var record = manager.GetStageRecord("chapter1_stage1");
Debug.Log($"Best Score: {record.bestScore}, Stars: {record.bestStars}");
```

---

## 주의사항

1. **StageId는 유니크해야 함**: 중복되면 Dictionary에서 충돌 발생
2. **Chapter1의 첫 스테이지는 Tutorial 타입 권장**: 자동 해금되어 게임 시작 가능
3. **UnlockCondition은 순환 참조 금지**: A가 B 필요, B가 A 필요 → 무한 대기
4. **Star Thresholds는 오름차순**: [3000, 6000, 9000] ✅, [9000, 3000, 6000] ❌
5. **Scene To Load 설정 필수**: StageButton에서 씬 로드 시 사용됨

---

## 예제 프로젝트 구조

```
Assets/
  ScriptableObjects/
    Stages/
      Chapter1/
        StageData_Chapter1_Stage1.asset (Tutorial)
        StageData_Chapter1_Stage2.asset
        StageData_Chapter1_Stage3.asset
        StageData_Chapter1_Stage4.asset
        StageData_Chapter1_Stage5.asset (Boss)
        StageData_Chapter1_Secret1.asset (Secret)
      Chapter2/
        ...
    Conditions/
      StageClear/
        Condition_ClearStage1.asset
        Condition_ClearStage2_2Stars.asset
        ...
      ChapterProgress/
        Condition_Chapter1_60Percent.asset
        ...
      GlobalStats/
        Condition_AllStars15.asset
        ...
      Composite/
        Condition_BossUnlock.asset (AndCondition)
        ...
```

---

## 다음 단계

1. ✅ StageDataSO 생성 (이 가이드)
2. ⏭️ UnlockCondition 생성 및 연결
3. ⏭️ StageProgressManager에 등록
4. ⏭️ UI 설정 (StageButton)
5. ⏭️ 실제 게임 씬 생성 및 연결

---

## 문제 해결

### "Stage not found" 에러
→ StageProgressManager의 Stage Database에 추가했는지 확인

### "Unlock condition failed" 경고
→ UnlockCondition의 Target Stage Id가 올바른지 확인

### 스테이지가 해금되지 않음
→ Debug Mode 켜고 "Print Progress Stats"로 현재 상태 확인
→ UnlockCondition의 GetProgress()로 진행도 확인 (0.0~1.0)

### 통계가 업데이트되지 않음
→ CompleteStage() 호출 시 statistics Dictionary 전달 확인
→ 키 이름 확인: "enemies_defeated", "damage_dealt", "damage_taken"
