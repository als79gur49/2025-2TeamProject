using System.Collections.Generic;
using Game.Core;
using Game.Core.Effects;
using Game.Components;
using Game.Interfaces;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 배치 시 owner 기준 TeamRelation(Ally/Enemy)에 해당하는 유닛 n명에게
    /// StunStatusEffect를 부여하여 일정 턴 동안 스턴 상태로 만드는 소스 이펙트입니다.
    /// 자신은 실제 행동 제약을 적용하지 않고, 대상 유닛들에게 스턴 이펙트를 추가하는 역할만 수행합니다.
    /// </summary>
    public class DeployTeamStunSourceEffect : IEffect
    {
        public string EffectName => "배치: 팀 다중 스턴";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int stunTurns;
        private readonly int maxTargets;
        private readonly TeamRelation targetRelation;

        private readonly IGridManager gridManager;

        public DeployTeamStunSourceEffect(
            Unit owner,
            int stunTurns,
            int maxTargets,
            TeamRelation targetRelation,
            EffectTrigger trigger,
            int priority,
            int durationTurns)
        {
            Owner = owner;
            this.stunTurns = Mathf.Max(1, stunTurns);
            this.maxTargets = Mathf.Max(0, maxTargets);
            this.targetRelation = targetRelation;
            Trigger = trigger;
            Priority = priority;
            RemainingDuration = durationTurns;

            gridManager = ServiceLocator.Get<IGridManager>();
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
                   gridManager != null &&
                   maxTargets > 0 &&
                   stunTurns > 0;
        }

        public void Apply(EffectContext context)
        {
            if (!CanApply(context))
            {
                return;
            }

            var targetTeam = ResolveTargetTeam();
            if (targetTeam == TeamType.None)
            {
                return;
            }

            var gridSize = gridManager.GridSize;
            if (gridSize.x <= 0 || gridSize.y <= 0)
            {
                return;
            }

            Vector2Int ownerPos;
            if (Owner.CurrentTile != null)
            {
                ownerPos = new Vector2Int(Owner.CurrentTile.X, Owner.CurrentTile.Y);
            }
            else
            {
                ownerPos = gridManager.GetUnitPosition(Owner.gameObject);
            }

            var candidates = new List<(Unit unit, Vector2Int pos)>();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var unitGO = gridManager.GetUnitAtPosition(pos);
                    if (unitGO == null) continue;

                    // 자기 자신은 스턴 대상에서 제외
                    if (unitGO == Owner.gameObject) continue;

                    var team = unitGO.GetComponent<ITeamComponent>();
                    if (team == null) continue;

                    if (team.Team != targetTeam) continue;

                    var targetUnit = unitGO.GetComponent<Unit>();
                    if (targetUnit == null || !targetUnit.IsAlive) continue;

                    candidates.Add((targetUnit, pos));
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            // Owner와의 맨해튼 거리 기준으로 가까운 유닛부터 선택
            candidates.Sort((a, b) =>
            {
                int distA = Mathf.Abs(a.pos.x - ownerPos.x) + Mathf.Abs(a.pos.y - ownerPos.y);
                int distB = Mathf.Abs(b.pos.x - ownerPos.x) + Mathf.Abs(b.pos.y - ownerPos.y);
                return distA.CompareTo(distB);
            });

            int appliedCount = 0;
            foreach (var (unit, _) in candidates)
            {
                if (appliedCount >= maxTargets)
                    break;

                var stunEffect = new StunStatusEffect(
                    owner: unit,
                    stunTurns: stunTurns,
                    trigger: EffectTrigger.OnTurnStart,
                    priority: Priority,
                    durationTurns: stunTurns
                );

                unit.AddEffect(stunEffect);
                appliedCount++;
            }

            Debug.Log($"[DeployTeamStunSourceEffect] {Owner.name} stunned {appliedCount} unit(s) of team {targetTeam} (relation {targetRelation}) for {stunTurns} turn(s)");
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

