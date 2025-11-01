using System;
using System.Collections.Generic;
using System.Linq; // ToArray() 사용을 위해 추가
using UnityEngine;

/// <summary>
/// UI 패널들의 생명주기를 관리하는 매니저
/// Singleton 패턴으로 전역 액세스 제공
/// </summary>
public class UIPanelManager : MonoBehaviour
{
    [Header("UI Panel Manager Settings")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool debugMode = false;

    // --- 패널 관리 (타입 기반으로 통일) ---
    private readonly Dictionary<Type, IUIPanel> typedPanels = new Dictionary<Type, IUIPanel>();
    private readonly Stack<IUIPanel> panelStack = new Stack<IUIPanel>();

    // --- 현재 활성화된 패널들 ---
    private readonly List<IUIPanel> activePanels = new List<IUIPanel>();

    // --- Singleton 패턴 ---
    private static UIPanelManager instance;
    public static UIPanelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIPanelManager>();

                if (instance == null)
                {
                    var managerGO = new GameObject("UIPanelManager");
                    instance = managerGO.AddComponent<UIPanelManager>();
                    DontDestroyOnLoad(managerGO);
                    Debug.Log("UIPanelManager 자동 생성됨");
                }
            }
            return instance;
        }
    }

    // --- 이벤트 ---
    public static event Action<IUIPanel> OnPanelShown;
    public static event Action<IUIPanel> OnPanelHidden;
    public static event Action<IUIPanel> OnPanelRegistered;
    public static event Action<Type> OnPanelUnregistered;

    #region Unity Lifecycle

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("UIPanelManager 중복 인스턴스 제거");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeAllPanels();
            AutoRegisterAllPanels();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            CleanupAllPanels();
            instance = null;
        }
    }

    #endregion

    #region Panel Registration

    public void RegisterPanel(IUIPanel panel)
    {
        if (panel == null)
        {
            Debug.LogError("등록하려는 패널이 null입니다.");
            return;
        }

        Type panelType = panel.GetType();

        if (typedPanels.ContainsKey(panelType))
        {
            Debug.LogWarning($"패널이 이미 등록되어 있습니다: {panelType.Name}");
            return;
        }

        typedPanels[panelType] = panel;

        panel.OnPanelShown += OnPanelShownHandler;
        panel.OnPanelHidden += OnPanelHiddenHandler;

        if (panel is IOpenablePanel openablePanel)
        {
            if (openablePanel.OpenButton != null)
            {
                openablePanel.OpenButton.onClick.AddListener(panel.OnShow);
            }
            if (openablePanel.CloseButton != null)
            {
                openablePanel.CloseButton.onClick.AddListener(panel.OnHide);
            }
        }

        if (panel is IGameResultPanel resultPanel)
        {
            if (resultPanel.PrimaryActionButton != null)
            {
                resultPanel.PrimaryActionButton.onClick.AddListener(resultPanel.OnPrimaryAction);
            }
            if (resultPanel.SecondaryActionButton != null)
            {
                resultPanel.SecondaryActionButton.onClick.AddListener(resultPanel.OnSecondaryAction);
            }
        }

        // IDualClosePanel 자동 버튼 바인딩
        if (panel is IDualClosePanel dualClosePanel)
        {
            if (dualClosePanel.PrimaryCloseButton != null)
            {
                dualClosePanel.PrimaryCloseButton.onClick.AddListener(panel.OnHide);
            }
            if (dualClosePanel.SecondaryCloseButton != null)
            {
                dualClosePanel.SecondaryCloseButton.onClick.AddListener(panel.OnHide);
            }
        }

        OnPanelRegistered?.Invoke(panel);

        if (debugMode)
            Debug.Log($"패널 등록 완료: {panelType.Name}");
    }

    public void UnregisterPanel<T>() where T : class, IUIPanel
    {
        Type panelType = typeof(T);
        if (!typedPanels.TryGetValue(panelType, out IUIPanel panel))
        {
            Debug.LogWarning($"등록되지 않은 패널입니다: {panelType.Name}");
            return;
        }

        if (panel is IOpenablePanel openablePanel)
        {
            if (openablePanel.OpenButton != null) openablePanel.OpenButton.onClick.RemoveListener(panel.OnShow);
            if (openablePanel.CloseButton != null) openablePanel.CloseButton.onClick.RemoveListener(panel.OnHide);
        }

        if (panel is IGameResultPanel resultPanel)
        {
            if (resultPanel.PrimaryActionButton != null) resultPanel.PrimaryActionButton.onClick.RemoveListener(resultPanel.OnPrimaryAction);
            if (resultPanel.SecondaryActionButton != null) resultPanel.SecondaryActionButton.onClick.RemoveListener(resultPanel.OnSecondaryAction);
        }

        // IDualClosePanel 버튼 이벤트 해제
        if (panel is IDualClosePanel dualClosePanel)
        {
            if (dualClosePanel.PrimaryCloseButton != null) dualClosePanel.PrimaryCloseButton.onClick.RemoveListener(panel.OnHide);
            if (dualClosePanel.SecondaryCloseButton != null) dualClosePanel.SecondaryCloseButton.onClick.RemoveListener(panel.OnHide);
        }

        panel.OnPanelShown -= OnPanelShownHandler;
        panel.OnPanelHidden -= OnPanelHiddenHandler;

        activePanels.Remove(panel);

        if (panelStack.Count > 0 && panelStack.Peek() == panel)
        {
            panelStack.Pop();
        }

        typedPanels.Remove(panelType);

        OnPanelUnregistered?.Invoke(panelType);

        if (debugMode)
            Debug.Log($"패널 등록 해제 완료: {panelType.Name}");
    }

    public void AutoRegisterAllPanels()
    {
        var allPanels = FindObjectsOfType<MonoBehaviour>(true);
        int registeredCount = 0;

        foreach (var mono in allPanels)
        {
            if (mono is IUIPanel panel)
            {
                RegisterPanel(panel);
                registeredCount++;
            }
        }

        if (debugMode)
            Debug.Log($"자동 패널 등록 완료: {registeredCount}개");
    }

    #endregion

    #region Panel Access

    public T GetPanel<T>() where T : class, IUIPanel
    {
        Type panelType = typeof(T);

        if (typedPanels.TryGetValue(panelType, out IUIPanel panel))
        {
            return panel as T;
        }

        Debug.LogWarning($"패널을 찾을 수 없습니다: {panelType.Name}");
        return null;
    }

    public bool HasPanel<T>() where T : class, IUIPanel
    {
        return typedPanels.ContainsKey(typeof(T));
    }

    #endregion

    #region Panel Control

    public void ShowPanel<T>() where T : class, IUIPanel
    {
        var panel = GetPanel<T>();
        if (panel != null)
        {
            panel.OnShow();
        }
    }

    public void HidePanel<T>() where T : class, IUIPanel
    {
        var panel = GetPanel<T>();
        if (panel != null)
        {
            panel.OnHide();
        }
    }

    public void TogglePanel<T>() where T : UIPanel
    {
        var panel = GetPanel<T>();
        if (panel != null)
        {
            panel.Toggle();
        }
    }

    public void HideAllPanels()
    {
        foreach (var panel in activePanels.ToArray())
        {
            panel.OnHide();
        }
    }

    #endregion

    #region Panel Stack Management

    public void PushPanel<T>() where T : class, IUIPanel
    {
        var panel = GetPanel<T>();
        if (panel != null)
        {
            if (panelStack.Count > 0)
            {
                // 옵션: 이전 패널을 숨기고 싶다면
                // panelStack.Peek().OnHide(); 
            }
            panelStack.Push(panel);
            panel.OnShow();
        }
    }

    public void PopPanel()
    {
        if (panelStack.Count > 0)
        {
            var panel = panelStack.Pop();
            panel.OnHide();

            // 옵션: 이전 패널을 다시 보여주고 싶다면
            // if (panelStack.Count > 0) { panelStack.Peek().OnShow(); }
        }
    }

    public IUIPanel GetTopPanel()
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
        OnPanelShown?.Invoke(panel);
        if (debugMode) Debug.Log($"패널 표시됨: {panel.GetType().Name}");
    }

    private void OnPanelHiddenHandler(IUIPanel panel)
    {
        activePanels.Remove(panel);
        OnPanelHidden?.Invoke(panel);
        if (debugMode) Debug.Log($"패널 숨겨짐: {panel.GetType().Name}");
    }

    #endregion

    #region Initialization & Cleanup

    private void InitializeAllPanels()
    {
        foreach (var panel in typedPanels.Values)
        {
            panel.Initialize();
        }
        if (debugMode) Debug.Log($"모든 패널 초기화 완료: {typedPanels.Count}개");
    }

    private void CleanupAllPanels()
    {
        foreach (var panelType in typedPanels.Keys.ToArray())
        {
            if (typedPanels.TryGetValue(panelType, out var panel))
            {
                panel.Cleanup();
            }
        }

        typedPanels.Clear();
        activePanels.Clear();
        panelStack.Clear();

        if (debugMode) Debug.Log("모든 패널 정리 완료");
    }

    #endregion

    #region Debug & Utility

    [ContextMenu("Log Panel Status")]
    public void LogPanelStatus()
    {
        Debug.Log("=== UI Panel Manager Status ===");
        Debug.Log($"등록된 패널: {typedPanels.Count}개");
        Debug.Log($"활성 패널: {activePanels.Count}개");
        Debug.Log($"스택 깊이: {panelStack.Count}");

        foreach (var kvp in typedPanels)
        {
            Debug.Log($"- {kvp.Key.Name}: {(kvp.Value.IsActive ? "활성" : "비활성")}");
        }
        Debug.Log("================================");
    }

    #endregion
}