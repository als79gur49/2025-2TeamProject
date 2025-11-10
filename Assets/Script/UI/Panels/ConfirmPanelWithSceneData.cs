using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.SceneManagement;
using Game.Core;
using Game.Controllers;
using Game.Services;

namespace Game.UI.Panels
{
    /// <summary>
    /// SceneData를 보유한 확인 패널
    /// Inspector에서 미리 설정된 SceneData로 씬 전환을 수행합니다.
    /// 덱/카드 검증 실패 시 경고 메시지를 표시하고,
    /// 확인 버튼 클릭 시 설정된 씬(예: TitleScene)으로 이동합니다.
    /// </summary>
    public class ConfirmPanelWithSceneData : UIPanel, IDialogPanel
    {
        [Header("Scene Configuration")]
        [SerializeField]
        [Tooltip("Inspector에서 미리 설정된 SceneData (예: TitleScene)")]
        private SceneData sceneData;

        [Header("UI Components")]
        [SerializeField]
        private TextMeshProUGUI messageText;

        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private Button cancelButton;

        #region IDialogPanel Implementation

        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;

        #endregion

        #region Dependencies

        private ISceneTransitionController sceneTransitionController;

        #endregion

        #region Lifecycle

        protected override void OnInitializeWithDependencies()
        {
            // Get SceneTransitionController
            if (ServiceLocator.IsRegistered<ISceneTransitionController>())
            {
                sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();
            }
            else
            {
                Debug.LogError("[ConfirmPanelWithSceneData] ISceneTransitionController not found in ServiceLocator!");
            }

            // Setup confirm button listener
            // (Cancel button is auto-bound by GlobalUIPanelManager)
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirmButtonClick);
            }
            else
            {
                Debug.LogError("[ConfirmPanelWithSceneData] Confirm button is not assigned!");
            }

            // Cancel button validation
            if (cancelButton == null)
            {
                Debug.LogError("[ConfirmPanelWithSceneData] Cancel button is not assigned!");
            }
        }

        private void OnDestroy()
        {
            // Cleanup confirm button listener
            // (Cancel button is managed by GlobalUIPanelManager)
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 메시지를 설정하고 패널을 표시합니다.
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        public void ShowMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
            else
            {
                Debug.LogWarning("[ConfirmPanelWithSceneData] Message text component is not assigned!");
            }

            OnShow();
        }

        /// <summary>
        /// 메시지를 설정합니다 (패널은 표시하지 않음).
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
            else
            {
                Debug.LogWarning("[ConfirmPanelWithSceneData] Message text component is not assigned!");
            }
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// 확인 버튼 클릭 시 - Inspector에서 설정된 SceneData로 씬 전환
        /// </summary>
        private void OnConfirmButtonClick()
        {
            if (sceneData == null)
            {
                Debug.LogError("[ConfirmPanelWithSceneData] SceneData is not assigned in Inspector!");
                OnHide();
                return;
            }

            if (sceneTransitionController == null)
            {
                Debug.LogError("[ConfirmPanelWithSceneData] ISceneTransitionController is not available!");
                OnHide();
                return;
            }

            Debug.Log($"[ConfirmPanelWithSceneData] Loading scene: {sceneData.SceneName}");

            // 패널 닫기
            OnHide();

            // 씬 전환
            sceneTransitionController.LoadSceneWithLoading(sceneData);
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sceneData == null)
            {
                Debug.LogWarning("[ConfirmPanelWithSceneData] SceneData is not assigned! Please assign a SceneData in Inspector.");
            }
        }
#endif
    }
}
