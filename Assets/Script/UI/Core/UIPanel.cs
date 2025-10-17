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
        if (initializeOnAwake)
        {
            Initialize();
        }
    }

    protected virtual void Start()
    {
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

    protected virtual void OnInitialize() { }
    protected virtual void OnShowPanel() { }
    protected virtual void OnHidePanel() { }
    protected virtual void OnCleanup() { }

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