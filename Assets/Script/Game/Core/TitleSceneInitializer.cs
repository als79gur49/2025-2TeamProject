using UnityEngine;
using Game.Initialization;
using Game.UI.Panels;
using Game.UI.Coordinators;
using Game.Managers;
using Game.SaveSystem;

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

        #region Scene Manager Registration

        /// <summary>
        /// Phase 2: 로컬 서비스 등록
        /// ShopManager 초기화 및 ServiceLocator에 등록
        /// </summary>
        protected override void RegisterLocalServices()
        {
            base.RegisterLocalServices();

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
            // 현재는 추가 설정 없음

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

        #region Editor Utilities

#if UNITY_EDITOR
        [ContextMenu("Validate TitleScene Setup")]
        private void ValidateSetup()
        {
            Debug.Log("=== TitleScene Setup Validation ===");

            // LocalUIPanelManager 확인
            var pm = FindObjectOfType<LocalUIPanelManager>();
            Debug.Log($"LocalUIPanelManager: {(pm != null ? "✅ Found" : "❌ Not Found")}");

            // Coordinator 확인
            Debug.Log($"DeckInventoryCoordinator: {(deckInventoryCoordinator != null ? "✅ Assigned" : "❌ Not Assigned")}");

            // 패널 확인
            if (pm != null)
            {
                var inv = pm.GetComponentInChildren<InventoryPanel>();
                var deck = pm.GetComponentInChildren<DeckBuilderPanel>();
                var shop = pm.GetComponentInChildren<ShopPanel>();

                Debug.Log($"InventoryPanel: {(inv != null ? "✅ Found" : "❌ Not Found")}");
                Debug.Log($"DeckBuilderPanel: {(deck != null ? "✅ Found" : "❌ Not Found")}");
                Debug.Log($"ShopPanel: {(shop != null ? "✅ Found" : "❌ Not Found")}");
            }

            // CollectionManager 확인
            var collection = ServiceLocator.IsRegistered<ICardCollection>()
                ? ServiceLocator.Get<ICardCollection>()
                : null;
            Debug.Log($"CollectionManager: {(collection != null ? "✅ Available" : "❌ Not Available")}");

            Debug.Log("================================");
        }
#endif

        #endregion
    }
}
