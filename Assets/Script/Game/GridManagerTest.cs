using UnityEngine;
using Game.Interfaces;
using Game.Core;

public class GridManagerTest : MonoBehaviour
{
    // Phase 2: Interface-based dependencies
    [Inject(Required = false)]
    private IGridManager gridManager;
    
    [Inject(Required = false)]
    private IGridServices gridServices;
    
    private GridManager legacyGridManager; // 호환성을 위해 유지
    private Unit testUnit;
    
    void Start()
    {
        InitializeDependencies();
        TestGridGeneration();
        CreateTestUnit();
    }
    
    /// <summary>
    /// Phase 2: ServiceLocator 기반 의존성 주입
    /// </summary>
    private void InitializeDependencies()
    {
        // ServiceLocator를 통한 의존성 주입
        this.InjectDependencies();
        
        // Fallback: 서비스 로케이터에서 직접 조회
        if (gridManager == null)
        {
            gridManager = ServiceLocator.Get<IGridManager>();
        }
        
        if (gridServices == null)
        {
            gridServices = ServiceLocator.Get<IGridServices>();
        }
        
        // 레거시 그리드 매니저 참조 (테스트용)
        legacyGridManager = FindObjectOfType<GridManager>();
        if (legacyGridManager == null && gridManager == null)
        {
            GameObject gridObj = new GameObject("GridManager");
            legacyGridManager = gridObj.AddComponent<GridManager>();
            
            // 새로 생성된 GridManager는 IGridManager도 구현하므로
            gridManager = legacyGridManager;
        }
        
        Debug.Log($"[GridManagerTest] Dependencies initialized - GridManager: {gridManager != null}, GridServices: {gridServices != null}");
    }
    
    void Update()
    {
        HandleInput();
    }
    
    private void TestGridGeneration()
    {
        Debug.Log("=== Grid Manager Phase 2 Test Started ===");
        
        // Phase 2: Interface-based testing
        if (gridManager != null)
        {
            var gridSize = gridManager.GridSize;
            Debug.Log($"Grid Size (Interface): {gridSize.x}x{gridSize.y}");
            
            // Test interface methods
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (!gridManager.IsValidPosition(position))
                    {
                        Debug.LogError($"Position ({x}, {y}) is not valid according to interface!");
                    }
                }
            }
        }
        
        // Legacy system testing for comparison
        if (legacyGridManager != null)
        {
            Debug.Log($"Grid Size (Legacy): {legacyGridManager.Width}x{legacyGridManager.Height}");
            
            for (int x = 0; x < legacyGridManager.Width; x++)
            {
                for (int y = 0; y < legacyGridManager.Height; y++)
                {
                    Tile tile = legacyGridManager.GetTile(x, y);
                    if (tile == null)
                    {
                        Debug.LogError($"Tile at ({x}, {y}) is null in legacy system!");
                    }
                }
            }
        }
        
        Debug.Log("Grid generation test completed");
        Debug.Log("Phase 2 Controls:");
        Debug.Log("- Q: Place test unit at (2,2) using interface");
        Debug.Log("- W: Move test unit to (4,3) using interface");
        Debug.Log("- E: Remove test unit using interface");
        Debug.Log("- R: Test invalid positions using interface");
        Debug.Log("- T: Test GridServices functionality");
    }
    
    private void CreateTestUnit()
    {
        GameObject unitObj = new GameObject("TestGridUnit");
        unitObj.transform.position = new Vector3(-5, 1, -5);
        testUnit = unitObj.AddComponent<Unit>();
        
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(unitObj.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.GetComponent<Renderer>().material.color = Color.cyan;
    }
    
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TestPlaceUnit(2, 2);
        }
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            TestMoveUnit(4, 3);
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            TestRemoveUnit();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            TestInvalidPositions();
        }
    }
    
    private void TestPlaceUnit(int x, int y)
    {
        if (testUnit == null)
        {
            Debug.LogWarning("Test unit is null!");
            return;
        }
        
        if (gridManager.CanPlaceUnitAt(x, y))
        {
            bool success = gridManager.PlaceUnitAt(x, y, testUnit);
            Debug.Log($"Placing unit at ({x}, {y}): {(success ? "Success" : "Failed")}");
        }
        else
        {
            Debug.Log($"Cannot place unit at ({x}, {y}) - position occupied or invalid");
        }
    }
    
    private void TestMoveUnit(int newX, int newY)
    {
        if (testUnit == null) return;
        
        Tile currentTile = FindTileWithUnit(testUnit);
        if (currentTile != null)
        {
            currentTile.RemoveUnit();
            Debug.Log($"Removed unit from ({currentTile.X}, {currentTile.Y})");
        }
        
        TestPlaceUnit(newX, newY);
    }
    
    private void TestRemoveUnit()
    {
        if (testUnit == null) return;
        
        Tile currentTile = FindTileWithUnit(testUnit);
        if (currentTile != null)
        {
            currentTile.RemoveUnit();
            testUnit.transform.position = new Vector3(-5, 1, -5);
            Debug.Log($"Unit removed from ({currentTile.X}, {currentTile.Y})");
        }
        else
        {
            Debug.Log("Unit not found on any tile");
        }
    }
    
    private void TestInvalidPositions()
    {
        Debug.Log("Testing invalid positions:");
        
        int[] testX = { -1, gridManager.Width, 0, gridManager.Width - 1 };
        int[] testY = { -1, gridManager.Height, 0, gridManager.Height - 1 };
        
        for (int i = 0; i < testX.Length; i++)
        {
            bool isValid = gridManager.IsValidPosition(testX[i], testY[i]);
            Debug.Log($"Position ({testX[i]}, {testY[i]}): {(isValid ? "Valid" : "Invalid")}");
        }
    }
    
    private Tile FindTileWithUnit(Unit unit)
    {
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTile(x, y);
                if (tile != null && tile.OccupyingUnit == unit)
                {
                    return tile;
                }
            }
        }
        return null;
    }
}