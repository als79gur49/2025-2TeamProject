# Sequential Unit Action Processing System Design (v2.0)

## 🎯 **System Overview**

현재 `UnitService`의 `ProcessUnitsForPhase()` → `ProcessActionPhase()`에서 모든 유닛이 동시에 실행되는 문제를 해결하기 위한 순차적 실행 시스템 설계입니다. v2.0에서는 **즉시 완료 기능이 포함된 취소 시스템**과 **핵심 서비스(`GameServiceManager`, `GameService`)와의 연동 방안**을 명시하여 통합 안정성을 높였습니다.

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
    // bool completeAllActions: true일 경우 남은 유닛들의 로직을 즉시 실행하고 종료
    bool CancelCurrentPhase(bool completeAllActions = false); 
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
        // 취소 요청 확인
        if (currentContext.State == PhaseExecutionState.Cancelling)
        {
            Debug.Log($"[UnitService] Phase execution cancelled during unit processing.");
            OnPhaseCancelled?.Invoke(currentContext.Phase);
            ResetPhaseContext();
            yield break;
        }
            
        currentContext.CurrentUnitIndex = i;
        var unit = units[i];
        
        // 유닛 액션 실행 (예외 처리 추가)
        try
        {
            yield return StartCoroutine(ProcessUnitActionAsync(unit));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UnitService] Error processing unit {unit.name}: {ex.Message}");
        }

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
        
        // 유닛 액션(애니메이션 등) 완료까지 대기 (필요시)
        yield return null; // 또는 유닛 액션 완료 이벤트 대기
    }
}
```

### **3. Enhanced Cancellation System**

사용자가 턴 종료를 요청할 경우, 단순 중단이 아닌 **즉시 완료** 처리가 가능하도록 시스템을 강화합니다. `CancelCurrentPhase`는 `completeAllActions` 플래그를 통해 두 가지 모드로 동작합니다.

```csharp
public bool CancelCurrentPhase(bool completeAllActions = false)
{
    if (currentContext?.State != PhaseExecutionState.Executing)
    {
        return false;
    }

    currentContext.State = PhaseExecutionState.Cancelling;
    
    // 진행 중인 메인 코루틴 중지
    if (currentContext.ExecutionCoroutine != null)
    {
        StopCoroutine(currentContext.ExecutionCoroutine);
    }
    
    // 즉시 완료 옵션 처리
    if (completeAllActions)
    {
        Debug.Log($"[UnitService] Instantly completing remaining actions for phase {currentContext.Phase}...");
        var units = currentContext.UnitsToProcess;
        // 현재 유닛부터 마지막 유닛까지의 행동 로직을 시각적 딜레이 없이 즉시 실행
        for (int i = currentContext.CurrentUnitIndex; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit != null && unit.IsAlive)
            {
                unit.OnTurnStart(); // 시각적 연출이 없는 순수 로직만 실행
            }
        }
        // 모든 액션을 완료했으므로, Phase 'Completed' 이벤트 호출
        OnPhaseCompleted?.Invoke(currentContext.Phase);
    }
    else
    {
        // 단순 취소이므로, Phase 'Cancelled' 이벤트 호출
        OnPhaseCancelled?.Invoke(currentContext.Phase);
    }

    OnUnitsProcessed?.Invoke(); // 기존 이벤트 호환성
    ResetPhaseContext();
    return true;
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

## 🔗 **Integration with Core Services**

이 비동기 시스템을 게임에 통합하기 위해, 중앙 관리자인 `GameServiceManager`와 `GameService`의 수정이 반드시 필요합니다.

### **1. `GameServiceManager` 연동**

`HandlePhaseChanged`가 `UnitService`의 동기 메서드를 호출하는 기존 방식은 비동기 흐름과 충돌합니다. `ProcessUnitsForPhaseAsync`를 호출하여 비동기 처리를 '시작'만 시키도록 변경해야 합니다.

