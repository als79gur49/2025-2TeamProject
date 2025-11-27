using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 격자 기반 선분 순회 유틸리티 (Bresenham)
    /// </summary>
    public static class GridTraversalUtility
    {
        /// <summary>
        /// from → to 선분이 통과하는 모든 그리드 좌표를 반환한다.
        /// 첫 번째 원소는 from, 마지막 원소는 to이다.
        /// </summary>
        public static List<Vector2Int> GetTraversedTiles(Vector2Int from, Vector2Int to)
        {
            var result = new List<Vector2Int>();

            int x0 = from.x;
            int y0 = from.y;
            int x1 = to.x;
            int y1 = to.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);

            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;

            int err = dx - dy;

            while (true)
            {
                result.Add(new Vector2Int(x0, y0));

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;

                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return result;
        }
    }
}

