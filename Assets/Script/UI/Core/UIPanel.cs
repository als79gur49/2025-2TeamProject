using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// UIPanel 공용 애니메이션 타입
/// </summary>
public enum UIPanelAnimationType
{
    None,
    Fade,
    ScaleSlideFromTop
}

/// <summary>
/// UI 패널의 추상 베이스 클래스
/// IUIPanel 인터페이스 구현 및 공통 기능 제공
/// </summary>
public abstract class UIPanel : MonoBehaviour, IUIPanel
{
    [Header("UI Panel Settings")]
    [SerializeField] protected UIPanelPriority priority = UIPanelPriority.Normal;
    [SerializeField] protected bool initializeOnAwake = true;
    [SerializeField] protected bool hideOnStart = true;

    [Header("Animation Settings")]
    [SerializeField] protected UIPanelAnimationType showAnimation = UIPanelAnimationType.None;
    [SerializeField] protected UIPanelAnimationType hideAnimation = UIPanelAnimationType.None;
    [SerializeField] protected float showDuration = 0.25f;
    [SerializeField] protected float hideDuration = 0.2f;
    [SerializeField] protected Ease showEase = Ease.OutQuad;
    [SerializeField] protected Ease hideEase = Ease.InQuad;

    [Header("Animation Targets")]
    [SerializeField] protected CanvasGroup animationCanvasGroup;
    [SerializeField] protected RectTransform animationRectTransform;
    [SerializeField] protected float slideOffsetY = 400f;
    [SerializeField] protected float scaleFrom = 0.8f;

    // --- 프로퍼티 ---
    public bool IsActive => gameObject.activeInHierarchy;
    public UIPanelPriority Priority => priority;
    public UIPanelState CurrentState => currentState;

    // --- 상태 관리 ---
    protected UIPanelState currentState = UIPanelState.Inactive;

    // --- 이벤트 ---
    public event Action<IUIPanel> OnPanelShown;
    public event Action<IUIPanel> OnPanelHidden;

    // --- 초기화 상태 ---
    protected bool isInitialized = false;

    // --- 애니메이션 상태 ---
    protected Vector2 originalAnchoredPos;
    protected Vector3 originalScale = Vector3.one;
    protected Tween currentTween;

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // ✅ Awake()에서는 자기 자신에게만 종속적인 초기화만 수행
        // Instantiate 직후에도 작동해야 하는 초기화 수행
        // ServiceLocator.Get() 등 외부 의존성이 필요한 초기화는 Start()에서 수행

        // 애니메이션 타겟 컴포넌트 설정
        if (animationRectTransform == null)
        {
            animationRectTransform = GetComponent<RectTransform>();
        }
        if (animationCanvasGroup == null)
        {
            animationCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (animationRectTransform != null)
        {
            originalAnchoredPos = animationRectTransform.anchoredPosition;
            originalScale = animationRectTransform.localScale;
        }

        if (initializeOnAwake && !isInitialized)
        {
            // 🎯 의존성 없는 기본 초기화 (Instantiate 직후 사용 가능)
            OnInitializeSelf();
        }
    }

    protected virtual void Start()
    {
        // ✅ Start()는 모든 Awake()가 완료된 후 호출됨 (Unity 보장)
        // 이 시점에서 ServiceLocator.Get()을 호출하면
        // 모든 서비스가 이미 RegisterSingleton()으로 등록된 상태이므로 항상 안전
        if (initializeOnAwake && !isInitialized)
        {
            // 🎯 의존성 있는 초기화 (ServiceLocator 등 외부 서비스 필요)
            OnInitializeWithDependencies();

            // 완전한 초기화 완료 표시
            isInitialized = true;
            currentState = UIPanelState.Inactive;
            Debug.Log($"[UIPanel] Initialized: {GetType().Name}");
        }

        if (hideOnStart)
        {
            gameObject.SetActive(false);
        }
    }

