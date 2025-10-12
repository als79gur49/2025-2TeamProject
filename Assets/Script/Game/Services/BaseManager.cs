using UnityEngine;
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
        [Tooltip("Base size in grid tiles (default: 1x3)")]
        private Vector2Int baseSize = new Vector2Int(1, 3);

        [Header("Placement Configuration")]
        [SerializeField]
        [Tooltip("Y offset for base positioning (centered vertically)")]
        private int baseCenterYOffset = 0; // 0 = use grid center

        [Header("Debug Settings")]
        [SerializeField] private bool enableLogging = true;

        // Dependencies (injected)
        private IGridManager gridManager;
        private ITeamConfigurationManager teamConfigManager;

        // Base instances
        private Base playerBase;
        private Base enemyBase;

        // Initialization state
        private bool isInitialized = false;
        private bool dependenciesInjected = false;

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
        }

        #endregion

        #region Dependency Injection

        /// <summary>
        /// Injects required dependencies
        /// Called by GameInitializer or GameServiceManager
        /// </summary>
        public void InjectDependencies(IGridManager gridManager, ITeamConfigurationManager teamConfigManager)
        {
            this.gridManager = gridManager;
            this.teamConfigManager = teamConfigManager;

            dependenciesInjected = true;
            Log("[BaseManager] Dependencies injected successfully");
        }

        /// <summary>
        /// Validates that all dependencies are available
        /// </summary>
        private bool ValidateDependencies()
        {
            if (!dependenciesInjected)
            {
                Debug.LogError("[BaseManager] Dependencies not injected!");
                return false;
            }

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

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes both Player and Enemy bases
        /// Called from GameService.StartGame()
        /// </summary>
        public void InitializeBases()
        {
            if (!ValidateDependencies())
            {
                Debug.LogError("[BaseManager] Cannot initialize - dependency validation failed");
                return;
            }

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

            isInitialized = true;
            Log($"[BaseManager] Bases initialized - Player: {playerBasePos}, Enemy: {enemyBasePos}");
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

            // Apply team visuals
            ApplyTeamVisuals(baseComponent, team);

            // Set world position
            Vector3 worldPos = gridManager.GridToWorldPosition(gridPosition);
            baseObject.transform.position = worldPos;

            Log($"[BaseManager] {teamName} Base created successfully");
            return baseComponent;
        }

        /// <summary>
        /// Applies team-specific visuals to Base
        /// </summary>
        private void ApplyTeamVisuals(Base baseComponent, TeamType team)
        {
            if (teamConfigManager == null) return;

            Material teamMaterial = teamConfigManager.GetTeamMaterial(team);
            if (teamMaterial != null)
            {
                Renderer renderer = baseComponent.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = teamMaterial;
                }
            }
        }

        #endregion

        #region Position Calculation

        /// <summary>
        /// Calculates Player Base position (left side of grid)
        /// </summary>
        private Vector2Int CalculatePlayerBasePosition()
        {
            Vector2Int gridSize = gridManager.GridSize;
            int centerY = baseCenterYOffset != 0 ? baseCenterYOffset : gridSize.y / 2;

            // Player base at leftmost column, centered vertically
            return new Vector2Int(0, centerY - baseSize.y / 2);
        }

        /// <summary>
        /// Calculates Enemy Base position (right side of grid)
        /// </summary>
        private Vector2Int CalculateEnemyBasePosition()
        {
            Vector2Int gridSize = gridManager.GridSize;
            int centerY = baseCenterYOffset != 0 ? baseCenterYOffset : gridSize.y / 2;

            // Enemy base at rightmost column, centered vertically
            return new Vector2Int(gridSize.x - baseSize.x, centerY - baseSize.y / 2);
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

        #region Cleanup

        /// <summary>
        /// Cleans up Base objects for game restart
        /// </summary>
        public void CleanupBases()
        {
            Log("[BaseManager] Cleaning up bases...");

            CleanupEventSubscriptions();

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
