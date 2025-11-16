using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game;
using Game.Core;
using Game.Interfaces;

namespace Game.Services
{
    /// <summary>
    /// BaseManager - Manages Base lifecycle and game-ending conditions
    ///
    /// Responsibilities:
    /// - Initialize Player and Enemy bases at game start
    /// - Monitor Base health and trigger game-over events
    /// - Coordinate with GridManager for Base placement
    /// - Emit victory/defeat events for GameService
    /// - Manage Base HP UI updates through event-based system
    ///
    /// Architecture Integration:
    /// - Registered in GameInitializer.RegisterCoreServices()
    /// - Dependencies: IGridManager, ITeamConfigurationManager
    /// - Consumers: GameService (listens to Base destruction events)
    /// </summary>
    public class BaseManager : MonoBehaviour, IBaseManager
    {
        [Header("Base Configuration")]
        [SerializeField]
        [Tooltip("Base prefab to instantiate (must have Base component)")]
        private GameObject basePrefab;

        [SerializeField]
        [Tooltip("Base size in grid tiles (default: 3x1)")]
        private Vector2Int baseSize = new Vector2Int(3, 1);

        [Header("Placement Configuration")]
        [SerializeField]
        [Tooltip("Y offset for base positioning (distance from top/bottom edges)")]
        private int baseCenterYOffset = 0; // 0 = place at edges

        [Header("UI References")]
        [SerializeField]
        [Tooltip("Player Base HP UI GameObject (should contain Slider and/or TextMeshProUGUI)")]
        private GameObject playerBaseHealthUI;

        [SerializeField]
        [Tooltip("Enemy Base HP UI GameObject (should contain Slider and/or TextMeshProUGUI)")]
        private GameObject enemyBaseHealthUI;

        [Header("Debug Settings")]
        [SerializeField] private bool enableLogging = true;

        // Dependencies (injected)
        private IGridManager gridManager;

        // Base instances
        private Base playerBase;
        private Base enemyBase;

        // Initialization state
        private bool isInitialized = false;

        #region Events

        /// <summary>Event fired when Player's Base is destroyed (Game Loss)</summary>
        public event System.Action OnPlayerBaseDestroyed;

        /// <summary>Event fired when Enemy's Base is destroyed (Game Victory)</summary>
        public event System.Action OnEnemyBaseDestroyed;

        #endregion

        #region Properties

        /// <summary>Gets the Player's Base instance</summary>
        public Base PlayerBase => playerBase;

        /// <summary>Gets the Enemy's Base instance</summary>
        public Base EnemyBase => enemyBase;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Log("[BaseManager] Awake() - Waiting for dependency injection");
        }

