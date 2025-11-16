using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;

namespace Game.Services.Modifiers.TargetSelectors
{
    /// <summary>
    /// 이동 타겟 선택기
    /// 이동 가능한 빈 타일을 찾음
    /// </summary>
    public class MovementTargetSelector : ITargetSelector
    {
        private readonly IGridManager gridManager;

        public MovementTargetSelector(IGridManager gridManager)
        {
            this.gridManager = gridManager;
        }

        public List<Tile> FindTargets(Vector2Int origin, TargetingParams parameters, ITeamComponent teamComponent)
        {
            var movableTiles = new List<Tile>();
            var gridController = gridManager?.GetGridController();
            if (gridController == null) return movableTiles;

            // 무제한 범위 (부스터) 처리
            if (parameters.Range >= 99)
            {
                return FindAllWalkableTiles(origin, gridController);
            }

            if (teamComponent == null)
                return movableTiles;

            // 팀에 따라 진행 방향 결정
            int direction = 0;
            if (teamComponent.Team == TeamType.Player || teamComponent.Team == TeamType.Ally)
            {
                direction = 1;  // +Y 방향
            }
            else if (teamComponent.Team == TeamType.Enemy)
            {
                direction = -1; // -Y 방향
            }

            if (direction == 0)
                return movableTiles;

            // 한 방향으로만 직선 탐색, 막히면 중단
            for (int distance = 1; distance <= parameters.Range; distance++)
            {
                Vector2Int checkPos = new Vector2Int(origin.x, origin.y + (direction * distance));
                var tile = gridController.GetTileAtPosition(checkPos);

                if (tile == null)
                    break;

                // 이동 불가능(점유/기지 등)하면 그 뒤로는 탐색하지 않음
                if (!IsWalkable(tile))
                    break;

                movableTiles.Add(tile);
            }

            return movableTiles;
        }

        private List<Tile> FindAllWalkableTiles(Vector2Int origin, IGridController gridController)
        {
            var tiles = new List<Tile>();
            var gridSize = gridManager.GridSize;

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (pos == origin) continue;

                    var tile = gridController.GetTileAtPosition(pos);
                    if (IsWalkable(tile))
                    {
                        tiles.Add(tile);
                    }
                }
            }

            return tiles;
        }

        private bool IsWalkable(Tile tile)
        {
            return tile != null &&
                   tile.OccupyingUnit == null &&
                   tile.OccupyingBase == null;
        }

    }
}
