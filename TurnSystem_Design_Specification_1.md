Turn System Design Specification
📋 개요 (Overview)
현재의 이진(Binary) 턴 시스템을 적 소환 → 아군 소환 → 적 행동 → 아군 행동의 4-페이즈(4-Phase) 턴 시스템으로 전환하고, 그리드 순서에 따른 유닛 처리를 지원하기 위한 포괄적인 설계 문서입니다.

🎯 핵심 요구사항 분석 (Core Requirements Analysis)
현재 시스템 상태 (Current System State)
턴 관리: 단순 토글 방식의 플레이어/적 이진 시스템

유닛 처리: 순서 없는 기본적인 팀 기반 처리

서비스 아키텍처: 이벤트 집계를 포함한 잘 정립된 GameServiceManager

그리드 시스템: 유닛의 위치를 추적하는 깔끔한 아키텍처

필수 변경 사항 (Required Changes)
4-페이즈 턴 시스템: 이진 시스템을 구조화된 페이즈 진행 방식으로 교체합니다.

소환 페이즈: 유닛 소환 기능 (구현은 플레이스홀더)

행동 페이즈: 그리드 순서(우상단에서 좌하단)에 따른 유닛 처리

하위 호환성: 기존 이벤트 시스템 및 서비스 통합을 유지합니다.

GameService 역할 재정의: GameService가 턴 로직을 직접 제어하는 대신, 페이즈 종료 '요청'만 보내도록 책임을 축소합니다.

UIService 개선: 4가지 페이즈 상태를 유저에게 명확히 피드백하도록 UI 로직을 강화합니다.

🏗️ 아키텍처 설계 (Architecture Design)
1. 턴 페이즈 열거형 (Turn Phase Enumeration)
C#

namespace Game.Services
{
    /// <summary>
    /// 전체 턴 사이클을 구성하는 네 가지 페이즈를 정의합니다.
    /// </summary>
    public enum TurnPhase
    {
        EnemySummon = 0,    // 적군 소환 턴
        AllySummon = 1,     // 아군 소환 턴  
        EnemyAction = 2,    // 적군 행동 턴
        AllyAction = 3      // 아군 행동 턴
    }
}
설계 근거:

순차적인 숫자 값은 페이즈 전환 로직을 단순화합니다.

명확한 네이밍 컨벤션은 요구사항과 일치합니다.

고정된 순서는 예측 가능한 게임 흐름을 보장합니다.

2. 향상된 ITurnService 인터페이스 (Enhanced ITurnService Interface)
C#

namespace Game.Services
{
    public interface ITurnService
    {
        // 페이즈 관리 속성
        TurnPhase CurrentPhase { get; }
        int TurnCount { get; }          // 전체 턴 사이클 (4 페이즈마다 1 증가)
        int PhaseCount { get; }         // 진행된 총 페이즈 수
        
        // 레거시 호환성 속성
        bool IsPlayerTurn { get; }      // AllySummon & AllyAction 페이즈에서 true
        
        // 페이즈 제어 메서드
        void StartGame();               // EnemySummon 페이즈로 초기화
        void StartCurrentPhase();       // 현재 페이즈 처리 시작
        void EndCurrentPhase();         // 현재 페이즈 종료 및 다음 페이즈로 전환
        
        // 페이즈 질의 메서드
        bool IsSummonPhase { get; }     // 소환 페이즈인지 확인
        bool IsActionPhase { get; }     // 행동 페이즈인지 확인
        bool IsEnemyPhase { get; }      // 적의 페이즈인지 확인
        bool IsAllyPhase { get; }       // 아군의 페이즈인지 확인
        
        // 이벤트 (하위 호환성 유지)
        event Action<bool> OnTurnChanged;           // 레거시 호환용
        event Action<int> OnTurnCountChanged;       // 턴 사이클 변경 시
        event Action<TurnPhase> OnPhaseChanged;     // 신규: 페이즈 변경 시
        event Action<int> OnPhaseCountChanged;      // 신규: 페이즈 카운트 변경 시
    }
}
주요 설계 특징:

