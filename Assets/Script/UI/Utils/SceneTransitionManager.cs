using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 관리자 (기존 SceneLoader 개선)
/// 로딩 화면, 사운드, 애니메이션을 포함한 포괄적인 씬 전환 시스템
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private bool useLoadingScreen = true;
    [SerializeField] private bool playTransitionSound = true;
    [SerializeField] private bool useAsyncLoading = true;
    
    [Header("로딩 UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private TMPro.TextMeshProUGUI loadingText;
    [SerializeField] private TMPro.TextMeshProUGUI tipText;
    
    [Header("페이드 애니메이션")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;
    
    [Header("사운드 설정")]
    [SerializeField] private string sceneLoadSound = "SceneTransition";
    [SerializeField] private string loadingLoopSound = "LoadingLoop";
    
    [Header("로딩 팁")]
    [SerializeField] private string[] loadingTips = {
        "게임을 즐기는 중입니다...",
        "새로운 모험이 기다리고 있습니다!",
        "잠시만 기다려주세요.",
        "곧 게임이 시작됩니다."
    };
    
    // Singleton 패턴
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SceneTransitionManager>();
                
                if (instance == null)
                {
                    var managerGO = new GameObject("SceneTransitionManager");
                    instance = managerGO.AddComponent<SceneTransitionManager>();
                    DontDestroyOnLoad(managerGO);
                    Debug.Log("SceneTransitionManager 자동 생성됨");
                }
            }
            return instance;
        }
    }
    
    // 오디오 서비스 참조
    private IEffectAudioService effectAudioService;
    private IBGMAudioService bgmAudioService;
    
    // 현재 로딩 상태
    private bool isLoading = false;
    private string currentLoadingScene = "";
    
    // 이벤트
    public static event Action<string> OnSceneLoadStarted;
    public static event Action<string, float> OnSceneLoadProgress;
    public static event Action<string> OnSceneLoadCompleted;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Singleton 패턴 구현
        if (instance != null && instance != this)
        {
            Debug.LogWarning("SceneTransitionManager 중복 인스턴스 제거");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 오디오 서비스 초기화
        InitializeAudioServices();
        
        // 초기 UI 설정
        InitializeUI();
    }
    
    private void Start()
    {
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// 오디오 서비스 초기화
    /// </summary>
    private void InitializeAudioServices()
    {
        try
        {
            var container = AudioServiceContainer.Instance;
            effectAudioService = container.GetService<IEffectAudioService>();
            bgmAudioService = container.GetService<IBGMAudioService>();
            
            if (effectAudioService == null)
                Debug.LogWarning("SceneTransitionManager: EffectAudioService를 찾을 수 없습니다.");
            
            if (bgmAudioService == null)
                Debug.LogWarning("SceneTransitionManager: BGMAudioService를 찾을 수 없습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SceneTransitionManager: 오디오 서비스 초기화 실패 - {ex.Message}");
        }
    }
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // 로딩 패널 초기 비활성화
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        // 페이드 이미지 초기화
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.raycastTarget = false;
        }
    }
    
    #endregion
    
    #region Public Scene Loading Methods
    
    /// <summary>
    /// 씬 로드 (기본 SceneLoader 기능)
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름</param>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning($"이미 씬 로딩 중입니다: {currentLoadingScene}");
            return;
        }
        
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("씬 이름이 비어있습니다.");
            return;
        }
        
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }
    
    /// <summary>
    /// 씬 로드 (추가 옵션 포함)
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름</param>
    /// <param name="showLoading">로딩 화면 표시 여부</param>
    /// <param name="playSound">전환 사운드 재생 여부</param>
    public void LoadScene(string sceneName, bool showLoading, bool playSound = true)
    {
        var originalShowLoading = useLoadingScreen;
        var originalPlaySound = playTransitionSound;
        
        useLoadingScreen = showLoading;
        playTransitionSound = playSound;
        
        LoadScene(sceneName);
        
        // 원래 설정 복원
        useLoadingScreen = originalShowLoading;
        playTransitionSound = originalPlaySound;
    }
    
    /// <summary>
    /// 씬 로드 (콜백 포함)
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름</param>
    /// <param name="onLoadComplete">로드 완료 콜백</param>
    public void LoadScene(string sceneName, Action<string> onLoadComplete)
    {
        StartCoroutine(LoadSceneWithCallback(sceneName, onLoadComplete));
    }
    
    /// <summary>
    /// 현재 씬 재시작
    /// </summary>
    public void RestartCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// 메인 메뉴로 이동
    /// </summary>
    public void LoadMainMenu()
    {
        LoadScene("MainMenu");
    }
    
    /// <summary>
    /// 게임 종료
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("게임 종료");
        
        // 종료 사운드 재생
        if (playTransitionSound && effectAudioService != null)
        {
            effectAudioService.PlayEffect("GameExit");
        }
        
        // 에디터에서는 플레이 모드 종료, 빌드에서는 애플리케이션 종료
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    #endregion
    
    #region Scene Loading Coroutines
    
    /// <summary>
    /// 씬 로딩 코루틴
    /// </summary>
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isLoading = true;
        currentLoadingScene = sceneName;
        
        Debug.Log($"씬 로딩 시작: {sceneName}");
        OnSceneLoadStarted?.Invoke(sceneName);
        
        // 전환 사운드 재생
        if (playTransitionSound && effectAudioService != null)
        {
            effectAudioService.PlayEffect(sceneLoadSound);
        }
        
        // 페이드 인
        if (fadeImage != null)
        {
            yield return StartCoroutine(FadeIn());
        }
        
        // 로딩 화면 표시
        if (useLoadingScreen)
        {
            ShowLoadingScreen();
            
            // 로딩 사운드 시작
            if (playTransitionSound && bgmAudioService != null && !string.IsNullOrEmpty(loadingLoopSound))
            {
                bgmAudioService.FadeInBGM(loadingLoopSound, 1.0f);
            }
        }
        
        // 비동기 씬 로딩
        if (useAsyncLoading)
        {
            yield return StartCoroutine(LoadSceneAsync(sceneName));
        }
        else
        {
            // 동기 로딩
            SceneManager.LoadScene(sceneName);
        }
        
        // 로딩 완료
        isLoading = false;
        currentLoadingScene = "";
        
        Debug.Log($"씬 로딩 완료: {sceneName}");
        OnSceneLoadCompleted?.Invoke(sceneName);
    }
    
    /// <summary>
    /// 비동기 씬 로딩
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        
        float progress = 0f;
        
        while (!operation.isDone)
        {
            // 진행률 계산 (0.9까지는 로딩, 0.9~1.0은 활성화)
            progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // UI 업데이트
            UpdateLoadingProgress(progress);
            
            // 이벤트 알림
            OnSceneLoadProgress?.Invoke(sceneName, progress);
            
            // 로딩이 거의 완료되면 최소 로딩 시간 대기
            if (operation.progress >= 0.9f)
            {
                // 최소 로딩 시간 확보 (사용자 경험 향상)
                yield return new WaitForSeconds(0.5f);
                
                // 진행률 100% 표시
                UpdateLoadingProgress(1.0f);
                OnSceneLoadProgress?.Invoke(sceneName, 1.0f);
                
                yield return new WaitForSeconds(0.2f);
                
                // 씬 활성화
                operation.allowSceneActivation = true;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 콜백 포함 씬 로딩
    /// </summary>
    private IEnumerator LoadSceneWithCallback(string sceneName, Action<string> onLoadComplete)
    {
        yield return StartCoroutine(LoadSceneCoroutine(sceneName));
        onLoadComplete?.Invoke(sceneName);
    }
    
    #endregion
    
    #region Loading Screen Management
    
    /// <summary>
    /// 로딩 화면 표시
    /// </summary>
    private void ShowLoadingScreen()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        // 로딩 팁 설정
        if (tipText != null && loadingTips.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, loadingTips.Length);
            tipText.text = loadingTips[randomIndex];
        }
        
        // 초기 진행률 설정
        UpdateLoadingProgress(0f);
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
        
        // 로딩 사운드 정지
        if (bgmAudioService != null)
        {
            bgmAudioService.FadeOutBGM(0.5f);
        }
    }
    
    /// <summary>
    /// 로딩 진행률 업데이트
    /// </summary>
    private void UpdateLoadingProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        
        // 진행률 바 업데이트
        if (loadingProgressBar != null)
        {
            loadingProgressBar.value = progress;
        }
        
        // 진행률 텍스트 업데이트
        if (loadingText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100);
            loadingText.text = $"로딩 중... {percentage}%";
        }
    }
    
    #endregion
    
    #region Fade Animation
    
    /// <summary>
    /// 페이드 인 (화면을 어둡게)
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;
        
        fadeImage.raycastTarget = true;
        
        float elapsedTime = 0f;
        Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        Color endColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;
            fadeImage.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }
        
        fadeImage.color = endColor;
    }
    
    /// <summary>
    /// 페이드 아웃 (화면을 밝게)
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;
        
        float elapsedTime = 0f;
        Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        Color endColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;
            fadeImage.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }
        
        fadeImage.color = endColor;
        fadeImage.raycastTarget = false;
    }
    
    #endregion
    
    #region Scene Events
    
    /// <summary>
    /// 씬 로드 완료 이벤트 핸들러
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"씬 로드 완료: {scene.name}");
        
        // 로딩 화면 숨김
        if (useLoadingScreen)
        {
            HideLoadingScreen();
        }
        
        // 페이드 아웃
        if (fadeImage != null)
        {
            StartCoroutine(FadeOut());
        }
        
        // 새 씬의 BGM 재생 (BGMManager 기능 통합)
        if (bgmAudioService != null)
        {
            StartCoroutine(PlayNewSceneBGM(scene.name));
        }
    }
    
    /// <summary>
    /// 새 씬의 BGM 재생
    /// </summary>
    private IEnumerator PlayNewSceneBGM(string sceneName)
    {
        // 기존 BGMManager의 기능을 통합
        yield return new WaitForSeconds(0.1f); // 씬 초기화 대기
        
        if (bgmAudioService.PlayBGM(sceneName))
        {
            Debug.Log($"새 씬 BGM 재생: {sceneName}");
        }
        else
        {
            Debug.Log($"씬 BGM을 찾을 수 없습니다: {sceneName}");
        }
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// 현재 로딩 중인지 확인
    /// </summary>
    public bool IsLoading => isLoading;
    
    /// <summary>
    /// 현재 로딩 중인 씬 이름
    /// </summary>
    public string CurrentLoadingScene => currentLoadingScene;
    
    /// <summary>
    /// 로딩 팁 추가
    /// </summary>
    public void AddLoadingTip(string tip)
    {
        if (string.IsNullOrEmpty(tip)) return;
        
        var tipList = new System.Collections.Generic.List<string>(loadingTips);
        tipList.Add(tip);
        loadingTips = tipList.ToArray();
    }
    
    /// <summary>
    /// 페이드 설정 변경
    /// </summary>
    public void SetFadeSettings(float fadeInTime, float fadeOutTime, Color fadeCol)
    {
        fadeInDuration = fadeInTime;
        fadeOutDuration = fadeOutTime;
        fadeColor = fadeCol;
    }
    
    #endregion
}