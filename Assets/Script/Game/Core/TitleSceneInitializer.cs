using UnityEngine;
using Game.Initialization;
using Game.UI.Panels;
using Game.UI.Coordinators;
using Game.Managers;
using Game.SaveSystem;
using Game.UI.Components;

namespace Game.Core
{
    /// <summary>
    /// TitleScene 전용 초기화 클래스
    /// 타이틀 화면에서 덱 빌더 기능을 제공하는 씬에서 사용
    ///
    /// 초기화 대상:
    /// - InventoryPanel: 플레이어가 소유한 카드 표시
    /// - DeckBuilderPanel: 덱 구성 및 편집
    /// - ShopPanel: 상점 UI 및 아이템 구매
    /// - DeckInventoryCoordinator: 두 패널 간 통신 중재
    ///
    /// 사용 방법:
    /// 1. TitleScene에 GameObject 생성
    /// 2. 이 스크립트 추가
    /// 3. Inspector에서 DeckInventoryCoordinator 할당
    /// 4. LocalUIPanelManager가 씬에 존재하는지 확인
    /// </summary>
    public class TitleSceneInitializer : SceneInitializer
    {
        [Header("TitleScene 전용 참조")]
        [SerializeField] private DeckInventoryCoordinator deckInventoryCoordinator;

        [Header("Scene Managers")]
        [SerializeField] private ShopManager shopManager;

        [Header("TitleScene 전용 DeckSwitcher")]
        [SerializeField] private DeckSwitcher deckSwitcher;

        [Header("Debug Options")]
        [SerializeField] private bool showDebugInfo = false;

        #region Scene Manager Registration

        /// <summary>
        /// Phase 2: 로컬 서비스 등록
        /// ShopManager 초기화 및 ServiceLocator에 등록
        /// </summary>
        protected override void RegisterLocalServices()
        {
            base.RegisterLocalServices();

            // ServiceBootstrap에서 등록된 글로벌 서비스 상세 검증
            ValidateBootstrapServices();

            Log("[Phase 2.5] Registering Scene Managers...");

            // ShopManager 등록
            if (shopManager == null)
            {
                LogWarning("   ⚠️ ShopManager not assigned! Searching in scene...");
                shopManager = FindObjectOfType<ShopManager>();
            }

            if (shopManager != null)
            {
                // ShopManager 초기화 (의존성 주입)
                var playerData = ServiceLocator.Get<IPlayerDataManager>();
                var collection = ServiceLocator.Get<ICardCollection>();
                var saveAdapter = ServiceLocator.Get<ISaveDataAdapter>();

                if (playerData == null)
                    LogError("   ❌ IPlayerDataManager not found in ServiceLocator!");
                if (collection == null)
                    LogError("   ❌ ICardCollection not found in ServiceLocator!");
                if (saveAdapter == null)
                    LogError("   ❌ ISaveDataAdapter not found in ServiceLocator!");

                if (playerData != null && collection != null && saveAdapter != null)
                {
                    shopManager.Initialize(playerData, collection, saveAdapter);

                    // 일반 서비스 등록 방식 사용
                    ServiceLocator.Register<IShopManager>(shopManager);
                    Log("   ✓ ShopManager registered as IShopManager");
                }
            }
            else
            {
                LogError("   ❌ ShopManager not found!");
            }

            Log("✅ Scene Managers registered successfully");
        }

        #endregion

        #region Bootstrap Service Validation

