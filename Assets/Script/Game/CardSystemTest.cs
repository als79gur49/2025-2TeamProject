using UnityEngine;

public class CardSystemTest : MonoBehaviour
{
    private HandManager handManager;
    private InputManager inputManager;
    private GridManager gridManager;
    private TurnManager turnManager;
    
    void Start()
    {
        InitializeManagers();
        TestCardSystem();
    }
    
    void Update()
    {
        HandleTestInput();
    }
    
    private void InitializeManagers()
    {
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            GameObject gridObj = new GameObject("GridManager");
            gridManager = gridObj.AddComponent<GridManager>();
        }
        
        turnManager = FindObjectOfType<TurnManager>();
        if (turnManager == null)
        {
            GameObject turnObj = new GameObject("TurnManager");
            turnManager = turnObj.AddComponent<TurnManager>();
            turnManager.StartGame();
        }
        
        handManager = FindObjectOfType<HandManager>();
        if (handManager == null)
        {
            GameObject handObj = new GameObject("HandManager");
            handManager = handObj.AddComponent<HandManager>();
        }
        
        inputManager = FindObjectOfType<InputManager>();
        if (inputManager == null)
        {
            GameObject inputObj = new GameObject("InputManager");
            inputManager = inputObj.AddComponent<InputManager>();
        }
    }
    
    private void TestCardSystem()
    {
        Debug.Log("=== Card System Test Started ===");
        Debug.Log("1. Grid should be generated automatically");
        Debug.Log("2. UI button 'Place Unit' should appear on screen");
        Debug.Log("3. Click the button to select a card");
        Debug.Log("4. Click on empty tiles to place units");
        Debug.Log("5. Try clicking occupied tiles (should fail)");
        Debug.Log("");
        Debug.Log("Test Controls:");
        Debug.Log("- T: Test automatic unit placement");
        Debug.Log("- C: Clear all units from grid");
        Debug.Log("- P: Toggle player turn");
        Debug.Log("=== Instructions Complete ===");
    }
    
    private void HandleTestInput()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestAutomaticPlacement();
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearAllUnits();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePlayerTurn();
        }
    }
    
    private void TestAutomaticPlacement()
    {
        Debug.Log("Testing automatic unit placement...");
        
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                Tile tile = gridManager.GetTile(x, y);
                if (tile != null && tile.CanPlaceUnit())
                {
                    bool success = handManager.TryPlaceUnit(tile);
                    if (success)
                    {
                        Debug.Log($"Auto-placed unit at ({x}, {y})");
                    }
                }
            }
        }
    }
    
    private void ClearAllUnits()
    {
        Debug.Log("Clearing all units from grid...");
        
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTile(x, y);
                if (tile != null && tile.IsOccupied)
                {
                    Unit unit = tile.OccupyingUnit;
                    tile.RemoveUnit();
                    if (unit != null)
                    {
                        Destroy(unit.gameObject);
                    }
                }
            }
        }
        
        Debug.Log("All units cleared!");
    }
    
    private void TogglePlayerTurn()
    {
        if (turnManager != null)
        {
            turnManager.EndTurn();
            Debug.Log($"Turn switched. Current turn: {(turnManager.IsPlayerTurn ? "Player" : "Enemy")}");
        }
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Card System Status:");
        GUILayout.Label($"Current Turn: {(turnManager != null && turnManager.IsPlayerTurn ? "Player" : "Enemy")}");
        GUILayout.Label($"Card Selected: {(handManager != null && handManager.IsCardSelected ? "Yes" : "No")}");
        
        int occupiedTiles = CountOccupiedTiles();
        GUILayout.Label($"Units on Grid: {occupiedTiles}");
        
        GUILayout.EndArea();
    }
    
    private int CountOccupiedTiles()
    {
        if (gridManager == null) return 0;
        
        int count = 0;
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTile(x, y);
                if (tile != null && tile.IsOccupied)
                {
                    count++;
                }
            }
        }
        return count;
    }
}