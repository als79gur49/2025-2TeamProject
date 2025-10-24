using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Card.Effects;

namespace Game.Interfaces
{
    /// <summary>
    /// 읽기 전용 그리드 상태 인터페이스 - 데이터 조회만 가능
    /// </summary>
    public interface IReadOnlyGridState
    {
        // 그리드 속성
        Vector2Int GridSize { get; }

        // ✅ 새로운 Vector2 타일 크기 (X/Y 개별 설정 지원)
        Vector2 TileSizeVector { get; }

        // ✅ 기존 프로퍼티 유지 (deprecated, 하위 호환성)
        [System.Obsolete("Use TileSizeVector instead. Returns X component for backward compatibility.")]
        float TileSize { get; }

        Vector3 GridOrigin { get; }
        
        // 위치 검증
        bool IsValidPosition(Vector2Int position);
        bool IsPositionOccupied(Vector2Int position);
        bool IsPositionBlocked(Vector2Int position);
        
        // 유닛 조회
        GameObject GetUnitAtPosition(Vector2Int position);
        /// <summary>공격 가능한 타겟 반환 (Unit 우선, 없으면 Base)</summary>
        GameObject GetAttackableTargetAtPosition(Vector2Int position);
        Vector2Int GetUnitPosition(GameObject unit);
        bool TryGetUnitPosition(GameObject unit, out Vector2Int position);
        /// <summary>공격 대상(Unit/Base)의 위치 반환</summary>
        Vector2Int GetPositionToAttackTarget(GameObject target);
        bool TryGetPositionToAttackTarget(GameObject target, out Vector2Int position);
        
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
        // 유닛 상태 변경 연산
        /// <summary>
        /// 그리드 데이터 레이어만 업데이트 (Transform 변경 없음)
        /// </summary>
        bool UpdateGridDataLayer(GameObject unit, Vector2Int newPosition);

        /// <summary>
        /// [Deprecated] 하위 호환성을 위한 메서드. UpdateGridDataLayer() 사용 권장
        /// </summary>
        [System.Obsolete("Use UpdateGridDataLayer() instead for clearer intent", false)]
        bool SetUnitPosition(GameObject unit, Vector2Int newPosition);

        bool RemoveUnit(GameObject unit);

        // Base 상태 관리 연산
        /// <summary>
        /// Base를 그리드에 배치 (순수 상태 관리)
        /// 주의: Base.Initialize()는 호출 전에 완료되어 있어야 함
        /// </summary>
        bool PlaceBase(GameObject baseObject, Vector2Int startPosition, Vector2Int baseSize, TeamType team);

        /// <summary>
        /// Base를 그리드에서 제거
        /// </summary>
        bool RemoveBase(GameObject baseObject);

        /// <summary>
        /// 특정 위치의 Base 반환
        /// </summary>
        GameObject GetBaseAtPosition(Vector2Int position);

        /// <summary>
        /// Base가 점유한 모든 타일 위치 반환
        /// </summary>
        List<Vector2Int> GetBaseOccupiedPositions(GameObject baseObject);

        /// <summary>
        /// 모든 Base 객체 반환
        /// </summary>
        System.Collections.Generic.IEnumerable<GameObject> GetAllBases();

        // 타일 상태 관리
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

        // ✅ 새로운 Vector2 타일 크기 (X/Y 개별 설정 지원)
        Vector2 TileSizeVector { get; }

        // ✅ 기존 프로퍼티 유지 (deprecated, 하위 호환성)
        [System.Obsolete("Use TileSizeVector instead. Returns X component for backward compatibility.")]
        float TileSize { get; }
        
        // ✅ 위치 유효성 검증
        bool IsValidPosition(Vector2Int gridPosition);
        bool IsPositionOccupied(Vector2Int gridPosition);
        bool IsPositionBlocked(Vector2Int gridPosition);
        
