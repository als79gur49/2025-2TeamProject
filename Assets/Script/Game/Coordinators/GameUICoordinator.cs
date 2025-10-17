using UnityEngine;
using Game.Services;
using Game.Core;

namespace Game.Coordinators
{
    /// <summary>
    /// GameUICoordinator - Mediator between game events and UI actions
    ///
    /// Responsibilities:
    /// - Subscribe to game-level events (GameOutcomeManager)
    /// - Coordinate UI responses through UIPanelManager
    /// - Maintain clean separation between game logic and UI layers
    ///
    /// Architecture Benefits:
    /// - Decouples UI panels from game services
    /// - Centralizes game→UI event coordination
    /// - Follows Mediator pattern for clean architecture
    /// - Maintains Single Responsibility Principle
    ///
    /// Initialization Pattern:
    /// - Single Init() method retrieves dependencies from ServiceLocator
    /// - Self-sufficient initialization aligned with ResourceManager pattern
    ///
    /// Registered in: GameInitializer.RegisterGameOutcomeServices()
    /// Dependencies: IGameOutcomeManager (from ServiceLocator), UIPanelManager (Singleton)
    /// </summary>
    public class GameUICoordinator : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableLogging = true;

        // Dependencies (retrieved from ServiceLocator)
        private IGameOutcomeManager gameOutcomeManager;
        private UIPanelManager uiPanelManager;

        // Initialization state
        private bool isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            Log("[GameUICoordinator] Awake() - Waiting for Init() call");
        }

        private void OnDestroy()
        {
            CleanupEventSubscriptions();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the coordinator by retrieving dependencies and subscribing to events
        /// Called by GameInitializer during service registration
        ///
        /// Pattern: Single-step initialization
        /// - Retrieves IGameOutcomeManager from ServiceLocator
        /// - Retrieves UIPanelManager from Singleton
        /// - Validates dependencies
        /// - Subscribes to game events
        /// </summary>
        public void Init()
        {
            if (isInitialized)
            {
                Log("[GameUICoordinator] Already initialized");
                return;
            }

            Log("[GameUICoordinator] Initializing...");

            // Retrieve dependencies from ServiceLocator
            gameOutcomeManager = ServiceLocator.Get<IGameOutcomeManager>();
            uiPanelManager = UIPanelManager.Instance;

            // Validate dependencies
            if (!ValidateDependencies())
            {
                Debug.LogError("[GameUICoordinator] Cannot initialize - dependency validation failed");
                return;
            }

            // Subscribe to game outcome events
            SubscribeToGameEvents();

            isInitialized = true;
            Log("[GameUICoordinator] Initialization complete");
        }

        /// <summary>
        /// Validates that all dependencies are available
        /// </summary>
        private bool ValidateDependencies()
        {
            if (gameOutcomeManager == null)
            {
                Debug.LogError("[GameUICoordinator] IGameOutcomeManager is null - ensure it's registered in ServiceLocator");
                return false;
            }

            if (uiPanelManager == null)
            {
                Debug.LogError("[GameUICoordinator] UIPanelManager.Instance is null - ensure it exists in scene");
                return false;
            }

            return true;
        }

        #endregion

        #region Event Management

        /// <summary>
        /// Subscribes to GameOutcomeManager events
        /// </summary>
        private void SubscribeToGameEvents()
        {
            if (gameOutcomeManager != null)
            {
                gameOutcomeManager.OnVictory += HandleVictory;
                gameOutcomeManager.OnDefeat += HandleDefeat;
                Log("[GameUICoordinator] Subscribed to GameOutcomeManager events");
            }
            else
            {
                Debug.LogError("[GameUICoordinator] Cannot subscribe - GameOutcomeManager is null");
            }
        }

        /// <summary>
        /// Cleans up event subscriptions
        /// </summary>
        private void CleanupEventSubscriptions()
        {
            if (gameOutcomeManager != null)
            {
                gameOutcomeManager.OnVictory -= HandleVictory;
                gameOutcomeManager.OnDefeat -= HandleDefeat;
                Log("[GameUICoordinator] Unsubscribed from GameOutcomeManager events");
            }
        }

        #endregion

        #region Game Event Handlers

        /// <summary>
        /// Handles victory event from GameOutcomeManager
        /// Coordinates UI response by showing VictoryPanel
        /// </summary>
        private void HandleVictory()
        {
            Log("[GameUICoordinator] Victory event received - coordinating UI response");

            if (uiPanelManager != null)
            {
                uiPanelManager.ShowPanel<VictoryPanel>();
                Log("[GameUICoordinator] VictoryPanel displayed successfully");
            }
            else
            {
                Debug.LogError("[GameUICoordinator] Cannot show VictoryPanel - UIPanelManager is null");
            }
        }

        /// <summary>
        /// Handles defeat event from GameOutcomeManager
        /// Coordinates UI response by showing DefeatPanel
        /// </summary>
        private void HandleDefeat()
        {
            Log("[GameUICoordinator] Defeat event received - coordinating UI response");

            if (uiPanelManager != null)
            {
                uiPanelManager.ShowPanel<DefeatPanel>();
                Log("[GameUICoordinator] DefeatPanel displayed successfully");
            }
            else
            {
                Debug.LogError("[GameUICoordinator] Cannot show DefeatPanel - UIPanelManager is null");
            }
        }

        #endregion

        #region Debug Utilities

        /// <summary>
        /// Logs debug messages if logging is enabled
        /// </summary>
        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log(message);
            }
        }

        #endregion
    }
}
