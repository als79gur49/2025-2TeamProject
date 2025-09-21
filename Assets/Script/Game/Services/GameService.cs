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
        
        // 🔧 Dependency injection state
        private bool dependenciesInjected = false;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            Debug.Log("[GameService] Awake() called - Waiting for dependency injection");
        }
        
        #endregion
        
        #region Dependency Injection
        
        /// <summary>
        /// Injects required dependencies - Called by GameServiceManager
        /// </summary>
        public void InjectDependencies(ITurnService turnService, IUnitService unitService, IUIService uiService)
        {
            this.turnService = turnService;
            this.unitService = unitService;
            this.uiService = uiService;
            
            dependenciesInjected = true;
            Debug.Log("[GameService] Dependencies injected successfully");
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initializes the GameService - Dependencies must be injected first
        /// </summary>
        public void Initialize()
        {
            if (!dependenciesInjected)
            {
                Debug.LogError("[GameService] Cannot initialize - Dependencies not injected!");
                return;
            }
            
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
                uiService.OnEndTurnRequested += HandleEndTurnRequest;
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
            if (!dependenciesInjected)
            {
                Debug.LogError("[GameService] Cannot start game - Dependencies not injected!");
                return;
            }
            
            Debug.Log("[GameService] Starting game...");
            
            IsGameActive = true;
            turnService?.StartGame();
            uiService?.UpdateDisplay();
            
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
        /// Handles end turn request from UI
        /// Coordinates between TurnService and UnitService
        /// </summary>
        private void HandleEndTurnRequest()
        {
            if (!IsGameActive || !dependenciesInjected)
            {
                Debug.LogWarning("[GameService] Cannot process end turn - Game not active or dependencies missing");
                return;
            }
            
            if (turnService == null || unitService == null)
            {
                Debug.LogError("[GameService] Cannot process end turn - Required services are null");
                return;
            }
            
            Debug.Log("[GameService] Processing end turn request...");
            
            try
            {
                // Process units for current turn before ending
                unitService.ProcessUnitsForCurrentPlayer(turnService.IsPlayerTurn);
                
                // End the turn
                turnService.EndTurn();
                
                // Update UI to reflect new state
                uiService?.UpdateDisplay();
                
                Debug.Log("[GameService] End turn processed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameService] Error processing end turn: {ex.Message}");
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
                uiService.OnEndTurnRequested -= HandleEndTurnRequest;
                uiService.OnRestartRequested -= RestartGame;
            }
        }
        
        #endregion
    }
}