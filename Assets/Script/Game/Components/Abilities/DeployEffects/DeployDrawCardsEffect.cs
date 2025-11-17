using Game.Core;
using Game.Core.Effects;
using Game.Interfaces;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 배치 시 특정 팀이 카드를 드로우하게 만드는 유닛 이펙트입니다.
    /// 대상 팀은 유닛의 팀을 기준으로 한 TeamRelation(Ally/Enemy 등)으로 결정됩니다.
    /// </summary>
    public class DeployDrawCardsEffect : IEffect
    {
        public string EffectName => "배치: 카드 드로우";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int drawCount;
        private readonly TeamRelation targetRelation;

        public DeployDrawCardsEffect(
            Unit owner,
            int drawCount,
            TeamRelation targetRelation,
            EffectTrigger trigger,
            int priority,
            int durationTurns)
        {
            Owner = owner;
            this.drawCount = drawCount;
            this.targetRelation = targetRelation;
            Trigger = trigger;
            Priority = priority;
            RemainingDuration = durationTurns;
        }

        public void TickDuration()
        {
            if (RemainingDuration > 0)
            {
                RemainingDuration--;
            }
        }

        public bool CanApply(EffectContext context)
        {
            return Owner != null &&
                   Owner.IsAlive &&
                   drawCount > 0;
        }

        public void Apply(EffectContext context)
        {
            var cardServiceManager = ServiceLocator.Get<ICardServiceManager>();
            if (cardServiceManager == null)
            {
                Debug.LogError("[DeployDrawCardsEffect] ICardServiceManager not found in ServiceLocator");
                return;
            }

            var targetTeam = ResolveTargetTeam();
            if (targetTeam == TeamType.None)
            {
                return;
            }

            cardServiceManager.DrawCardsForTeam(targetTeam, drawCount);

            Debug.Log($"[DeployDrawCardsEffect] {Owner.name} caused team {targetTeam} to draw {drawCount} card(s)");
        }

        private TeamType ResolveTargetTeam()
        {
            if (Owner == null)
            {
                return TeamType.None;
            }

            var teamComponent = Owner.GetComponent<ITeamComponent>();
            TeamType ownerTeam;

            if (teamComponent != null)
            {
                ownerTeam = teamComponent.Team;
            }
            else
            {
                ownerTeam = Owner.IsPlayerUnit ? TeamType.Player : TeamType.Enemy;
            }

            switch (targetRelation)
            {
                case TeamRelation.Self:
                case TeamRelation.Ally:
                    return ownerTeam;
                case TeamRelation.Enemy:
                    if (ownerTeam == TeamType.Player) return TeamType.Enemy;
                    if (ownerTeam == TeamType.Enemy) return TeamType.Player;
                    return TeamType.None;
                default:
                    return TeamType.None;
            }
        }
    }
}

