using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    [CreateAssetMenu(fileName = "SelfDamageDebuff", menuName = "Game/Unit Effects/Status/SelfDamage")]
    public class SelfDamageDebuffData : UnitEffectData
    {
        [Header("자해 디버프 설정")]
        [SerializeField] private int damagePerTurn = 1;

        public int DamagePerTurn => damagePerTurn;

        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.SelfDamageDebuffEffect(
                owner,
                DamagePerTurn,
                Trigger,
                Priority,
                DurationTurns
            );
        }
    }
}

