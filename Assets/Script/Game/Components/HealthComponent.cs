using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using Game.Repositories;
using System.ComponentModel;
using Unity.Collections;

namespace Game.Components
{
    /// <summary>
    /// 개선된 체력 컴포넌트 - 완전한 캡슐화와 의존성 주입 적용
    /// </summary>
    public class HealthComponent : MonoBehaviour, IAdvancedHealthComponent
    {
        // ✅ 데미지 표시를 위한 Repository (ServiceLocator를 통해 주입)
        private IDamageDisplayRepository damageDisplayRepository;
        [Header("기본 설정")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int startingHealth = -1; // -1이면 maxHealth로 시작
        [SerializeField] private bool canHealAboveMax = true;
        [SerializeField] private bool canRevive = false;

        [Header("방어 설정")]
        [SerializeField] private int baseArmor = 0;
        [SerializeField] private float maxDamageReduction = 0.8f; // 최대 80% 피해 감소

        [Header("재생 설정")]
        [SerializeField] private bool enableRegeneration = false;
        [SerializeField] private int regenerationAmount = 1;
        [SerializeField] private float regenerationInterval = 1f;

        // ✅ 인터페이스 이벤트 구현 (Action으로 통일)
        public event Action<int> OnHealthChanged;
        public event Action<int, int> OnDamageTaken;
        public event Action<int, int> OnHealed;
        public event Action OnDeath;
        public event Action OnRevived;
        public event Action OnFullHealthRestored;
        public event Action<int> OnArmorChanged;
        public event Action<int> OnTemporaryHealthAdded;
        public event Action<int> OnTemporaryHealthRemoved;

        // ✅ 현재 상태
        private int currentHealth;
        private int currentArmor;
        private int temporaryHealth;
        private bool isAlive = true;
        private bool isInvulnerable = false;
        private float invulnerabilityEndTime = 0f;
        private float lastRegenerationTime;

        // ✅ 수정자 관리
        private readonly List<StatModifier> armorModifiers = new List<StatModifier>();
        private readonly List<StatModifier> healthModifiers = new List<StatModifier>();
        
        // ✅ IHealthComponent 기본 속성 구현
        public int CurrentHealth => currentHealth;
        public int MaxHealth => GetModifiedMaxHealth();
        public float HealthPercentage => MaxHealth > 0 ? (float)currentHealth / MaxHealth : 0f;
        public bool IsAlive => isAlive && currentHealth > 0;
        public bool IsDead => !IsAlive;
        public bool IsFullHealth => currentHealth >= MaxHealth;

        // ✅ IAdvancedHealthComponent 추가 속성 구현
        public int Armor => GetModifiedArmor();
        public float DamageReduction => CalculateDamageReduction(Armor);
        public bool HasRegeneration => enableRegeneration && IsAlive;
        public int RegenerationAmount => regenerationAmount;
        public float RegenerationInterval => regenerationInterval;
        public int TemporaryHealth => temporaryHealth;
        public int TotalEffectiveHealth => currentHealth + temporaryHealth;
        public bool IsInvulnerable => isInvulnerable && (invulnerabilityEndTime < 0f || Time.time < invulnerabilityEndTime);

        public event Action<bool> OnInvulnerabilityChanged;

        private void Awake()
        {
            // 초기 체력 설정
            currentHealth = startingHealth > 0 ? startingHealth : maxHealth;
            currentArmor = baseArmor;
            
        }

        private void Start()
        {
            lastRegenerationTime = Time.time;

            // ServiceLocator에서 DamageDisplayRepository 가져오기
            damageDisplayRepository = ServiceLocator.Get<IDamageDisplayRepository>();

            if (damageDisplayRepository == null)
            {
                Debug.LogWarning($"[HealthComponent] DamageDisplayRepository not found in ServiceLocator for {gameObject.name}");
            }
        }

        private void Update()
        {
            // 무적 상태 시간 체크
            if (isInvulnerable && invulnerabilityEndTime > 0f && Time.time >= invulnerabilityEndTime)
            {
                SetInvulnerable(false);
            }

            // 재생 처리
            if (HasRegeneration && Time.time - lastRegenerationTime >= regenerationInterval)
            {
                ProcessRegeneration();
                lastRegenerationTime = Time.time;
            }
        }


        // ✅ 기본 체력 조작 메서드들
        public void TakeDamage(int damage)
        {
            if (damage <= 0)
            {
                Debug.LogWarning($"[HealthComponent] {gameObject.name} received invalid damage: {damage}");
                return;
            }

            if (!IsAlive)
            {
                Debug.Log($"[HealthComponent] {gameObject.name} is already dead, ignoring damage: {damage}");
                return;
            }

            if (IsInvulnerable)
            {
                Debug.Log($"[HealthComponent] {gameObject.name} is invulnerable, blocking damage: {damage}");
                return;
            }

            var damageInfo = new DamageInfo(damage, DamageType.Physical, null);
            ProcessDamage(damageInfo);
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[HealthComponent] {gameObject.name} received invalid heal amount: {amount}");
                return;
            }

            if (!IsAlive && !canRevive)
            {
                Debug.Log($"[HealthComponent] {gameObject.name} is dead and cannot be revived, ignoring heal: {amount}");
                return;
            }

            int previousHealth = currentHealth;
            int maxHealTo = canHealAboveMax ? int.MaxValue : MaxHealth;
            int actualHeal = Mathf.Min(amount, maxHealTo - currentHealth);

            if (actualHeal <= 0)
            {
                Debug.Log($"[HealthComponent] {gameObject.name} is already at maximum health, ignoring heal: {amount}");
                return;
            }

            currentHealth += actualHeal;

            // 부활 처리
            if (!isAlive && currentHealth > 0)
            {
                isAlive = true;
                OnRevived?.Invoke();
            }

            OnHealed?.Invoke(actualHeal, currentHealth);
            OnHealthChanged?.Invoke(currentHealth);

            if (IsFullHealth)
            {
                OnFullHealthRestored?.Invoke();
            }
        }

