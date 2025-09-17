using System;
using UnityEngine;
using Game.Core;

namespace Game.Interfaces
{
    /// <summary>
    /// Result 패턴을 사용하는 향상된 체력 컴포넌트 인터페이스
    /// </summary>
    public interface IHealthComponentEnhanced : IHealthComponent
    {
        // ✅ Result 패턴을 사용하는 체력 조작 메서드들
        Result<int> TakeDamageResult(int damage);
        Result<int> TakeDamageResult(DamageInfo damageInfo);
        Result<int> HealResult(int amount);
        Result<int> SetHealthResult(int newHealth);
        Result<int> SetMaxHealthResult(int newMaxHealth);
        Result RestoreToFullHealthResult();
        
        // ✅ 안전한 체력 조작 (유효성 검증 포함)
        Result<int> TakeDamageSafe(int damage, bool allowOverkill = false);
        Result<int> HealSafe(int amount, bool allowOverheal = false);
        Result<int> SetHealthSafe(int newHealth, bool validateRange = true);
        
        // ✅ 조건부 체력 조작
        Result<int> TakeDamageIf(int damage, Func<int, bool> condition);
        Result<int> HealIf(int amount, Func<int, bool> condition);
        
        // ✅ 체력 상태 검증
        Result ValidateHealthOperation(int amount, HealthOperationType operationType);
        Result ValidateTargetHealth(int targetHealth);
        Result ValidateAliveState();
        
        // ✅ 일괄 체력 조작
        Result<HealthModificationSummary> ApplyMultipleModifications(params HealthModification[] modifications);
        
        // ✅ 체력 복원 지점 관리
        Result<HealthSnapshot> CreateHealthSnapshot();
        Result RestoreFromSnapshot(HealthSnapshot snapshot);
        
        // ✅ 향상된 이벤트
        event Action<Result<int>> OnHealthChangeResult;
        event Action<Result<int>> OnDamageResult;
        event Action<Result<int>> OnHealResult;
        event Action<Result> OnOperationFailed;
    }

    /// <summary>
    /// 체력 조작 타입
    /// </summary>
    public enum HealthOperationType
    {
        Damage,
        Heal,
        SetHealth,
        SetMaxHealth,
        Restore
    }

    /// <summary>
    /// 체력 수정 요청
    /// </summary>
    [System.Serializable]
    public readonly struct HealthModification
    {
        public readonly HealthOperationType Type;
        public readonly int Amount;
        public readonly string Description;
        public readonly DamageInfo? DamageInfo;

        public HealthModification(HealthOperationType type, int amount, string description = "", DamageInfo? damageInfo = null)
        {
            Type = type;
            Amount = amount;
            Description = description ?? "";
            DamageInfo = damageInfo;
        }

        public static HealthModification Damage(int amount, string description = "", DamageInfo? damageInfo = null) =>
            new HealthModification(HealthOperationType.Damage, amount, description, damageInfo);

        public static HealthModification Heal(int amount, string description = "") =>
            new HealthModification(HealthOperationType.Heal, amount, description);

        public static HealthModification SetHealth(int amount, string description = "") =>
            new HealthModification(HealthOperationType.SetHealth, amount, description);

        public static HealthModification SetMaxHealth(int amount, string description = "") =>
            new HealthModification(HealthOperationType.SetMaxHealth, amount, description);

        public static HealthModification Restore(string description = "Full health restore") =>
            new HealthModification(HealthOperationType.Restore, 0, description);
    }

    /// <summary>
    /// 체력 수정 결과 요약
    /// </summary>
    [System.Serializable]
    public readonly struct HealthModificationSummary
    {
        public readonly int SuccessfulOperations;
        public readonly int FailedOperations;
        public readonly int TotalOperations;
        public readonly int InitialHealth;
        public readonly int FinalHealth;
        public readonly int InitialMaxHealth;
        public readonly int FinalMaxHealth;
        public readonly HealthModificationResult[] Results;
        public readonly string[] ErrorMessages;

        public HealthModificationSummary(int successfulOps, int failedOps, int totalOps,
                                        int initialHealth, int finalHealth, int initialMaxHealth, int finalMaxHealth,
                                        HealthModificationResult[] results, string[] errorMessages)
        {
            SuccessfulOperations = successfulOps;
            FailedOperations = failedOps;
            TotalOperations = totalOps;
            InitialHealth = initialHealth;
            FinalHealth = finalHealth;
            InitialMaxHealth = initialMaxHealth;
            FinalMaxHealth = finalMaxHealth;
            Results = results ?? new HealthModificationResult[0];
            ErrorMessages = errorMessages ?? new string[0];
        }

        public bool IsCompleteSuccess => FailedOperations == 0;
        public bool IsPartialSuccess => SuccessfulOperations > 0 && FailedOperations > 0;
        public bool IsCompleteFailure => SuccessfulOperations == 0;

        public float SuccessRate => TotalOperations > 0 ? (float)SuccessfulOperations / TotalOperations : 0f;
    }

    /// <summary>
    /// 체력 스냅샷 (복원 지점)
    /// </summary>
    [System.Serializable]
    public readonly struct HealthSnapshot
    {
        public readonly int CurrentHealth;
        public readonly int MaxHealth;
        public readonly int TemporaryHealth;
        public readonly int Armor;
        public readonly bool IsInvulnerable;
        public readonly DateTime CreatedAt;
        public readonly string Description;

        public HealthSnapshot(int currentHealth, int maxHealth, int temporaryHealth, int armor,
                             bool isInvulnerable, string description = "")
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            TemporaryHealth = temporaryHealth;
            Armor = armor;
            IsInvulnerable = isInvulnerable;
            CreatedAt = DateTime.Now;
            Description = description ?? "";
        }

