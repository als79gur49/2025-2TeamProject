using UnityEngine;
using Game.Interfaces;
using Game.Components;
using Game.Core;

/// <summary>
/// [LEGACY - Phase 1->2] 레거시 GridManager - 하위 호환성 유지하면서 새 시스템 도입 + IGridManager 구현
/// Phase 3에서는 새로운 GridManager를 사용하세요. 이 클래스는 백업 목적으로만 유지됩니다.
/// </summary>
[System.Obsolete("GridManagerLegacy is deprecated. Use the new Phase 3 GridManager instead.", false)]
public class GridManagerLegacy : MonoBehaviour, IGridManager
{
    // 기존 public 인터페이스 유지 (SerializedField는 그대로)
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 6;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private float tileSize = 1.0f;
    
    // 새로운 내부 아키텍처
    private GridState gridState;
    private GridController gridController;
    private GridRenderer gridRenderer;
    private GridDataBridge dataBridge;
    
    // 레거시 속성 유지 (새 시스템으로 위임)
    public int Width => gridState?.GridSize.x ?? width;
    public int Height => gridState?.GridSize.y ?? height;
    public Tile[,] Tiles => GetTilesArray(); // 호환성용 변환
    
    private void Start()
    {
        InitializeNewSystem();
        GenerateGrid(); // 기존 GenerateGrid 호출 유지
    }
    
    /// <summary>
    /// 새 시스템 초기화
    /// </summary>
    private void InitializeNewSystem()
    {
        try
        {
            // 새 아키텍처 구성요소 생성
            CreateGridComponents();
            
            // 서비스 등록
            RegisterServices();
            
            Debug.Log($"[GridManager] Phase 1 architecture initialized successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridManager] Failed to initialize new system: {ex.Message}");
            // 실패 시 레거시 시스템 유지
        }
    }
    
    /// <summary>
    /// 그리드 컴포넌트들 생성
    /// </summary>
    private void CreateGridComponents()
    {
        // 1. GridState 생성 (데이터 계층)
        var gridStateGO = new GameObject("GridState");
        gridStateGO.transform.SetParent(transform);
        gridState = gridStateGO.AddComponent<GridState>();
        
        // GridState 초기화를 위해 필드 설정 (reflection 사용)
        var gridSizeField = typeof(GridState).GetField("gridSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tileSizeField = typeof(GridState).GetField("tileSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (gridSizeField != null) gridSizeField.SetValue(gridState, new Vector2Int(width, height));
        if (tileSizeField != null) tileSizeField.SetValue(gridState, tileSize);
        
        // 2. GridController 생성 (비즈니스 로직 계층)
        gridController = new GridController(gridState);
        
        // 3. GridRenderer 생성 (프레젠테이션 계층)
        var rendererGO = new GameObject("GridRenderer");
        rendererGO.transform.SetParent(transform);
        gridRenderer = rendererGO.AddComponent<GridRenderer>();
        
        // 4. GridDataBridge 생성 (호환성 계층)
        dataBridge = new GridDataBridge(gridState);
        
        Debug.Log($"[GridManager] Grid components created successfully");
    }
    
    /// <summary>
    /// 서비스 등록
    /// </summary>
    private void RegisterServices()
    {
        var services = new GridServices(gridState, gridController, gridRenderer);
        ServiceLocator.Register<IGridServices>(services);
        ServiceLocator.Register<IGridManager>(gridController);
        ServiceLocator.Register<IReadOnlyGridState>(gridState);
        ServiceLocator.Register<IGridState>(gridState);
        ServiceLocator.Register<IGridController>(gridController);
        ServiceLocator.Register<IGridRenderer>(gridRenderer);
        
        Debug.Log($"[GridManager] Services registered successfully");
    }
    
    // 레거시 메서드들 유지 (새 시스템으로 위임)
    public Tile GetTile(int x, int y)
    {
        return dataBridge?.GetTile(x, y);
    }
    
    public bool IsValidPosition(int x, int y)
    {
        return dataBridge?.IsValidPosition(x, y) ?? (x >= 0 && x < width && y >= 0 && y < height);
    }
    
    public bool CanPlaceUnitAt(int x, int y)
    {
        return dataBridge?.CanPlaceUnitAt(x, y) ?? false;
    }
    
    public bool PlaceUnitAt(int x, int y, Unit unit)
    {
        return dataBridge?.PlaceUnit(x, y, unit) ?? false;
    }
    
    public void RemoveUnitAt(int x, int y)
    {
        dataBridge?.RemoveUnit(x, y);
    }
    
    /// <summary>
    /// 호환성을 위한 Tile 배열 변환
    /// </summary>
    private Tile[,] GetTilesArray()
    {
        if (dataBridge == null) return new Tile[width, height];
        
        return dataBridge.GetTilesArray();
    }
    
    /// <summary>
    /// 기존 GenerateGrid 메서드 유지 (내부적으로 새 시스템 사용)
    /// </summary>
    public void GenerateGrid()
    {
        if (tilePrefab == null)
        {
            CreateDefaultTilePrefab();
        }
        
        if (gridRenderer != null && gridState != null)
        {
            // 새 시스템에서 그리드 생성
            try
            {
                gridRenderer.Initialize(gridState, tilePrefab);
                RegisterTilesWithBridge();
                Debug.Log($"[GridManager] Grid generated using new system: {Width}x{Height} tiles");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GridManager] Failed to generate grid with new system: {ex.Message}");
                GenerateGridLegacy(); // 폴백
            }
        }
        else
        {
            // 폴백: 레거시 시스템 사용
            GenerateGridLegacy();
        }
    }
    
