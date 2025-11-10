using UnityEngine;
using Game.Core;
using Game.Initialization;
using Game.SaveSystem;
using Game.Managers;
using Game.UI.Components;

namespace Game.Core
{
    /// <summary>
    /// 스테이지 씬 초기화 매니저 - DeckSwitcher와 필요한 서비스 등록
    /// ServiceBootstrap에서 등록된 글로벌 서비스를 활용하며,
    /// 스테이지 씬 전용 UI 컴포넌트를 초기화합니다.
    ///
    /// Template Method 패턴을 사용하여 SceneInitializer 상속
    /// - 서비스 등록: RegisterServices()
    /// - UI 초기화: InitializeUIPanels()
    /// - Coordinator 초기화: InitializeCoordinators()
    /// </summary>
    public class StageSceneInitializer : SceneInitializer
    {
        [Header("Stage UI Components")]
        [SerializeField] private DeckSwitcher deckSwitcher;

        [Header("Debug Options")]
        [SerializeField] private bool showDebugInfo = false;

        #region SceneInitializer Lifecycle

        /// <summary>
        /// Start 오버라이드 - 서비스 등록 후 SceneInitializer 워크플로우 실행
        /// </summary>
        protected override void Start()
        {
            if (autoInitializeOnStart)
            {
                // 1. 스테이지 전용 서비스 등록
                RegisterServices();

                // 2. SceneInitializer 워크플로우 실행 (UI 패널/Coordinator 초기화)
                base.Start();
            }
        }

        #endregion

        #region Service Registration

        /// <summary>
        /// 스테이지 씬에 필요한 서비스 등록
        /// ServiceBootstrap에서 이미 등록된 글로벌 서비스(SaveDataAdapter, PlayerDataManager)를 활용
        /// </summary>
        private void RegisterServices()
        {
            Log("Registering Stage Scene services...");

            // ServiceBootstrap에서 이미 등록된 글로벌 서비스 확인
            ValidateBootstrapServices();

            // 추가 스테이지 전용 서비스가 필요하면 여기서 등록
            // 예: 특정 스테이지 전용 매니저, 컨트롤러 등

            Log("Stage Scene services registration completed");
        }

        /// <summary>
        /// ServiceBootstrap에서 등록된 글로벌 서비스 확인
        /// </summary>
        private void ValidateBootstrapServices()
        {
            Log("Validating Bootstrap services...");

            // SaveDataAdapter 확인 (DeckSwitcher에 필요)
            if (!ServiceLocator.IsRegistered<ISaveDataAdapter>())
            {
                LogError("❌ Critical: ISaveDataAdapter not registered!");
                LogError("   Ensure ServiceBootstrap scene is loaded first.");
            }
            else
            {
                Log("✅ ISaveDataAdapter available from Bootstrap");
            }

            // PlayerDataManager 확인
            if (!ServiceLocator.IsRegistered<IPlayerDataManager>())
            {
                LogError("❌ Critical: IPlayerDataManager not registered!");
                LogError("   Ensure ServiceBootstrap scene is loaded first.");
            }
            else
            {
                Log("✅ IPlayerDataManager available from Bootstrap");
            }

            // StageProgressManager 확인
            if (!ServiceLocator.IsRegistered<IStageProgressManager>())
            {
                LogWarning("⚠️ Warning: IStageProgressManager not registered");
            }
            else
            {
                Log("✅ IStageProgressManager available from Bootstrap");
            }

            Log("Bootstrap services validation completed");
        }

        #endregion

        #region SceneInitializer Abstract Methods Implementation

        /// <summary>
        /// Phase 3: UI 패널 초기화
        /// 스테이지 씬의 UI 컴포넌트 초기화
        /// </summary>
        protected override void InitializeUIPanels()
        {
            Log("[Phase 3] Initializing Stage Scene UI Panels...");

            // DeckSwitcher 초기화
            InitializeDeckSwitcher();

            // 다른 스테이지 UI 패널이 있다면 여기서 초기화
            // 예: StageResultPanel, PauseMenuPanel 등

            Log("✅ Stage Scene UI Panels initialized");
        }

