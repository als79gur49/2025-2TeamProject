using UnityEngine;

namespace Game.Utilities
{
    /// <summary>
    /// 그리드 좌표 생성을 위한 명시적 헬퍼
    ///
    /// 좌표계 규칙 (카르테시안 좌표계 - Unity 2D 표준):
    /// - 기준점: 좌하단(0,0)
    /// - X축: 가로 방향 (좌→우 증가) = Column
    /// - Y축: 세로 방향 (하→상 증가) = Row
    ///
    /// Vector2Int 구조:
    /// - Vector2Int.x = X축 값 = Column (가로 위치)
    /// - Vector2Int.y = Y축 값 = Row (세로 위치)
    /// - Vector2Int(x, y) = Vector2Int(column, row)
    ///
    /// 사용 예:
    /// - new Vector2Int(3, 5) = 3번 열(가로), 5번 행(세로, 아래에서 5번째)
    /// - 배열 접근: grid[x, y] = grid[column][row]
    /// </summary>
    public static class GridPositionHelper
    {
        // ===== 위치 생성 메서드들 =====

        /// <summary>
        /// X, Y 축 기반 위치 생성 (표준 카르테시안 좌표계)
        /// </summary>
        /// <param name="x">X축 위치 (가로, Column, 좌→우)</param>
        /// <param name="y">Y축 위치 (세로, Row, 하→상)</param>
        /// <returns>그리드 위치 Vector2Int(x, y)</returns>
        public static Vector2Int CreateXY(int x, int y)
            => new Vector2Int(x, y);

        /// <summary>
        /// Column, Row 기반 위치 생성 (의미 명시)
        /// </summary>
        /// <param name="col">열 번호 (가로, X축, 좌→우)</param>
        /// <param name="row">행 번호 (세로, Y축, 하→상)</param>
        /// <returns>그리드 위치 Vector2Int(col, row)</returns>
        public static Vector2Int CreateColRow(int col, int row)
            => new Vector2Int(col, row);

        /// <summary>
        /// Row, Column 순서로 위치 생성 (2차원 배열 관습)
        /// 주의: Vector2Int는 내부적으로 (x, y) = (col, row) 순서로 저장됩니다
        /// </summary>
        /// <param name="row">행 번호 (세로, Y축, 하→상)</param>
        /// <param name="col">열 번호 (가로, X축, 좌→우)</param>
        /// <returns>그리드 위치 Vector2Int(col, row)</returns>
        public static Vector2Int CreateRowCol(int row, int col)
            => new Vector2Int(col, row); // 순서 주의!

        // ===== 값 추출 메서드들 =====

        /// <summary>
        /// X축 값 추출 (Column, 가로 위치)
        /// </summary>
        public static int GetX(this Vector2Int pos) => pos.x;

        /// <summary>
        /// Y축 값 추출 (Row, 세로 위치)
        /// </summary>
        public static int GetY(this Vector2Int pos) => pos.y;

        /// <summary>
        /// Column 값 추출 (X축, 가로 위치)
        /// </summary>
        public static int GetCol(this Vector2Int pos) => pos.x;

        /// <summary>
        /// Row 값 추출 (Y축, 세로 위치)
        /// </summary>
        public static int GetRow(this Vector2Int pos) => pos.y;

        /// <summary>
        /// 디버깅용: 좌표를 명시적으로 출력
        /// </summary>
        public static string ToDebugString(this Vector2Int pos)
            => $"(x:{pos.x}, y:{pos.y}) = (col:{pos.x}, row:{pos.y}) [좌하단 기준]";

        // ===== 거리 계산 메서드들 =====

        /// <summary>
        /// 맨하탄 거리 계산 (올바른 구현)
        /// 가로 거리 + 세로 거리
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>맨하탄 거리 (X축 거리 + Y축 거리)</returns>
        public static int CalculateManhattanDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
        }

