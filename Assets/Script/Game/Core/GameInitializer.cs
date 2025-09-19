using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.Components;


    /// <summary>
    /// 게임 초기화 매니저 - 모든 서비스 등록 및 의존성 주입 설정
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("서비스 참조")]
        [SerializeField] private GridState gridState;
        
        [Header("초기화 설정")]
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private bool logInitializationSteps = true;

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

            // GridController와 GridServices를 통한 Grid 서비스 등록
            if (gridState != null)
            {
                var gridController = new GridController(gridState);
                ServiceLocator.Register<IGridController>(gridController);
                ServiceLocator.Register<IGridManager>(gridController);
                Log("✅ IGridController and IGridManager registered via GridController");
            }
            else
            {
                LogError("❌ GridState not found - Grid services not registered");
            }

            // 추가 핵심 서비스들을 여기에 등록할 수 있습니다
            // 예: TurnManager, CardManager, AudioManager 등
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

            // 필수 서비스 확인
            if (!ServiceLocator.IsRegistered<IGridManager>())
            {
                LogError("❌ Critical service missing: IGridManager");
            }
            
            if (!ServiceLocator.IsRegistered<IGridController>())
            {
                LogError("❌ Critical service missing: IGridController");
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
