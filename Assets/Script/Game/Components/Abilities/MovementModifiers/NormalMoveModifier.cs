using UnityEngine;
using Game.Interfaces;
using Game.Core;

namespace Game.Components.Abilities
{
    public class NormalMoveModifier : IMovementModifier
    {
        public string ModifierName => "일반 이동";
        public ActionType ActionType => ActionType.Movement;
        public int Priority { get; private set; }
        public ChainBehavior ChainBehavior => ChainBehavior.AlwaysTerminate;
        public Unit Owner { get; private set; }
        public int MaxMoveRange { get; private set; }

        private IActionModifier nextModifier;
        private IGridManager gridManager;

        public NormalMoveModifier(Unit owner, int moveRange = 1, int priority = 10)
        {
            Owner = owner;
            MaxMoveRange = moveRange;
            Priority = priority;
            gridManager = ServiceLocator.Get<IGridManager>();
        }

        public void SetNext(IActionModifier next) => nextModifier = next;

        public ActionResult Evaluate(ActionContext context)
        {
            ActionResult result = new ActionResult
            {
                ActionType = ActionType.Movement,
                ChainBehavior = this.ChainBehavior
            };

            context.MoveRange = MaxMoveRange;
            Vector2Int? moveTarget = FindMoveTarget(context.ActorPosition);

            if (moveTarget.HasValue)
            {
                result.IsSuccess = true;
                result.MoveDestination = moveTarget.Value;
                result.SelectedModifier = this;
            }
            else
            {
                result.IsSuccess = false;
            }

            return result;
        }

        public void Execute(ActionContext context) { }

        public Vector2Int CalculateFinalDestination(Vector2Int intended, ActionContext context)
            => intended;

        private Vector2Int? FindMoveTarget(Vector2Int currentPos)
        {
            Vector2Int forward = new Vector2Int(currentPos.x, currentPos.y + 1);
            if (gridManager.IsValidPosition(forward) && !gridManager.IsPositionWalkable(forward))
                return forward;

            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(currentPos.x + 1, currentPos.y),
                new Vector2Int(currentPos.x - 1, currentPos.y),
                new Vector2Int(currentPos.x, currentPos.y - 1),
            };

            foreach (var dir in directions)
                if (gridManager.IsValidPosition(dir) && !gridManager.IsPositionWalkable(dir))
                    return dir;

            return null;
        }
    }
}
