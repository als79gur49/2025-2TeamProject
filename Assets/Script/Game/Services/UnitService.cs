using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Services;
using Game.Components;
using Game.Interfaces;

namespace Game.Services
{
    public class UnitService : MonoBehaviour, IUnitService
    {
        private List<Unit> allUnits = new List<Unit>();
        
        // Phase 1: Sequential Processing System - State Management
        private PhaseExecutionContext currentContext;
        private float unitActionInterval = 0.14f; // 기본 1초 간격

        // GlobalStateManager integration for action chain management
        private IGlobalStateManager globalStateManager;

        public int ActiveUnitCount => allUnits.Count(u => u != null && u.IsAlive);
        
        // 기존 이벤트
        public event System.Action<Unit> OnUnitRegistered;
        public event System.Action<Unit> OnUnitUnregistered;
        public event System.Action OnUnitsProcessed;
        
        // Phase 1: 새로운 이벤트 시스템
        public event System.Action<TurnPhase> OnPhaseStarted;
        public event System.Action<TurnPhase> OnPhaseCompleted;
        public event System.Action<TurnPhase> OnPhaseCancelled;
        public event System.Action<Unit, int, int> OnUnitProcessed;
        
        // Phase 1: 새로운 속성들
        public bool IsPhaseExecuting => currentContext?.State == PhaseExecutionState.Executing;
        public TurnPhase? CurrentPhase => currentContext?.Phase;
        public float UnitActionInterval 
        { 
            get => unitActionInterval; 
            set => unitActionInterval = Mathf.Max(0.1f, value); // 최소 0.1초
        }
        
        private void Awake()
        {
            Debug.Log("[UnitService] Awake() - Waiting for manual initialization");
        }

        /// <summary>
        /// Manual initialization - called by GameServiceManager
        /// Starts periodic cleanup routine for dead units
        /// </summary>
        public void Init()
        {
            // Get GlobalStateManager from ServiceLocator
            globalStateManager = ServiceLocator.Get<IGlobalStateManager>();
            if (globalStateManager == null)
            {
                Debug.LogWarning("[UnitService] GlobalStateManager not found - turn control may not work properly");
            }

            // Periodic cleanup of dead units
            InvokeRepeating(nameof(CleanupDeadUnits), 1f, 2f);
            Debug.Log("[UnitService] Initialized - Cleanup routine started");
        }
        
        public void RegisterUnit(Unit unit)
        {
            if (unit == null || allUnits.Contains(unit)) return;
            
            allUnits.Add(unit);
            Debug.Log($"[UnitService] Unit registered: {unit.name}");
            OnUnitRegistered?.Invoke(unit);
        }
        
        public void UnregisterUnit(Unit unit)
        {
            if (allUnits.Remove(unit))
            {
                Debug.Log($"[UnitService] Unit unregistered: {unit?.name}");
                OnUnitUnregistered?.Invoke(unit);
            }
        }
        
        public List<Unit> GetActiveUnits(bool? isPlayerUnit = null)
        {
            return allUnits.Where(u => u != null && u.IsAlive && 
                                      (isPlayerUnit == null || u.IsPlayerUnit == isPlayerUnit))
                          .ToList();
        }
        
        public int GetUnitCount(bool isPlayerUnit)
        {
            return allUnits.Count(u => u != null && u.IsAlive && u.IsPlayerUnit == isPlayerUnit);
        }
        
        /// <summary>
        /// 그리드 위치에 따라 정렬된 유닛 리스트를 가져옵니다.
        /// 플레이어: 상단→하단, 우측→좌측 / 적군: 상단→하단, 좌측→우측 (대칭)
        /// </summary>
        public List<Unit> GetUnitsInGridOrder(bool isPlayerUnits)
        {
            var units = GetActiveUnits(isPlayerUnits);

            //(x, y) 우측 y, 상단 x
            if (isPlayerUnits)
            {
                // 플레이어 유닛: X 내림차순 (상단 -> 하단), y 내림차순 (우측 -> 좌측)
                return units.OrderByDescending(unit => unit.X)
                           .ThenByDescending(unit => unit.Y)
                           .ToList();
            }
            else
            {
                // 적군 유닛: x 내림차순 (상단 -> 하단), y 오름차순 (좌측 -> 우측) - 대칭
                return units.OrderByDescending(unit => unit.X)
                           .ThenBy(unit => unit.Y)
                           .ToList();
            }
        }
        
        private void CleanupDeadUnits()
        {
            int removedCount = allUnits.RemoveAll(u => u == null || !u.IsAlive);
            if (removedCount > 0)
            {
                Debug.Log($"[UnitService] Periodic cleanup removed {removedCount} dead/null units");
            }
        }
        
        /// <summary>
        /// 특정 유닛이 죽었을 때 즉시 목록에서 제거합니다.
        /// </summary>
        public void NotifyUnitDeath(Unit deadUnit)
        {
            if (deadUnit == null) return;
            
            bool wasRemoved = allUnits.Remove(deadUnit);
            if (wasRemoved)
            {
                Debug.Log($"[UnitService] Immediately removed dead unit from list: {deadUnit.name}");
                OnUnitUnregistered?.Invoke(deadUnit);
            }
            else
            {
                Debug.LogWarning($"[UnitService] Attempted to remove dead unit {deadUnit.name}, but it was not found in allUnits list");
            }
        }
        
