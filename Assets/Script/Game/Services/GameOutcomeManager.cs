using UnityEngine;
using Game.Services;
using Game.Core;

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

        // Dependencies (retrieved from ServiceLocator)
        private IBaseManager baseManager;

        // Initialization state
        private bool isInitialized = false;

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

        #region Initialization

        /// <summary>
        /// Validates that all dependencies are available from ServiceLocator
        /// </summary>
        private bool ValidateDependencies()
        {
            if (baseManager == null)
            {
                Debug.LogError("[GameOutcomeManager] IBaseManager not found in ServiceLocator!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initializes the GameOutcomeManager by retrieving dependencies from ServiceLocator
        /// and subscribing to BaseManager events
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                Log("[GameOutcomeManager] Already initialized");
                return;
            }

            Log("[GameOutcomeManager] Initializing...");
            
            // Retrieve dependencies from ServiceLocator
            baseManager = ServiceLocator.Get<IBaseManager>();

            if (!ValidateDependencies())
            {
                Debug.LogError("[GameOutcomeManager] Cannot initialize - dependency validation failed");
                return;
            }

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
