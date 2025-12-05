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

        public DurationType DurationType => DurationType.ActionBased;

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

        public void TickDuration(DurationTickContext context)
        {
            // 배치 보너스는 유닛의 행동 턴(Action Turn)을 기준으로 지속 턴을 감소시킵니다.
            if (context.Source != DurationTickSource.ActionEnd)
            {
                return;
            }

            if (RemainingDuration > 0)
            {
                RemainingDuration--;

                // 지속 시간이 끝나는 시점에 공격력 보너스 해제
                if (RemainingDuration == 0 && Owner != null)
                {
                    var combat = Owner.GetComponent<ICombatSystem>();
                    if (combat != null && attackBonus != 0)
                    {
                        combat.ModifyAttackPower(-attackBonus);
                    }
                }
            }
        }

        public bool CanApply(EffectContext context)
        {
            return Owner != null;
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
                // 현재 공격력 대신, 기본 공격력에 델타를 적용
                combat.ModifyAttackPower(attackBonus);
            }

            Debug.Log($"[DeployBonusEffect] {Owner.name} gained +{healthBonus} HP, +{attackBonus} ATK on deploy");
        }
    }
}
