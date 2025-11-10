using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core;

/// <summary>
/// 전역 UI 패널 관리자 (Global UI Manager)
/// 게임 전체에서 사용되는 UI (설정, 알림 등)를 관리
///
/// Lifecycle: DontDestroyOnLoad로 씬 전환 시에도 유지
/// Access: ServiceLocator.Get<GlobalUIPanelManager>()
/// Architecture: ServiceLocator 패턴 (Singleton Instance 제거)
/// </summary>
public class GlobalUIPanelManager : MonoBehaviour, IGlobalService
{
    [Header("Global Canvas Settings")]
    [SerializeField] private Canvas globalCanvas;
    [SerializeField] private int canvasSortOrder = 1000;

    [Header("Manager Settings")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool debugMode = false;

    // --- 패널 관리 (타입 기반) ---
    private readonly Dictionary<Type, IUIPanel> globalPanels = new Dictionary<Type, IUIPanel>();
    private readonly Stack<IUIPanel> panelStack = new Stack<IUIPanel>();
    private readonly List<IUIPanel> activePanels = new List<IUIPanel>();

    // --- 이벤트 ---
    public static event Action<IUIPanel> OnGlobalPanelShown;
    public static event Action<IUIPanel> OnGlobalPanelHidden;
    public static event Action<IUIPanel> OnGlobalPanelRegistered;
    public static event Action<Type> OnGlobalPanelUnregistered;

    #region Unity Lifecycle

    private void Awake()
    {
        // ✅ DontDestroyOnLoad 적용 (씬 전환 시 유지)
        DontDestroyOnLoad(gameObject);

        // Canvas 설정
        InitializeCanvas();

        if (debugMode)
            Debug.Log("[GlobalUIPanelManager] Initialized with DontDestroyOnLoad");
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeAllPanels();
            AutoRegisterGlobalPanels();
        }
    }

    private void OnDestroy()
    {
        CleanupAllPanels();
    }

    #endregion

    #region Canvas Initialization

    /// <summary>
    /// Global Canvas 초기화 및 Sort Order 설정
    /// </summary>
    private void InitializeCanvas()
    {
        if (globalCanvas == null)
        {
            globalCanvas = GetComponent<Canvas>();
        }

        if (globalCanvas == null)
        {
            Debug.LogError("[GlobalUIPanelManager] Canvas component not found! " +
                          "Please add Canvas component or assign globalCanvas in Inspector.");
            return;
        }

        // ✅ Sort Order 설정: 전역 UI는 항상 최상위 렌더링 (1000)
        globalCanvas.sortingOrder = canvasSortOrder;
        globalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        if (debugMode)
            Debug.Log($"[GlobalUIPanelManager] Canvas initialized with Sort Order: {canvasSortOrder}");
    }

    #endregion

    #region Panel Registration

    /// <summary>
    /// 전역 패널 등록
    /// </summary>
    public void RegisterGlobalPanel(IUIPanel panel)
    {
        if (panel == null)
        {
            Debug.LogError("[GlobalUIPanelManager] Cannot register null panel");
            return;
        }

        Type panelType = panel.GetType();

        if (globalPanels.ContainsKey(panelType))
        {
            Debug.LogWarning($"[GlobalUIPanelManager] Panel already registered: {panelType.Name}");
            return;
        }

        globalPanels[panelType] = panel;

        // 이벤트 구독
        panel.OnPanelShown += OnPanelShownHandler;
        panel.OnPanelHidden += OnPanelHiddenHandler;

        // IOpenablePanel 자동 버튼 바인딩
        if (panel is IOpenablePanel openablePanel)
        {
            if (openablePanel.OpenButton != null)
                openablePanel.OpenButton.onClick.AddListener(panel.OnShow);

            if (openablePanel.CloseButton != null)
                openablePanel.CloseButton.onClick.AddListener(panel.OnHide);
        }

        // IGameResultPanel 자동 버튼 바인딩
        if (panel is IGameResultPanel resultPanel)
        {
            if (resultPanel.PrimaryActionButton != null)
                resultPanel.PrimaryActionButton.onClick.AddListener(resultPanel.OnPrimaryAction);

            if (resultPanel.SecondaryActionButton != null)
                resultPanel.SecondaryActionButton.onClick.AddListener(resultPanel.OnSecondaryAction);
        }

        // IDualClosePanel 자동 버튼 바인딩
        if (panel is IDualClosePanel dualClosePanel)
        {
            if (dualClosePanel.PrimaryCloseButton != null)
                dualClosePanel.PrimaryCloseButton.onClick.AddListener(panel.OnHide);

            if (dualClosePanel.SecondaryCloseButton != null)
                dualClosePanel.SecondaryCloseButton.onClick.AddListener(panel.OnHide);
        }

        // IDialogPanel 자동 버튼 바인딩
        if (panel is IDialogPanel dialogPanel)
        {
            // 취소 버튼은 자동으로 OnHide에 바인딩
            if (dialogPanel.CancelButton != null)
                dialogPanel.CancelButton.onClick.AddListener(panel.OnHide);

            // 확인 버튼은 각 구현체에서 처리
            // (예: ConfirmPanelWithStageData가 OnInitializeWithDependencies에서 설정)
        }

        OnGlobalPanelRegistered?.Invoke(panel);

        if (debugMode)
            Debug.Log($"[GlobalUIPanelManager] Registered: {panelType.Name}");
    }

