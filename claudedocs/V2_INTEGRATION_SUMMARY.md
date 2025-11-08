# Stage System V2 Integration Summary

**통합 완료일**: 2025-11-08
**전략**: Big Bang (전체 교체, V1 삭제)
**결과**: ✅ 성공 (8 Phases 완료)

---

## 통합된 파일 목록

### 핵심 시스템 파일

| 원본 파일 (V2) | 최종 위치 | 상태 |
|----------------|-----------|------|
| `StageProgressManagerV2.cs` | `Assets/Script/Managers/StageProgressManager.cs` | ✅ 통합 완료 |
| `StageProgressDataV2.cs` | `Assets/Script/SaveSystem/Data/StageProgressData.cs` | ✅ 통합 완료 |
| `StageDataSOV2.cs` | `Assets/Script/ScriptableObjects/StageDataSO.cs` | ✅ 통합 완료 |
| `SaveDataAdapterV2.cs` | `Assets/Script/SaveSystem/Core/SaveDataAdapter.cs` | ✅ 대체 완료 |
| `StageButtonV2.cs` | `Assets/Script/UI/Game/StageButton.cs` | ✅ 통합 완료 |

### UnlockCondition 시스템

| 원본 파일 (V2) | 최종 위치 | 상태 |
|----------------|-----------|------|
| `UnlockConditionV2.cs` | `Assets/Script/ScriptableObjects/Conditions/UnlockCondition.cs` | ✅ 통합 완료 |
| `StageClearConditionV2.cs` | `Assets/Script/ScriptableObjects/Conditions/StageClearCondition.cs` | ✅ 통합 완료 |
| `ChapterProgressConditionV2.cs` | `Assets/Script/ScriptableObjects/Conditions/ChapterProgressCondition.cs` | ✅ 통합 완료 |
| `CompositeConditionsV2.cs` | `Assets/Script/ScriptableObjects/Conditions/CompositeConditions.cs` | ✅ 통합 완료 |
| `StatsConditionsV2.cs` | `Assets/Script/ScriptableObjects/Conditions/StatsConditions.cs` | ✅ 통합 완료 |

---

## 삭제된 V1 파일

| V1 파일 | 삭제 이유 |
|---------|-----------|
| `Assets/Script/Managers/StageProgressManager.cs` (V1) | V2로 대체 |
| `Assets/Script/SaveSystem/Data/StageProgressData.cs` (V1) | V2로 대체 |

---

## 주요 변경사항

### 1. 아키텍처 개선

**V1 (List 기반)**:
```csharp
List<StageRecord> stageRecords;
// O(n) 검색: foreach로 stageId 찾기
```

**V2 (Dictionary 기반)**:
```csharp
Dictionary<string, StageRecord> stageRecords;
// O(1) 검색: stageRecords[stageId] 직접 접근
```

### 2. 스테이지 식별 방식 변경

**V1**:
- Chapter/Stage 번호 기반
- `GetStageRecord(int chapter, int stage)`

**V2**:
- 유니크 ID 기반 (예: "chapter1_stage3")
- `GetStageRecord(string stageId)`

### 3. UnlockCondition 시스템 강화

**새로운 조건 타입**:
- `StageClearCondition`: 특정 스테이지 클리어 조건
- `ChapterProgressCondition`: 챕터 진행도 조건
- `GlobalStatsCondition`: 전체 통계 조건
- `StageStatsCondition`: 스테이지별 상세 통계 조건
- `AndCondition` / `OrCondition`: 복합 논리 조건

### 4. 통계 시스템 추가

**StageStatistics**:
```csharp
public class StageStatistics
{
    public int totalEnemiesDefeated;
    public int totalDamageDealt;
    public int totalDamageTaken;
    public int perfectClearCount;  // 노데미지 클리어
    public float bestClearTime;
    public Dictionary<string, int> customStats;
}
```

**PlaySession 추적**:
- 각 플레이 세션별 기록 저장
- 시작/종료 시간, 점수, 별, 통계 등 상세 기록

---

## Phase별 작업 내용

### Phase 1: V1 시스템 파일 삭제 ✅
- `StageProgressManager.cs` (V1) 삭제
- `StageProgressData.cs` (V1) 삭제

### Phase 2: V2 파일 재배치 및 이름 변경 ✅
- 11개 V2 파일을 적절한 디렉토리로 이동
- 모든 클래스명에서 "V2" 접미사 제거
- `sed` 명령으로 일괄 변경

### Phase 3: 코드 내부 정리 ✅
- `StageDataSO.cs`에서 레거시 메서드 제거:
  - `ConvertToLegacyFormat()` 삭제
  - `ParseStageId()` 삭제
- CreateAssetMenu 경로 수정: "Game/Stage Data V2" → "Game/Stage Data"

### Phase 4: SaveSystem 통합 ✅
- `SaveDataAdapter.cs`:
  - 상수명 변경: `STAGE_PROGRESS_V2_FILE` → `STAGE_PROGRESS_FILE`
  - 모든 "V2" 주석 제거
  - 파일 경로: `"stage_progress.dat"` (일관성 유지)
