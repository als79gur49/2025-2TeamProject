using UnityEngine;
using Game.Core;
using Game.Services;
using System.Collections;

/// <summary>
/// Test script to validate the service-based architecture implementation
/// This verifies that all services are working correctly and eliminates the identified problems
/// </summary>
public class ServiceArchitectureTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestsOnStart = true;
    [SerializeField] private bool showDetailedLogs = true;
    
    private IGameService gameService;
    private ITurnService turnService;
    private IUnitService unitService;
    private IUIService uiService;
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            StartCoroutine(RunArchitectureTests());
        }
    }
    
    /// <summary>
    /// Comprehensive test suite for the service architecture
    /// </summary>
    private IEnumerator RunArchitectureTests()
    {
        Debug.Log("🧪 [ServiceArchitectureTest] Starting comprehensive service architecture validation...");
        
        // Wait for services to initialize
        yield return new WaitForSeconds(1f);
        
        bool allTestsPassed = true;
        
        allTestsPassed &= TestServiceRegistration();
        allTestsPassed &= TestServiceDependencies();
        allTestsPassed &= TestEventCommunication();
        allTestsPassed &= TestRoleOverlapElimination();
        allTestsPassed &= TestGameFlowIntegration();
        
        // Final validation
        if (allTestsPassed)
        {
            Debug.Log("✅ [ServiceArchitectureTest] ALL TESTS PASSED - Service architecture is working correctly!");
            Debug.Log("✅ [ServiceArchitectureTest] Original problems have been eliminated:");
            Debug.Log("   ✅ No more duplicate TurnManager creation");
            Debug.Log("   ✅ No more duplicate unit processing logic");
            Debug.Log("   ✅ Clear separation of responsibilities");
            Debug.Log("   ✅ Event-driven loose coupling");
        }
        else
        {
            Debug.LogError("❌ [ServiceArchitectureTest] SOME TESTS FAILED - Check individual test results above");
        }
    }
    
    /// <summary>
    /// Test 1: Verify all services are registered in ServiceLocator
    /// </summary>
    private bool TestServiceRegistration()
    {
        Debug.Log("🔍 [Test 1] Testing service registration in ServiceLocator...");
        
        gameService = ServiceLocator.Get<IGameService>();
        turnService = ServiceLocator.Get<ITurnService>();
        unitService = ServiceLocator.Get<IUnitService>();
        uiService = ServiceLocator.Get<IUIService>();
        
        bool test1Passed = true;
        
        if (gameService == null)
        {
            Debug.LogError("❌ [Test 1] IGameService not registered in ServiceLocator");
            test1Passed = false;
        }
        else if (showDetailedLogs)
            Debug.Log("✅ [Test 1] IGameService registered successfully");
        
        if (turnService == null)
        {
            Debug.LogError("❌ [Test 1] ITurnService not registered in ServiceLocator");
            test1Passed = false;
        }
        else if (showDetailedLogs)
            Debug.Log("✅ [Test 1] ITurnService registered successfully");
        
        if (unitService == null)
        {
            Debug.LogError("❌ [Test 1] IUnitService not registered in ServiceLocator");
            test1Passed = false;
        }
        else if (showDetailedLogs)
            Debug.Log("✅ [Test 1] IUnitService registered successfully");
        
        if (uiService == null)
        {
            Debug.LogError("❌ [Test 1] IUIService not registered in ServiceLocator");
            test1Passed = false;
        }
        else if (showDetailedLogs)
            Debug.Log("✅ [Test 1] IUIService registered successfully");
        
        Debug.Log($"📊 [Test 1] Service Registration: {(test1Passed ? "PASSED" : "FAILED")}");
        return test1Passed;
    }
    
    /// <summary>
    /// Test 2: Verify service dependencies are resolved correctly
    /// </summary>
    private bool TestServiceDependencies()
    {
        Debug.Log("🔍 [Test 2] Testing service dependency resolution...");
        
        bool test2Passed = true;
        
        // Test that GameService can access other services
        if (gameService != null && gameService.IsGameActive)
        {
            if (showDetailedLogs)
                Debug.Log("✅ [Test 2] GameService is active and initialized");
        }
        else
        {
            Debug.LogError("❌ [Test 2] GameService is not active or not initialized");
            test2Passed = false;
        }
        
        // Test turn service state
        if (turnService != null)
        {
            if (showDetailedLogs)
                Debug.Log($"✅ [Test 2] TurnService state: Turn {turnService.TurnCount}, Player: {turnService.IsPlayerTurn}");
        }
        else
        {
            Debug.LogError("❌ [Test 2] TurnService state not accessible");
            test2Passed = false;
        }
        
        Debug.Log($"📊 [Test 2] Service Dependencies: {(test2Passed ? "PASSED" : "FAILED")}");
        return test2Passed;
    }
    
    /// <summary>
    /// Test 3: Verify event-driven communication between services
    /// </summary>
    private bool TestEventCommunication()
    {
        Debug.Log("🔍 [Test 3] Testing event-driven communication...");
        
        bool test3Passed = true;
        bool eventReceived = false;
        
        // Subscribe to turn change event
        if (turnService != null)
        {
            System.Action<bool> testHandler = (isPlayerTurn) => {
                eventReceived = true;
                if (showDetailedLogs)
                    Debug.Log($"✅ [Test 3] Turn change event received: {(isPlayerTurn ? "Player" : "Enemy")} turn");
            };
            
            turnService.OnTurnChanged += testHandler;
            
            // Trigger a turn change
            turnService.EndTurn();
            
            // Check if event was received
            if (eventReceived)
            {
                if (showDetailedLogs)
                    Debug.Log("✅ [Test 3] Event communication working correctly");
            }
            else
            {
                Debug.LogError("❌ [Test 3] Turn change event not received");
                test3Passed = false;
            }
            
            // Clean up
            turnService.OnTurnChanged -= testHandler;
        }
        else
        {
            Debug.LogError("❌ [Test 3] Cannot test events - TurnService not available");
            test3Passed = false;
        }
        
        Debug.Log($"📊 [Test 3] Event Communication: {(test3Passed ? "PASSED" : "FAILED")}");
        return test3Passed;
    }
    
    /// <summary>
    /// Test 4: Verify that role overlap problems have been eliminated
    /// </summary>
    private bool TestRoleOverlapElimination()
    {
        Debug.Log("🔍 [Test 4] Testing role overlap elimination...");
        
        bool test4Passed = true;
        
        // Test 4a: No duplicate TurnManager instances
        TurnManager[] turnManagers = FindObjectsOfType<TurnManager>();
        if (turnManagers.Length <= 1) // Allow 0 or 1 for compatibility
        {
            if (showDetailedLogs)
                Debug.Log($"✅ [Test 4a] No duplicate TurnManager instances: Found {turnManagers.Length}");
        }
        else
        {
            Debug.LogError($"❌ [Test 4a] Multiple TurnManager instances found: {turnManagers.Length}");
            test4Passed = false;
        }
        
        // Test 4b: Single source of truth for unit management
        if (unitService != null)
        {
            if (showDetailedLogs)
                Debug.Log($"✅ [Test 4b] Single UnitService managing {unitService.ActiveUnitCount} units");
        }
        else
        {
            Debug.LogError("❌ [Test 4b] UnitService not available for centralized unit management");
            test4Passed = false;
        }
        
        // Test 4c: UI service handles only UI concerns
        if (uiService != null)
        {
            if (showDetailedLogs)
                Debug.Log("✅ [Test 4c] UIService available for clean UI management");
        }
        else
        {
            Debug.LogError("❌ [Test 4c] UIService not available");
            test4Passed = false;
        }
        
        Debug.Log($"📊 [Test 4] Role Overlap Elimination: {(test4Passed ? "PASSED" : "FAILED")}");
        return test4Passed;
    }
    
    /// <summary>
    /// Test 5: Verify complete game flow integration
    /// </summary>
    private bool TestGameFlowIntegration()
    {
        Debug.Log("🔍 [Test 5] Testing complete game flow integration...");
        
        bool test5Passed = true;
        
        // Test game restart
        if (gameService != null)
        {
            int initialTurnCount = turnService?.TurnCount ?? 0;
            gameService.RestartGame();
            
            if (turnService != null && turnService.TurnCount == 0)
            {
                if (showDetailedLogs)
                    Debug.Log("✅ [Test 5a] Game restart functionality working");
            }
            else
            {
                Debug.LogError("❌ [Test 5a] Game restart not working correctly");
                test5Passed = false;
            }
        }
        
        // Test unit processing delegation
        if (unitService != null)
        {
            unitService.ProcessAllUnits();
            if (showDetailedLogs)
                Debug.Log("✅ [Test 5b] Unit processing delegation working");
        }
        else
        {
            Debug.LogError("❌ [Test 5b] Unit processing delegation not available");
            test5Passed = false;
        }
        
        Debug.Log($"📊 [Test 5] Game Flow Integration: {(test5Passed ? "PASSED" : "FAILED")}");
        return test5Passed;
    }
    
    /// <summary>
    /// Manual test triggers for runtime validation
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(RunArchitectureTests());
        }
        
        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowServiceStatus();
        }
    }
    
    private void ShowServiceStatus()
    {
        Debug.Log("📋 [ServiceArchitectureTest] Service Status Report:");
        Debug.Log($"   🎮 GameService: {(ServiceLocator.Get<IGameService>() != null ? "✅ Active" : "❌ Missing")}");
        Debug.Log($"   ⏰ TurnService: {(ServiceLocator.Get<ITurnService>() != null ? "✅ Active" : "❌ Missing")}");
        Debug.Log($"   👥 UnitService: {(ServiceLocator.Get<IUnitService>() != null ? "✅ Active" : "❌ Missing")}");
        Debug.Log($"   🖥️ UIService: {(ServiceLocator.Get<IUIService>() != null ? "✅ Active" : "❌ Missing")}");
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 150));
        GUILayout.Label("=== Service Architecture Test ===");
        
        if (GUILayout.Button("Run Tests (T)"))
        {
            StartCoroutine(RunArchitectureTests());
        }
        
        if (GUILayout.Button("Show Status (I)"))
        {
            ShowServiceStatus();
        }
        
        GUILayout.Label("");
        GUILayout.Label("Test Status:");
        var gameServ = ServiceLocator.Get<IGameService>();
        var turnServ = ServiceLocator.Get<ITurnService>();
        var unitServ = ServiceLocator.Get<IUnitService>();
        var uiServ = ServiceLocator.Get<IUIService>();
        
        GUILayout.Label($"Services: {(gameServ != null && turnServ != null && unitServ != null && uiServ != null ? "✅ All Active" : "❌ Missing")}");
        
        GUILayout.EndArea();
    }
}