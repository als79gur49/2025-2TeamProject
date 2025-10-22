using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core;

/// <summary>
/// 씬 종속 UI 패널 관리자 (Local UI Manager)
/// 현재 활성화된 씬에만 존재하는 UI (게임 결과, 씬 전용 UI 등)를 관리
///
/// Lifecycle: 씬 로드 시 생성, 씬 언로드 시 자동 파괴
/// Access: ServiceLocator.Get<LocalUIPanelManager>()
/// Architecture: Scene Scoped Service (씬마다 별도 인스턴스)
/// </summary>
public class LocalUIPanelManager : MonoBehaviour
{
    [Header("Local Canvas Settings")]
    [SerializeField] private Canvas localCanvas;
    [SerializeField] private int canvasSortOrder = 0;

    [Header("Auto Registration")]
    [SerializeField] private bool autoRegisterOnAwake = true;

    [Header("Manager Settings")]
    [SerializeField] private bool debugMode = false;

    // --- 패널 관리 (타입 기반) ---
    private readonly Dictionary<Type, IUIPanel> localPanels = new Dictionary<Type, IUIPanel>();
    private readonly Stack<IUIPanel> panelStack = new Stack<IUIPanel>();
    private readonly List<IUIPanel> activePanels = new List<IUIPanel>();

    // --- 이벤트 ---
    public static event Action<IUIPanel> OnLocalPanelShown;
    public static event Action<IUIPanel> OnLocalPanelHidden;
    public static event Action<IUIPanel> OnLocalPanelRegistered;
    public static event Action<Type> OnLocalPanelUnregistered;

    #region Unity Lifecycle

    private void Awake()
    {
        // ✅ ServiceLocator에 자체 등록 (씬 스코프)
        ServiceLocator.Register<LocalUIPanelManager>(this);

        if (debugMode)
            Debug.Log($"[LocalUIPanelManager] Registered to ServiceLocator for scene: {gameObject.scene.name}");

        // Canvas 설정
        InitializeCanvas();

        // 자동 패널 등록
        if (autoRegisterOnAwake)
        {
            AutoRegisterLocalPanels();
        }
    }

    private void Start()
    {
        InitializeAllPanels();
    }

    private void OnDestroy()
    {
        // ✅ ServiceLocator에서 자체 해제
        ServiceLocator.Unregister<LocalUIPanelManager>();

        if (debugMode)
            Debug.Log($"[LocalUIPanelManager] Unregistered from ServiceLocator (scene destroyed)");

        // 모든 패널 정리
        CleanupAllPanels();
    }

    #endregion

    #region Canvas Initialization

    /// <summary>
    /// Local Canvas 초기화 및 Sort Order 설정
    /// </summary>
    private void InitializeCanvas()
    {
        if (localCanvas == null)
        {
            localCanvas = GetComponent<Canvas>();
        }

        if (localCanvas == null)
        {
            Debug.LogError("[LocalUIPanelManager] Canvas component not found! " +
                          "Please add Canvas component or assign localCanvas in Inspector.");
            return;
        }

        // ✅ Sort Order 설정: 씬 종속 UI는 기본 레벨 (0)
        localCanvas.sortingOrder = canvasSortOrder;
        localCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        if (debugMode)
            Debug.Log($"[LocalUIPanelManager] Canvas initialized with Sort Order: {canvasSortOrder}");
    }

    #endregion

    #region Panel Registration

    /// <summary>
    /// 씬 종속 패널 등록
    /// </summary>
    public void RegisterLocalPanel(IUIPanel panel)
    {
        if (panel == null)
        {
            Debug.LogError("[LocalUIPanelManager] Cannot register null panel");
            return;
        }

        Type panelType = panel.GetType();

        if (localPanels.ContainsKey(panelType))
        {
            Debug.LogWarning($"[LocalUIPanelManager] Panel already registered: {panelType.Name}");
            return;
        }

        localPanels[panelType] = panel;

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

        OnLocalPanelRegistered?.Invoke(panel);

        if (debugMode)
            Debug.Log($"[LocalUIPanelManager] Registered: {panelType.Name}");
    }

