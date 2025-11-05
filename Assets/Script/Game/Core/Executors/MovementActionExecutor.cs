using UnityEngine;
using Game.Interfaces;
using Game.Components;

namespace Game.Core.Executors
{
    public class MovementActionExecutor : IActionExecutor
    {
        public ActionType SupportedActionType => ActionType.Movement;

        public void Execute(ActionResult result, ActionContext context, Unit owner)
        {
            Debug.Log($"[MovementExecutor] 이동 실행");

            if (!result.MoveDestination.HasValue)
            {
                Debug.LogWarning($"[MovementExecutor] 이동 목적지 없음");
                return;
            }

            var movementComponent = owner.GetComponent<MovementComponent>();
            if (movementComponent != null)
                movementComponent.ExecuteMoveWithResult(result, context);
            else
                Debug.LogWarning($"[MovementExecutor] MovementComponent 없음");
        }
    }
}