    /// <summary>
    /// 타일들을 브리지에 등록
    /// </summary>
    private void RegisterTilesWithBridge()
    {
        if (gridRenderer == null || dataBridge == null || gridState == null)
            return;
            
        var size = gridState.GridSize;
        
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                var position = new Vector2Int(x, y);
                if (gridRenderer.TryGetTileGameObject(position, out var tileGameObject))
                {
                    var tile = tileGameObject.GetComponent<Tile>();
                    if (tile != null)
                    {
                        dataBridge.RegisterTile(position, tile);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 레거시 그리드 생성 (폴백용)
    /// </summary>
    private void GenerateGridLegacy()
    {
        Debug.LogWarning("[GridManager] Using legacy grid generation as fallback");
        
        var tiles = new Tile[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject tileObject = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                
                Tile tile = tileObject.GetComponent<Tile>();
                if (tile == null)
                {
                    tile = tileObject.AddComponent<Tile>();
                }
                
                tile.Initialize(x, y);
                tiles[x, y] = tile;
                
                // 브리지에도 등록
                dataBridge?.RegisterTile(new Vector2Int(x, y), tile);
            }
        }
        
        Debug.Log($"[GridManager] Legacy grid generated: {width}x{height} tiles");
    }
    
    private void CreateDefaultTilePrefab()
    {
        GameObject defaultTile = GameObject.CreatePrimitive(PrimitiveType.Plane);
        defaultTile.transform.localScale = new Vector3(0.1f, 1, 0.1f);
        defaultTile.GetComponent<Renderer>().material.color = Color.green;
        tilePrefab = defaultTile;
    }
    
    /// <summary>
    /// 새 시스템 접근자 (디버깅 및 고급 사용을 위함)
    /// </summary>
    public IGridState GetGridState() => gridState;
    public IGridController GetGridController() => gridController;
    public IGridRenderer GetGridRenderer() => gridRenderer;
    public GridDataBridge GetDataBridge() => dataBridge;
    
    /// <summary>
    /// 정리 작업
    /// </summary>
    private void OnDestroy()
    {
        // 서비스 등록 해제
        try
        {
            ServiceLocator.UnregisterAll();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GridManager] Error during cleanup: {ex.Message}");
        }
        
        // 브리지 정리
        dataBridge?.Cleanup();
    }
    
    /// <summary>
    /// 에디터 전용 - 컴포넌트 시스템 상태 확인
    /// </summary>
    [ContextMenu("Check System Status")]
    private void CheckSystemStatus()
    {
        Debug.Log("=== GridManager System Status ===");
        Debug.Log($"GridState: {(gridState != null ? "✓" : "✗")} {gridState?.GetType().Name}");
        Debug.Log($"GridController: {(gridController != null ? "✓" : "✗")} {gridController?.GetType().Name}");
        Debug.Log($"GridRenderer: {(gridRenderer != null ? "✓" : "✗")} {gridRenderer?.GetType().Name}");
        Debug.Log($"GridDataBridge: {(dataBridge != null ? "✓" : "✗")} {dataBridge?.GetType().Name}");
        
        if (gridState != null)
        {
            Debug.Log($"Grid Size: {gridState.GridSize}");
            Debug.Log($"Tile Size: {gridState.TileSize}");
            Debug.Log($"Total Tiles: {gridState.TotalTiles}");
            Debug.Log($"Occupied Tiles: {gridState.OccupiedTiles}");
        }
        
        // 서비스 등록 상태 확인
        var services = ServiceLocator.Get<IGridServices>();
        Debug.Log($"Services Registered: {services != null}");
    }
    
    /// <summary>
    /// 에디터 전용 - 새 시스템으로 강제 재초기화
    /// </summary>
    [ContextMenu("Force Reinitialize New System")]
    private void ForceReinitializeNewSystem()
    {
        // 기존 시스템 정리
        OnDestroy();
        
        // 새 시스템 재초기화
        InitializeNewSystem();
        GenerateGrid();
        
        Debug.Log("[GridManager] System reinitialized successfully");
    }
    
    // ============================================================================
    // IGridManager 인터페이스 구현 - Phase 2: Interface-based delegation
    // ============================================================================
    
    #region IGridManager Implementation
    
    /// <summary>
    /// 그리드 크기 (인터페이스 구현)
    /// </summary>
    Vector2Int IGridManager.GridSize => gridController?.GridSize ?? new Vector2Int(width, height);
    
    /// <summary>
    /// 타일 크기 (인터페이스 구현)
    /// </summary>
    float IGridManager.TileSize => gridController?.TileSize ?? tileSize;
    
    /// <summary>
    /// 위치 유효성 검증 (인터페이스 구현)
    /// </summary>
    bool IGridManager.IsValidPosition(Vector2Int gridPosition)
    {
        return gridController?.IsValidPosition(gridPosition) ?? dataBridge?.IsValidPosition(gridPosition.x, gridPosition.y) ?? false;
    }
    
    /// <summary>
    /// 위치 점유 상태 확인 (인터페이스 구현)
    /// </summary>
    bool IGridManager.IsPositionOccupied(Vector2Int gridPosition)
    {
        return gridController?.IsPositionOccupied(gridPosition) ?? false;
    }
    
    /// <summary>
    /// 위치 차단 상태 확인 (인터페이스 구현)
    /// </summary>
    bool IGridManager.IsPositionBlocked(Vector2Int gridPosition)
    {
        return gridController?.IsPositionBlocked(gridPosition) ?? false;
    }
    
    /// <summary>
    /// 위치에 있는 유닛 조회 (인터페이스 구현)
    /// </summary>
    GameObject IGridManager.GetUnitAtPosition(Vector2Int gridPosition)
    {
        return gridController?.GetUnitAtPosition(gridPosition);
    }
    
    /// <summary>
    /// 유닛의 위치 조회 (인터페이스 구현)
    /// </summary>
    Vector2Int IGridManager.GetUnitPosition(GameObject unit)
    {
        return gridController?.GetUnitPosition(unit) ?? Vector2Int.zero;
    }
    
    /// <summary>
    /// 유닛 위치 조회 시도 (인터페이스 구현)
    /// </summary>
    bool IGridManager.TryGetUnitPosition(GameObject unit, out Vector2Int position)
    {
        if (gridController != null)
        {
            return gridController.TryGetUnitPosition(unit, out position);
        }
        position = Vector2Int.zero;
        return false;
    }
    
    /// <summary>
    /// 유닛 이동 가능 여부 확인 (인터페이스 구현)
    /// </summary>
    bool IGridManager.CanMoveUnit(GameObject unit, Vector2Int targetPosition)
    {
        return gridController?.CanMoveUnit(unit, targetPosition) ?? false;
    }
    
    /// <summary>
    /// 유닛 이동 (인터페이스 구현)
    /// </summary>
    bool IGridManager.MoveUnit(GameObject unit, Vector2Int newPosition)
    {
        return gridController?.MoveUnit(unit, newPosition) ?? false;
    }
    
    /// <summary>
    /// 유닛 이동 시도 (인터페이스 구현)
    /// </summary>
    bool IGridManager.TryMoveUnit(GameObject unit, Vector2Int newPosition, out string errorMessage)
    {
        if (gridController != null)
        {
            return gridController.TryMoveUnit(unit, newPosition, out errorMessage);
        }
        errorMessage = "GridController not initialized";
        return false;
    }
    
    /// <summary>
    /// 경로 탐색 (인터페이스 구현)
    /// </summary>
    List<Vector2Int> IGridManager.FindPath(Vector2Int start, Vector2Int end, GameObject movingUnit)
    {
        return gridController?.FindPath(start, end, movingUnit) ?? new List<Vector2Int>();
    }
    
    /// <summary>
    /// 경로 확인 (인터페이스 구현)
    /// </summary>
    bool IGridManager.IsPathClear(Vector2Int start, Vector2Int end, GameObject ignoredUnit)
    {
        return gridController?.IsPathClear(start, end, ignoredUnit) ?? false;
    }
    
    /// <summary>
    /// 경로 거리 계산 (인터페이스 구현)
    /// </summary>
    int IGridManager.GetPathDistance(Vector2Int start, Vector2Int end)
    {
        return gridController?.GetPathDistance(start, end) ?? -1;
    }
    
    /// <summary>
    /// 범위 내 위치 조회 (인터페이스 구현)
    /// </summary>
    List<Vector2Int> IGridManager.GetPositionsInRange(Vector2Int center, int range, bool includeOccupied)
    {
        return gridController?.GetPositionsInRange(center, range, includeOccupied) ?? new List<Vector2Int>();
    }
    
    /// <summary>
    /// 범위 내 유닛 조회 (인터페이스 구현)
    /// </summary>
    List<GameObject> IGridManager.GetUnitsInRange(Vector2Int center, int range)
    {
        return gridController?.GetUnitsInRange(center, range) ?? new List<GameObject>();
    }
    
    /// <summary>
    /// 유효한 이동 위치 조회 (인터페이스 구현)
    /// </summary>
    List<Vector2Int> IGridManager.GetValidMovePositions(GameObject unit, int moveRange)
    {
        return gridController?.GetValidMovePositions(unit, moveRange) ?? new List<Vector2Int>();
    }
    
    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환 (인터페이스 구현)
    /// </summary>
    Vector3 IGridManager.GridToWorldPosition(Vector2Int gridPosition)
    {
        return gridController?.GridToWorldPosition(gridPosition) ?? 
               new Vector3(gridPosition.x * tileSize, 0, gridPosition.y * tileSize);
    }
    
    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환 (인터페이스 구현)
    /// </summary>
    Vector2Int IGridManager.WorldToGridPosition(Vector3 worldPosition)
    {
        return gridController?.WorldToGridPosition(worldPosition) ?? 
               new Vector2Int(Mathf.RoundToInt(worldPosition.x / tileSize), Mathf.RoundToInt(worldPosition.z / tileSize));
    }
    
    /// <summary>
    /// 타일 차단 설정 (인터페이스 구현)
    /// </summary>
    void IGridManager.SetTileBlocked(Vector2Int position, bool blocked)
    {
        gridController?.SetTileBlocked(position, blocked);
    }
    
    /// <summary>
    /// 타일 하이라이트 설정 (인터페이스 구현)
    /// </summary>
    void IGridManager.SetTileHighlight(Vector2Int position, Color highlightColor)
    {
        gridRenderer?.SetTileHighlight(position, highlightColor);
    }
    
    /// <summary>
    /// 모든 하이라이트 제거 (인터페이스 구현)
    /// </summary>
    void IGridManager.ClearAllHighlights()
    {
        gridRenderer?.ClearAllHighlights();
    }
    
    /// <summary>
    /// 유닛 이동 이벤트 (인터페이스 구현)
    /// </summary>
    public event System.Action<GameObject, Vector2Int, Vector2Int> OnUnitMoved
    {
        add { if (gridController != null) gridController.OnUnitMoved += value; }
        remove { if (gridController != null) gridController.OnUnitMoved -= value; }
    }
    
    /// <summary>
    /// 유닛 배치 이벤트 (인터페이스 구현)
    /// </summary>
    public event System.Action<Vector2Int, GameObject> OnUnitPlaced
    {
        add { if (gridController != null) gridController.OnUnitPlaced += value; }
        remove { if (gridController != null) gridController.OnUnitPlaced -= value; }
    }
    
    /// <summary>
    /// 유닛 제거 이벤트 (인터페이스 구현)
    /// </summary>
    public event System.Action<Vector2Int, GameObject> OnUnitRemoved
    {
        add { if (gridController != null) gridController.OnUnitRemoved += value; }
        remove { if (gridController != null) gridController.OnUnitRemoved -= value; }
    }
    
    #endregion
}