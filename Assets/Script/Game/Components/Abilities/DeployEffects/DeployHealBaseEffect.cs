using Game;
using Game.Core;
using Game.Core.Effects;
using Game.Interfaces;
using Game.Services;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 배치 시 특정 팀의 Base를 회복시키는 유닛 이펙트입니다.
    /// 대상 팀은 유닛의 팀을 기준으로 한 TeamRelation(Ally/Enemy 등)으로 결정됩니다.
    /// </summary>
    public class DeployHealBaseEffect : IEffect
    {
        public string EffectName => "배치: 베이스 회복";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int healAmount;
        private readonly TeamRelation targetRelation;

        public DeployHealBaseEffect(
            Unit owner,
            int healAmount,
            TeamRelation targetRelation,
            EffectTrigger trigger,
            int priority,
            int durationTurns)
        {
            Owner = owner;
            this.healAmount = healAmount;
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
                   healAmount > 0;
        }

        public void Apply(EffectContext context)
        {
            var baseManager = ServiceLocator.Get<IBaseManager>();
            if (baseManager == null)
            {
                Debug.LogError("[DeployHealBaseEffect] IBaseManager not found in ServiceLocator");
                return;
            }

            var targetTeam = ResolveTargetTeam();
            if (targetTeam == TeamType.None)
            {
                return;
            }

            Base targetBase = null;
            if (targetTeam == TeamType.Player)
            {
                targetBase = baseManager.PlayerBase;
            }
            else if (targetTeam == TeamType.Enemy)
            {
                targetBase = baseManager.EnemyBase;
            }

            if (targetBase == null || targetBase.HealthComponent == null)
            {
                return;
            }

            var health = targetBase.HealthComponent;
            if (!health.CanHeal(healAmount))
            {
                return;
            }

            health.Heal(healAmount);

            Debug.Log($"[DeployHealBaseEffect] {Owner.name} healed {targetTeam} base for {healAmount}");
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

