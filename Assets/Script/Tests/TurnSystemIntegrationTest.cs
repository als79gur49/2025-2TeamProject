using UnityEngine;
using UnityEngine.Assertions;
using Game.Services;
using Game.Core;

namespace Game.Tests
{
    /// <summary>
    /// Integration test for the phase 3 turn system implementation
    /// Tests service connections and phase transitions
    /// </summary>
    public class TurnSystemIntegrationTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool verboseLogging = true;
        
        private GameServiceManager serviceManager;
        private ITurnService turnService;
        private IUnitService unitService;
        private IUIService uiService;
        private IGameService gameService;
        
        private bool phaseChangedEventFired = false;
        private bool phaseCountChangedEventFired = false;
        private bool unitsProcessedEventFired = false;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                StartCoroutine(RunIntegrationTestCoroutine());
            }
        }
        
        private System.Collections.IEnumerator RunIntegrationTestCoroutine()
        {
            yield return new WaitForSeconds(1f); // Wait for services to initialize
            
            Log("🧪 Starting Turn System Integration Test...");
            
            try
            {
                // Step 1: Initialize services
                InitializeTestServices();
                yield return new WaitForSeconds(0.5f);
                
                // Step 2: Test service connections
                TestServiceConnections();
                yield return new WaitForSeconds(0.5f);
                
                // Step 3: Test phase transitions
                TestPhaseTransitions();
                yield return new WaitForSeconds(0.5f);
                
                // Step 4: Test UI integration
                TestUIIntegration();
                yield return new WaitForSeconds(0.5f);
                
                Log("✅ All integration tests passed!");
            }
            catch (System.Exception ex)
            {
                Log($"❌ Integration test failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }
        
        private void InitializeTestServices()
        {
            Log("🔧 Initializing test services...");
            
            // Find or create GameServiceManager
            serviceManager = FindObjectOfType<GameServiceManager>();
            if (serviceManager == null)
            {
                GameObject managerObj = new GameObject("GameServiceManager");
                serviceManager = managerObj.AddComponent<GameServiceManager>();
            }
            
            // Wait for initialization
            if (!serviceManager.IsInitialized)
            {
                serviceManager.InitializeServices();
            }
            
            // Get service references
            turnService = ServiceLocator.Get<ITurnService>();
            unitService = ServiceLocator.Get<IUnitService>();
            uiService = ServiceLocator.Get<IUIService>();
            gameService = ServiceLocator.Get<IGameService>();
            
            Assert.IsNotNull(turnService, "TurnService should be available");
            Assert.IsNotNull(unitService, "UnitService should be available");
            Assert.IsNotNull(uiService, "UIService should be available");
            Assert.IsNotNull(gameService, "GameService should be available");
            
            Log("✅ Test services initialized successfully");
        }
        
        private void TestServiceConnections()
        {
            Log("🔗 Testing service connections...");
            
            // Subscribe to events to verify they're wired correctly
            turnService.OnPhaseChanged += OnPhaseChangedTest;
            turnService.OnPhaseCountChanged += OnPhaseCountChangedTest;
            unitService.OnUnitsProcessed += OnUnitsProcessedTest;
            
            Log("✅ Service connections tested successfully");
        }
        
        private void TestPhaseTransitions()
        {
            Log("🔄 Testing phase transitions...");
            
            // Start the game to initialize phases
            turnService.StartGame();
            
            // Verify initial state
            Assert.AreEqual(TurnPhase.EnemySummon, turnService.CurrentPhase, 
                "Game should start with EnemySummon phase");
            Assert.AreEqual(0, turnService.TurnCount, "Turn count should start at 0");
            Assert.AreEqual(0, turnService.PhaseCount, "Phase count should start at 0");
            
            // Test phase progression
            TurnPhase[] expectedPhases = { 
                TurnPhase.AllySummon, 
                TurnPhase.EnemyAction, 
                TurnPhase.AllyAction, 
                TurnPhase.EnemySummon 
            };
            
            for (int i = 0; i < expectedPhases.Length; i++)
            {
                phaseChangedEventFired = false;
                phaseCountChangedEventFired = false;
                
                turnService.EndCurrentPhase();
                
                Assert.AreEqual(expectedPhases[i], turnService.CurrentPhase, 
                    $"Phase {i + 1} should be {expectedPhases[i]}");
                Assert.AreEqual(i + 1, turnService.PhaseCount, 
                    $"Phase count should be {i + 1}");
                Assert.IsTrue(phaseChangedEventFired, 
                    "PhaseChanged event should have fired");
                Assert.IsTrue(phaseCountChangedEventFired, 
                    "PhaseCountChanged event should have fired");
                
                Log($"✅ Phase transition {i + 1}/4 successful: {expectedPhases[i]}");
            }
            
            // After 4 phases, turn count should increment
            Assert.AreEqual(1, turnService.TurnCount, "Turn count should increment after 4 phases");
            
            Log("✅ Phase transitions tested successfully");
        }
        
        private void TestUIIntegration()
        {
            Log("🖼️ Testing UI integration...");
            
            // Verify UI responds to phase changes
            uiService.UpdateDisplay();
            
            // Test button text changes with phases
            TurnPhase[] testPhases = { 
                TurnPhase.EnemySummon, 
                TurnPhase.AllySummon, 
                TurnPhase.EnemyAction, 
                TurnPhase.AllyAction 
            };
            
            foreach (var phase in testPhases)
            {
                // Manually set phase for testing (accessing private field would require reflection)
                while (turnService.CurrentPhase != phase)
                {
                    turnService.EndCurrentPhase();
                }
                
                uiService.UpdateDisplay();
                Log($"✅ UI updated for phase: {phase}");
            }
            
            Log("✅ UI integration tested successfully");
        }
        
        // Event handlers for testing
        private void OnPhaseChangedTest(TurnPhase phase)
        {
            phaseChangedEventFired = true;
            Log($"📡 PhaseChanged event received: {phase}");
        }
        
        private void OnPhaseCountChangedTest(int count)
        {
            phaseCountChangedEventFired = true;
            Log($"📡 PhaseCountChanged event received: {count}");
        }
        
        private void OnUnitsProcessedTest()
        {
            unitsProcessedEventFired = true;
            Log($"📡 UnitsProcessed event received");
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (turnService != null)
            {
                turnService.OnPhaseChanged -= OnPhaseChangedTest;
                turnService.OnPhaseCountChanged -= OnPhaseCountChangedTest;
            }
            
            if (unitService != null)
            {
                unitService.OnUnitsProcessed -= OnUnitsProcessedTest;
            }
        }
        
        private void Log(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[TurnSystemIntegrationTest] {System.DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }
        
        /// <summary>
        /// Manual test trigger for editor testing
        /// </summary>
        [ContextMenu("Run Integration Test")]
        public void RunIntegrationTest()
        {
            StartCoroutine(RunIntegrationTestCoroutine());
        }
    }
}