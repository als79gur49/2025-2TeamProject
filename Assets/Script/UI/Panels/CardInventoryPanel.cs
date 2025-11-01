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
        /// 좌측 덱 빌더와 우측 인벤토리를 함께 활성화
        /// </summary>
        protected override void OnShowPanel()
        {
            base.OnShowPanel();

            Debug.Log("[CardInventoryPanel] Panel shown with both child panels");
        }

        /// <summary>
        /// 패널 숨김
        /// 두 하위 패널을 함께 비활성화
        /// </summary>
        protected override void OnHidePanel()
        {
            base.OnHidePanel();

            Debug.Log("[CardInventoryPanel] Panel hidden with both child panels");
        }

        /// <summary>
        /// 정리 작업
        /// 버튼 리스너 해제 및 리소스 정리
        /// </summary>
        protected override void OnCleanup()
        {
            base.OnCleanup();

            // 버튼 리스너 해제
            if (openButton != null)
                openButton.onClick.RemoveAllListeners();
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();

            Debug.Log("[CardInventoryPanel] Cleanup complete");
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
        }

        #endregion

    }
}