하위 호환성: 기존의 IsPlayerTurn 속성과 관련 이벤트를 보존합니다.

세분화된 페이즈 관리: 페이즈별 로직을 위한 신규 이벤트를 제공합니다.

편리한 질의 메서드: 페이즈 유형을 쉽게 확인할 수 있습니다.

명확한 의미: 턴 '사이클'과 개별 '페이즈'를 명확히 구분합니다.

3. TurnService 구현 (TurnService Implementation)
C#

public class TurnService : MonoBehaviour, ITurnService
{
    [Header("Turn State")]
    [SerializeField] private TurnPhase currentPhase = TurnPhase.EnemySummon;
    [SerializeField] private int turnCount = 0;
    [SerializeField] private int phaseCount = 0;
    
    // 속성 구현
    public TurnPhase CurrentPhase => currentPhase;
    public int TurnCount => turnCount;
    public int PhaseCount => phaseCount;
    
    // 레거시 호환성
    public bool IsPlayerTurn => IsAllyPhase;
    
    // 페이즈 질의 속성
    public bool IsSummonPhase => currentPhase == TurnPhase.EnemySummon || currentPhase == TurnPhase.AllySummon;
    public bool IsActionPhase => currentPhase == TurnPhase.EnemyAction || currentPhase == TurnPhase.AllyAction;
    public bool IsEnemyPhase => currentPhase == TurnPhase.EnemySummon || currentPhase == TurnPhase.EnemyAction;
    public bool IsAllyPhase => currentPhase == TurnPhase.AllySummon || currentPhase == TurnPhase.AllyAction;
    
    // 이벤트
    public event Action<bool> OnTurnChanged;
    public event Action<int> OnTurnCountChanged;
    public event Action<TurnPhase> OnPhaseChanged;
    public event Action<int> OnPhaseCountChanged;
    
    public void StartGame()
    {
        currentPhase = TurnPhase.EnemySummon;
        turnCount = 0;
        phaseCount = 0;
        
        // 초기 이벤트 발생
        OnPhaseChanged?.Invoke(currentPhase);
        OnPhaseCountChanged?.Invoke(phaseCount);
        OnTurnChanged?.Invoke(IsPlayerTurn);
        OnTurnCountChanged?.Invoke(turnCount);
        
        Debug.Log($"[TurnService] Game started - Phase: {currentPhase}");
    }
    
    public void StartCurrentPhase()
    {
        Debug.Log($"[TurnService] Phase {currentPhase} started");
        // 페이즈별 초기화 로직 추가 가능
    }
    
    public void EndCurrentPhase()
    {
        var previousPhase = currentPhase;
        var wasPlayerTurn = IsPlayerTurn;
        
        // 다음 페이즈로 전환
        currentPhase = GetNextPhase(currentPhase);
        phaseCount++;
        
        // 4 페이즈마다 턴 카운트 증가
        if (phaseCount % 4 == 0)
        {
            turnCount++;
            OnTurnCountChanged?.Invoke(turnCount);
        }
        
        // 이벤트 발생
        OnPhaseChanged?.Invoke(currentPhase);
        OnPhaseCountChanged?.Invoke(phaseCount);
        
        // 플레이어 턴 상태가 변경되었을 때만 레거시 이벤트 발생
        if (wasPlayerTurn != IsPlayerTurn)
        {
            OnTurnChanged?.Invoke(IsPlayerTurn);
        }
        
        Debug.Log($"[TurnService] Phase changed: {previousPhase} → {currentPhase}");
    }
    
    private TurnPhase GetNextPhase(TurnPhase current)
    {
        return current switch
        {
            TurnPhase.EnemySummon => TurnPhase.AllySummon,
            TurnPhase.AllySummon => TurnPhase.EnemyAction,
            TurnPhase.EnemyAction => TurnPhase.AllyAction,
            TurnPhase.AllyAction => TurnPhase.EnemySummon,
            _ => TurnPhase.EnemySummon
        };
    }
}
4. 그리드 순서 기반 유닛 처리 시스템 (Grid-Ordered Unit Processing System)
C#

