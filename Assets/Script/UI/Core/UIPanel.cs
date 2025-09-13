using System;
using UnityEngine;

/// <summary>
/// UI 패널의 추상 베이스 클래스
/// IUIPanel 인터페이스 구현 및 공통 기능 제공
/// </summary>
public abstract class UIPanel : MonoBehaviour, IUIPanel
{
    [Header("UI Panel Settings")]
    [SerializeField] protected string panelID;
    [SerializeField] protected UIPanelPriority priority = UIPanelPriority.Normal;
    [SerializeField] protected bool initializeOnAwake = true;
    [SerializeField] protected bool hideOnStart = true;
    
    // 프로퍼티
    public string PanelID => string.IsNullOrEmpty(panelID) ? GetType().Name : panelID;
    public bool IsActive => gameObject.activeInHierarchy;
    public UIPanelPriority Priority => priority;
    
    // 상태 관리
    protected UIPanelState currentState = UIPanelState.Inactive;
    public UIPanelState CurrentState => currentState;
    
    // 이벤트
    public event Action<IUIPanel> OnPanelShown;
    public event Action<IUIPanel> OnPanelHidden;
    
    // 초기화 상태
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
        
        // 파생 클래스에서 오버라이드
        OnInitialize();
        
        isInitialized = true;
        currentState = UIPanelState.Inactive;
        
        Debug.Log($"UI Panel Initialized: {PanelID}");
    }
    
    public virtual void OnShow()
    {
        if (currentState == UIPanelState.Active) return;
        
        currentState = UIPanelState.Showing;
        gameObject.SetActive(true);
        
        // 파생 클래스에서 오버라이드
        OnShowPanel();
        
        currentState = UIPanelState.Active;
        OnPanelShown?.Invoke(this);
        
        Debug.Log($"UI Panel Shown: {PanelID}");
    }
    
    public virtual void OnHide()
    {
        if (currentState == UIPanelState.Inactive) return;
        
        currentState = UIPanelState.Hiding;
        
        // 파생 클래스에서 오버라이드
        OnHidePanel();
        
        gameObject.SetActive(false);
        currentState = UIPanelState.Inactive;
        OnPanelHidden?.Invoke(this);
        
        Debug.Log($"UI Panel Hidden: {PanelID}");
    }
    
    public virtual void Cleanup()
    {
        if (!isInitialized) return;
        
        // 파생 클래스에서 오버라이드
        OnCleanup();
        
        // 이벤트 정리
        OnPanelShown = null;
        OnPanelHidden = null;
        
        isInitialized = false;
        currentState = UIPanelState.Inactive;
        
        Debug.Log($"UI Panel Cleaned Up: {PanelID}");
    }
    
    #endregion
    
    #region 가상 메서드 (파생 클래스에서 오버라이드)
    
    /// <summary>
    /// 파생 클래스에서 초기화 로직 구현
    /// </summary>
    protected virtual void OnInitialize() { }
    
    /// <summary>
    /// 파생 클래스에서 패널 표시 로직 구현
    /// </summary>
    protected virtual void OnShowPanel() { }
    
    /// <summary>
    /// 파생 클래스에서 패널 숨김 로직 구현
    /// </summary>
    protected virtual void OnHidePanel() { }
    
    /// <summary>
    /// 파생 클래스에서 정리 로직 구현
    /// </summary>
    protected virtual void OnCleanup() { }
    
    #endregion
    
    #region 유틸리티 메서드
    
    /// <summary>
    /// 패널 토글 (표시/숨김)
    /// </summary>
    public void Toggle()
    {
        if (IsActive)
            OnHide();
        else
            OnShow();
    }
    
    /// <summary>
    /// 패널 상태 유효성 검사
    /// </summary>
    /// <returns>유효한 상태인지 여부</returns>
    protected bool ValidateState()
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"UI Panel not initialized: {PanelID}");
            return false;
        }
        return true;
    }
    
    /// <summary>
    /// Inspector에서 Panel ID 자동 설정
    /// </summary>
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(panelID))
        {
            panelID = GetType().Name;
        }
    }
    
    #endregion
}