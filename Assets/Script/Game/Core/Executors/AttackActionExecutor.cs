using UnityEngine;
using Game.Interfaces;
using Game.Components;

namespace Game.Core.Executors
{
    public class AttackActionExecutor : IActionExecutor
    {
        public ActionType SupportedActionType => ActionType.Attack;

        public void Execute(ActionResult result, ActionContext context, Unit owner)
        {
            Debug.Log($"[AttackExecutor] 공격 실행");

            var combatComponent = owner.GetComponent<CombatComponent>();
            if (combatComponent != null)
                combatComponent.ExecuteAttackWithResult(result, context);
            else
                Debug.LogWarning($"[AttackExecutor] CombatComponent 없음");
        }
    }
}
