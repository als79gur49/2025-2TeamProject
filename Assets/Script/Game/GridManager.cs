using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.Components;
using Game.Core;

/// <summary>
/// Phase 3: 최종 GridManager - 순수 코디네이터
/// Clean Architecture 4계층 구조 완성
/// - Unity Integration Layer: GridManager (이 클래스)
/// - Presentation Layer: GridRenderer
/// - Business Logic Layer: GridController  
/// - Data Layer: GridState
/// </summary>
public class GridManager : MonoBehaviour, IGridManager
{
    [Header("Grid Configuration")]
    [SerializeField] private Vector2Int gridSize = new(10, 10);
    [SerializeField] private Vector2 tileSize = Vector2.one;
    [SerializeField] private GameObject tilePrefab;
    
    [Header("Pathfinding Settings")]
    [SerializeField] private bool allowDiagonalMovement = false;
    [SerializeField] private int maxPathfindingIterations = 1000;
    
    [Header("Advanced Settings")]
    [SerializeField] private bool enablePathfindingCache = true;
    [SerializeField] private bool enablePerformanceLogging = false;

    // 클린 아키텍처 컴포넌트들
    private GridState gridState;
    private GridController gridController;
    private GridRenderer gridRenderer;
    
    // 성능 모니터링
    private System.Diagnostics.Stopwatch initializationTimer;
    
    private void Awake()
    {
        if (enablePerformanceLogging)
            initializationTimer = System.Diagnostics.Stopwatch.StartNew();
            
        // 중앙집중형 구조: 서비스 등록은 GameInitializer에서 처리
        // 그리드 시스템만 초기화
        InitializeGridSystem();
        
        if (enablePerformanceLogging)
        {
            initializationTimer.Stop();
            Debug.Log($"[GridManager] Grid system initialization completed in {initializationTimer.ElapsedMilliseconds}ms");
        }
    }

    /// <summary>
    /// 그리드 시스템 초기화 - 적절한 의존성 흐름
    /// Data Layer → Business Logic Layer → Presentation Layer → Unity Integration Layer
    /// </summary>
    private void InitializeGridSystem()
    {
        try
        {
            // 1. 데이터 계층 생성 (가장 하위 계층)
            CreateDataLayer();
            
            // 2. 비즈니스 로직 계층 생성 (데이터에 의존)
            CreateBusinessLogicLayer();
            
            // 3. 프레젠테이션 계층 생성 (데이터에 의존)
            CreatePresentationLayer();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridManager] Failed to initialize grid system: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }
    
    /// <summary>
    /// 데이터 계층 생성 - GridState
    /// </summary>
    private void CreateDataLayer()
    {
        var gridStateGO = new GameObject("GridState")
        {
            transform = { parent = transform }
        };
        
        gridState = gridStateGO.AddComponent<GridState>();
        
        // GridState 설정 (Inspector 값 전달)
        SetGridStateProperties();
        
        Debug.Log($"[GridManager] Data Layer created: GridState ({gridSize.x}x{gridSize.y}, tile size: {tileSize})");
    }
    
