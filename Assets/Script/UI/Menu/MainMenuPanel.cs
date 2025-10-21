using UnityEngine;
using UnityEngine.UI;
using Game.SceneManagement;
using Game.Controllers;
using Game.Services;
using Game.Core;

namespace Game.UI.Menu
{
    /// <summary>
    /// 메인 메뉴 패널
    /// 게임 시작 버튼과 씬 전환 기능 제공
    /// </summary>
    public class MainMenuPanel : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Scene References")]
        [SerializeField] private SceneData stageSceneData;

        [Header("Dependencies")]
        // ServiceLocator를 통해 가져오므로 SerializeField 제거
        private ISceneTransitionController sceneTransitionController;

        // 🔒 State
        private bool isTransitioning = false;

        #region Initialization

        /// <summary>
        /// 의존성 없는 초기화 (Awake에서 호출됨)
        /// UI 컴포넌트 검증 및 버튼 이벤트 등록
        /// </summary>
        protected override void OnInitializeSelf()
        {
            base.OnInitializeSelf();

            // 버튼 이벤트 등록
            RegisterButtonEvents();

            Debug.Log("[MainMenuPanel] Self-initialized successfully (Awake)");
        }

        /// <summary>
        /// 의존성 있는 초기화 (Start에서 호출됨)
        /// ServiceLocator에서 전역 서비스 가져오기
        /// </summary>
        protected override void OnInitializeWithDependencies()
        {
            base.OnInitializeWithDependencies();

            // ✅ 글로벌 싱글턴 서비스를 ServiceLocator에서 가져오기
            sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();

            if (sceneTransitionController == null)
            {
                Debug.LogError("[MainMenuPanel] ISceneTransitionController not found in ServiceLocator! " +
                              "Ensure SceneTransitionController is placed in Bootstrap scene and RegisterSingleton() was called.");
            }

            // UI 검증
            ValidateReferences();

            Debug.Log("[MainMenuPanel] Dependency initialization complete (Start)");
        }

        /// <summary>
        /// 버튼 이벤트 등록
        /// </summary>
        private void RegisterButtonEvents()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OnStartGameButtonClicked);
                startGameButton.onClick.AddListener(() => { Debug.Log("Button Clicked"); });
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitButtonClicked);
            }
        }

        /// <summary>
        /// 필수 참조 검증
        /// </summary>
        private void ValidateReferences()
        {
            if (startGameButton == null)
                Debug.LogWarning("[MainMenuPanel] Start Game Button not assigned!");

            if (stageSceneData == null)
                Debug.LogWarning("[MainMenuPanel] Stage Scene Data not assigned!");

            // ✅ ServiceLocator를 통해 검증
            if (sceneTransitionController == null)
                Debug.LogError("[MainMenuPanel] ISceneTransitionController not found in ServiceLocator!");
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// 게임 시작 버튼 클릭 핸들러
        /// </summary>
        private void OnStartGameButtonClicked()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[MainMenuPanel] Scene transition already in progress");
                return;
            }

            if (stageSceneData == null)
            {
                Debug.LogError("[MainMenuPanel] Stage Scene Data is not assigned!");
                return;
            }

            // ✅ 안전하게 ServiceLocator에서 다시 가져오기 (null일 경우 재시도)
            if (sceneTransitionController == null)
            {
                sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();
            }

            if (sceneTransitionController == null)
            {
                Debug.LogError("[MainMenuPanel] SceneTransitionController not available in ServiceLocator!");
                return;
            }

            // ✅ 인터페이스를 통해 IsTransitioning 체크
            if (sceneTransitionController.IsTransitioning)
            {
                Debug.LogWarning("[MainMenuPanel] SceneTransitionController is already transitioning!");
                return;
            }

            Debug.Log("[MainMenuPanel] Start Game button clicked - Loading stage scene...");

            // 버튼 비활성화 (중복 클릭 방지)
            SetButtonsInteractable(false);
            isTransitioning = true;

            // 씬 전환 시작
            sceneTransitionController.LoadSceneWithLoading(stageSceneData);
        }

        /// <summary>
        /// 설정 버튼 클릭 핸들러
        /// </summary>
        private void OnSettingsButtonClicked()
        {
            Debug.Log("[MainMenuPanel] Settings button clicked");
            // TODO: 설정 패널 표시
            // settingsPanel.OnShow();
        }

        /// <summary>
        /// 종료 버튼 클릭 핸들러
        /// </summary>
        private void OnQuitButtonClicked()
        {
            Debug.Log("[MainMenuPanel] Quit button clicked");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// 모든 버튼의 상호작용 가능 여부 설정
        /// </summary>
        private void SetButtonsInteractable(bool interactable)
        {
            if (startGameButton != null)
                startGameButton.interactable = interactable;

            if (settingsButton != null)
                settingsButton.interactable = interactable;

            if (quitButton != null)
                quitButton.interactable = interactable;
        }

        #endregion

        #region Panel Lifecycle

        protected override void OnShowPanel()
        {
            base.OnShowPanel();

            // 패널이 표시될 때 버튼 활성화
            SetButtonsInteractable(true);
            isTransitioning = false;

            Debug.Log("[MainMenuPanel] Panel shown");
        }

        protected override void OnHidePanel()
        {
            base.OnHidePanel();

            Debug.Log("[MainMenuPanel] Panel hidden");
        }

        #endregion

        #region Cleanup

        protected override void OnCleanup()
        {
            base.OnCleanup();

            // 버튼 이벤트 해제
            if (startGameButton != null)
                startGameButton.onClick.RemoveAllListeners();

            if (settingsButton != null)
                settingsButton.onClick.RemoveAllListeners();

            if (quitButton != null)
                quitButton.onClick.RemoveAllListeners();

            Debug.Log("[MainMenuPanel] Cleaned up");
        }

        #endregion

        #region Runtime UI Creation (Fallback)

        /// <summary>
        /// UI 요소가 없을 때 런타임에 자동 생성 (Fallback)
        /// Inspector에서 할당하는 것을 권장
        /// </summary>
        private void CreateUIElements()
        {
            // Canvas 확인
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[MainMenuPanel] Canvas not found in parent!");
                return;
            }

            // Start Button 생성
            if (startGameButton == null)
            {
                startGameButton = CreateButton("StartGameButton", "게임 시작", new Vector2(0, 50));
            }

            // Settings Button 생성
            if (settingsButton == null)
            {
                settingsButton = CreateButton("SettingsButton", "설정", new Vector2(0, -20));
            }

            // Quit Button 생성
            if (quitButton == null)
            {
                quitButton = CreateButton("QuitButton", "종료", new Vector2(0, -90));
            }

            // 버튼 이벤트 등록
            RegisterButtonEvents();
        }

        /// <summary>
        /// 버튼 UI 생성 헬퍼
        /// </summary>
        private Button CreateButton(string name, string text, Vector2 position)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(transform);

            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.8f, 0.8f);

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 50);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = position;

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);

            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 18;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Debug.Log($"[MainMenuPanel] Button created: {name}");

            return button;
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Inspector에서 UI 요소 자동 생성 버튼
        /// </summary>
        [ContextMenu("Create UI Elements")]
        private void CreateUIElementsInEditor()
        {
            CreateUIElements();
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif
    }
}
