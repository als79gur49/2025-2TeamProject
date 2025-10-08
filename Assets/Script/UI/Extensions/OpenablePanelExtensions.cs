using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// IOpenablePanel 인터페이스에 대한 확장 메서드 모음
/// 열기/닫기 버튼의 자동 바인딩 및 토글 기능 제공
/// </summary>
public static class OpenablePanelExtensions
{
    #region 자동 바인딩

    /// <summary>
    /// IOpenablePanel의 모든 버튼 자동 바인딩
    /// OpenButton -> OnShow(), CloseButton -> OnHide()
    /// </summary>
    public static void AutoBindOpenCloseButtons(this IOpenablePanel panel)
    {
        if (panel == null)
        {
            Debug.LogError("[OpenablePanelExtensions] Panel is null");
            return;
        }

        var uiPanel = panel as IUIPanel;
        if (uiPanel == null)
        {
            Debug.LogError("[OpenablePanelExtensions] Panel does not implement IUIPanel");
            return;
        }

        // Open 버튼 바인딩
        if (panel.OpenButton != null)
        {
            panel.OpenButton.onClick.AddListener(() =>
            {
                Debug.Log($"[{GetPanelName(panel)}] Open button clicked");
                uiPanel.OnShow();
            });
        }

        // Close 버튼 바인딩
        if (panel.CloseButton != null)
        {
            panel.CloseButton.onClick.AddListener(() =>
            {
                Debug.Log($"[{GetPanelName(panel)}] Close button clicked");
                uiPanel.OnHide();
            });
        }
    }

    /// <summary>
    /// IOpenablePanel의 버튼 바인딩 해제
    /// </summary>
    public static void UnbindOpenCloseButtons(this IOpenablePanel panel)
    {
        if (panel == null) return;

        panel.OpenButton?.onClick.RemoveAllListeners();
        panel.CloseButton?.onClick.RemoveAllListeners();
    }

    #endregion

    #region 개별 버튼 바인딩

    /// <summary>
    /// Open 버튼만 바인딩
    /// </summary>
    public static void BindOpenButton(this IOpenablePanel panel)
    {
        if (panel?.OpenButton != null)
        {
            panel.OpenButton.onClick.AddListener(() => (panel as IUIPanel)?.OnShow());
        }
    }

    /// <summary>
    /// Open 버튼에 커스텀 액션 바인딩
    /// </summary>
    public static void BindOpenButton(this IOpenablePanel panel, Action customAction)
    {
        if (panel?.OpenButton != null)
        {
            panel.OpenButton.onClick.AddListener(() =>
            {
                customAction?.Invoke();
                (panel as IUIPanel)?.OnShow();
            });
        }
    }

    /// <summary>
    /// Close 버튼만 바인딩
    /// </summary>
    public static void BindCloseButton(this IOpenablePanel panel)
    {
        if (panel?.CloseButton != null)
        {
            panel.CloseButton.onClick.AddListener(() => (panel as IUIPanel)?.OnHide());
        }
    }

    /// <summary>
    /// Close 버튼에 커스텀 액션 바인딩 (닫기 전에 실행)
    /// </summary>
    public static void BindCloseButton(this IOpenablePanel panel, Action customAction)
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

    #endregion

    #region 토글 기능

    /// <summary>
    /// Open 버튼을 토글 버튼으로 바인딩 (열림/닫힘 토글)
    /// </summary>
    public static void BindOpenButtonAsToggle(this IOpenablePanel panel)
    {
        if (panel?.OpenButton == null) return;

        var uiPanel = panel as IUIPanel;
        if (uiPanel == null) return;

        panel.OpenButton.onClick.AddListener(() =>
        {
            if (uiPanel.IsActive)
            {
                Debug.Log($"[{GetPanelName(panel)}] Toggle button clicked - Closing");
                uiPanel.OnHide();
            }
            else
            {
                Debug.Log($"[{GetPanelName(panel)}] Toggle button clicked - Opening");
                uiPanel.OnShow();
            }
        });
    }

    /// <summary>
    /// 패널 토글 (현재 상태의 반대로 전환)
    /// </summary>
    public static void Toggle(this IOpenablePanel panel)
    {
        var uiPanel = panel as IUIPanel;
        if (uiPanel == null) return;

        if (uiPanel.IsActive)
            uiPanel.OnHide();
        else
            uiPanel.OnShow();
    }

    #endregion

    #region 버튼 상태 제어

    /// <summary>
    /// 버튼 활성화/비활성화 상태 설정
    /// </summary>
    public static void SetButtonsEnabled(this IOpenablePanel panel, bool openEnabled, bool closeEnabled)
    {
        if (panel == null) return;

        if (panel.OpenButton != null)
            panel.OpenButton.interactable = openEnabled;

        if (panel.CloseButton != null)
            panel.CloseButton.interactable = closeEnabled;
    }

    /// <summary>
    /// Open 버튼 활성화/비활성화
    /// </summary>
    public static void SetOpenButtonEnabled(this IOpenablePanel panel, bool enabled)
    {
        if (panel?.OpenButton != null)
            panel.OpenButton.interactable = enabled;
    }

