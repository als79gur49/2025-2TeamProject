# Sequential Unit Action Processing System Design

## 🎯 **System Overview**

현재 UnitService의 ProcessUnitsForPhase() → ProcessActionPhase()에서 모든 유닛이 동시에 실행되는 문제를 해결하기 위한 순차적 실행 시스템 설계입니다.

## 🏗️ **Core Architecture**

### **1. State Management System**

```csharp
public enum PhaseExecutionState
{
    Idle,           // 대기 상태
    Executing,      // 페이즈 실행 중
    Cancelling      // 취소 진행 중
}

public class PhaseExecutionContext
{
    public TurnPhase Phase { get; set; }
    public PhaseExecutionState State { get; set; }
    public List<Unit> UnitsToProcess { get; set; }
    public int CurrentUnitIndex { get; set; }
    public float StartTime { get; set; }
    public Coroutine ExecutionCoroutine { get; set; }
}
```

### **2. Enhanced Interface Design**

```csharp
public interface IUnitService
{
    // 기존 인터페이스 유지...

    // 새로운 순차 처리 인터페이스
    bool ProcessUnitsForPhaseAsync(TurnPhase phase);
    bool CancelCurrentPhase();
    float GetPhaseProgress();
    bool IsPhaseExecuting { get; }
    TurnPhase? CurrentPhase { get; }
    
    // 설정 가능한 시간 간격
    float UnitActionInterval { get; set; }
    
    // 강화된 이벤트 시스템
    event Action<TurnPhase> OnPhaseStarted;
    event Action<TurnPhase> OnPhaseCompleted;
    event Action<TurnPhase> OnPhaseCancelled;
    event Action<Unit, int, int> OnUnitProcessed; // 유닛, 현재 인덱스, 총 개수
}
```

## ⚡ **Core Design Patterns**

### **1. Phase Isolation Pattern**
```csharp
public bool ProcessUnitsForPhaseAsync(TurnPhase phase)
{
    // Phase 중복 실행 방지
    if (currentContext?.State == PhaseExecutionState.Executing)
    {
        Debug.LogWarning($"Phase {currentContext.Phase} still executing. Cannot start {phase}");
        return false;
    }
    
    // 새 Context 생성
    currentContext = new PhaseExecutionContext
    {
        Phase = phase,
        State = PhaseExecutionState.Executing,
        UnitsToProcess = GetUnitsForPhase(phase),
        CurrentUnitIndex = 0,
        StartTime = Time.time
    };
    
    // 비동기 실행 시작
    currentContext.ExecutionCoroutine = StartCoroutine(ExecutePhaseSequentially());
    OnPhaseStarted?.Invoke(phase);
    
    return true;
}
```

### **2. Sequential Execution Engine**
```csharp
private IEnumerator ExecutePhaseSequentially()
{
    var units = currentContext.UnitsToProcess;
    
    for (int i = 0; i < units.Count; i++)
    {
        // 취소 확인
        if (currentContext.State == PhaseExecutionState.Cancelling)
            yield break;
            
        currentContext.CurrentUnitIndex = i;
        var unit = units[i];
        
        // 유닛 액션 실행
        yield return StartCoroutine(ProcessUnitActionAsync(unit));
        
        // 진행 상황 알림
        OnUnitProcessed?.Invoke(unit, i + 1, units.Count);
        
        // 다음 유닛까지 대기 (마지막 유닛 제외)
        if (i < units.Count - 1)
        {
            yield return new WaitForSeconds(unitActionInterval);
        }
    }
    
    // 페이즈 완료
    CompleteCurrentPhase();
}

private IEnumerator ProcessUnitActionAsync(Unit unit)
{
    if (unit != null && unit.IsAlive)
    {
        Debug.Log($"[UnitService] Processing action for unit {unit.name} at ({unit.X}, {unit.Y})");
        unit.OnTurnStart();
        
        // 유닛 액션 완료까지 대기 (필요시)
        yield return null; // 또는 유닛 액션 완료 이벤트 대기
    }
}
```

