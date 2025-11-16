using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Services.Modifiers.Conditions
{
    /// <summary>
    /// 최소 체력 조건
    /// 유닛의 체력이 지정된 퍼센트 이상일 때만 통과
    /// </summary>
    public class MinimumHealthCondition : IModifierCondition
    {
        private readonly int minimumHealthPercentage;

        public string Description => $"체력 {minimumHealthPercentage}% 이상 필요";

        public MinimumHealthCondition(int minimumHealthPercentage)
        {
            this.minimumHealthPercentage = Mathf.Clamp(minimumHealthPercentage, 0, 100);
        }

        public bool Evaluate(Unit owner, ActionContext context)
        {
            if (owner == null) return false;

            var health = owner.GetComponent<IHealthComponent>();
            if (health == null) return false;

            float currentPercent = (float)health.CurrentHealth / health.MaxHealth * 100f;
            return currentPercent >= minimumHealthPercentage;
        }
    }
}
