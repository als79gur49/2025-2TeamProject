using UnityEngine;

namespace Game.Interfaces
{
    public interface IActionExecutor
    {
        ActionType SupportedActionType { get; }
        void Execute(ActionResult result, ActionContext context, Unit owner);
    }
}
