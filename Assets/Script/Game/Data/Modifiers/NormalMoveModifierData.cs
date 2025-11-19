using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Data.Modifiers
{
    /// <summary>
    /// 일반 이동 Modifier 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "NormalMoveModifier", menuName = "Game/Modifiers/Movement/Normal", order = 10)]
    public class NormalMoveModifierData : MovementModifierData
    {
        public override ModifierConfig ToConfig()
        {
            var movementConfig = new MovementConfig(
                maxRange: 1,
                diagonalMovement: diagonalMovement,
                unlimitedRange: false
            );

            var targetingParams = new TargetingParams(
                range: 1,
                piercing: false,
                diagonalAllowed: diagonalMovement,
                requiresClearLane: false,
                targetRelation: TeamRelation.Enemy,
                targetType: TargetType.Both,
                targetNexusOnly: false,
                directions: AttackDirectionFlags.Forward
            );

            return new ModifierConfig(
                name: modifierName,
                priority: priority,
                chainBehavior: chainBehavior,
                actionType: actionType,
                type: ModifierType.NormalMovement,
                conditions: BuildConditions(),
                targetingParams: targetingParams,
                attackConfig: null,
                movementConfig: movementConfig
            );
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            modifierName = "일반 이동";
        }
#endif
    }
}
