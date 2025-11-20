using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// 중심에서 시작해 한 방향으로 우선 확장하는 라인 Shape
    /// - Row 축: 중심 → 우(+y) → 좌(-y) → 우(+2) → 좌(-2) 순서
    /// - Column 축: 중심 → 위(+x) → 아래(-x) → 위(+2) → 아래(-2) 순서
    /// TileCount 값이 "최대 타일 개수"를 의미합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Shapes/SequentialLine")]
    public class SequentialLineShapeDefinition : AreaShapeDefinition
    {
        [SerializeField] private LineAxis axis = LineAxis.Row;
        [SerializeField] [Min(1)] private int tileCount = 1;

        public LineAxis Axis => axis;
        public int TileCount => tileCount;

        public override IEnumerable<Tile> GetTiles(Vector2Int center, IGridController gridController)
        {
            var tiles = new List<Tile>();

            if (gridController == null)
            {
                return tiles;
            }

            if (tileCount <= 0)
            {
                return tiles;
            }

            var gridSize = gridController.GridSize;

            // 중심 타일 우선 추가
            int added = 0;

            if (gridController.IsValidPosition(center))
            {
                var centerTile = gridController.GetTileAtPosition(center);
                if (centerTile != null)
                {
                    tiles.Add(centerTile);
                    added++;
                }
            }

            if (added >= tileCount)
            {
                return tiles;
            }

            if (axis == LineAxis.Row)
            {
                // 같은 행에서 우 → 좌 순서로 확장
                int row = center.x;
                if (row < 0 || row >= gridSize.x)
                {
                    return tiles;
                }

                for (int offset = 1; offset < gridSize.y && added < tileCount; offset++)
                {
                    // 우측(+y)
                    Vector2Int rightPos = new Vector2Int(row, center.y + offset);
                    if (gridController.IsValidPosition(rightPos))
                    {
                        var rightTile = gridController.GetTileAtPosition(rightPos);
                        if (rightTile != null)
                        {
                            tiles.Add(rightTile);
                            added++;
                            if (added >= tileCount) break;
                        }
                    }

                    if (added >= tileCount)
                    {
                        break;
                    }

                    // 좌측(-y)
                    Vector2Int leftPos = new Vector2Int(row, center.y - offset);
                    if (gridController.IsValidPosition(leftPos))
                    {
                        var leftTile = gridController.GetTileAtPosition(leftPos);
                        if (leftTile != null)
                        {
                            tiles.Add(leftTile);
                            added++;
                        }
                    }
                }
            }
            else // Column
            {
                // 같은 열에서 위 → 아래 순서로 확장
                int column = center.y;
                if (column < 0 || column >= gridSize.y)
                {
                    return tiles;
                }

                for (int offset = 1; offset < gridSize.x && added < tileCount; offset++)
                {
                    // 위쪽(+x)
                    Vector2Int upPos = new Vector2Int(center.x + offset, column);
                    if (gridController.IsValidPosition(upPos))
                    {
                        var upTile = gridController.GetTileAtPosition(upPos);
                        if (upTile != null)
                        {
                            tiles.Add(upTile);
                            added++;
                            if (added >= tileCount) break;
                        }
                    }

                    if (added >= tileCount)
                    {
                        break;
                    }

                    // 아래쪽(-x)
                    Vector2Int downPos = new Vector2Int(center.x - offset, column);
                    if (gridController.IsValidPosition(downPos))
                    {
                        var downTile = gridController.GetTileAtPosition(downPos);
                        if (downTile != null)
                        {
                            tiles.Add(downTile);
                            added++;
                        }
                    }
                }
            }

            return tiles;
        }

        public override string GetRangeDescription()
        {
            if (tileCount <= 0)
            {
                return "범위 없음";
            }

            if (tileCount == 1)
            {
                return "해당 타일만";
            }

            if (axis == LineAxis.Row)
            {
                return $"같은 행에서 {tileCount}타일 (우→좌 순서)";
            }

            return $"같은 열에서 {tileCount}타일 (위→아래 순서)";
        }
    }
}