        // Phase 1: Sequential Processing System - Basic Implementation
        
        /// <summary>
        /// 비동기적으로 페이즈에 맞는 유닛들을 순차 처리를 시작합니다.
        /// Phase 1에서는 기본 구조만 구현하고, 실제 순차 처리는 기존 로직을 사용합니다.
        /// </summary>
        public bool ProcessUnitsForPhaseAsync(TurnPhase phase)
        {
            // Phase 중복 실행 방지
            if (currentContext?.State == PhaseExecutionState.Executing)
            {
                Debug.LogWarning($"[UnitService] Phase {currentContext.Phase} still executing. Cannot start {phase}");
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
            
            Debug.Log($"[UnitService] Starting async processing for phase {phase} with {currentContext.UnitsToProcess.Count} units");
            
            // 페이즈 시작 이벤트 발생
            OnPhaseStarted?.Invoke(phase);
            
            // Phase 2: 실제 비동기 순차 실행 시작
            currentContext.ExecutionCoroutine = StartCoroutine(ExecutePhaseSequentially());
            
            return true;
        }
        
        /// <summary>
        /// 현재 진행 중인 페이즈를 취소합니다.
        /// </summary>
        public bool CancelCurrentPhase(bool completeAllActions = false)
        {
            if (currentContext?.State != PhaseExecutionState.Executing)
            {
                return false;
            }

            currentContext.State = PhaseExecutionState.Cancelling;
            
            Debug.Log($"[UnitService] Cancelling phase {currentContext.Phase}, completeAllActions: {completeAllActions}");
            
            // 진행 중인 메인 코루틴 중지
            if (currentContext.ExecutionCoroutine != null)
            {
                StopCoroutine(currentContext.ExecutionCoroutine);
            }
            
            // Store phase before reset for event firing
            var cancelledPhase = currentContext.Phase;
            
            // 즉시 완료 옵션 처리
            if (completeAllActions)
            {
                Debug.Log($"[UnitService] Instantly completing remaining actions for phase {cancelledPhase}...");
                var units = currentContext.UnitsToProcess;
                // 현재 유닛부터 마지막 유닛까지의 행동 로직을 시각적 딜레이 없이 즉시 실행
                for (int i = currentContext.CurrentUnitIndex; i < units.Count; i++)
                {
                    var unit = units[i];
                    if (unit != null && unit.IsAlive)
                    {
                        try
                        {
                            unit.Act(); // 시각적 연출이 없는 순수 로직만 실행
                            OnUnitProcessed?.Invoke(unit, i + 1, units.Count);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[UnitService] Error instantly completing unit {unit.name}: {ex.Message}");
                        }
                    }
                }
                
                // Reset context BEFORE firing completion event
                ResetPhaseContext();
                OnPhaseCompleted?.Invoke(cancelledPhase); // 모든 액션을 완료했으므로, Phase 'Completed' 이벤트 호출
            }
            else
            {
                // Reset context BEFORE firing cancellation event
                ResetPhaseContext();
                OnPhaseCancelled?.Invoke(cancelledPhase); // 단순 취소이므로, Phase 'Cancelled' 이벤트 호출
            }

            OnUnitsProcessed?.Invoke(); // 기존 이벤트 호환성
            return true;
        }
        
        /// <summary>
        /// 현재 페이즈의 진행률을 가져옵니다.
        /// </summary>
        public float GetPhaseProgress()
        {
            if (currentContext == null || currentContext.UnitsToProcess.Count == 0)
                return 0f;
            
            // 실행 중이 아니라면 완료된 것으로 간주
            if (currentContext.State != PhaseExecutionState.Executing)
                return 1.0f;
                
            return (float)currentContext.CurrentUnitIndex / currentContext.UnitsToProcess.Count;
        }
        
        /// <summary>
        /// 페이즈에 맞는 유닛 리스트를 가져옵니다.
        /// </summary>
        private List<Unit> GetUnitsForPhase(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.EnemySummon:
                    return new List<Unit>();
                case TurnPhase.EnemyAction:
                    return GetUnitsInGridOrder(false); // 적 유닛
                    
                case TurnPhase.AllySummon:
                    return new List<Unit>();
                case TurnPhase.AllyAction:
                    return GetUnitsInGridOrder(true); // 아군 유닛

                case TurnPhase.TurnStart:
                    return GetActiveUnits(); // 모든 활성 유닛
                case TurnPhase.TurnEnd:
                    return GetActiveUnits(); // 모든 활성 유닛

                default:
                    return new List<Unit>();
            }
        }
        
        /// <summary>
        /// 현재 페이즈를 완료 처리합니다.
        /// </summary>
        private void CompleteCurrentPhase()
        {
            if (currentContext != null)
            {
                var completedPhase = currentContext.Phase; // Store phase before reset
                Debug.Log($"[UnitService] Completed phase {completedPhase}");
                ResetPhaseContext(); // Reset context BEFORE firing events
                OnPhaseCompleted?.Invoke(completedPhase); // Fire event with stored phase
                OnUnitsProcessed?.Invoke(); // 기존 이벤트 호환성
            }
        }
        