// 향상된 UnitService의 페이즈별 처리 메서드
public class UnitService : MonoBehaviour, IUnitService
{
    // ... 기존 코드 ...
    
    /// <summary>
    /// 현재 턴 페이즈에 맞춰 적절한 순서로 유닛을 처리합니다.
    /// </summary>
    public void ProcessUnitsForPhase(TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.EnemySummon:
                ProcessSummonPhase(false); // 적 소환
                break;
                
            case TurnPhase.AllySummon:
                ProcessSummonPhase(true); // 아군 소환
                break;
                
            case TurnPhase.EnemyAction:
                ProcessActionPhase(false); // 적 행동
                break;
                
            case TurnPhase.AllyAction:
                ProcessActionPhase(true); // 아군 행동
                break;
        }
    }
    
    /// <summary>
    /// 소환 페이즈를 처리합니다 (플레이스홀더 구현).
    /// </summary>
    private void ProcessSummonPhase(bool isPlayerUnits)
    {
        Debug.Log($"[UnitService] Processing summon phase for {(isPlayerUnits ? "Player" : "Enemy")} units");
        // TODO: 유닛 소환 로직 구현 필요
        OnUnitsProcessed?.Invoke();
    }
    
    /// <summary>
    /// 그리드 순서에 따라 행동 페이즈를 처리합니다.
    /// </summary>
    private void ProcessActionPhase(bool isPlayerUnits)
    {
        var units = GetUnitsInGridOrder(isPlayerUnits);
        
        Debug.Log($"[UnitService] Processing {units.Count} {(isPlayerUnits ? "player" : "enemy")} units in grid order");
        
        foreach (var unit in units)
        {
            ProcessUnitAction(unit);
        }
        
        OnUnitsProcessed?.Invoke();
    }
    
    /// <summary>
    /// 그리드 위치(우상단에서 좌하단)에 따라 정렬된 유닛 리스트를 가져옵니다.
    /// </summary>
    public List<Unit> GetUnitsInGridOrder(bool isPlayerUnits)
    {
        var units = GetActiveUnits(isPlayerUnits);
        
        // 그리드 순서: Y 내림차순 (상단 -> 하단), X 내림차순 (우측 -> 좌측)
        return units.OrderByDescending(unit => unit.Y)
                   .ThenByDescending(unit => unit.X)
                   .ToList();
    }
    
    /// <summary>
    /// 개별 유닛의 행동을 처리합니다.
    /// </summary>
    private void ProcessUnitAction(Unit unit)
    {
        Debug.Log($"[UnitService] Processing action for unit {unit.name} at ({unit.X}, {unit.Y})");
        // TODO: 유닛 행동 로직 구현
    }
}
5. 향상된 IUnitService 인터페이스 (Enhanced IUnitService Interface)
C#

namespace Game.Services
{
    public interface IUnitService
    {
        // ... 기존 인터페이스 멤버 ...
        
        // 신규 페이즈별 처리 메서드
        void ProcessUnitsForPhase(TurnPhase phase);
        List<Unit> GetUnitsInGridOrder(bool isPlayerUnits);
        
        // ... 기존 이벤트 ...
    }
}
6. GameServiceManager 통합 (GameServiceManager Integration)
C#

public class GameServiceManager : MonoBehaviour
{
    // ... 기존 이벤트 ...
    public event Action<bool> OnTurnChanged;
    public event Action<int> OnTurnCountChanged;
    // ...
    
    // 페이즈 관리를 위한 신규 이벤트 추가
    public event Action<TurnPhase> OnPhaseChanged;
    public event Action<int> OnPhaseCountChanged;
    
    private void ConnectServiceEvents()
    {
        // ... 기존 TurnService 이벤트 ...
        turnService.OnTurnChanged += HandleTurnChanged;
        turnService.OnTurnCountChanged += HandleTurnCountChanged;
        
        // 신규 페이즈 이벤트
        turnService.OnPhaseChanged += HandlePhaseChanged;
        turnService.OnPhaseCountChanged += HandlePhaseCountChanged;
        
        // ... 기존 UnitService 및 GameService 이벤트 ...
        
        // UIService의 이벤트 핸들러 이름 변경을 반영
        uiService.OnEndTurnRequested += HandleEndPhaseRequested;
        uiService.OnRestartRequested += HandleRestartRequested;
        
        areEventsConnected = true;
        LogEvent("🔗 All service events connected (including new phase events)");
    }