    /// <summary>
    /// 전역 패널 등록 해제
    /// </summary>
    public void UnregisterGlobalPanel<T>() where T : class, IUIPanel
    {
        Type panelType = typeof(T);
        if (!globalPanels.TryGetValue(panelType, out IUIPanel panel))
        {
            Debug.LogWarning($"[GlobalUIPanelManager] Panel not registered: {panelType.Name}");
            return;
        }

        // 버튼 이벤트 해제
        if (panel is IOpenablePanel openablePanel)
        {
            if (openablePanel.OpenButton != null)
                openablePanel.OpenButton.onClick.RemoveListener(panel.OnShow);

            if (openablePanel.CloseButton != null)
                openablePanel.CloseButton.onClick.RemoveListener(panel.OnHide);
        }

        if (panel is IGameResultPanel resultPanel)
        {
            if (resultPanel.PrimaryActionButton != null)
                resultPanel.PrimaryActionButton.onClick.RemoveListener(resultPanel.OnPrimaryAction);

            if (resultPanel.SecondaryActionButton != null)
                resultPanel.SecondaryActionButton.onClick.RemoveListener(resultPanel.OnSecondaryAction);
        }

        // IDualClosePanel 버튼 이벤트 해제
        if (panel is IDualClosePanel dualClosePanel)
        {
            if (dualClosePanel.PrimaryCloseButton != null)
                dualClosePanel.PrimaryCloseButton.onClick.RemoveListener(panel.OnHide);

            if (dualClosePanel.SecondaryCloseButton != null)
                dualClosePanel.SecondaryCloseButton.onClick.RemoveListener(panel.OnHide);
        }

        // IDialogPanel 버튼 이벤트 해제
        if (panel is IDialogPanel dialogPanel)
        {
            if (dialogPanel.CancelButton != null)
                dialogPanel.CancelButton.onClick.RemoveListener(panel.OnHide);
        }

        // 이벤트 구독 해제
        panel.OnPanelShown -= OnPanelShownHandler;
        panel.OnPanelHidden -= OnPanelHiddenHandler;

        // 리스트에서 제거
        activePanels.Remove(panel);

        if (panelStack.Count > 0 && panelStack.Peek() == panel)
            panelStack.Pop();

        globalPanels.Remove(panelType);

        OnGlobalPanelUnregistered?.Invoke(panelType);

        if (debugMode)
            Debug.Log($"[GlobalUIPanelManager] Unregistered: {panelType.Name}");
    }

    /// <summary>
    /// 하위의 모든 IUIPanel을 자동으로 전역 패널로 등록
    /// </summary>
    public void AutoRegisterGlobalPanels()
    {
        var allPanels = GetComponentsInChildren<MonoBehaviour>(true);
        int registeredCount = 0;

        foreach (var mono in allPanels)
        {
            if (mono is IUIPanel panel && mono != this)
            {
                RegisterGlobalPanel(panel);
                registeredCount++;
            }
        }

        if (debugMode)
            Debug.Log($"[GlobalUIPanelManager] Auto-registered {registeredCount} global panels");
    }

    #endregion

    #region Panel Access

    /// <summary>
    /// 전역 패널 조회
    /// </summary>
    public T GetGlobalPanel<T>() where T : class, IUIPanel
    {
        Type panelType = typeof(T);

        if (globalPanels.TryGetValue(panelType, out IUIPanel panel))
        {
            return panel as T;
        }

        if (debugMode)
            Debug.LogWarning($"[GlobalUIPanelManager] Global panel not found: {panelType.Name}");

        return null;
    }

    /// <summary>
    /// 전역 패널 등록 여부 확인
    /// </summary>
    public bool HasGlobalPanel<T>() where T : class, IUIPanel
    {
        return globalPanels.ContainsKey(typeof(T));
    }

    #endregion

    #region Panel Control

    /// <summary>
    /// 전역 패널 표시
    /// </summary>
    public void ShowGlobalPanel<T>() where T : class, IUIPanel
    {
        var panel = GetGlobalPanel<T>();
        if (panel != null)
        {
            panel.OnShow();
        }
        else
        {
            Debug.LogError($"[GlobalUIPanelManager] Cannot show panel - not found: {typeof(T).Name}");
        }
    }

