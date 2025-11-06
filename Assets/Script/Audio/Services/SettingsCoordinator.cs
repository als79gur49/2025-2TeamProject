using System;
using UnityEngine;
using Game.Core;
using Game.SaveSystem;

/// <summary>
/// 설정 패널과 저장 시스템을 연결하는 Coordinator
/// 패널 열림/닫힘 시점에 자동으로 로드/저장 처리
///
/// 역할:
/// - SettingsPanel의 OnPanelShown/OnPanelHidden 이벤트 구독
/// - 패널 열림 → SaveDataAdapter를 통해 설정 로드
/// - 패널 닫힘 → SaveDataAdapter를 통해 설정 저장
///
/// 초기화:
/// - GameInitializer.InitializeCoordinators()에서 생성 및 초기화
/// - TitleSceneInitializer.InitializeCoordinators()에서 생성 및 초기화
///
/// 볼륨 저장 형식:
/// - 모든 볼륨 값은 dB 단위 (-80 ~ 20 dB)로 JSON에 저장됨
/// - UI 슬라이더(0~1)는 VolumeController에서 로그 스케일로 변환
/// - SaveDataAdapter가 VolumeController ↔ AudioSettingsData 간 중개
/// </summary>
public class SettingsCoordinator : MonoBehaviour, ISettingsCoordinator
{
    #region Dependencies
    private ISaveDataAdapter saveAdapter;
    private SettingsPanel settingsPanel;
    #endregion

    #region Initialization

    /// <summary>
    /// 초기화 (GameInitializer/TitleSceneInitializer에서 호출)
    /// </summary>
    /// <param name="panel">연결할 SettingsPanel</param>
    public void Initialize(SettingsPanel panel)
    {
        if (panel == null)
        {
            Debug.LogError("[SettingsCoordinator] SettingsPanel is null!");
            return;
        }

        settingsPanel = panel;

        // ISaveDataAdapter 획득 (외부 인터페이스)
        saveAdapter = ServiceLocator.Get<ISaveDataAdapter>();

        if (saveAdapter == null)
        {
            Debug.LogError("[SettingsCoordinator] ISaveDataAdapter not found in ServiceLocator!");
            return;
        }

        // UIPanel의 기존 이벤트 구독
        settingsPanel.OnPanelShown += OnPanelShown;
        settingsPanel.OnPanelHidden += OnPanelHidden;

        Debug.Log("[SettingsCoordinator] Initialized and subscribed to SettingsPanel events");
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 패널 열림 → 설정 로드
    /// JSON에서 dB 값 로드 → VolumeController에 적용
    /// </summary>
    private void OnPanelShown(IUIPanel panel)
    {
        Debug.Log("[SettingsCoordinator] Settings panel shown - Loading settings");

        if (saveAdapter == null)
        {
            Debug.LogWarning("[SettingsCoordinator] SaveDataAdapter is null - cannot load settings");
            return;
        }

        // SaveDataAdapter를 통해 로드 (VolumeController에 dB 값 자동 적용)
        saveAdapter.LoadSpecific(SaveFileType.AudioSettings);
    }

    /// <summary>
    /// 패널 닫힘 → 설정 저장
    /// VolumeController에서 dB 값 수집 → JSON에 저장
    /// </summary>
    private void OnPanelHidden(IUIPanel panel)
    {
        Debug.Log("[SettingsCoordinator] Settings panel hidden - Saving settings");

        if (saveAdapter == null)
        {
            Debug.LogWarning("[SettingsCoordinator] SaveDataAdapter is null - cannot save settings");
            return;
        }

        // SaveDataAdapter를 통해 저장 (VolumeController에서 dB 값 수집 후 JSON 저장)
        saveAdapter.SaveSpecific(SaveFileType.AudioSettings);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// 정리 (이벤트 구독 해제)
    /// </summary>
    public void Cleanup()
    {
        if (settingsPanel != null)
        {
            settingsPanel.OnPanelShown -= OnPanelShown;
            settingsPanel.OnPanelHidden -= OnPanelHidden;
        }

        Debug.Log("[SettingsCoordinator] Cleaned up");
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    #endregion
}
