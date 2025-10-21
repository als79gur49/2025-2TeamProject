using System;
using UnityEngine;
using Game.Services;
using Game.SceneManagement;
using Game.Core;
using System.Runtime.CompilerServices;

namespace Game.Controllers
{
    /// <summary>
    /// 씬 전환을 관리하는 글로벌 싱글턴 서비스
    ///
    /// Lifecycle: 글로벌 싱글턴 (DontDestroyOnLoad) - 모든 씬 전환 시 유지됨
    /// Dependencies: ISceneLoaderService (must be registered before this controller)
    ///
    /// Bootstrap 씬에 배치하여 게임 전체 생명주기 동안 유지됨
    /// ServiceLocator.RegisterSingleton()으로 자동 생명주기 관리
    /// </summary>
    public class SceneTransitionController : MonoBehaviour, ISceneTransitionController, IGlobalService
    {
        // 🎯 Singleton Pattern
        private static SceneTransitionController instance;
        public static SceneTransitionController Instance => instance;

        [Header("Dependencies")]
        [SerializeField] private GameObject loadingScreenPrefab;
        private LoadingScreenPanel currentLoadingPanel;

        // 🔒 Services
        private ISceneLoaderService sceneLoaderService;
        private IBGMAudioService bgmAudioService;

        // 📡 Events
        public event Action<SceneData> OnSceneTransitionStarted;
        public event Action<SceneData> OnSceneTransitionCompleted;
        public event Action<SceneData, string> OnSceneTransitionFailed;

        // 🔒 State
        private bool isTransitioning = false;

        // 🔓 Public Property (ISceneTransitionController 인터페이스 구현)
        public bool IsTransitioning => isTransitioning;

        #region Unity Lifecycle

