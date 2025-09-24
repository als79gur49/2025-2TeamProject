using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    public class UnitService : MonoBehaviour, IUnitService
    {
        private List<Unit> allUnits = new List<Unit>();
        
        // Phase 1: Sequential Processing System - State Management
        private PhaseExecutionContext currentContext;
        private float unitActionInterval = 0.3f; // 기본 1초 간격
        
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
            Debug.Log("[UnitService] Awake() called - Registration handled by GameInitializer");
        }
        
        private void Start()
        {
            // Periodic cleanup of dead units
            InvokeRepeating(nameof(CleanupDeadUnits), 1f, 2f);
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
            if (unit != null && unit.IsAlive)
            {
                Debug.Log($"[UnitService] Processing action for unit {unit.name} at ({unit.X}, {unit.Y})");
                unit.OnTurnStart();
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
                            unit.OnTurnStart(); // 시각적 연출이 없는 순수 로직만 실행
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
            
            for (int i = 0; i < units.Count; i++)
            {
                // 취소 요청 확인
                if (currentContext.State == PhaseExecutionState.Cancelling)
                {
                    var cancelledPhase = currentContext.Phase; // Store phase before reset
                    Debug.Log($"[UnitService] Phase execution cancelled during unit processing.");
                    ResetPhaseContext(); // Reset context BEFORE firing event
                    OnPhaseCancelled?.Invoke(cancelledPhase);
                    yield break;
                }
                    
                currentContext.CurrentUnitIndex = i;
                var unit = units[i];
                
                // 유닛 액션 실행 (예외 처리를 ProcessUnitActionAsync 내부로 이동)
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

        /// <summary>
        /// 개별 유닛의 액션을 비동기적으로 처리합니다.
        /// </summary>
        private IEnumerator ProcessUnitActionAsync(Unit unit)
        {
            if (unit != null && unit.IsAlive)
            {
                try
                {
                    Debug.Log($"[UnitService] Processing action for unit {unit.name} at ({unit.X}, {unit.Y})");
                    unit.OnTurnStart();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[UnitService] Error processing unit {unit?.name}: {ex.Message}");
                }
                
                // 유닛 액션(애니메이션 등) 완료까지 대기 (필요시)
                yield return null; // 또는 유닛 액션 완료 이벤트 대기
            }
        }
    }
}