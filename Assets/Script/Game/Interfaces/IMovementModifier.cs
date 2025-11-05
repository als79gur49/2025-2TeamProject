using UnityEngine;

namespace Game.Interfaces
{
    public interface IMovementModifier : IActionModifier
    {
        int MaxMoveRange { get; }
        Vector2Int CalculateFinalDestination(Vector2Int intended, ActionContext context);
    }
}