### **3. Cancellation System**
```csharp
public bool CancelCurrentPhase()
{
    if (currentContext?.State == PhaseExecutionState.Executing)
    {
        currentContext.State = PhaseExecutionState.Cancelling;
        
        if (currentContext.ExecutionCoroutine != null)
        {
            StopCoroutine(currentContext.ExecutionCoroutine);
        }
        
        OnPhaseCancelled?.Invoke(currentContext.Phase);
        ResetPhaseContext();
        return true;
    }
    return false;
}

private void CompleteCurrentPhase()
{
    if (currentContext != null)
    {
        OnPhaseCompleted?.Invoke(currentContext.Phase);
        OnUnitsProcessed?.Invoke(); // 기존 이벤트 호환성
        ResetPhaseContext();
    }
}

private void ResetPhaseContext()
{
    currentContext = null;
}
```

## 🔧 **Implementation Strategy**

### **1. UnitService 확장**
```csharp
public class UnitService : MonoBehaviour, IUnitService
{
    [SerializeField] private float unitActionInterval = 1.0f;
    private PhaseExecutionContext currentContext = null;
    
    // 기존 메서드들 유지 (하위 호환성)
    public void ProcessUnitsForPhase(TurnPhase phase)
    {
        // 레거시 동기 실행 (필요시 유지)
        // 또는 새로운 비동기 메서드로 리다이렉트
        ProcessUnitsForPhaseAsync(phase);
    }
    
    // 새로운 비동기 메서드들...
}
```

### **2. Configuration System**
```csharp
[System.Serializable]
public class PhaseTimingConfig
{
    public float unitActionInterval = 1.0f;
    public float phaseTransitionDelay = 0.5f;
    public bool allowEarlyTermination = true;
}
```

## 📊 **Benefits & Features**

### **✅ 요구사항 해결**
- **순차 실행**: 각 유닛이 설정된 시간 간격으로 순차 실행
- **페이즈 격리**: 이전 페이즈 완료 후 다음 페이즈 시작 보장
- **원자적 실행**: ProcessUnitsForPhase() 호출 시 해당 페이즈 완전 실행

### **🚀 추가 혜택**
- **진행 추적**: GetPhaseProgress()로 실시간 진행률 확인
- **취소 지원**: CancelCurrentPhase()로 안전한 중단
- **이벤트 강화**: 세밀한 페이즈 및 유닛 처리 이벤트
- **설정 가능**: unitActionInterval로 게임플레이 조정

## 🛠️ **Migration Plan**

### **Phase 1: Core Infrastructure**
1. PhaseExecutionState, PhaseExecutionContext 타입 추가
2. IUnitService 인터페이스 확장
3. 기본 상태 관리 시스템 구현

### **Phase 2: Sequential Execution**
1. ExecutePhaseSequentially() 코루틴 구현
2. ProcessUnitActionAsync() 개별 유닛 처리
3. 진행률 추적 및 이벤트 시스템

### **Phase 3: Enhanced Features**
1. 취소 시스템 구현
2. 설정 시스템 추가
3. 기존 시스템과의 통합 테스트

### **Phase 4: Integration**
1. 의존 시스템 업데이트
2. UI 연동 (진행률 표시 등)
3. 성능 최적화 및 튜닝

## ⚠️ **Risk Mitigation**

- **하위 호환성**: 기존 동기 메서드 유지
- **메모리 관리**: 적절한 코루틴 생명주기 관리
- **성능**: 설정 가능한 간격으로 성능 튜닝
- **안정성**: 페이즈 전환 시 철저한 상태 검증

이 설계는 요구된 순차적 유닛 실행과 페이즈 격리를 완전히 해결하면서, 시스템의 확장성과 유지보수성을 크게 향상시킵니다.