    // `GameService`의 책임 변경을 반영한 핸들러
    private void HandleEndPhaseRequested()
    {
        LogEvent("🔚 End phase requested by user");
        // GameService는 더 이상 유닛 처리나 턴 종료를 직접 호출하지 않음
        // 이 이벤트는 GameService를 통해 TurnService.EndCurrentPhase()를 호출하도록 연결됨
        // (GameService 내부 로직 변경 필요)
        OnEndTurnRequested?.Invoke(); // 기존 이벤트 이름 유지 또는 변경 가능
    }
    
    // 신규 이벤트 핸들러
    private void HandlePhaseChanged(TurnPhase phase)
    {
        LogEvent($"🔄 Phase changed: {phase}");
        OnPhaseChanged?.Invoke(phase);
        
        // 페이즈 변경 시 자동으로 해당 페이즈의 유닛 처리 로직을 트리거
        unitService?.ProcessUnitsForPhase(phase);
    }
    
    // ...
}
📝 구현 계획 (Implementation Plan)
1단계: 핵심 기반 구축
TurnPhase Enum 생성: Game.Services 네임스페이스에 페이즈 열거형 정의

ITurnService 인터페이스 업데이트: 페이즈 관리 속성 및 이벤트 추가

향상된 TurnService 구현: 하위 호환성을 유지하며 4-페이즈 로직 구현

2단계: 유닛 처리 강화
IUnitService 인터페이스 확장: 페이즈별 처리 메서드 추가

그리드 순서 처리 구현: 우상단부터 좌하단까지 유닛 정렬 로직 구현

페이즈별 로직 분리: 소환과 행동 페이즈 처리 로직 분리

3단계: 서비스 통합 및 리팩토링
GameServiceManager 업데이트: 신규 페이즈 이벤트 및 핸들러 추가

GameService 리팩토링: HandleEndTurnRequest를 HandleEndPhaseRequest로 변경하고, turnService.EndCurrentPhase()만 호출하도록 로직 단순화

UIService 리팩토링: OnPhaseChanged 이벤트를 구독하여, 4가지 페이즈 상태를 모두 UI에 정확히 표시하도록 수정

4단계: 테스트 및 검증
단위 테스트: 페이즈 전환 로직 및 그리드 정렬 기능 검증

통합 테스트: 서비스 간의 연동 및 이벤트 흐름 검증

호환성 테스트: 기존 기능이 문제없이 동작하는지 확인

🔄 턴 흐름 다이어그램 (Turn Flow Diagram)
Game Start
    ↓
EnemySummon Phase (적 소환)
    ↓ (EndCurrentPhase 트리거)
AllySummon Phase (아군 소환)
    ↓ (EndCurrentPhase 트리거)
EnemyAction Phase → 적 유닛 처리 (그리드 순서: 우상단 -> 좌하단)
    ↓ (EndCurrentPhase 트리거)
AllyAction Phase → 아군 유닛 처리 (그리드 순서: 우상단 -> 좌하단)
    ↓ (EndCurrentPhase 트리거, TurnCount++)
EnemySummon Phase (다음 사이클 시작)
    ↓
...
🎯 주요 이점 (Key Benefits)
✅ 아키텍처 개선
구조화된 턴 흐름: 이진 시스템을 명확한 4-페이즈 진행 방식으로 대체

그리드 기반 순서: 예측 가능한 유닛 행동 순서 (우상단부터 좌하단까지)

향상된 이벤트: 세분화된 페이즈별 알림 기능

하위 호환성: 기존 코드에 대한 파괴적인 변경 최소화

🧪 테스트 전략 (Testing Strategy)
단위 테스트 (Unit Tests)
C#

