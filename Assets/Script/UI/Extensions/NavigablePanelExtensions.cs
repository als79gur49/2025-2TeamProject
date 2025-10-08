using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// INavigablePanel 인터페이스에 대한 확장 메서드 모음
/// 이전/다음 버튼의 자동 바인딩 및 탐색 기능 제공
/// </summary>
public static class NavigablePanelExtensions
{
    #region 자동 바인딩

    /// <summary>
    /// INavigablePanel의 모든 버튼 자동 바인딩
    /// </summary>
    /// <param name="panel">탐색 가능한 패널</param>
    /// <param name="onPrevious">이전 버튼 클릭 시 실행될 콜백</param>
    /// <param name="onNext">다음 버튼 클릭 시 실행될 콜백</param>
    public static void AutoBindNavigationButtons(this INavigablePanel panel, Action onPrevious, Action onNext)
    {
        if (panel == null)
        {
            Debug.LogError("[NavigablePanelExtensions] Panel is null");
            return;
        }

        // Previous 버튼 바인딩
        if (panel.PreviousButton != null)
        {
            panel.PreviousButton.onClick.AddListener(() =>
            {
                Debug.Log($"[{GetPanelName(panel)}] Previous button clicked");
                onPrevious?.Invoke();
            });
        }

        // Next 버튼 바인딩
        if (panel.NextButton != null)
        {
            panel.NextButton.onClick.AddListener(() =>
            {
                Debug.Log($"[{GetPanelName(panel)}] Next button clicked");
                onNext?.Invoke();
            });
        }
    }

    /// <summary>
    /// INavigablePanel의 버튼 바인딩 해제
    /// </summary>
    public static void UnbindNavigationButtons(this INavigablePanel panel)
    {
        if (panel == null) return;

        panel.PreviousButton?.onClick.RemoveAllListeners();
        panel.NextButton?.onClick.RemoveAllListeners();
    }

    #endregion

    #region 개별 버튼 바인딩

    /// <summary>
    /// Previous 버튼만 바인딩
    /// </summary>
    public static void BindPreviousButton(this INavigablePanel panel, Action onPrevious)
    {
        if (panel?.PreviousButton != null)
        {
            panel.PreviousButton.onClick.AddListener(() => onPrevious?.Invoke());
        }
    }

    /// <summary>
    /// Next 버튼만 바인딩
    /// </summary>
    public static void BindNextButton(this INavigablePanel panel, Action onNext)
    {
        if (panel?.NextButton != null)
        {
            panel.NextButton.onClick.AddListener(() => onNext?.Invoke());
        }
    }

    #endregion

    #region 버튼 상태 제어

    /// <summary>
    /// 버튼 활성화/비활성화 상태 설정
    /// </summary>
    public static void SetNavigationEnabled(this INavigablePanel panel, bool previousEnabled, bool nextEnabled)
    {
        if (panel == null) return;

        if (panel.PreviousButton != null)
            panel.PreviousButton.interactable = previousEnabled;

        if (panel.NextButton != null)
            panel.NextButton.interactable = nextEnabled;
    }

    /// <summary>
    /// Previous 버튼 활성화/비활성화
    /// </summary>
    public static void SetPreviousButtonEnabled(this INavigablePanel panel, bool enabled)
    {
        if (panel?.PreviousButton != null)
            panel.PreviousButton.interactable = enabled;
    }

    /// <summary>
    /// Next 버튼 활성화/비활성화
    /// </summary>
    public static void SetNextButtonEnabled(this INavigablePanel panel, bool enabled)
    {
        if (panel?.NextButton != null)
            panel.NextButton.interactable = enabled;
    }

    /// <summary>
    /// 버튼 가시성 설정
    /// </summary>
    public static void SetNavigationVisible(this INavigablePanel panel, bool previousVisible, bool nextVisible)
    {
        if (panel == null) return;

        if (panel.PreviousButton != null)
            panel.PreviousButton.gameObject.SetActive(previousVisible);

        if (panel.NextButton != null)
            panel.NextButton.gameObject.SetActive(nextVisible);
    }

    #endregion

    #region 페이지네이션 헬퍼