```csharp
// GameServiceManager.cs
private void HandlePhaseChanged(TurnPhase phase)
{
    LogEvent($"🔄 Phase changed to: {phase}. Starting unit processing...");
    OnPhaseChanged?.Invoke(phase);
    
    // UnitService의 비동기 처리를 '요청'하고 즉시 리턴
    if (unitService != null && !unitService.ProcessUnitsForPhaseAsync(phase))
    {
        // 만약 실행에 실패했다면 (예: 이전 페이즈가 아직 실행 중), 게임 멈춤을 방지
        LogWarning("Unit processing could not be started. A previous phase might still be running.");
    }
}
```

### **2. `GameService` 연동**

사용자의 턴 종료 요청(`HandleEndPhaseRequest`)이 `TurnService`를 직접 제어하지 않고, `UnitService`의 상태에 따라 현재 페이즈를 즉시 완료시키도록 변경해야 합니다.

```csharp
// GameService.cs
private void HandleEndPhaseRequest()
{
    if (!IsGameActive) return;
    
    Debug.Log("[GameService] Processing end phase request...");
    
    // UnitService가 현재 페이즈를 실행 중인지 확인
    if (unitService.IsPhaseExecuting)
    {
        // 실행 중이라면, 즉시 완료 옵션과 함께 취소를 요청
        Debug.Log("[GameService] Phase is executing. Requesting immediate completion.");
        unitService.CancelCurrentPhase(true); // true: 남은 유닛 로직 즉시 실행
    }
    
    // UnitService의 처리가 완료되었으므로, 안전하게 다음 페이즈로 전환
    turnService.EndCurrentPhase();
}
```

## 🛠️ **Migration Plan**

### **Phase 1: Core Infrastructure**

1.  `PhaseExecutionState`, `PhaseExecutionContext` 타입 추가
2.  `IUnitService` 인터페이스 확장 (`CancelCurrentPhase(bool)` 포함)
3.  `UnitService`에 기본 상태 관리 시스템 및 비동기 메서드 골격 구현

### **Phase 2: Sequential Execution**

1.  `ExecutePhaseSequentially` 코루틴 및 예외 처리 로직 구현
2.  `ProcessUnitActionAsync` 개별 유닛 처리 로직 구현
3.  진행률 추적 및 `OnUnitProcessed` 등 이벤트 시스템 연동

### **Phase 3: Enhanced Features & Integration**

1.  **즉시 완료** 기능이 포함된 `CancelCurrentPhase(bool)` 시스템 구현
2.  `GameServiceManager`의 `HandlePhaseChanged`를 수정하여 비동기 처리 호출
3.  `GameService`의 `HandleEndPhaseRequest`를 수정하여 안전한 턴 종료 로직 구현
4.  설정 시스템 추가 (`unitActionInterval` 등)

### **Phase 4: Finalization & UI**

1.  UI 연동: `OnPhaseStarted` 이벤트 발생 시 '턴 종료' 버튼 비활성화, `OnPhaseCompleted`/`OnPhaseCancelled` 이벤트 발생 시 다시 활성화하여 사용자 오입력 방지
2.  전체 통합 테스트 및 성능 최적화

## ⚠️ **Risk Mitigation**

  - **하위 호환성**: 기존 동기 메서드(`ProcessUnitsForPhase`)는 `ProcessUnitsForPhaseAsync`를 호출하는 Wrapper로 유지하여 기존 코드의 급격한 변경을 최소화합니다.
  - **메모리 관리**: `ResetPhaseContext()`를 통해 페이즈 종료 시 Context 객체를 명확히 `null`로 만들어 가비지 컬렉션이 원활하도록 합니다.
  - **UI 상태 불일치**: `OnPhaseStarted`, `OnPhaseCompleted` 이벤트를 사용하여 유닛이 순차 행동하는 동안에는 '턴 종료' 버튼을 비활성화시켜 사용자의 중복 입력을 막습니다.
  - **비동기 예외 처리**: `ExecutePhaseSequentially` 내부에 `try-catch` 블록을 추가하여, 특정 유닛의 액션에서 에러가 발생하더라도 전체 페이즈 실행이 멈추지 않도록 방지합니다.
  - **안정성**: 페이즈 전환 시 `IsPhaseExecuting` 상태를 철저히 검증하여, 코루틴이 중복 실행되는 것을 원천적으로 차단합니다.