        private void Awake()
        {
            // 🎯 Singleton 체크 및 중복 방지
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[SceneTransitionController] Duplicate instance detected. Destroying: {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            instance = this;

            // 🎯 글로벌 싱글턴 서비스: 씬 전환 시 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);

            Debug.Log("[SceneTransitionController] Initialized as Global Singleton Service with DontDestroyOnLoad");

            // LoadingScreenPrefab 검증
            if (loadingScreenPrefab == null)
            {
                Debug.LogError("[SceneTransitionController] LoadingScreenPrefab is not assigned! Please assign a LoadingScreenPanel prefab in the Inspector.");
            }

            // ⚠️ NOTE: ServiceLocator registration is handled by ServiceBootstrap
            // ServiceBootstrap calls RegisterSingleton after instantiating this prefab

            // ✅ FIX: Awake에서 즉시 서비스 주입 (Start 대신)
            // ServiceBootstrap.Awake()에서 이미 SceneLoaderService를 등록했으므로 가능
            InitializeServices();
        }

        private void Start()
        {
            // InitializeServices() moved to Awake() for early initialization
        }

        #endregion

        #region Initialization

        /// <summary>
        /// ServiceLocator에서 필요한 서비스 가져오기
        /// </summary>
        private void InitializeServices()
        {
            sceneLoaderService = ServiceLocator.Get<ISceneLoaderService>();
            // sceneLoaderService 자동 검색 (할당되지 않은 경우)
            if (sceneLoaderService == null)
            {
                sceneLoaderService = FindObjectOfType<SceneLoaderService>();
            }

            if (sceneLoaderService == null)
            {
                Debug.LogError("[SceneTransitionController] ISceneLoaderService not found in ServiceLocator!");
                return;
            }

            // BGMAudioService 주입
            bgmAudioService = ServiceLocator.Get<IBGMAudioService>();
            if (bgmAudioService == null)
            {
                Debug.LogWarning("[SceneTransitionController] IBGMAudioService not found in ServiceLocator! BGM transitions will be disabled.");
            }

            // 서비스 이벤트 구독
            SubscribeToServiceEvents();

            Debug.Log("[SceneTransitionController] Initialized successfully");
        }

        /// <summary>
        /// SceneLoaderService 이벤트 구독
        /// </summary>
        private void SubscribeToServiceEvents()
        {
            if (sceneLoaderService != null)
            {
                sceneLoaderService.OnSceneLoadStarted += HandleSceneLoadStarted;
                sceneLoaderService.OnSceneLoadCompleted += HandleSceneLoadCompleted;
                sceneLoaderService.OnSceneLoadFailed += HandleSceneLoadFailed;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 로딩 화면과 함께 씬을 비동기로 로드합니다
        /// </summary>
        /// <param name="sceneData">로드할 씬 데이터</param>
        public void LoadSceneWithLoading(SceneData sceneData)
        {
            LoadSceneWithLoading(sceneData, null);
        }

        /// <summary>
        /// 커스텀 로딩 화면과 함께 씬을 비동기로 로드합니다 (확장성 지원)
        /// </summary>
        /// <param name="sceneData">로드할 씬 데이터</param>
        /// <param name="customLoadingPrefab">커스텀 로딩 화면 Prefab (null이면 기본 Prefab 사용)</param>
        public void LoadSceneWithLoading(SceneData sceneData, GameObject customLoadingPrefab)
        {
            if (sceneData == null)
            {
                Debug.LogError("[SceneTransitionController] SceneData is null!");
                return;
            }

            if (isTransitioning)
            {
                Debug.LogWarning("[SceneTransitionController] Scene transition already in progress!");
                return;
            }

            if (sceneLoaderService == null)
            {
                Debug.LogError("[SceneTransitionController] SceneLoaderService not initialized!");
                return;
            }

            // 씬 로딩 전 검증
            if (!sceneLoaderService.ValidateSceneBeforeLoad(sceneData, out string errorMessage))
            {
                Debug.LogError($"[SceneTransitionController] Scene validation failed: {errorMessage}");
                OnSceneTransitionFailed?.Invoke(sceneData, errorMessage);
                return;
            }

            isTransitioning = true;

            // 로딩 화면 표시 (커스텀 Prefab 사용 또는 기본 Prefab 사용)
            ShowLoadingScreen(sceneData, customLoadingPrefab);

            // 🎯 최소 로딩 표시 시간 및 FadeOut 시간 가져오기
            float minimumDisplayTime = currentLoadingPanel != null ? currentLoadingPanel.MinimumDisplayTime : 0f;
            float fadeOutDuration = currentLoadingPanel != null ? currentLoadingPanel.FadeOutDuration : 0f;

            // 🎯 비동기 씬 로딩 시작 (FadeOut 시간 전달)
            sceneLoaderService.LoadSceneAsync(sceneData, OnLoadingProgressUpdated, minimumDisplayTime, fadeOutDuration);

            OnSceneTransitionStarted?.Invoke(sceneData);
            Debug.Log($"[SceneTransitionController] Starting scene transition to: {sceneData.SceneName} (MinDisplay: {minimumDisplayTime}s, FadeOut: {fadeOutDuration}s)");
        }

        /// <summary>
        /// 로딩 화면 없이 씬을 직접 로드합니다 (빠른 전환용)
        /// </summary>
        /// <param name="sceneData">로드할 씬 데이터</param>
        public void LoadSceneImmediate(SceneData sceneData)
        {
            if (sceneData == null)
            {
                Debug.LogError("[SceneTransitionController] SceneData is null!");
                return;
            }

            if (sceneLoaderService == null)
            {
                Debug.LogError("[SceneTransitionController] SceneLoaderService not initialized!");
                return;
            }

            sceneLoaderService.LoadScene(sceneData);
        }

        #endregion

        #region Loading Screen Management

        /// <summary>
        /// 로딩 화면을 표시합니다
        /// </summary>
        /// <param name="sceneData">씬 데이터</param>
        /// <param name="customPrefab">커스텀 로딩 Prefab (null이면 기본 Prefab 사용)</param>
        private void ShowLoadingScreen(SceneData sceneData, GameObject customPrefab = null)
        {
            // 사용할 Prefab 결정 (커스텀 > 기본)
            GameObject prefabToUse = customPrefab != null ? customPrefab : loadingScreenPrefab;

            if (prefabToUse == null)
            {
                Debug.LogError("[SceneTransitionController] LoadingScreenPrefab not assigned!");
                return;
            }

            // 🎯 Prefab 인스턴스화
            GameObject loadingPanelInstance = Instantiate(prefabToUse);
            currentLoadingPanel = loadingPanelInstance.GetComponent<LoadingScreenPanel>();

            if (currentLoadingPanel == null)
            {
                Debug.LogError("[SceneTransitionController] LoadingScreenPanel component not found on instantiated prefab!");
                Destroy(loadingPanelInstance);
                return;
            }

            // 로딩 배경 설정 (SceneData에 커스텀 배경이 있는 경우)
            if (sceneData.LoadingBackground != null)
            {
                currentLoadingPanel.SetLoadingBackground(sceneData.LoadingBackground);
            }

            // 로딩 팁 설정
            string loadingTip = sceneData.GetRandomLoadingTip();
            currentLoadingPanel.SetLoadingTip(loadingTip);

            // 로딩 화면 표시
            currentLoadingPanel.OnShow();
            currentLoadingPanel.UpdateProgress(0f);

            Debug.Log("[SceneTransitionController] LoadingScreenPanel instantiated and shown");
        }

        /// <summary>
        /// 로딩 화면을 숨깁니다
        /// </summary>
        private void HideLoadingScreen()
        {
            if (currentLoadingPanel == null) return;

            currentLoadingPanel.OnHide();
            // OnHide()의 FadeOut 코루틴이 완료되면 LoadingScreenPanel이 자동으로 Destroy됨
        }

        /// <summary>
        /// 로딩 진행률 업데이트 콜백
        /// </summary>
        private void OnLoadingProgressUpdated(float progress)
        {
            if (currentLoadingPanel != null)
            {
                currentLoadingPanel.UpdateProgress(progress);
            }

            Debug.Log($"[SceneTransitionController] Loading progress: {progress:P0}");
        }

        #endregion

        #region Service Event Handlers

        /// <summary>
        /// 씬 전환 시 BGM 크로스페이드 처리
        /// SceneData에 bgMusic이 설정되어 있으면 자동으로 크로스페이드 실행
        /// AudioData의 FadeOutTime과 FadeInTime을 사용하여 동적으로 크로스페이드 시간 계산
        /// </summary>
        private void HandleBGMTransition(SceneData sceneData)
        {
            // SceneData에 bgMusic이 없으면 스킵
            if (sceneData.BgMusic == null)
            {
                Debug.Log($"[SceneTransitionController] No BGM configured for scene: {sceneData.SceneName}");
                return;
            }

            // BGMAudioService가 없으면 스킵
            if (bgmAudioService == null)
            {
                Debug.LogWarning("[SceneTransitionController] BGMAudioService not available for BGM transition");
                return;
            }

            // ScriptableObject를 AudioData로 캐스팅
            AudioData newBGM = sceneData.BgMusic as AudioData;
            if (newBGM == null)
            {
                Debug.LogError($"[SceneTransitionController] BgMusic is not an AudioData: {sceneData.BgMusic.GetType()}");
                return;
            }

            // CrossFade 시간 계산: AudioData의 FadeOut/FadeIn 시간 사용
            float crossFadeTime;
            AudioData currentBGM = bgmAudioService.CurrentBGM;

            if (currentBGM != null)
            {
                // 현재 BGM이 있는 경우: 현재 BGM의 FadeOut 시간 + 새 BGM의 FadeIn 시간
                crossFadeTime = currentBGM.FadeOutTime + newBGM.FadeInTime;
                Debug.Log($"[SceneTransitionController] Calculating crossfade: Current BGM FadeOut({currentBGM.FadeOutTime}s) + New BGM FadeIn({newBGM.FadeInTime}s) = {crossFadeTime}s");
            }
            else
            {
                // 첫 BGM이거나 현재 재생 중인 BGM이 없는 경우: 새 BGM의 FadeIn 시간만 사용
                // CrossFade는 halfTime으로 나누므로 2배 적용
                crossFadeTime = newBGM.FadeInTime * 2f;
                Debug.Log($"[SceneTransitionController] No current BGM, using new BGM FadeIn({newBGM.FadeInTime}s) * 2 = {crossFadeTime}s");
            }

            // 최소값 보장 (0초 방지)
            if (crossFadeTime <= 0f)
            {
                crossFadeTime = 1.0f;
                Debug.LogWarning($"[SceneTransitionController] CrossFade time is 0 or negative, using default 1.0s");
            }

            // CrossFadeBGM 실행 (현재 BGM FadeOut → 새 BGM FadeIn)
            Debug.Log($"[SceneTransitionController] Starting BGM crossfade to: {newBGM.name} ({crossFadeTime}s)");
            bgmAudioService.CrossFadeBGM(newBGM, crossFadeTime);
        }

        /// <summary>
        /// 씬 로딩 시작 이벤트 핸들러
        /// </summary>
        private void HandleSceneLoadStarted(SceneData sceneData)
        {
            Debug.Log($"[SceneTransitionController] Scene load started: {sceneData.SceneName}");

            // BGM 크로스페이드 처리
            HandleBGMTransition(sceneData);
        }

        /// <summary>
        /// 씬 로딩 완료 이벤트 핸들러
        /// </summary>
        private void HandleSceneLoadCompleted(SceneData sceneData)
        {
            Debug.Log($"[SceneTransitionController] Scene load completed: {sceneData.SceneName}");

            // 로딩 화면 숨기기
            HideLoadingScreen();

            isTransitioning = false;
            OnSceneTransitionCompleted?.Invoke(sceneData);
        }

        /// <summary>
        /// 씬 로딩 실패 이벤트 핸들러
        /// </summary>
        private void HandleSceneLoadFailed(SceneData sceneData, string errorMessage)
        {
            Debug.LogError($"[SceneTransitionController] Scene load failed: {sceneData.SceneName} - {errorMessage}");

            // 로딩 화면 숨기기
            HideLoadingScreen();

            isTransitioning = false;
            OnSceneTransitionFailed?.Invoke(sceneData, errorMessage);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // 🎯 Singleton 인스턴스 정리
            if (instance == this)
            {
                instance = null;
                Debug.Log("[SceneTransitionController] Singleton instance cleared");
            }

            // 현재 로딩 패널 정리 (아직 파괴되지 않았다면)
            if (currentLoadingPanel != null)
            {
                Destroy(currentLoadingPanel.gameObject);
                currentLoadingPanel = null;
                Debug.Log("[SceneTransitionController] Current loading panel cleaned up");
            }

            // ✅ ServiceLocator Unregister는 ServiceCleanup 컴포넌트가 자동으로 처리
            // RegisterSingleton()이 자동으로 부착한 ServiceCleanup이 OnDestroy()에서 Unregister 호출
            Debug.Log("[SceneTransitionController] ServiceCleanup will auto-unregister from ServiceLocator");

            // 이벤트 구독 해제
            if (sceneLoaderService != null)
            {
                sceneLoaderService.OnSceneLoadStarted -= HandleSceneLoadStarted;
                sceneLoaderService.OnSceneLoadCompleted -= HandleSceneLoadCompleted;
                sceneLoaderService.OnSceneLoadFailed -= HandleSceneLoadFailed;
            }
        }

        #endregion

        #region IGlobalService Implementation

        /// <summary>
        /// Validate controller state for debugging.
        /// </summary>
        public bool IsValid()
        {
            bool isValid = instance == this
                && gameObject != null
                && loadingScreenPrefab != null
                && sceneLoaderService != null;

            if (!isValid)
            {
                Debug.LogError($"[SceneTransitionController] Validation failed - " +
                    $"Instance: {instance == this}, " +
                    $"GameObject: {gameObject != null}, " +
                    $"LoadingPrefab: {loadingScreenPrefab != null}, " +
                    $"SceneLoader: {sceneLoaderService != null}");
            }

            return isValid;
        }

        #endregion

        #region Static Utility (Optional)

        /// <summary>
        /// 씬에서 SceneTransitionController 인스턴스를 찾습니다
        /// </summary>
        public static SceneTransitionController FindInScene()
        {
            return FindObjectOfType<SceneTransitionController>();
        }

        #endregion
    }
}
