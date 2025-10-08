using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// IButtonedPanel 인터페이스에 대한 확장 메서드 모음
/// 닫기/뒤로가기 버튼의 자동 바인딩 및 유틸리티 기능 제공
/// </summary>
public static class ButtonedPanelExtensions
{
    #region 자동 바인딩

    /// <summary>
    /// IButtonedPanel의 모든 버튼 자동 바인딩
    /// CloseButton -> OnHide(), BackButton -> PopPanel()
    /// </summary>
    public static void AutoBindButtons(this IButtonedPanel panel)
    {
        if (panel == null)
        {
            Debug.LogError("[ButtonedPanelExtensions] Panel is null");
            return;
        }

        // Close 버튼 바인딩
        if (panel.CloseButton != null)
        {
            panel.CloseButton.onClick.AddListener(() =>
            {
                if (UIPanelManager.Instance.HasPanel<SettingsPanel>())
                {
                    Debug.Log($"[{GetPanelName(panel)}] Close button clicked");
                }
                (panel as IUIPanel)?.OnHide();
            });
        }

        // Back 버튼 바인딩
        if (panel.BackButton != null)
        {
            panel.BackButton.onClick.AddListener(() =>
            {
                Debug.Log($"[{GetPanelName(panel)}] Back button clicked");

                if (UIPanelManager.Instance != null)
                {
                    UIPanelManager.Instance.PopPanel();
                }
                else
                {
                    // Fallback: 그냥 패널 숨김
                    (panel as IUIPanel)?.OnHide();
                }
            });
        }
    }

    /// <summary>
    /// IButtonedPanel의 버튼 바인딩 해제
    /// </summary>
    public static void UnbindButtons(this IButtonedPanel panel)
    {
        if (panel == null) return;

        panel.CloseButton?.onClick.RemoveAllListeners();
        panel.BackButton?.onClick.RemoveAllListeners();
    }

    #endregion

    #region 개별 버튼 바인딩

    /// <summary>
    /// Close 버튼만 바인딩
    /// </summary>
    public static void BindCloseButton(this IButtonedPanel panel)
    {
        if (panel?.CloseButton != null)
        {
            panel.CloseButton.onClick.AddListener(() => (panel as IUIPanel)?.OnHide());
        }
    }

    /// <summary>
    /// Close 버튼에 커스텀 액션 바인딩
    /// </summary>
    public static void BindCloseButton(this IButtonedPanel panel, Action customAction)
    {
        if (panel?.CloseButton != null)
        {
            panel.CloseButton.onClick.AddListener(() =>
            {
                customAction?.Invoke();
                (panel as IUIPanel)?.OnHide();
            });
        }
    }

    /// <summary>
    /// Back 버튼만 바인딩
    /// </summary>
    public static void BindBackButton(this IButtonedPanel panel)
    {
        if (panel?.BackButton != null)
        {
            panel.BackButton.onClick.AddListener(() =>
            {
                if (UIPanelManager.Instance != null)
                {
                    UIPanelManager.Instance.PopPanel();
                }
            });
        }
    }

    /// <summary>
    /// Back 버튼에 커스텀 액션 바인딩
    /// </summary>
    public static void BindBackButton(this IButtonedPanel panel, Action customAction)
    {
        if (panel?.BackButton != null)
        {
            panel.BackButton.onClick.AddListener(() => customAction?.Invoke());
        }
    }

    #endregion

    #region 버튼 상태 제어

    /// <summary>
    /// 버튼 활성화/비활성화 상태 설정
    /// </summary>
    public static void SetButtonsEnabled(this IButtonedPanel panel, bool closeEnabled, bool backEnabled)
    {
        if (panel == null) return;

        if (panel.CloseButton != null)
            panel.CloseButton.interactable = closeEnabled;

        if (panel.BackButton != null)
            panel.BackButton.interactable = backEnabled;
    }

    /// <summary>
    /// Close 버튼 활성화/비활성화
    /// </summary>
    public static void SetCloseButtonEnabled(this IButtonedPanel panel, bool enabled)
    {
        if (panel?.CloseButton != null)
            panel.CloseButton.interactable = enabled;
    }

    /// <summary>
    /// Back 버튼 활성화/비활성화
    /// </summary>
    public static void SetBackButtonEnabled(this IButtonedPanel panel, bool enabled)
    {
        if (panel?.BackButton != null)
            panel.BackButton.interactable = enabled;
    }

    /// <summary>
    /// 버튼 가시성 설정
    /// </summary>
    public static void SetButtonsVisible(this IButtonedPanel panel, bool closeVisible, bool backVisible)
    {
        if (panel == null) return;

        if (panel.CloseButton != null)
            panel.CloseButton.gameObject.SetActive(closeVisible);

        if (panel.BackButton != null)
            panel.BackButton.gameObject.SetActive(backVisible);
    }

    #endregion

    #region 체이닝 메서드

    /// <summary>
    /// 자동 바인딩 후 패널 자신을 반환 (체이닝용)
    /// </summary>
    public static T WithAutoBindButtons<T>(this T panel) where T : class, IButtonedPanel
    {
        panel.AutoBindButtons();
        return panel;
    }

    /// <summary>
    /// 커스텀 Close 액션 설정 후 패널 반환 (체이닝용)
    /// </summary>
    public static T WithCustomCloseAction<T>(this T panel, Action customAction) where T : class, IButtonedPanel
    {
        panel.BindCloseButton(customAction);
        return panel;
    }

    /// <summary>
    /// 버튼 스타일 설정 후 패널 반환 (체이닝용)
    /// </summary>
    public static T WithButtonStyle<T>(this T panel, Color closeColor, Color? backColor = null) where T : class, IButtonedPanel
    {
        if (panel?.CloseButton != null)
        {
            var image = panel.CloseButton.GetComponent<Image>();
            if (image != null) image.color = closeColor;
        }

        if (panel?.BackButton != null && backColor.HasValue)
        {
            var image = panel.BackButton.GetComponent<Image>();
            if (image != null) image.color = backColor.Value;
        }

        return panel;
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 패널 이름 가져오기
    /// </summary>
    private static string GetPanelName(IButtonedPanel panel)
    {
        if (panel == null) return "Unknown";

        if (panel is IUIPanel uiPanel)
            return uiPanel.PanelID;

        return panel.GetType().Name;
    }

    /// <summary>
    /// 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasButtons(this IButtonedPanel panel)
    {
        return panel != null && (panel.CloseButton != null || panel.BackButton != null);
    }

    /// <summary>
    /// Close 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasCloseButton(this IButtonedPanel panel)
    {
        return panel?.CloseButton != null;
    }

    /// <summary>
    /// Back 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasBackButton(this IButtonedPanel panel)
    {
        return panel?.BackButton != null;
    }

    #endregion
}
