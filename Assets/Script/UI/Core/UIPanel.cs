using System;
using UnityEngine;

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

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // ✅ Awake()에서는 자기 자신에게만 종속적인 초기화만 수행
        // Instantiate 직후에도 작동해야 하는 초기화 수행
        // ServiceLocator.Get() 등 외부 의존성이 필요한 초기화는 Start()에서 수행

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
            Debug.Log($"UI Panel Initialized: {GetType().Name}");
        }

        if (hideOnStart)
        {
            gameObject.SetActive(false);
        }
    }

    protected virtual void OnDestroy()
    {
        Cleanup();
    }

    #endregion

    #region IUIPanel 구현

    public virtual void Initialize()
    {
        if (isInitialized) return;

        currentState = UIPanelState.Initializing;
        OnInitialize();
        isInitialized = true;
        currentState = UIPanelState.Inactive;

        Debug.Log($"UI Panel Initialized: {GetType().Name}");
    }

    public virtual void OnShow()
    {
        if (currentState == UIPanelState.Active) return;

        currentState = UIPanelState.Showing;
        gameObject.SetActive(true);
        OnShowPanel();
        currentState = UIPanelState.Active;
        OnPanelShown?.Invoke(this);

        Debug.Log($"UI Panel Shown: {GetType().Name}");
    }

    public virtual void OnHide()
    {
        if (currentState == UIPanelState.Inactive) return;

        currentState = UIPanelState.Hiding;
        OnHidePanel();
        gameObject.SetActive(false);
        currentState = UIPanelState.Inactive;
        OnPanelHidden?.Invoke(this);

        Debug.Log($"UI Panel Hidden: {GetType().Name}");
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