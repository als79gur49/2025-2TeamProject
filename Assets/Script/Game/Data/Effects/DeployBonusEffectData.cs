using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    [CreateAssetMenu(fileName = "DeployBonusEffect", menuName = "Game/Unit Effects/Deploy/Bonus")]
    public class DeployBonusEffectData : UnitEffectData
    {
        [Header("배치 보너스 설정")]
        [SerializeField] private int healthBonus = 0;
        [SerializeField] private int attackBonus = 0;

        public int HealthBonus => healthBonus;
        public int AttackBonus => attackBonus;

        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.DeployBonusEffect(
                owner,
                HealthBonus,
                AttackBonus,
                Trigger,
                Priority,
                DurationTurns
            );
        }
    }
}

