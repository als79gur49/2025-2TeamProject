using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 플레이어 패배 패널
/// GameUICoordinator에 의해 표시됨 (게임 로직으로부터 분리)
/// IGameResultPanel을 구현하여 UIPanelManager의 자동 바인딩 지원
///
/// Architecture:
/// - Pure UI component with no game service dependencies
/// - Displayed by GameUICoordinator in response to game events
/// - Focuses solely on UI display and user interaction
/// </summary>
public class DefeatPanel : UIPanel, IGameResultPanel
{
    [Header("Navigation Buttons (IGameResultPanel)")]
    [SerializeField] private Button restartButton;   // 현재 레벨 재시작 (Primary Action)
    [SerializeField] private Button mainMenuButton;  // 메인 메뉴로 이동 (Secondary Action)

    // IGameResultPanel 구현
    public Button PrimaryActionButton => restartButton;
    public Button SecondaryActionButton => mainMenuButton;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI defeatMessageText;  // 패배 메시지 텍스트

    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "SampleScene";  // 메인 메뉴 씬 이름

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

        // 패배 메시지 표시
        if (defeatMessageText != null)
        {
            defeatMessageText.text = "Defeat!";
        }

        // 게임 일시정지 (옵션)
        if (pauseGameOnShow)
        {
            Time.timeScale = 0f;
        }

        Debug.Log("[DefeatPanel] Defeat panel displayed");
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
    /// Primary Action: 현재 씬 재시작
    /// UIPanelManager가 자동으로 restartButton에 바인딩
    /// </summary>
    public void OnPrimaryAction()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[DefeatPanel] Restarting current scene: {currentSceneName}");

        // 게임 재개 (씬 전환 전)
        Time.timeScale = 1f;

        // 현재 씬 재로드
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// Secondary Action: 메인 메뉴로 이동
    /// UIPanelManager가 자동으로 mainMenuButton에 바인딩
    /// </summary>
    public void OnSecondaryAction()
    {
        Debug.Log($"[DefeatPanel] Loading main menu: {mainMenuSceneName}");

        // 게임 재개 (씬 전환 전)
        Time.timeScale = 1f;

        // 메인 메뉴 씬 로드
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 패배 메시지 커스터마이징
    /// </summary>
    public void SetDefeatMessage(string message)
    {
        if (defeatMessageText != null)
        {
            defeatMessageText.text = message;
        }
    }

    /// <summary>
    /// 메인 메뉴 씬 이름 설정
    /// </summary>
    public void SetMainMenuScene(string sceneName)
    {
        mainMenuSceneName = sceneName;
    }

    #endregion
}
