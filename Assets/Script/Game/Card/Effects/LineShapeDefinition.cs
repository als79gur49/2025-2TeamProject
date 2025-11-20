using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// 행/열 기반 직선 범위를 정의하는 Shape
    /// - FullLine: 같은 행/열의 모든 타일
    /// - RangeAroundCenter: 중심 기준 좌우/상하 Range만큼의 타일
    /// </summary>
    public enum LineAxis
    {
        /// <summary>
        /// 같은 x, y를 변화시키는 가로 라인 (행)
        /// </summary>
        Row,

        /// <summary>
        /// 같은 y, x를 변화시키는 세로 라인 (열)
        /// </summary>
        Column
    }

    public enum LineRangeMode
    {
        /// <summary>
        /// 해당 행/열의 모든 타일
        /// </summary>
        FullLine,

        /// <summary>
        /// 중심 기준 양쪽 Range만큼의 타일
        /// </summary>
        RangeAroundCenter
    }

    [CreateAssetMenu(menuName = "Game/Effects/Shapes/Line")]
    public class LineShapeDefinition : AreaShapeDefinition
    {
        [SerializeField] private LineAxis axis = LineAxis.Row;
        [SerializeField] private LineRangeMode rangeMode = LineRangeMode.FullLine;
        [SerializeField] [Min(0)] private int range = 0;

        public LineAxis Axis => axis;
        public LineRangeMode RangeMode => rangeMode;
        public int Range => range;

        public override IEnumerable<Tile> GetTiles(Vector2Int center, IGridController gridController)
        {
            var tiles = new List<Tile>();

            if (gridController == null)
            {
                return tiles;
            }

            var gridSize = gridController.GridSize;

            if (rangeMode == LineRangeMode.FullLine)
            {
                if (axis == LineAxis.Row)
                {
                    int row = center.x;
                    if (row < 0 || row >= gridSize.x)
                    {
                        return tiles;
                    }

                    for (int y = 0; y < gridSize.y; y++)
                    {
                        var pos = new Vector2Int(row, y);
                        if (!gridController.IsValidPosition(pos)) continue;

                        var tile = gridController.GetTileAtPosition(pos);
                        if (tile != null)
                        {
                            tiles.Add(tile);
                        }
                    }
                }
                else // Column
                {
                    int column = center.y;
                    if (column < 0 || column >= gridSize.y)
                    {
                        return tiles;
                    }

                    for (int x = 0; x < gridSize.x; x++)
                    {
                        var pos = new Vector2Int(x, column);
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

            // RangeAroundCenter
            if (range <= 0)
            {
                var tile = gridController.GetTileAtPosition(center);
                if (tile != null)
                {
                    tiles.Add(tile);
                }

                return tiles;
            }

            if (axis == LineAxis.Row)
            {
                int row = center.x;
                if (row < 0 || row >= gridSize.x)
                {
                    return tiles;
                }

                for (int dy = -range; dy <= range; dy++)
                {
                    int y = center.y + dy;
                    if (y < 0 || y >= gridSize.y) continue;

                    var pos = new Vector2Int(row, y);
                    if (!gridController.IsValidPosition(pos)) continue;

                    var tile = gridController.GetTileAtPosition(pos);
                    if (tile != null)
                    {
                        tiles.Add(tile);
                    }
                }
            }
            else // Column
            {
                int column = center.y;
                if (column < 0 || column >= gridSize.y)
                {
                    return tiles;
                }

                for (int dx = -range; dx <= range; dx++)
                {
                    int x = center.x + dx;
                    if (x < 0 || x >= gridSize.x) continue;

                    var pos = new Vector2Int(x, column);
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

        public override bool IsInArea(Vector2Int center, Vector2Int tilePos, IGridController gridController)
        {
            if (gridController == null)
            {
                return false;
            }

            if (!gridController.IsValidPosition(tilePos))
            {
                return false;
            }

            if (rangeMode == LineRangeMode.FullLine)
            {
                if (axis == LineAxis.Row)
                {
                    return tilePos.x == center.x;
                }

                return tilePos.y == center.y;
            }

            // RangeAroundCenter
            if (range <= 0)
            {
                return tilePos == center;
            }

            if (axis == LineAxis.Row)
            {
                if (tilePos.x != center.x)
                {
                    return false;
                }

                int deltaY = Mathf.Abs(tilePos.y - center.y);
                return deltaY <= range;
            }
            else
            {
                if (tilePos.y != center.y)
                {
                    return false;
                }

                int deltaX = Mathf.Abs(tilePos.x - center.x);
                return deltaX <= range;
            }
        }

        public override string GetRangeDescription()
        {
            if (rangeMode == LineRangeMode.FullLine)
            {
                return axis == LineAxis.Row
                    ? "같은 행의 모든 타일"
                    : "같은 열의 모든 타일";
            }

            if (range <= 0)
            {
                return "해당 타일만";
            }

            if (axis == LineAxis.Row)
            {
                return $"같은 행에서 좌우 {range}칸";
            }

            return $"같은 열에서 상하 {range}칸";
        }
    }
}

