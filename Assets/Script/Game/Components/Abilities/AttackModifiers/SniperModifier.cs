using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class SniperModifier : IAttackModifier
    {
        public string ModifierName => "스나이퍼";
        public ActionType ActionType => ActionType.Attack;
        public int Priority { get; private set; }
        public ChainBehavior ChainBehavior => ChainBehavior.FallbackOnFailure;
        public Unit Owner { get; private set; }

        private IActionModifier nextModifier;
        private IGridManager gridManager;
        private ITeamComponent teamComponent;
        private ICombatSystem combatSystem;

        public SniperModifier(Unit owner, int priority = 100)
        {
            Owner = owner;
            Priority = priority;
            gridManager = ServiceLocator.Get<IGridManager>();
            teamComponent = owner.GetComponent<ITeamComponent>();
            combatSystem = owner.GetComponent<ICombatSystem>();
        }

        public void SetNext(IActionModifier next) => nextModifier = next;

        public ActionResult Evaluate(ActionContext context)
        {
            ActionResult result = new ActionResult
            {
                ActionType = ActionType.Attack,
                ChainBehavior = this.ChainBehavior
            };

            bool hasEnemyInRow = CheckEnemyInSameRow(context.ActorPosition);
            context.HasClearRow = !hasEnemyInRow;

            if (!hasEnemyInRow)
            {
                Tile enemyNexusTile = GetEnemyNexusTile();
                if (enemyNexusTile != null)
                {
                    result.ValidTiles.Add(enemyNexusTile);
                    result.IsSuccess = true;
                    result.SelectedModifier = this;
                }
            }
            else
            {
                result.IsSuccess = false;
            }

            return result;
        }

        public void Execute(ActionContext context) { }

        public int CalculateDamage(ActionContext context)
            => combatSystem?.CurrentAttackPower ?? 0;

        private bool CheckEnemyInSameRow(Vector2Int pos)
        {
            var gridController = gridManager.GetGridController();
            if (gridController == null) return false;
            var gridSize = gridManager.GridSize;

            for (int x = 0; x < gridSize.x; x++)
            {
                if (x == pos.x) continue;
                var tile = gridController.GetTileAtPosition(new Vector2Int(x, pos.y));
                if (tile == null) continue;

                bool hasUnit = tile.OccupyingUnit != null && tile.OccupyingUnit.IsAlive;
                bool hasBase = tile.OccupyingBase != null && tile.OccupyingBase.IsAlive;

                if ((hasUnit || hasBase) && IsEnemy(hasUnit ? tile.OccupyingUnit.gameObject : tile.OccupyingBase.gameObject))
                    return true;
            }
            return false;
        }

        private Tile GetEnemyNexusTile()
        {
            var gridController = gridManager.GetGridController();
            if (gridController == null) return null;
            var gridSize = gridManager.GridSize;

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var tile = gridController.GetTileAtPosition(new Vector2Int(x, y));
                    if (tile?.OccupyingBase != null && tile.OccupyingBase.IsAlive && IsEnemy(tile.OccupyingBase.gameObject))
                        return tile;
                }
            }
            return null;
        }

        private bool IsEnemy(GameObject target)
        {
            if (teamComponent == null) return true;
            var targetTeam = target.GetComponent<ITeamComponent>();
            return targetTeam == null ? true : teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
        }
    }
}
