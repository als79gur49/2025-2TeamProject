using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using Game.Services.Modifiers;

namespace Game.Data.Modifiers
{
    /// <summary>
    /// Modifier 타입 열거형
    /// Registry에서 Modifier 생성 시 사용
    /// </summary>
    public enum ModifierType
    {
        RangedAttack,
        MeleeAttack,
        SniperAttack,
        NormalMovement,
        BoosterMovement
    }

    /// <summary>
    /// 불변 Modifier 설정 구조체
    /// ScriptableObject로부터 변환된 런타임 데이터 전송 객체
    /// </summary>
    public readonly struct ModifierConfig
    {
        // 기본 설정
        public readonly string Name;
        public readonly int Priority;
        public readonly ChainBehavior ChainBehavior;
        public readonly ActionType ActionType;
        public readonly ModifierType Type;

        // 조건 및 타겟팅
        public readonly IReadOnlyList<IModifierCondition> Conditions;
        public readonly TargetingParams TargetingParams;

        // 타입별 설정 (null일 수 있음)
        public readonly AttackConfig? AttackConfig;
        public readonly MovementConfig? MovementConfig;

        public ModifierConfig(
            string name,
            int priority,
            ChainBehavior chainBehavior,
            ActionType actionType,
            ModifierType type,
            IReadOnlyList<IModifierCondition> conditions = null,
            TargetingParams targetingParams = default,
            AttackConfig? attackConfig = null,
            MovementConfig? movementConfig = null)
        {
            Name = name;
            Priority = priority;
            ChainBehavior = chainBehavior;
            ActionType = actionType;
            Type = type;
            Conditions = conditions ?? new List<IModifierCondition>();
            TargetingParams = targetingParams;
            AttackConfig = attackConfig;
            MovementConfig = movementConfig;
        }

        /// <summary>
        /// 설정 유효성 검증
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(Name)) return false;
            if (Priority < 0 || Priority > 200) return false;

            // ActionType에 따른 Config 검증
            if (ActionType == ActionType.Attack && !AttackConfig.HasValue) return false;
            if (ActionType == ActionType.Movement && !MovementConfig.HasValue) return false;

            return true;
        }

        public override string ToString()
        {
            return $"ModifierConfig[{Name}, Priority:{Priority}, Type:{Type}]";
        }
    }

    /// <summary>
    /// 공격 관련 설정
    /// </summary>
    public readonly struct AttackConfig
    {
        public readonly int Range;
        public readonly int DamageModifier;
        public readonly bool Piercing;
        public readonly bool RequiresClearLane;
        public readonly bool TargetNexusOnly;

        public AttackConfig(
            int range = 1,
            int damageModifier = 0,
            bool piercing = false,
            bool requiresClearLane = false,
            bool targetNexusOnly = false)
        {
            Range = Mathf.Max(1, range);
            DamageModifier = damageModifier;
            Piercing = piercing;
            RequiresClearLane = requiresClearLane;
            TargetNexusOnly = targetNexusOnly;
        }
    }

    /// <summary>
    /// 이동 관련 설정
    /// </summary>
    public readonly struct MovementConfig
    {
        public readonly int MaxRange;
        public readonly bool DiagonalMovement;
        public readonly bool UnlimitedRange;

        public MovementConfig(
            int maxRange = 3,
            bool diagonalMovement = false,
            bool unlimitedRange = false)
        {
            MaxRange = Mathf.Max(1, maxRange);
            DiagonalMovement = diagonalMovement;
            UnlimitedRange = unlimitedRange;
        }
    }

    /// <summary>
    /// 타겟팅 파라미터
    /// ITargetSelector에 전달되는 설정
    /// </summary>
    public readonly struct TargetingParams
    {
        public readonly int Range;
        public readonly bool Piercing;
        public readonly bool DiagonalAllowed;
        public readonly bool RequiresClearLane;
        public readonly TeamRelation TargetRelation;
        public readonly TargetType TargetType;
        public readonly bool TargetNexusOnly;

        public TargetingParams(
            int range = 1,
            bool piercing = false,
            bool diagonalAllowed = false,
            bool requiresClearLane = false,
            TeamRelation targetRelation = TeamRelation.Enemy,
            TargetType targetType = TargetType.Both,
            bool targetNexusOnly = false)
        {
            Range = range;
            Piercing = piercing;
            DiagonalAllowed = diagonalAllowed;
            RequiresClearLane = requiresClearLane;
            TargetRelation = targetRelation;
            TargetType = targetType;
            TargetNexusOnly = targetNexusOnly;
        }
    }

    /// <summary>
    /// 타겟 타입 열거형
    /// </summary>
    public enum TargetType
    {
        Unit,
        Base,
        Both
    }
}
