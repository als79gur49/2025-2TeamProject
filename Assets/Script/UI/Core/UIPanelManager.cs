using System;
using System.Collections.Generic;
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

    [Header("인스펙터에서 UI연결")]
    [SerializeField]
    private List<IUIPanel> panels;
    // 패널 관리
    private readonly Dictionary<string, IUIPanel> registeredPanels = new Dictionary<string, IUIPanel>();
    private readonly Dictionary<Type, IUIPanel> typedPanels = new Dictionary<Type, IUIPanel>();
    private readonly Stack<IUIPanel> panelStack = new Stack<IUIPanel>();
    
    // 현재 활성화된 패널들
    private readonly List<IUIPanel> activePanels = new List<IUIPanel>();
    
    // Singleton 패턴
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
    
    // 이벤트
    public static event Action<IUIPanel> OnPanelShown;
    public static event Action<IUIPanel> OnPanelHidden;
    public static event Action<IUIPanel> OnPanelRegistered;
    public static event Action<string> OnPanelUnregistered;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Singleton 패턴 구현
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
    
    /// <summary>
    /// 패널 등록 (자동 검색)
    /// </summary>
    public void RegisterPanel(IUIPanel panel)
    {
        if (panel == null)
        {
            Debug.LogError("등록하려는 패널이 null입니다.");
            return;
        }
        
        string panelId = panel.PanelID;
        Type panelType = panel.GetType();
        
        // 중복 등록 확인
        if (registeredPanels.ContainsKey(panelId))
        {
            Debug.LogWarning($"패널이 이미 등록되어 있습니다: {panelId}");
            return;
        }
        
        // 패널 등록
        registeredPanels[panelId] = panel;
        typedPanels[panelType] = panel;
        
        // 이벤트 연결
        panel.OnPanelShown += OnPanelShownHandler;
        panel.OnPanelHidden += OnPanelHiddenHandler;
        
        OnPanelRegistered?.Invoke(panel);
        
        if (debugMode)
            Debug.Log($"패널 등록 완료: {panelId} ({panelType.Name})");
    }
    
    /// <summary>
    /// 패널 등록 해제
    /// </summary>
    public void UnregisterPanel(string panelId)
    {
        if (!registeredPanels.TryGetValue(panelId, out IUIPanel panel))
        {
            Debug.LogWarning($"등록되지 않은 패널입니다: {panelId}");
            return;
        }
        
        // 이벤트 해제
        panel.OnPanelShown -= OnPanelShownHandler;
        panel.OnPanelHidden -= OnPanelHiddenHandler;
        
        // 활성 패널에서 제거
        activePanels.Remove(panel);
        
        // 스택에서 제거
        if (panelStack.Count > 0 && panelStack.Peek() == panel)
        {
            panelStack.Pop();
        }
        
        // 딕셔너리에서 제거
        registeredPanels.Remove(panelId);
        typedPanels.Remove(panel.GetType());
        
        OnPanelUnregistered?.Invoke(panelId);
        
        if (debugMode)
            Debug.Log($"패널 등록 해제 완료: {panelId}");
    }
    
    /// <summary>
    /// 씬의 모든 UI 패널 자동 등록
    /// </summary>
    public void AutoRegisterAllPanels()
    {
        var allPanels = FindObjectsOfType<MonoBehaviour>();
        int registeredCount = 0;
        
        foreach (var mono in allPanels)
        {
            if (mono is IUIPanel panel)
            {
                RegisterPanel(panel);
                registeredCount++;

                Debug.Log($"등록된 패널:{panel.GetType()} / ID: {panel.PanelID}");
            }
        }
        
        if (debugMode)
            Debug.Log($"자동 패널 등록 완료: {registeredCount}개");
    }
    
    #endregion
    
    #region Panel Access
    
    /// <summary>
    /// ID로 패널 가져오기
    /// </summary>
    public T GetPanel<T>(string panelId) where T : class, IUIPanel
    {
        if (registeredPanels.TryGetValue(panelId, out IUIPanel panel))
        {
            return panel as T;
        }
        
        Debug.LogWarning($"패널을 찾을 수 없습니다: {panelId}");
        return null;
    }
    
    /// <summary>
    /// 타입으로 패널 가져오기
    /// </summary>
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
    
    /// <summary>
    /// 패널 존재 여부 확인
    /// </summary>
    public bool HasPanel(string panelId)
    {
        return registeredPanels.ContainsKey(panelId);
    }
    
    /// <summary>
    /// 패널 존재 여부 확인 (타입)
    /// </summary>
    public bool HasPanel<T>() where T : class, IUIPanel
    {
        return typedPanels.ContainsKey(typeof(T));
    }
    
    #endregion
    
    #region Panel Control
    
    /// <summary>
    /// 패널 표시 (ID)
    /// </summary>
    public void ShowPanel(string panelId)
    {
        var panel = GetPanel<IUIPanel>(panelId);
        if (panel != null)
        {
            panel.OnShow();
        }
    }
    
    /// <summary>
    /// 패널 표시 (타입)
    /// </summary>
    public void ShowPanel<T>() where T : class, IUIPanel
    {
        var panel = GetPanel<T>();
        if (panel != null)
        {
            panel.OnShow();
        }
    }
    
    /// <summary>
    /// 패널 숨김 (ID)
    /// </summary>
    public void HidePanel(string panelId)
    {
        var panel = GetPanel<IUIPanel>(panelId);
        if (panel != null)
        {
            panel.OnHide();
        }
    }
    
    /// <summary>
    /// 패널 숨김 (타입)
    /// </summary>
    public void HidePanel<T>() where T : class, IUIPanel
    {
        var panel = GetPanel<T>();
        if (panel != null)
        {
            panel.OnHide();
        }
    }
    
    /// <summary>
    /// 패널 토글 (ID)
    /// </summary>
    public void TogglePanel(string panelId)
    {
        var panel = GetPanel<UIPanel>(panelId);
        if (panel != null)
        {
            panel.Toggle();
        }
    }
    
    /// <summary>
    /// 패널 토글 (타입)
    /// </summary>
    public void TogglePanel<T>() where T : UIPanel
    {
        var panel = GetPanel<T>();
        if (panel != null)
        {
            panel.Toggle();
        }
    }
    
    /// <summary>
    /// 모든 패널 숨김
    /// </summary>
    public void HideAllPanels()
    {
        foreach (var panel in activePanels.ToArray()) // ToArray로 복사하여 iteration 중 수정 방지
        {
            panel.OnHide();
        }
    }
    
    #endregion
    
    #region Panel Stack Management
    
    /// <summary>
    /// 패널을 스택에 푸시하고 표시
    /// </summary>
    public void PushPanel<T>() where T : class, IUIPanel
    {
        var panel = GetPanel<T>();
        if (panel != null)
        {
            panelStack.Push(panel);
            panel.OnShow();
        }
    }
    
    /// <summary>
    /// 스택 최상위 패널 팝 및 숨김
    /// </summary>
    public void PopPanel()
    {
        if (panelStack.Count > 0)
        {
            var panel = panelStack.Pop();
            panel.OnHide();
        }
    }
    
    /// <summary>
    /// 현재 스택 최상위 패널
    /// </summary>
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
        
        if (debugMode)
            Debug.Log($"패널 표시됨: {panel.PanelID}");
    }
    
    private void OnPanelHiddenHandler(IUIPanel panel)
    {
        activePanels.Remove(panel);
        OnPanelHidden?.Invoke(panel);
        
        if (debugMode)
            Debug.Log($"패널 숨겨짐: {panel.PanelID}");
    }
    
    #endregion
    
    #region Initialization & Cleanup
    
    /// <summary>
    /// 모든 등록된 패널 초기화
    /// </summary>
    private void InitializeAllPanels()
    {
        foreach (var panel in registeredPanels.Values)
        {
            panel.Initialize();
        }
        
        if (debugMode)
            Debug.Log($"모든 패널 초기화 완료: {registeredPanels.Count}개");
    }
    
    /// <summary>
    /// 모든 패널 정리
    /// </summary>
    private void CleanupAllPanels()
    {
        foreach (var panel in registeredPanels.Values)
        {
            panel.Cleanup();
        }
        
        registeredPanels.Clear();
        typedPanels.Clear();
        activePanels.Clear();
        panelStack.Clear();
        
        if (debugMode)
            Debug.Log("모든 패널 정리 완료");
    }
    
    #endregion
    
    #region Debug & Utility
    
    /// <summary>
    /// 현재 패널 상태 로깅
    /// </summary>
    [ContextMenu("Log Panel Status")]
    public void LogPanelStatus()
    {
        Debug.Log("=== UI Panel Manager Status ===");
        Debug.Log($"등록된 패널: {registeredPanels.Count}개");
        Debug.Log($"활성 패널: {activePanels.Count}개");
        Debug.Log($"스택 깊이: {panelStack.Count}");
        
        foreach (var kvp in registeredPanels)
        {
            var panel = kvp.Value;
            Debug.Log($"- {kvp.Key}: {(panel.IsActive ? "활성" : "비활성")}");
        }
        Debug.Log("================================");
    }
    
    #endregion
}