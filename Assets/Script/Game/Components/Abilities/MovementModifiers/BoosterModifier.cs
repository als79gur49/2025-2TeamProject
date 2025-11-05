using UnityEngine;
using Game.Interfaces;
using Game.Core;

namespace Game.Components.Abilities
{
    public class BoosterModifier : IMovementModifier
    {
        public string ModifierName => "부스터";
        public ActionType ActionType => ActionType.Movement;
        public int Priority { get; private set; }
        public ChainBehavior ChainBehavior => ChainBehavior.AlwaysTerminate;
        public Unit Owner { get; private set; }
        public int MaxMoveRange => 99;

        private IActionModifier nextModifier;
        private IGridManager gridManager;

        public BoosterModifier(Unit owner, int priority = 20)
        {
            Owner = owner;
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
        {
            Vector2Int direction = new Vector2Int(
                Mathf.Clamp(intended.x - context.ActorPosition.x, -1, 1),
                Mathf.Clamp(intended.y - context.ActorPosition.y, -1, 1)
            );

            Vector2Int finalPos = context.ActorPosition;

            while (true)
            {
                Vector2Int nextPos = new Vector2Int(
                    finalPos.x + direction.x,
                    finalPos.y + direction.y
                );

                if (!gridManager.IsValidPosition(nextPos) || !gridManager.IsPositionWalkable(nextPos))
                    break;

                finalPos = nextPos;
            }

            return finalPos;
        }

        private Vector2Int? FindMoveTarget(Vector2Int currentPos)
        {
            Vector2Int forward = new Vector2Int(currentPos.x, currentPos.y + 1);
            return gridManager.IsValidPosition(forward) ? forward : (Vector2Int?)null;
        }
    }
}
