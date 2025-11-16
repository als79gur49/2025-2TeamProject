using Game.Core.Effects;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 유닛에 부착되어 일정 턴 동안 기절 상태를 유지하는 Effect입니다.
    /// 실제 행동 스킵은 Unit.ExecuteAITurn에서 HasEffect(\"스턴\") 체크로 처리합니다.
    /// </summary>
    public class StunStatusEffect : IEffect
    {
        public string EffectName => "스턴";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        public StunStatusEffect(Unit owner, int stunTurns, EffectTrigger trigger, int priority, int durationTurns)
        {
            Owner = owner;
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
            return Owner != null && Owner.IsAlive;
        }

        public void Apply(EffectContext context)
        {
            // 실제 행동 제한은 Unit.ExecuteAITurn가 EffectManager.HasEffect(\"스턴\")를 통해 처리합니다.
            Debug.Log($"[StunStatusEffect] {Owner?.name} is stunned. Remaining turns: {RemainingDuration}");
        }
    }
}