        /// <summary>
        /// DeckSwitcher 컴포넌트 초기화
        /// </summary>
        private void InitializeDeckSwitcher()
        {
            Log("   Initializing DeckSwitcher...");

            if (deckSwitcher == null)
            {
                // Inspector에 할당되지 않은 경우 씬에서 찾기
                deckSwitcher = FindObjectOfType<DeckSwitcher>();
            }

            if (deckSwitcher == null)
            {
                LogWarning("   ⚠️ DeckSwitcher not found in scene - Deck switching feature not available");
                return;
            }

            // DeckSwitcher는 UIPanel을 상속하므로 자동으로 OnInitializeWithDependencies() 호출됨
            // 여기서는 추가 설정이 필요한 경우만 처리
            Log("   ✅ DeckSwitcher initialized successfully");
        }

        /// <summary>
        /// Phase 4: Coordinator 초기화
        /// 스테이지 씬에는 별도 Coordinator가 없으므로 최소한만 처리
        /// </summary>
        protected override void InitializeCoordinators()
        {
            Log("[Phase 4] Initializing Stage Scene Coordinators...");

            // 스테이지 씬에 Coordinator가 필요한 경우 여기서 초기화
            // 예: StageEventCoordinator, GameplayCoordinator 등

            Log("✅ Stage Scene Coordinators initialized (minimal)");
        }

        #endregion

        #region Public API

        /// <summary>
        /// 런타임에서 DeckSwitcher의 덱 목록을 새로고침
        /// (덱이 외부에서 추가/삭제된 경우 호출)
        /// </summary>
        public void RefreshDeckSwitcher()
        {
            if (deckSwitcher != null)
            {
                deckSwitcher.RefreshDeckList();
                Log("DeckSwitcher deck list refreshed");
            }
            else
            {
                LogWarning("Cannot refresh DeckSwitcher - component not found");
            }
        }

        /// <summary>
        /// 현재 선택된 덱 이름 가져오기
        /// </summary>
        public string GetCurrentSelectedDeck()
        {
            if (deckSwitcher != null)
            {
                return deckSwitcher.GetSelectedDeckName();
            }

            LogWarning("Cannot get selected deck - DeckSwitcher not found");
            return string.Empty;
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (logInitializationSteps)
            {
                Debug.Log($"[StageSceneInitializer] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[StageSceneInitializer] {message}");
        }

        private void LogWarning(string message)
        {
            if (logInitializationSteps)
            {
                Debug.LogWarning($"[StageSceneInitializer] {message}");
            }
        }

        #endregion

        #region Editor Debug Tools

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 220, 300, 150));
            GUILayout.Box("Stage Scene Debug");

            if (ServiceLocator.IsInitialized)
            {
                GUILayout.Label("✅ ServiceLocator Initialized");
            }
            else
            {
                GUILayout.Label("❌ ServiceLocator Not Initialized");
            }

            // DeckSwitcher 상태
            if (deckSwitcher != null)
            {
                GUILayout.Label($"DeckSwitcher: Active");
                string currentDeck = deckSwitcher.GetSelectedDeckName();
                if (!string.IsNullOrEmpty(currentDeck))
                {
                    GUILayout.Label($"  Current Deck: {currentDeck}");
                }
            }
            else
            {
                GUILayout.Label("DeckSwitcher: Not Found");
            }

            if (GUILayout.Button("Refresh DeckSwitcher"))
            {
                RefreshDeckSwitcher();
            }

            GUILayout.EndArea();
        }

        [UnityEditor.MenuItem("Game/Stage/Refresh Deck Switcher")]
        private static void RefreshDeckSwitcherMenuItem()
        {
            var initializer = FindObjectOfType<StageSceneInitializer>();
            if (initializer != null)
            {
                initializer.RefreshDeckSwitcher();
                Debug.Log("DeckSwitcher refreshed via menu");
            }
            else
            {
                Debug.LogWarning("StageSceneInitializer not found in scene");
            }
        }

        [UnityEditor.MenuItem("Game/Stage/Get Current Deck")]
        private static void GetCurrentDeckMenuItem()
        {
            var initializer = FindObjectOfType<StageSceneInitializer>();
            if (initializer != null)
            {
                string currentDeck = initializer.GetCurrentSelectedDeck();
                if (!string.IsNullOrEmpty(currentDeck))
                {
                    Debug.Log($"Current selected deck: {currentDeck}");
                }
                else
                {
                    Debug.Log("No deck selected");
                }
            }
            else
            {
                Debug.LogWarning("StageSceneInitializer not found in scene");
            }
        }
#endif

        #endregion
    }
}
