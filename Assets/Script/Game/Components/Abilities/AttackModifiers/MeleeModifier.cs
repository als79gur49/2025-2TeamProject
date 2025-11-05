using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class MeleeModifier : IAttackModifier
    {
        public string ModifierName => "근접";
        public ActionType ActionType => ActionType.Attack;
        public int Priority { get; private set; }
        public ChainBehavior ChainBehavior => ChainBehavior.AlwaysContinue;
        public Unit Owner { get; private set; }

        private IActionModifier nextModifier;
        private IGridManager gridManager;
        private ITeamComponent teamComponent;
        private ICombatSystem combatSystem;

        public MeleeModifier(Unit owner, int priority = 80)
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

            List<Tile> adjacentTiles = FindAdjacentEnemyTiles(context.ActorPosition);

            if (adjacentTiles.Count > 0)
            {
                result.ValidTiles = adjacentTiles;
                result.IsSuccess = true;
                result.SelectedModifier = this;
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

        private List<Tile> FindAdjacentEnemyTiles(Vector2Int pos)
        {
            List<Tile> tiles = new List<Tile>();
            var gridController = gridManager.GetGridController();
            if (gridController == null) return tiles;

            int direction = teamComponent?.Team == TeamType.Player ? 1 : -1;

            Vector2Int checkPos = new Vector2Int(pos.x, pos.y + (direction * 1));
            var tile = gridController.GetTileAtPosition(checkPos);

            if (tile == null) return new List<Tile>();

            bool hasUnit = tile.OccupyingUnit != null && tile.OccupyingUnit.IsAlive;
            bool hasBase = tile.OccupyingBase != null && tile.OccupyingBase.IsAlive;
            
            if (hasUnit || hasBase)
            {
                var targetObj = hasUnit ? tile.OccupyingUnit.gameObject : tile.OccupyingBase.gameObject;
                if (IsEnemy(targetObj))
                    tiles.Add(tile);
            }

            return tiles;
        }

        private bool IsEnemy(GameObject target)
        {
            if (teamComponent == null) return true;
            var targetTeam = target.GetComponent<ITeamComponent>();
            return targetTeam == null ? true : teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
        }
    }
}
