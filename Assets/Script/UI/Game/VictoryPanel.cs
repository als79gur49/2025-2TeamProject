using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 플레이어 승리 패널
/// GameUICoordinator에 의해 표시됨 (게임 로직으로부터 분리)
/// IGameResultPanel을 구현하여 UIPanelManager의 자동 바인딩 지원
///
/// Architecture:
/// - Pure UI component with no game service dependencies
/// - Displayed by GameUICoordinator in response to game events
/// - Focuses solely on UI display and user interaction
/// </summary>
public class VictoryPanel : UIPanel, IGameResultPanel
{
    [Header("Navigation Buttons (IGameResultPanel)")]
    [SerializeField] private Button nextLevelButton;  // 다음 레벨로 이동 (Primary Action)
    [SerializeField] private Button mainMenuButton;   // 메인 메뉴로 이동 (Secondary Action)

    // IGameResultPanel 구현
    public Button PrimaryActionButton => nextLevelButton;
    public Button SecondaryActionButton => mainMenuButton;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI victoryMessageText;  // 승리 메시지 텍스트

    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "SampleScene";  // 메인 메뉴 씬 이름
    [SerializeField] private string nextLevelSceneName = "SampleScene"; // 다음 레벨 씬 이름

    [Header("Settings")]
    [SerializeField] private bool pauseGameOnShow = true;  // 패널 표시 시 게임 일시정지

    #region UIPanel 오버라이드

    protected override void OnInitialize()
    {
        base.OnInitialize();

        // 버튼 바인딩은 UIPanelManager가 자동으로 처리
        // (IGameResultPanel 구현으로 인해 자동 바인딩됨)
    }

    protected override void OnShowPanel()
    {
        base.OnShowPanel();

        // 승리 메시지 표시
        if (victoryMessageText != null)
        {
            victoryMessageText.text = "Victory!";
        }

        // 게임 일시정지 (옵션)
        if (pauseGameOnShow)
        {
            Time.timeScale = 0f;
        }

        Debug.Log("[VictoryPanel] Victory panel displayed");
    }

    protected override void OnHidePanel()
    {
        // 게임 재개
        if (pauseGameOnShow)
        {
            Time.timeScale = 1f;
        }

        base.OnHidePanel();
    }

    protected override void OnCleanup()
    {
        // 버튼 정리는 UIPanelManager가 자동으로 처리
        // (IGameResultPanel 구현으로 인해 자동 정리됨)

        base.OnCleanup();
    }

    #endregion

    #region IGameResultPanel 구현

    /// <summary>
    /// Primary Action: 다음 레벨로 이동
    /// UIPanelManager가 자동으로 nextLevelButton에 바인딩
    /// </summary>
    public void OnPrimaryAction()
    {
        Debug.Log($"[VictoryPanel] Loading next level: {nextLevelSceneName}");

        // 게임 재개 (씬 전환 전)
        Time.timeScale = 1f;

        // 다음 레벨 씬 로드
        SceneManager.LoadScene(nextLevelSceneName);
    }

    /// <summary>
    /// Secondary Action: 메인 메뉴로 이동
    /// UIPanelManager가 자동으로 mainMenuButton에 바인딩
    /// </summary>
    public void OnSecondaryAction()
    {
        Debug.Log($"[VictoryPanel] Loading main menu: {mainMenuSceneName}");

        // 게임 재개 (씬 전환 전)
        Time.timeScale = 1f;

        // 메인 메뉴 씬 로드
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 승리 메시지 커스터마이징
    /// </summary>
    public void SetVictoryMessage(string message)
    {
        if (victoryMessageText != null)
        {
            victoryMessageText.text = message;
        }
    }

    /// <summary>
    /// 다음 레벨 씬 이름 설정
    /// </summary>
    public void SetNextLevelScene(string sceneName)
    {
        nextLevelSceneName = sceneName;
    }

    #endregion
}