        public void SetHealth(int newHealth)
        {
            if (newHealth < 0) newHealth = 0;
            
            int previousHealth = currentHealth;
            currentHealth = newHealth;

            // 생존 상태 업데이트
            bool wasAlive = isAlive;
            isAlive = currentHealth > 0;

            if (wasAlive && !isAlive)
            {
                ProcessDeath();
            }
            else if (!wasAlive && isAlive)
            {
                OnRevived?.Invoke();
            }

            OnHealthChanged?.Invoke(currentHealth);
        }

        public void SetMaxHealth(int newMaxHealth)
        {
            if (newMaxHealth <= 0) return;

            maxHealth = newMaxHealth;
            currentHealth = Mathf.Min(currentHealth, MaxHealth);
            
            OnHealthChanged?.Invoke(currentHealth);
        }

        public void RestoreToFullHealth()
        {
            if (IsFullHealth) return;

            int previousHealth = currentHealth;
            currentHealth = MaxHealth;

            if (!isAlive)
            {
                isAlive = true;
                OnRevived?.Invoke();
            }

            OnHealed?.Invoke(currentHealth - previousHealth, currentHealth);
            OnHealthChanged?.Invoke(currentHealth);
            OnFullHealthRestored?.Invoke();
        }

        // ✅ 상태 확인 메서드들
        public bool CanTakeDamage(int damage)
        {
            return damage > 0 && IsAlive && !IsInvulnerable;
        }

        public bool CanHeal(int amount)
        {
            return amount > 0 && (IsAlive || canRevive) && 
                   (currentHealth < MaxHealth || canHealAboveMax);
        }

        public bool WouldDieFromDamage(int damage)
        {
            if (!CanTakeDamage(damage)) return false;
            
            var finalDamage = CalculateDamageAfterArmor(damage);
            return (TotalEffectiveHealth - finalDamage) <= 0;
        }

        // ✅ 고급 기능 메서드들
        public void AddTemporaryHealth(int amount)
        {
            if (amount <= 0) return;

            temporaryHealth += amount;
            OnTemporaryHealthAdded?.Invoke(amount);
        }

        public void RemoveTemporaryHealth(int amount)
        {
            if (amount <= 0) return;

            int actualRemoval = Mathf.Min(amount, temporaryHealth);
            temporaryHealth -= actualRemoval;
            
            OnTemporaryHealthRemoved?.Invoke(actualRemoval);
        }

        public void SetInvulnerable(bool invulnerable, float duration = -1f)
        {
            bool wasInvulnerable = IsInvulnerable;
            isInvulnerable = invulnerable;
            invulnerabilityEndTime = duration > 0f ? Time.time + duration : -1f;

            if (wasInvulnerable != IsInvulnerable)
            {
                OnInvulnerabilityChanged?.Invoke(IsInvulnerable);
            }
        }

