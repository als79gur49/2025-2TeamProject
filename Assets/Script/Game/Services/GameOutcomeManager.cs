using UnityEngine;
using Game.Services;

namespace Game.Services
{
    /// <summary>
    /// GameOutcomeManager - Manages victory and defeat conditions through event observation
    ///
    /// Responsibilities:
    /// - Subscribe to BaseManager destruction events
    /// - Broadcast victory/defeat conditions to registered listeners
    /// - Act as mediator/handler between BaseManager and game state systems
    ///
    /// Architecture Integration:
    /// - Registered in GameInitializer.RegisterGameOutcomeServices()
    /// - Dependencies: IBaseManager
    /// - Consumers: GameService, UI systems, achievement systems
    /// </summary>
    public class GameOutcomeManager : MonoBehaviour, IGameOutcomeManager
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableLogging = true;

        // Dependencies (injected)
        private IBaseManager baseManager;

        // Initialization state
        private bool isInitialized = false;
        private bool dependenciesInjected = false;

        #region Events

        /// <summary>Event fired when victory conditions are met (Enemy Base destroyed)</summary>
        public event System.Action OnVictory;

        /// <summary>Event fired when defeat conditions are met (Player Base destroyed)</summary>
        public event System.Action OnDefeat;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Log("[GameOutcomeManager] Awake() - Waiting for dependency injection");
        }

        private void OnDestroy()
        {
            CleanupEventSubscriptions();
        }

        #endregion

        #region Dependency Injection

        /// <summary>
        /// Injects required dependencies
        /// Called by GameInitializer during service registration
        /// </summary>
        public void InjectDependencies(IBaseManager baseManager)
        {
            this.baseManager = baseManager;
            dependenciesInjected = true;
            Log("[GameOutcomeManager] Dependencies injected successfully");
        }

        /// <summary>
        /// Validates that all dependencies are available
        /// </summary>
        private bool ValidateDependencies()
        {
            if (!dependenciesInjected)
            {
                Debug.LogError("[GameOutcomeManager] Dependencies not injected!");
                return false;
            }

            if (baseManager == null)
            {
                Debug.LogError("[GameOutcomeManager] IBaseManager is null!");
                return false;
            }

            return true;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the GameOutcomeManager and subscribes to BaseManager events
        /// Called from GameInitializer after dependency injection
        /// </summary>
        public void Initialize()
        {
            if (!ValidateDependencies())
            {
                Debug.LogError("[GameOutcomeManager] Cannot initialize - dependency validation failed");
                return;
            }

            if (isInitialized)
            {
                Log("[GameOutcomeManager] Already initialized");
                return;
            }

            Log("[GameOutcomeManager] Initializing...");

            // Subscribe to BaseManager events
            SubscribeToBaseManagerEvents();

            isInitialized = true;
            Log("[GameOutcomeManager] Initialization complete");
        }

        #endregion

        #region Event Management

        /// <summary>
        /// Subscribes to BaseManager destruction events
        /// </summary>
        private void SubscribeToBaseManagerEvents()
        {
            if (baseManager != null)
            {
                baseManager.OnPlayerBaseDestroyed += HandlePlayerBaseDeath;
                baseManager.OnEnemyBaseDestroyed += HandleEnemyBaseDeath;
                Log("[GameOutcomeManager] Subscribed to BaseManager events");
            }
            else
            {
                Debug.LogError("[GameOutcomeManager] Cannot subscribe - BaseManager is null");
            }
        }

        /// <summary>
        /// Cleans up event subscriptions
        /// </summary>
        private void CleanupEventSubscriptions()
        {
            if (baseManager != null)
            {
                baseManager.OnPlayerBaseDestroyed -= HandlePlayerBaseDeath;
                baseManager.OnEnemyBaseDestroyed -= HandleEnemyBaseDeath;
                Log("[GameOutcomeManager] Unsubscribed from BaseManager events");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles Player Base destruction event (triggers defeat)
        /// </summary>
        private void HandlePlayerBaseDeath()
        {
            Log("[GameOutcomeManager] Player Base destroyed - Broadcasting defeat event");
            OnDefeat?.Invoke();
        }

        /// <summary>
        /// Handles Enemy Base destruction event (triggers victory)
        /// </summary>
        private void HandleEnemyBaseDeath()
        {
            Log("[GameOutcomeManager] Enemy Base destroyed - Broadcasting victory event");
            OnVictory?.Invoke();
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