    /// <summary>
    /// 현재 인덱스 기반으로 버튼 상태 자동 업데이트
    /// </summary>
    /// <param name="panel">탐색 가능한 패널</param>
    /// <param name="currentIndex">현재 인덱스 (0부터 시작)</param>
    /// <param name="totalCount">전체 항목 수</param>
    public static void UpdateNavigationState(this INavigablePanel panel, int currentIndex, int totalCount)
    {
        if (panel == null) return;

        bool hasPrevious = currentIndex > 0;
        bool hasNext = currentIndex < totalCount - 1;

        panel.SetNavigationEnabled(hasPrevious, hasNext);
    }

    /// <summary>
    /// 페이지 정보 기반으로 버튼 상태 자동 업데이트
    /// </summary>
    /// <param name="panel">탐색 가능한 패널</param>
    /// <param name="currentPage">현재 페이지 (1부터 시작)</param>
    /// <param name="totalPages">전체 페이지 수</param>
    public static void UpdatePaginationState(this INavigablePanel panel, int currentPage, int totalPages)
    {
        if (panel == null) return;

        bool hasPrevious = currentPage > 1;
        bool hasNext = currentPage < totalPages;

        panel.SetNavigationEnabled(hasPrevious, hasNext);
    }

    #endregion

    #region 버튼 텍스트 설정

    /// <summary>
    /// 버튼 텍스트 설정 (TMPro 지원)
    /// </summary>
    public static void SetNavigationTexts(this INavigablePanel panel, string previousText, string nextText)
    {
        if (panel == null) return;

        // Previous 버튼 텍스트
        if (panel.PreviousButton != null && !string.IsNullOrEmpty(previousText))
        {
            var tmpText = panel.PreviousButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
                tmpText.text = previousText;
            else
            {
                var text = panel.PreviousButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (text != null)
                    text.text = previousText;
            }
        }

        // Next 버튼 텍스트
        if (panel.NextButton != null && !string.IsNullOrEmpty(nextText))
        {
            var tmpText = panel.NextButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
                tmpText.text = nextText;
            else
            {
                var text = panel.NextButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (text != null)
                    text.text = nextText;
            }
        }
    }

    #endregion

    #region 체이닝 메서드

    /// <summary>
    /// 자동 바인딩 후 패널 자신을 반환 (체이닝용)
    /// </summary>
    public static T WithAutoBindNavigationButtons<T>(this T panel, Action onPrevious, Action onNext)
        where T : class, INavigablePanel
    {
        panel.AutoBindNavigationButtons(onPrevious, onNext);
        return panel;
    }

    /// <summary>
    /// 버튼 텍스트 설정 후 패널 반환 (체이닝용)
    /// </summary>
    public static T WithNavigationTexts<T>(this T panel, string previousText, string nextText)
        where T : class, INavigablePanel
    {
        panel.SetNavigationTexts(previousText, nextText);
        return panel;
    }

    /// <summary>
    /// 버튼 색상 설정 후 패널 반환 (체이닝용)
    /// </summary>
    public static T WithNavigationColors<T>(this T panel, Color previousColor, Color nextColor)
        where T : class, INavigablePanel
    {
        if (panel?.PreviousButton != null)
        {
            var image = panel.PreviousButton.GetComponent<Image>();
            if (image != null) image.color = previousColor;
        }

        if (panel?.NextButton != null)
        {
            var image = panel.NextButton.GetComponent<Image>();
            if (image != null) image.color = nextColor;
        }

        return panel;
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 패널 이름 가져오기
    /// </summary>
    private static string GetPanelName(INavigablePanel panel)
    {
        if (panel == null) return "Unknown";

        if (panel is IUIPanel uiPanel)
            return uiPanel.PanelID;

        return panel.GetType().Name;
    }

    /// <summary>
    /// 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasNavigationButtons(this INavigablePanel panel)
    {
        return panel != null && (panel.PreviousButton != null || panel.NextButton != null);
    }

    /// <summary>
    /// Previous 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasPreviousButton(this INavigablePanel panel)
    {
        return panel?.PreviousButton != null;
    }

    /// <summary>
    /// Next 버튼이 할당되었는지 확인
    /// </summary>
    public static bool HasNextButton(this INavigablePanel panel)
    {
        return panel?.NextButton != null;
    }

    #endregion
}
