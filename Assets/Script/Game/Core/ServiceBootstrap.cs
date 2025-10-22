using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Game.Services;
using Game.Controllers;
using Game.SceneManagement;

namespace Game.Core
{
    /// <summary>
    /// Centralized bootstrap system for initializing and registering global services.
    ///
    /// Execution Order: Set to -100 in Script Execution Order settings (Edit → Project Settings → Script Execution Order)
    /// Lifecycle: Creates and registers all global services at game startup, persists across scenes
    ///
    /// Unity Official Pattern: Persistent Bootstrap + Additive Loading
    /// - BootstrapScene remains loaded (DontDestroyOnLoad services)
    /// - Initial scene loaded additively to preserve bootstrap
    ///
    /// Usage:
    /// 1. Create a "BootstrapScene" and add a GameObject with this component
    /// 2. Assign service prefabs in the Inspector
    /// 3. Set this scene as index 0 in Build Settings
    /// 4. Configure Script Execution Order to -100
    /// 5. Assign initialSceneData ScriptableObject in Inspector
    /// </summary>
    public class ServiceBootstrap : MonoBehaviour
    {
        [Header("Service Prefab References")]
        [Tooltip("Optional: Leave null to auto-create. SceneLoaderService has no dependencies.")]
        [SerializeField] private GameObject sceneLoaderServicePrefab;

        [Tooltip("Required: Must have LoadingScreenPanel reference. SceneTransitionController depends on SceneLoaderService.")]
        [SerializeField] private GameObject sceneTransitionControllerPrefab;

        [Header("Audio Service Configuration")]
        [Tooltip("Required: AudioServiceContainer prefab with all audio services configured")]
        [SerializeField] private GameObject audioServiceContainerPrefab;

        [Header("UI Service Configuration")]
        [Tooltip("Required: GlobalUIPanelManager prefab with Canvas (Sort Order 1000)")]
        [SerializeField] private GameObject globalUIPanelManagerPrefab;

        [Header("Initial Scene Configuration")]
        [Tooltip("SceneData asset to load after bootstrap initialization. Contains scene name, BGM, and loading screen configuration.")]
        [SerializeField] private SceneData initialSceneData;

        [Header("Debug Options")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool validateOnStart = true;

        private static bool isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            // Prevent duplicate initialization
            if (isInitialized)
            {
                Log("ServiceBootstrap already initialized. Destroying duplicate...");
                Destroy(gameObject);
                return;
            }

            isInitialized = true;
            DontDestroyOnLoad(gameObject);

            Log("=== ServiceBootstrap: Starting Service Initialization ===");
            InitializeServices();
            Log("=== ServiceBootstrap: Service Initialization Complete ===");
        }

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateServices();
            }

