using UnityEngine;

public class UnitControllerTest : MonoBehaviour
{
    [SerializeField] private GameObject playerUnitPrefab;
    [SerializeField] private GameObject enemyUnitPrefab;
    
    private GridManager gridManager;
    private UnitController unitController;
    private TurnManager turnManager;
    
    private void Start()
    {
        InitializeManagers();
        CreateTestUnits();
        TestMovementAndCombat();
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
        
        unitController = FindObjectOfType<UnitController>();
        if (unitController == null)
        {
            GameObject controllerObj = new GameObject("UnitController");
            unitController = controllerObj.AddComponent<UnitController>();
        }
    }
    
    private void CreateTestUnits()
    {
        CreateDefaultPrefabs();
        
        Debug.Log("=== Creating Test Units ===");
        
        CreatePlayerUnit(1, 1, "PlayerUnit1", Color.blue);
        CreatePlayerUnit(3, 1, "PlayerUnit2", Color.cyan);
        
        CreateEnemyUnit(1, 4, "EnemyUnit1", Color.red);
        CreateEnemyUnit(3, 4, "EnemyUnit2", Color.magenta);
        
        Debug.Log("Test units created successfully!");
    }
    
    private void CreateDefaultPrefabs()
    {
        if (playerUnitPrefab == null)
        {
            playerUnitPrefab = CreateUnitPrefab("PlayerUnitPrefab", Color.blue, true);
        }
        
        if (enemyUnitPrefab == null)
        {
            enemyUnitPrefab = CreateUnitPrefab("EnemyUnitPrefab", Color.red, false);
        }
    }
    
    private GameObject CreateUnitPrefab(string name, Color color, bool isPlayer)
    {
        GameObject prefab = new GameObject(name);
        Unit unit = prefab.AddComponent<Unit>();
        
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(prefab.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.GetComponent<Renderer>().material.color = color;
        
        return prefab;
    }
    
    private void CreatePlayerUnit(int x, int y, string unitName, Color color)
    {
        if (!gridManager.CanPlaceUnitAt(x, y)) return;
        
        GameObject unitObj = Instantiate(playerUnitPrefab);
        unitObj.name = unitName;
        
        Unit unit = unitObj.GetComponent<Unit>();
        
        Tile tile = gridManager.GetTile(x, y);
        if (tile != null)
        {
            tile.PlaceUnit(unit);
            unitObj.GetComponentInChildren<Renderer>().material.color = color;
            
            Debug.Log($"Created player unit at ({x}, {y}): {unitName}");
        }
    }
    
    private void CreateEnemyUnit(int x, int y, string unitName, Color color)
    {
        if (!gridManager.CanPlaceUnitAt(x, y)) return;
        
        GameObject unitObj = Instantiate(enemyUnitPrefab);
        unitObj.name = unitName;
        
        Unit unit = unitObj.GetComponent<Unit>();
        
        SerializedObjectHelper.SetPrivateField(unit, "isPlayerUnit", false);
        
        Tile tile = gridManager.GetTile(x, y);
        if (tile != null)
        {
            tile.PlaceUnit(unit);
            unitObj.GetComponentInChildren<Renderer>().material.color = color;
            
            Debug.Log($"Created enemy unit at ({x}, {y}): {unitName}");
        }
    }
    
    private void TestMovementAndCombat()
    {
        Debug.Log("=== Movement and Combat Test Instructions ===");
        Debug.Log("1. Player units (blue/cyan) start at bottom");
        Debug.Log("2. Enemy units (red/magenta) start at top");
        Debug.Log("3. Press SPACE to process turns");
        Debug.Log("4. Units will move toward each other");
        Debug.Log("5. When adjacent, they will attack");
        Debug.Log("6. Watch the console for combat logs");
        Debug.Log("=== Test Ready ===");
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CreatePlayerUnit(0, 0, "TestPlayer", Color.green);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CreateEnemyUnit(7, 5, "TestEnemy", Color.yellow);
        }
    }
}

public static class SerializedObjectHelper
{
    public static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}