    /// <summary>
    /// Close 버튼 활성화/비활성화
    /// </summary>
    public static void SetCloseButtonEnabled(this IOpenablePanel panel, bool enabled)
    {
        if (panel?.CloseButton != null)
            panel.CloseButton.interactable = enabled;
    }

    /// <summary>
    /// 버튼 가시성 설정
    /// </summary>
    public static void SetButtonsVisible(this IOpenablePanel panel, bool openVisible, bool closeVisible)
    {
        if (panel == null) return;

        if (panel.OpenButton != null)
            panel.OpenButton.gameObject.SetActive(openVisible);

        if (panel.CloseButton != null)
            panel.CloseButton.gameObject.SetActive(closeVisible);
    }

    #endregion

    #region 버튼 텍스트 설정

    /// <summary>
    /// 버튼 텍스트 설정 (TMPro 지원)
    /// </summary>
    public static void SetButtonTexts(this IOpenablePanel panel, string openText, string closeText)
    {
        if (panel == null) return;

        // Open 버튼 텍스트
        if (panel.OpenButton != null && !string.IsNullOrEmpty(openText))
        {
            var tmpText = panel.OpenButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
                tmpText.text = openText;
            else
            {
                var text = panel.OpenButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (text != null)
                    text.text = openText;
            }
        }

        // Close 버튼 텍스트
        if (panel.CloseButton != null && !string.IsNullOrEmpty(closeText))
        {
            var tmpText = panel.CloseButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
                tmpText.text = closeText;
            else
            {
                var text = panel.CloseButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (text != null)
                    text.text = closeText;
            }
        }
    }

    #endregion

    #region 체이닝 메서드

    /// <summary>
    /// 자동 바인딩 후 패널 자신을 반환 (체이닝용)
    /// </summary>
    public static T WithAutoBindOpenCloseButtons<T>(this T panel) where T : class, IOpenablePanel
    {
        panel.AutoBindOpenCloseButtons();
        return panel;
    }

    /// <summary>
    /// 토글 버튼으로 바인딩 후 패널 반환 (체이닝용)
    /// </summary>
    public static T WithToggleButton<T>(this T panel) where T : class, IOpenablePanel
    {
        panel.BindOpenButtonAsToggle();
        panel.BindCloseButton();
        return panel;
    }

    /// <summary>
    /// 커스텀 Close 액션 설정 후 패널 반환 (체이닝용)
    /// </summary>
    public static T WithCustomCloseAction<T>(this T panel, Action customAction) where T : class, IOpenablePanel
    {
        panel.BindCloseButton(customAction);
        return panel;
    }

    /// <summary>
    /// 버튼 텍스트 설정 후 패널 반환 (체이닝용)
    /// </summary>
    public static T WithButtonTexts<T>(this T panel, string openText, string closeText) where T : class, IOpenablePanel
    {
        panel.SetButtonTexts(openText, closeText);
        return panel;
    }

    /// <summary>
    /// 버튼 색상 설정 후 패널 반환 (체이닝용)
    /// </summary>
    public static T WithButtonColors<T>(this T panel, Color openColor, Color closeColor) where T : class, IOpenablePanel
    {
        if (panel?.OpenButton != null)
        {
            var image = panel.OpenButton.GetComponent<Image>();
            if (image != null) image.color = openColor;
        }

        if (panel?.CloseButton != null)
        {
            var image = panel.CloseButton.GetComponent<Image>();
            if (image != null) image.color = closeColor;
        }

        return panel;
    }

    #endregion

    #region UIPanelManager 연동

    /// <summary>
    /// UIPanelManager를 통해 패널 열기
    /// </summary>
    public static void ShowViaManager<T>(this T panel) where T : class, IOpenablePanel, IUIPanel
    {
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.ShowPanel<T>();
        }
        else
        {
            panel.OnShow();
        }
    }

    /// <summary>
    /// UIPanelManager를 통해 패널 닫기
    /// </summary>
    public static void HideViaManager<T>(this T panel) where T : class, IOpenablePanel, IUIPanel
    {
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.HidePanel<T>();
        }
        else
        {
            panel.OnHide();
        }
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 패널 이름 가져오기
    /// </summary>
    private static string GetPanelName(IOpenablePanel panel)
    {
        if (panel == null) return "Unknown";

        if (panel is IUIPanel uiPanel)
            return uiPanel.PanelID;

        return panel.GetType().Name;
    }

    /// <summary>
    /// 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasButtons(this IOpenablePanel panel)
    {
        return panel != null && (panel.OpenButton != null || panel.CloseButton != null);
    }

    /// <summary>
    /// Open 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasOpenButton(this IOpenablePanel panel)
    {
        return panel?.OpenButton != null;
    }

    /// <summary>
    /// Close 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasCloseButton(this IOpenablePanel panel)
    {
        return panel?.CloseButton != null;
    }

    #endregion
}
