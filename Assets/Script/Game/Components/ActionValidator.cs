using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Components
{
    /// <summary>
    /// 행동 유효성 검증 담당 클래스 - 단일 책임 원칙 적용 (검증만 담당)
    /// </summary>
    public class ActionValidator : MonoBehaviour
    {
        [Header("검증 설정")]
        [SerializeField] private bool enableRangeValidation = true;
        [SerializeField] private bool enableResourceValidation = true;
        [SerializeField] private bool enableCooldownValidation = true;
        [SerializeField] private bool enableTargetValidation = true;

        // ✅ 의존성 주입 필드
        [Inject] private IGridManager gridManager;
        [Inject] private IHealthComponent healthComponent;

        // ✅ 쿨다운 관리
        private readonly Dictionary<Type, float> actionCooldowns = new Dictionary<Type, float>();
        private readonly Dictionary<Type, float> lastActionTimes = new Dictionary<Type, float>();

        // ✅ 비활성화된 행동 타입들
        private readonly HashSet<Type> disabledActionTypes = new HashSet<Type>();

        // ✅ 유효성 검증 규칙 델리게이트
        private readonly Dictionary<Type, Func<IGameAction, ValidationResult>> customValidators = 
            new Dictionary<Type, Func<IGameAction, ValidationResult>>();

        private void Awake()
        {
            // 의존성 주입
            this.InjectDependencies();
        }

        private void Start()
        {
            // 기본 검증 규칙 등록
            RegisterDefaultValidationRules();
        }

        /// <summary>
        /// 행동 유효성 검증 - 메인 진입점
        /// </summary>
        public bool ValidateAction(IGameAction action)
        {
            if (action == null)
            {
                LogValidationError("Action is null");
                return false;
            }

            var result = ValidateActionDetailed(action);
            
            if (!result.IsValid)
            {
                LogValidationError($"{action.GetType().Name}: {result.ErrorMessage}");
            }

            return result.IsValid;
        }

        /// <summary>
        /// 상세한 유효성 검증 결과 반환
        /// </summary>
        public ValidationResult ValidateActionDetailed(IGameAction action)
        {
            if (action == null)
                return ValidationResult.Invalid("Action is null");

            // 1. 기본 행동 유효성 검증
            if (!action.CanExecute())
                return ValidationResult.Invalid("Action reports it cannot execute");

            // 2. 행동 타입 비활성화 확인
            if (IsActionTypeDisabled(action.GetType()))
                return ValidationResult.Invalid($"Action type {action.GetType().Name} is disabled");

            // 3. 유닛 생존 상태 확인
            if (healthComponent != null && healthComponent.IsDead)
                return ValidationResult.Invalid("Unit is dead");

            // 4. 쿨다운 검증
            if (enableCooldownValidation)
            {
                var cooldownResult = ValidateCooldown(action);
                if (!cooldownResult.IsValid)
                    return cooldownResult;
            }

            // 5. 커스텀 검증 규칙 적용
            if (customValidators.TryGetValue(action.GetType(), out var validator))
            {
                var customResult = validator(action);
                if (!customResult.IsValid)
                    return customResult;
            }

            // 6. 범위 검증 (위치 기반 행동의 경우)
            if (enableRangeValidation && action is IPositionBasedAction positionAction)
            {
                var rangeResult = ValidateRange(positionAction);
                if (!rangeResult.IsValid)
                    return rangeResult;
            }

            // 7. 대상 검증 (대상 기반 행동의 경우)
            if (enableTargetValidation && action is ITargetBasedAction targetAction)
            {
                var targetResult = ValidateTarget(targetAction);
                if (!targetResult.IsValid)
                    return targetResult;
            }

            return ValidationResult.Valid();
        }

        /// <summary>
        /// 쿨다운 검증
        /// </summary>
        private ValidationResult ValidateCooldown(IGameAction action)
        {
            var actionType = action.GetType();
            
            if (!actionCooldowns.ContainsKey(actionType))
                return ValidationResult.Valid(); // 쿨다운이 없는 행동

            var cooldownDuration = actionCooldowns[actionType];
            var lastActionTime = lastActionTimes.GetValueOrDefault(actionType, 0f);
            var timeSinceLastAction = Time.time - lastActionTime;

            if (timeSinceLastAction < cooldownDuration)
            {
                var remainingCooldown = cooldownDuration - timeSinceLastAction;
                return ValidationResult.Invalid($"Action on cooldown for {remainingCooldown:F1} seconds");
            }

            return ValidationResult.Valid();
        }

        /// <summary>
        /// 범위 검증 (위치 기반 행동)
        /// </summary>
        private ValidationResult ValidateRange(IPositionBasedAction action)
        {
            if (gridManager == null)
                return ValidationResult.Invalid("GridManager not available");

            var currentPosition = gridManager.GetUnitPosition(gameObject);
            var targetPosition = action.TargetPosition;
            var requiredRange = action.Range;

            var distance = Vector2Int.Distance(currentPosition, targetPosition);
            
            if (distance > requiredRange)
            {
                return ValidationResult.Invalid($"Target out of range: {distance} > {requiredRange}");
            }

            // 경로 확인 (장애물 등)
            if (action.RequiresLineOfSight && !gridManager.IsPathClear(currentPosition, targetPosition, gameObject))
            {
                return ValidationResult.Invalid("Line of sight blocked");
            }

            return ValidationResult.Valid();
        }

        /// <summary>
        /// 대상 검증 (대상 기반 행동)
        /// </summary>
        private ValidationResult ValidateTarget(ITargetBasedAction action)
        {
            var target = action.Target;
            
            if (target == null)
                return ValidationResult.Invalid("Target is null");

            // 대상이 유효한 GameObject인지 확인
            if (target == null) // Unity null check
                return ValidationResult.Invalid("Target has been destroyed");

            // 대상 생존 확인 (필요한 경우)
            if (action.RequiresLivingTarget)
            {
                var targetHealth = target.GetComponent<IHealthComponent>();
                if (targetHealth == null || targetHealth.IsDead)
                    return ValidationResult.Invalid("Target is dead");
            }

            // 팀 검증 (필요한 경우)
            if (action.TargetTeamRestriction != TeamRestriction.Any)
            {
                var result = ValidateTeamRestriction(action.Target, action.TargetTeamRestriction);
                if (!result.IsValid)
                    return result;
            }

            return ValidationResult.Valid();
        }

        /// <summary>
        /// 팀 제약 검증 - ITeamComponent 인터페이스 사용
        /// </summary>
        private ValidationResult ValidateTeamRestriction(GameObject target, TeamRestriction restriction)
        {
            var myTeam = GetComponent<ITeamComponent>();
            var targetTeam = target.GetComponent<ITeamComponent>();

            if (myTeam == null || targetTeam == null)
                return ValidationResult.Valid(); // 팀 정보가 없으면 허용

            return restriction switch
            {
                TeamRestriction.AlliesOnly => myTeam.IsAlly(targetTeam) ? 
                    ValidationResult.Valid() : ValidationResult.Invalid("Target must be ally"),
                TeamRestriction.EnemiesOnly => myTeam.IsEnemy(targetTeam) ? 
                    ValidationResult.Valid() : ValidationResult.Invalid("Target must be enemy"),
                TeamRestriction.Self => target == gameObject ? 
                    ValidationResult.Valid() : ValidationResult.Invalid("Target must be self"),
                TeamRestriction.Any => ValidationResult.Valid(),
                _ => ValidationResult.Valid()
            };
        }

        /// <summary>
        /// 기본 검증 규칙 등록
        /// </summary>
        private void RegisterDefaultValidationRules()
        {
            // 이동 행동 검증
            RegisterCustomValidator<MoveAction>(action => 
            {
                if (gridManager == null) return ValidationResult.Invalid("GridManager not available");
                
                var targetPos = action.TargetPosition;
                if (!gridManager.IsValidPosition(targetPos))
                    return ValidationResult.Invalid("Invalid target position");
                
                if (gridManager.IsPositionOccupied(targetPos))
                    return ValidationResult.Invalid("Target position is occupied");
                
                return ValidationResult.Valid();
            });

            // 공격 행동 검증
            RegisterCustomValidator<AttackAction>(action => 
            {
                if (action.Target == null)
                    return ValidationResult.Invalid("No attack target");
                
                var targetHealth = action.Target.GetComponent<IHealthComponent>();
                if (targetHealth == null || targetHealth.IsDead)
                    return ValidationResult.Invalid("Target cannot be damaged");
                
                return ValidationResult.Valid();
            });
        }

        /// <summary>
        /// 커스텀 검증자 등록
        /// </summary>
        public void RegisterCustomValidator<T>(Func<T, ValidationResult> validator) where T : IGameAction
        {
            customValidators[typeof(T)] = action => validator((T)action);
        }

        /// <summary>
        /// 행동 타입 비활성화
        /// </summary>
        public void DisableActionType<T>() where T : IGameAction
        {
            disabledActionTypes.Add(typeof(T));
        }

        /// <summary>
        /// 행동 타입 활성화
        /// </summary>
        public void EnableActionType<T>() where T : IGameAction
        {
            disabledActionTypes.Remove(typeof(T));
        }

        /// <summary>
        /// 행동 타입 비활성화 상태 확인
        /// </summary>
        public bool IsActionTypeDisabled(Type actionType)
        {
            return disabledActionTypes.Contains(actionType);
        }

        /// <summary>
        /// 쿨다운 설정
        /// </summary>
        public void SetActionCooldown<T>(float cooldownSeconds) where T : IGameAction
        {
            actionCooldowns[typeof(T)] = cooldownSeconds;
        }

        /// <summary>
        /// 행동 실행 후 쿨다운 시작 (ActionHandler에서 호출)
        /// </summary>
        public void StartCooldown(IGameAction action)
        {
            var actionType = action.GetType();
            if (actionCooldowns.ContainsKey(actionType))
            {
                lastActionTimes[actionType] = Time.time;
            }
        }

        /// <summary>
        /// 쿨다운 초기화
        /// </summary>
        public void ResetCooldowns()
        {
            lastActionTimes.Clear();
        }

        /// <summary>
        /// 남은 쿨다운 시간 조회
        /// </summary>
        public float GetRemainingCooldown<T>() where T : IGameAction
        {
            var actionType = typeof(T);
            
            if (!actionCooldowns.ContainsKey(actionType))
                return 0f;

            var cooldownDuration = actionCooldowns[actionType];
            var lastActionTime = lastActionTimes.GetValueOrDefault(actionType, 0f);
            var timeSinceLastAction = Time.time - lastActionTime;

            return Mathf.Max(0f, cooldownDuration - timeSinceLastAction);
        }

        private void LogValidationError(string message)
        {
            Debug.LogWarning($"[ActionValidator] {gameObject.name}: {message}");
        }

        // ✅ 디버깅용 메서드
        public override string ToString()
        {
            return $"ActionValidator[Disabled:{disabledActionTypes.Count}, Cooldowns:{actionCooldowns.Count}]";
        }
    }

    /// <summary>
    /// 유효성 검증 결과
    /// </summary>
    public readonly struct ValidationResult
    {
        public readonly bool IsValid;
        public readonly string ErrorMessage;

        public ValidationResult(bool isValid, string errorMessage = "")
        {
            IsValid = isValid;
            ErrorMessage = errorMessage ?? "";
        }

        public static ValidationResult Valid() => new ValidationResult(true);
        public static ValidationResult Invalid(string error) => new ValidationResult(false, error);
    }

    /// <summary>
    /// 위치 기반 행동 인터페이스
    /// </summary>
    public interface IPositionBasedAction : IGameAction
    {
        Vector2Int TargetPosition { get; }
        int Range { get; }
        bool RequiresLineOfSight { get; }
    }

    /// <summary>
    /// 대상 기반 행동 인터페이스
    /// </summary>
    public interface ITargetBasedAction : IGameAction
    {
        GameObject Target { get; }
        bool RequiresLivingTarget { get; }
        TeamRestriction TargetTeamRestriction { get; }
    }

    /// <summary>
    /// 팀 제약 열거형
    /// </summary>
    public enum TeamRestriction
    {
        Any,
        AlliesOnly,
        EnemiesOnly,
        Self
    }

    /// <summary>
    /// 예시 행동 클래스들 (다른 파일로 분리 가능)
    /// </summary>
    public class MoveAction : IPositionBasedAction
    {
        public string Name => "Move";
        public int ActionCost => 1;
        public Vector2Int TargetPosition { get; set; }
        public int Range => 3;
        public bool RequiresLineOfSight => false;

        public ActionResult Execute()
        {
            // 이동 로직 구현
            return ActionResult.Succeeded();
        }

        public bool CanExecute() => true;
    }

    public class AttackAction : ITargetBasedAction, IPositionBasedAction
    {
        public string Name => "Attack";
        public int ActionCost => 1;
        public GameObject Target { get; set; }
        public bool RequiresLivingTarget => true;
        public TeamRestriction TargetTeamRestriction => TeamRestriction.EnemiesOnly;
        public Vector2Int TargetPosition { get; set; }
        public int Range => 1;
        public bool RequiresLineOfSight => true;

        public ActionResult Execute()
        {
            // 공격 로직 구현
            return ActionResult.Succeeded();
        }

        public bool CanExecute() => Target != null;
    }
}