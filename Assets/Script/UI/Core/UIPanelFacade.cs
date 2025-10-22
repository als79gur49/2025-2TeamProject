using UnityEngine;
using Game.Core;

/// <summary>
/// UI 패널 시스템의 단일 진입점 (Facade Pattern)
/// GlobalUIPanelManager와 LocalUIPanelManager의 복잡성을 숨기고
/// 통합된 API를 제공
///
/// Usage:
/// - 전역 UI: UIPanelFacade.ShowGlobalPanel<SettingsPanel>();
/// - 씬 UI: UIPanelFacade.ShowLocalPanel<VictoryPanel>();
/// - 자동 라우팅: UIPanelFacade.ShowPanel<T>(); (Global → Local 순서로 검색)
///
/// Architecture: ServiceLocator를 통해 Manager 접근 (Singleton 패턴 제거)
/// </summary>
public static class UIPanelFacade
{
    // ========================================
    // 전역 UI (Global) - DontDestroyOnLoad
    // ========================================

    /// <summary>
    /// 전역 패널 표시
    /// </summary>
    public static void ShowGlobalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<GlobalUIPanelManager>();

        if (manager == null)
        {
            Debug.LogError("[UIPanelFacade] GlobalUIPanelManager not found in ServiceLocator! " +
                          "Ensure ServiceBootstrap has initialized the global UI system.");
            return;
        }

