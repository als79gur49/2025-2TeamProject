using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    [CreateAssetMenu(fileName = "ExplosionOnDeathEffect", menuName = "Game/Unit Effects/Death/Explosion")]
    public class ExplosionOnDeathEffectData : UnitEffectData
    {
        [Header("폭발 설정")]
        [SerializeField] private int explosionDamage = 5;
        [SerializeField] private int explosionRadius = 1;

        public int ExplosionDamage => explosionDamage;
        public int ExplosionRadius => explosionRadius;

        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.ExplosionOnDeathEffect(
                owner,
                ExplosionDamage,
                ExplosionRadius,
                Trigger,
                Priority,
                DurationTurns
            );
        }
    }
}

