using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class RangedModifier : IAttackModifier
    {
        public string ModifierName { get; private set; }
        public ActionType ActionType => ActionType.Attack;
        public int Priority { get; private set; }
        public virtual ChainBehavior ChainBehavior => ChainBehavior.AlwaysContinue;
        public Unit Owner { get; private set; }

        protected int range;
        private IActionModifier nextModifier;
        private IGridManager gridManager;
        private ITeamComponent teamComponent;
        private ICombatSystem combatSystem;

        public RangedModifier(Unit owner, int range, int priority = 90)
        {
            Owner = owner;
            this.range = range;
            Priority = priority;
            ModifierName = $"원거리({range})";
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

            context.AttackRange = range;
            List<Tile> tilesInRange = FindTilesInRange(context.ActorPosition);

            if (tilesInRange.Count > 0)
            {
                result.ValidTiles = tilesInRange;
                result.IsSuccess = true;
                result.SelectedModifier = this;

                Debug.Log($"[RangedModifier] Evaluate Result: {result.IsSuccess}," +
                      $" TileCounts: {result.ValidTiles.Count}," +
                      $" Modifier: {result.SelectedModifier.ModifierName}");
            }
            else
            {
                result.IsSuccess = false;

                Debug.Log($"[RangedModifier] Evaluate Result: {result.IsSuccess}," +
                      $" TileCounts: {result.ValidTiles.Count}");
            }

            return result;
        }

        public void Execute(ActionContext context) { }

        public int CalculateDamage(ActionContext context)
            => combatSystem?.CurrentAttackPower ?? 0;

        private List<Tile> FindTilesInRange(Vector2Int attackerPos)
        {
            List<Tile> tiles = new List<Tile>();
            var gridController = gridManager.GetGridController();
            if (gridController == null) return tiles;

            int direction = teamComponent?.Team == TeamType.Player ? 1 : -1;

            for (int i = 1; i <= range; i++)
            {
                Vector2Int checkPos = new Vector2Int(attackerPos.x, attackerPos.y + (direction * i));
                var tile = gridController.GetTileAtPosition(checkPos);
                if (tile == null) continue;

                bool hasUnit = tile.OccupyingUnit != null && tile.OccupyingUnit.IsAlive;
                bool hasBase = tile.OccupyingBase != null && tile.OccupyingBase.IsAlive;

                if (hasUnit || hasBase)
                {
                    var targetObj = hasUnit ? tile.OccupyingUnit.gameObject : tile.OccupyingBase.gameObject;
                    if (IsEnemy(targetObj))
                    {
                        tiles.Add(tile);
                        break;
                    }
                }
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

    public class RangedContinueModifier : RangedModifier
    {
        public override ChainBehavior ChainBehavior => ChainBehavior.AlwaysContinue;
        public RangedContinueModifier(Unit owner, int range, int priority = 90)
            : base(owner, range, priority) { }
    }
}
