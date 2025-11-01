using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.Components;
using Game.Services;
using Game.Coordinators;
using Game.Initialization;
using PlasticPipe.PlasticProtocol.Messages;
using Game;


/// <summary>
/// 게임 초기화 매니저 - 모든 서비스 등록 및 의존성 주입 설정
/// PrototypeTestScene (게임 씬) 전용 초기화 클래스
///
/// Template Method 패턴을 사용하여 SceneInitializer 상속
/// - 서비스 초기화는 기존 로직 유지
/// - UI 패널/Coordinator 초기화는 SceneInitializer 워크플로우 따름
/// </summary>
public class GameInitializer : SceneInitializer
{
    [Header("서비스 참조")]
    [SerializeField] private GlobalStateManager globalStateManager; // 전역 상태 관리 서비스 (최우선 초기화)
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameServiceManager gameServiceManager;
    [SerializeField] private CardServiceManager cardServiceManager; // 신규 참조 추가
    [SerializeField] private ResourceManager resourceManager; // Phase 2: 자원 관리 서비스 추가
    [SerializeField] private Game.VFX.SpellEffectExecutor spellEffectExecutor; // VFX 서비스 추가
    [SerializeField] private TeamConfigurationManager teamConfigurationManager; // 팀별 설정 관리 서비스 추가
    [SerializeField] private BaseManager baseManager; // Base 관리 서비스 추가
    [SerializeField] private GameOutcomeManager gameOutcomeManager; // 승/패 조건 관리 서비스 추가
    [SerializeField] private GameUICoordinator gameUICoordinator; // 게임-UI 이벤트 중재 서비스 추가

    [Header("Damage Display Services")]
    [SerializeField] private Game.Repositories.DamageDisplayRepository damageDisplayRepository; // 데미지 표시 Repository
    [SerializeField] private Game.Services.DamageDisplayService damageDisplayService; // 데미지 표시 Service
    [SerializeField] private DamageDisplayEventChannelSO damageDisplayEventChannel; // 데미지 표시 EventChannel
    [SerializeField] private GameObject damagePopupPrefab; // 데미지 팝업 프리팹

    // ✅ autoInitializeOnStart, logInitializationSteps는 SceneInitializer에서 상속


    // ✅ Awake는 제거 - 서비스 초기화는 Start()에서 수행