    protected virtual void OnDestroy()
    {
        // DOTween 애니메이션 정리
        currentTween?.Kill();
        currentTween = null;

        Cleanup();
    }

    #endregion

    #region IUIPanel 구현

    public virtual void Initialize()
    {
        if (isInitialized) return;

        currentState = UIPanelState.Initializing;

        // ✅ 의존성 있는 초기화만 수행
        // OnInitializeSelf()는 Awake()에서 이미 실행됨
        OnInitializeWithDependencies();

        // 하위 호환성을 위해 OnInitialize()도 호출
        OnInitialize();

        isInitialized = true;
        currentState = UIPanelState.Inactive;

        Debug.Log($"UI Panel Initialized: {GetType().Name}");
    }

    public virtual void OnShow()
    {
        if (currentState == UIPanelState.Active || currentState == UIPanelState.Showing) return;

        currentState = UIPanelState.Showing;
        gameObject.SetActive(true);

        OnShowPanel();
        PlayShowAnimation();
    }

    public virtual void OnHide()
    {
        if (currentState == UIPanelState.Inactive || currentState == UIPanelState.Hiding) return;

        currentState = UIPanelState.Hiding;
        OnHidePanel();
        PlayHideAnimation();
    }

    public virtual void Cleanup()
    {
        if (!isInitialized) return;

        OnCleanup();
        OnPanelShown = null;
        OnPanelHidden = null;
        isInitialized = false;
        currentState = UIPanelState.Inactive;

        Debug.Log($"UI Panel Cleaned Up: {GetType().Name}");
    }

    #endregion

    #region 가상 메서드 (파생 클래스에서 오버라이드)

    /// <summary>
    /// 의존성 없는 초기화 (Awake에서 호출)
    /// UI 컴포넌트 검증, 내부 상태 초기화 등
    /// ServiceLocator.Get() 사용 금지!
    /// </summary>
    protected virtual void OnInitializeSelf() { }

    /// <summary>
    /// 의존성 있는 초기화 (Start에서 호출)
    /// ServiceLocator.Get(), 외부 서비스 접근 등
    /// </summary>
    protected virtual void OnInitializeWithDependencies() { }

    /// <summary>
    /// 하위 호환성 유지: Initialize() → OnInitializeSelf() + OnInitializeWithDependencies()
    /// </summary>
    [System.Obsolete("Use OnInitializeSelf() and OnInitializeWithDependencies() instead")]
    protected virtual void OnInitialize() { }

    protected virtual void OnShowPanel() { }
    protected virtual void OnHidePanel() { }
    protected virtual void OnCleanup() { }

    #endregion

    #region Protected Event Helpers

    /// <summary>
    /// 파생 클래스가 애니메이션 완료 후 OnPanelShown 이벤트를 발생시킬 수 있도록 지원
    /// </summary>
    protected void RaiseOnPanelShown() => OnPanelShown?.Invoke(this);

    /// <summary>
    /// 파생 클래스가 애니메이션 완료 후 OnPanelHidden 이벤트를 발생시킬 수 있도록 지원
    /// </summary>
    protected void RaiseOnPanelHidden() => OnPanelHidden?.Invoke(this);

    #endregion

    #region Animation Helpers

    /// <summary>
    /// 패널 표시 애니메이션 실행
    /// </summary>
    protected virtual void PlayShowAnimation()
    {
        currentTween?.Kill();

        switch (showAnimation)
        {
            case UIPanelAnimationType.None:
                OnShowAnimationComplete();
                break;
            case UIPanelAnimationType.Fade:
                PlayFadeIn();
                break;
            case UIPanelAnimationType.ScaleSlideFromTop:
                PlayScaleSlideIn();
                break;
            default:
                OnShowAnimationComplete();
                break;
        }
    }

