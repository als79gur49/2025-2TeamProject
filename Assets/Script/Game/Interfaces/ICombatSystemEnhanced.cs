using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.Interfaces
{
    /// <summary>
    /// Result 패턴을 사용하는 향상된 전투 시스템 인터페이스
    /// </summary>
    public interface ICombatSystemEnhanced : ICombatSystem
    {
        // ✅ Result 패턴을 사용하는 공격 메서드들
        Result<CombatResult> AttackResult(GameObject target);
        Result<CombatResult> AttackPositionResult(Vector2Int position);
        Result<CombatResult> PerformSpecialAttackResult(GameObject target);
        Result<CombatResult> PerformCounterAttackResult(GameObject attacker);
        
        // ✅ 안전한 공격 메서드들 (유효성 검증 포함)
        Result<CombatResult> AttackSafe(GameObject target, bool validateRange = true, bool validateTeam = true);
        Result<CombatResult> AttackIfValid(GameObject target, params ICombatValidator[] validators);
        
        // ✅ 조건부 공격
        Result<CombatResult> AttackIf(GameObject target, Func<GameObject, bool> condition);
        Result<CombatResult> AttackWhenInRange(GameObject target, bool moveToRange = false);
        
        // ✅ 전투 상태 검증
        Result ValidateCombatOperation(CombatOperationType operationType);
        Result ValidateAttackTarget(GameObject target);
        Result ValidateAttackPosition(Vector2Int position);
        Result ValidateCombatState();
        
        // ✅ 일괄 공격 작업
        Result<CombatSummary> AttackMultipleTargets(params GameObject[] targets);
        Result<CombatSummary> AttackTargetsInRange(Vector2Int center, int range);
        
        // ✅ 공격 시뮬레이션
        Result<CombatSimulationResult> SimulateAttack(GameObject target, bool includeCritical = true);
        Result<List<CombatSimulationResult>> SimulateMultipleAttacks(GameObject target, int attackCount);
        
        // ✅ 전투 설정 변경
        Result SetAttackPowerResult(int newAttackPower);
        Result SetAttackRangeResult(int newRange);
        Result SetAttackSpeedResult(float newSpeed);
        Result SetCriticalStatsResult(float chance, float multiplier);
        
        // ✅ 향상된 이벤트
        event Action<Result<CombatResult>> OnAttackResult;
        event Action<Result> OnCombatOperationFailed;
        event Action<CombatValidationResult> OnCombatValidation;
    }

    /// <summary>
    /// 전투 작업 타입
    /// </summary>
    public enum CombatOperationType
    {
        Attack,
        SpecialAttack,
        CounterAttack,
        EnterCombat,
        ExitCombat,
        ChangeAttackPower,
        ChangeAttackRange,
        ChangeAttackSpeed
    }

    /// <summary>
    /// 전투 검증 인터페이스
    /// </summary>
    public interface ICombatValidator
    {
        Result<bool> Validate(ICombatSystem combatSystem, GameObject target);
        string ValidatorName { get; }
    }

    /// <summary>
    /// 전투 검증 결과
    /// </summary>
    [System.Serializable]
    public readonly struct CombatValidationResult
    {
        public readonly bool IsValid;
        public readonly string[] SuccessMessages;
        public readonly string[] ErrorMessages;
        public readonly string[] WarningMessages;
        public readonly CombatValidationType ValidationType;

        public CombatValidationResult(bool isValid, CombatValidationType validationType,
                                     string[] successMessages = null, string[] errorMessages = null, string[] warningMessages = null)
        {
            IsValid = isValid;
            ValidationType = validationType;
            SuccessMessages = successMessages ?? new string[0];
            ErrorMessages = errorMessages ?? new string[0];
            WarningMessages = warningMessages ?? new string[0];
        }

        public static CombatValidationResult Success(CombatValidationType type, params string[] messages) =>
            new CombatValidationResult(true, type, messages);

        public static CombatValidationResult Failure(CombatValidationType type, params string[] messages) =>
            new CombatValidationResult(false, type, errorMessages: messages);

        public static CombatValidationResult Warning(CombatValidationType type, params string[] messages) =>
            new CombatValidationResult(true, type, warningMessages: messages);
    }

    /// <summary>
    /// 전투 검증 타입
    /// </summary>
    public enum CombatValidationType
    {
        Target,
        Range,
        Team,
        Health,
        Ability,
        Cooldown,
        Resources,
        Environment
    }

    /// <summary>
    /// 전투 요약
    /// </summary>
    [System.Serializable]
    public readonly struct CombatSummary
    {
        public readonly int TotalAttacks;
        public readonly int SuccessfulAttacks;
        public readonly int FailedAttacks;
        public readonly int TotalDamageDealt;
        public readonly int CriticalHits;
        public readonly int Misses;
        public readonly CombatResult[] Results;
        public readonly string[] ErrorMessages;

        public CombatSummary(int totalAttacks, int successfulAttacks, int failedAttacks,
                           int totalDamageDealt, int criticalHits, int misses,
                           CombatResult[] results, string[] errorMessages)
        {
            TotalAttacks = totalAttacks;
            SuccessfulAttacks = successfulAttacks;
            FailedAttacks = failedAttacks;
            TotalDamageDealt = totalDamageDealt;
            CriticalHits = criticalHits;
            Misses = misses;
            Results = results ?? new CombatResult[0];
            ErrorMessages = errorMessages ?? new string[0];
        }

        public float SuccessRate => TotalAttacks > 0 ? (float)SuccessfulAttacks / TotalAttacks : 0f;
        public float HitRate => SuccessfulAttacks > 0 ? (float)(SuccessfulAttacks - Misses) / SuccessfulAttacks : 0f;
        public float CriticalRate => SuccessfulAttacks > 0 ? (float)CriticalHits / SuccessfulAttacks : 0f;
        public float AverageDamage => SuccessfulAttacks > 0 ? (float)TotalDamageDealt / SuccessfulAttacks : 0f;
    }

    /// <summary>
    /// 전투 시뮬레이션 결과
    /// </summary>
    [System.Serializable]
    public readonly struct CombatSimulationResult
    {
        public readonly bool WouldHit;
        public readonly bool WouldCritical;
        public readonly int EstimatedDamage;
        public readonly int MinDamage;
        public readonly int MaxDamage;
        public readonly float HitChance;
        public readonly float CriticalChance;
        public readonly GameObject Target;
        public readonly DamageType DamageType;

        public CombatSimulationResult(bool wouldHit, bool wouldCritical, int estimatedDamage,
                                    int minDamage, int maxDamage, float hitChance, float criticalChance,
                                    GameObject target, DamageType damageType)
        {
            WouldHit = wouldHit;
            WouldCritical = wouldCritical;
            EstimatedDamage = estimatedDamage;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            HitChance = hitChance;
            CriticalChance = criticalChance;
            Target = target;
            DamageType = damageType;
        }

        public bool WouldKill(IHealthComponent targetHealth) => 
            targetHealth != null && EstimatedDamage >= targetHealth.CurrentHealth;

        public int DamageRange => MaxDamage - MinDamage;
    }

    /// <summary>
    /// 기본 전투 검증자들
    /// </summary>
    public static class DefaultCombatValidators
    {
        public static readonly ICombatValidator TargetExists = new TargetExistsValidator();
        public static readonly ICombatValidator TargetAlive = new TargetAliveValidator();
        public static readonly ICombatValidator TargetInRange = new TargetInRangeValidator();
        public static readonly ICombatValidator TargetIsEnemy = new TargetIsEnemyValidator();
        public static readonly ICombatValidator AttackerAlive = new AttackerAliveValidator();
        public static readonly ICombatValidator AttackerCanAttack = new AttackerCanAttackValidator();
        public static readonly ICombatValidator NotOnCooldown = new NotOnCooldownValidator();
    }

    /// <summary>
    /// 대상 존재 검증자
    /// </summary>
    public class TargetExistsValidator : ICombatValidator
    {
        public string ValidatorName => "Target Exists";

        public Result<bool> Validate(ICombatSystem combatSystem, GameObject target)
        {
            if (target == null)
                return Result<bool>.Failure("Target is null", ResultErrorType.Validation);

            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// 대상 생존 검증자
    /// </summary>
    public class TargetAliveValidator : ICombatValidator
    {
        public string ValidatorName => "Target Alive";

        public Result<bool> Validate(ICombatSystem combatSystem, GameObject target)
        {
            var health = target?.GetComponent<IHealthComponent>();
            if (health == null)
                return Result<bool>.Failure("Target has no health component", ResultErrorType.Validation);

            if (!health.IsAlive)
                return Result<bool>.Failure("Target is already dead", ResultErrorType.Business);

            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// 사거리 검증자
    /// </summary>
    public class TargetInRangeValidator : ICombatValidator
    {
        public string ValidatorName => "Target In Range";

        public Result<bool> Validate(ICombatSystem combatSystem, GameObject target)
        {
            if (!combatSystem.CanAttackTarget(target))
                return Result<bool>.Failure("Target is not in attack range", ResultErrorType.Business);

            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// 적대 관계 검증자
    /// </summary>
    public class TargetIsEnemyValidator : ICombatValidator
    {
        public string ValidatorName => "Target Is Enemy";

        public Result<bool> Validate(ICombatSystem combatSystem, GameObject target)
        {
            if (!combatSystem.CanAttackByTeam(target))
                return Result<bool>.Failure("Cannot attack target due to team relationship", ResultErrorType.Business);

            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// 공격자 생존 검증자
    /// </summary>
    public class AttackerAliveValidator : ICombatValidator
    {
        public string ValidatorName => "Attacker Alive";

        public Result<bool> Validate(ICombatSystem combatSystem, GameObject target)
        {
            if (combatSystem is Component component)
            {
                var health = component.GetComponent<IHealthComponent>();
                if (health != null && !health.IsAlive)
                    return Result<bool>.Failure("Attacker is dead", ResultErrorType.Business);
            }

            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// 공격 가능 상태 검증자
    /// </summary>
    public class AttackerCanAttackValidator : ICombatValidator
    {
        public string ValidatorName => "Attacker Can Attack";

        public Result<bool> Validate(ICombatSystem combatSystem, GameObject target)
        {
            if (!combatSystem.CanAttack)
                return Result<bool>.Failure("Attacker cannot attack at this time", ResultErrorType.Business);

            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// 쿨다운 검증자
    /// </summary>
    public class NotOnCooldownValidator : ICombatValidator
    {
        public string ValidatorName => "Not On Cooldown";

        public Result<bool> Validate(ICombatSystem combatSystem, GameObject target)
        {
            if (Time.time < combatSystem.NextAttackTime)
                return Result<bool>.Failure($"Attack on cooldown until {combatSystem.NextAttackTime}", ResultErrorType.Business);

            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// 전투 시스템 확장 메서드 (Result 패턴)
    /// </summary>
    public static class CombatSystemEnhancedExtensions
    {
        /// <summary>
        /// 전투 가능 상태 전체 검증
        /// </summary>
        public static Result<CombatValidationResult> ValidateFullCombatState(this ICombatSystem combatSystem, GameObject target)
        {
            var validators = new[]
            {
                DefaultCombatValidators.AttackerAlive,
                DefaultCombatValidators.AttackerCanAttack,
                DefaultCombatValidators.NotOnCooldown,
                DefaultCombatValidators.TargetExists,
                DefaultCombatValidators.TargetAlive,
                DefaultCombatValidators.TargetInRange,
                DefaultCombatValidators.TargetIsEnemy
            };

            var errors = new List<string>();
            var warnings = new List<string>();
            var successes = new List<string>();

            foreach (var validator in validators)
            {
                var result = validator.Validate(combatSystem, target);
                if (result.IsFailure)
                {
                    errors.Add($"{validator.ValidatorName}: {result.ErrorMessage}");
                }
                else
                {
                    successes.Add($"{validator.ValidatorName}: OK");
                }
            }

            var validationResult = errors.Count == 0
                ? CombatValidationResult.Success(CombatValidationType.Target, successes.ToArray())
                : CombatValidationResult.Failure(CombatValidationType.Target, errors.ToArray());

            return Result<CombatValidationResult>.Success(validationResult);
        }

        /// <summary>
        /// 안전한 공격 범위 내 대상 찾기
        /// </summary>
        public static Result<List<GameObject>> GetValidTargetsInRange(this ICombatSystem combatSystem, Vector2Int center)
        {
            try
            {
                var targetsInRange = combatSystem.GetTargetsInRange(center);
                var validTargets = new List<GameObject>();

                foreach (var target in targetsInRange)
                {
                    var validation = combatSystem.ValidateFullCombatState(target);
                    if (validation.IsSuccess && validation.Value.IsValid)
                    {
                        validTargets.Add(target);
                    }
                }

                return Result<List<GameObject>>.Success(validTargets);
            }
            catch (Exception ex)
            {
                return Result<List<GameObject>>.Failure(ex);
            }
        }

        /// <summary>
        /// 최적 공격 대상 선택
        /// </summary>
        public static Result<GameObject> SelectOptimalTarget(this ICombatSystem combatSystem, Vector2Int attackerPosition,
                                                            Func<GameObject, float> targetPriorityFunc = null)
        {
            var targetsResult = combatSystem.GetValidTargetsInRange(attackerPosition);
            if (targetsResult.IsFailure)
                return Result<GameObject>.Failure(targetsResult.ErrorMessage, targetsResult.ErrorType);

            var validTargets = targetsResult.Value;
            if (validTargets.Count == 0)
                return Result<GameObject>.Failure("No valid targets in range", ResultErrorType.NotFound);

            // 기본 우선순위: 체력이 낮은 적 우선
            if (targetPriorityFunc == null)
            {
                targetPriorityFunc = target =>
                {
                    var health = target.GetComponent<IHealthComponent>();
                    return health != null ? 1f / (health.CurrentHealth + 1) : 0f;
                };
            }

            GameObject bestTarget = null;
            float bestPriority = float.MinValue;

            foreach (var target in validTargets)
            {
                var priority = targetPriorityFunc(target);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestTarget = target;
                }
            }

            return bestTarget != null
                ? Result<GameObject>.Success(bestTarget)
                : Result<GameObject>.Failure("Failed to select optimal target", ResultErrorType.General);
        }

        /// <summary>
        /// 연쇄 공격 실행
        /// </summary>
        public static Result<CombatSummary> PerformChainAttack(this ICombatSystemEnhanced combatSystem, 
                                                              GameObject[] targets, bool stopOnFirstFailure = false)
        {
            var results = new List<CombatResult>();
            var errors = new List<string>();
            int successfulAttacks = 0;
            int totalDamage = 0;
            int criticalHits = 0;
            int misses = 0;

            foreach (var target in targets)
            {
                var attackResult = combatSystem.AttackResult(target);
                
                if (attackResult.IsSuccess)
                {
                    var combat = attackResult.Value;
                    results.Add(combat);
                    successfulAttacks++;
                    
                    if (combat.IsHit)
                    {
                        totalDamage += combat.DamageDealt;
                        if (combat.Critical) criticalHits++;
                    }
                    else
                    {
                        misses++;
                    }
                }
                else
                {
                    errors.Add(attackResult.ErrorMessage);
                    if (stopOnFirstFailure)
                        break;
                }
            }

            var summary = new CombatSummary(
                targets.Length, successfulAttacks, targets.Length - successfulAttacks,
                totalDamage, criticalHits, misses, results.ToArray(), errors.ToArray());

            return Result<CombatSummary>.Success(summary);
        }

        /// <summary>
        /// 조건부 공격 (람다 조건 사용)
        /// </summary>
        public static Result<CombatResult> AttackWhen(this ICombatSystemEnhanced combatSystem, GameObject target,
                                                     params Func<GameObject, bool>[] conditions)
        {
            foreach (var condition in conditions)
            {
                if (!condition(target))
                    return Result<CombatResult>.Failure($"Attack condition not met for target {target.name}", 
                                                       ResultErrorType.Business);
            }

            return combatSystem.AttackResult(target);
        }
    }
}