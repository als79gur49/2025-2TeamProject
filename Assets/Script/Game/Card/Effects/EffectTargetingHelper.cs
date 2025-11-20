using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Interfaces;
using Game.Services;

namespace Game.Card.Effects
{
    /// <summary>
    /// 타일 기반 통합 타겟팅 시스템
    /// 핵심 원칙: "모든 타겟팅은 타일을 반환하고, 효과는 타일에 적용된다"
    /// VFX 시스템: 타일 위치(Vector3Int)를 VFX 재생 좌표로 사용
    /// </summary>
    public static class EffectTargetingHelper
    {
        #region Public API - 타일 기반 VFX 통합

        /// <summary>
        /// [타일 기반] 새로운 EffectDefinition 기반으로 대상 타일들을 반환
        /// AreaShape + TargetFilter 조합을 사용합니다.
        /// </summary>
        public static List<Tile> GetTargetTiles(
            Vector2Int center,
            EffectDefinition definition,
            GameContext context)
        {
            var result = new List<Tile>();

            if (context?.GridController == null)
            {
                Debug.LogError("[EffectTargeting] GridController null");
                return result;
            }

            if (definition == null)
            {
                Debug.LogError("[EffectTargeting] EffectDefinition null");
                return result;
            }

            if (definition.TargetScope == EffectTargetScope.Global)
            {
                // 전역 효과는 타일 기반 타겟을 사용하지 않습니다.
                return result;
            }

            if (definition.AreaShape == null)
            {
                Debug.LogWarning("[EffectTargeting] AreaShape가 null입니다. 타겟 타일이 없습니다.");
                return result;
            }

            var tilesInRange = definition.AreaShape.GetTiles(center, context.GridController);

            if (definition.TargetFilter == null)
            {
                result.AddRange(tilesInRange);
                return result;
            }

            foreach (var tile in tilesInRange)
            {
                if (tile == null) continue;
                var unit = tile.OccupyingUnit;

                if (definition.TargetFilter.Matches(tile, unit, context))
                {
                    result.Add(tile);
                }
            }

            return result;
        }

        /// <summary>
        /// [타일 기반] 타일 리스트를 VFX 재생용 월드 좌표 리스트로 변환
        /// </summary>
        /// <param name="tiles">타겟 타일 리스트</param>
        /// <returns>VFX 재생에 사용할 Vector3 위치 리스트</returns>
        public static List<Vector3> TilesToWorldPositions(List<Tile> tiles)
        {
            return tiles.Select(t => t.transform.position).ToList();
        }

        /// <summary>
        /// [타일 기반] 타일 리스트를 그리드 좌표 리스트로 변환
        /// </summary>
        /// <param name="tiles">타겟 타일 리스트</param>
        /// <returns>Grid 좌표 리스트 (Vector3Int)</returns>
        public static List<Vector3Int> TilesToGridPositions(List<Tile> tiles)
        {
            return tiles.Select(t =>
            {
                var pos = t.GetGridPosition();
                return new Vector3Int(pos.x, pos.y, 0);
            }).ToList();
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// 중심 위치에서 지정된 범위 내의 모든 타일 반환 (동기)
        /// 맨해튼 거리 기반 다이아몬드 형태 범위 계산
        /// </summary>
        private static List<Tile> GetTilesInRange(
            Vector2Int center,
            int range,
            IGridController gridController)
        {
            var tiles = new List<Tile>();

            // Range 0: 단일 타일
            if (range == 0)
            {
                var tile = gridController.GetTileAtPosition(center);
                if (tile != null) tiles.Add(tile);
                return tiles;
            }

            // Range 1+: 맨해튼 거리 기반 다이아몬드 형태
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    // 맨해튼 거리 계산 (문제 2 해결: 대각선 제외)
                    int manhattanDistance = Mathf.Abs(x) + Mathf.Abs(y);
                    if (manhattanDistance > range) continue;

                    var pos = center + new Vector2Int(x, y);
                    var tile = gridController.GetTileAtPosition(pos);

                    if (tile != null)
                    {
                        tiles.Add(tile);
                    }
                }
            }

            return tiles;
        }

        #endregion
    }
}
