using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interfaces
{
    /// <summary>
    /// 전투 시스템 인터페이스 - 의존성 역전 원칙 적용
    /// </summary>
    public interface ICombatSystem
    {
        // ✅ 공격력 정보
        int BaseAttackPower { get; }
        int CurrentAttackPower { get; }
        int AttackRange { get; }
        float AttackSpeed { get; }
        
        // ✅ 전투 상태
        bool CanAttack { get; }
        bool IsInCombat { get; }
        float LastAttackTime { get; }
        float NextAttackTime { get; }
        
        // ✅ 공격 메서드
        bool CanAttackTarget(GameObject target);
        bool CanAttackPosition(Vector2Int position);
        CombatResult Attack(GameObject target);
        CombatResult AttackPosition(Vector2Int position);

        /// <summary>
        /// 타일 기반 범위 공격 (멀티타일 엔티티 중복 피해 방지)
        /// </summary>
        /// <param name="targetTiles">공격할 타일 목록</param>
        /// <param name="isSpecialAttack">특수 공격 여부</param>
        /// <param name="forceCritical">강제 크리티컬 여부</param>
        /// <returns>피해를 받은 고유 타겟 수</returns>
        int AttackTiles(List<Tile> targetTiles, bool isSpecialAttack = false, bool forceCritical = false);

        // ✅ 공격 범위 확인
        List<Vector2Int> GetAttackRange(Vector2Int fromPosition);
        List<GameObject> GetTargetsInRange(Vector2Int fromPosition);
        bool IsTargetInRange(GameObject target, Vector2Int fromPosition);
        
        // ✅ 공격력 조작
        void SetBaseAttackPower(int newAttackPower);
        void ModifyAttackPower(int modifier);
        void SetAttackRange(int newRange);
        void SetAttackSpeed(float newSpeed);
        
        // ✅ 전투 상태 관리
        void EnterCombat();
        void ExitCombat();
        void ResetAttackCooldown();
        
        // ✅ 이벤트
        event Action<GameObject, CombatResult> OnAttackPerformed;
        event Action<List<GameObject>> OnAttackStarted;
        event Action<GameObject> OnAttackMissed;
        event Action OnCombatStateChanged;
        event Action<int> OnAttackPowerChanged;
    }

    /// <summary>
    /// 고급 전투 시스템 인터페이스 - 추가 기능
    /// </summary>
    public interface IAdvancedCombatSystem : ICombatSystem
    {
        // ✅ 크리티컬 공격
        float CriticalChance { get; }
        float CriticalMultiplier { get; }
        bool CanCritical { get; }
        
        // ✅ 공격 타입
        DamageType AttackType { get; }
        List<DamageType> AvailableAttackTypes { get; }
        
        // ✅ 특수 공격
        bool HasSpecialAttack { get; }
        int SpecialAttackCooldown { get; }
        bool CanUseSpecialAttack { get; }
        
        // ✅ 반격 시스템
        bool CanCounterAttack { get; }
        float CounterAttackChance { get; }
        int CounterAttackDamage { get; }
        
        // ✅ 관통 공격
        bool CanPierceArmor { get; }
        float ArmorPenetration { get; }
        
        // ✅ 고급 공격 메서드
        CombatResult PerformSpecialAttack(GameObject target);
        CombatResult PerformCounterAttack(GameObject attacker);
        CombatResult PerformCriticalAttack(GameObject target);
        
        // ✅ 설정 메서드
        void SetCriticalChance(float chance);
        void SetCriticalMultiplier(float multiplier);
        void SetAttackType(DamageType type);
        void SetArmorPenetration(float penetration);
        
        // ✅ 고급 이벤트
        event Action<GameObject, CombatResult> OnCriticalAttack;
        event Action<GameObject, CombatResult> OnSpecialAttack;
        event Action<GameObject, CombatResult> OnCounterAttack;
        event Action<DamageType> OnAttackTypeChanged;
    }

    /// <summary>
    /// 전투 결과 구조체
    /// </summary>
    public readonly struct CombatResult
    {
        public readonly bool Success;
        public readonly bool IsHit;
        public readonly bool Critical;
        public readonly bool CounterAttack;
        public readonly int DamageDealt;
        public readonly DamageType DamageType;
        public readonly GameObject Target;
        public readonly string Message;

        public CombatResult(bool success, bool hit, int damageDealt, GameObject target, 
                           bool critical = false, bool counterAttack = false, 
                           DamageType damageType = DamageType.Physical, string message = "")
        {
            Success = success;
            IsHit = hit;
            Critical = critical;
            CounterAttack = counterAttack;
            DamageDealt = damageDealt;
            DamageType = damageType;
            Target = target;
            Message = message ?? "";
        }

        public static CombatResult Failed(string message) => 
            new CombatResult(false, false, 0, null, false, false, DamageType.Physical, message);
        
        public static CombatResult Miss(GameObject target, string message = "") => 
            new CombatResult(true, false, 0, target, false, false, DamageType.Physical, message);
        
        public static CombatResult Hit(int damage, GameObject target, DamageType type = DamageType.Physical, 
                                      bool critical = false, string message = "") => 
            new CombatResult(true, true, damage, target, critical, false, type, message);
        
        public static CombatResult Counter(int damage, GameObject target, DamageType type = DamageType.Physical, 
                                          string message = "") => 
            new CombatResult(true, true, damage, target, false, true, type, message);
    }

    /// <summary>
    /// 공격 범위 타입 열거형
    /// </summary>
    public enum AttackRangeType
    {
        Single,         // 단일 대상
        Line,           // 직선
        Cross,          // 십자형
        Square,         // 사각형 범위
        Circle,         // 원형 범위
        Cone,           // 원뿔형
        All             // 전체 범위
    }

    /// <summary>
    /// 공격 정보 구조체
    /// </summary>
    public readonly struct AttackInfo
    {
        public readonly int BaseDamage;
        public readonly DamageType Type;
        public readonly AttackRangeType RangeType;
        public readonly int Range;
        public readonly float CriticalChance;
        public readonly float CriticalMultiplier;
        public readonly bool IgnoreArmor;
        public readonly GameObject Attacker;

        public AttackInfo(int baseDamage, DamageType type, AttackRangeType rangeType, int range,
                         GameObject attacker, float criticalChance = 0f, float criticalMultiplier = 2f,
                         bool ignoreArmor = false)
        {
            BaseDamage = baseDamage;
            Type = type;
            RangeType = rangeType;
            Range = range;
            CriticalChance = criticalChance;
            CriticalMultiplier = criticalMultiplier;
            IgnoreArmor = ignoreArmor;
            Attacker = attacker;
        }
    }

    /// <summary>
    /// 전투 관련 유틸리티 확장 메서드
    /// </summary>
    public static class CombatSystemExtensions
    {
        /// <summary>
        /// 공격이 크리티컬인지 확인
        /// </summary>
        public static bool RollCritical(this ICombatSystem combat, float bonus = 0f)
        {
            if (combat is IAdvancedCombatSystem advanced)
            {
                float totalChance = advanced.CriticalChance + bonus;
                return UnityEngine.Random.Range(0f, 1f) < totalChance;
            }
            return false;
        }

        /// <summary>
        /// 최종 피해량 계산
        /// </summary>
        public static int CalculateFinalDamage(this ICombatSystem combat, int baseDamage, bool isCritical = false)
        {
            float damage = baseDamage;
            
            if (isCritical && combat is IAdvancedCombatSystem advanced)
            {
                damage *= advanced.CriticalMultiplier;
            }
            
            return Mathf.RoundToInt(damage);
        }

        /// <summary>
        /// 공격 가능한 대상인지 확인
        /// </summary>
        public static bool IsValidTarget(this ICombatSystem combat, GameObject target)
        {
            if (target == null) return false;
            
            // 자기 자신은 공격 불가
            if (combat is Component combatComponent && combatComponent.gameObject == target)
                return false;
            
            // 체력이 있는 대상만 공격 가능
            var healthComponent = target.GetComponent<IHealthComponent>();
            return healthComponent != null && healthComponent.IsAlive;
        }

        /// <summary>
        /// 팀 관계에 따른 공격 가능 여부 확인
        /// </summary>
        public static bool CanAttackByTeam(this ICombatSystem combat, GameObject target)
        {
            if (combat is not Component combatComponent) return false;
            
            var attackerTeam = combatComponent.GetComponent<ITeamComponent>();
            var targetTeam = target.GetComponent<ITeamComponent>();
            
            if (attackerTeam == null || targetTeam == null) return true;
            
            var relation = attackerTeam.GetRelationTo(targetTeam);
            return relation == TeamRelation.Enemy;
        }
    }
}