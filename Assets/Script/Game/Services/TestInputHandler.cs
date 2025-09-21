using UnityEngine;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    /// <summary>
    /// Separate component for handling test/debug input
    /// Separated from GameService to follow single responsibility principle
    /// </summary>
    public class TestInputHandler : MonoBehaviour
    {
        [Header("Input Configuration")]
        [SerializeField] private bool enableTestInput = true;
        [SerializeField] private KeyCode endTurnKey = KeyCode.Space;
        [SerializeField] private KeyCode processAllUnitsKey = KeyCode.U;
        [SerializeField] private KeyCode restartGameKey = KeyCode.R;
        
        // Service references
        private IGameService gameService;
        private IUnitService unitService;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Get services from ServiceLocator (for test input only)
            InitializeServiceReferences();
        }
        
        private void Update()
        {
            if (enableTestInput)
            {
                HandleTestInput();
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize service references for test input handling
        /// </summary>
        private void InitializeServiceReferences()
        {
            try
            {
                gameService = ServiceLocator.Get<IGameService>();
                unitService = ServiceLocator.Get<IUnitService>();
                
                if (gameService == null)
                    Debug.LogWarning("[TestInputHandler] IGameService not found - some inputs may not work");
                if (unitService == null)
                    Debug.LogWarning("[TestInputHandler] IUnitService not found - some inputs may not work");
                    
                Debug.Log("[TestInputHandler] Initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TestInputHandler] Failed to initialize: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Input Handling
        
        /// <summary>
        /// Handle test/debug keyboard input
        /// </summary>
        private void HandleTestInput()
        {
            // End Turn (Space key)
            if (Input.GetKeyDown(endTurnKey))
            {
                HandleEndTurnInput();
            }
            
            // Process All Units (U key)
            if (Input.GetKeyDown(processAllUnitsKey))
            {
                HandleProcessAllUnitsInput();
            }
            
            // Restart Game (R key)
            if (Input.GetKeyDown(restartGameKey))
            {
                HandleRestartGameInput();
            }
        }
        
        /// <summary>
        /// Handle end turn input
        /// </summary>
        private void HandleEndTurnInput()
        {
            if (gameService != null && gameService.IsGameActive)
            {
                Debug.Log("[TestInputHandler] End turn requested via keyboard");
                // Access UIService to trigger end turn event
                var uiService = ServiceLocator.Get<IUIService>();
                if (uiService is UIService concreteUIService)
                {
                    // Create a method to trigger the event from UIService
                    concreteUIService.TriggerEndTurnRequest();
                }
            }
            else
            {
                Debug.LogWarning("[TestInputHandler] Cannot end turn - game not active or service unavailable");
            }
        }
        
        /// <summary>
        /// Handle process all units input (debug feature)
        /// </summary>
        private void HandleProcessAllUnitsInput()
        {
            if (unitService != null)
            {
                Debug.Log("[TestInputHandler] Process all units requested via keyboard");
                unitService.ProcessAllUnits();
            }
            else
            {
                Debug.LogWarning("[TestInputHandler] Cannot process units - UnitService unavailable");
            }
        }
        
        /// <summary>
        /// Handle restart game input
        /// </summary>
        private void HandleRestartGameInput()
        {
            if (gameService != null)
            {
                Debug.Log("[TestInputHandler] Game restart requested via keyboard");
                gameService.RestartGame();
            }
            else
            {
                Debug.LogWarning("[TestInputHandler] Cannot restart game - GameService unavailable");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Enable or disable test input handling
        /// </summary>
        /// <param name="enabled">Whether test input should be enabled</param>
        public void SetTestInputEnabled(bool enabled)
        {
            enableTestInput = enabled;
            Debug.Log($"[TestInputHandler] Test input {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Get current test input status
        /// </summary>
        /// <returns>True if test input is enabled</returns>
        public bool IsTestInputEnabled()
        {
            return enableTestInput;
        }
        
        #endregion
    }
}