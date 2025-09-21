using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.Core;
using Game.Services;

/// <summary>
/// REFACTORED UnitController - Demonstrates how to remove redundant responsibilities
/// This shows the migration path from legacy UnitController to service-based architecture
/// Recommendation: Use UnitService + GameService instead of this refactored version for new development
/// </summary>
public class UnitControllerLegacyRefactored : MonoBehaviour
{
    // ✅ KEEP: Grid-related dependencies (this is the legitimate responsibility)
    [Inject(Required = false)]
    private IGridManager gridManager;
    
    [Inject(Required = false)] 
    private IGridServices gridServices;
    
    // ✅ REFACTOR: Use service interfaces instead of concrete managers
    private ITurnService turnService;
    private IUnitService unitService;
    private IGameService gameService;
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        InitializeGridDependencies();
        InitializeGameServices();
    }
    
    /// <summary>
    /// ✅ KEEP: Grid-related dependency injection (legitimate responsibility)
    /// </summary>
    private void InitializeGridDependencies()
    {
        this.InjectDependencies();
        
        if (gridManager == null)
        {
            gridManager = ServiceLocator.Get<IGridManager>();
            if (gridManager == null)
            {
                Debug.LogError($"[UnitControllerRefactored] IGridManager not found in ServiceLocator");
            }
        }
        
        if (gridServices == null)
        {
            gridServices = ServiceLocator.Get<IGridServices>();
            if (gridServices == null)
            {
                Debug.LogError($"[UnitControllerRefactored] IGridServices not found in ServiceLocator");
            }
        }
        
        Debug.Log($"[UnitControllerRefactored] Grid dependencies initialized - GridManager: {gridManager != null}, GridServices: {gridServices != null}");
    }
    
    /// <summary>
    /// ✅ REFACTORED: Remove TurnManager creation, use service architecture
    /// </summary>
    private void InitializeGameServices()
    {
        // Get services from ServiceLocator instead of creating managers
        turnService = ServiceLocator.Get<ITurnService>();
        unitService = ServiceLocator.Get<IUnitService>();
        gameService = ServiceLocator.Get<IGameService>();
        
        if (turnService == null)
            Debug.LogError("[UnitControllerRefactored] ITurnService not found - ensure GameServiceManager is initialized");
        if (unitService == null)
            Debug.LogError("[UnitControllerRefactored] IUnitService not found - ensure GameServiceManager is initialized");
        if (gameService == null)
            Debug.LogError("[UnitControllerRefactored] IGameService not found - ensure GameServiceManager is initialized");
    }
    
    private void Update()
    {
        // ✅ REFACTORED: Keep only input handling, delegate game logic to services
        HandleTestInput();
    }
    
    /// <summary>
    /// ✅ REFACTORED: Simplified input handling - delegate to services instead of duplicating logic
    /// </summary>
    private void HandleTestInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // ✅ DELEGATE: Let GameService handle turn processing
            Debug.Log("[UnitControllerRefactored] Delegating turn processing to GameService");
            // The GameService will coordinate with TurnService and UnitService
        }
        
        if (Input.GetKeyDown(KeyCode.U))
        {
            // ✅ DELEGATE: Let UnitService handle unit processing
            Debug.Log("[UnitControllerRefactored] Delegating unit processing to UnitService");
            unitService?.ProcessAllUnits();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            // ✅ DELEGATE: Let GameService handle game restart
            Debug.Log("[UnitControllerRefactored] Delegating game restart to GameService");
            gameService?.RestartGame();
        }
    }
    
    /// <summary>
    /// ✅ ADAPTER: Provide compatibility methods that delegate to UnitService
    /// These methods maintain the original API while delegating to the service architecture
    /// </summary>
    public void RegisterUnit(Unit unit)
    {
        Debug.Log($"[UnitControllerRefactored] Delegating unit registration to UnitService: {unit?.name}");
        unitService?.RegisterUnit(unit);
    }
    
    public void UnregisterUnit(Unit unit)
    {
        Debug.Log($"[UnitControllerRefactored] Delegating unit unregistration to UnitService: {unit?.name}");
        unitService?.UnregisterUnit(unit);
    }
    
    /// <summary>
    /// ✅ SIMPLIFIED: Grid-related functionality remains (legitimate responsibility)
    /// This is where UnitController should focus - grid interactions, positioning, etc.
    /// </summary>
    public void MoveUnitOnGrid(Unit unit, Vector2Int targetPosition)
    {
        if (gridManager == null || gridServices == null) 
        {
            Debug.LogWarning("[UnitControllerRefactored] Grid services not available for movement");
            return;
        }
        
        // This is legitimate UnitController responsibility - grid interactions
        Debug.Log($"[UnitControllerRefactored] Moving unit {unit?.name} to {targetPosition} using grid services");
        // Implementation would use gridManager and gridServices
    }
    
    public bool CanUnitMoveToPosition(Unit unit, Vector2Int targetPosition)
    {
        if (gridManager == null) return false;
        
        // This is legitimate UnitController responsibility - grid validation
        Debug.Log($"[UnitControllerRefactored] Checking if unit {unit?.name} can move to {targetPosition}");
        // Implementation would use gridManager to validate movement
        return true; // Simplified for demonstration
    }
    
    /// <summary>
    /// ✅ REFACTORED: Debug UI showing delegation to services
    /// </summary>
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 250, 350, 200));
        GUILayout.Label("=== Unit Controller (Refactored) ===");
        
        // Show service status instead of managing state directly
        GUILayout.Label($"Turn Service: {(turnService != null ? "✅" : "❌")}");
        GUILayout.Label($"Unit Service: {(unitService != null ? "✅" : "❌")}");
        GUILayout.Label($"Game Service: {(gameService != null ? "✅" : "❌")}");
        
        if (turnService != null)
            GUILayout.Label($"Current Turn: {(turnService.IsPlayerTurn ? "Player" : "Enemy")}");
        if (turnService != null)
            GUILayout.Label($"Turn Count: {turnService.TurnCount}");
        if (unitService != null)
            GUILayout.Label($"Active Units: {unitService.ActiveUnitCount}");
        
        GUILayout.Label("");
        GUILayout.Label("Grid Services:");
        GUILayout.Label($"Grid Manager: {(gridManager != null ? "✅" : "❌")}");
        GUILayout.Label($"Grid Services: {(gridServices != null ? "✅" : "❌")}");
        
        GUILayout.Label("");
        GUILayout.Label("Controls:");
        GUILayout.Label("SPACE - Delegate Turn Processing");
        GUILayout.Label("U - Delegate Unit Processing");
        GUILayout.Label("R - Delegate Game Restart");
        GUILayout.EndArea();
    }
}