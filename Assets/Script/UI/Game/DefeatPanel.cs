using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Game.SceneManagement;
using Game.Services;
using Game.Core;

/// <summary>
/// 플레이어 패배 패널
/// GameUICoordinator에 의해 표시됨 (게임 로직으로부터 분리)
/// IGameResultPanel을 구현하여 UIPanelManager의 자동 바인딩 지원
///
/// Architecture:
/// - Uses ServiceLocator for SceneTransitionController access
/// - Uses SceneData ScriptableObjects for type-safe scene references
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
    [SerializeField] private SceneData mainMenuScene;  // 메인 메뉴 씬 데이터

    [Header("Settings")]
    [SerializeField] private bool pauseGameOnShow = true;  // 패널 표시 시 게임 일시정지

    // Dependencies
    private ISceneTransitionController sceneTransitionController;

    #region Initialization

    /// <summary>
    /// 의존성 없는 초기화 (Awake에서 호출됨)
    /// UI 컴포넌트 검증
    /// </summary>
    protected override void OnInitializeSelf()
    {
        base.OnInitializeSelf();

        // 버튼 바인딩은 UIPanelManager가 자동으로 처리
        // (IGameResultPanel 구현으로 인해 자동 바인딩됨)

        // UI 컴포넌트 검증
        ValidateReferences();

        Debug.Log("[DefeatPanel] Self-initialized successfully (Awake)");
    }

    /// <summary>
    /// 의존성 있는 초기화 (Start에서 호출됨)
    /// ServiceLocator에서 전역 서비스 가져오기
    /// </summary>
    protected override void OnInitializeWithDependencies()
    {
        base.OnInitializeWithDependencies();
        
        // ServiceLocator에서 SceneTransitionController 가져오기
        sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();

        if (sceneTransitionController == null)
        {
            Debug.LogError("[DefeatPanel] ISceneTransitionController not found in ServiceLocator! " +
                          "Ensure SceneTransitionController is registered in Bootstrap scene.");
        }

        Debug.Log("[DefeatPanel] Dependency initialization complete (Start)");
    }

    /// <summary>
    /// 필수 참조 검증
    /// </summary>
    private void ValidateReferences()
    {
        if (restartButton == null)
            Debug.LogWarning("[DefeatPanel] Restart Button not assigned!");

        if (mainMenuButton == null)
            Debug.LogWarning("[DefeatPanel] Main Menu Button not assigned!");

        if (mainMenuScene == null)
            Debug.LogWarning("[DefeatPanel] Main Menu Scene Data not assigned!");
    }

    #endregion

    #region UIPanel 오버라이드

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
        // 안전하게 ServiceLocator에서 다시 가져오기 (null일 경우 재시도)
        if (sceneTransitionController == null)
        {
            sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();
        }

        if (sceneTransitionController == null)
        {
            Debug.LogError("[DefeatPanel] SceneTransitionController not available in ServiceLocator!");
            return;
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[DefeatPanel] Restarting current scene: {currentSceneName}");

        // 게임 재개 (씬 전환 전)
        Time.timeScale = 1f;

        // ⚠️ Note: 현재 씬을 재시작하기 위해 씬 이름을 사용
        // 향후 개선: SceneTransitionController에 현재 SceneData 추적 기능 추가 고려
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// Secondary Action: 메인 메뉴로 이동
    /// UIPanelManager가 자동으로 mainMenuButton에 바인딩
    /// </summary>
    public void OnSecondaryAction()
    {
        if (mainMenuScene == null)
        {
            Debug.LogError("[DefeatPanel] Main Menu Scene Data is not assigned!");
            return;
        }

        // 안전하게 ServiceLocator에서 다시 가져오기 (null일 경우 재시도)
        if (sceneTransitionController == null)
        {
            sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();
        }

        if (sceneTransitionController == null)
        {
            Debug.LogError("[DefeatPanel] SceneTransitionController not available in ServiceLocator!");
            return;
        }

        Debug.Log($"[DefeatPanel] Loading main menu: {mainMenuScene.SceneName}");

        // 게임 재개 (씬 전환 전)
        Time.timeScale = 1f;

        // SceneTransitionController로 씬 전환 (로딩 화면 포함)
        sceneTransitionController.LoadSceneWithLoading(mainMenuScene);
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
    /// 메인 메뉴 씬 데이터 설정
    /// </summary>
    public void SetMainMenuScene(SceneData sceneData)
    {
        mainMenuScene = sceneData;
    }

    #endregion
}