    /// <summary>
    /// Start 오버라이드 - 서비스 초기화 후 SceneInitializer 워크플로우 실행
    /// </summary>
    protected override void Start()
    {
        if (autoInitializeOnStart)
        {
            // 1. 게임 서비스 초기화 (기존 로직 유지)
            InitializeGame();

            // 2. SceneInitializer 워크플로우 실행 (UI 패널/Coordinator 초기화)
            base.Start();
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

        // ✅ FIX: Bootstrap 서비스가 있으면 Clear하지 않음
        // Bootstrap(ServiceBootstrap)이 이미 글로벌 서비스를 등록했으므로 보존해야 함
        // ISceneTransitionController 존재 여부로 Bootstrap 초기화 확인
        if (ServiceLocator.IsRegistered<ISceneTransitionController>())
        {
            Log("  ✓ Bootstrap services detected - preserving global services (ISceneTransitionController, IVolumeController, etc.)");
            Log("  → Game services will be registered alongside Bootstrap services");
            // Bootstrap 서비스는 유지하고 게임 서비스만 추가 등록
        }
        else
        {
            // Bootstrap 없이 씬을 직접 실행하는 경우에만 Clear
            // (에디터에서 PrototypeTestScene을 단독으로 Play할 때)
            Log("  ⚠ No Bootstrap detected - clearing ServiceLocator for standalone scene initialization");
            Log("  → Global services (SceneTransition, Audio) will NOT be available");
            ServiceLocator.Clear();
        }

        Log("ServiceLocator initialization complete");
    }

    /// <summary>
    /// 핵심 서비스 등록
    /// </summary>
    private void RegisterCoreServices()
    {
        Log("Registering core services...");

        // ✅ 최우선: GlobalStateManager 등록 - 모든 서비스보다 먼저 초기화
        RegisterGlobalStateManager();

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

        // Resource Services 등록 - ResourceManager를 통한 자원 관리 시스템 등록
        RegisterResourceServices();

        // Card Services 등록 - CardServiceManager를 통한 카드 시스템 등록
        RegisterCardServices();

        // VFX Services 등록 - SpellEffectExecutor를 통한 VFX 시스템 등록
        RegisterVFXServices();

        // Team Configuration Services 등록 - TeamConfigurationManager를 통한 팀별 설정 시스템 등록
        RegisterTeamConfigurationServices();

        // Game Outcome Services 등록 - GameOutcomeManager를 통한 승/패 조건 관리
        RegisterGameOutcomeServices();

        // Damage Display Services 등록 - Repository Pattern을 통한 데미지 표시 시스템
        RegisterDamageDisplayServices();
    }

    /// <summary>
    /// GlobalStateManager 등록 - 전역 상태 관리 시스템 (최우선 초기화)
    /// </summary>
    private void RegisterGlobalStateManager()
    {
        Log("Registering GlobalStateManager (highest priority)...");

        if (globalStateManager != null)
        {
            // GlobalStateManager의 Awake()가 이미 ServiceLocator에 등록했는지 확인
            var registered = ServiceLocator.Get<IGlobalStateManager>();

            if (registered != null)
            {
                Log("✅ IGlobalStateManager already registered in Awake()");
            }
            else
            {
                // 만약 등록되지 않았다면 수동 등록
                ServiceLocator.Register<IGlobalStateManager>(globalStateManager);
                Log("✅ IGlobalStateManager manually registered");
            }
        }
        else
        {
            LogError("❌ GlobalStateManager not found - Global state management not available");
        }

        Log("GlobalStateManager registration completed");
    }

    /// <summary>
    /// GridManager를 통한 그리드 서비스 등록 - 중앙집중형 구조
    /// 하위 서비스(GridController, GridState, GridRenderer)는 GridManager를 통해 접근
    /// </summary>
    private void RegisterGridServices()
    {
        Log("Registering GridManager for centralized grid service access");

        if (gridManager != null)
        {
            // GridManager만 ServiceLocator에 등록
            // 하위 서비스들은 GridManager의 GetGridController(), GetGridState(), GetGridRenderer()를 통해 접근
            ServiceLocator.Register<IGridManager>(gridManager);
            Log("✅ IGridManager registered");
            Log("   Sub-services accessible via: GridManager.GetGridController(), GetGridState(), GetGridRenderer()");
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
    /// VFX 서비스들 등록 - SpellEffectExecutor를 통한 VFX 시스템 등록
    /// </summary>
    private void RegisterVFXServices()
    {
        Log("Registering VFX services via SpellEffectExecutor...");

        // SpellEffectExecutor 등록
        if (spellEffectExecutor != null)
        {
            ServiceLocator.Register<Game.VFX.ISpellEffectExecutor>(spellEffectExecutor);
            Log("✅ ISpellEffectExecutor registered");
        }
        else
        {
            LogError("❌ SpellEffectExecutor not found - VFX services not registered");
        }

        Log("VFX services registration completed");
    }

    /// <summary>
    /// 팀별 설정 서비스 등록 - TeamConfigurationManager를 통한 팀별 Material 등 설정 관리
    /// </summary>
    private void RegisterTeamConfigurationServices()
    {
        Log("Registering team configuration services via TeamConfigurationManager...");

        // TeamConfigurationManager 등록
        if (teamConfigurationManager != null)
        {
            teamConfigurationManager.Initialize();
            ServiceLocator.Register<ITeamConfigurationManager>(teamConfigurationManager);
            Log("✅ TeamConfigurationManager initialized and registered");
        }
        else
        {
            LogError("❌ TeamConfigurationManager not found - Team configuration services not registered");
        }

        Log("Team configuration services registration completed");
    }

    /// <summary>
    /// Game Outcome 관리 서비스 등록 - GameOutcomeManager와 GameUICoordinator를 통한 승/패 조건 관리 및 UI 연동
    /// </summary>
    private void RegisterGameOutcomeServices()
    {
        Log("Registering Game Outcome management services via GameOutcomeManager...");

        // GameOutcomeManager 등록
        if (gameOutcomeManager != null)
        {
            // GameOutcomeManager 의존성 주입
            IBaseManager baseManagerInterface = baseManager;

            if (baseManagerInterface != null)
            {
                gameOutcomeManager.InjectDependencies(baseManagerInterface);
                gameOutcomeManager.Initialize();
                ServiceLocator.Register<IGameOutcomeManager>(gameOutcomeManager);
                Log("✅ GameOutcomeManager initialized and registered");
            }
            else
            {
                LogError("❌ GameOutcomeManager dependency missing - BaseManager is null");
            }
        }
        else
        {
            LogError("❌ GameOutcomeManager not found - Game outcome management services not registered");
        }

        // GameUICoordinator 등록 (Mediator Pattern - 게임 이벤트와 UI 연결)
        if (gameUICoordinator != null)
        {
            gameUICoordinator.Init();
            Log("✅ GameUICoordinator initialized (mediates game events to UI)");
        }
        else
        {
            LogError("❌ GameUICoordinator not found - Game-to-UI coordination not available");
        }

        Log("Game outcome management services registration completed");
    }

    /// <summary>
    /// Damage Display 서비스 등록 - Repository Pattern 기반 데미지 표시 시스템
    /// </summary>
    private void RegisterDamageDisplayServices()
    {
        Log("Registering Damage Display services...");

        // DamageDisplayRepository 초기화 및 등록
        if (damageDisplayRepository != null && damageDisplayEventChannel != null)
        {
            damageDisplayRepository.Initialize(damageDisplayEventChannel);
            ServiceLocator.Register<Game.Repositories.IDamageDisplayRepository>(damageDisplayRepository);
            Log("✅ DamageDisplayRepository initialized and registered");
        }
        else
        {
            LogError("❌ DamageDisplayRepository or EventChannel not assigned");
        }

        // DamageDisplayService 초기화 및 등록
        if (damageDisplayService != null && damageDisplayEventChannel != null && damagePopupPrefab != null)
        {
            damageDisplayService.Initialize(damageDisplayEventChannel, damagePopupPrefab);
            ServiceLocator.Register<Game.Services.IDamageDisplayService>(damageDisplayService);
            Log("✅ DamageDisplayService initialized and registered");
        }
        else
        {
            LogError("❌ DamageDisplayService, EventChannel, or Popup Prefab not assigned");
        }

        Log("Damage Display services registration completed");
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

        // GlobalStateManager 서비스 확인 (최우선)
        if (!ServiceLocator.IsRegistered<IGlobalStateManager>())
        {
            LogError("❌ Critical service missing: IGlobalStateManager");
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

        // VFX 서비스 확인
        if (!ServiceLocator.IsRegistered<Game.VFX.ISpellEffectExecutor>())
        {
            LogError("❌ Critical service missing: ISpellEffectExecutor");
        }

        // Team Configuration 서비스 확인
        if (!ServiceLocator.IsRegistered<ITeamConfigurationManager>())
        {
            LogError("❌ Critical service missing: ITeamConfigurationManager");
        }

        // Game Outcome 서비스 확인
        if (!ServiceLocator.IsRegistered<IGameOutcomeManager>())
        {
            LogError("❌ Critical service missing: IGameOutcomeManager");
        }

        // Damage Display 서비스 확인
        if (!ServiceLocator.IsRegistered<Game.Repositories.IDamageDisplayRepository>())
        {
            LogError("❌ Warning: IDamageDisplayRepository not registered");
        }

        if (!ServiceLocator.IsRegistered<Game.Services.IDamageDisplayService>())
        {
            LogError("❌ Warning: IDamageDisplayService not registered");
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

    #region SceneInitializer Abstract Methods Implementation

    /// <summary>
    /// Phase 3: UI 패널 초기화
    /// PrototypeTestScene (게임 씬)에는 덱 빌더 UI가 없으므로 최소한만 초기화
    /// </summary>
    protected override void InitializeUIPanels()
    {
        Log("[Phase 3] Initializing PrototypeTestScene UI Panels...");

        // PrototypeTestScene은 게임 씬이므로 인벤토리/덱 빌더 패널이 없음
        // 게임 결과 패널 등 게임 씬 전용 UI만 존재

        Log("✅ PrototypeTestScene UI Panels initialized (minimal)");
    }

    /// <summary>
    /// Phase 4: Coordinator 초기화
    /// GameUICoordinator는 이미 RegisterGameOutcomeServices()에서 초기화됨
    /// </summary>
    protected override void InitializeCoordinators()
    {
        Log("[Phase 4] Initializing PrototypeTestScene Coordinators...");

        // GameUICoordinator는 이미 RegisterGameOutcomeServices()에서 Init() 호출됨
        // DeckInventoryCoordinator는 이 씬에 없음

        Log("✅ PrototypeTestScene Coordinators initialized (GameUICoordinator already initialized)");
    }

    #endregion
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