        /// <summary>
        /// ServiceBootstrap에서 등록된 글로벌 서비스 확인
        /// </summary>
        private void ValidateBootstrapServices()
        {
            Log("Validating Bootstrap services...");

            // SaveDataAdapter 확인 (DeckSwitcher 및 상점 등에 필요)
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

        #region UI Panel and Coordinator Initialization

        /// <summary>
        /// Phase 3: UI 패널 초기화
        /// InventoryPanel, DeckBuilderPanel, ShopPanel 초기화
        /// </summary>
        protected override void InitializeUIPanels()
        {
            Log("[Phase 3] Initializing TitleScene UI Panels...");

            // panelManager는 SceneInitializer의 protected 필드
            if (panelManager == null)
            {
                LogError("❌ LocalUIPanelManager not found!");
                return;
            }

            // 패널 가져오기
            var inventory = UIPanelFacade.GetPanel<InventoryPanel>();
            var deck = UIPanelFacade.GetPanel<DeckBuilderPanel>();
            var shop = UIPanelFacade.GetPanel<ShopPanel>();

            // Null 체크
            if (inventory == null || deck == null || shop == null)
            {
                LogError("❌ Required panels not found!");
                if (inventory == null)
                    LogError("   - InventoryPanel not found in LocalUIPanelManager!");
                if (deck == null)
                    LogError("   - DeckBuilderPanel not found in LocalUIPanelManager!");
                if (shop == null)
                    LogError("   - ShopPanel not found in LocalUIPanelManager!");
                LogError("   Make sure all panels are children of LocalUIPanelManager and implement IUIPanel");
                return;
            }

            // 명시적 순서로 초기화
            Log("   Initializing InventoryPanel...");

            // CollectionManager는 ServiceBootstrap에서 전역 서비스로 등록됨
            var collectionManager = ServiceLocator.Get<ICardCollection>();
            if (collectionManager == null)
            {
                LogError("❌ CollectionManager not available from ServiceLocator!");
                return;
            }
            inventory.Initialize(collectionManager);

            Log("   Initializing DeckBuilderPanel...");
            deck.Initialize();

            Log("   Initializing ShopPanel...");
            shop.Initialize();

            Log("✅ TitleScene UI Panels initialized successfully");

            // DeckSwitcher 초기화 (선택적)
            InitializeDeckSwitcher();
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
        /// DeckInventoryCoordinator 초기화 (모든 패널 초기화 완료 후)
        /// </summary>
        protected override void InitializeCoordinators()
        {
            Log("[Phase 4] Initializing TitleScene Coordinators...");

            // Coordinator 존재 확인
            if (deckInventoryCoordinator == null)
            {
                LogError("❌ DeckInventoryCoordinator not assigned in Inspector!");
                LogError("   Please assign DeckInventoryCoordinator in TitleSceneInitializer Inspector");
                return;
            }

            // 패널 가져오기 (이미 초기화 완료 보장)
            var inventory = UIPanelFacade.GetPanel<InventoryPanel>();
            var deck = UIPanelFacade.GetPanel<DeckBuilderPanel>();

            // Null 체크 (Phase 3에서 이미 확인했지만 안전을 위해)
            if (inventory == null || deck == null)
            {
                LogError("❌ Panels not initialized properly - cannot initialize Coordinator");
                return;
            }

            // Coordinator 초기화
            Log("   Initializing DeckInventoryCoordinator...");

            // CollectionManager는 ServiceBootstrap에서 전역 서비스로 등록됨
            var collectionManager = ServiceLocator.Get<ICardCollection>();
            if (collectionManager == null)
            {
                LogError("❌ CollectionManager not available from ServiceLocator!");
                return;
            }

            deckInventoryCoordinator.Initialize(inventory, deck, collectionManager);

            // SettingsCoordinator 초기화
            InitializeSettingsCoordinator();

            Log("✅ TitleScene Coordinators initialized successfully");
        }

        /// <summary>
        /// SettingsCoordinator 초기화 (SettingsPanel 연결)
        /// </summary>
        private void InitializeSettingsCoordinator()
        {
            Log("   Initializing SettingsCoordinator...");

            // SettingsPanel 찾기
            var settingsPanel = UIPanelFacade.GetPanel<SettingsPanel>();

            if (settingsPanel == null)
            {
                // LocalUIPanelManager에서 못 찾으면 직접 검색
                settingsPanel = FindObjectOfType<SettingsPanel>();
            }

            if (settingsPanel == null)
            {
                LogWarning("   ⚠️ SettingsPanel not found - SettingsCoordinator not initialized");
                return;
            }

            // GameObject 생성 및 컴포넌트 추가
            var coordinatorGO = new GameObject("SettingsCoordinator");
            coordinatorGO.transform.SetParent(transform); // TitleSceneInitializer 자식으로 배치

            var coordinator = coordinatorGO.AddComponent<SettingsCoordinator>();

            // 초기화 (SettingsPanel 연결 + 이벤트 구독)
            coordinator.Initialize(settingsPanel);

            Log("   ✅ SettingsCoordinator initialized and connected to SettingsPanel");
        }

        #endregion

        #region Optional Overrides

        /// <summary>
        /// Phase 5: TitleScene 특화 초기화 (선택적)
        /// 타이틀 씬만의 추가 설정이 필요한 경우 여기에 구현
        /// </summary>
        protected override void InitializeSceneSpecifics()
        {
            Log("[Phase 5] Initializing TitleScene specifics...");

            // 예: 덱 빌더 필터 설정, 정렬 옵션 로드 등

            // TitleScene 진입 시 현재 진행 중인 스테이지 컨텍스트 초기화
            if (ServiceLocator.IsRegistered<IStageProgressManager>())
            {
                var progressManager = ServiceLocator.Get<IStageProgressManager>();
                progressManager.ClearCurrentStage();
                Log("   ✓ Cleared currentStageId in StageProgressManager for TitleScene");
            }
            else
            {
                LogWarning("   ⚠️ IStageProgressManager not registered - cannot clear currentStageId for TitleScene");
            }

            Log("✅ TitleScene specifics initialized (none)");
        }

        /// <summary>
        /// 초기화 완료 시 호출
        /// </summary>
        protected override void OnInitializationComplete()
        {
            base.OnInitializationComplete();

            Log("TitleScene initialization completed - ready for deck building");

            // 초기 패널 표시 등 (필요 시)
            // panelManager.ShowLocalPanel<InventoryPanel>();
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
    }
}
