using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    [CreateAssetMenu(fileName = "FrenzyOnKillEffect", menuName = "Game/Unit Effects/Attack/FrenzyOnKill")]
    public class FrenzyOnKillEffectData : UnitEffectData
    {
        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.FrenzyOnKillEffect(
                owner,
                Trigger,
                Priority,
                DurationTurns
            );
        }
    }
}