        /// <summary>
        /// X축 거리 계산 (가로 방향 거리, Column 거리)
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>X축 거리 (가로 거리)</returns>
        public static int CalculateXDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(to.x - from.x);
        }

        /// <summary>
        /// Y축 거리 계산 (세로 방향 거리, Row 거리)
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>Y축 거리 (세로 거리)</returns>
        public static int CalculateYDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(to.y - from.y);
        }

        /// <summary>
        /// Column 거리 계산 (가로 방향)
        /// </summary>
        public static int CalculateColDistance(Vector2Int from, Vector2Int to)
        {
            return CalculateXDistance(from, to);
        }

        /// <summary>
        /// Row 거리 계산 (세로 방향)
        /// </summary>
        public static int CalculateRowDistance(Vector2Int from, Vector2Int to)
        {
            return CalculateYDistance(from, to);
        }

        /// <summary>
        /// 체비셰프 거리 계산 (대각선 이동 포함)
        /// max(X축 거리, Y축 거리)
        /// </summary>
        public static int CalculateChebyshevDistance(Vector2Int from, Vector2Int to)
        {
            int xDistance = Mathf.Abs(to.x - from.x);
            int yDistance = Mathf.Abs(to.y - from.y);
            return Mathf.Max(xDistance, yDistance);
        }

        // ===== 인접성 확인 메서드들 =====

        /// <summary>
        /// 두 위치가 인접한지 확인 (맨하탄 거리 1, 상하좌우만)
        /// </summary>
        public static bool IsAdjacent(Vector2Int from, Vector2Int to)
        {
            return CalculateManhattanDistance(from, to) == 1;
        }

        /// <summary>
        /// 두 위치가 8방향 인접한지 확인 (체비셰프 거리 1, 대각선 포함)
        /// </summary>
        public static bool IsAdjacentWithDiagonal(Vector2Int from, Vector2Int to)
        {
            return CalculateChebyshevDistance(from, to) == 1;
        }

        /// <summary>
        /// 순수 대각선 위치인지 확인 (상하좌우는 제외)
        /// </summary>
        public static bool IsPureDiagonal(Vector2Int from, Vector2Int to)
        {
            int xDiff = Mathf.Abs(to.x - from.x);
            int yDiff = Mathf.Abs(to.y - from.y);
            return xDiff == 1 && yDiff == 1;
        }

        // ===== 범위 계산 메서드들 =====

        /// <summary>
        /// 지정된 범위 내 모든 위치 생성 (정사각형 범위)
        /// </summary>
        /// <param name="center">중심 위치</param>
        /// <param name="range">범위 (0이면 중심만, 1이면 3x3, 2이면 5x5)</param>
        /// <returns>범위 내 모든 위치 리스트</returns>
        public static System.Collections.Generic.List<Vector2Int> GetPositionsInRange(Vector2Int center, int range)
        {
            var positions = new System.Collections.Generic.List<Vector2Int>();

            if (range == 0)
            {
                positions.Add(center);
                return positions;
            }

            // X축(가로), Y축(세로) 오프셋 적용
            for (int xOffset = -range; xOffset <= range; xOffset++)
            {
                for (int yOffset = -range; yOffset <= range; yOffset++)
                {
                    var pos = new Vector2Int(center.x + xOffset, center.y + yOffset);
                    positions.Add(pos);
                }
            }

            return positions;
        }

        /// <summary>
        /// 맨하탄 거리 기반 범위 내 위치 확인
        /// </summary>
        public static bool IsWithinManhattanRange(Vector2Int center, Vector2Int target, int range)
        {
            return CalculateManhattanDistance(center, target) <= range;
        }

        /// <summary>
        /// X축(가로) 거리 기반 범위 내 위치 확인
        /// </summary>
        public static bool IsWithinXRange(Vector2Int center, Vector2Int target, int range)
        {
            return CalculateXDistance(center, target) <= range;
        }

        /// <summary>
        /// Y축(세로) 거리 기반 범위 내 위치 확인
        /// </summary>
        public static bool IsWithinYRange(Vector2Int center, Vector2Int target, int range)
        {
            return CalculateYDistance(center, target) <= range;
        }

        // ===== 방향 벡터 상수들 (카르테시안 좌표계, Y축 상향) =====

        /// <summary>4방향 (상하좌우)</summary>
        public static readonly Vector2Int[] FourDirections = new[]
        {
            new Vector2Int(0, 1),   // 위 (Y축 증가)
            new Vector2Int(0, -1),  // 아래 (Y축 감소)
            new Vector2Int(-1, 0),  // 왼쪽 (X축 감소)
            new Vector2Int(1, 0)    // 오른쪽 (X축 증가)
        };

        /// <summary>8방향 (상하좌우 + 대각선)</summary>
        public static readonly Vector2Int[] EightDirections = new[]
        {
            new Vector2Int(0, 1),    // 위
            new Vector2Int(0, -1),   // 아래
            new Vector2Int(-1, 0),   // 왼쪽
            new Vector2Int(1, 0),    // 오른쪽
            new Vector2Int(-1, 1),   // 좌상
            new Vector2Int(1, 1),    // 우상
            new Vector2Int(-1, -1),  // 좌하
            new Vector2Int(1, -1)    // 우하
        };
    }
}
