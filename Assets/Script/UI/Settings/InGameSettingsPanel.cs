using UnityEngine;
using UnityEngine.UI;
using Game.Services;
using Game.SceneManagement;
using Game.Core;
using Game.Managers;
using Game.Data;

/// <summary>
/// 인게임 설정 패널 (SettingsPanel 확장)
/// SettingsPanel의 모든 기능(볼륨 설정, 음소거 등)을 상속받고
/// Retry 및 Home 버튼 기능을 추가합니다.
///
/// 사용 예:
/// - Retry: 현재 스테이지를 재시작합니다
/// - Home: 메인 메뉴/타이틀 씬으로 돌아갑니다
///
/// 초기화:
/// - GameInitializer.InitializeSettingsCoordinator()에서 자동 초기화
/// - 현재 스테이지 SceneData는 IStageProgressManager에서 런타임 주입
/// - Home SceneData는 Inspector에서 직렬화로 할당
/// </summary>
public class InGameSettingsPanel : SettingsPanel
{
    [Header("In-Game Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button homeButton;

    [Header("Scene Configuration")]
    [SerializeField]
    [Tooltip("Home/Title 씬으로 이동할 때 사용할 SceneData (Inspector에서 할당)")]
    private SceneData homeSceneData;

    // 런타임 주입 필드
    private SceneData currentStageSceneData;
    private ISceneTransitionController sceneTransitionController;
    private IStageProgressManager stageProgressManager;

    #region Initialization

    /// <summary>
    /// 의존성 있는 초기화 (Start에서 호출됨)
    /// 부모의 서비스 초기화 후 인게임 전용 서비스 추가 주입
    /// </summary>
    protected override void OnInitializeWithDependencies()
    {
        base.OnInitializeWithDependencies();

        // 인게임 전용 서비스 초기화
        InitializeInGameServices();

        // 현재 스테이지 SceneData 주입
        InjectCurrentStageSceneData();

        // 버튼 이벤트 설정
        SetupInGameButtons();

        Debug.Log("[InGameSettingsPanel] In-game specific initialization complete");
    }

    #endregion

    #region Service Initialization

    /// <summary>
    /// 인게임 전용 서비스 초기화
    /// </summary>
    private void InitializeInGameServices()
    {
        try
        {
            // SceneTransitionController 주입
            sceneTransitionController = ServiceLocator.Get<ISceneTransitionController>();
            if (sceneTransitionController == null)
            {
                Debug.LogError("[InGameSettingsPanel] ISceneTransitionController not found in ServiceLocator! " +
                              "Retry and Home buttons will not work.");
                return;
            }

            // StageProgressManager 주입
            stageProgressManager = ServiceLocator.Get<IStageProgressManager>();
            if (stageProgressManager == null)
            {
                Debug.LogError("[InGameSettingsPanel] IStageProgressManager not found in ServiceLocator! " +
                              "Retry button will not work.");
                return;
            }

            Debug.Log("[InGameSettingsPanel] In-game services initialized successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InGameSettingsPanel] Service initialization failed - {ex.Message}");
        }
    }

    /// <summary>
    /// 현재 스테이지의 SceneData를 IStageProgressManager에서 가져와 주입
    /// </summary>
    private void InjectCurrentStageSceneData()
    {
        if (stageProgressManager == null)
        {
            Debug.LogWarning("[InGameSettingsPanel] StageProgressManager not available, cannot inject current stage data");
            return;
        }

        try
        {
            // 현재 스테이지 ID 가져오기
            string currentStageId = stageProgressManager.GetCurrentStageId();

            if (string.IsNullOrEmpty(currentStageId))
            {
                Debug.LogWarning("[InGameSettingsPanel] Current stage ID is null or empty");
                return;
            }

            // 스테이지 데이터 가져오기
            StageDataSO stageData = stageProgressManager.GetStageData(currentStageId);

            if (stageData == null)
            {
                Debug.LogWarning($"[InGameSettingsPanel] StageData not found for stage ID: {currentStageId}");
                return;
            }

            // SceneData 주입
            currentStageSceneData = stageData.SceneData;

            if (currentStageSceneData == null)
            {
                Debug.LogWarning($"[InGameSettingsPanel] SceneData is null in StageData: {currentStageId}");
                return;
            }

            Debug.Log($"[InGameSettingsPanel] Current stage SceneData injected successfully: {currentStageSceneData.SceneName}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InGameSettingsPanel] Failed to inject current stage SceneData - {ex.Message}");
        }
    }

    #endregion

    #region Button Setup

    /// <summary>
    /// 인게임 버튼 이벤트 리스너 설정
    /// </summary>
    private void SetupInGameButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
            Debug.Log("[InGameSettingsPanel] Retry button listener registered");
        }
        else
        {
            Debug.LogWarning("[InGameSettingsPanel] Retry button is not assigned in Inspector");
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(OnHomeClicked);
            Debug.Log("[InGameSettingsPanel] Home button listener registered");
        }
        else
        {
            Debug.LogWarning("[InGameSettingsPanel] Home button is not assigned in Inspector");
        }
    }

