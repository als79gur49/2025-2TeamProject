using UnityEngine;

public class UIManagerTest : MonoBehaviour
{
    private UIManager uiManager;
    private TurnManager turnManager;
    private GridManager gridManager;
    private UnitController unitController;
    
    private void Start()
    {
        InitializeAllSystems();
        CreateTestUnits();
        TestUISystem();
    }
    
    private void InitializeAllSystems()
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
        
        unitController = FindObjectOfType<UnitController>();
        if (unitController == null)
        {
            GameObject controllerObj = new GameObject("UnitController");
            unitController = controllerObj.AddComponent<UnitController>();
        }
        
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            GameObject uiObj = new GameObject("UIManager");
            uiManager = uiObj.AddComponent<UIManager>();
        }
        
        uiManager.SetTurnManager(turnManager);
        uiManager.SetUnitController(unitController);
    }
    
    private void CreateTestUnits()
    {
        Debug.Log("=== Creating test units for UI test ===");
        
        CreateTestUnit(2, 1, "PlayerUnit", Color.blue, true);
        CreateTestUnit(4, 1, "PlayerUnit2", Color.cyan, true);
        
        CreateTestUnit(2, 4, "EnemyUnit", Color.red, false);
        CreateTestUnit(4, 4, "EnemyUnit2", Color.magenta, false);
        
        Debug.Log("Test units created for UI testing");
    }
    
    private void CreateTestUnit(int x, int y, string unitName, Color color, bool isPlayer)
    {
        if (!gridManager.CanPlaceUnitAt(x, y)) return;
        
        GameObject unitObj = new GameObject(unitName);
        Unit unit = unitObj.AddComponent<Unit>();
        
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(unitObj.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.GetComponent<Renderer>().material.color = color;
        
        if (!isPlayer)
        {
            var field = typeof(Unit).GetField("isPlayerUnit", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(unit, false);
            }
        }
        
        Tile tile = gridManager.GetTile(x, y);
        if (tile != null)
        {
            tile.PlaceUnit(unit);
        }
    }
    
    private void TestUISystem()
    {
        Debug.Log("=== UI System Test Started ===");
        Debug.Log("UI Elements that should appear:");
        Debug.Log("1. 'End Turn' button (bottom-right corner)");
        Debug.Log("2. Turn status text (top-center)");
        Debug.Log("3. Button color changes based on current turn");
        Debug.Log("4. Text color changes: Cyan (Player), Red (Enemy)");
        Debug.Log("");
        Debug.Log("Test Controls:");
        Debug.Log("- Click 'End Turn' button to switch turns");
        Debug.Log("- Press ENTER/RETURN to end turn via keyboard");
        Debug.Log("- Watch units move/attack when turn ends");
        Debug.Log("- Button shows 'End Player Turn' or 'End Enemy Turn'");
        Debug.Log("=== UI Test Instructions Complete ===");
    }
    
    private void Update()
    {
        HandleTestInput();
    }
    
    private void HandleTestInput()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestTurnSwitching();
        }
        
        if (Input.GetKeyDown(KeyCode.U))
        {
            TestUIUpdates();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }
    
    private void TestTurnSwitching()
    {
        Debug.Log("=== Testing Turn Switching ===");
        Debug.Log($"Before: Turn {turnManager.TurnCount}, {(turnManager.IsPlayerTurn ? "Player" : "Enemy")} turn");
        
        for (int i = 0; i < 3; i++)
        {
            turnManager.EndTurn();
            Debug.Log($"After EndTurn {i+1}: Turn {turnManager.TurnCount}, {(turnManager.IsPlayerTurn ? "Player" : "Enemy")} turn");
        }
        
        Debug.Log("=== Turn Switching Test Complete ===");
    }
    
    private void TestUIUpdates()
    {
        Debug.Log("=== Testing UI Updates ===");
        
        if (uiManager != null)
        {
            Debug.Log("UI Manager is active");
            Debug.Log($"Current turn display should show: Turn {turnManager.TurnCount}");
            Debug.Log($"Button should show: End {(turnManager.IsPlayerTurn ? "Player" : "Enemy")} Turn");
            Debug.Log($"Text color should be: {(turnManager.IsPlayerTurn ? "Cyan" : "Red")}");
        }
        else
        {
            Debug.LogError("UI Manager not found!");
        }
        
        Debug.Log("=== UI Updates Test Complete ===");
    }
    
    private void RestartGame()
    {
        Debug.Log("=== Restarting Game ===");
        
        if (turnManager != null)
        {
            turnManager.StartGame();
        }
        
        Debug.Log("Game restarted - should be Player Turn 0");
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 450, 350, 150));
        GUILayout.Label("=== UI Manager Test ===");
        
        if (turnManager != null)
        {
            GUILayout.Label($"Current Turn: {turnManager.TurnCount}");
            GUILayout.Label($"Active Player: {(turnManager.IsPlayerTurn ? "Player" : "Enemy")}");
        }
        
        if (uiManager != null)
        {
            GUILayout.Label("UI Manager: Active");
        }
        else
        {
            GUILayout.Label("UI Manager: Missing!");
        }
        
        GUILayout.Label("");
        GUILayout.Label("Test Controls:");
        GUILayout.Label("T - Test Turn Switching");
        GUILayout.Label("U - Test UI Updates");
        GUILayout.Label("R - Restart Game");
        
        GUILayout.EndArea();
    }
}