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
public class GridManager : MonoBehaviour
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
            
        InitializeGridSystem();
        RegisterServices();
        
        if (enablePerformanceLogging)
        {
            initializationTimer.Stop();
            Debug.Log($"[GridManager] Initialization completed in {initializationTimer.ElapsedMilliseconds}ms");
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
    /// 서비스 등록 - 의존성 주입 컨테이너에 등록
    /// </summary>
    private void RegisterServices()
    {
        var services = new GridServices(gridState, gridController, gridRenderer);
        
        // 인터페이스별로 적절한 서비스 등록
        ServiceLocator.Register<IGridServices>(services);
        ServiceLocator.Register<IGridManager>(gridController);
        ServiceLocator.Register<IGridController>(gridController);
        ServiceLocator.Register<IReadOnlyGridState>(gridState);
        ServiceLocator.Register<IGridState>(gridState);
        ServiceLocator.Register<IGridRenderer>(gridRenderer);
        
        // 서비스 로케이터 초기화 완료 마킹
        ServiceLocator.MarkAsInitialized();
        
        Debug.Log("[GridManager] All services registered successfully");
    }
    
    private void OnDestroy()
    {
        // 서비스 등록 해제
        try
        {
            ServiceLocator.UnregisterAll();
            Debug.Log("[GridManager] Services unregistered successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GridManager] Error during service cleanup: {ex.Message}");
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
    /// 에디터 전용 - 강제 시스템 재초기화
    /// </summary>
    [ContextMenu("Force System Reinitialize")]
    private void ForceSystemReinitialize()
    {
        Debug.Log("[GridManager] Force reinitializing system...");
        
        // 기존 시스템 정리
        OnDestroy();
        
        // 자식 오브젝트 정리
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        // 새 시스템 초기화
        InitializeGridSystem();
        RegisterServices();
        
        Debug.Log("[GridManager] System reinitialized successfully");
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

    // ============================================================================
    // Public API - 외부에서 시스템 접근 (필요한 경우에만)
    // ============================================================================

    /// <summary>
    /// 그리드 서비스 접근자 (권장: ServiceLocator 사용)
    /// </summary>
    public IGridServices GetGridServices()
    {
        return ServiceLocator.Get<IGridServices>();
    }
    
    /// <summary>
    /// 그리드 관리자 접근자 (권장: ServiceLocator 사용)
    /// </summary>
    public IGridManager GetGridManager()
    {
        return ServiceLocator.Get<IGridManager>();
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
}