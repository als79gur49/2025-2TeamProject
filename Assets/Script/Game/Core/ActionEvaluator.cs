using Game.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using static Codice.Client.BaseCommands.Import.Commit;

namespace Game.Core
{
    public class ActionEvaluator
    {
        private List<IActionModifier> actionModifiers = new List<IActionModifier>();
        private IActionModifier actionModifierChain;
        private IActionModifier currentModifier;

        public void AddModifier(IActionModifier modifier)
        {
            if (modifier == null) return;
            actionModifiers.Add(modifier);
            Debug.Log($"[ActionEvaluator] {modifier.ModifierName} is added");

            RebuildChain();
        }

        private void RebuildChain()
        {
            if (actionModifiers.Count == 0) return;

            actionModifiers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            for (int i = 0; i < actionModifiers.Count - 1; i++)
                actionModifiers[i].SetNext(actionModifiers[i + 1]);

            actionModifierChain = actionModifiers[0];
            Debug.Log($"[ActionEvaluator] ActionModifierName: {actionModifierChain.ModifierName}, Modifiers Length: {actionModifiers.Count} ");
        }

        public ActionResult EvaluateNextAction(ActionContext context)
        {
            if (currentModifier == null)
            {
                Debug.LogWarning("[ActionEvaluator] No current modifier - no actions available for this unit");
                return new ActionResult { IsSuccess = false };
            }

            ActionResult result = currentModifier.Evaluate(context);
            Debug.Log($"[ActionEvaluator] EvaluateNextAction: {result.SelectedModifier?.ModifierName ?? "Not Exist"} / Success: {result.IsSuccess}");

            if (!result.IsSuccess && result.ShouldContinueChain())
            {
                currentModifier = GetNextModifier(currentModifier);
                if (currentModifier != null)
                    return EvaluateNextAction(context);
            }

            return result;
        }

        public void MoveToNextModifier()
        {
            currentModifier = GetNextModifier(currentModifier);
        }

        public void Reset()
        {
            currentModifier = actionModifierChain;
        }

        private IActionModifier GetNextModifier(IActionModifier current)
        {
            int currentIndex = actionModifiers.IndexOf(current);
            return (currentIndex >= 0 && currentIndex < actionModifiers.Count - 1)
                ? actionModifiers[currentIndex + 1]
                : null;
        }
    }
}
