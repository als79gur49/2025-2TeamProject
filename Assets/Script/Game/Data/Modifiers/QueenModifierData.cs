using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Data.Modifiers
{
    /// <summary>
    /// 퀸 공격 Modifier 데이터
    /// 정면 + 대각선 방향으로 무제한(사실상 보드 끝까지) 공격
    /// </summary>
    [CreateAssetMenu(fileName = "QueenModifier", menuName = "Game/Modifiers/Attack/Queen", order = 5)]
    public class QueenModifierData : AttackModifierData
    {
        [Header("퀸 설정")]
        [SerializeField, Range(1, 99)]
        private int attackRange = 99;

        [SerializeField]
        private bool piercing = false;

        [SerializeField]
        private AttackDirectionFlags directions =
            AttackDirectionFlags.Forward |
            AttackDirectionFlags.DiagonalUp |
            AttackDirectionFlags.DiagonalDown;

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
                diagonalAllowed: true,
                requiresClearLane: false,
                targetRelation: TeamRelation.Enemy,
                targetType: TargetType.Both,
                targetNexusOnly: false,
                directions: directions
            );

            return new ModifierConfig(
                name: modifierName,
                priority: priority,
                chainBehavior: chainBehavior,
                actionType: actionType,
                type: ModifierType.QueenAttack,
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

            attackRange = Mathf.Clamp(attackRange, 1, 99);

            if (string.IsNullOrEmpty(modifierName) || modifierName == "Unnamed Modifier")
            {
                modifierName = "퀸";
            }
        }
#endif
    }
}

