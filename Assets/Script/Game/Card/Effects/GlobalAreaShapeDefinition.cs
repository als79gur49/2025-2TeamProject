using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// 맵 전체의 모든 타일을 반환하는 Shape
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Shapes/Global")]
    public class GlobalAreaShapeDefinition : AreaShapeDefinition
    {
        /// <summary>
        /// GlobalAreaShape는 중심 좌표와 무관하게 항상 맵 전체 타일을 반환합니다.
        /// </summary>
        public override bool IsCenterIndependent => true;

        public override IEnumerable<Tile> GetTiles(Vector2Int center, IGridController gridController)
        {
            var tiles = new List<Tile>();

            if (gridController == null)
            {
                return tiles;
            }

            var gridSize = gridController.GridSize;

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (!gridController.IsValidPosition(pos)) continue;

                    var tile = gridController.GetTileAtPosition(pos);
                    if (tile != null)
                    {
                        tiles.Add(tile);
                    }
                }
            }

            return tiles;
        }

        /// <summary>
        /// 맵 전체를 대상으로 하므로, 유효한 타일 위치이면 항상 포함됩니다.
        /// </summary>
        public override bool IsInArea(Vector2Int center, Vector2Int tilePos, IGridController gridController)
        {
            if (gridController == null)
            {
                return false;
            }

            return gridController.IsValidPosition(tilePos);
        }

        public override string GetRangeDescription()
        {
            return "맵 전체";
        }
    }
}
