using UnityEngine;
using Game.Interfaces;
using System.Collections.Generic;

namespace Game.Core
{
    public class ActionExecutorRegistry
    {
        private Dictionary<ActionType, IActionExecutor> executors = new Dictionary<ActionType, IActionExecutor>();

        public void RegisterExecutor(IActionExecutor executor)
        {
            if (executor == null) return;
            executors[executor.SupportedActionType] = executor;
            Debug.Log($"[ActionExecutorRegistry] {executor.SupportedActionType} Executor 등록");
        }

        public bool TryExecute(ActionResult result, ActionContext context, Unit owner)
        {
            if (executors.TryGetValue(result.ActionType, out var executor))
            {
                executor.Execute(result, context, owner);
                Debug.LogWarning($"[ActionExecutorRegistry] Execute {result.ActionType} {result.SelectedModifier.ModifierName}");

                return true;
            }

            Debug.LogWarning($"[ActionExecutorRegistry] {result.ActionType} Executor 없음");
            return false;
        }
    }
}
