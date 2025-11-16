using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;

namespace Game.Services.Modifiers.TargetSelectors
{
    /// <summary>
    /// 넥서스 타겟 선택기
    /// 적 넥서스만 찾음 (스나이퍼용)
    /// </summary>
    public class NexusTargetSelector : ITargetSelector
    {
        private readonly IGridManager gridManager;

        public NexusTargetSelector(IGridManager gridManager)
        {
            this.gridManager = gridManager;
        }

        public List<Tile> FindTargets(Vector2Int origin, TargetingParams parameters, ITeamComponent teamComponent)
        {
            var foundTargets = new List<Tile>();
            var gridController = gridManager?.GetGridController();
            if (gridController == null || teamComponent == null) return foundTargets;

            var gridSize = gridManager.GridSize;

            // 스나이퍼는 자신의 행(row)에서만 넥서스를 탐색
            int actorRow = origin.x;
            if (actorRow < 0 || actorRow >= gridSize.x) return foundTargets;

            for (int y = 0; y < gridSize.y; y++)
            {
                var tile = gridController.GetTileAtPosition(new Vector2Int(actorRow, y));
                if (tile?.OccupyingBase == null || !tile.OccupyingBase.IsAlive) continue;

                // 적 넥서스인지 확인 (팀당 베이스가 하나뿐이므로 IsAlive + 팀 관계 체크로 충분)
                if (IsValidRelation(tile.OccupyingBase.gameObject, teamComponent, parameters.TargetRelation))
                {
                    foundTargets.Add(tile);
                    break; // 같은 행에서 첫 번째 넥서스 타일만 타겟
                }
            }

            return foundTargets;
        }

        private bool IsValidRelation(GameObject target, ITeamComponent teamComponent, TeamRelation desiredRelation)
        {
            if (target == null) return false;

            var targetTeam = target.GetComponent<ITeamComponent>();
            if (targetTeam == null) return desiredRelation == TeamRelation.Enemy;

            var actualRelation = teamComponent.GetRelationTo(targetTeam);
            return actualRelation == desiredRelation;
        }
    }
}
