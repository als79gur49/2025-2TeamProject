using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    [CreateAssetMenu(fileName = "StunOnAttackEffect", menuName = "Game/Unit Effects/Attack/StunOnHit")]
    public class StunOnAttackEffectData : UnitEffectData
    {
        [Header("스턴 설정")]
        [SerializeField] private int stunTurns = 1;

        public int StunTurns => stunTurns;

        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.StunEffect(
                owner,
                StunTurns,
                Trigger,
                Priority,
                DurationTurns
            );
        }
    }
}

