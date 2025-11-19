using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Services.Modifiers;
using Game.Services.Modifiers.Conditions;
using Game.Interfaces;

namespace Game.Data.Modifiers
{
    /// <summary>
    /// 스나이퍼 공격 Modifier 데이터
    /// 항상 넥서스만 공격, 같은 행에 적이 없어야 함
    /// </summary>
    [CreateAssetMenu(fileName = "SniperModifier", menuName = "Game/Modifiers/Attack/Sniper", order = 3)]
    public class SniperModifierData : AttackModifierData
    {
        [Header("스나이퍼 설정")]
        [SerializeField]
        private bool requiresClearLane = true; // 직선상 장애물 없어야 함

        public bool RequiresClearLane => requiresClearLane;

        public override ModifierConfig ToConfig()
        {
            var attackConfig = new AttackConfig(
                range: 99, // 스나이퍼는 무제한 범위
                damageModifier: damageAdded,
                piercing: false,
                requiresClearLane: requiresClearLane,
                targetNexusOnly: true
            );

            var targetingParams = new TargetingParams(
                range: 99,
                piercing: false,
                diagonalAllowed: false,
                requiresClearLane: requiresClearLane,
                targetRelation: TeamRelation.Enemy,
                targetType: TargetType.Base,
                targetNexusOnly: true,
                directions: AttackDirectionFlags.Forward
            );

            return new ModifierConfig(
                name: modifierName,
                priority: priority,
                chainBehavior: chainBehavior,
                actionType: actionType,
                type: ModifierType.SniperAttack,
                conditions: BuildConditions(),
                targetingParams: targetingParams,
                attackConfig: attackConfig,
                movementConfig: null
            );
        }

        protected override List<IModifierCondition> BuildConditions()
        {
            var conditions = base.BuildConditions();

            if (requiresClearLane)
            {
                var gridManager = ServiceLocator.Get<IGridManager>();
                if (gridManager != null)
                {
                    conditions.Add(new ClearLaneCondition(gridManager));
                }
                else
                {
                    Debug.LogWarning("[SniperModifierData] GridManager not found in ServiceLocator. ClearLaneCondition not added.");
                }
            }

            return conditions;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            modifierName = "스나이퍼";
        }
#endif
    }
}