[Test]
public void TurnService_EndCurrentPhase_ShouldAdvancePhaseCorrectly() { /*...*/ }

[Test]
public void TurnService_CompleteFourPhases_ShouldIncrementTurnCount() { /*...*/ }

[Test]
public void UnitService_GetUnitsInGridOrder_ShouldReturnTopRightToBottomLeft() { /*...*/ }
통합 테스트 (Integration Tests)
C#

[Test]
public void GameServiceManager_PhaseTransition_ShouldTriggerUnitProcessing()
{
    // GIVEN: GameServiceManager가 생성되고 이벤트가 연결됨
    var manager = CreateGameServiceManager();
    var unitsProcessedFired = false;
    manager.OnUnitsProcessed += () => unitsProcessedFired = true;
    
    // WHEN: 페이즈가 종료됨
    manager.GetComponent<TurnService>().EndCurrentPhase();
    
    // THEN: 유닛 처리 이벤트가 자동으로 발생해야 함
    Assert.IsTrue(unitsProcessedFired);
}
📋 마이그레이션 체크리스트 (Migration Checklist)
사전 구현
[ ] 코드베이스 전체에서 TurnService 사용 현황 검토

[ ] IsPlayerTurn에 하드코딩된 참조가 있는지 식별

[ ] 현재 턴 기반 게임 로직의 의존성 문서화

구현 단계
[ ] TurnPhase enum 생성

[ ] ITurnService 인터페이스 업데이트

[ ] 향상된 TurnService 구현

[ ] IUnitService 인터페이스에 페이즈별 메서드 추가

[ ] UnitService에 그리드 순서 처리 기능 구현

[ ] GameServiceManager에 신규 페이즈 이벤트 추가

[ ] GameService의 HandleEndTurnRequest 로직 리팩토링

[ ] UIService가 OnPhaseChanged를 구독하여 UI를 업데이트하도록 수정

[ ] 모든 신규 기능에 대한 단위 테스트 추가

[ ] 서비스 연동을 위한 통합 테스트 실행

사후 구현
[ ] UnitService의 레거시 메서드(ProcessUnitsForCurrentPlayer)에 [Obsolete] 어트리뷰트 추가

[ ] 문서 및 코드 주석 업데이트

[ ] 다양한 유닛 수에 대한 성능 테스트

[ ] 4-페이즈 게임 플레이에 대한 사용자 수용 테스트(UAT)

📚 추가 고려사항 (Additional Considerations)
페이즈 전환 트리거 (Phase Transition Triggers)
EndCurrentPhase()가 호출되는 시점을 명확히 정의해야 합니다.

Summon Phase (소환 페이즈): 유저가 '소환 완료' 버튼을 누르거나, 제한 시간이 만료되면 EndCurrentPhase()를 호출합니다. 이 로직은 UIService나 GameService에서 관리될 수 있습니다.

Action Phase (행동 페이즈): 해당 팀의 모든 유닛이 행동을 완료하면 UnitService가 OnAllActionsCompleted와 같은 이벤트를 발생시킵니다. GameService는 이 이벤트를 수신하여 자동으로 EndCurrentPhase()를 호출해야 합니다. 이를 통해 불필요한 사용자 입력을 줄일 수 있습니다.

향후 개선 사항 (Future Enhancements)
동적 페이즈 길이: 페이즈별로 다른 시간 제한 설정

페이즈별 UI: 소환과 행동 페이즈에 각기 다른 UI 제공

고급 그리드 처리: 그리드 위치 내에서 우선순위에 따른 유닛 순서 지정

오류 처리 (Error Handling)
잘못된 페이즈 전환: 상태가 오염되지 않도록 보호 장치 마련

유닛 데이터 누락: 그리드 위치가 없는 유닛을 우아하게 처리

서비스 의존성: 특정 서비스가 사용 불가능할 때의 견고한 처리 로직

이 설계 문서는 기존의 견고한 아키텍처를 유지하면서 4-페이즈 턴 시스템을 구현하기 위한 포괄적인 청사진을 제공합니다.