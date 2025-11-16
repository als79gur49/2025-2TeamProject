using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Data.Modifiers
{
    /// <summary>
    /// 근접 공격 Modifier 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeModifier", menuName = "Game/Modifiers/Attack/Melee", order = 2)]
    public class MeleeModifierData : AttackModifierData
    {
        public override ModifierConfig ToConfig()
        {
            var attackConfig = new AttackConfig(
                range: 1,
                damageModifier: damageAdded,
                piercing: false,
                requiresClearLane: false,
                targetNexusOnly: false
            );

            var targetingParams = new TargetingParams(
                range: 1,
                piercing: false,
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
                type: ModifierType.MeleeAttack,
                conditions: BuildConditions(),
                targetingParams: targetingParams,
                attackConfig: attackConfig,
                movementConfig: null
            );
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            modifierName = "근접";
        }
#endif
    }
}
