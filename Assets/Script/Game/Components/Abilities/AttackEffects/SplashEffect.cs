using Game.Core;
using Game.Core.Effects;
using Game.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 공격 적중 시 주변 타일에 추가 피해를 주는 스플래시 효과입니다.
    /// </summary>
    public class SplashEffect : IEffect
    {
        public string EffectName { get; private set; }
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int splashDamage;
        private readonly IGridManager gridManager;
        private readonly ITeamComponent teamComponent;

        public SplashEffect(Unit owner, int damage, EffectTrigger trigger, int priority, int durationTurns)
        {
            Owner = owner;
            splashDamage = damage;
            Trigger = trigger;
            Priority = priority;
            RemainingDuration = durationTurns;
            EffectName = $"방사({damage})";

            gridManager = ServiceLocator.Get<IGridManager>();
            teamComponent = owner.GetComponent<ITeamComponent>();
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

            var gridController = gridManager.GetGridController();
            if (gridController == null) return;

            foreach (var primaryTile in context.AffectedTiles)
            {
                if (primaryTile == null) continue;

                // 상하 타일에 스플래시 피해 적용
                Vector2Int[] splashPositions =
                {
                    new Vector2Int(primaryTile.X + 1, primaryTile.Y),
                    new Vector2Int(primaryTile.X - 1, primaryTile.Y)
                };

                foreach (Vector2Int splashPos in splashPositions)
                {
                    var tile = gridController.GetTileAtPosition(splashPos);
                    if (tile == null) continue;

                    var targetHealth = tile.GetDamageableTarget();
                    if (targetHealth != null && targetHealth.IsAlive && IsEnemy(targetHealth.gameObject))
                    {
                        targetHealth.TakeDamage(splashDamage, splashPos);
                    }
                }
            }
        }

        private bool IsEnemy(GameObject target)
        {
            if (teamComponent == null) return true;
            var targetTeam = target.GetComponent<ITeamComponent>();
            return targetTeam == null ? true : teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
        }
    }
}
