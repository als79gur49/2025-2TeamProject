using UnityEngine;
using System.Collections.Generic;

public class UnitController : MonoBehaviour
{
    private GridManager gridManager;
    private TurnManager turnManager;
    private List<Unit> allUnits = new List<Unit>();
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        gridManager = FindObjectOfType<GridManager>();
        turnManager = FindObjectOfType<TurnManager>();
        
        if (turnManager == null)
        {
            GameObject turnObj = new GameObject("TurnManager");
            turnManager = turnObj.AddComponent<TurnManager>();
            turnManager.StartGame();
        }
        
        InvokeRepeating(nameof(UpdateUnitsList), 1f, 1f);
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