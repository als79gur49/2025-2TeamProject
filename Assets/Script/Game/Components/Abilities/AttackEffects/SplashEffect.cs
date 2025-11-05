using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class SplashEffect : IAttackEffect
    {
        public string EffectName { get; private set; }
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }

        private int splashDamage;
        private IGridManager gridManager;
        private ITeamComponent teamComponent;

        public SplashEffect(Unit owner, int damage, int priority = 10)
        {
            Owner = owner;
            splashDamage = damage;
            Priority = priority;
            EffectName = $"방사({damage})";
            gridManager = ServiceLocator.Get<IGridManager>();
            teamComponent = owner.GetComponent<ITeamComponent>();
        }

        public void ApplyEffectToTiles(List<Tile> primaryTiles, ActionContext context)
        {
            var gridController = gridManager.GetGridController();
            if (gridController == null) return;

            foreach (var primaryTile in primaryTiles)
            {
                if (primaryTile == null) continue;

                Vector2Int[] splashPositions = new Vector2Int[]
                {
                    new Vector2Int(primaryTile.X, primaryTile.Y + 1),
                    new Vector2Int(primaryTile.X, primaryTile.Y - 1)
                };

                foreach (Vector2Int splashPos in splashPositions)
                {
                    var tile = gridController.GetTileAtPosition(splashPos);
                    if (tile == null) continue;

                    var targetHealth = tile.GetDamageableTarget();
                    if (targetHealth != null && targetHealth.IsAlive && IsEnemy(targetHealth.gameObject))
                        targetHealth.TakeDamage(splashDamage);
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
