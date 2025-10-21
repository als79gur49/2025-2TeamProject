using UnityEngine;
using UnityEngine.UI;
using Game.Services;
using Game.SceneManagement;
using Game.Core;

namespace Game.Examples
{
    /// <summary>
    /// SceneData 및 SceneLoaderService 사용 예제
    ///
    /// 이 스크립트는 ScriptableObject 기반 씬 관리 시스템의
    /// 다양한 사용 방법을 보여줍니다.
    ///
    /// 사용법:
    /// 1. 이 스크립트를 빈 GameObject에 추가
    /// 2. Inspector에서 SceneData 에셋들을 할당
    /// 3. (옵션) UI 요소들을 할당하여 로딩 진행률 표시
    /// </summary>
    public class SceneLoaderExample : MonoBehaviour
    {
        [Header("씬 데이터 에셋")]
        [SerializeField]
        [Tooltip("타이틀 씬 데이터")]
        private SceneData titleSceneData;

        [SerializeField]
        [Tooltip("게임플레이 씬 데이터")]
        private SceneData gameplaySceneData;

        [SerializeField]
        [Tooltip("테스트 씬 데이터")]
        private SceneData testSceneData;

        [Header("UI 요소 (옵션)")]
        [SerializeField]
        [Tooltip("로딩 진행률 표시 슬라이더")]
        private Slider loadingProgressBar;

        [SerializeField]
        [Tooltip("로딩 상태 텍스트")]
        private Text loadingStatusText;

        [SerializeField]
        [Tooltip("로딩 팁 텍스트")]
        private Text loadingTipText;

        [SerializeField]
        [Tooltip("로딩 화면 패널")]
        private GameObject loadingPanel;

        // 서비스 참조
        private ISceneLoaderService _sceneLoader;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeSceneLoader();
            SubscribeToEvents();
            ValidateSceneData();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// SceneLoaderService 초기화
        /// </summary>
        private void InitializeSceneLoader()
        {
            // 방법 1: ServiceLocator에서 가져오기 (권장)
            _sceneLoader = ServiceLocator.Get<ISceneLoaderService>();
            
            // ServiceLocator에 등록되지 않은 경우 직접 찾기
            if (_sceneLoader == null)
            {
                _sceneLoader = FindObjectOfType<SceneLoaderService>();

                // 없으면 새로 생성
                if (_sceneLoader == null)
                {
                    var serviceObject = new GameObject("SceneLoaderService");
                    _sceneLoader = serviceObject.AddComponent<SceneLoaderService>();
                    Debug.LogWarning("[SceneLoaderExample] SceneLoaderService를 새로 생성했습니다. GameInitializer에서 등록하는 것을 권장합니다.");
                }
            }

            Debug.Log("[SceneLoaderExample] SceneLoaderService 초기화 완료");
        }

        /// <summary>
        /// 씬 로딩 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_sceneLoader == null) return;

            _sceneLoader.OnSceneLoadStarted += HandleSceneLoadStarted;
            _sceneLoader.OnSceneLoadProgress += HandleSceneLoadProgress;
            _sceneLoader.OnSceneLoadCompleted += HandleSceneLoadCompleted;
            _sceneLoader.OnSceneLoadFailed += HandleSceneLoadFailed;

            Debug.Log("[SceneLoaderExample] 이벤트 구독 완료");
        }

        /// <summary>
        /// 씬 로딩 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_sceneLoader == null) return;

