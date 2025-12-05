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

        public DurationType DurationType => DurationType.TimeBased;

        private readonly int damagePerTurn;

        public SelfDamageDebuffEffect(Unit owner, int damagePerTurn, EffectTrigger trigger, int priority, int durationTurns)
        {
            Owner = owner;
            this.damagePerTurn = damagePerTurn;
            Trigger = trigger;
            Priority = priority;
            RemainingDuration = durationTurns;
        }

        public void TickDuration(DurationTickContext context)
        {
            // 자해 디버프는 전역 턴 종료(TurnEnd)를 기준으로 지속 시간을 감소시킵니다.
            if (context.Source != DurationTickSource.TurnEnd)
                return;

            if (RemainingDuration > 0)
                RemainingDuration--;
        }

        public bool CanApply(EffectContext context)
        {
            return Owner != null && Owner.IsAlive;
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
