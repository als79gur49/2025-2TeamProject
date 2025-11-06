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
        private ITeamComponent teamComponent;
        private IMovementSystem movementSystem;

        public NormalMoveModifier(Unit owner, int moveRange = 1, int priority = 10)
        {
            Owner = owner;
            MaxMoveRange = moveRange;
            Priority = priority;
            gridManager = ServiceLocator.Get<IGridManager>();
            teamComponent = owner.GetComponent<ITeamComponent>();
            movementSystem = owner.GetComponent<IMovementSystem>();
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
            => intended;

        private Vector2Int? FindMoveTarget(Vector2Int currentPos)
        {
            // Validate dependencies
            if (movementSystem == null || gridManager == null)
            {
                Debug.LogWarning($"[NormalMoveModifier] Missing dependencies for {Owner?.name}");
                return null;
            }

            // Get all valid positions within current movement range
            var validPositions = movementSystem.GetValidMovePositions();

            if (validPositions.Count == 0)
            {
                return null;
            }

            // Determine team-based direction (Player: +Y, Enemy: -Y)
            int direction = teamComponent?.Team == TeamType.Player ? 1 : -1;

            // Find the furthest forward position (same logic as BasicUnitAI.GetForwardMovePosition)
            Vector2Int? bestPosition = null;
            int maxDistance = 0;

            foreach (var pos in validPositions)
            {
                int distance = (pos.y - currentPos.y) * direction;
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    bestPosition = pos;
                }
            }

            return bestPosition;
        }
    }
}
