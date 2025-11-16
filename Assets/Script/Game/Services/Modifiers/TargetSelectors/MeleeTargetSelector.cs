using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;

namespace Game.Services.Modifiers.TargetSelectors
{
    /// <summary>
    /// 근접 타겟 선택기
    /// 바로 인접한 적만 찾음
    /// </summary>
    public class MeleeTargetSelector : ITargetSelector
    {
        private readonly IGridManager gridManager;

        public MeleeTargetSelector(IGridManager gridManager)
        {
            this.gridManager = gridManager;
        }

        public List<Tile> FindTargets(Vector2Int origin, TargetingParams parameters, ITeamComponent teamComponent)
        {
            var foundTargets = new List<Tile>();
            var gridController = gridManager?.GetGridController();
            if (gridController == null || teamComponent == null) return foundTargets;

            int direction = teamComponent.Team == TeamType.Player ? 1 : -1;
            Vector2Int checkPos = new Vector2Int(origin.x, origin.y + direction);

            var tile = gridController.GetTileAtPosition(checkPos);
            if (tile == null) return foundTargets;

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
