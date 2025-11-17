using Game.Core;
using Game.Core.Effects;
using Game.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 공격 적중 시 대상에게 스턴 상태를 부여하는 Effect입니다.
    /// </summary>
    public class StunEffect : IEffect
    {
        public string EffectName => "스턴";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int stunDuration;

        public StunEffect(Unit owner, int stunDuration, EffectTrigger trigger, int priority, int durationTurns)
        {
            Owner = owner;
            this.stunDuration = stunDuration;
            Trigger = trigger;
            Priority = priority;
            RemainingDuration = durationTurns;
        }

        public void TickDuration()
        {
            if (RemainingDuration > 0)
                RemainingDuration--;
        }

        public bool CanApply(EffectContext context)
        {
            return Owner != null && Owner.IsAlive &&
                   context != null &&
                   context.AffectedTiles != null &&
                   context.AffectedTiles.Count > 0;
        }

        public void Apply(EffectContext context)
        {
            if (context == null || context.AffectedTiles == null) return;

            foreach (var tile in context.AffectedTiles)
            {
                if (tile == null) continue;

                var targetHealth = tile.GetDamageableTarget();
                if (targetHealth != null && targetHealth.IsAlive)
                {
                    var targetUnit = targetHealth.gameObject.GetComponent<Unit>();
                    if (targetUnit != null)
                    {
                        targetUnit.AddStun(stunDuration);
                    }
                }
            }
        }
    }
}
