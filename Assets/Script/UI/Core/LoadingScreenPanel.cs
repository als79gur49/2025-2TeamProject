using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 씬 로딩 중 표시되는 로딩 화면 패널
/// 진행률 표시, 로딩 팁 표시, 페이드 인/아웃 효과 지원
/// </summary>
public class LoadingScreenPanel : UIPanel
{
    [Header("UI References")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI loadingTipText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float minimumDisplayTime = 1.0f; // 최소 표시 시간 (너무 빠른 전환 방지)

    // 🔍 Properties
    public float MinimumDisplayTime => minimumDisplayTime;
    public float FadeOutDuration => fadeOutDuration;

    [Header("Progress Settings")]
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private string loadingTextFormat = "Loading... {0:0}%";
    [SerializeField] private string defaultLoadingTip = "로딩 중...";

    // 🔒 State
    private float currentProgress = 0f;
    private float displayStartTime = 0f;
    private bool isFading = false;

    #region Initialization

    /// <summary>
    /// 의존성 없는 초기화 (Awake에서 호출됨)
    /// Instantiate 직후에도 작동해야 하는 UI 컴포넌트 검증 및 내부 상태 초기화
    /// </summary>
    protected override void OnInitializeSelf()
    {
        base.OnInitializeSelf();

        // Canvas 컴포넌트 검증 (Prefab에 미리 설정되어 있어야 함)
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[LoadingScreenPanel] Canvas component is missing! This Prefab must have Canvas, CanvasScaler, and GraphicRaycaster components.", this);
        }
        else if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Debug.LogWarning("[LoadingScreenPanel] Canvas renderMode should be ScreenSpaceOverlay for proper overlay rendering.", this);
        }

        // CanvasGroup 자동 생성 (없는 경우)
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // UI 요소 검증
        ValidateUIComponents();

