using Game.Core.Effects;
using Game.Interfaces;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 배치 시 체력 및 공격력을 증가시키는 Effect입니다.
    /// </summary>
    public class DeployBonusEffect : IEffect
    {
        public string EffectName => "배치 보너스";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int healthBonus;
        private readonly int attackBonus;

        public DeployBonusEffect(Unit owner, int healthBonus, int attackBonus, EffectTrigger trigger, int priority, int durationTurns)
        {
            Owner = owner;
            this.healthBonus = healthBonus;
            this.attackBonus = attackBonus;
            Trigger = trigger;
            Priority = priority;
            RemainingDuration = durationTurns;
        }

        public void TickDuration()
        {
            if (RemainingDuration > 0)
                RemainingDuration--;
        }

        public bool CanApply(EffectContext context)
        {
            return Owner != null && Trigger == EffectTrigger.OnDeploy;
        }

        public void Apply(EffectContext context)
        {
            var health = Owner.GetComponent<IHealthComponent>();
            var combat = Owner.GetComponent<ICombatSystem>();

            if (health != null && healthBonus != 0)
            {
                health.SetMaxHealth(health.MaxHealth + healthBonus);
                health.Heal(healthBonus);
            }

            if (combat != null && attackBonus != 0)
            {
                combat.SetBaseAttackPower(combat.CurrentAttackPower + attackBonus);
            }

            Debug.Log($"[DeployBonusEffect] {Owner.name} gained +{healthBonus} HP, +{attackBonus} ATK on deploy");
        }
    }
}

