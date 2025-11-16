using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;

namespace Game.Services.Modifiers.TargetSelectors
{
    /// <summary>
    /// 원거리 타겟 선택기
    /// 직선 방향으로 지정된 범위 내의 적을 찾음
    /// </summary>
    public class RangedTargetSelector : ITargetSelector
    {
        private readonly IGridManager gridManager;

        public RangedTargetSelector(IGridManager gridManager)
        {
            this.gridManager = gridManager;
        }

        public List<Tile> FindTargets(Vector2Int origin, TargetingParams parameters, ITeamComponent teamComponent)
        {
            var foundTargets = new List<Tile>();
            var gridController = gridManager?.GetGridController();
            if (gridController == null || teamComponent == null) return foundTargets;

            int direction = teamComponent.Team == TeamType.Player ? 1 : -1;

            for (int i = 1; i <= parameters.Range; i++)
            {
                Vector2Int checkPos = new Vector2Int(origin.x, origin.y + (direction * i));
                var tile = gridController.GetTileAtPosition(checkPos);
                if (tile == null) continue;

                bool hasUnit = tile.OccupyingUnit != null && tile.OccupyingUnit.IsAlive;
                bool hasBase = tile.OccupyingBase != null && tile.OccupyingBase.IsAlive;

                if (hasUnit || hasBase)
                {
                    var targetObj = hasUnit ? tile.OccupyingUnit.gameObject : tile.OccupyingBase.gameObject;

                    // 타겟 타입 체크
                    bool isValidTargetType = (parameters.TargetType == TargetType.Unit && hasUnit) ||
                                              (parameters.TargetType == TargetType.Base && hasBase) ||
                                              (parameters.TargetType == TargetType.Both);

                    if (isValidTargetType && IsValidRelation(targetObj, teamComponent, parameters.TargetRelation))
                    {
                        foundTargets.Add(tile);

                        // Piercing이 아니면 첫 타겟에서 멈춤
                        if (!parameters.Piercing)
                            break;
                    }
                    else if (!parameters.Piercing)
                    {
                        // 아군이 막고 있으면 관통 불가 시 중단
                        break;
                    }
                }
            }

            return parameters.Piercing ? foundTargets : foundTargets.Take(1).ToList();
        }

        private bool IsValidRelation(GameObject target, ITeamComponent teamComponent, TeamRelation desiredRelation)
        {
            if (target == null) return false;

            var targetTeam = target.GetComponent<ITeamComponent>();
            if (targetTeam == null) return desiredRelation == TeamRelation.Enemy; // 팀 없으면 적으로 간주

            var actualRelation = teamComponent.GetRelationTo(targetTeam);
            return actualRelation == desiredRelation;
        }
    }
}