    /// <summary>
    /// 패널 숨김 애니메이션 실행
    /// </summary>
    protected virtual void PlayHideAnimation()
    {
        currentTween?.Kill();

        switch (hideAnimation)
        {
            case UIPanelAnimationType.None:
                OnHideAnimationComplete();
                break;
            case UIPanelAnimationType.Fade:
                PlayFadeOut();
                break;
            case UIPanelAnimationType.ScaleSlideFromTop:
                PlayScaleSlideOut();
                break;
            default:
                OnHideAnimationComplete();
                break;
        }
    }

    /// <summary>
    /// 표시 애니메이션 완료 시 공통 처리
    /// </summary>
    protected virtual void OnShowAnimationComplete()
    {
        currentTween = null;
        currentState = UIPanelState.Active;
        RaiseOnPanelShown();

        Debug.Log($"UI Panel Shown: {GetType().Name}");
    }

    /// <summary>
    /// 숨김 애니메이션 완료 시 공통 처리
    /// </summary>
    protected virtual void OnHideAnimationComplete()
    {
        currentTween = null;
        gameObject.SetActive(false);
        currentState = UIPanelState.Inactive;
        RaiseOnPanelHidden();

        Debug.Log($"UI Panel Hidden: {GetType().Name}");
    }

    /// <summary>
    /// 페이드 인 애니메이션
    /// </summary>
    protected virtual void PlayFadeIn()
    {
        if (animationCanvasGroup == null)
        {
            OnShowAnimationComplete();
            return;
        }

        animationCanvasGroup.alpha = 0f;
        animationCanvasGroup.interactable = false;

        currentTween = animationCanvasGroup
            .DOFade(1f, showDuration)
            .SetEase(showEase)
            .OnComplete(() =>
            {
                animationCanvasGroup.interactable = true;
                OnShowAnimationComplete();
            });
    }

    /// <summary>
    /// 페이드 아웃 애니메이션
    /// </summary>
    protected virtual void PlayFadeOut()
    {
        if (animationCanvasGroup == null)
        {
            OnHideAnimationComplete();
            return;
        }

        animationCanvasGroup.interactable = false;

        currentTween = animationCanvasGroup
            .DOFade(0f, hideDuration)
            .SetEase(hideEase)
            .OnComplete(OnHideAnimationComplete);
    }

    /// <summary>
    /// 위에서 내려오며 스케일 인
    /// </summary>
    protected virtual void PlayScaleSlideIn()
    {
        if (animationRectTransform == null)
        {
            OnShowAnimationComplete();
            return;
        }

        animationRectTransform.anchoredPosition = originalAnchoredPos + new Vector2(0f, slideOffsetY);
        animationRectTransform.localScale = originalScale * scaleFrom;

        var sequence = DOTween.Sequence();
        currentTween = sequence;

        sequence.Append(animationRectTransform
            .DOAnchorPos(originalAnchoredPos, showDuration)
            .SetEase(showEase));

        sequence.Join(animationRectTransform
            .DOScale(originalScale, showDuration)
            .SetEase(showEase));

        sequence.OnComplete(OnShowAnimationComplete);
    }

    /// <summary>
    /// 위로 올라가며 스케일 아웃
    /// </summary>
    protected virtual void PlayScaleSlideOut()
    {
        if (animationRectTransform == null)
        {
            OnHideAnimationComplete();
            return;
        }

        var targetPos = originalAnchoredPos + new Vector2(0f, slideOffsetY);

        var sequence = DOTween.Sequence();
        currentTween = sequence;

        sequence.Append(animationRectTransform
            .DOAnchorPos(targetPos, hideDuration)
            .SetEase(hideEase));

        sequence.Join(animationRectTransform
            .DOScale(originalScale * scaleFrom, hideDuration)
            .SetEase(hideEase));

        sequence.OnComplete(OnHideAnimationComplete);
    }

    #endregion

    #region 유틸리티 메서드

    public void Toggle()
    {
        if (IsActive)
            OnHide();
        else
            OnShow();
    }

    protected bool ValidateState()
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"UI Panel not initialized: {GetType().Name}");
            return false;
        }
        return true;
    }

    #endregion
}