        // 🎯 FIX: 초기 상태만 설정, ResetPanel() 호출 안 함
        // ResetPanel()을 호출하면 SetLoadingTip(defaultLoadingTip)이 실행되어
        // SceneTransitionController에서 설정한 SceneData의 로딩 팁이 기본값으로 덮어씌워짐
        currentProgress = 0f;
        UpdateProgressDisplay(0f);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        Debug.Log("[LoadingScreenPanel] Self-initialized successfully (Awake)");
    }

    /// <summary>
    /// 의존성 있는 초기화 (Start에서 호출됨)
    /// ServiceLocator 등 외부 서비스 접근이 필요한 초기화
    /// LoadingScreenPanel은 외부 의존성이 없으므로 비어있음
    /// </summary>
    protected override void OnInitializeWithDependencies()
    {
        base.OnInitializeWithDependencies();

        Debug.Log("[LoadingScreenPanel] Dependency initialization complete (Start)");
    }

    /// <summary>
    /// UI 컴포넌트가 제대로 할당되었는지 검증
    /// </summary>
    private void ValidateUIComponents()
    {
        if (progressBar == null)
            Debug.LogWarning("[LoadingScreenPanel] ProgressBar not assigned - progress bar will not be displayed");

        if (progressText == null)
            Debug.LogWarning("[LoadingScreenPanel] ProgressText not assigned - percentage text will not be displayed");

        if (loadingTipText == null)
            Debug.LogWarning("[LoadingScreenPanel] LoadingTipText not assigned - loading tips will not be displayed");
    }

    /// <summary>
    /// 패널을 초기 상태로 리셋
    /// </summary>
    private void ResetPanel()
    {
        currentProgress = 0f;
        UpdateProgressDisplay(0f);
        SetLoadingTip(defaultLoadingTip);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    #endregion

    #region Panel Lifecycle

    public override void OnShow()
    {
        if (currentState == UIPanelState.Active) return;

        currentState = UIPanelState.Showing;
        gameObject.SetActive(true);
        displayStartTime = Time.time;

        // 🎯 Prefab으로 인스턴스화되므로 이미 독립적인 Canvas를 가짐
        // Canvas, CanvasScaler, GraphicRaycaster는 Prefab에 미리 설정되어 있어야 함

        // ✅ DontDestroyOnLoad 적용 - 씬 전환 시 FadeOut을 위해 유지
        // OnSceneLoadCompleted 이벤트는 씬 전환 **전**에 발생하지만,
        // FadeOut 애니메이션은 씬 전환 **후**에도 실행되어야 함
        // FadeOut 완료 후 Destroy(gameObject)로 자동 정리됨
        DontDestroyOnLoad(gameObject);
        Debug.Log("[LoadingScreenPanel] Applied DontDestroyOnLoad for FadeOut across scene transition");

        // 페이드 인 애니메이션
        StartCoroutine(FadeIn());

        Debug.Log("[LoadingScreenPanel] Showing loading screen (Prefab instantiated)");
    }

    public override void OnHide()
    {
        if (currentState == UIPanelState.Inactive) return;

        // 씬 전환 타이밍은 SceneLoaderService에서 제어하므로
        // 여기서는 바로 페이드아웃 시작
        StartCoroutine(FadeOut());

        Debug.Log("[LoadingScreenPanel] Hiding loading screen");
    }

    #endregion

    #region Progress Management

    /// <summary>
    /// 로딩 진행률 업데이트
    /// </summary>
    /// <param name="progress">0.0 ~ 1.0 범위의 진행률</param>
    public void UpdateProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        UpdateProgressDisplay(currentProgress);
    }

    /// <summary>
    /// 진행률 UI 업데이트
    /// </summary>
    private void UpdateProgressDisplay(float progress)
    {
        // Progress Bar 업데이트
        if (progressBar != null)
        {
            progressBar.value = progress;
            Debug.Log($"Updated Value is {progress}");
        }

        // Progress Text 업데이트
        if (progressText != null && showPercentage)
        {
            float percentage = progress * 100f;
            progressText.text = string.Format(loadingTextFormat, percentage);
        }
    }

    /// <summary>
    /// 로딩 팁 텍스트 설정
    /// </summary>
    public void SetLoadingTip(string tip)
    {
        if (loadingTipText != null)
        {
            Debug.Log($"|{tip}|  text");
            loadingTipText.text = string.IsNullOrWhiteSpace(tip) ? defaultLoadingTip : tip;
        }
    }

    /// <summary>
    /// 로딩 화면 배경 이미지 설정
    /// </summary>
    public void SetLoadingBackground(Sprite background)
    {
        if (backgroundImage != null && background != null)
        {
            backgroundImage.sprite = background;
            Debug.Log($"[LoadingScreenPanel] Background image set: {background.name}");
        }
    }

    #endregion

    #region Animations

    /// <summary>
    /// 페이드 인 애니메이션
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (canvasGroup == null)
        {
            currentState = UIPanelState.Active;
            RaiseOnPanelShown();
            yield break;
        }

        isFading = true;
        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        isFading = false;
        currentState = UIPanelState.Active;
        RaiseOnPanelShown();
    }

    /// <summary>
    /// 페이드 아웃 애니메이션
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (canvasGroup == null)
        {
            gameObject.SetActive(false);
            currentState = UIPanelState.Inactive;
            RaiseOnPanelHidden();
            Destroy(gameObject); // FadeOut 후 패널 파괴
            yield break;
        }

        currentState = UIPanelState.Hiding;
        isFading = true;
        float elapsedTime = 0f;
        canvasGroup.alpha = 1f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        isFading = false;
        gameObject.SetActive(false);
        currentState = UIPanelState.Inactive;
        RaiseOnPanelHidden();
        ResetPanel();

        // FadeOut 완료 후 DontDestroyOnLoad로 유지된 패널 파괴
        Destroy(gameObject);
        Debug.Log("[LoadingScreenPanel] FadeOut complete - Panel destroyed");
    }


    #endregion

    #region Runtime UI Creation (Fallback)

    /// <summary>
    /// UI 요소가 없을 때 런타임에 자동 생성 (Fallback)
    /// Inspector에서 할당하는 것을 권장
    /// </summary>
    protected override void OnShowPanel()
    {
        base.OnShowPanel();

        // Progress Bar가 없으면 자동 생성
        if (progressBar == null)
        {
            CreateProgressBar();
        }

        // Progress Text가 없으면 자동 생성
        if (progressText == null && showPercentage)
        {
            CreateProgressText();
        }

        // Loading Tip Text가 없으면 자동 생성
        if (loadingTipText == null)
        {
            CreateLoadingTipText();
        }

        // Background Image가 없으면 자동 생성
        if (backgroundImage == null)
        {
            CreateBackgroundImage();
        }
    }

    private void CreateProgressBar()
    {
        GameObject sliderObj = new GameObject("ProgressBar");
        sliderObj.transform.SetParent(transform);

        progressBar = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.3f, 0.3f);
        sliderRect.anchorMax = new Vector2(0.7f, 0.35f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform);
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        progressBar.fillRect = fillObj.GetComponent<RectTransform>();
        progressBar.minValue = 0f;
        progressBar.maxValue = 1f;

        Debug.Log("[LoadingScreenPanel] Progress bar auto-created");
    }

    private void CreateProgressText()
    {
        GameObject textObj = new GameObject("ProgressText");
        textObj.transform.SetParent(transform);

        progressText = textObj.AddComponent<TextMeshProUGUI>();
        progressText.fontSize = 24;
        progressText.alignment = TextAlignmentOptions.Center;
        progressText.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.3f, 0.35f);
        textRect.anchorMax = new Vector2(0.7f, 0.4f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Debug.Log("[LoadingScreenPanel] Progress text auto-created");
    }

    private void CreateLoadingTipText()
    {
        GameObject textObj = new GameObject("LoadingTipText");
        textObj.transform.SetParent(transform);

        loadingTipText = textObj.AddComponent<TextMeshProUGUI>();
        loadingTipText.fontSize = 18;
        loadingTipText.alignment = TextAlignmentOptions.Center;
        loadingTipText.color = new Color(0.8f, 0.8f, 0.8f, 1f);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.2f, 0.15f);
        textRect.anchorMax = new Vector2(0.8f, 0.25f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Debug.Log("[LoadingScreenPanel] Loading tip text auto-created");
    }

    private void CreateBackgroundImage()
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(transform);
        bgObj.transform.SetAsFirstSibling(); // 맨 뒤로

        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.95f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Debug.Log("[LoadingScreenPanel] Background image auto-created");
    }

    #endregion
}