        private void OnDestroy()
        {
            CleanupEventSubscriptions();
            CleanupHealthEventSubscriptions();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Manual initialization with dependency injection - called by GameServiceManager
        /// Injects dependencies and initializes both Player and Enemy bases
        /// </summary>
        public void Init(IGridManager gridManager)
        {
            // Phase 1: Inject dependencies from parameters
            this.gridManager = gridManager;

            // Phase 2: Validate dependencies
            if (!ValidateDependencies())
            {
                Debug.LogError("[BaseManager] Cannot initialize - dependency validation failed");
                return;
            }

            // Phase 3: Initialize bases
            if (isInitialized)
            {
                Log("[BaseManager] Already initialized, cleaning up old bases first");
                CleanupBases();
            }

            Log("[BaseManager] Initializing bases...");

            // Calculate base positions
            Vector2Int playerBasePos = CalculatePlayerBasePosition();
            Vector2Int enemyBasePos = CalculateEnemyBasePosition();

            // Create bases
            playerBase = CreateBase(TeamType.Player, playerBasePos);
            enemyBase = CreateBase(TeamType.Enemy, enemyBasePos);

            // Validate creation
            if (playerBase == null || enemyBase == null)
            {
                Debug.LogError("[BaseManager] Failed to create bases!");
                return;
            }

            // Subscribe to death events
            SubscribeToBaseEvents();

            // Subscribe to health change events for UI updates
            SubscribeToHealthEvents();

            isInitialized = true;
            Log($"[BaseManager] Bases initialized - Player: {playerBasePos}, Enemy: {enemyBasePos}");
        }

        /// <summary>
        /// Validates that all dependencies are available
        /// </summary>
        private bool ValidateDependencies()
        {
            if (gridManager == null)
            {
                Debug.LogError("[BaseManager] IGridManager is null!");
                return false;
            }

            if (basePrefab == null)
            {
                Debug.LogError("[BaseManager] Base prefab is not assigned in Inspector!");
                return false;
            }

            if (basePrefab.GetComponent<Base>() == null)
            {
                Debug.LogError("[BaseManager] Base prefab does not have Base component!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a Base at the specified position
        /// </summary>
        private Base CreateBase(TeamType team, Vector2Int gridPosition)
        {
            string teamName = team == TeamType.Player ? "Player" : "Enemy";
            Log($"[BaseManager] Creating {teamName} Base at {gridPosition}");

            // Instantiate Base GameObject
            GameObject baseObject = Instantiate(basePrefab);
            baseObject.name = $"Base_{teamName}";

            // Get Base component
            Base baseComponent = baseObject.GetComponent<Base>();
            if (baseComponent == null)
            {
                Debug.LogError($"[BaseManager] Base prefab missing Base component!");
                Destroy(baseObject);
                return null;
            }

            // Initialize Base with grid position and team
            baseComponent.Initialize(gridPosition, baseSize, team);

            // Place Base on grid
            IGridState gridState = gridManager.GetGridState();
            if (gridState != null)
            {
                bool placed = gridState.PlaceBase(baseObject, gridPosition, baseSize, team);
                if (!placed)
                {
                    Debug.LogError($"[BaseManager] Failed to place {teamName} Base on grid!");
                    Destroy(baseObject);
                    return null;
                }
            }

            // Set world position
            Vector3 worldPos = gridManager.GridToWorldPosition(gridPosition);
            baseObject.transform.position = worldPos;

            Log($"[BaseManager] {teamName} Base created successfully");
            return baseComponent;
        }

        #endregion

        #region Position Calculation

        /// <summary>
        /// Calculates Player Base position (bottom side of grid)
        /// </summary>
        private Vector2Int CalculatePlayerBasePosition()
        {
            Vector2Int gridSize = gridManager.GridSize;
            int centerX = gridSize.x / 2;
            int yPosition = baseCenterYOffset;

            // Player base at bottom side with offset, centered horizontally
            return new Vector2Int(centerX - baseSize.x / 2, yPosition);
        }

        /// <summary>
        /// Calculates Enemy Base position (top side of grid)
        /// </summary>
        private Vector2Int CalculateEnemyBasePosition()
        {
            Vector2Int gridSize = gridManager.GridSize;
            int centerX = gridSize.x / 2;
            int yPosition = gridSize.y - baseSize.y - baseCenterYOffset;

            // Enemy base at top side with offset, centered horizontally
            return new Vector2Int(centerX - baseSize.x / 2, yPosition);
        }

        #endregion

        #region Event Management

        /// <summary>
        /// Subscribes to Base death events
        /// </summary>
        private void SubscribeToBaseEvents()
        {
            if (playerBase != null)
            {
                playerBase.OnDeath += HandlePlayerBaseDeath;
            }

            if (enemyBase != null)
            {
                enemyBase.OnDeath += HandleEnemyBaseDeath;
            }
        }

        /// <summary>
        /// Cleans up event subscriptions
        /// </summary>
        private void CleanupEventSubscriptions()
        {
            if (playerBase != null)
            {
                playerBase.OnDeath -= HandlePlayerBaseDeath;
            }

            if (enemyBase != null)
            {
                enemyBase.OnDeath -= HandleEnemyBaseDeath;
            }
        }

        /// <summary>
        /// Handles Player Base destruction (triggers game loss)
        /// </summary>
        private void HandlePlayerBaseDeath(GameObject baseObject)
        {
            Log("[BaseManager] Player Base destroyed - Triggering game loss event");
            OnPlayerBaseDestroyed?.Invoke();
        }

        /// <summary>
        /// Handles Enemy Base destruction (triggers game victory)
        /// </summary>
        private void HandleEnemyBaseDeath(GameObject baseObject)
        {
            Log("[BaseManager] Enemy Base destroyed - Triggering game victory event");
            OnEnemyBaseDestroyed?.Invoke();
        }

        #endregion

        #region Health Event Management

        /// <summary>
        /// Subscribes to Base HealthComponent events for UI updates
        /// </summary>
        private void SubscribeToHealthEvents()
        {
            if (playerBase?.HealthComponent != null)
            {
                playerBase.HealthComponent.OnHealthChanged += HandlePlayerHealthChanged;
                Log("[BaseManager] Subscribed to Player Base health events");

                // Initial UI update
                HandlePlayerHealthChanged(playerBase.HealthComponent.CurrentHealth);
            }
            else
            {
                Debug.LogWarning("[BaseManager] Player Base or HealthComponent is null - cannot subscribe to health events");
            }

            if (enemyBase?.HealthComponent != null)
            {
                enemyBase.HealthComponent.OnHealthChanged += HandleEnemyHealthChanged;
                Log("[BaseManager] Subscribed to Enemy Base health events");

                // Initial UI update
                HandleEnemyHealthChanged(enemyBase.HealthComponent.CurrentHealth);
            }
            else
            {
                Debug.LogWarning("[BaseManager] Enemy Base or HealthComponent is null - cannot subscribe to health events");
            }
        }

        /// <summary>
        /// Cleans up HealthComponent event subscriptions
        /// </summary>
        private void CleanupHealthEventSubscriptions()
        {
            if (playerBase?.HealthComponent != null)
            {
                playerBase.HealthComponent.OnHealthChanged -= HandlePlayerHealthChanged;
                Log("[BaseManager] Unsubscribed from Player Base health events");
            }

            if (enemyBase?.HealthComponent != null)
            {
                enemyBase.HealthComponent.OnHealthChanged -= HandleEnemyHealthChanged;
                Log("[BaseManager] Unsubscribed from Enemy Base health events");
            }
        }

        /// <summary>
        /// Handles Player Base HP changes and updates UI
        /// </summary>
        private void HandlePlayerHealthChanged(int currentHP)
        {
            if (playerBase?.HealthComponent == null) return;

            int maxHP = playerBase.HealthComponent.MaxHealth;
            Log($"[BaseManager] Player Base HP changed: {currentHP}/{maxHP}");
            UpdateHealthUI(playerBaseHealthUI, currentHP, maxHP);
        }

        /// <summary>
        /// Handles Enemy Base HP changes and updates UI
        /// </summary>
        private void HandleEnemyHealthChanged(int currentHP)
        {
            if (enemyBase?.HealthComponent == null) return;

            int maxHP = enemyBase.HealthComponent.MaxHealth;
            Log($"[BaseManager] Enemy Base HP changed: {currentHP}/{maxHP}");
            UpdateHealthUI(enemyBaseHealthUI, currentHP, maxHP);
        }

        /// <summary>
        /// Updates the Health UI for a specific Base
        /// Supports both Slider and TextMeshProUGUI components
        /// </summary>
        private void UpdateHealthUI(GameObject healthUI, int currentHP, int maxHP)
        {
            if (healthUI == null)
            {
                // UI 참조가 없을 수 있음 (선택 사항)
                return;
            }

            // Update TextMeshProUGUI if present
            TextMeshProUGUI text = healthUI.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{currentHP}";
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up Base objects for game restart
        /// </summary>
        public void CleanupBases()
        {
            Log("[BaseManager] Cleaning up bases...");

            CleanupEventSubscriptions();
            CleanupHealthEventSubscriptions();

            if (playerBase != null)
            {
                Destroy(playerBase.gameObject);
                playerBase = null;
            }

            if (enemyBase != null)
            {
                Destroy(enemyBase.gameObject);
                enemyBase = null;
            }

            isInitialized = false;
            Log("[BaseManager] Bases cleaned up");
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
