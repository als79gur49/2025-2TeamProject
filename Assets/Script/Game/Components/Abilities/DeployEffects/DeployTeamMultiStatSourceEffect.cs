using System.Collections.Generic;
using Game.Core;
using Game.Core.Effects;
using Game.Components;
using Game.Interfaces;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 배치 시 owner 기준 TeamRelation(Ally/Enemy)에 해당하는 유닛 n명에게 TeamStatBuffEffect를 부여하는 소스 이펙트입니다.
    /// 자신은 실제 스탯을 변경하지 않고, 대상 유닛들에게 버프 이펙트를 추가하는 역할만 수행합니다.
    /// </summary>
    public class DeployTeamMultiStatSourceEffect : IEffect
    {
        public string EffectName => "배치: 팀 다중 스탯 버프";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int healthDelta;
        private readonly int attackDelta;
        private readonly int movementDelta;
        private readonly int maxTargets;
        private readonly int buffDurationTurns;
        private readonly TeamRelation targetRelation;

        private readonly IGridManager gridManager;

        public DeployTeamMultiStatSourceEffect(
            Unit owner,
            int healthDelta,
            int attackDelta,
            int movementDelta,
            int maxTargets,
            TeamRelation targetRelation,
            int buffDurationTurns,
            EffectTrigger trigger,
            int priority,
            int durationTurns)
        {
            Owner = owner;
            this.healthDelta = healthDelta;
            this.attackDelta = attackDelta;
            this.movementDelta = movementDelta;
            this.maxTargets = Mathf.Max(0, maxTargets);
            this.buffDurationTurns = buffDurationTurns;
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
                   (healthDelta != 0 || attackDelta != 0 || movementDelta != 0);
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

            // Owner 위치 기준으로 가장 가까운 유닛부터 선택
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

                    // 자기 자신 포함 여부는 선택 사항: 여기서는 포함하지 않음
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

            // 거리 기준 정렬 (가까운 유닛 우선)
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

                var buff = new TeamStatBuffEffect(
                    owner: unit,
                    healthDelta: healthDelta,
                    attackDelta: attackDelta,
                    movementDelta: movementDelta,
                    durationTurns: buffDurationTurns,
                    priority: Priority
                );

                unit.AddEffect(buff);
                appliedCount++;
            }

            Debug.Log($"[DeployTeamMultiStatSourceEffect] {Owner.name} applied team buff to {appliedCount} unit(s) of team {targetTeam} (relation {targetRelation})");
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
