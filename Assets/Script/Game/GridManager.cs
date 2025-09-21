using UnityEngine;
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
    [SerializeField] private float tileSize = 1f;
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
            
            Debug.Log("[GridManager] Phase 3 Clean Architecture initialized successfully");
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
        
        // 렌더러 초기화
        gridRenderer.Initialize(gridState, tilePrefab);
        
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
        
        var renderer = tilePrefab.GetComponent<Renderer>();
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
    /// 런타임 설정 변경 지원
    /// </summary>
    public void UpdateGridSettings(Vector2Int newSize, float newTileSize)
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
            gridRenderer.Initialize(gridState, tilePrefab); // 재초기화
        }
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
            if (gridState.GridSize != gridSize || Mathf.Abs(gridState.TileSize - tileSize) > 0.001f)
            {
                UpdateGridSettings(gridSize, tileSize);
            }
            
            UpdatePathfindingSettings(allowDiagonalMovement, maxPathfindingIterations);
        }
    }

    // ============================================================================
    // IGridManager 인터페이스 구현 - GridController로 위임
    // ============================================================================

    public Vector2Int GridSize => gridController?.GridSize ?? Vector2Int.zero;
    public float TileSize => gridController?.TileSize ?? 1f;

    public bool IsValidPosition(Vector2Int gridPosition) => gridController?.IsValidPosition(gridPosition) ?? false;
    public bool IsPositionOccupied(Vector2Int gridPosition) => gridController?.IsPositionOccupied(gridPosition) ?? false;
    public bool IsPositionBlocked(Vector2Int gridPosition) => gridController?.IsPositionBlocked(gridPosition) ?? false;

    public GameObject GetUnitAtPosition(Vector2Int gridPosition) => gridController?.GetUnitAtPosition(gridPosition);
    public Vector2Int GetUnitPosition(GameObject unit) => gridController?.GetUnitPosition(unit) ?? new Vector2Int(-1, -1);
    public bool TryGetUnitPosition(GameObject unit, out Vector2Int position)
    {
        if (gridController != null)
            return gridController.TryGetUnitPosition(unit, out position);
        
        position = new Vector2Int(-1, -1);
        return false;
    }

    public bool CanMoveUnit(GameObject unit, Vector2Int targetPosition) => gridController?.CanMoveUnit(unit, targetPosition) ?? false;
    public bool MoveUnit(GameObject unit, Vector2Int newPosition) => gridController?.MoveUnit(unit, newPosition) ?? false;
    public bool MoveUnit(GameObject unit, Vector2Int startPosition, Vector2Int endPosition) => gridController?.MoveUnit(unit, startPosition, endPosition) ?? false;
    public bool TryMoveUnit(GameObject unit, Vector2Int newPosition, out string errorMessage)
    {
        if (gridController != null)
            return gridController.TryMoveUnit(unit, newPosition, out errorMessage);
        
        errorMessage = "GridController not initialized";
        return false;
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, GameObject movingUnit = null) => gridController?.FindPath(start, end, movingUnit) ?? new List<Vector2Int>();
    public bool IsPathClear(Vector2Int start, Vector2Int end, GameObject ignoredUnit = null) => gridController?.IsPathClear(start, end, ignoredUnit) ?? false;
    public int GetPathDistance(Vector2Int start, Vector2Int end) => gridController?.GetPathDistance(start, end) ?? -1;

    public List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true) => gridController?.GetPositionsInRange(center, range, includeOccupied) ?? new List<Vector2Int>();
    public List<GameObject> GetUnitsInRange(Vector2Int center, int range) => gridController?.GetUnitsInRange(center, range) ?? new List<GameObject>();
    public List<Vector2Int> GetValidMovePositions(GameObject unit, int moveRange) => gridController?.GetValidMovePositions(unit, moveRange) ?? new List<Vector2Int>();

    public Vector3 GridToWorldPosition(Vector2Int gridPosition) => gridController?.GridToWorldPosition(gridPosition) ?? Vector3.zero;
    public Vector2Int WorldToGridPosition(Vector3 worldPosition) => gridController?.WorldToGridPosition(worldPosition) ?? Vector2Int.zero;

    public void SetTileBlocked(Vector2Int position, bool blocked) => gridController?.SetTileBlocked(position, blocked);
    public void SetTileHighlight(Vector2Int position, Color highlightColor) => gridController?.SetTileHighlight(position, highlightColor);
    public void ClearAllHighlights() => gridController?.ClearAllHighlights();

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