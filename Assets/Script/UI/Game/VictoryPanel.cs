using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.SceneManagement;
using Game.Services;
using Game.Core;

/// <summary>
/// 플레이어 승리 패널
/// GameUICoordinator에 의해 표시됨 (게임 로직으로부터 분리)
/// IGameResultPanel을 구현하여 UIPanelManager의 자동 바인딩 지원
///
/// Architecture:
/// - Uses ServiceLocator for SceneTransitionController access
/// - Uses SceneData ScriptableObjects for type-safe scene references
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

    [Header("Audio Configuration")]
    [SerializeField] private SoundEventChannelSO soundEventChannel;  // Event Channel
    [SerializeField] private AudioData victorySoundData;             // Victory 사운드

    [Header("Scene Configuration")]
    [SerializeField] private SceneData mainMenuScene;  // 메인 메뉴 씬 데이터
    [SerializeField] private SceneData nextLevelScene; // 다음 레벨 씬 데이터

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

        Debug.Log("[VictoryPanel] Self-initialized successfully (Awake)");
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
            Debug.LogError("[VictoryPanel] ISceneTransitionController not found in ServiceLocator! " +
                          "Ensure SceneTransitionController is registered in Bootstrap scene.");
        }

        Debug.Log("[VictoryPanel] Dependency initialization complete (Start)");
    }

    /// <summary>
    /// 필수 참조 검증
    /// </summary>
    private void ValidateReferences()
    {
        if (nextLevelButton == null)
            Debug.LogWarning("[VictoryPanel] Next Level Button not assigned!");

        if (mainMenuButton == null)
            Debug.LogWarning("[VictoryPanel] Main Menu Button not assigned!");

        if (mainMenuScene == null)
            Debug.LogWarning("[VictoryPanel] Main Menu Scene Data not assigned!");

        if (nextLevelScene == null)
            Debug.LogWarning("[VictoryPanel] Next Level Scene Data not assigned!");
    }

    #endregion

    #region UIPanel 오버라이드

    protected override void OnShowPanel()
    {
        base.OnShowPanel();

        // Victory 사운드 출력 (CombatComponent 패턴)
        if (soundEventChannel != null && victorySoundData != null)
        {
            soundEventChannel.RaiseSoundEvent(victorySoundData, this);
            Debug.Log("[VictoryPanel] Victory sound played via SoundEventChannel");
        }

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
        if (nextLevelScene == null)
        {
            Debug.LogError("[VictoryPanel] Next Level Scene Data is not assigned!");
            return;
        }

        // 안전하게 ServiceLocator에서 다시 가져오기 (null일 경우 재시도)
        if (sceneTransitionController == null)
        {
            sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();
        }

        if (sceneTransitionController == null)
        {
            Debug.LogError("[VictoryPanel] SceneTransitionController not available in ServiceLocator!");
            return;
        }

        Debug.Log($"[VictoryPanel] Loading next level: {nextLevelScene.SceneName}");

        // 게임 재개 (씬 전환 전)
        Time.timeScale = 1f;

        // SceneTransitionController로 씬 전환 (로딩 화면 포함)
        sceneTransitionController.LoadSceneWithLoading(nextLevelScene);
    }

    /// <summary>
    /// Secondary Action: 메인 메뉴로 이동
    /// UIPanelManager가 자동으로 mainMenuButton에 바인딩
    /// </summary>
    public void OnSecondaryAction()
    {
        if (mainMenuScene == null)
        {
            Debug.LogError("[VictoryPanel] Main Menu Scene Data is not assigned!");
            return;
        }

        // 안전하게 ServiceLocator에서 다시 가져오기 (null일 경우 재시도)
        if (sceneTransitionController == null)
        {
            sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();
        }

        if (sceneTransitionController == null)
        {
            Debug.LogError("[VictoryPanel] SceneTransitionController not available in ServiceLocator!");
            return;
        }

        Debug.Log($"[VictoryPanel] Loading main menu: {mainMenuScene.SceneName}");

        // 게임 재개 (씬 전환 전)
        Time.timeScale = 1f;

        // SceneTransitionController로 씬 전환 (로딩 화면 포함)
        sceneTransitionController.LoadSceneWithLoading(mainMenuScene);
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
    /// 다음 레벨 씬 데이터 설정
    /// </summary>
    public void SetNextLevelScene(SceneData sceneData)
    {
        nextLevelScene = sceneData;
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
