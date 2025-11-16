using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    [CreateAssetMenu(fileName = "SplashEffect", menuName = "Game/Unit Effects/Attack/Splash")]
    public class SplashEffectData : UnitEffectData
    {
        [Header("Splash 설정")]
        [SerializeField] private int splashDamage = 5;

        public int SplashDamage => splashDamage;

        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.SplashEffect(
                owner,
                SplashDamage,
                Trigger,
                Priority,
                DurationTurns
            );
        }
    }
}

