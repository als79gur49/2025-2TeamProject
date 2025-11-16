using Game.Core.Effects;
using Game.Interfaces;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 턴 종료 시마다 자기 자신에게 피해를 주는 디버프 Effect입니다.
    /// </summary>
    public class SelfDamageDebuffEffect : IEffect
    {
        public string EffectName => "자해 디버프";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int damagePerTurn;

        public SelfDamageDebuffEffect(Unit owner, int damagePerTurn, EffectTrigger trigger, int priority, int durationTurns)
        {
            Owner = owner;
            this.damagePerTurn = damagePerTurn;
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
            return Owner != null && Owner.IsAlive && Trigger == EffectTrigger.OnTurnEnd;
        }

        public void Apply(EffectContext context)
        {
            var health = Owner.GetComponent<IHealthComponent>();
            if (health == null || !health.IsAlive) return;

            health.TakeDamage(damagePerTurn);
            Debug.Log($"[SelfDamageDebuffEffect] {Owner.name} takes {damagePerTurn} self damage");
        }
    }
}

