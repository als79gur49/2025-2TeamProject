using Game.Core;
using Game.Core.Effects;
using Game.Components;
using Game.Interfaces;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 배치 시 owner가 위치한 열(x축)에 있는 아군 또는 적군 전체에게 TeamStatBuffEffect를 부여하는 소스 이펙트입니다.
    /// 자신은 실제 스탯을 변경하지 않고, 대상 유닛들에게 버프 이펙트를 추가하는 역할만 수행합니다.
    /// </summary>
    public class DeployColumnStatSourceEffect : IEffect
    {
        public string EffectName => "배치: 열 스탯 버프";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int healthDelta;
        private readonly int attackDelta;
        private readonly int movementDelta;
        private readonly int buffDurationTurns;
        private readonly TeamRelation targetRelation;

        private readonly string buffVfxIdOverride;

        private readonly IGridManager gridManager;

        public DeployColumnStatSourceEffect(
            Unit owner,
            int healthDelta,
            int attackDelta,
            int movementDelta,
            TeamRelation targetRelation,
            int buffDurationTurns,
            EffectTrigger trigger,
            int priority,
            int durationTurns,
            string buffVfxIdOverride = null)
        {
            Owner = owner;
            this.healthDelta = healthDelta;
            this.attackDelta = attackDelta;
            this.movementDelta = movementDelta;
            this.buffDurationTurns = buffDurationTurns;
            this.targetRelation = targetRelation;
            this.buffVfxIdOverride = buffVfxIdOverride;
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

            Vector2Int ownerPos;
            if (Owner.CurrentTile != null)
            {
                ownerPos = new Vector2Int(Owner.CurrentTile.X, Owner.CurrentTile.Y);
            }
            else
            {
                ownerPos = gridManager.GetUnitPosition(Owner.gameObject);
            }

            // 시스템 좌표계: (x, y)에서 x는 위/아래, y는 좌/우이므로
            // 세로 방향(위/아래) 유닛을 찾기 위해 y를 고정하고 x를 변화시킵니다.
            int fixedY = ownerPos.y;
            if (fixedY < 0 || fixedY >= gridSize.y)
            {
                return;
            }

            int appliedCount = 0;

            for (int x = 0; x < gridSize.x; x++)
            {
                var pos = new Vector2Int(x, fixedY);
                var unitGO = gridManager.GetUnitAtPosition(pos);
                if (unitGO == null) continue;

                // 자기 자신 포함 여부는 선택 사항: 여기서는 포함하지 않음
                if (unitGO == Owner.gameObject) continue;

                var team = unitGO.GetComponent<ITeamComponent>();
                if (team == null) continue;

                if (team.Team != targetTeam) continue;

                var targetUnit = unitGO.GetComponent<Unit>();
                if (targetUnit == null || !targetUnit.IsAlive) continue;
                var buff = new TeamStatBuffEffect(
                    owner: targetUnit,
                    healthDelta: healthDelta,
                    attackDelta: attackDelta,
                    movementDelta: movementDelta,
                    durationTurns: buffDurationTurns,
                    priority: Priority,
                    revertOnExpire: true,
                    persistentVfxId: buffVfxIdOverride,
                    anchor: VFXAnchorType.Default
                );

                targetUnit.AddEffect(buff);
                appliedCount++;
            }

            Debug.Log($"[DeployColumnStatSourceEffect] {Owner.name} applied column buff to {appliedCount} unit(s) of team {targetTeam} (relation {targetRelation}) on fixedY {fixedY}");
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