- Newtonsoft.Json 직렬화 확인

### Phase 5: ServiceLocator 등록 확인 ✅
- `ServiceBootstrap.cs` (PHASE 2.5):
  - `SaveGameManager` 생성
  - `SaveDataAdapter` 생성 및 초기화
  - `ISaveDataAdapter` → ServiceLocator 등록
- `StageProgressManager`:
  - 자체 ServiceLocator 등록
  - Singleton 패턴 유지

**초기화 순서**:
```
1. SceneLoaderService
2. AudioServiceContainer
2.5. SaveSystem (SaveGameManager + SaveDataAdapter)
3. SceneTransitionController
4. GlobalUIPanelManager
```

### Phase 6: 통계 시스템 확인 ✅
- `StageProgressManager.UpdateStatistics()` 동작 확인
- 표준 통계: enemies_defeated, damage_dealt, damage_taken
- 커스텀 통계: Dictionary에 저장
- UnlockCondition에서 통계 활용 확인

### Phase 7: 테스트 스테이지 문서 생성 ✅
- `claudedocs/STAGE_CREATION_GUIDE.md` 작성
- 4가지 예제 스테이지 구성:
  - Tutorial Stage (자동 해금)
  - Normal Stage (조건부 해금)
  - Boss Stage (복합 조건)
  - Secret Stage (통계 조건)
- Unity Editor 생성 가이드
- UnlockCondition 설정 방법

### Phase 8: 네임스페이스 정리 및 검증 ✅
- ✅ "V2" 네임스페이스 참조 없음
- ✅ "V2" 클래스명 참조 없음
- ✅ "V2" 파일 없음
- ✅ 모든 핵심 클래스 정상 위치 확인:
  - StageProgressManager.cs
  - StageProgressData.cs
  - StageDataSO.cs
  - UnlockCondition.cs
  - SaveDataAdapter.cs
  - StageButton.cs

---

## 네임스페이스 구조

```csharp
namespace Game.Data
{
    // StageDataSO, UnlockCondition 시스템
    public class StageDataSO : ScriptableObject { }
    public abstract class UnlockCondition : ScriptableObject { }
    public class StageClearCondition : UnlockCondition { }
    // ... 기타 Condition 클래스들
}

namespace Game.SaveSystem
{
    // 진행도 데이터 및 Save/Load
    public class StageProgressData { }
    public class StageRecord { }
    public class PlaySession { }
    public class StageStatistics { }
    public class SaveDataAdapter : MonoBehaviour, ISaveDataAdapter { }
}

namespace Game.Managers
{
    // 진행도 관리
    public class StageProgressManager : MonoBehaviour { }
}

namespace Game.UI
{
    // UI 컴포넌트
    public class StageButton : MonoBehaviour { }
}
```

---

## 데이터 흐름

### 저장 흐름
```
1. 게임 플레이 완료
2. StageProgressManager.CompleteStage(stageId, score, statistics)
3. PlaySession 생성 및 기록
4. StageRecord 업데이트 (bestScore, bestStars)
5. StageStatistics 업데이트
6. PlayerProgressStats 갱신
7. AutoSave 또는 QuickSave 호출
8. SaveDataAdapter.SaveStageProgress()
9. JSON 직렬화 → "stage_progress.dat" 저장
```

### 로드 흐름
```
1. 게임 시작 또는 QuickLoad 호출
2. SaveDataAdapter.LoadStageProgress()
3. "stage_progress.dat" 읽기
4. JSON 역직렬화 → StageProgressData
5. StageProgressManager.SetProgressData()
6. 진행도 UI 업데이트
```

### 해금 체크 흐름
```
1. StageButton.UpdateButtonState()
2. StageDataSO.IsUnlocked(progressData)
3. UnlockCondition.Evaluate(progressData)
4. 조건 만족 시 스테이지 해금
5. UI 업데이트 (잠금 해제 표시)
```

---

## 주요 기능

### 1. 스테이지 관리
- ✅ Dictionary 기반 O(1) 접근
- ✅ 유니크 ID 기반 관리
- ✅ 자동 해금 (튜토리얼)
- ✅ 조건부 해금 (UnlockCondition)

### 2. 진행도 추적
- ✅ 스테이지별 최고 점수/별
- ✅ 플레이 세션 기록
- ✅ 챕터별 진행률
- ✅ 전체 통계 (해금/클리어/별/점수)

### 3. 통계 시스템
- ✅ 기본 통계 (적 처치, 데미지)
- ✅ 커스텀 통계 (Dictionary)
- ✅ 퍼펙트 클리어 추적
- ✅ 최고 클리어 시간

### 4. UnlockCondition 시스템
- ✅ 6가지 조건 타입
- ✅ 논리 조합 (AND/OR)
- ✅ 진행도 표시 (0.0~1.0)
- ✅ 설명 텍스트 자동 생성

