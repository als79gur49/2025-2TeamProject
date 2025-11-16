using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Data.Modifiers
{
    /// <summary>
    /// 원거리 공격 Modifier 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "RangedModifier", menuName = "Game/Modifiers/Attack/Ranged", order = 1)]
    public class RangedModifierData : AttackModifierData
    {
        [Header("원거리 설정")]
        [SerializeField, Range(1, 10)]
        private int attackRange = 3;

        [SerializeField]
        private bool piercing = false; // 관통 공격 여부

        public int AttackRange => attackRange;
        public bool Piercing => piercing;

        public override ModifierConfig ToConfig()
        {
            var attackConfig = new AttackConfig(
                range: attackRange,
                damageModifier: damageAdded,
                piercing: piercing,
                requiresClearLane: false,
                targetNexusOnly: false
            );

            var targetingParams = new TargetingParams(
                range: attackRange,
                piercing: piercing,
                diagonalAllowed: false,
                requiresClearLane: false,
                targetRelation: TeamRelation.Enemy,
                targetType: TargetType.Both,
                targetNexusOnly: false
            );

            return new ModifierConfig(
                name: modifierName,
                priority: priority,
                chainBehavior: chainBehavior,
                actionType: actionType,
                type: ModifierType.RangedAttack,
                conditions: BuildConditions(),
                targetingParams: targetingParams,
                attackConfig: attackConfig,
                movementConfig: null
            );
        }

        public override bool Validate()
        {
            if (!base.Validate()) return false;

            if (attackRange <= 0)
            {
                Debug.LogError($"[RangedModifierData] {name}: Invalid attack range {attackRange}");
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            attackRange = Mathf.Max(1, attackRange);

            // 자동 이름 업데이트
            if (modifierName == "Unnamed Modifier" || modifierName.StartsWith("원거리"))
            {
                modifierName = $"원거리({attackRange})";
            }
        }
#endif
    }
}