        /// <summary>
        /// 페이즈 컨텍스트를 초기화합니다.
        /// </summary>
        private void ResetPhaseContext()
        {
            currentContext = null;
        }
        
        // Phase 2: Sequential Execution Engine
        
        /// <summary>
        /// 페이즈의 유닛들을 순차적으로 실행하는 코루틴입니다.
        /// </summary>
        private IEnumerator ExecutePhaseSequentially()
        {
            var units = currentContext.UnitsToProcess;
            var phase = currentContext.Phase;

            Debug.Log($"[UnitService] Executing phase {phase} for {units.Count} units.");

            for (int i = 0; i < units.Count; i++)
            {
                if (currentContext.State == PhaseExecutionState.Cancelling)
                {
                    var cancelledPhase = currentContext.Phase;
                    Debug.Log("[UnitService] Phase execution cancelled during unit processing.");
                    ResetPhaseContext();
                    OnPhaseCancelled?.Invoke(cancelledPhase);
                    yield break;
                }

                currentContext.CurrentUnitIndex = i;
                var unit = units[i];

                if (unit != null && unit.IsAlive)
                {
                    // 페이즈별 로직을 비동기적으로 처리
                    yield return StartCoroutine(ProcessUnitActionAsync(unit, phase));

                    OnUnitProcessed?.Invoke(unit, i + 1, units.Count);
                }

                // ProcessUnitActionAsync에 조건을 넣을려고 하였지만, Try-catch구문이라 반환이 불가능하다.
                if (i < units.Count - 1 &&
                    (phase == TurnPhase.EnemyAction) || phase == TurnPhase.AllyAction)
                {
                    yield return new WaitForSeconds(unitActionInterval);
                }
            }

            CompleteCurrentPhase();
        }

        private IEnumerator ProcessUnitActionAsync(Unit unit, TurnPhase phase)
        {
            // 1. 애니메이션 컨트롤러 캐싱
            var animController = unit?.GetAnimationController();

            // 2. 페이즈별 액션 실행
            switch (phase)
            {
                case TurnPhase.TurnStart:
                    Debug.Log($"[UnitService] Turn start for unit {unit.name}");
                    try
                    {
                        unit.OnTurnStart();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[UnitService] Error in OnTurnStart for {unit?.name}: {ex.Message}");
                    }
                    break;

                case TurnPhase.EnemyAction:
                case TurnPhase.AllyAction:
                    Debug.Log($"[UnitService] Processing action for unit {unit.name} at ({unit.X}, {unit.Y})");

                    // 유닛 행동 실행 (이동 or 공격 → 애니메이션 트리거)
                    try
                    {
                        unit.Act();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[UnitService] Error in Act() for {unit?.name}: {ex.Message}");
                        yield break;
                    }

                    yield return null;

                    // 3. 애니메이션 완료 대기 (별도 코루틴으로 분리)
                    if (animController != null && animController.IsAnimationPlaying)
                    {
                        yield return StartCoroutine(WaitForAnimationComplete(animController, unit));
                    }
                    break;

                case TurnPhase.TurnEnd:
                    Debug.Log($"[UnitService] Turn end cleanup for unit: {unit.name}");
                    try
                    {
                        unit.OnTurnEnd();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[UnitService] Error in OnTurnEnd for {unit?.name}: {ex.Message}");
                    }
                    break;

                // Summon 페이즈는 현재 처리할 유닛이 없으므로 호출되지 않음
            }

            // 4. 기존 딜레이 유지 (필요 시)
            yield return null;
        }

        /// <summary>
        /// 액션 체인 완료를 대기하는 코루틴
        /// GlobalStateManager의 GameFlowLock을 체크하여 전체 체인(Attack → Movement)을 추적
        /// </summary>
        private IEnumerator WaitForAnimationComplete(IAnimationController animController, Unit unit)
        {
            Debug.Log($"[UnitService] Waiting for {unit.name} action chain to complete...");

            float timeout = 20f; // 20초 타임아웃 (전체 액션 체인 대응)
            float elapsed = 0f;

            // GlobalStateManager의 GameFlowLock을 체크하여 전체 액션 체인 대기
            // Attack → Movement 전체를 하나의 단위로 추적
            while (globalStateManager != null &&
                   globalStateManager.IsBusy(BusyType.GameFlowLock) &&
                   elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            // Unit destroyed check
            if (unit == null)
            {
                Debug.Log($"[UnitService] Unit was destroyed during action chain");
                yield break;
            }

            // Timeout handling
            if (elapsed >= timeout)
            {
                Debug.LogWarning($"[UnitService] Action chain timeout for {unit.name}, forcing completion");
                // Note: GlobalStateManager has auto-timeout at 15s, this is extra safety
            }
            else
            {
                Debug.Log($"[UnitService] Action chain completed for {unit.name}");
            }
        }
    }
}
