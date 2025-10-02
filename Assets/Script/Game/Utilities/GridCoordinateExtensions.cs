using UnityEngine;

namespace Game.Utilities
{
    /// <summary>
    /// Phase 3.4: 좌표계 통합을 위한 확장 메서드
    ///
    /// 좌표계 규칙:
    /// - 기준: 좌하단(0,0), +x=우측(가로), +y=상단(세로)
    /// - Vector2Int 구조: (x, y) = (col, row) = (가로, 세로)
    /// - Vector2Int.x = 가로 위치 (Column, X축)
    /// - Vector2Int.y = 세로 위치 (Row, Y축)
    /// </summary>
    public static class GridCoordinateExtensions
    {
        /// <summary>
        /// Phase 3.4: 명시적 X, Y 좌표로 그리드 위치 생성
        /// </summary>
        /// <param name="x">가로 위치 (Column, X축, 0부터 시작)</param>
        /// <param name="y">세로 위치 (Row, Y축, 0부터 시작)</param>
        /// <returns>그리드 위치 Vector2Int(x, y)</returns>
        public static Vector2Int ToGridPosition(int x, int y) => new Vector2Int(x, y);

        /// <summary>
        /// Phase 3.4: Vector2Int를 x, y 좌표로 분해
        /// </summary>
        /// <param name="pos">그리드 위치</param>
        /// <param name="x">가로 위치 (Column, X축)</param>
        /// <param name="y">세로 위치 (Row, Y축)</param>
        public static void Deconstruct(this Vector2Int pos, out int x, out int y)
        {
            x = pos.x;  // 가로 (Column)
            y = pos.y;  // 세로 (Row)
        }

        /// <summary>
        /// Phase 3.4: Tile 객체로부터 그리드 위치 추출
        /// Tile.X = 가로(Column), Tile.Y = 세로(Row)
        /// </summary>
        public static Vector2Int GetGridPosition(this Tile tile) => new Vector2Int(tile.X, tile.Y);

        /// <summary>
        /// Phase 3.4: Tile 객체에 그리드 위치 설정
        /// </summary>
        /// <param name="tile">대상 타일</param>
        /// <param name="position">설정할 그리드 위치 (x=가로, y=세로)</param>
        public static void SetGridPosition(this Tile tile, Vector2Int position)
        {
            // position.x = 가로(Column), position.y = 세로(Row)
            tile.Initialize(position.x, position.y);
        }
    }
}