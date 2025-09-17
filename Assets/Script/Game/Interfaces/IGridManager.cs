using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interfaces
{
    /// <summary>
    /// 그리드 관리 인터페이스 - 의존성 역전 원칙 적용
    /// </summary>
    public interface IGridManager
    {
        // ✅ 기본 그리드 정보
        Vector2Int GridSize { get; }
        float TileSize { get; }
        
        // ✅ 위치 유효성 검증
        bool IsValidPosition(Vector2Int gridPosition);
        bool IsPositionOccupied(Vector2Int gridPosition);
        bool IsPositionBlocked(Vector2Int gridPosition);
        
        // ✅ 유닛 위치 관리
        GameObject GetUnitAtPosition(Vector2Int gridPosition);
        Vector2Int GetUnitPosition(GameObject unit);
        bool TryGetUnitPosition(GameObject unit, out Vector2Int position);
        
        // ✅ 유닛 이동
        bool CanMoveUnit(GameObject unit, Vector2Int targetPosition);
        bool MoveUnit(GameObject unit, Vector2Int newPosition);
        bool TryMoveUnit(GameObject unit, Vector2Int newPosition, out string errorMessage);
        
        // ✅ 경로 탐색
        List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, GameObject movingUnit = null);
        bool IsPathClear(Vector2Int start, Vector2Int end, GameObject ignoredUnit = null);
        int GetPathDistance(Vector2Int start, Vector2Int end);
        
        // ✅ 범위 검색
        List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true);
        List<GameObject> GetUnitsInRange(Vector2Int center, int range);
        List<Vector2Int> GetValidMovePositions(GameObject unit, int moveRange);
        
        // ✅ 좌표 변환
        Vector3 GridToWorldPosition(Vector2Int gridPosition);
        Vector2Int WorldToGridPosition(Vector3 worldPosition);
        
        // ✅ 타일 상태 관리
        void SetTileBlocked(Vector2Int position, bool blocked);
        void SetTileHighlight(Vector2Int position, Color highlightColor);
        void ClearAllHighlights();
        
        // ✅ 이벤트
        event Action<GameObject, Vector2Int, Vector2Int> OnUnitMoved; // 유닛, 이전위치, 새위치
        event Action<Vector2Int, GameObject> OnUnitPlaced; // 위치, 유닛
        event Action<Vector2Int, GameObject> OnUnitRemoved; // 위치, 유닛
    }

    /// <summary>
    /// 그리드 타일 인터페이스
    /// </summary>
    public interface IGridTile
    {
        Vector2Int GridPosition { get; }
        bool IsOccupied { get; }
        bool IsBlocked { get; }
        GameObject OccupyingUnit { get; }
        
        void SetOccupied(GameObject unit);
        void ClearOccupied();
        void SetBlocked(bool blocked);
        void SetHighlight(Color color);
        void ClearHighlight();
    }

    /// <summary>
    /// 경로 탐색 결과
    /// </summary>
    public readonly struct PathfindingResult
    {
        public readonly bool Success;
        public readonly List<Vector2Int> Path;
        public readonly int Distance;
        public readonly string ErrorMessage;

        public PathfindingResult(bool success, List<Vector2Int> path, int distance, string errorMessage = "")
        {
            Success = success;
            Path = path ?? new List<Vector2Int>();
            Distance = distance;
            ErrorMessage = errorMessage ?? "";
        }

        public static PathfindingResult Failed(string error) => new PathfindingResult(false, null, -1, error);
        public static PathfindingResult Succeeded(List<Vector2Int> path) => new PathfindingResult(true, path, path.Count - 1);
    }

    /// <summary>
    /// 그리드 쿼리 옵션
    /// </summary>
    [Flags]
    public enum GridQueryOptions
    {
        None = 0,
        IncludeOccupied = 1 << 0,
        IncludeBlocked = 1 << 1,
        IgnoreSelf = 1 << 2,
        OnlyAllies = 1 << 3,
        OnlyEnemies = 1 << 4
    }
}