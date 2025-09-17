using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Interfaces
{
    /// <summary>
    /// 체력 컴포넌트 인터페이스 - 의존성 역전 원칙 적용
    /// </summary>
    public interface IHealthComponent
    {
        // ✅ 체력 정보
        int CurrentHealth { get; }
        int MaxHealth { get; }
        float HealthPercentage { get; }
        bool IsAlive { get; }
        bool IsDead { get; }
        bool IsFullHealth { get; }
        
        // ✅ 체력 조작
        void TakeDamage(int damage);
        void Heal(int amount);
        void SetHealth(int newHealth);
        void SetMaxHealth(int newMaxHealth);
        void RestoreToFullHealth();
        
        // ✅ 상태 확인
        bool CanTakeDamage(int damage);
        bool CanHeal(int amount);
        bool WouldDieFromDamage(int damage);
        
        // ✅ 이벤트
        event Action<int> OnHealthChanged;     // 새로운 체력 값
        event Action<int, int> OnDamageTaken;  // 피해량, 남은 체력
        event Action<int, int> OnHealed;       // 회복량, 새로운 체력
        event Action OnDeath;                  // 사망
        event Action OnRevived;                // 부활
        event Action OnFullHealthRestored;     // 풀피 회복
    }

    /// <summary>
    /// 고급 체력 컴포넌트 인터페이스 - 추가 기능
    /// </summary>
    public interface IAdvancedHealthComponent : IHealthComponent
    {
        // ✅ 방어력 및 저항
        int Armor { get; }
        float DamageReduction { get; }
        
        // ✅ 재생 및 지속 효과
        bool HasRegeneration { get; }
        int RegenerationAmount { get; }
        float RegenerationInterval { get; }
        
        // ✅ 임시 체력
        int TemporaryHealth { get; }
        int TotalEffectiveHealth { get; } // 실제 체력 + 임시 체력
        
        // ✅ 상태 이상
        bool IsInvulnerable { get; }
        bool IsPoisoned { get; }
        bool IsBleeding { get; }
        
        // ✅ 고급 체력 조작
        void AddTemporaryHealth(int amount);
        void RemoveTemporaryHealth(int amount);
        void SetInvulnerable(bool invulnerable, float duration = -1f);
        void ApplyPoison(int damagePerTick, float duration, float interval = 1f);
        void ApplyBleeding(int damagePerTick, float duration, float interval = 1f);
        void ClearAllStatusEffects();
        
        // ✅ 방어력 조작
        void SetArmor(int newArmor);
        void ModifyArmor(int armorChange);
        int CalculateDamageAfterArmor(int rawDamage);
        
        // ✅ 재생 설정
        void SetRegeneration(int amount, float interval);
        void DisableRegeneration();
        
        // ✅ 고급 이벤트
        event Action<int> OnArmorChanged;
        event Action<int> OnTemporaryHealthAdded;
        event Action<int> OnTemporaryHealthRemoved;
        event Action<bool> OnInvulnerabilityChanged;
        event Action OnPoisonApplied;
        event Action OnBleedingApplied;
        event Action OnStatusEffectCleared;
    }

    /// <summary>
    /// 체력 수정 결과
    /// </summary>
    public readonly struct HealthModificationResult
    {
        public readonly bool Success;
        public readonly int ActualAmount;  // 실제로 적용된 양
        public readonly int PreviousHealth;
        public readonly int NewHealth;
        public readonly string Message;

        public HealthModificationResult(bool success, int actualAmount, int previousHealth, int newHealth, string message = "")
        {
            Success = success;
            ActualAmount = actualAmount;
            PreviousHealth = previousHealth;
            NewHealth = newHealth;
            Message = message ?? "";
        }

        public static HealthModificationResult Failed(string message) => 
            new HealthModificationResult(false, 0, 0, 0, message);
        
        public static HealthModificationResult Succeeded(int actualAmount, int previousHealth, int newHealth, string message = "") => 
            new HealthModificationResult(true, actualAmount, previousHealth, newHealth, message);
    }

    /// <summary>
    /// 피해 정보 구조체
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly int RawDamage;
        public readonly int FinalDamage;
        public readonly DamageType Type;
        public readonly GameObject Source;
        public readonly bool IsCritical;
        public readonly bool IgnoreArmor;

        public DamageInfo(int rawDamage, DamageType type, GameObject source = null, bool isCritical = false, bool ignoreArmor = false)
        {
            RawDamage = rawDamage;
            FinalDamage = rawDamage;
            Type = type;
            Source = source;
            IsCritical = isCritical;
            IgnoreArmor = ignoreArmor;
        }

        private DamageInfo(int rawDamage, int finalDamage, DamageType type, GameObject source, bool isCritical, bool ignoreArmor)
        {
            RawDamage = rawDamage;
            FinalDamage = finalDamage;
            Type = type;
            Source = source;
            IsCritical = isCritical;
            IgnoreArmor = ignoreArmor;
        }

        public DamageInfo WithFinalDamage(int finalDamage)
        {
            return new DamageInfo(RawDamage, finalDamage, Type, Source, IsCritical, IgnoreArmor);
        }
    }

    /// <summary>
    /// 피해 타입 열거형
    /// </summary>
    public enum DamageType
    {
        Physical,   // 물리 피해
        Magical,    // 마법 피해
        Fire,       // 화염 피해
        Ice,        // 빙결 피해
        Lightning,  // 번개 피해
        Poison,     // 독 피해
        Bleeding,   // 출혈 피해
        True        // 고정 피해 (방어력 무시)
    }

    /// <summary>
    /// 체력 상태 열거형
    /// </summary>
    public enum HealthState
    {
        Full,       // 100%
        Healthy,    // 75-99%
        Wounded,    // 50-74%
        Injured,    // 25-49%
        Critical,   // 1-24%
        Dead        // 0%
    }

    /// <summary>
    /// 체력 관련 유틸리티 확장 메서드
    /// </summary>
    public static class HealthComponentExtensions
    {
        public static HealthState GetHealthState(this IHealthComponent health)
        {
            if (health.IsDead) return HealthState.Dead;
            
            float percentage = health.HealthPercentage;
            return percentage switch
            {
                >= 1.0f => HealthState.Full,
                >= 0.75f => HealthState.Healthy,
                >= 0.5f => HealthState.Wounded,
                >= 0.25f => HealthState.Injured,
                > 0f => HealthState.Critical,
                _ => HealthState.Dead
            };
        }

        public static Color GetHealthStateColor(this IHealthComponent health)
        {
            return health.GetHealthState() switch
            {
                HealthState.Full => Color.green,
                HealthState.Healthy => Color.yellow,
                HealthState.Wounded => new Color(1f, 0.5f, 0f), // 주황색
                HealthState.Injured => Color.red,
                HealthState.Critical => Color.magenta,
                HealthState.Dead => Color.black,
                _ => Color.white
            };
        }

        public static bool IsInDanger(this IHealthComponent health, float threshold = 0.25f)
        {
            return health.IsAlive && health.HealthPercentage <= threshold;
        }

        public static int GetMissingHealth(this IHealthComponent health)
        {
            return health.MaxHealth - health.CurrentHealth;
        }
    }
}