        // ✅ 유닛 위치 관리
        GameObject GetUnitAtPosition(Vector2Int gridPosition);
        /// <summary>공격 가능한 타겟 반환 (Unit 우선, 없으면 Base)</summary>
        GameObject GetAttackableTargetAtPosition(Vector2Int gridPosition);
        Vector2Int GetUnitPosition(GameObject unit);
        bool TryGetUnitPosition(GameObject unit, out Vector2Int position);
        /// <summary>공격 대상(Unit/Base)의 위치 반환</summary>
        Vector2Int GetPositionToAttackTarget(GameObject target);
        bool TryGetPositionToAttackTarget(GameObject target, out Vector2Int position);
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

        // ✅ 높이 계산 (Phase 4: Base 높이 차이 반영)
        /// <summary>
        /// 높이를 포함한 월드 좌표 계산 (GridController 위임)
        /// MovementComponent가 IGridHeightCalculator에 직접 의존하지 않도록
        /// </summary>
        Vector3 CalculateWorldPositionWithHeight(Vector2Int gridPosition);

        /// <summary>
        /// 특정 위치의 지면 높이 반환 (GridController 위임)
        /// </summary>
        float GetGroundHeightAt(Vector2Int gridPosition);

        // ✅ 타일 상태 관리
        void SetTileBlocked(Vector2Int position, bool blocked);
        void SetTileHighlight(Vector2Int position, Color highlightColor);
        void ClearHighlight(Vector2Int position);
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
    /// 그리드 공간 쿼리 인터페이스 - 기본 공간 관련 조회 및 거리 계산
    /// </summary>
    public interface IGridSpatialQuery
    {
        // 그리드 속성들
        Vector2Int GridSize { get; }

        // ✅ 새로운 Vector2 타일 크기 (X/Y 개별 설정 지원)
        Vector2 TileSizeVector { get; }

        // ✅ 기존 프로퍼티 유지 (deprecated, 하위 호환성)
        [System.Obsolete("Use TileSizeVector instead. Returns X component for backward compatibility.")]
        float TileSize { get; }

        // 위치 검증 메서드들
        bool IsValidPosition(Vector2Int gridPosition);
        bool IsPositionOccupied(Vector2Int gridPosition);
        bool IsPositionBlocked(Vector2Int gridPosition);

        // 유닛 위치 관리
        GameObject GetUnitAtPosition(Vector2Int gridPosition);
        /// <summary>공격 가능한 타겟 반환 (Unit 우선, 없으면 Base)</summary>
        GameObject GetAttackableTargetAtPosition(Vector2Int gridPosition);
        Vector2Int GetUnitPosition(GameObject unit);
        bool TryGetUnitPosition(GameObject unit, out Vector2Int position);
        /// <summary>공격 대상(Unit/Base)의 위치 반환</summary>
        Vector2Int GetPositionToAttackTarget(GameObject target);
        bool TryGetPositionToAttackTarget(GameObject target, out Vector2Int position);

        // 좌표 변환
        Vector3 GridToWorldPosition(Vector2Int gridPosition);
        Vector2Int WorldToGridPosition(Vector3 worldPosition);

        // 범위 검색
        List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true);
        List<GameObject> GetUnitsInRange(Vector2Int center, int range);

        // Phase 3.15: 거리 계산 메서드들 (공간 쿼리의 핵심 기능)
        /// <summary>플레이어 기준점에서 대상 위치까지의 맨하탄 거리</summary>
        int GetDistanceFromPlayerBase(Vector2Int targetPosition);

        /// <summary>적군 기준점에서 대상 위치까지의 맨하탄 거리</summary>
        int GetDistanceFromEnemyBase(Vector2Int targetPosition);

        /// <summary>지정된 거리 내의 모든 유효한 위치 반환</summary>
        List<Vector2Int> GetPositionsWithinRange(int maxRange, bool isPlayerBased, bool includeOccupied = true);
    }

    /// <summary>
    /// 그리드 팀 쿼리 인터페이스 - 팀 기반 유닛 조회 및 기준점 위치
    /// </summary>
    public interface IGridTeamQuery
    {
        // 팀별 유닛 존재 확인
        bool HasUnit(Vector2Int position);
        bool HasPlayerUnit(Vector2Int position);
        bool HasEnemyUnit(Vector2Int position);

        // 확장성을 위한 일반화된 메서드들
        bool HasUnitWithTeam(Vector2Int position, TeamType team);
        bool HasUnitWithRelation(Vector2Int position, TeamType relativeTo, TeamRelation relation);