    /// <summary>
    /// 전역 패널 숨김
    /// </summary>
    public void HideGlobalPanel<T>() where T : class, IUIPanel
    {
        var panel = GetGlobalPanel<T>();
        if (panel != null)
        {
            panel.OnHide();
        }
    }

    /// <summary>
    /// 전역 패널 토글
    /// </summary>
    public void ToggleGlobalPanel<T>() where T : UIPanel
    {
        var panel = GetGlobalPanel<T>();
        if (panel != null)
        {
            panel.Toggle();
        }
    }

    /// <summary>
    /// 모든 전역 패널 숨김
    /// </summary>
    public void HideAllGlobalPanels()
    {
        foreach (var panel in activePanels.ToArray())
        {
            panel.OnHide();
        }
    }

    #endregion

    #region Panel Stack Management

    /// <summary>
    /// 전역 패널을 스택에 푸시하고 표시
    /// </summary>
    public void PushGlobalPanel<T>() where T : class, IUIPanel
    {
        var panel = GetGlobalPanel<T>();
        if (panel != null)
        {
            if (panelStack.Count > 0)
            {
                // 이전 패널을 숨기려면 주석 해제
                // panelStack.Peek().OnHide();
            }
            panelStack.Push(panel);
            panel.OnShow();
        }
    }

    /// <summary>
    /// 스택에서 전역 패널 팝
    /// </summary>
    public void PopGlobalPanel()
    {
        if (panelStack.Count > 0)
        {
            var panel = panelStack.Pop();
            panel.OnHide();

            // 이전 패널을 다시 보여주려면 주석 해제
            // if (panelStack.Count > 0) panelStack.Peek().OnShow();
        }
    }

    /// <summary>
    /// 스택 최상위 전역 패널 조회
    /// </summary>
    public IUIPanel GetTopGlobalPanel()
    {
        return panelStack.Count > 0 ? panelStack.Peek() : null;
    }

    #endregion

    #region Event Handlers

    private void OnPanelShownHandler(IUIPanel panel)
    {
        if (!activePanels.Contains(panel))
        {
            activePanels.Add(panel);
        }
        OnGlobalPanelShown?.Invoke(panel);

        if (debugMode)
            Debug.Log($"[GlobalUIPanelManager] Panel shown: {panel.GetType().Name}");
    }

    private void OnPanelHiddenHandler(IUIPanel panel)
    {
        activePanels.Remove(panel);
        OnGlobalPanelHidden?.Invoke(panel);

        if (debugMode)
            Debug.Log($"[GlobalUIPanelManager] Panel hidden: {panel.GetType().Name}");
    }

    #endregion

    #region Initialization & Cleanup

    private void InitializeAllPanels()
    {
        foreach (var panel in globalPanels.Values)
        {
            panel.Initialize();
        }

        if (debugMode)
            Debug.Log($"[GlobalUIPanelManager] All panels initialized: {globalPanels.Count}");
    }

    private void CleanupAllPanels()
    {
        foreach (var panelType in globalPanels.Keys.ToArray())
        {
            if (globalPanels.TryGetValue(panelType, out var panel))
            {
                panel.Cleanup();
            }
        }

        globalPanels.Clear();
        activePanels.Clear();
        panelStack.Clear();

        if (debugMode)
            Debug.Log("[GlobalUIPanelManager] All panels cleaned up");
    }

    #endregion

    #region IGlobalService Implementation

    /// <summary>
    /// 전역 서비스 유효성 검증
    /// </summary>
    public bool IsValid()
    {
        bool isValid = gameObject != null && globalCanvas != null;

        if (!isValid && debugMode)
        {
            Debug.LogError($"[GlobalUIPanelManager] Validation failed - " +
                $"GameObject: {gameObject != null}, Canvas: {globalCanvas != null}");
        }

        return isValid;
    }

    #endregion

    #region Debug & Utility

    [ContextMenu("Log Global Panel Status")]
    public void LogPanelStatus()
    {
        Debug.Log("=== GlobalUIPanelManager Status ===");
        Debug.Log($"Registered Panels: {globalPanels.Count}");
        Debug.Log($"Active Panels: {activePanels.Count}");
        Debug.Log($"Stack Depth: {panelStack.Count}");
        Debug.Log($"Canvas Sort Order: {globalCanvas?.sortingOrder}");

        foreach (var kvp in globalPanels)
        {
            Debug.Log($"- {kvp.Key.Name}: {(kvp.Value.IsActive ? "Active" : "Inactive")}");
        }
        Debug.Log("====================================");
    }

    #endregion
}
