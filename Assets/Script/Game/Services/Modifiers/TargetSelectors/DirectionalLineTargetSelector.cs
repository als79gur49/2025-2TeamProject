using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;

namespace Game.Services.Modifiers.TargetSelectors
{
    /// <summary>
    /// 방향 플래그 기반 직선/대각선 타겟 선택기
    /// AttackDirectionFlags + TeamType을 이용해
    /// Self/Ally는 +X(오른쪽), Enemy는 -X(왼쪽) 방향으로 탐색
    /// </summary>
    public class DirectionalLineTargetSelector : ITargetSelector
    {
        private readonly IGridManager gridManager;

        public DirectionalLineTargetSelector(IGridManager gridManager)
        {
            this.gridManager = gridManager;
        }

        public List<Tile> FindTargets(Vector2Int origin, TargetingParams parameters, ITeamComponent teamComponent)
        {
            var foundTargets = new List<Tile>();
            var gridController = gridManager?.GetGridController();
            if (gridController == null || teamComponent == null)
                return foundTargets;

            var directions = GetDirectionVectors(teamComponent.Team, parameters.Directions);
            var gridSize = gridManager.GridSize;
            int maxDistance = parameters.Range >= 99
                ? Mathf.Max(gridSize.x, gridSize.y)
                : parameters.Range;

            foreach (var dir in directions)
            {
                for (int distance = 1; distance <= maxDistance; distance++)
                {
                    Vector2Int checkPos = new Vector2Int(
                        origin.x + dir.x * distance,
                        origin.y + dir.y * distance
                    );

                    var tile = gridController.GetTileAtPosition(checkPos);
                    if (tile == null)
                        break;

                    bool hasUnit = tile.OccupyingUnit != null && tile.OccupyingUnit.IsAlive;
                    bool hasBase = tile.OccupyingBase != null && tile.OccupyingBase.IsAlive;

                    if (hasUnit || hasBase)
                    {
                        var targetObj = hasUnit ? tile.OccupyingUnit.gameObject : tile.OccupyingBase.gameObject;

                        bool isValidTargetType =
                            (parameters.TargetType == TargetType.Unit && hasUnit) ||
                            (parameters.TargetType == TargetType.Base && hasBase) ||
                            (parameters.TargetType == TargetType.Both);

                        if (isValidTargetType && IsValidRelation(targetObj, teamComponent, parameters.TargetRelation))
                        {
                            foundTargets.Add(tile);

                            if (!parameters.Piercing)
                                break;
                        }
                        else if (!parameters.Piercing)
                        {
                            // 아군 또는 유효하지 않은 타겟이 막고 있고 관통이 불가능하면 중단
                            break;
                        }
                    }
                }
            }

            return foundTargets;
        }

        private IEnumerable<Vector2Int> GetDirectionVectors(TeamType team, AttackDirectionFlags flags)
        {
            // 기존 Ranged/Movement 셀렉터와 동일하게
            // 전진 방향은 Y축 기준으로 결정
            int forwardY;
            if (team == TeamType.Enemy)
            {
                forwardY = -1;
            }
            else
            {
                // Player, Ally, Neutral, None 모두 +Y를 기본 전진 방향으로 사용
                forwardY = 1;
            }

            // Forward: (0, forwardY)
            if (flags.HasFlag(AttackDirectionFlags.Forward))
                yield return new Vector2Int(0, forwardY);

            // DiagonalUp: (+1, forwardY)  → 상단 대각
            if (flags.HasFlag(AttackDirectionFlags.DiagonalUp))
                yield return new Vector2Int(+1, forwardY);

            // DiagonalDown: (-1, forwardY) → 하단 대각
            if (flags.HasFlag(AttackDirectionFlags.DiagonalDown))
                yield return new Vector2Int(-1, forwardY);
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