        manager.ShowGlobalPanel<T>();
    }

    /// <summary>
    /// 전역 패널 숨김
    /// </summary>
    public static void HideGlobalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<GlobalUIPanelManager>();
        manager?.HideGlobalPanel<T>();
    }

    /// <summary>
    /// 전역 패널 토글
    /// </summary>
    public static void ToggleGlobalPanel<T>() where T : UIPanel
    {
        var manager = ServiceLocator.Get<GlobalUIPanelManager>();
        manager?.ToggleGlobalPanel<T>();
    }

    /// <summary>
    /// 전역 패널 조회
    /// </summary>
    public static T GetGlobalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<GlobalUIPanelManager>();
        return manager?.GetGlobalPanel<T>();
    }

    /// <summary>
    /// 전역 패널 존재 여부 확인
    /// </summary>
    public static bool HasGlobalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<GlobalUIPanelManager>();
        return manager?.HasGlobalPanel<T>() ?? false;
    }

    /// <summary>
    /// 모든 전역 패널 숨김
    /// </summary>
    public static void HideAllGlobalPanels()
    {
        var manager = ServiceLocator.Get<GlobalUIPanelManager>();
        manager?.HideAllGlobalPanels();
    }

    // ========================================
    // 씬 종속 UI (Local) - Scene Scoped
    // ========================================

    /// <summary>
    /// 씬 종속 패널 표시
    /// </summary>
    public static void ShowLocalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<LocalUIPanelManager>();

        if (manager == null)
        {
            Debug.LogError("[UIPanelFacade] LocalUIPanelManager not found in ServiceLocator! " +
                          "Ensure the current scene has a Canvas with LocalUIPanelManager component.");
            return;
        }

        manager.ShowLocalPanel<T>();
    }

    /// <summary>
    /// 씬 종속 패널 숨김
    /// </summary>
    public static void HideLocalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<LocalUIPanelManager>();
        manager?.HideLocalPanel<T>();
    }

    /// <summary>
    /// 씬 종속 패널 토글
    /// </summary>
    public static void ToggleLocalPanel<T>() where T : UIPanel
    {
        var manager = ServiceLocator.Get<LocalUIPanelManager>();
        manager?.ToggleLocalPanel<T>();
    }

    /// <summary>
    /// 씬 종속 패널 조회
    /// </summary>
    public static T GetLocalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<LocalUIPanelManager>();
        return manager?.GetLocalPanel<T>();
    }

    /// <summary>
    /// 씬 종속 패널 존재 여부 확인
    /// </summary>
    public static bool HasLocalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<LocalUIPanelManager>();
        return manager?.HasLocalPanel<T>() ?? false;
    }

    /// <summary>
    /// 모든 씬 종속 패널 숨김
    /// </summary>
    public static void HideAllLocalPanels()
    {
        var manager = ServiceLocator.Get<LocalUIPanelManager>();
        manager?.HideAllLocalPanels();
    }

    // ========================================
    // 자동 라우팅 (Smart Routing)
    // ========================================

    /// <summary>
    /// 패널 자동 표시 (Global → Local 순서로 검색)
    ///
    /// 동작 방식:
    /// 1. GlobalUIPanelManager에서 검색하여 있으면 표시
    /// 2. 없으면 LocalUIPanelManager에서 검색하여 표시
    /// 3. 둘 다 없으면 경고 로그
    /// </summary>
    public static void ShowPanel<T>() where T : class, IUIPanel
    {
        // 1. Global에서 검색
        var globalManager = ServiceLocator.Get<GlobalUIPanelManager>();
        if (globalManager != null && globalManager.HasGlobalPanel<T>())
        {
            globalManager.ShowGlobalPanel<T>();
            return;
        }

        // 2. Local에서 검색
        var localManager = ServiceLocator.Get<LocalUIPanelManager>();
        if (localManager != null && localManager.HasLocalPanel<T>())
        {
            localManager.ShowLocalPanel<T>();
            return;
        }

        // 3. 둘 다 없으면 경고
        Debug.LogWarning($"[UIPanelFacade] Panel not found in Global or Local managers: {typeof(T).Name}");
    }

    /// <summary>
    /// 패널 자동 숨김 (Global → Local 순서로 검색)
    /// </summary>
    public static void HidePanel<T>() where T : class, IUIPanel
    {
        // 1. Global에서 검색
        var globalManager = ServiceLocator.Get<GlobalUIPanelManager>();
        if (globalManager != null && globalManager.HasGlobalPanel<T>())
        {
            globalManager.HideGlobalPanel<T>();
            return;
        }

        // 2. Local에서 검색
        var localManager = ServiceLocator.Get<LocalUIPanelManager>();
        if (localManager != null && localManager.HasLocalPanel<T>())
        {
            localManager.HideLocalPanel<T>();
            return;
        }
    }

    /// <summary>
    /// 패널 자동 토글 (Global → Local 순서로 검색)
    /// </summary>
    public static void TogglePanel<T>() where T : UIPanel
    {
        // 1. Global에서 검색
        var globalManager = ServiceLocator.Get<GlobalUIPanelManager>();
        if (globalManager != null && globalManager.HasGlobalPanel<T>())
        {
            globalManager.ToggleGlobalPanel<T>();
            return;
        }

        // 2. Local에서 검색
        var localManager = ServiceLocator.Get<LocalUIPanelManager>();
        if (localManager != null && localManager.HasLocalPanel<T>())
        {
            localManager.ToggleLocalPanel<T>();
            return;
        }
    }

    /// <summary>
    /// 패널 자동 조회 (Global → Local 순서로 검색)
    /// </summary>
    public static T GetPanel<T>() where T : class, IUIPanel
    {
        // 1. Global에서 검색
        var globalManager = ServiceLocator.Get<GlobalUIPanelManager>();
        if (globalManager != null)
        {
            var globalPanel = globalManager.GetGlobalPanel<T>();
            if (globalPanel != null)
                return globalPanel;
        }

        // 2. Local에서 검색
        var localManager = ServiceLocator.Get<LocalUIPanelManager>();
        if (localManager != null)
        {
            var localPanel = localManager.GetLocalPanel<T>();
            if (localPanel != null)
                return localPanel;
        }

        return null;
    }

    /// <summary>
    /// 패널 존재 여부 확인 (Global 또는 Local)
    /// </summary>
    public static bool HasPanel<T>() where T : class, IUIPanel
    {
        return HasGlobalPanel<T>() || HasLocalPanel<T>();
    }

    // ========================================
    // 모든 패널 제어
    // ========================================

    /// <summary>
    /// 모든 패널 숨김 (Global + Local)
    /// </summary>
    public static void HideAllPanels()
    {
        HideAllGlobalPanels();
        HideAllLocalPanels();
    }

    // ========================================
    // Stack Management (Advanced)
    // ========================================

    /// <summary>
    /// 전역 패널을 스택에 푸시하고 표시
    /// </summary>
    public static void PushGlobalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<GlobalUIPanelManager>();
        manager?.PushGlobalPanel<T>();
    }

    /// <summary>
    /// 전역 패널 스택에서 팝
    /// </summary>
    public static void PopGlobalPanel()
    {
        var manager = ServiceLocator.Get<GlobalUIPanelManager>();
        manager?.PopGlobalPanel();
    }

    /// <summary>
    /// 씬 종속 패널을 스택에 푸시하고 표시
    /// </summary>
    public static void PushLocalPanel<T>() where T : class, IUIPanel
    {
        var manager = ServiceLocator.Get<LocalUIPanelManager>();
        manager?.PushLocalPanel<T>();
    }

    /// <summary>
    /// 씬 종속 패널 스택에서 팝
    /// </summary>
    public static void PopLocalPanel()
    {
        var manager = ServiceLocator.Get<LocalUIPanelManager>();
        manager?.PopLocalPanel();
    }

    // ========================================
    // 디버그 및 유틸리티
    // ========================================

    /// <summary>
    /// 전역 UI 시스템 상태 로그
    /// </summary>
    public static void LogGlobalUIStatus()
    {
        var manager = ServiceLocator.Get<GlobalUIPanelManager>();
        if (manager != null)
        {
            manager.LogPanelStatus();
        }
        else
        {
            Debug.LogWarning("[UIPanelFacade] GlobalUIPanelManager not available");
        }
    }

    /// <summary>
    /// 씬 종속 UI 시스템 상태 로그
    /// </summary>
    public static void LogLocalUIStatus()
    {
        var manager = ServiceLocator.Get<LocalUIPanelManager>();
        if (manager != null)
        {
            manager.LogPanelStatus();
        }
        else
        {
            Debug.LogWarning("[UIPanelFacade] LocalUIPanelManager not available");
        }
    }

    /// <summary>
    /// 전체 UI 시스템 상태 로그
    /// </summary>
    public static void LogAllUIStatus()
    {
        Debug.Log("========================================");
        Debug.Log("=== UI Panel System Status ===");
        Debug.Log("========================================");

        LogGlobalUIStatus();
        Debug.Log("----------------------------------------");
        LogLocalUIStatus();

        Debug.Log("========================================");
    }
}
