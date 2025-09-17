using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.Core;
using Game.Interfaces;

namespace Game.Components
{
    /// <summary>
    /// 행동 실행 담당 클래스 - 단일 책임 원칙 적용 (실행만 담당)
    /// </summary>
    public class ActionHandler : MonoBehaviour
    {
        [Header("행동 포인트 설정")]
        [SerializeField] private int maxActionPoints = 2;
        [SerializeField] private bool regenerateOnTurnStart = true;
        
        // ✅ 의존성 주입 필드
        [Inject] private IGridManager gridManager;
        private ActionValidator validator;

        // ✅ 현재 상태
        private int currentActionPoints;
        private bool isActing = false;
        private readonly Queue<IGameAction> actionQueue = new Queue<IGameAction>();

        // ✅ 이벤트
        [Header("이벤트")]
        public UnityEvent<int> OnActionPointsChanged;
        public UnityEvent<IGameAction> OnActionExecuted;
        public UnityEvent<IGameAction> OnActionFailed;
        public UnityEvent OnActionPointsExhausted;
        public UnityEvent OnActionPointsRestored;

        // ✅ 속성
        public int CurrentActionPoints => currentActionPoints;
        public int MaxActionPoints => maxActionPoints;
        public float ActionPointsPercentage => maxActionPoints > 0 ? (float)currentActionPoints / maxActionPoints : 0f;
        public bool HasActionPoints => currentActionPoints > 0;
        public bool IsActing => isActing;
        public int QueuedActionsCount => actionQueue.Count;

        private void Awake()
        {
            // 의존성 주입
            this.InjectDependencies();
            
            // ActionValidator 가져오기
            validator = GetComponent<ActionValidator>();
            if (validator == null)
            {
                Debug.LogError($"[ActionHandler] ActionValidator not found on {gameObject.name}");
            }

            // 초기화
            currentActionPoints = maxActionPoints;
        }

        private void Start()
        {
            // 서비스 유효성 검증
            if (gridManager == null)
            {
                Debug.LogError($"[ActionHandler] IGridManager not registered in ServiceLocator");
            }
        }

        /// <summary>
        /// 행동 실행 - 유효성 검증 후 실행
        /// </summary>
        public bool ExecuteAction(IGameAction action)
        {
            if (action == null)
            {
                Debug.LogWarning("[ActionHandler] Attempted to execute null action");
                return false;
            }

            // 유효성 검증
            if (validator != null && !validator.ValidateAction(action))
            {
                OnActionFailed?.Invoke(action);
                return false;
            }

            // 행동 포인트 확인
            if (!CanAffordAction(action))
            {
                Debug.LogWarning($"[ActionHandler] Not enough action points for {action.GetType().Name}");
                OnActionFailed?.Invoke(action);
                return false;
            }

            return ExecuteActionInternal(action);
        }

        /// <summary>
        /// 행동 대기열에 추가
        /// </summary>
        public bool QueueAction(IGameAction action)
        {
            if (action == null) return false;
            
            if (validator != null && !validator.ValidateAction(action))
            {
                return false;
            }

            actionQueue.Enqueue(action);
            return true;
        }

        /// <summary>
        /// 대기열의 다음 행동 실행
        /// </summary>
        public bool ExecuteNextQueuedAction()
        {
            if (actionQueue.Count == 0 || isActing)
                return false;

            var action = actionQueue.Dequeue();
            return ExecuteAction(action);
        }

        /// <summary>
        /// 모든 대기열 행동 실행
        /// </summary>
        public void ExecuteAllQueuedActions()
        {
            while (actionQueue.Count > 0 && !isActing)
            {
                if (!ExecuteNextQueuedAction())
                    break;
            }
        }