    /// <summary>
    /// GridState 속성 설정 (Reflection을 통한 Inspector 값 전달)
    /// </summary>
    private void SetGridStateProperties()
    {
        var gridSizeField = typeof(GridState).GetField("gridSize", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tileSizeField = typeof(GridState).GetField("tileSize", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var gridOriginField = typeof(GridState).GetField("gridOrigin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        gridSizeField?.SetValue(gridState, gridSize);
        tileSizeField?.SetValue(gridState, tileSize);
        gridOriginField?.SetValue(gridState, transform.position);
    }

    /// <summary>
    /// 비즈니스 로직 계층 생성 - GridController
    /// </summary>
    private void CreateBusinessLogicLayer()
    {
        // GridController는 MonoBehaviour가 아닌 순수 C# 클래스
        gridController = new GridController(gridState);
        gridController.SetPathfindingOptions(allowDiagonalMovement, maxPathfindingIterations);
        
        // 캐시 설정
        if (!enablePathfindingCache)
        {
            gridController.ClearPathCache();
        }
        
        Debug.Log("[GridManager] Business Logic Layer created: GridController");
    }

    /// <summary>
    /// 프레젠테이션 계층 생성 - GridRenderer
    /// </summary>
    private void CreatePresentationLayer()
    {
        var rendererGO = new GameObject("GridRenderer")
        {
            transform = { parent = transform }
        };
        
        gridRenderer = rendererGO.AddComponent<GridRenderer>();

        // 타일 프리팹 검증 및 생성
        ValidateOrCreateTilePrefab();

        // 렌더러 초기화 (gridController는 IGridHeightCalculator를 구현)
        gridRenderer.Initialize(gridState, tilePrefab, gridController);

        Debug.Log("[GridManager] Presentation Layer created: GridRenderer");
    }
    
    /// <summary>
    /// 타일 프리팹 검증 및 기본 생성
    /// </summary>
    private void ValidateOrCreateTilePrefab()
    {
        if (tilePrefab == null)
        {
            Debug.LogWarning("[GridManager] No tile prefab assigned, creating default");
            CreateDefaultTilePrefab();
        }
        
        // 타일에 필수 컴포넌트가 있는지 확인
        if (tilePrefab.GetComponent<Tile>() == null)
        {
            Debug.LogWarning("[GridManager] Tile prefab missing Tile component, adding automatically");
            tilePrefab.AddComponent<Tile>();
        }
    }
    
    /// <summary>
    /// 기본 타일 프리팹 생성
    /// </summary>
    private void CreateDefaultTilePrefab()
    {
        tilePrefab = GameObject.CreatePrimitive(PrimitiveType.Plane);
        tilePrefab.name = "DefaultTile";
        tilePrefab.transform.localScale = new Vector3(0.1f, 1f, 0.1f);

        // CreatePrimitive는 Root에 Renderer를 생성하므로 자식으로 구조화 필요 없음
        var renderer = tilePrefab.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.green;
        }

        tilePrefab.AddComponent<Tile>();
    }

    /// <summary>
    /// GameInitializer에서 호출할 초기화 메서드 - 중앙집중형 구조
    /// </summary>
    public void InitializeForServiceLocator()
    {
        // 그리드 시스템이 아직 초기화되지 않았다면 초기화
        if (gridState == null || gridController == null || gridRenderer == null)
        {
            InitializeGridSystem();
        }
        
        Debug.Log("[GridManager] Grid system prepared for centralized service registration");
    }
    
    private void OnDestroy()
    {
        // 중앙집중형 구조: 서비스 해제는 GameInitializer에서 처리
        // GridManager는 자체 리소스만 정리
        try
        {
            // 그리드 관련 리소스 정리
            gridController?.ClearPathCache();
            
            Debug.Log("[GridManager] GridManager resources cleaned up successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GridManager] Error during GridManager cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// 런타임 설정 변경 지원 - Vector2 타일 크기 지원
    /// </summary>
    public void UpdateGridSettings(Vector2Int newSize, Vector2 newTileSize)
    {
        if (gridState == null)
        {
            Debug.LogWarning("[GridManager] GridState not initialized, cannot update settings");
            return;
        }

        Debug.Log($"[GridManager] Updating grid settings: {gridSize} → {newSize}, tile size: {tileSize} → {newTileSize}");

        gridSize = newSize;
        tileSize = newTileSize;

        gridState.ResizeGrid(newSize);

        // 렌더러 업데이트
        if (gridRenderer != null)
        {
            gridRenderer.Initialize(gridState, tilePrefab, gridController); // 재초기화
        }
    }

    /// <summary>
    /// 하위 호환성 오버로드 - float 타일 크기 (deprecated)
    /// </summary>
    [System.Obsolete("Use UpdateGridSettings(Vector2Int, Vector2) instead")]
    public void UpdateGridSettings(Vector2Int newSize, float newTileSize)
    {
        UpdateGridSettings(newSize, new Vector2(newTileSize, newTileSize));
    }

    /// <summary>
    /// 길찾기 설정 변경
    /// </summary>
    public void UpdatePathfindingSettings(bool diagonal, int maxIterations)
    {
        allowDiagonalMovement = diagonal;
        maxPathfindingIterations = maxIterations;
        gridController?.SetPathfindingOptions(diagonal, maxIterations);
        
        Debug.Log($"[GridManager] Pathfinding settings updated: diagonal={diagonal}, maxIterations={maxIterations}");
    }
    
    /// <summary>
    /// 성능 캐시 관리
    /// </summary>
    public void SetCacheEnabled(bool enabled)
    {
        enablePathfindingCache = enabled;
        
        if (!enabled)
        {
            gridController?.ClearPathCache();
        }
        
        Debug.Log($"[GridManager] Pathfinding cache {(enabled ? "enabled" : "disabled")}");
    }

    // ============================================================================
    // 디버깅 및 에디터 지원 메서드
    // ============================================================================


    /// <summary>
    /// 에디터 전용 - 시스템 상태 확인
    /// </summary>
    [ContextMenu("Check System Status")]
    private void CheckSystemStatus()
    {
        Debug.Log("=== GridManager Phase 3 System Status ===");
        Debug.Log($"🏗️ Architecture: Clean Architecture 4-Layer");
        Debug.Log($"📊 GridState: {GetComponentStatus(gridState)} | Size: {gridState?.GridSize} | Tiles: {gridState?.TotalTiles} | Occupied: {gridState?.OccupiedTiles}");
        Debug.Log($"🧠 GridController: {GetComponentStatus(gridController)} | PathCache: {(enablePathfindingCache ? "✓" : "✗")}");
        Debug.Log($"🎨 GridRenderer: {GetComponentStatus(gridRenderer)} | TilePrefab: {(tilePrefab != null ? "✓" : "✗")}");
        Debug.Log($"🌐 ServiceLocator: {(ServiceLocator.IsInitialized ? "✓ Initialized" : "✗ Not initialized")} | Services: {ServiceLocator.GetRegisteredServices().Count}");
        
        // 성능 통계
        if (enablePerformanceLogging && gridController != null)
        {
            Debug.Log($"⚡ Performance: Diagonal={allowDiagonalMovement}, MaxIterations={maxPathfindingIterations}");
        }
    }
    
    /// <summary>
    /// 컴포넌트 상태를 문자열로 반환
    /// </summary>
    private string GetComponentStatus(object component)
    {
        if (component == null) return "✗ null";
        if (component is MonoBehaviour mono && mono == null) return "✗ destroyed";
        return $"✓ {component.GetType().Name}";
    }


    /// <summary>
    /// 에디터 전용 - 성능 테스트
    /// </summary>
    [ContextMenu("Run Performance Test")]
    private void RunPerformanceTest()
    {
        if (gridController == null || gridState == null)
        {
            Debug.LogWarning("[GridManager] System not initialized, cannot run performance test");
            return;
        }
        
        var timer = System.Diagnostics.Stopwatch.StartNew();
        const int testIterations = 100;
        
        Debug.Log($"[GridManager] Running performance test ({testIterations} iterations)...");
        
        // 경로 탐색 성능 테스트
        for (int i = 0; i < testIterations; i++)
        {
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(gridSize.x - 1, gridSize.y - 1);
            gridController.FindPath(start, end);
        }
        
        timer.Stop();
        var avgTime = (float)timer.ElapsedMilliseconds / testIterations;
        
        Debug.Log($"[GridManager] Performance Test Results:");
        Debug.Log($"  Total time: {timer.ElapsedMilliseconds}ms");
        Debug.Log($"  Average per pathfinding: {avgTime:F2}ms");
        Debug.Log($"  Grid size: {gridSize.x}x{gridSize.y} ({gridSize.x * gridSize.y} tiles)");
    }


    // Unity Inspector에서 실시간 설정 변경을 위한 OnValidate
    private void OnValidate()
    {
        // 런타임 중에만 적용
        if (Application.isPlaying && gridState != null && gridController != null)
        {
            // Vector2 비교 (부동소수점 안전)
            bool tileSizeChanged =
                !Mathf.Approximately(gridState.TileSizeVector.x, tileSize.x) ||
                !Mathf.Approximately(gridState.TileSizeVector.y, tileSize.y);

            if (gridState.GridSize != gridSize || tileSizeChanged)
            {
                UpdateGridSettings(gridSize, tileSize);
            }

            UpdatePathfindingSettings(allowDiagonalMovement, maxPathfindingIterations);
        }
    }

    // ============================================================================
    // IGridManager 인터페이스 구현 - 최적화된 GridController 위임
    // ============================================================================

    /// <summary>
    /// GridController에 대한 안전한 접근을 제공하는 내부 메서드
    /// </summary>
    private IGridController GetController()
    {
        if (gridController == null)
        {
            Debug.LogWarning("[GridManager] GridController not initialized. Call InitializeGridSystem() first.");
        }
        return gridController;
    }

    // 기본 속성들
    public Vector2Int GridSize => GetController()?.GridSize ?? Vector2Int.zero;

    // ✅ 새로운 Vector2 타일 크기 (X/Y 개별 설정 지원)
    public Vector2 TileSizeVector => GetController()?.TileSizeVector ?? Vector2.one;

    // ✅ 기존 프로퍼티 유지 (deprecated, 하위 호환성)
    [System.Obsolete("Use TileSizeVector instead. Returns X component for backward compatibility.")]
    public float TileSize => GetController()?.TileSize ?? 1f;

    // 위치 검증 메서드들
    public bool IsValidPosition(Vector2Int gridPosition) => GetController()?.IsValidPosition(gridPosition) ?? false;
    public bool IsPositionOccupied(Vector2Int gridPosition) => GetController()?.IsPositionOccupied(gridPosition) ?? false;
    public bool IsPositionWalkable(Vector2Int gridPosition) => GetController()?.IsPositionWalkable(gridPosition) ?? false;
    public bool IsPositionBlocked(Vector2Int gridPosition) => GetController()?.IsPositionBlocked(gridPosition) ?? false;

    // 유닛 위치 관리
    public GameObject GetUnitAtPosition(Vector2Int gridPosition) => GetController()?.GetUnitAtPosition(gridPosition);
    public GameObject GetAttackableTargetAtPosition(Vector2Int gridPosition) => GetController()?.GetAttackableTargetAtPosition(gridPosition);
    public Vector2Int GetUnitPosition(GameObject unit) => GetController()?.GetUnitPosition(unit) ?? new Vector2Int(-1, -1);
    public bool TryGetUnitPosition(GameObject unit, out Vector2Int position)
    {
        var controller = GetController();
        return controller?.TryGetUnitPosition(unit, out position) ?? (position = new Vector2Int(-1, -1), false).Item2;
    }
    public Vector2Int GetPositionToAttackTarget(GameObject target) => GetController()?.GetPositionToAttackTarget(target) ?? new Vector2Int(-1, -1);
    public bool TryGetPositionToAttackTarget(GameObject target, out Vector2Int position)
    {
        var controller = GetController();
        return controller?.TryGetPositionToAttackTarget(target, out position) ?? (position = new Vector2Int(-1, -1), false).Item2;
    }

    // 🔧 FIX: Unit death에서 GridState 정리를 위한 RemoveUnit 메서드 추가
    public bool RemoveUnit(GameObject unit) => GetController()?.RemoveUnit(unit) ?? false;

    // 특정 위치에 특정 관계의 유닛이 있는지 확인
    public bool HasUnitWithRelation(Vector2Int position, TeamType relativeTo, TeamRelation relation)
        => GetController()?.HasUnitWithRelation(position, relativeTo, relation) ?? false;

    // 유닛 이동 (가장 중요한 기능들)
    public bool CanMoveUnit(GameObject unit, Vector2Int targetPosition) => GetController()?.CanMoveUnit(unit, targetPosition) ?? false;
    public bool MoveUnit(GameObject unit, Vector2Int newPosition) => GetController()?.MoveUnit(unit, newPosition) ?? false;
    public bool MoveUnit(GameObject unit, Vector2Int startPosition, Vector2Int endPosition) => GetController()?.MoveUnit(unit, startPosition, endPosition) ?? false;
    public bool TryMoveUnit(GameObject unit, Vector2Int newPosition, out string errorMessage)
    {
        var controller = GetController();
        return controller?.TryMoveUnit(unit, newPosition, out errorMessage) ?? (errorMessage = "GridController not initialized", false).Item2;
    }

    // 경로 탐색
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, GameObject movingUnit = null) => GetController()?.FindPath(start, end, movingUnit) ?? new List<Vector2Int>();
    public bool IsPathClear(Vector2Int start, Vector2Int end, GameObject ignoredUnit = null) => GetController()?.IsPathClear(start, end, ignoredUnit) ?? false;
    public int GetPathDistance(Vector2Int start, Vector2Int end) => GetController()?.GetPathDistance(start, end) ?? -1;

    // 범위 검색
    public List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true) => GetController()?.GetPositionsInRange(center, range, includeOccupied) ?? new List<Vector2Int>();
    public List<GameObject> GetUnitsInRange(Vector2Int center, int range) => GetController()?.GetUnitsInRange(center, range) ?? new List<GameObject>();
    public List<Vector2Int> GetValidMovePositions(GameObject unit, int moveRange) => GetController()?.GetValidMovePositions(unit, moveRange) ?? new List<Vector2Int>();

    // 좌표 변환
    public Vector3 GridToWorldPosition(Vector2Int gridPosition) => GetController()?.GridToWorldPosition(gridPosition) ?? Vector3.zero;
    public Vector2Int WorldToGridPosition(Vector3 worldPosition) => GetController()?.WorldToGridPosition(worldPosition) ?? Vector2Int.zero;

    // Phase 4: Base 높이 차이 반영 - 높이 계산 메서드 위임
    /// <summary>
    /// GridController의 높이 계산 메서드 위임
    /// MovementComponent가 IGridHeightCalculator에 직접 의존하지 않도록
    /// </summary>
    public Vector3 CalculateWorldPositionWithHeight(Vector2Int gridPosition)
    {
        var heightCalculator = gridController as IGridHeightCalculator;
        return heightCalculator?.CalculateWorldPositionWithHeight(gridPosition)
               ?? GridToWorldPosition(gridPosition); // 폴백: 높이 없는 기본 좌표
    }

    /// <summary>
    /// GridController의 지면 높이 조회 위임
    /// </summary>
    public float GetGroundHeightAt(Vector2Int gridPosition)
    {
        var heightCalculator = gridController as IGridHeightCalculator;
        return heightCalculator?.GetGroundHeightAt(gridPosition) ?? 0f; // 폴백: 높이 0
    }

    // 타일 상태 관리 - 렌더러와 연동
    public void SetTileBlocked(Vector2Int position, bool blocked) => GetController()?.SetTileBlocked(position, blocked);
    public void SetTileHighlight(Vector2Int position, Color highlightColor)
    {
        // 하이라이트는 렌더링 기능이므로 GridRenderer에만 위임
        gridRenderer?.SetTileHighlight(position, highlightColor);
    }

    public void ClearHighlight(Vector2Int position)
    {
        // 특정 타일 하이라이트 정리는 렌더링 기능이므로 GridRenderer에만 위임
        gridRenderer?.ClearHighlight(position);
    }

    public void ClearAllHighlights()
    {
        // 하이라이트 정리는 렌더링 기능이므로 GridRenderer에만 위임
        gridRenderer?.ClearAllHighlights();
    }

    // IGridManager 이벤트들 - GridController의 이벤트를 중계
    public event System.Action<GameObject, Vector2Int, Vector2Int> OnUnitMoved
    {
        add { if (gridController != null) gridController.OnUnitMoved += value; }
        remove { if (gridController != null) gridController.OnUnitMoved -= value; }
    }

    public event System.Action<Vector2Int, GameObject> OnUnitPlaced
    {
        add { if (gridController != null) gridController.OnUnitPlaced += value; }
        remove { if (gridController != null) gridController.OnUnitPlaced -= value; }
    }

    public event System.Action<Vector2Int, GameObject> OnUnitRemoved
    {
        add { if (gridController != null) gridController.OnUnitRemoved += value; }
        remove { if (gridController != null) gridController.OnUnitRemoved -= value; }
    }

    /// <summary>
    /// GridController와 GridState, GridRenderer에 대한 접근 제공
    /// </summary>
    public IGridController GetGridController() => gridController;
    public IGridState GetGridState() => gridState;
    public IGridRenderer GetGridRenderer() => gridRenderer;
}