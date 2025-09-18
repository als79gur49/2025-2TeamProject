using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.Core;

public class UnitController : MonoBehaviour
{
    // Phase 2: Interface-based dependencies
    [Inject(Required = false)]
    private IGridManager gridManager;
    
    [Inject(Required = false)] 
    private IGridServices gridServices;
    
    private TurnManager turnManager;
    private List<Unit> allUnits = new List<Unit>();
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        // Phase 2: ServiceLocator-based dependency injection
        InitializeDependencies();
        
        turnManager = FindObjectOfType<TurnManager>();
        
        if (turnManager == null)
        {
            GameObject turnObj = new GameObject("TurnManager");
            turnManager = turnObj.AddComponent<TurnManager>();
            turnManager.StartGame();
        }
        
        InvokeRepeating(nameof(UpdateUnitsList), 1f, 1f);
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
            if (gridManager == null)
            {
                Debug.LogWarning($"[UnitController] IGridManager not found in ServiceLocator. Falling back to FindObjectOfType.");
                var legacyGridManager = FindObjectOfType<GridManager>();
                gridManager = legacyGridManager; // GridManager는 IGridManager를 구현해야 함
            }
        }
        
        // Grid services 조회
        if (gridServices == null)
        {
            gridServices = ServiceLocator.Get<IGridServices>();
        }
        
        Debug.Log($"[UnitController] Dependencies initialized - GridManager: {gridManager != null}, GridServices: {gridServices != null}");
    }
    
    private void Update()
    {
        HandleTestInput();
    }
    
    private void HandleTestInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ProcessTurn();
        }
        
        if (Input.GetKeyDown(KeyCode.U))
        {
            ProcessAllUnitsAction();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (turnManager != null)
            {
                turnManager.StartGame();
                Debug.Log("Game restarted!");
            }
        }
    }
    
    private void ProcessTurn()
    {
        if (turnManager == null) return;
        
        Debug.Log($"=== Processing Turn {turnManager.TurnCount + 1} ===");
        
        if (turnManager.IsPlayerTurn)
        {
            ProcessPlayerUnits();
        }
        else
        {
            ProcessEnemyUnits();
        }
        
        turnManager.EndTurn();
        Debug.Log($"=== Turn {turnManager.TurnCount} Complete ===");
    }
    
    private void ProcessPlayerUnits()
    {
        Debug.Log("Processing Player Units Actions...");
        
        foreach (Unit unit in allUnits)
        {
            if (unit != null && unit.IsAlive && unit.IsPlayerUnit)
            {
                unit.OnTurnStart();
            }
        }
    }
    
    private void ProcessEnemyUnits()
    {
        Debug.Log("Processing Enemy Units Actions...");
        
        foreach (Unit unit in allUnits)
        {
            if (unit != null && unit.IsAlive && !unit.IsPlayerUnit)
            {
                unit.OnTurnStart();
            }
        }
    }
    
    private void ProcessAllUnitsAction()
    {
        Debug.Log("=== Processing All Units Action ===");
        
        foreach (Unit unit in allUnits)
        {
            if (unit != null && unit.IsAlive)
            {
                unit.OnTurnStart();
            }
        }
        
        Debug.Log("=== All Units Processed ===");
    }
    
    private void UpdateUnitsList()
    {
        allUnits.Clear();
        Unit[] foundUnits = FindObjectsOfType<Unit>();
        
        foreach (Unit unit in foundUnits)
        {
            if (unit.IsAlive)
            {
                allUnits.Add(unit);
            }
        }
    }
    
    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
        }
    }
    
    public void UnregisterUnit(Unit unit)
    {
        allUnits.Remove(unit);
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 250, 300, 200));
        GUILayout.Label("=== Unit Controller ===");
        GUILayout.Label($"Current Turn: {(turnManager != null && turnManager.IsPlayerTurn ? "Player" : "Enemy")}");
        GUILayout.Label($"Turn Count: {(turnManager != null ? turnManager.TurnCount : 0)}");
        GUILayout.Label($"Active Units: {allUnits.Count}");
        
        int playerUnits = 0, enemyUnits = 0;
        foreach (Unit unit in allUnits)
        {
            if (unit != null && unit.IsAlive)
            {
                if (unit.IsPlayerUnit) playerUnits++;
                else enemyUnits++;
            }
        }
        
        GUILayout.Label($"Player Units: {playerUnits}");
        GUILayout.Label($"Enemy Units: {enemyUnits}");
        GUILayout.Label("");
        GUILayout.Label("Controls:");
        GUILayout.Label("SPACE - Process Turn");
        GUILayout.Label("U - All Units Act");
        GUILayout.Label("R - Restart Game");
        GUILayout.EndArea();
    }
}