        /// <summary>
        /// 내부 행동 실행 로직
        /// </summary>
        private bool ExecuteActionInternal(IGameAction action)
        {
            isActing = true;

            try
            {
                // 행동 포인트 소모
                ConsumeActionPoints(action.ActionCost);

                // 행동 실행
                var result = action.Execute();
                
                if (result.Success)
                {
                    OnActionExecuted?.Invoke(action);
                    Debug.Log($"[ActionHandler] Successfully executed {action.GetType().Name}");
                }
                else
                {
                    // 실패 시 행동 포인트 복구
                    RestoreActionPoints(action.ActionCost);
                    OnActionFailed?.Invoke(action);
                    Debug.LogWarning($"[ActionHandler] Action execution failed: {result.ErrorMessage}");
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                // 예외 발생 시 행동 포인트 복구
                RestoreActionPoints(action.ActionCost);
                OnActionFailed?.Invoke(action);
                Debug.LogError($"[ActionHandler] Exception during action execution: {ex.Message}");
                return false;
            }
            finally
            {
                isActing = false;
            }
        }

        /// <summary>
        /// 행동 비용 확인
        /// </summary>
        public bool CanAffordAction(IGameAction action)
        {
            return action != null && currentActionPoints >= action.ActionCost;
        }

        /// <summary>
        /// 행동 포인트 소모
        /// </summary>
        public bool ConsumeActionPoints(int cost)
        {
            if (cost < 0 || currentActionPoints < cost)
                return false;

            currentActionPoints = Mathf.Max(0, currentActionPoints - cost);
            OnActionPointsChanged?.Invoke(currentActionPoints);

            if (currentActionPoints == 0)
            {
                OnActionPointsExhausted?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// 행동 포인트 복구
        /// </summary>
        public void RestoreActionPoints(int amount)
        {
            if (amount <= 0) return;

            int previousPoints = currentActionPoints;
            currentActionPoints = Mathf.Min(maxActionPoints, currentActionPoints + amount);
            
            if (currentActionPoints != previousPoints)
            {
                OnActionPointsChanged?.Invoke(currentActionPoints);
                
                if (previousPoints == 0 && currentActionPoints > 0)
                {
                    OnActionPointsRestored?.Invoke();
                }
            }
        }

        /// <summary>
        /// 행동 포인트 완전 회복
        /// </summary>
        public void RestoreAllActionPoints()
        {
            if (currentActionPoints < maxActionPoints)
            {
                currentActionPoints = maxActionPoints;
                OnActionPointsChanged?.Invoke(currentActionPoints);
                OnActionPointsRestored?.Invoke();
            }
        }

        /// <summary>
        /// 최대 행동 포인트 설정
        /// </summary>
        public void SetMaxActionPoints(int newMax)
        {
            maxActionPoints = Mathf.Max(1, newMax);
            currentActionPoints = Mathf.Min(currentActionPoints, maxActionPoints);
            OnActionPointsChanged?.Invoke(currentActionPoints);
        }

        /// <summary>
        /// 턴 시작 시 호출 (외부에서 호출)
        /// </summary>
        public void OnTurnStart()
        {
            if (regenerateOnTurnStart)
            {
                RestoreAllActionPoints();
            }
            
            // 대기열 정리
            ClearActionQueue();
        }

        /// <summary>
        /// 턴 종료 시 호출 (외부에서 호출)
        /// </summary>
        public void OnTurnEnd()
        {
            // 행동 중이면 강제 중단
            if (isActing)
            {
                isActing = false;
            }
            
            // 대기열 정리
            ClearActionQueue();
        }

        /// <summary>
        /// 행동 대기열 정리
        /// </summary>
        public void ClearActionQueue()
        {
            actionQueue.Clear();
        }

        /// <summary>
        /// 강제 행동 중단
        /// </summary>
        public void ForceStopAction()
        {
            isActing = false;
            ClearActionQueue();
        }

        // ✅ 디버깅용 메서드
        public override string ToString()
        {
            return $"ActionHandler[AP:{currentActionPoints}/{maxActionPoints}, Acting:{isActing}, Queue:{actionQueue.Count}]";
        }

        private void OnValidate()
        {
            maxActionPoints = Mathf.Max(1, maxActionPoints);
        }
    }

    /// <summary>
    /// 게임 행동 인터페이스
    /// </summary>
    public interface IGameAction
    {
        string Name { get; }
        int ActionCost { get; }
        ActionResult Execute();
        bool CanExecute();
    }

    /// <summary>
    /// 행동 실행 결과
    /// </summary>
    public readonly struct ActionResult
    {
        public readonly bool Success;
        public readonly string ErrorMessage;
        public readonly object Data;

        public ActionResult(bool success, string errorMessage = "", object data = null)
        {
            Success = success;
            ErrorMessage = errorMessage ?? "";
            Data = data;
        }

        public static ActionResult Succeeded(object data = null) => new ActionResult(true, "", data);
        public static ActionResult Failed(string error) => new ActionResult(false, error);
    }
}