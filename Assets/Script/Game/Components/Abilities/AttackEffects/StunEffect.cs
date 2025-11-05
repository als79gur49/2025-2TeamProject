using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class StunEffect : IAttackEffect
    {
        public string EffectName => "스턴";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }

        private int stunDuration;

        public StunEffect(Unit owner, int duration = 1, int priority = 5)
        {
            Owner = owner;
            stunDuration = duration;
            Priority = priority;
        }

        public void ApplyEffectToTiles(List<Tile> primaryTiles, ActionContext context)
        {
            foreach (var tile in primaryTiles)
            {
                if (tile == null) continue;

                var targetHealth = tile.GetDamageableTarget();
                if (targetHealth != null && targetHealth.IsAlive)
                {
                    var targetUnit = targetHealth.gameObject.GetComponent<Unit>();
                    if (targetUnit != null)
                        targetUnit.AddStun(stunDuration);
                }
            }
        }
    }
}