            _sceneLoader.OnSceneLoadStarted -= HandleSceneLoadStarted;
            _sceneLoader.OnSceneLoadProgress -= HandleSceneLoadProgress;
            _sceneLoader.OnSceneLoadCompleted -= HandleSceneLoadCompleted;
            _sceneLoader.OnSceneLoadFailed -= HandleSceneLoadFailed;
        }

        /// <summary>
        /// SceneData 에셋 유효성 검사
        /// </summary>
        private void ValidateSceneData()
        {
            if (titleSceneData != null)
            {
                Debug.Log($"[SceneLoaderExample] Title Scene: {titleSceneData}");
            }

            if (gameplaySceneData != null)
            {
                Debug.Log($"[SceneLoaderExample] Gameplay Scene: {gameplaySceneData}");
            }

            if (testSceneData != null)
            {
                Debug.Log($"[SceneLoaderExample] Test Scene: {testSceneData}");
            }
        }

        #endregion

        #region Public Methods - Scene Loading

        /// <summary>
        /// 타이틀 씬을 동기적으로 로드 (즉시 전환)
        /// </summary>
        public void LoadTitleSceneSync()
        {
            if (titleSceneData == null)
            {
                Debug.LogError("[SceneLoaderExample] Title Scene Data가 설정되지 않았습니다!");
                return;
            }

            Debug.Log("[SceneLoaderExample] 타이틀 씬 동기 로딩 시작...");
            _sceneLoader.LoadScene(titleSceneData);
        }

        /// <summary>
        /// 게임플레이 씬을 비동기적으로 로드 (진행률 표시)
        /// </summary>
        public void LoadGameplaySceneAsync()
        {
            if (gameplaySceneData == null)
            {
                Debug.LogError("[SceneLoaderExample] Gameplay Scene Data가 설정되지 않았습니다!");
                return;
            }

            Debug.Log("[SceneLoaderExample] 게임플레이 씬 비동기 로딩 시작...");

            // 로딩 화면 표시
            ShowLoadingScreen(gameplaySceneData);

            // 비동기 로딩 시작 (진행률 콜백 포함)
            _sceneLoader.LoadSceneAsync(gameplaySceneData, OnLoadingProgress);
        }

        /// <summary>
        /// 테스트 씬을 비동기적으로 로드
        /// </summary>
        public void LoadTestSceneAsync()
        {
            if (testSceneData == null)
            {
                Debug.LogError("[SceneLoaderExample] Test Scene Data가 설정되지 않았습니다!");
                return;
            }

            Debug.Log("[SceneLoaderExample] 테스트 씬 비동기 로딩 시작...");
            ShowLoadingScreen(testSceneData);
            _sceneLoader.LoadSceneAsync(testSceneData, OnLoadingProgress);
        }

        /// <summary>
        /// 씬 로딩 전 유효성 검사 예제
        /// </summary>
        public void ValidateAndLoadScene(SceneData sceneData)
        {
            if (_sceneLoader.ValidateSceneBeforeLoad(sceneData, out string errorMessage))
            {
                Debug.Log($"[SceneLoaderExample] 검증 성공! 씬 로딩 시작: {sceneData.SceneName}");
                _sceneLoader.LoadSceneAsync(sceneData);
            }
            else
            {
                Debug.LogError($"[SceneLoaderExample] 검증 실패: {errorMessage}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 씬 로딩 시작 이벤트 처리
        /// </summary>
        private void HandleSceneLoadStarted(SceneData sceneData)
        {
            Debug.Log($"[SceneLoaderExample] 씬 로딩 시작: {sceneData.SceneName}");

            // 로딩 화면 표시
            ShowLoadingScreen(sceneData);
        }

        /// <summary>
        /// 씬 로딩 진행률 이벤트 처리
        /// </summary>
        private void HandleSceneLoadProgress(SceneData sceneData, float progress)
        {
            Debug.Log($"[SceneLoaderExample] 로딩 진행률: {progress:P0}");

            // UI 업데이트
            UpdateLoadingUI(progress);
        }

        /// <summary>
        /// 씬 로딩 완료 이벤트 처리
        /// </summary>
        private void HandleSceneLoadCompleted(SceneData sceneData)
        {
            Debug.Log($"[SceneLoaderExample] 씬 로딩 완료: {sceneData.SceneName}");

            // 로딩 화면 숨김
            HideLoadingScreen();
        }

        /// <summary>
        /// 씬 로딩 실패 이벤트 처리
        /// </summary>
        private void HandleSceneLoadFailed(SceneData sceneData, string errorMessage)
        {
            Debug.LogError($"[SceneLoaderExample] 씬 로딩 실패: {sceneData.SceneName} - {errorMessage}");

            // 에러 메시지 표시
            if (loadingStatusText != null)
            {
                loadingStatusText.text = $"로딩 실패: {errorMessage}";
            }

            // 로딩 화면 숨김
            HideLoadingScreen();
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// 로딩 화면 표시
        /// </summary>
        private void ShowLoadingScreen(SceneData sceneData)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            // 로딩 팁 표시
            if (loadingTipText != null)
            {
                loadingTipText.text = sceneData.GetRandomLoadingTip();
            }

            // 상태 텍스트 업데이트
            if (loadingStatusText != null)
            {
                loadingStatusText.text = $"{sceneData.SceneName} 로딩 중...";
            }

            // 진행률 초기화
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = 0f;
            }
        }

        /// <summary>
        /// 로딩 화면 숨김
        /// </summary>
        private void HideLoadingScreen()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 로딩 UI 업데이트
        /// </summary>
        private void UpdateLoadingUI(float progress)
        {
            // 진행률 바 업데이트
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = progress;
            }

            // 상태 텍스트 업데이트
            if (loadingStatusText != null)
            {
                loadingStatusText.text = $"로딩 중... {progress * 100:F0}%";
            }
        }

        /// <summary>
        /// 비동기 로딩 진행률 콜백 (추가적인 UI 업데이트용)
        /// </summary>
        private void OnLoadingProgress(float progress)
        {
            // 이벤트 핸들러에서 이미 처리하지만,
            // 추가적인 커스텀 로직이 필요한 경우 여기에 작성
            Debug.Log($"[SceneLoaderExample] 진행률 콜백: {progress:P0}");
        }

        #endregion

        #region Test Methods (디버깅용)

        /// <summary>
        /// 현재 씬 이름 출력
        /// </summary>
        [ContextMenu("Print Current Scene Name")]
        public void PrintCurrentSceneName()
        {
            string currentScene = _sceneLoader.GetCurrentSceneName();
            Debug.Log($"[SceneLoaderExample] 현재 씬: {currentScene}");
        }

        /// <summary>
        /// 로딩 상태 확인
        /// </summary>
        [ContextMenu("Check Loading Status")]
        public void CheckLoadingStatus()
        {
            Debug.Log($"[SceneLoaderExample] 로딩 중: {_sceneLoader.IsLoading}");
            Debug.Log($"[SceneLoaderExample] 진행률: {_sceneLoader.LoadingProgress:P0}");
        }

        /// <summary>
        /// 모든 SceneData 검증
        /// </summary>
        [ContextMenu("Validate All Scene Data")]
        public void ValidateAllSceneData()
        {
            SceneData[] allScenes = { titleSceneData, gameplaySceneData, testSceneData };

            foreach (var sceneData in allScenes)
            {
                if (sceneData == null) continue;

                bool isValid = _sceneLoader.ValidateSceneBeforeLoad(sceneData, out string error);
                Debug.Log($"[SceneLoaderExample] {sceneData.name}: {(isValid ? "✓ 유효" : $"✗ 무효 - {error}")}");
            }
        }

        #endregion
    }
}