    /// <summary>
    /// 인게임 버튼 이벤트 리스너 정리
    /// </summary>
    private void CleanupInGameButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryClicked);
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(OnHomeClicked);
        }
    }

    #endregion

    #region Button Event Handlers

    /// <summary>
    /// Retry 버튼 클릭 핸들러
    /// 현재 스테이지를 재시작합니다
    /// </summary>
    private void OnRetryClicked()
    {
        Debug.Log("[InGameSettingsPanel] Retry button clicked");

        // Null 체크
        if (sceneTransitionController == null)
        {
            Debug.LogError("[InGameSettingsPanel] SceneTransitionController is not initialized! Cannot retry stage.");
            return;
        }

        if (currentStageSceneData == null)
        {
            Debug.LogError("[InGameSettingsPanel] Current stage SceneData is null! Cannot retry stage.");
            return;
        }

        // 이미 전환 중인지 체크
        if (sceneTransitionController.IsTransitioning)
        {
            Debug.LogWarning("[InGameSettingsPanel] Scene transition already in progress. Please wait.");
            return;
        }

        // 현재 스테이지 씬 리로드
        Debug.Log($"[InGameSettingsPanel] Restarting current stage: {currentStageSceneData.SceneName}");
        sceneTransitionController.LoadSceneWithLoading(currentStageSceneData);
    }

    /// <summary>
    /// Home 버튼 클릭 핸들러
    /// 메인 메뉴/타이틀 씬으로 돌아갑니다
    /// </summary>
    private void OnHomeClicked()
    {
        Debug.Log("[InGameSettingsPanel] Home button clicked");

        // Null 체크
        if (sceneTransitionController == null)
        {
            Debug.LogError("[InGameSettingsPanel] SceneTransitionController is not initialized! Cannot go home.");
            return;
        }

        if (homeSceneData == null)
        {
            Debug.LogError("[InGameSettingsPanel] Home SceneData is not assigned in Inspector! Cannot go home.");
            return;
        }

        // 이미 전환 중인지 체크
        if (sceneTransitionController.IsTransitioning)
        {
            Debug.LogWarning("[InGameSettingsPanel] Scene transition already in progress. Please wait.");
            return;
        }

        // 홈 씬으로 전환
        Debug.Log($"[InGameSettingsPanel] Transitioning to home scene: {homeSceneData.SceneName}");
        sceneTransitionController.LoadSceneWithLoading(homeSceneData);
    }

    #endregion

    #region UIPanel Overrides

    protected override void OnShowPanel()
    {
        base.OnShowPanel();

        // 패널이 표시될 때마다 현재 스테이지 데이터 새로고침
        InjectCurrentStageSceneData();

        // 버튼 상태 업데이트 (필요한 경우)
        UpdateButtonStates();
    }

    protected override void OnCleanup()
    {
        // 인게임 버튼 이벤트 정리
        CleanupInGameButtons();

        // 부모 클래스 정리
        base.OnCleanup();

        Debug.Log("[InGameSettingsPanel] In-game panel cleanup complete");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 버튼 활성화 상태 업데이트
    /// SceneData가 유효한 경우에만 버튼 활성화
    /// </summary>
    private void UpdateButtonStates()
    {
        // Retry 버튼 상태
        if (retryButton != null)
        {
            bool canRetry = currentStageSceneData != null && sceneTransitionController != null;
            retryButton.interactable = canRetry;

            if (!canRetry)
            {
                Debug.LogWarning("[InGameSettingsPanel] Retry button disabled - missing dependencies");
            }
        }

        // Home 버튼 상태
        if (homeButton != null)
        {
            bool canGoHome = homeSceneData != null && sceneTransitionController != null;
            homeButton.interactable = canGoHome;

            if (!canGoHome)
            {
                Debug.LogWarning("[InGameSettingsPanel] Home button disabled - missing dependencies");
            }
        }
    }

    #endregion
}
