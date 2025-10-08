using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// IDialogPanel 인터페이스에 대한 확장 메서드 모음
/// 확인/취소 버튼의 자동 바인딩 및 다이얼로그 관리 기능 제공
/// </summary>
public static class DialogPanelExtensions
{
    #region 자동 바인딩

    /// <summary>
    /// IDialogPanel의 모든 버튼 자동 바인딩
    /// </summary>
    /// <param name="dialog">다이얼로그 패널</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 실행될 콜백</param>
    /// <param name="onCancel">취소 버튼 클릭 시 실행될 콜백</param>
    public static void AutoBindDialogButtons(this IDialogPanel dialog, Action onConfirm = null, Action onCancel = null)
    {
        if (dialog == null)
        {
            Debug.LogError("[DialogPanelExtensions] Dialog is null");
            return;
        }

        // Confirm 버튼 바인딩
        if (dialog.ConfirmButton != null)
        {
            dialog.ConfirmButton.onClick.AddListener(() =>
            {
                Debug.Log($"[{GetDialogName(dialog)}] Confirm button clicked");
                onConfirm?.Invoke();
                (dialog as IUIPanel)?.OnHide();
            });
        }

        // Cancel 버튼 바인딩
        if (dialog.CancelButton != null)
        {
            dialog.CancelButton.onClick.AddListener(() =>
            {
                Debug.Log($"[{GetDialogName(dialog)}] Cancel button clicked");
                onCancel?.Invoke();
                (dialog as IUIPanel)?.OnHide();
            });
        }
    }

    /// <summary>
    /// IDialogPanel의 버튼 바인딩 해제
    /// </summary>
    public static void UnbindDialogButtons(this IDialogPanel dialog)
    {
        if (dialog == null) return;

        dialog.ConfirmButton?.onClick.RemoveAllListeners();
        dialog.CancelButton?.onClick.RemoveAllListeners();
    }

    #endregion

    #region 개별 버튼 바인딩

    /// <summary>
    /// Confirm 버튼만 바인딩
    /// </summary>
    public static void BindConfirmButton(this IDialogPanel dialog, Action onConfirm, bool autoClose = true)
    {
        if (dialog?.ConfirmButton != null)
        {
            dialog.ConfirmButton.onClick.AddListener(() =>
            {
                onConfirm?.Invoke();
                if (autoClose)
                {
                    (dialog as IUIPanel)?.OnHide();
                }
            });
        }
    }

    /// <summary>
    /// Cancel 버튼만 바인딩
    /// </summary>
    public static void BindCancelButton(this IDialogPanel dialog, Action onCancel, bool autoClose = true)
    {
        if (dialog?.CancelButton != null)
        {
            dialog.CancelButton.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                if (autoClose)
                {
                    (dialog as IUIPanel)?.OnHide();
                }
            });
        }
    }

    #endregion

    #region 버튼 상태 제어

    /// <summary>
    /// 버튼 활성화/비활성화 상태 설정
    /// </summary>
    public static void SetButtonsEnabled(this IDialogPanel dialog, bool confirmEnabled, bool cancelEnabled)
    {
        if (dialog == null) return;

        if (dialog.ConfirmButton != null)
            dialog.ConfirmButton.interactable = confirmEnabled;

        if (dialog.CancelButton != null)
            dialog.CancelButton.interactable = cancelEnabled;
    }

    /// <summary>
    /// Confirm 버튼 활성화/비활성화
    /// </summary>
    public static void SetConfirmButtonEnabled(this IDialogPanel dialog, bool enabled)
    {
        if (dialog?.ConfirmButton != null)
            dialog.ConfirmButton.interactable = enabled;
    }

    /// <summary>
    /// Cancel 버튼 활성화/비활성화
    /// </summary>
    public static void SetCancelButtonEnabled(this IDialogPanel dialog, bool enabled)
    {
        if (dialog?.CancelButton != null)
            dialog.CancelButton.interactable = enabled;
    }

    /// <summary>
    /// 버튼 가시성 설정
    /// </summary>
    public static void SetButtonsVisible(this IDialogPanel dialog, bool confirmVisible, bool cancelVisible)
    {
        if (dialog == null) return;

        if (dialog.ConfirmButton != null)
            dialog.ConfirmButton.gameObject.SetActive(confirmVisible);

        if (dialog.CancelButton != null)
            dialog.CancelButton.gameObject.SetActive(cancelVisible);
    }

    #endregion

    #region 버튼 텍스트 설정

    /// <summary>
    /// 버튼 텍스트 설정 (TMPro 지원)
    /// </summary>
    public static void SetButtonTexts(this IDialogPanel dialog, string confirmText, string cancelText)
    {
        if (dialog == null) return;

        // Confirm 버튼 텍스트
        if (dialog.ConfirmButton != null && !string.IsNullOrEmpty(confirmText))
        {
            var tmpText = dialog.ConfirmButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
                tmpText.text = confirmText;
            else
            {
                var text = dialog.ConfirmButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (text != null)
                    text.text = confirmText;
            }
        }

        // Cancel 버튼 텍스트
        if (dialog.CancelButton != null && !string.IsNullOrEmpty(cancelText))
        {
            var tmpText = dialog.CancelButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
                tmpText.text = cancelText;
            else
            {
                var text = dialog.CancelButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (text != null)
                    text.text = cancelText;
            }
        }
    }

    #endregion

    #region 체이닝 메서드

    /// <summary>
    /// 자동 바인딩 후 다이얼로그 자신을 반환 (체이닝용)
    /// </summary>
    public static T WithAutoBindDialogButtons<T>(this T dialog, Action onConfirm = null, Action onCancel = null)
        where T : class, IDialogPanel
    {
        dialog.AutoBindDialogButtons(onConfirm, onCancel);
        return dialog;
    }

    /// <summary>
    /// 버튼 텍스트 설정 후 다이얼로그 반환 (체이닝용)
    /// </summary>
    public static T WithButtonTexts<T>(this T dialog, string confirmText, string cancelText)
        where T : class, IDialogPanel
    {
        dialog.SetButtonTexts(confirmText, cancelText);
        return dialog;
    }

    /// <summary>
    /// 버튼 색상 설정 후 다이얼로그 반환 (체이닝용)
    /// </summary>
    public static T WithButtonColors<T>(this T dialog, Color confirmColor, Color cancelColor)
        where T : class, IDialogPanel
    {
        if (dialog?.ConfirmButton != null)
        {
            var image = dialog.ConfirmButton.GetComponent<Image>();
            if (image != null) image.color = confirmColor;
        }

        if (dialog?.CancelButton != null)
        {
            var image = dialog.CancelButton.GetComponent<Image>();
            if (image != null) image.color = cancelColor;
        }

        return dialog;
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 다이얼로그 이름 가져오기
    /// </summary>
    private static string GetDialogName(IDialogPanel dialog)
    {
        if (dialog == null) return "Unknown";

        if (dialog is IUIPanel uiPanel)
            return uiPanel.PanelID;

        return dialog.GetType().Name;
    }

    /// <summary>
    /// 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasButtons(this IDialogPanel dialog)
    {
        return dialog != null && (dialog.ConfirmButton != null || dialog.CancelButton != null);
    }

    /// <summary>
    /// Confirm 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasConfirmButton(this IDialogPanel dialog)
    {
        return dialog?.ConfirmButton != null;
    }

    /// <summary>
    /// Cancel 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasCancelButton(this IDialogPanel dialog)
    {
        return dialog?.CancelButton != null;
    }

    #endregion
}
