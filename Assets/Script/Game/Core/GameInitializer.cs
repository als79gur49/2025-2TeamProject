using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.Components;
using Game.Services;
using PlasticPipe.PlasticProtocol.Messages;
using Game;


/// <summary>
/// 게임 초기화 매니저 - 모든 서비스 등록 및 의존성 주입 설정
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("서비스 참조")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameServiceManager gameServiceManager;
    [SerializeField] private CardServiceManager cardServiceManager; // 신규 참조 추가
    [SerializeField] private ResourceManager resourceManager; // Phase 2: 자원 관리 서비스 추가

    [Header("초기화 설정")]
    [SerializeField] private bool autoInitializeOnStart = true;
    [SerializeField] private bool logInitializationSteps = true;


    private void Awake()
    {
        //if (autoInitializeOnStart)
        //{
        //    InitializeGame();
        //}
    }
    private void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeGame();
        }
    }

    /// <summary>
    /// 게임 시스템 초기화
    /// </summary>
    public void InitializeGame()
    {
        Log("Starting game initialization...");

        // 1. 서비스 로케이터 초기화
        InitializeServiceLocator();

        // 2. 핵심 서비스 등록
        RegisterCoreServices();

        // 3. 컴포넌트 서비스 등록
        RegisterComponentServices();

        // 4. 의존성 주입 완료 확인
        ValidateServices();

        // 5. 초기화 완료 마킹
        ServiceLocator.MarkAsInitialized();

        Log("Game initialization completed successfully!");
    }

    /// <summary>
    /// 서비스 로케이터 초기화
    /// </summary>
    private void InitializeServiceLocator()
    {
        Log("Initializing ServiceLocator...");

        // 이전 서비스들 정리 (에디터에서 재시작할 때)
        ServiceLocator.Clear();

        Log("ServiceLocator cleared and ready");
    }

    /// <summary>
    /// 핵심 서비스 등록
    /// </summary>
    private void RegisterCoreServices()
    {
        Log("Registering core services...");

        // GridManager를 통한 중앙집중형 Grid 서비스 등록
        if (gridManager != null)
        {
            // GridManager가 자체적으로 그리드 시스템을 초기화하도록 설정
            gridManager.InitializeForServiceLocator();
            
            // GridManager에서 생성된 컴포넌트들을 가져와서 서비스 등록
            RegisterGridServices();
            Log("✅ Grid services registered via GridManager");
        }
        else
        {
            LogError("❌ GridManager not found - Grid services not registered");
        }

        // Game Services 등록 - 직접 참조를 통한 안전한 등록
        RegisterGameServices();

        // Card Services 등록 - CardServiceManager를 통한 카드 시스템 등록
        RegisterCardServices();

        // Resource Services 등록 - ResourceManager를 통한 자원 관리 시스템 등록
        RegisterResourceServices();
    }

    /// <summary>
    /// GridManager를 통한 그리드 서비스 등록 - 중앙집중형 구조
    /// </summary>
    private void RegisterGridServices()
    {
        Log("Registering GridManager and associated grid services");

        if (gridManager != null)
        {
            // GridManager를 IGridManager로 등록
            ServiceLocator.Register<IGridManager>(gridManager);
            Log("✅ IGridManager registered");

            // GridController를 IGridController로 등록 (GridManager에서 접근 가능)
            var gridController = gridManager.GetGridController();
            if (gridController != null)
            {
                ServiceLocator.Register<IGridController>(gridController);
                Log("✅ IGridController registered");
            }
            else
            {
                LogError("❌ GridController not available from GridManager");
            }

            // GridState와 GridRenderer도 등록 (필요한 경우)
            var gridState = gridManager.GetGridState();
            var gridRenderer = gridManager.GetGridRenderer();
            
            if (gridState != null)
            {
                ServiceLocator.Register<IGridState>(gridState);
                Log("✅ IGridState registered");
            }
            
            if (gridRenderer != null)
            {
                ServiceLocator.Register<IGridRenderer>(gridRenderer);
                Log("✅ IGridRenderer registered");
            }
        }
        else
        {
            LogError("❌ GridManager not assigned in inspector");
        }

        Log("Grid services registration completed via centralized structure");
    }

    /// <summary>
    /// 게임 서비스들 등록 - 직접 참조를 통한 안전한 등록
    /// </summary>
    private void RegisterGameServices()
    {
        Log("Registering game services via direct references...");

        // GameServiceManager 등록
        if (gameServiceManager != null)
        {
            ServiceLocator.Register<IGameServiceManager>(gameServiceManager);
            Log("✅ IGameServiceManager registered via direct reference");
        }
        else
        {
            LogError("❌ GameServiceManager reference not assigned in inspector");
        }

        Log("Game services registration completed");
    }

    /// <summary>
    /// 카드 서비스들 등록 - CardServiceManager를 통한 카드 시스템 등록
    /// </summary>
    private void RegisterCardServices()
    {
        Log("Registering card services via CardServiceManager...");

        // CardServiceManager 등록
        if (cardServiceManager != null)
        {
            cardServiceManager.InitializeAndRegisterServices();
            Log("✅ Card services registered via CardServiceManager");
        }
        else
        {
            LogError("❌ CardServiceManager not found - Card services not registered");
        }

        Log("Card services registration completed");
    }

    /// <summary>
    /// 자원 관리 서비스 등록 - ResourceManager를 통한 자원 시스템 등록
    /// </summary>
    private void RegisterResourceServices()
    {
        Log("Registering resource services via ResourceManager...");

        // ResourceManager 등록
        if (resourceManager != null)
        {
            resourceManager.Initialize();
            ServiceLocator.Register<IResourceManager>(resourceManager);
            Log("✅ ResourceManager initialized and registered");
        }
        else
        {
            LogError("❌ ResourceManager not found - Resource services not registered");
        }

        Log("Resource services registration completed");
    }

    /// <summary>
    /// 컴포넌트 서비스 등록
    /// </summary>
    private void RegisterComponentServices()
    {
        Log("Registering component services...");

        // 씬에 있는 모든 HealthComponent를 찾아서 의존성 주입
        var healthComponents = FindObjectsOfType<HealthComponent>();
        foreach (var health in healthComponents)
        {
            health.InjectDependencies();
            Log($"✅ HealthComponent dependencies injected: {health.gameObject.name}");
        }

        // ActionHandler 컴포넌트들 의존성 주입
        var actionHandlers = FindObjectsOfType<ActionHandler>();
        foreach (var handler in actionHandlers)
        {
            handler.InjectDependencies();
            Log($"✅ ActionHandler dependencies injected: {handler.gameObject.name}");
        }

        // ActionValidator 컴포넌트들 의존성 주입
        var actionValidators = FindObjectsOfType<ActionValidator>();
        foreach (var validator in actionValidators)
        {
            validator.InjectDependencies();
            Log($"✅ ActionValidator dependencies injected: {validator.gameObject.name}");
        }

    }

    /// <summary>
    /// 서비스 유효성 검증
    /// </summary>
    private void ValidateServices()
    {
        Log("Validating services...");

        var registeredServices = ServiceLocator.GetRegisteredServices();
        Log($"Total registered services: {registeredServices.Count}");

        foreach (var service in registeredServices)
        {
            Log($"  - {service.Key.Name}: {service.Value.GetType().Name}");
        }

        // Grid 서비스 확인
        if (!ServiceLocator.IsRegistered<IGridManager>())
        {
            LogError("❌ Critical service missing: IGridManager");
        }

        // Game 서비스 확인   
        if (!ServiceLocator.IsRegistered<IGameServiceManager>())
        {
            LogError("❌ Critical service missing: IGameServiceManager");
        }

        // Card 서비스 확인
        if (!ServiceLocator.IsRegistered<ICardServiceManager>())
        {
            LogError("❌ Critical service missing: ICardServiceManager");
        }

        // 서비스 상태 검증 (파괴된 MonoBehaviour 정리)
        ServiceLocator.ValidateServices();

        Log("Service validation completed");
    }

    /// <summary>
    /// 런타임에서 새 오브젝트의 의존성 주입
    /// </summary>
    public void InjectDependenciesForNewObject(GameObject newObject)
    {
        if (newObject == null) return;

        var components = newObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (var component in components)
        {
            component.InjectDependencies();
            Log($"✅ Dependencies injected for new object: {component.gameObject.name}");
        }
    }

    /// <summary>
    /// 서비스 재등록 (런타임 중 필요한 경우)
    /// </summary>
    public void ReregisterService<T>(T implementation) where T : class
    {
        ServiceLocator.Register<T>(implementation);
        Log($"✅ Service re-registered: {typeof(T).Name}");
    }

    /// <summary>
    /// 게임 종료 시 정리
    /// </summary>
    private void OnApplicationQuit()
    {
        Log("Cleaning up services on application quit...");
        ServiceLocator.Clear();
    }

    /// <summary>
    /// 에디터에서 플레이 모드 종료 시 정리
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Log("Application paused - validating services...");
            ServiceLocator.ValidateServices();
        }
    }

    private void Log(string message)
    {
        if (logInitializationSteps)
        {
            Debug.Log($"[GameInitializer] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GameInitializer] {message}");
    }

    // ✅ 에디터용 도구들
#if UNITY_EDITOR
    [Header("에디터 도구")]
    [SerializeField] private bool showDebugInfo = true;

    private void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("Service Locator Debug");

        if (ServiceLocator.IsInitialized)
        {
            GUILayout.Label("✅ ServiceLocator Initialized");
        }
        else
        {
            GUILayout.Label("❌ ServiceLocator Not Initialized");
        }

        var services = ServiceLocator.GetRegisteredServices();
        GUILayout.Label($"Registered Services: {services.Count}");

        foreach (var service in services)
        {
            GUILayout.Label($"  {service.Key.Name}");
        }

        if (GUILayout.Button("Re-Initialize"))
        {
            InitializeGame();
        }

        if (GUILayout.Button("Clear Services"))
        {
            ServiceLocator.Clear();
        }

        GUILayout.EndArea();
    }

    [UnityEditor.MenuItem("Game/Initialize Services")]
    private static void InitializeServicesMenuItem()
    {
        var initializer = FindObjectOfType<GameInitializer>();
        if (initializer != null)
        {
            initializer.InitializeGame();
            Debug.Log("Services initialized via menu");
        }
        else
        {
            Debug.LogWarning("GameInitializer not found in scene");
        }
    }

    [UnityEditor.MenuItem("Game/Validate Services")]
    private static void ValidateServicesMenuItem()
    {
        var registeredServices = ServiceLocator.GetRegisteredServices();
        Debug.Log($"Currently registered services: {registeredServices.Count}");

        foreach (var service in registeredServices)
        {
            Debug.Log($"  {service.Key.Name}: {service.Value?.GetType().Name ?? "NULL"}");
        }

        ServiceLocator.ValidateServices();
    }
#endif
}

/// <summary>
/// 자동 의존성 주입 컴포넌트 (새로 생성되는 오브젝트용)
/// </summary>
public class AutoInjectDependencies : MonoBehaviour
{
    [SerializeField] private bool injectOnStart = true;
    [SerializeField] private bool injectOnEnable = false;

    private void Start()
    {
        if (injectOnStart)
        {
            InjectDependencies();
        }
    }

    private void OnEnable()
    {
        if (injectOnEnable && ServiceLocator.IsInitialized)
        {
            InjectDependencies();
        }
    }

    private void InjectDependencies()
    {
        var components = GetComponentsInChildren<MonoBehaviour>();
        foreach (var component in components)
        {
            component.InjectDependencies();
        }

        Debug.Log($"[AutoInjectDependencies] Dependencies injected for {gameObject.name}");
    }
}
