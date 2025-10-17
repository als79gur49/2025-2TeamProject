using UnityEngine;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    /// <summary>
    /// Refactored GameService - Pure game state coordinator without ServiceLocator dependencies
    /// Focuses on game state management and coordination between services
    /// </summary>
    public class GameService : MonoBehaviour, IGameService
    {
        // 💉 Injected Dependencies - No more ServiceLocator
        private ITurnService turnService;
        private IUnitService unitService;
        private IUIService uiService;
        
        // 🎮 Game State
        public bool IsGameActive { get; private set; }

        // 📡 Events
        public event System.Action OnGameStarted;
        public event System.Action OnGameEnded;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            Debug.Log("[GameService] Awake() called - Waiting for dependency injection");
        }
        
        #endregion
        
        #region Initialization

        /// <summary>
        /// Manual initialization with dependency injection - called by GameServiceManager
        /// Injects dependencies, validates, and subscribes to events
        /// </summary>
        public void Init(ITurnService turnService, IUnitService unitService, IUIService uiService)
        {
            // Phase 1: Inject dependencies from parameters
            this.turnService = turnService;
            this.unitService = unitService;
            this.uiService = uiService;

            // Phase 2: Validate and initialize
            ValidateDependencies();
            SubscribeToEvents();

            Debug.Log("[GameService] Initialized successfully");
        }
        
        /// <summary>
        /// Validates that all required dependencies are available
        /// </summary>
        private void ValidateDependencies()
        {
            if (turnService == null)
                Debug.LogError("[GameService] ITurnService is null after injection");
            if (unitService == null)
                Debug.LogError("[GameService] IUnitService is null after injection");
            if (uiService == null)
                Debug.LogError("[GameService] IUIService is null after injection");
        }
        
        /// <summary>
        /// Subscribes to UI events for game coordination
        /// </summary>
        private void SubscribeToEvents()
        {
            if (uiService != null)
            {
                uiService.OnEndTurnRequested += HandleEndPhaseRequest;
                uiService.OnRestartRequested += RestartGame;
            }
        }
        
        #endregion
        
        #region Game State Management
        
        /// <summary>
        /// Starts a new game session
        /// </summary>
        public void StartGame()
        {
            if (turnService == null || unitService == null || uiService == null)
            {
                Debug.LogError("[GameService] Cannot start game - Dependencies not initialized!");
                return;
            }

            Debug.Log("[GameService] Starting game...");

            IsGameActive = true;
            turnService.StartGame();
            uiService.UpdateDisplay();

            OnGameStarted?.Invoke();
        }
        
        /// <summary>
        /// Restarts the current game
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("[GameService] Restarting game...");
            
            // End current game if active
            if (IsGameActive)
            {
                IsGameActive = false;
                OnGameEnded?.Invoke();
            }
            
            // Start new game
            StartGame();
        }
        
        /// <summary>
        /// Ends the current game
        /// </summary>
        public void EndGame()
        {
            if (IsGameActive)
            {
                Debug.Log("[GameService] Ending game...");
                IsGameActive = false;
                OnGameEnded?.Invoke();
            }
        }
        
        #endregion
        
        #region Update Loop
        
        /// <summary>
        /// Frame update for game state monitoring
        /// </summary>
        public void Update()
        {
            // Monitor game state and coordinate services
            // Note: Test input handling moved to separate component
        }
        
        #endregion
        
        #region Turn Management Coordination
        
        /// <summary>
        /// Handles end phase request from UI
        /// Phase 4: Enhanced to handle sequential unit processing state
        /// </summary>
        private void HandleEndPhaseRequest()
        {
            if (!IsGameActive || turnService == null)
            {
                Debug.LogWarning($"[GameService] Cannot process end phase - IsGameActive({IsGameActive}) or TurnService not initialized");
                return;
            }
            
            if (turnService == null)
            {
                Debug.LogError("[GameService] Cannot process end phase - TurnService is null");
                return;
            }
            
            Debug.Log("[GameService] Processing end phase request...");
            
            try
            {
                // Phase 4: Check if UnitService is currently executing a phase
                if (unitService != null && unitService.IsPhaseExecuting)
                {
                    // If phase is executing, request immediate completion instead of direct phase transition
                    Debug.Log("[GameService] Phase is executing. Requesting immediate completion.");
                    unitService.CancelCurrentPhase(true); // true: complete remaining unit actions instantly
                }
                
                // Always attempt to end the current phase - this is safe even after cancellation
                // because CancelCurrentPhase already handles the phase completion logic
                turnService.EndCurrentPhase();
                
                Debug.Log("[GameService] End phase processed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameService] Error processing end phase: {ex.Message}");
            }
        }
        
        #endregion

        #region Event Cleanup

        /// <summary>
        /// Clean up event subscriptions on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (uiService != null)
            {
                uiService.OnEndTurnRequested -= HandleEndPhaseRequest;
                uiService.OnRestartRequested -= RestartGame;
            }
        }

        #endregion
    }
}