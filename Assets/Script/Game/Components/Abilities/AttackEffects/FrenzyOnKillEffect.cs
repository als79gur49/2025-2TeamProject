using Game.Core.Effects;
using Game.Interfaces;
using UnityEngine;

namespace Game.Components.Abilities
{
    public class FrenzyOnKillEffect : IEffect
    {
        public string EffectName { get; private set; } = "광란";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        public DurationType DurationType => DurationType.TimeBased;

        private readonly ITeamComponent teamComponent;

        public FrenzyOnKillEffect(Unit owner, EffectTrigger trigger, int priority, int durationTurns)
        {
            Owner = owner;
            Trigger = trigger;
            Priority = priority;
            RemainingDuration = durationTurns;

            teamComponent = owner != null ? owner.GetComponent<ITeamComponent>() : null;
        }

        public void TickDuration(DurationTickContext context)
        {
            if (context.Source != DurationTickSource.TurnEnd)
                return;

            if (RemainingDuration > 0)
                RemainingDuration--;
        }

        public bool CanApply(EffectContext context)
        {
            return Owner != null &&
                   Owner.IsAlive &&
                   context != null &&
                   context.AffectedTiles != null &&
                   context.AffectedTiles.Count > 0;
        }

        public void Apply(EffectContext context)
        {
            if (!CanApply(context))
                return;

            bool killedTarget = false;

            foreach (var tile in context.AffectedTiles)
            {
                if (tile == null)
                    continue;

                // ValidTiles에는 공격 시점에 실제로 타겟이 있던 타일만 들어옵니다.
                // 공격 후에 해당 타일에서 더 이상 피해를 받을 대상이 없다면
                // (GetDamageableTarget()이 null), 이번 공격으로 타겟이 제거되었다고 해석합니다.
                var targetHealth = tile.GetDamageableTarget();
                if (targetHealth == null)
                {
                    killedTarget = true;
                    break;
                }
            }

            if (!killedTarget)
                return;

            Owner.MarkFrenzyEligibleForNextAction();
            Debug.Log($"[FrenzyOnKillEffect] {Owner?.name} gained Frenzy extra action by killing an enemy");
        }

        private bool IsEnemy(GameObject target)
        {
            if (teamComponent == null) return true;
            var targetTeam = target.GetComponent<ITeamComponent>();
            return targetTeam == null ? true : teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
        }
    }
}