        // ✅ 방어력 관련 메서드들
        public void SetArmor(int newArmor)
        {
            currentArmor = Mathf.Max(0, newArmor);
            OnArmorChanged?.Invoke(Armor);
        }

        public void ModifyArmor(int armorChange)
        {
            SetArmor(currentArmor + armorChange);
        }

        public int CalculateDamageAfterArmor(int rawDamage)
        {
            if (rawDamage <= 0) return 0;
            
            float reduction = DamageReduction;
            int reducedDamage = Mathf.RoundToInt(rawDamage * (1f - reduction));
            return Mathf.Max(1, reducedDamage); // 최소 1 피해는 들어감
        }

        // ✅ 재생 관련 메서드들
        public void SetRegeneration(int amount, float interval)
        {
            regenerationAmount = Mathf.Max(0, amount);
            regenerationInterval = Mathf.Max(0.1f, interval);
            enableRegeneration = amount > 0;
        }

        public void DisableRegeneration()
        {
            enableRegeneration = false;
        }

        // ✅ 내부 처리 메서드들
        private void ProcessDamage(DamageInfo damageInfo)
        {
            int finalDamage = damageInfo.IgnoreArmor ? 
                damageInfo.RawDamage : 
                CalculateDamageAfterArmor(damageInfo.RawDamage);

            // 임시 체력부터 소모
            if (temporaryHealth > 0)
            {
                int tempDamage = Mathf.Min(finalDamage, temporaryHealth);
                RemoveTemporaryHealth(tempDamage);
                finalDamage -= tempDamage;
            }

            // 실제 체력 소모
            if (finalDamage > 0)
            {
                currentHealth = Mathf.Max(0, currentHealth - finalDamage);
                Debug.Log($"받은 데미지{finalDamage} | 남은 체력: {currentHealth}");

                // ✅ 데미지 표시 요청 (Repository를 통해 데이터 검증 및 EventChannel 발송)
                damageDisplayRepository?.SendDamageDisplay(
                    finalDamage,
                    damageInfo.IsCritical,
                    transform.position
                );

                OnDamageTaken?.Invoke(finalDamage, currentHealth);
                OnHealthChanged?.Invoke(currentHealth);

                if (currentHealth <= 0 && isAlive)
                {
                    ProcessDeath();
                }
            }
        }

        private void ProcessDeath()
        {
            isAlive = false;
            enableRegeneration = false; // 사망 시 재생 중단

            Debug.Log($"[HealthComponent] {gameObject.name} has died! Health: {currentHealth}/{MaxHealth}");

            // 이벤트 기반 사망 처리 - 구독자(Unit, Base 등)가 각자 처리하도록 위임
            // Unit과 Base 모두 OnDeath 이벤트를 구독하여 각자의 방식으로 파괴 처리
            OnDeath?.Invoke();
        }

        private void ProcessRegeneration()
        {
            if (currentHealth < MaxHealth)
            {
                Heal(regenerationAmount);
            }
        }

        private int GetModifiedMaxHealth()
        {
            int modifiedHealth = maxHealth;
            
            foreach (var modifier in healthModifiers)
            {
                if (modifier.IsActive && !modifier.IsExpired)
                {
                    modifiedHealth = Mathf.RoundToInt(modifier.ApplyModifier(modifiedHealth));
                }
            }
            
            return Mathf.Max(1, modifiedHealth);
        }

        private int GetModifiedArmor()
        {
            int modifiedArmor = currentArmor;
            
            foreach (var modifier in armorModifiers)
            {
                if (modifier.IsActive && !modifier.IsExpired)
                {
                    modifiedArmor = Mathf.RoundToInt(modifier.ApplyModifier(modifiedArmor));
                }
            }
            
            return Mathf.Max(0, modifiedArmor);
        }

        private float CalculateDamageReduction(int armor)
        {
            // 방어력에 따른 피해 감소 공식 (예시)
            float reduction = armor / (armor + 100f);
            return Mathf.Min(reduction, maxDamageReduction);
        }

        // ✅ 디버깅용 메서드
        public override string ToString()
        {
            return $"HealthComponent[{currentHealth}/{MaxHealth}({temporaryHealth}), Armor:{Armor}, Alive:{IsAlive}]";
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            baseArmor = Mathf.Max(0, baseArmor);
            regenerationAmount = Mathf.Max(0, regenerationAmount);
            regenerationInterval = Mathf.Max(0.1f, regenerationInterval);
            maxDamageReduction = Mathf.Clamp01(maxDamageReduction);
        }
    }
}
