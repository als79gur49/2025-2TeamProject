using System;

/// <summary>
/// 모든 UI 패널이 구현해야 하는 기본 인터페이스
/// </summary>
public interface IUIPanel
{
    /// <summary>
    /// 패널이 현재 활성화 상태인지 여부
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 패널의 우선순위
    /// </summary>
    UIPanelPriority Priority { get; }

    /// <summary>
    /// 패널의 현재 상태
    /// </summary>
    UIPanelState CurrentState { get; }

    // --- 이벤트 ---
    event Action<IUIPanel> OnPanelShown;
    event Action<IUIPanel> OnPanelHidden;

    // --- 생명주기 메서드 ---
    void Initialize();
    void OnShow();
    void OnHide();
    void Cleanup();
}

// --- Enums (참고용으로 추가) ---
public enum UIPanelPriority
{
    Normal,
    High,
    AlwaysOnTop
}

public enum UIPanelState
{
    Inactive,
    Initializing,
    Showing,
    Active,
    Hiding
}