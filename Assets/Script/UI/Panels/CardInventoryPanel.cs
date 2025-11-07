using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Panels
{
    /// <summary>
    /// 카드 인벤토리 통합 패널
    /// InventoryPanel과 DeckBuilderPanel을 포함하는 컨테이너 패널
    /// 두 패널을 함께 열고 닫으며, 레이아웃은 좌측(덱 빌더) + 우측(인벤토리)
    /// </summary>
    public class CardInventoryPanel : UIPanel, IOpenablePanel
    {
        [Header("Openable Panel Buttons")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Child Panels")]
        [SerializeField] private DeckBuilderPanel deckBuilderPanel;
        [SerializeField] private InventoryPanel inventoryPanel;

        // IOpenablePanel 구현
        public Button OpenButton => openButton;
        public Button CloseButton => closeButton;

        #region Lifecycle

        /// <summary>
        /// 의존성 없는 초기화 (Awake에서 호출)
        /// UI 컴포넌트 이벤트 설정 및 내부 상태 초기화
        /// </summary>
        protected override void OnInitializeSelf()
        {
            base.OnInitializeSelf();

            // 버튼 이벤트 바인딩
            if (openButton != null)
                openButton.onClick.AddListener(() => OnShow());
            if (closeButton != null)
                closeButton.onClick.AddListener(() => OnHide());

            // 자식 패널 찾기 (Inspector에서 할당하지 않은 경우 자동으로 찾기)
            if (deckBuilderPanel == null)
            {
                deckBuilderPanel = GetComponentInChildren<DeckBuilderPanel>(true);
                if (deckBuilderPanel != null)
                    Debug.Log("[CardInventoryPanel] DeckBuilderPanel found automatically");
            }

            if (inventoryPanel == null)
            {
                inventoryPanel = GetComponentInChildren<InventoryPanel>(true);
                if (inventoryPanel != null)
                    Debug.Log("[CardInventoryPanel] InventoryPanel found automatically");
            }

            // UI 컴포넌트 검증
            ValidateReferences();

            Debug.Log("[CardInventoryPanel] Self-initialized successfully (Awake)");
        }

        /// <summary>
        /// 의존성 있는 초기화 (Start에서 호출)
        /// 하위 패널 및 Coordinator 초기화
        /// </summary>
        protected override void OnInitializeWithDependencies()
        {
            base.OnInitializeWithDependencies();

            Debug.Log("[CardInventoryPanel] Dependency initialization complete (Start)");
        }

        /// <summary>
        /// 패널 표시
        /// 부모 패널을 먼저 활성화한 후, 자식 패널들을 활성화
        /// </summary>
        protected override void OnShowPanel()
        {
            base.OnShowPanel();

            // 자식 패널들도 활성화 (순서: DeckBuilderPanel → InventoryPanel)
            if (deckBuilderPanel != null)
            {
                deckBuilderPanel.OnShow();
                Debug.Log("[CardInventoryPanel] DeckBuilderPanel.OnShow() called");
            }
            else
            {
                Debug.LogWarning("[CardInventoryPanel] DeckBuilderPanel is null! Please assign it in the Inspector.");
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.OnShow();
                Debug.Log("[CardInventoryPanel] InventoryPanel.OnShow() called");
            }
            else
            {
                Debug.LogWarning("[CardInventoryPanel] InventoryPanel is null! Please assign it in the Inspector.");
            }

            Debug.Log("[CardInventoryPanel] Panel shown with both child panels activated");
        }

        /// <summary>
        /// 패널 숨김
        /// 자식 패널들을 먼저 숨긴 후, 부모 패널을 숨김
        /// </summary>
        protected override void OnHidePanel()
        {
            // 자식 패널들을 먼저 숨기기 (순서: DeckBuilderPanel → InventoryPanel → 부모)
            if (deckBuilderPanel != null)
            {
                deckBuilderPanel.OnHide();
                Debug.Log("[CardInventoryPanel] DeckBuilderPanel.OnHide() called");
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.OnHide();
                Debug.Log("[CardInventoryPanel] InventoryPanel.OnHide() called");
            }

            // 자식 패널들을 숨긴 후 부모 패널 숨기기
            base.OnHidePanel();

            Debug.Log("[CardInventoryPanel] All child panels hidden, then parent panel hidden");
        }

        /// <summary>
        /// 정리 작업
        /// 버튼 리스너 해제 및 리소스 정리
        /// </summary>
        protected override void OnCleanup()
        {
            // 자식 패널들 정리
            if (deckBuilderPanel != null)
            {
                deckBuilderPanel.Cleanup();
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.Cleanup();
            }

            base.OnCleanup();

            // 버튼 리스너 해제
            if (openButton != null)
                openButton.onClick.RemoveAllListeners();
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();

            Debug.Log("[CardInventoryPanel] Cleanup complete for parent and child panels");
        }

        #endregion

        #region Validation

        /// <summary>
        /// 필수 참조 검증
        /// </summary>
        private void ValidateReferences()
        {
            if (openButton == null)
                Debug.LogWarning("[CardInventoryPanel] OpenButton not assigned!");

            if (closeButton == null)
                Debug.LogWarning("[CardInventoryPanel] CloseButton not assigned!");

            if (deckBuilderPanel == null)
                Debug.LogWarning("[CardInventoryPanel] DeckBuilderPanel not found! Please assign it in the Inspector or ensure it exists as a child.");

            if (inventoryPanel == null)
                Debug.LogWarning("[CardInventoryPanel] InventoryPanel not found! Please assign it in the Inspector or ensure it exists as a child.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 외부에서 자식 패널에 접근이 필요한 경우를 위한 Getter
        /// </summary>
        public DeckBuilderPanel GetDeckBuilderPanel() => deckBuilderPanel;
        public InventoryPanel GetInventoryPanel() => inventoryPanel;

        #endregion

    }
}
