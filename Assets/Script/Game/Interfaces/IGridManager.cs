using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interfaces
{
    /// <summary>
    /// 읽기 전용 그리드 상태 인터페이스 - 데이터 조회만 가능
    /// </summary>
    public interface IReadOnlyGridState
    {
        // 그리드 속성
        Vector2Int GridSize { get; }
        float TileSize { get; }
        Vector3 GridOrigin { get; }
        
        // 위치 검증
        bool IsValidPosition(Vector2Int position);
        bool IsPositionOccupied(Vector2Int position);
        bool IsPositionBlocked(Vector2Int position);
        
        // 유닛 조회
        GameObject GetUnitAtPosition(Vector2Int position);
        Vector2Int GetUnitPosition(GameObject unit);
        bool TryGetUnitPosition(GameObject unit, out Vector2Int position);
        
        // 좌표 변환
        Vector3 GridToWorldPosition(Vector2Int gridPosition);
        Vector2Int WorldToGridPosition(Vector3 worldPosition);
        
        // 범위 검색
        List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true);
        List<GameObject> GetUnitsInRange(Vector2Int center, int range);
        
        // 상태 변경 이벤트
        event Action<GameObject, Vector2Int, Vector2Int> OnUnitMoved;
        event Action<Vector2Int, GameObject> OnUnitPlaced;
        event Action<Vector2Int, GameObject> OnUnitRemoved;
        event Action<Vector2Int, bool> OnTileBlockedChanged;
    }

    /// <summary>
    /// 완전한 그리드 상태 인터페이스 - 상태 변경 가능
    /// </summary>
    public interface IGridState : IReadOnlyGridState
    {
        // 상태 변경 연산
        bool SetUnitPosition(GameObject unit, Vector2Int newPosition);
        bool RemoveUnit(GameObject unit);
        void SetTileBlocked(Vector2Int position, bool blocked);
        void ResizeGrid(Vector2Int newSize);
        void ClearAllState();
    }

    /// <summary>
    /// 그리드 관리 인터페이스 - 의존성 역전 원칙 적용 (기존 호환성 유지)
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
        // 🔧 FIX: Unit death에서 GridState 정리를 위한 RemoveUnit 메서드 추가
        bool RemoveUnit(GameObject unit);
        
        // ✅ 유닛 이동
        bool CanMoveUnit(GameObject unit, Vector2Int targetPosition);
        bool MoveUnit(GameObject unit, Vector2Int newPosition);
        bool MoveUnit(GameObject unit, Vector2Int startPosition, Vector2Int endPosition);
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

        // ✅ 하위 컴포넌트 접근
        /// <summary>그리드 컨트롤러 반환</summary>
        IGridController GetGridController();

        /// <summary>그리드 상태 반환</summary>
        IGridState GetGridState();

        /// <summary>그리드 렌더러 반환</summary>
        IGridRenderer GetGridRenderer();
    }

    /// <summary>
    /// 그리드 컨트롤러 인터페이스 - 비즈니스 로직 담당
    /// IGridManager 상속 제거로 책임 분리
    /// </summary>
    public interface IGridController
    {
        // 그리드 속성들
        Vector2Int GridSize { get; }
        float TileSize { get; }

        // 의존성 초기화
        void Initialize(IGridState gridState);

        // 위치 검증 메서드들
        bool IsValidPosition(Vector2Int gridPosition);
        bool IsPositionOccupied(Vector2Int gridPosition);
        bool IsPositionBlocked(Vector2Int gridPosition);

        // 유닛 위치 관리
        GameObject GetUnitAtPosition(Vector2Int gridPosition);
        Vector2Int GetUnitPosition(GameObject unit);
        bool TryGetUnitPosition(GameObject unit, out Vector2Int position);
        bool RemoveUnit(GameObject unit);

        // 좌표 변환
        Vector3 GridToWorldPosition(Vector2Int gridPosition);
        Vector2Int WorldToGridPosition(Vector3 worldPosition);

        // 타일 상태 관리
        void SetTileBlocked(Vector2Int position, bool blocked);

        // 범위 검색
        List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true);
        List<GameObject> GetUnitsInRange(Vector2Int center, int range);

        // 유닛 이동
        bool CanMoveUnit(GameObject unit, Vector2Int targetPosition);
        bool MoveUnit(GameObject unit, Vector2Int newPosition);
        bool TryMoveUnit(GameObject unit, Vector2Int newPosition, out string errorMessage);

        // 길찾기 기능
        List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, GameObject movingUnit = null);
        PathfindingResult FindPathWithDetails(Vector2Int start, Vector2Int end, GameObject movingUnit = null);
        bool IsPathClear(Vector2Int start, Vector2Int end, GameObject ignoredUnit = null);
        int GetPathDistance(Vector2Int start, Vector2Int end);
        List<Vector2Int> GetValidMovePositions(GameObject unit, int moveRange);

        // 고급 설정
        void SetPathfindingOptions(bool allowDiagonal, int maxIterations);
        void ClearPathCache();

        // ✅ Clean Architecture: Unit positioning methods (Business Logic Layer responsibility)
        /// <summary>
        /// Move unit from one position to another with full business logic validation
        /// </summary>
        bool MoveUnit(GameObject unit, Vector2Int fromPosition, Vector2Int toPosition);

        /// <summary>
        /// Set unit's world position based on grid position (Business Logic responsibility)
        /// </summary>
        void SetUnitWorldPosition(GameObject unit, Vector2Int gridPosition);

        /// <summary>
        /// Calculate world position for unit placement including offset
        /// </summary>
        Vector3 CalculateUnitWorldPosition(Vector2Int gridPosition);

        /// <summary>
        /// Get the standard unit offset (e.g., Vector3.up * 0.5f)
        /// </summary>
        Vector3 GetUnitOffset();

        /// <summary>
        /// Validate and execute unit movement with comprehensive checks
        /// </summary>
        bool ValidateAndExecuteUnitMovement(GameObject unit, Vector2Int targetPosition);

        // 이벤트들
        event System.Action<GameObject, Vector2Int, Vector2Int> OnUnitMoved;
        event System.Action<Vector2Int, GameObject> OnUnitPlaced;
        event System.Action<Vector2Int, GameObject> OnUnitRemoved;
    }

    /// <summary>
    /// 그리드 렌더러 인터페이스 - 시각적 표현 담당
    /// </summary>
    public interface IGridRenderer
    {
        // 초기화
        void Initialize(IReadOnlyGridState gridState, GameObject tilePrefab);
        
        // 시각적 효과
        void SetTileHighlight(Vector2Int position, Color highlightColor);
        void SetMultipleTileHighlights(IEnumerable<Vector2Int> positions, Color color);
        void ClearAllHighlights();
        void ClearHighlight(Vector2Int position);
        
        // 타일 접근
        GameObject GetTileGameObject(Vector2Int position);
        bool TryGetTileGameObject(Vector2Int position, out GameObject tile);
        
        // 시각 효과
        void PlayTileEffect(Vector2Int position, string effectName);
        void SetTileTransparency(Vector2Int position, float alpha);
    }

    /// <summary>
    /// 그리드 서비스 통합 인터페이스
    /// </summary>
    public interface IGridServices
    {
        IGridState GridState { get; }
        IGridController GridController { get; }
        IGridRenderer GridRenderer { get; }
    }

    /// <summary>
    /// 그리드 의존성을 가지는 컴포넌트를 위한 인터페이스
    /// </summary>
    public interface IGridDependent
    {
        void Initialize(IGridServices gridServices);
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
    /// 통일된 그리드 유닛 인터페이스
    /// </summary>
    public interface IGridUnit
    {
        GameObject GameObject { get; }
        Vector2Int GridPosition { get; set; }
        bool CanOccupyTile(Vector2Int position);
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