            // ✅ Unity Official Pattern: Load initial scene additively
            // BootstrapScene persists, initial scene loads on top
            StartCoroutine(LoadInitialSceneAsync());
        }

        #endregion

        #region Scene Loading (SceneTransitionController Integration)

        /// <summary>
        /// Load initial scene using SceneTransitionController with proper BGM and metadata handling.
        ///
        /// Benefits of using SceneTransitionController instead of direct SceneManager:
        /// - Automatic BGM transition from SceneData.BgMusic
        /// - Loading screen with custom background and tips
        /// - Consistent scene transition flow throughout the game
        /// - Proper event handling (OnSceneTransitionStarted/Completed)
        ///
        /// Note: SceneTransitionController is already initialized in Awake() phase,
        /// so it's safe to use here in Start() coroutine.
        /// </summary>
        private IEnumerator LoadInitialSceneAsync()
        {
            // Validate SceneData reference
            if (initialSceneData == null)
            {
                LogError("Initial SceneData is not configured! Please assign a SceneData asset in the Inspector.");
                yield break;
            }

            Log($"Loading initial scene '{initialSceneData.SceneName}' via SceneTransitionController...");

            // Get SceneTransitionController from ServiceLocator
            ISceneTransitionController sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();

            if (sceneTransitionController == null)
            {
                LogError("SceneTransitionController not found in ServiceLocator! Cannot load initial scene.");
                LogError("This should never happen as SceneTransitionController is initialized in Awake().");
                yield break;
            }

            // ✅ Use SceneTransitionController for proper scene loading with BGM and metadata
            // This will:
            // 1. Show loading screen (if configured in SceneData)
            // 2. Load scene in Additive mode (preserving BootstrapScene)
            // 3. Trigger BGM transition from SceneData.BgMusic
            // 4. Set the loaded scene as active scene
            sceneTransitionController.LoadSceneWithLoading(initialSceneData);

            // Wait a frame to let the transition start
            yield return null;

            Log($"✓ Initial scene transition started: {initialSceneData.SceneName}");
            Log($"  BootstrapScene remains loaded with global services");
            Log($"  BGM will be played from SceneData configuration");
        }

        #endregion

        #region Service Initialization

        /// <summary>
        /// Initialize all global services in dependency order.
        /// ORDER IS CRITICAL: Dependencies must be registered before dependents.
        /// </summary>
        private void InitializeServices()
        {
            // PHASE 1: Foundational Services (no dependencies)
            InitializeSceneLoaderService();

            // PHASE 2: Audio System (no dependencies)
            InitializeAudioServiceContainer();

            // PHASE 3: Controllers (depend on Phase 1 services)
            InitializeSceneTransitionController();

            // PHASE 4: UI System (no dependencies)
            InitializeGlobalUIPanelManager();
        }

        /// <summary>
        /// Initialize SceneLoaderService and register with ServiceLocator.
        /// Dependencies: None
        /// </summary>
        private void InitializeSceneLoaderService()
        {
            Log("[1/3] Initializing SceneLoaderService...");

            // Create service instance
            GameObject serviceObj = sceneLoaderServicePrefab != null
                ? Instantiate(sceneLoaderServicePrefab)
                : new GameObject("SceneLoaderService");

            // Add component if not present
            SceneLoaderService service = serviceObj.GetComponent<SceneLoaderService>();
            if (service == null)
            {
                service = serviceObj.AddComponent<SceneLoaderService>();
            }

            // ✅ RegisterSingleton 사용 (ServiceCleanup 자동 부착)
            // SceneLoaderService의 Awake()에서 DontDestroyOnLoad 호출됨
            // ServiceCleanup은 게임 종료 시에만 OnDestroy()에서 자동 Unregister
            ServiceLocator.RegisterSingleton<ISceneLoaderService, SceneLoaderService>(service);

            Log("  ✓ SceneLoaderService created and registered");
        }

        /// <summary>
        /// Initialize AudioServiceContainer and register all audio services with ServiceLocator.
        /// Dependencies: None
        ///
        /// Registers:
        /// - AudioServiceContainer (container management)
        /// - IBGMAudioService (background music)
        /// - IEffectAudioService (sound effects)
        /// - IVolumeController (volume management)
        /// </summary>
        private void InitializeAudioServiceContainer()
        {
            Log("[Audio] Initializing Audio System (BGM + Effect + Volume)...");

            // Validate prefab reference
            if (audioServiceContainerPrefab == null)
            {
                LogError("  ✗ AudioServiceContainer prefab reference is missing!");
                LogError("  → Please assign the prefab in ServiceBootstrap Inspector");
                return;
            }

            // Create container instance from prefab
            GameObject containerObj = Instantiate(audioServiceContainerPrefab);
            AudioServiceContainer container = containerObj.GetComponent<AudioServiceContainer>();

            if (container == null)
            {
                LogError("  ✗ AudioServiceContainer component not found on prefab!");
                LogError("  → Verify the prefab has AudioServiceContainer component");
                Destroy(containerObj);
                return;
            }

            // ✅ RegisterSingleton 사용 (ServiceCleanup 자동 부착)
            // AudioServiceContainer의 Awake()에서 DontDestroyOnLoad 호출됨
            // ServiceCleanup은 게임 종료 시에만 OnDestroy()에서 자동 Unregister
            ServiceLocator.RegisterSingleton<AudioServiceContainer, AudioServiceContainer>(container);

            // ✅ FIX: Initialize services immediately before trying to access them
            // AudioServiceContainer.Start() would be too late - we need services NOW
            container.InitializeAllServices();
            Log("  ✓ AudioServiceContainer services initialized");

            // Register individual audio services for direct access
            // Use container.GetService<T>() instead of GetComponentInChildren to ensure services are found

            // BGM Service
            var bgmService = container.GetService<IBGMAudioService>();
            if (bgmService != null)
            {
                ServiceLocator.RegisterSingleton<IBGMAudioService, BGMAudioService>(bgmService as BGMAudioService);
                Log("  ✓ BGMAudioService registered");
            }
            else
            {
                LogError("  ✗ BGMAudioService not found in container");
            }

            // Effect Service
            var effectService = container.GetService<IEffectAudioService>();
            if (effectService != null)
            {
                ServiceLocator.RegisterSingleton<IEffectAudioService, EffectAudioService>(effectService as EffectAudioService);
                Log("  ✓ EffectAudioService registered");
            }
            else
            {
                LogError("  ✗ EffectAudioService not found in container");
            }

            // Volume Controller
            var volumeController = container.GetService<IVolumeController>();
            if (volumeController != null)
            {
                ServiceLocator.RegisterSingleton<IVolumeController, VolumeController>(volumeController as VolumeController);
                Log("  ✓ VolumeController registered");
            }
            else
            {
                LogError("  ✗ VolumeController not found in container");
            }

            Log("  ✓ Audio System initialization complete");
        }

        /// <summary>
        /// Initialize SceneTransitionController and register with ServiceLocator.
        /// Dependencies: ISceneLoaderService (must be registered first)
        /// </summary>
        private void InitializeSceneTransitionController()
        {
            Log("[3/3] Initializing SceneTransitionController...");

            // Validate dependency
            if (!ServiceLocator.IsRegistered<ISceneLoaderService>())
            {
                LogError("  ✗ Dependency check failed: ISceneLoaderService not registered!");
                LogError("  → Cannot initialize SceneTransitionController without SceneLoaderService");
                return;
            }

            // Validate prefab reference
            if (sceneTransitionControllerPrefab == null)
            {
                LogError("  ✗ SceneTransitionController prefab reference is missing!");
                LogError("  → Please assign the prefab in ServiceBootstrap Inspector");
                return;
            }

            // Create controller instance from prefab
            GameObject controllerObj = Instantiate(sceneTransitionControllerPrefab);
            SceneTransitionController controller = controllerObj.GetComponent<SceneTransitionController>();

            if (controller == null)
            {
                LogError("  ✗ SceneTransitionController component not found on prefab!");
                LogError("  → Verify the prefab has SceneTransitionController component");
                Destroy(controllerObj);
                return;
            }

            // ✅ RegisterSingleton 사용 (ServiceCleanup 자동 부착)
            // SceneTransitionController의 Awake()에서 DontDestroyOnLoad 호출됨
            // ServiceCleanup은 게임 종료 시에만 OnDestroy()에서 자동 Unregister
            ServiceLocator.RegisterSingleton<ISceneTransitionController, SceneTransitionController>(controller);

            Log("  ✓ SceneTransitionController created and registered");
        }

        /// <summary>
        /// Initialize GlobalUIPanelManager and register with ServiceLocator.
        /// Dependencies: None
        /// </summary>
        private void InitializeGlobalUIPanelManager()
        {
            Log("[4/4] Initializing GlobalUIPanelManager...");

            // Validate prefab reference
            if (globalUIPanelManagerPrefab == null)
            {
                LogError("  ✗ GlobalUIPanelManager prefab reference is missing!");
                LogError("  → Please assign the prefab in ServiceBootstrap Inspector");
                return;
            }

            // Create manager instance from prefab
            GameObject managerObj = Instantiate(globalUIPanelManagerPrefab);
            GlobalUIPanelManager manager = managerObj.GetComponent<GlobalUIPanelManager>();

            if (manager == null)
            {
                LogError("  ✗ GlobalUIPanelManager component not found on prefab!");
                LogError("  → Verify the prefab has GlobalUIPanelManager component");
                Destroy(managerObj);
                return;
            }

            // ✅ RegisterSingleton 사용 (ServiceCleanup 자동 부착)
            // GlobalUIPanelManager의 Awake()에서 DontDestroyOnLoad 호출됨
            // ServiceCleanup은 게임 종료 시에만 OnDestroy()에서 자동 Unregister
            ServiceLocator.RegisterSingleton<GlobalUIPanelManager, GlobalUIPanelManager>(manager);

            Log("  ✓ GlobalUIPanelManager created and registered");
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate all registered services are in valid state.
        /// Can be called from Unity Inspector via Context Menu.
        /// </summary>
        [ContextMenu("Validate Services")]
        public void ValidateServices()
        {
            Log("=== Service Validation ===");

            bool allValid = true;

            // Validate SceneLoaderService
            allValid &= ValidateService<ISceneLoaderService>("SceneLoaderService");

            // Validate Audio Services
            allValid &= ValidateService<AudioServiceContainer>("AudioServiceContainer");
            allValid &= ValidateAudioService<IBGMAudioService>("BGMAudioService");
            allValid &= ValidateAudioService<IEffectAudioService>("EffectAudioService");
            allValid &= ValidateAudioService<IVolumeController>("VolumeController");

            // Validate SceneTransitionController
            allValid &= ValidateService<ISceneTransitionController>("SceneTransitionController");

            // Validate GlobalUIPanelManager
            allValid &= ValidateService<GlobalUIPanelManager>("GlobalUIPanelManager");

            if (allValid)
            {
                Log("✓ All services validated successfully");
            }
            else
            {
                LogError("✗ Service validation failed - check errors above");
            }
        }

        /// <summary>
        /// Validate a specific service type.
        /// </summary>
        private bool ValidateService<T>(string serviceName) where T : class
        {
            // Check if registered
            if (!ServiceLocator.IsRegistered<T>())
            {
                LogError($"  ✗ {serviceName}: Not registered in ServiceLocator");
                return false;
            }

            // Check if retrievable
            T service = ServiceLocator.Get<T>();
            if (service == null)
            {
                LogError($"  ✗ {serviceName}: Registered but returns null");
                return false;
            }

            // Validate if it implements IGlobalService
            if (service is IGlobalService globalService)
            {
                if (!globalService.IsValid())
                {
                    LogError($"  ✗ {serviceName}: IsValid() returned false");
                    return false;
                }
            }

            Log($"  ✓ {serviceName}: Valid");
            return true;
        }

        /// <summary>
        /// Validate audio service type with additional audio-specific checks.
        /// </summary>
        private bool ValidateAudioService<T>(string serviceName) where T : class
        {
            // Check if registered
            if (!ServiceLocator.IsRegistered<T>())
            {
                LogError($"  ✗ {serviceName}: Not registered in ServiceLocator");
                return false;
            }

            // Check if retrievable
            T service = ServiceLocator.Get<T>();
            if (service == null)
            {
                LogError($"  ✗ {serviceName}: Registered but returns null");
                return false;
            }

            // Audio-specific validation
            if (service is IAudioService audioService)
            {
                if (!audioService.IsInitialized)
                {
                    LogError($"  ✗ {serviceName}: IsInitialized returned false");
                    return false;
                }
            }

            Log($"  ✓ {serviceName}: Valid");
            return true;
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ServiceBootstrap] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[ServiceBootstrap] {message}");
        }

        #endregion
    }
}
