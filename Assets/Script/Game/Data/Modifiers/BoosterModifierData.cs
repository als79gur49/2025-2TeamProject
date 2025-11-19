using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Data.Modifiers
{
    /// <summary>
    /// 부스터 이동 Modifier 데이터
    /// 전체 맵 이동 가능
    /// </summary>
    [CreateAssetMenu(fileName = "BoosterModifier", menuName = "Game/Modifiers/Movement/Booster", order = 11)]
    public class BoosterModifierData : MovementModifierData
    {
        public override ModifierConfig ToConfig()
        {
            var movementConfig = new MovementConfig(
                maxRange: 99,
                diagonalMovement: diagonalMovement,
                unlimitedRange: true
            );

            var targetingParams = new TargetingParams(
                range: 99,
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
                type: ModifierType.BoosterMovement,
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
            modifierName = "부스터";
        }
#endif
    }
}