        public bool IsValid => MaxHealth > 0 && CurrentHealth >= 0 && CurrentHealth <= (MaxHealth + TemporaryHealth);
    }

    /// <summary>
    /// 체력 컴포넌트 확장 메서드 (Result 패턴)
    /// </summary>
    public static class HealthComponentEnhancedExtensions
    {
        /// <summary>
        /// 체력 비율로 피해 입히기
        /// </summary>
        public static Result<int> TakeDamageByPercentage(this IHealthComponentEnhanced health, float percentage)
        {
            if (percentage < 0 || percentage > 1)
                return Result<int>.Failure("Percentage must be between 0 and 1", ResultErrorType.Validation);

            var damage = Mathf.RoundToInt(health.MaxHealth * percentage);
            return health.TakeDamageResult(damage);
        }

        /// <summary>
        /// 체력 비율로 회복하기
        /// </summary>
        public static Result<int> HealByPercentage(this IHealthComponentEnhanced health, float percentage)
        {
            if (percentage < 0 || percentage > 1)
                return Result<int>.Failure("Percentage must be between 0 and 1", ResultErrorType.Validation);

            var healAmount = Mathf.RoundToInt(health.MaxHealth * percentage);
            return health.HealResult(healAmount);
        }

        /// <summary>
        /// 목표 체력으로 조정 (현재보다 높으면 힐, 낮으면 데미지)
        /// </summary>
        public static Result<int> AdjustToTargetHealth(this IHealthComponentEnhanced health, int targetHealth)
        {
            if (targetHealth < 0 || targetHealth > health.MaxHealth)
                return Result<int>.Failure($"Target health {targetHealth} is out of valid range [0, {health.MaxHealth}]", 
                                          ResultErrorType.Validation);

            var difference = targetHealth - health.CurrentHealth;
            
            if (difference > 0)
                return health.HealResult(difference);
            else if (difference < 0)
                return health.TakeDamageResult(-difference);
            else
                return Result<int>.Success(health.CurrentHealth); // 이미 목표 체력
        }

        /// <summary>
        /// 조건부 실행
        /// </summary>
        public static Result<int> ExecuteIf(this IHealthComponentEnhanced health, 
                                           Func<IHealthComponent, bool> condition,
                                           Func<IHealthComponentEnhanced, Result<int>> operation)
        {
            if (!condition(health))
                return Result<int>.Failure("Condition not met for health operation", ResultErrorType.Business);

            return operation(health);
        }

        /// <summary>
        /// 안전한 체력 감소 (죽지 않게)
        /// </summary>
        public static Result<int> TakeDamageNonLethal(this IHealthComponentEnhanced health, int damage)
        {
            if (damage <= 0)
                return Result<int>.Failure("Damage must be positive", ResultErrorType.Validation);

            var maxDamage = health.CurrentHealth - 1; // 최소 1 체력 유지
            if (maxDamage <= 0)
                return Result<int>.Failure("Unit already at minimum health", ResultErrorType.Business);

            var actualDamage = Mathf.Min(damage, maxDamage);
            return health.TakeDamageResult(actualDamage);
        }

        /// <summary>
        /// 체력 상태 확인
        /// </summary>
        public static Result ValidateHealthState(this IHealthComponent health, HealthState requiredState)
        {
            var currentState = health.GetHealthState();
            
            if (currentState != requiredState)
                return Result.Failure($"Required health state: {requiredState}, current: {currentState}", 
                                    ResultErrorType.Business);

            return Result.Success();
        }

        /// <summary>
        /// 최소 체력 요구사항 확인
        /// </summary>
        public static Result ValidateMinimumHealth(this IHealthComponent health, int minimumHealth)
        {
            if (health.CurrentHealth < minimumHealth)
                return Result.Failure($"Insufficient health: {health.CurrentHealth} < {minimumHealth}", 
                                    ResultErrorType.Business);

            return Result.Success();
        }

        /// <summary>
        /// 체력 비율 요구사항 확인
        /// </summary>
        public static Result ValidateMinimumHealthPercentage(this IHealthComponent health, float minimumPercentage)
        {
            if (minimumPercentage < 0 || minimumPercentage > 1)
                return Result.Failure("Percentage must be between 0 and 1", ResultErrorType.Validation);

            if (health.HealthPercentage < minimumPercentage)
                return Result.Failure($"Insufficient health percentage: {health.HealthPercentage:P0} < {minimumPercentage:P0}", 
                                    ResultErrorType.Business);

            return Result.Success();
        }

        /// <summary>
        /// 연쇄적 체력 조작
        /// </summary>
        public static Result<int> ChainOperations(this IHealthComponentEnhanced health, 
                                                 params Func<IHealthComponentEnhanced, Result<int>>[] operations)
        {
            var currentHealth = health.CurrentHealth;
            
            foreach (var operation in operations)
            {
                var result = operation(health);
                if (result.IsFailure)
                    return result;
                
                currentHealth = result.Value;
            }

            return Result<int>.Success(currentHealth);
        }
    }
}