    /// <summary>
    /// 씬 종속 패널 등록 해제
    /// </summary>
    public void UnregisterLocalPanel<T>() where T : class, IUIPanel
    {
        Type panelType = typeof(T);
        if (!localPanels.TryGetValue(panelType, out IUIPanel panel))
        {
            Debug.LogWarning($"[LocalUIPanelManager] Panel not registered: {panelType.Name}");
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

        // 이벤트 구독 해제
        panel.OnPanelShown -= OnPanelShownHandler;
        panel.OnPanelHidden -= OnPanelHiddenHandler;

        // 리스트에서 제거
        activePanels.Remove(panel);

        if (panelStack.Count > 0 && panelStack.Peek() == panel)
            panelStack.Pop();

        localPanels.Remove(panelType);

        OnLocalPanelUnregistered?.Invoke(panelType);

        if (debugMode)
            Debug.Log($"[LocalUIPanelManager] Unregistered: {panelType.Name}");
    }

    /// <summary>
    /// 하위의 모든 IUIPanel을 자동으로 씬 종속 패널로 등록
    /// </summary>
    public void AutoRegisterLocalPanels()
    {
        var allPanels = GetComponentsInChildren<MonoBehaviour>(true);
        int registeredCount = 0;

        foreach (var mono in allPanels)
        {
            if (mono is IUIPanel panel && mono != this)
            {
                RegisterLocalPanel(panel);
                registeredCount++;
            }
        }

        if (debugMode)
            Debug.Log($"[LocalUIPanelManager] Auto-registered {registeredCount} local panels");
    }

    #endregion

    #region Panel Access

    /// <summary>
    /// 씬 종속 패널 조회
    /// </summary>
    public T GetLocalPanel<T>() where T : class, IUIPanel
    {
        Type panelType = typeof(T);

        if (localPanels.TryGetValue(panelType, out IUIPanel panel))
        {
            return panel as T;
        }

        if (debugMode)
            Debug.LogWarning($"[LocalUIPanelManager] Local panel not found: {panelType.Name}");

        return null;
    }

    /// <summary>
    /// 씬 종속 패널 등록 여부 확인
    /// </summary>
    public bool HasLocalPanel<T>() where T : class, IUIPanel
    {
        return localPanels.ContainsKey(typeof(T));
    }

    #endregion

    #region Panel Control

    /// <summary>
    /// 씬 종속 패널 표시
    /// </summary>
    public void ShowLocalPanel<T>() where T : class, IUIPanel
    {
        var panel = GetLocalPanel<T>();
        if (panel != null)
        {
            panel.OnShow();
        }
        else
        {
            Debug.LogError($"[LocalUIPanelManager] Cannot show panel - not found: {typeof(T).Name}");
        }
    }

    /// <summary>
    /// 씬 종속 패널 숨김
    /// </summary>
    public void HideLocalPanel<T>() where T : class, IUIPanel
    {
        var panel = GetLocalPanel<T>();
        if (panel != null)
        {
            panel.OnHide();
        }
    }

    /// <summary>
    /// 씬 종속 패널 토글
    /// </summary>
    public void ToggleLocalPanel<T>() where T : UIPanel
    {
        var panel = GetLocalPanel<T>();
        if (panel != null)
        {
            panel.Toggle();
        }
    }

    /// <summary>
    /// 모든 씬 종속 패널 숨김
    /// </summary>
    public void HideAllLocalPanels()
    {
        foreach (var panel in activePanels.ToArray())
        {
            panel.OnHide();
        }
    }

    #endregion

    #region Panel Stack Management

    /// <summary>
    /// 씬 종속 패널을 스택에 푸시하고 표시
    /// </summary>
    public void PushLocalPanel<T>() where T : class, IUIPanel
    {
        var panel = GetLocalPanel<T>();
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
    /// 스택에서 씬 종속 패널 팝
    /// </summary>
    public void PopLocalPanel()
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
    /// 스택 최상위 씬 종속 패널 조회
    /// </summary>
    public IUIPanel GetTopLocalPanel()
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
        OnLocalPanelShown?.Invoke(panel);

        if (debugMode)
            Debug.Log($"[LocalUIPanelManager] Panel shown: {panel.GetType().Name}");
    }

    private void OnPanelHiddenHandler(IUIPanel panel)
    {
        activePanels.Remove(panel);
        OnLocalPanelHidden?.Invoke(panel);

        if (debugMode)
            Debug.Log($"[LocalUIPanelManager] Panel hidden: {panel.GetType().Name}");
    }

    #endregion

    #region Initialization & Cleanup

    private void InitializeAllPanels()
    {
        foreach (var panel in localPanels.Values)
        {
            panel.Initialize();
        }

        if (debugMode)
            Debug.Log($"[LocalUIPanelManager] All panels initialized: {localPanels.Count}");
    }

    private void CleanupAllPanels()
    {
        foreach (var panelType in localPanels.Keys.ToArray())
        {
            if (localPanels.TryGetValue(panelType, out var panel))
            {
                panel.Cleanup();
            }
        }

        localPanels.Clear();
        activePanels.Clear();
        panelStack.Clear();

        if (debugMode)
            Debug.Log("[LocalUIPanelManager] All panels cleaned up");
    }

    #endregion

    #region Debug & Utility

    [ContextMenu("Log Local Panel Status")]
    public void LogPanelStatus()
    {
        Debug.Log("=== LocalUIPanelManager Status ===");
        Debug.Log($"Scene: {gameObject.scene.name}");
        Debug.Log($"Registered Panels: {localPanels.Count}");
        Debug.Log($"Active Panels: {activePanels.Count}");
        Debug.Log($"Stack Depth: {panelStack.Count}");
        Debug.Log($"Canvas Sort Order: {localCanvas?.sortingOrder}");

        foreach (var kvp in localPanels)
        {
            Debug.Log($"- {kvp.Key.Name}: {(kvp.Value.IsActive ? "Active" : "Inactive")}");
        }
        Debug.Log("====================================");
    }

    #endregion
}
