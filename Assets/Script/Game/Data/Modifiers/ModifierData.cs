using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using Game.Services.Modifiers;
using Game.Services.Modifiers.Conditions;

namespace Game.Data.Modifiers
{
    /// <summary>
    /// Modifier 데이터의 기본 추상 클래스
    /// ScriptableObject로 에디터에서 설정 가능한 데이터를 관리
    /// </summary>
    public abstract class ModifierData : ScriptableObject
    {
        [Header("기본 설정")]
        [SerializeField] protected string modifierName = "Unnamed Modifier";

        [SerializeField, Range(0, 200)]
        protected int priority = 50;

        [SerializeField]
        protected ChainBehavior chainBehavior = ChainBehavior.AlwaysContinue;

        [SerializeField]
        protected ActionType actionType;

        [Header("조건")]
        [SerializeField]
        protected bool requiresMinimumHealth = false;

        [SerializeField, Range(0, 100)]
        protected int minimumHealthPercentage = 30;

        // 프로퍼티
        public string ModifierName => modifierName;
        public int Priority => priority;
        public ChainBehavior ChainBehavior => chainBehavior;
        public ActionType ActionType => actionType;

        /// <summary>
        /// ModifierConfig로 변환 (Factory Method)
        /// 각 서브클래스에서 구체적인 Config를 생성
        /// </summary>
        public abstract ModifierConfig ToConfig();

        /// <summary>
        /// 조건 리스트 생성
        /// 서브클래스에서 오버라이드하여 추가 조건 포함 가능
        /// </summary>
        protected virtual List<IModifierCondition> BuildConditions()
        {
            var conditions = new List<IModifierCondition>();

            if (requiresMinimumHealth)
            {
                conditions.Add(new MinimumHealthCondition(minimumHealthPercentage));
            }

            return conditions;
        }

        /// <summary>
        /// 데이터 유효성 검증
        /// </summary>
        public virtual bool Validate()
        {
            if (string.IsNullOrEmpty(modifierName))
            {
                Debug.LogError($"[ModifierData] {name}: Modifier name is empty");
                return false;
            }

            if (priority < 0 || priority > 200)
            {
                Debug.LogError($"[ModifierData] {name}: Invalid priority {priority}");
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            // 에디터에서 값 변경 시 자동 검증
            if (string.IsNullOrEmpty(modifierName))
            {
                modifierName = name; // 파일명을 기본 이름으로 사용
            }

            priority = Mathf.Clamp(priority, 0, 200);
        }
#endif
    }

    /// <summary>
    /// 공격 Modifier 기본 클래스
    /// </summary>
    public abstract class AttackModifierData : ModifierData
    {
        [Header("공격 설정")]
        [SerializeField, Range(-10, 10)]
        protected int damageAdded = 0;  // 추가 데미지 (음수면 감소)

        public int DamageAdded => damageAdded;

        protected AttackModifierData()
        {
            actionType = ActionType.Attack;
        }

        protected override List<IModifierCondition> BuildConditions()
        {
            var conditions = base.BuildConditions();
            // 공격 관련 추가 조건이 있으면 여기에 추가
            return conditions;
        }
    }

    /// <summary>
    /// 이동 Modifier 기본 클래스
    /// </summary>
    public abstract class MovementModifierData : ModifierData
    {
        [Header("이동 설정")]
        [SerializeField]
        protected bool diagonalMovement = false;

        public bool DiagonalMovement => diagonalMovement;

        protected MovementModifierData()
        {
            actionType = ActionType.Movement;
        }

        protected override List<IModifierCondition> BuildConditions()
        {
            var conditions = base.BuildConditions();
            // 이동 관련 추가 조건이 있으면 여기에 추가
            return conditions;
        }
    }
}