        // 팀 정보 조회
        TeamType GetUnitTeam(Vector2Int position);

        // Phase 3.15: 팀 기준점 위치 조회 (팀 쿼리의 핵심 기능)
        /// <summary>플레이어 팀의 기준점 위치 반환 (가장 왼쪽 유닛)</summary>
        Vector2Int GetPlayerBasePosition();

        /// <summary>적군 팀의 기준점 위치 반환 (가장 오른쪽 유닛)</summary>
        Vector2Int GetEnemyBasePosition();
    }

    /// <summary>
    /// 그리드 카드 효과 쿼리 인터페이스 - 카드 효과 시스템 전용 쿼리
    /// Phase 2.12 & 3.15: 카드 효과 대상 결정 및 범위 검증 로직
    /// </summary>
    public interface IGridEffectQuery
    {
        // Phase 2.12: 카드 효과 대상 해결
        /// <summary>
        /// AffectedType과 AffectedRange를 기반으로 영향받을 유닛 리스트를 반환
        /// </summary>
        /// <param name="targetPosition">효과의 중심 위치</param>
        /// <param name="affectedType">영향받을 대상 타입 (Ally/Enemy/Any/None)</param>
        /// <param name="affectedRange">효과 범위 (0: 단일 대상, 1+: 범위 효과)</param>
        /// <param name="casterTeam">효과를 발동시킨 팀 (팀 구분용)</param>
        /// <returns>영향받을 유닛들의 GameObject 리스트</returns>
        List<GameObject> GetAffectedUnits(Vector2Int targetPosition, AffectedType affectedType, int affectedRange, TeamType casterTeam);

        // Phase 3.15: 카드 타겟 범위 검증
        /// <summary>
        /// 카드의 TargetRange를 검증하여 대상 위치가 카드 사용 가능 범위 내인지 확인
        /// </summary>
        /// <param name="cardData">검증할 카드 데이터</param>
        /// <param name="targetPosition">목표 위치</param>
        /// <param name="isPlayerCard">플레이어 카드인지 여부</param>
        /// <returns>TargetRange 검증 결과</returns>
        bool ValidateCardTargetRange(CardData cardData, Vector2Int targetPosition, bool isPlayerCard);

        // 디버깅 유틸리티
        /// <summary>
        /// GetAffectedUnits의 결과를 디버그 로그로 출력
        /// </summary>
        void DebugLogAffectedUnits(Vector2Int targetPosition, AffectedType affectedType, int affectedRange, TeamType casterTeam);

        /// <summary>
        /// TargetRange 검증 디버깅을 위한 상세 정보 출력
        /// </summary>
        void DebugTargetRangeValidation(CardData cardData, List<Vector2Int> testPositions, bool isPlayerCard);
    }

    /// <summary>
    /// 그리드 컨트롤러 인터페이스 - 비즈니스 로직 담당
    /// IGridManager 상속 제거로 책임 분리, ISP 적용으로 인터페이스 분리
    /// Phase 3.16: IGridEffectQuery 추가로 카드 효과 로직 분리
    /// Phase 4: IGridHeightCalculator 추가로 Base/Ground 높이 계산 기능 노출
    /// </summary>
    public interface IGridController : IGridSpatialQuery, IGridTeamQuery, IGridEffectQuery, IGridHeightCalculator
    {
        // 의존성 초기화
        void Initialize(IGridState gridState);

        // 유닛 관리
        bool RemoveUnit(GameObject unit);

        // 타일 상태 관리
        void SetTileBlocked(Vector2Int position, bool blocked);

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

        /// <summary>
        /// Get Tile component at specified grid position (for Unit currentTile setup)
        /// </summary>
        Tile GetTileAtPosition(Vector2Int gridPosition);

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

        // 카드 프리뷰 시스템 (드래그 드롭 시각적 피드백)
        void ShowCardPreview(Vector2Int center, List<Vector2Int> affectedPositions, Color previewColor);
        void ShowValidatedPreview(List<Vector2Int> validPositions, List<Vector2Int> invalidPositions);
        void ClearCardPreview();
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