using Game.Controllers;
using Game.Core;
using Game.SceneManagement;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Services
{
    /// <summary>
    /// SceneData ScriptableObject 기반 씬 로딩 서비스
    ///
    /// Lifecycle: 글로벌 싱글턴 (DontDestroyOnLoad) - 모든 씬 전환 시 유지됨
    /// Dependencies: None (foundational service)
    ///
    /// 기능:
    /// - 동기/비동기 씬 로딩
    /// - 로딩 진행률 추적 및 이벤트
    /// - 씬 검증 (Build Settings, 파일 존재 여부)
    /// - 로딩 화면 통합 지원
    ///
    /// 사용 예시:
    /// sceneLoaderService.LoadSceneAsync(titleSceneData, (progress) => {
    ///     loadingBar.fillAmount = progress;
    /// });
    /// </summary>
    public class SceneLoaderService : MonoBehaviour, ISceneLoaderService, IGlobalService
    {
        // 🔒 Singleton Instance
        private static SceneLoaderService instance;

        // 🔒 State
        private bool _isLoading = false;
        private float _loadingProgress = 0f;
        private SceneData _currentLoadingScene = null;

        // 📡 Events
        public event Action<SceneData> OnSceneLoadStarted;
        public event Action<SceneData, float> OnSceneLoadProgress;
        public event Action<SceneData> OnSceneLoadCompleted;
        public event Action<SceneData, string> OnSceneLoadFailed;

        // 🔍 Properties
        public bool IsLoading => _isLoading;
        public float LoadingProgress => _loadingProgress;

        #region Synchronous Loading

        /// <summary>
        /// 씬을 동기적으로 로드합니다 (즉시 로딩, 화면 멈춤)
        /// 작은 씬이나 빠른 전환이 필요할 때 사용
        /// </summary>
        public void LoadScene(SceneData sceneData)
        {
            if (!ValidateSceneBeforeLoad(sceneData, out string errorMessage))
            {
                Debug.LogError($"[SceneLoaderService] Cannot load scene: {errorMessage}");
                OnSceneLoadFailed?.Invoke(sceneData, errorMessage);
                return;
            }

            try
            {
                _isLoading = true;
                OnSceneLoadStarted?.Invoke(sceneData);

                Debug.Log($"[SceneLoaderService] Loading scene synchronously: {sceneData.SceneName}");

                SceneManager.LoadScene(sceneData.SceneName);

                _isLoading = false;
                OnSceneLoadCompleted?.Invoke(sceneData);

                Debug.Log($"[SceneLoaderService] Scene loaded successfully: {sceneData.SceneName}");
            }
            catch (Exception ex)
            {
                _isLoading = false;
                string error = $"Exception during scene load: {ex.Message}";
                Debug.LogError($"[SceneLoaderService] {error}");
                OnSceneLoadFailed?.Invoke(sceneData, error);
            }
        }

        #endregion

        #region Asynchronous Loading

        /// <summary>
        /// 씬을 비동기적으로 로드합니다 (진행률 표시 가능)
        /// 로딩 화면 표시가 필요한 큰 씬에 사용
        /// </summary>
        /// <param name="sceneData">로드할 씬 데이터</param>
        /// <param name="onProgress">진행률 업데이트 콜백</param>
        /// <param name="minimumLoadingDuration">최소 로딩 표시 시간 (초). 0이면 무시</param>
        /// <param name="fadeOutDuration">FadeOut 애니메이션 지속 시간 (초). 씬 전환 후 FadeOut에 사용</param>
        public void LoadSceneAsync(SceneData sceneData, Action<float> onProgress = null, float minimumLoadingDuration = 0f, float fadeOutDuration = 0f)
        {
            if (!ValidateSceneBeforeLoad(sceneData, out string errorMessage))
            {
                Debug.LogError($"[SceneLoaderService] Cannot load scene asynchronously: {errorMessage}");
                OnSceneLoadFailed?.Invoke(sceneData, errorMessage);
                return;
            }

            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoaderService] Already loading a scene. Current: {_currentLoadingScene?.SceneName}");
                return;
            }

            StartCoroutine(LoadSceneAsyncCoroutine(sceneData, onProgress, minimumLoadingDuration, fadeOutDuration));
        }

        /// <summary>
        /// 비동기 씬 로딩 코루틴
        /// </summary>
        private IEnumerator LoadSceneAsyncCoroutine(SceneData sceneData, Action<float> onProgress, float minimumLoadingDuration, float fadeOutDuration)
        {
            _isLoading = true;
            _loadingProgress = 0f;
            _currentLoadingScene = sceneData;
            float loadStartTime = Time.time;

            OnSceneLoadStarted?.Invoke(sceneData);
            Debug.Log($"[SceneLoaderService] Starting async load: {sceneData.SceneName}");

            // Unity의 비동기 씬 로딩 시작
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneData.SceneName);

            if (asyncLoad == null)
            {
                _isLoading = false;
                _currentLoadingScene = null;
                string error = $"Failed to start async load for scene: {sceneData.SceneName}";
                Debug.LogError($"[SceneLoaderService] {error}");
                OnSceneLoadFailed?.Invoke(sceneData, error);
                yield break;
            }

            // 씬 활성화 자동 방지 (수동으로 활성화 시점 제어)
            asyncLoad.allowSceneActivation = false;

            // 🎯 FadeOut을 위한 시간 계산: 씬 전환 전 대기 시간 = minimumLoadingDuration - fadeOutDuration
            float waitTimeBeforeSceneSwitch = Mathf.Max(0f, minimumLoadingDuration - fadeOutDuration);
            Debug.Log($"[SceneLoaderService] Wait time before scene switch: {waitTimeBeforeSceneSwitch:F2}s (Total: {minimumLoadingDuration:F2}s - FadeOut: {fadeOutDuration:F2}s)");

            // 로딩 진행률 추적
            bool sceneReady = false;
            while (!asyncLoad.isDone)
            {
                // Unity의 진행률은 0.0 ~ 0.9 (0.9에서 멈춤, 활성화 대기)
                _loadingProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

                // 씬 로딩이 준비됨 (0.9 도달)
                if (asyncLoad.progress >= 0.9f)
                {
                    sceneReady = true;
                    _loadingProgress = 1.0f;

                    // 🎯 수정된 대기 시간 체크: FadeOut 시간을 제외한 대기 시간
                    float elapsedTime = Time.time - loadStartTime;
                    if (elapsedTime >= waitTimeBeforeSceneSwitch)
                    {
                        // 두 조건 모두 충족: 씬 준비 완료 + 대기 시간 경과
                        Debug.Log($"[SceneLoaderService] Scene ready and wait time met. Triggering FadeOut and activating scene... (Elapsed: {elapsedTime:F2}s, Required: {waitTimeBeforeSceneSwitch:F2}s)");

                        // 🎯 중요: 씬 전환 전에 이벤트 발생 (FadeOut 시작)
                        // SceneLoaderService가 파괴되기 전에 이벤트를 발생시켜야 함
                        OnSceneLoadCompleted?.Invoke(sceneData);

                        // ✅ 씬 전환 직전에 _isLoading 리셋 (타이밍 갭 제거)
                        // 새 씬에서 즉시 다시 씬 전환을 시도해도 안전하도록 보장
                        _isLoading = false;
                        _currentLoadingScene = null;
                        Debug.Log($"[SceneLoaderService] Reset _isLoading before scene activation");

                        // 🎯 FadeOut이 시작된 후 씬 활성화
                        // LoadingScreenPanel은 DontDestroyOnLoad이므로 새 씬에서도 FadeOut 계속 실행됨
                        asyncLoad.allowSceneActivation = true;
                    }
                    else
                    {
                        // 대기 시간까지 대기 중
                        float remainingTime = waitTimeBeforeSceneSwitch - elapsedTime;
                        Debug.Log($"[SceneLoaderService] Scene ready but waiting ({remainingTime:F2}s remaining before scene switch)");
                    }
                }

                // 진행률 이벤트 발생
                OnSceneLoadProgress?.Invoke(sceneData, _loadingProgress);
                onProgress?.Invoke(_loadingProgress);

                yield return null;
            }

            // 로딩 완료
            _loadingProgress = 1.0f;
            _isLoading = false;
            _currentLoadingScene = null;

            OnSceneLoadProgress?.Invoke(sceneData, 1.0f);
            onProgress?.Invoke(1.0f);

            Debug.Log($"[SceneLoaderService] Scene loaded successfully: {sceneData.SceneName}");
        }

        #endregion

        #region Validation

        /// <summary>
        /// 씬 로딩이 가능한지 간단히 확인
        /// </summary>
        public bool CanLoadScene(SceneData sceneData)
        {
            return ValidateSceneBeforeLoad(sceneData, out _);
        }

        /// <summary>
        /// 씬 로딩 전 종합 검증 (에러 메시지 포함)
        /// </summary>
        public bool ValidateSceneBeforeLoad(SceneData sceneData, out string errorMessage)
        {
            errorMessage = string.Empty;

            // SceneData null 체크
            if (sceneData == null)
            {
                errorMessage = "SceneData is null";
                return false;
            }

            // 씬 이름 체크
            if (string.IsNullOrWhiteSpace(sceneData.SceneName))
            {
                errorMessage = $"SceneData '{sceneData.name}' has empty scene name";
                return false;
            }

            // Build Settings 체크
            if (!sceneData.IsSceneInBuildSettings())
            {
                errorMessage = $"Scene '{sceneData.SceneName}' is not in Build Settings or is disabled";
                return false;
            }

            // 씬 파일 존재 체크 (에디터 전용)
            if (!sceneData.DoesSceneExist())
            {
                errorMessage = $"Scene file '{sceneData.SceneName}' does not exist in project";
                return false;
            }

            return true;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 현재 활성화된 씬의 이름을 반환
        /// </summary>
        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        #endregion

        #region MonoBehaviour Lifecycle

        private void Awake()
        {
            // Singleton enforcement - prevent duplicate instances
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[SceneLoaderService] Duplicate instance detected on {gameObject.name}. Destroying...");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[SceneLoaderService] Initialized as global singleton");
        }

        private void OnDestroy()
        {
            // Clear instance reference if this was the active singleton
            if (instance == this)
            {
                instance = null;
                Debug.Log("[SceneLoaderService] Global singleton destroyed");
            }

            // 이벤트 구독 해제 (메모리 누수 방지)
            OnSceneLoadStarted = null;
            OnSceneLoadProgress = null;
            OnSceneLoadCompleted = null;
            OnSceneLoadFailed = null;
        }

        #endregion

        #region IGlobalService Implementation

        /// <summary>
        /// Validate service state for debugging.
        /// </summary>
        public bool IsValid()
        {
            bool isValid = instance == this && gameObject != null;

            if (!isValid)
            {
                Debug.LogError("[SceneLoaderService] Service is in invalid state");
            }

            return isValid;
        }

        #endregion

        #region Static Utility (Optional)

        /// <summary>
        /// 씬 로딩을 위한 간단한 정적 헬퍼 메서드
        /// ServiceLocator 없이 빠르게 사용 가능
        /// </summary>
        public static void QuickLoadScene(SceneData sceneData)
        {
            if (sceneData == null || string.IsNullOrWhiteSpace(sceneData.SceneName))
            {
                Debug.LogError("[SceneLoaderService] Cannot quick load: Invalid SceneData");
                return;
            }

            try
            {
                SceneManager.LoadScene(sceneData.SceneName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneLoaderService] Quick load failed: {ex.Message}");
            }
        }

        #endregion
    }
}
