using UnityEngine;

public class GridManagerTest : MonoBehaviour
{
    private GridManager gridManager;
    private Unit testUnit;
    
    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            GameObject gridObj = new GameObject("GridManager");
            gridManager = gridObj.AddComponent<GridManager>();
        }
        
        TestGridGeneration();
        CreateTestUnit();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    private void TestGridGeneration()
    {
        Debug.Log("=== Grid Manager Test Started ===");
        Debug.Log($"Grid Size: {gridManager.Width}x{gridManager.Height}");
        
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTile(x, y);
                if (tile == null)
                {
                    Debug.LogError($"Tile at ({x}, {y}) is null!");
                }
            }
        }
        
        Debug.Log("Grid generation test completed");
        Debug.Log("Controls:");
        Debug.Log("- Q: Place test unit at (2,2)");
        Debug.Log("- W: Move test unit to (4,3)");
        Debug.Log("- E: Remove test unit");
        Debug.Log("- R: Test invalid positions");
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