### 5. 저장/불러오기
- ✅ Newtonsoft.Json 직렬화
- ✅ 자동 저장 (60초 간격)
- ✅ 파일 분할 저장
- ✅ 버전 관리 (dataVersion = 2)

---

## 테스트 가이드

### Unity Editor 테스트

1. **StageProgressManager 설정**:
   ```
   Hierarchy → Create Empty → StageProgressManager
   Add Component → StageProgressManager
   Inspector → Stage Database에 StageDataSO 추가
   ```

2. **디버그 메뉴 사용**:
   - 우클릭 → Reset Progress: 진행도 초기화
   - 우클릭 → Unlock All Stages: 모든 스테이지 해금
   - 우클릭 → Print Progress Stats: 통계 출력

3. **런타임 테스트**:
   ```csharp
   var manager = StageProgressManager.Instance;

   // 스테이지 해금
   manager.UnlockStage("chapter1_stage2");

   // 스테이지 시작
   manager.StartStage("chapter1_stage1");

   // 스테이지 완료
   var stats = new Dictionary<string, int>
   {
       ["enemies_defeated"] = 10,
       ["damage_taken"] = 0  // 퍼펙트 클리어
   };
   manager.CompleteStage("chapter1_stage1", 9500, stats);

   // 진행도 확인
   var record = manager.GetStageRecord("chapter1_stage1");
   Debug.Log($"Score: {record.bestScore}, Stars: {record.bestStars}");
   ```

---

## 주의사항

### 필수 설정

1. **ServiceBootstrap 설정**:
   - BootstrapScene에 ServiceBootstrap GameObject 추가
   - Script Execution Order: -100으로 설정

2. **StageProgressManager 설정**:
   - Stage Database에 모든 StageDataSO 등록
   - Auto Save Enabled: true
   - Auto Save Interval: 60

3. **StageDataSO 생성**:
   - StageId는 유니크해야 함
   - Scene To Load 설정 필수
   - 첫 스테이지는 Tutorial 타입 권장

### 알려진 제약사항

1. **초기화 순서 경고**:
   - StageProgressManager 초기화 시 "SaveDataAdapter not found" 경고 발생 가능
   - 원인: ISaveDataAdapter가 아직 등록되지 않은 시점
   - 영향: 없음 (정상 동작, 경고만 발생)

2. **UnlockCondition 순환 참조**:
   - A가 B를 필요로 하고, B가 A를 필요로 하는 구조 금지
   - 결과: 무한 대기 상태

3. **ScriptableObject 참조**:
   - UnlockCondition은 ScriptableObject로 생성 후 참조
   - 코드에서 직접 new로 생성 불가

---

## 다음 단계

### 즉시 가능
1. ✅ Unity Editor에서 StageDataSO 생성
2. ✅ UnlockCondition 생성 및 연결
3. ✅ StageProgressManager에 등록
4. ✅ StageButton UI 설정

### 추후 작업
1. ⏭️ 실제 게임 씬 생성 (Scene To Load 연결)
2. ⏭️ UI 디자인 및 애니메이션
3. ⏭️ 보상 시스템 구현 (코인/경험치 적용)
4. ⏭️ 업적 시스템 연동

---

## 파일 참조

### 문서
- `claudedocs/STAGE_CREATION_GUIDE.md`: 스테이지 생성 가이드
- `claudedocs/V2_INTEGRATION_SUMMARY.md`: 이 파일

### 핵심 코드
- `Assets/Script/Managers/StageProgressManager.cs`: 진행도 관리자
- `Assets/Script/SaveSystem/Data/StageProgressData.cs`: 데이터 구조
- `Assets/Script/ScriptableObjects/StageDataSO.cs`: 스테이지 정의
- `Assets/Script/ScriptableObjects/Conditions/UnlockCondition.cs`: 해금 조건
- `Assets/Script/UI/Game/StageButton.cs`: UI 컴포넌트
- `Assets/Script/SaveSystem/Core/SaveDataAdapter.cs`: Save/Load 어댑터

### 시스템 코드
- `Assets/Script/Game/Core/ServiceBootstrap.cs`: 서비스 초기화
- `Assets/Script/Core/ServiceLocator.cs`: 의존성 관리

---

## 결론

**V2 스테이지 시스템 통합 완료**:
- ✅ 모든 V1 코드 제거
- ✅ V2 코드 완전 통합
- ✅ 네임스페이스 정리
- ✅ ServiceLocator 등록
- ✅ 통계 시스템 활성화
- ✅ 문서화 완료

**성능 개선**:
- List → Dictionary: O(n) → O(1) 검색
- 메모리 효율: Dictionary 기반 빠른 접근
- 확장성: UnlockCondition, Statistics 시스템

**다음 작업**:
1. Unity Editor에서 StageDataSO 생성 (STAGE_CREATION_GUIDE.md 참조)
2. 실제 게임 씬 제작
3. UI 디자인 및 구현
4. 플레이 테스트 및 밸런싱
