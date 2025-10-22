using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Services;
using Game.Core;

/// <summary>
/// 설정 패널 (VolumeController 연동)
/// ServiceLocator를 통한 서비스 접근 패턴 사용
/// IOpenablePanel을 구현하여 확장 메서드로 Open/Close 버튼 자동 바인딩 지원
/// </summary>
public class SettingsPanel : UIPanel, IOpenablePanel
{
    [Header("Panel Buttons (IOpenablePanel)")]
    [SerializeField] private Button openButton;  // 외부에서 설정 패널을 여는 버튼
    [SerializeField] private Button closeButton; // 패널 내부의 닫기 버튼

    // IOpenablePanel 구현
    public Button OpenButton => openButton;
    public Button CloseButton => closeButton;

    [Header("볼륨 설정 UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider effectVolumeSlider;

    [Header("볼륨 표시 텍스트")]
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private TextMeshProUGUI bgmVolumeText;
    [SerializeField] private TextMeshProUGUI effectVolumeText;

    [Header("음소거 토글")]
    [SerializeField] private Toggle masterMuteToggle;
    [SerializeField] private Toggle bgmMuteToggle;
    [SerializeField] private Toggle effectMuteToggle;

    [Header("음소거 토글 텍스트")]
    [SerializeField] private TextMeshProUGUI masterMuteToggleText;
    [SerializeField] private TextMeshProUGUI bgmMuteToggleText;
    [SerializeField] private TextMeshProUGUI effectMuteToggleText;

    [Header("UI 사운드 설정")]
    [SerializeField] private Toggle buttonSoundsToggle;
    [SerializeField] private Toggle panelSoundsToggle;
    [SerializeField] private Toggle hoverSoundsToggle;

    [Header("기타 버튼")]
    [SerializeField] private Button resetButton;
    [SerializeField] private Button applyButton;
    
    [Header("설정")]
    [SerializeField] private bool autoApplyChanges = true;
    [SerializeField] private bool showVolumePercentage = true;
    
    // 서비스 참조
    private IVolumeController volumeController;
    
    // 초기값 저장 (리셋용)
    private VolumeSettings initialSettings;
    

    private const string ON = "On";
    private const string OFF = "Off";

    #region Initialization

    /// <summary>
    /// 의존성 없는 초기화 (Awake에서 호출됨)
    /// UI 이벤트 설정
    /// </summary>
    protected override void OnInitializeSelf()
    {
        base.OnInitializeSelf();

        // UI 이벤트 설정 (버튼 리스너 등록)
        SetupUIEvents();

        Debug.Log("[SettingsPanel] Self-initialized successfully (Awake)");
    }

    /// <summary>
    /// 의존성 있는 초기화 (Start에서 호출됨)
    /// ServiceLocator에서 서비스 가져오기
    /// </summary>
    protected override void OnInitializeWithDependencies()
    {
        base.OnInitializeWithDependencies();

        // 서비스 초기화
        InitializeServices();

        // 초기 설정 로드
        LoadCurrentSettings();

        Debug.Log("[SettingsPanel] Dependency initialization complete (Start)");
    }

    #endregion

    #region UIPanel 오버라이드
    
    protected override void OnShowPanel()
    {
        base.OnShowPanel();
        
        // 현재 설정으로 UI 새로고침
        RefreshUI();
    }
    
    protected override void OnCleanup()
    {
        // 이벤트 정리
        CleanupUIEvents();

        base.OnCleanup();
    }
    
    #endregion
    
    #region 서비스 초기화

    /// <summary>
    /// 오디오 서비스들 초기화
    /// ServiceLocator를 통한 서비스 접근
    /// </summary>
    private void InitializeServices()
    {
        try
        {
            // ✅ ServiceLocator 패턴 사용 (MainMenuPanel과 일관성 유지)
            volumeController = ServiceLocator.Get<IVolumeController>();

            if (volumeController == null)
            {
                Debug.LogError("[SettingsPanel] VolumeController not found in ServiceLocator! " +
                              "Ensure VolumeController is registered in Bootstrap or AudioServiceContainer.");
                return;
            }

            Debug.Log("[SettingsPanel] Service initialization complete");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SettingsPanel] Service initialization failed - {ex.Message}");
        }
    }

    #endregion
    
    #region UI 이벤트 설정
    
    /// <summary>
    /// UI 이벤트 리스너 설정
    /// </summary>
    private void SetupUIEvents()
    {
        // 볼륨 슬라이더 이벤트
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        
        if (effectVolumeSlider != null)
            effectVolumeSlider.onValueChanged.AddListener(OnEffectVolumeChanged);
        
        // 음소거 토글 이벤트
        if (masterMuteToggle != null)
            masterMuteToggle.onValueChanged.AddListener(OnMasterMuteChanged);
        
        if (bgmMuteToggle != null)
            bgmMuteToggle.onValueChanged.AddListener(OnBGMMuteChanged);
        
        if (effectMuteToggle != null)
            effectMuteToggle.onValueChanged.AddListener(OnEffectMuteChanged);
        
        // UI 사운드 토글 이벤트
        if (buttonSoundsToggle != null)
            buttonSoundsToggle.onValueChanged.AddListener(OnButtonSoundsChanged);
        
        if (panelSoundsToggle != null)
            panelSoundsToggle.onValueChanged.AddListener(OnPanelSoundsChanged);
        
        if (hoverSoundsToggle != null)
            hoverSoundsToggle.onValueChanged.AddListener(OnHoverSoundsChanged);
        
        // 기타 버튼 이벤트 (Open/Close는 확장 메서드로 자동 처리됨)
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClicked);
        
        // VolumeController 이벤트 구독
        if (volumeController != null)
        {
            volumeController.OnVolumeChanged += OnVolumeControllerChanged;
            volumeController.OnMuteChanged += OnMuteControllerChanged;
        }
    }
    
    /// <summary>
    /// UI 이벤트 리스너 정리
    /// </summary>
    private void CleanupUIEvents()
    {
        // 슬라이더 이벤트 정리
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        
        if (effectVolumeSlider != null)
            effectVolumeSlider.onValueChanged.RemoveListener(OnEffectVolumeChanged);
        
        // 토글 이벤트 정리
        if (masterMuteToggle != null)
            masterMuteToggle.onValueChanged.RemoveListener(OnMasterMuteChanged);
        
        if (bgmMuteToggle != null)
            bgmMuteToggle.onValueChanged.RemoveListener(OnBGMMuteChanged);
        
        if (effectMuteToggle != null)
            effectMuteToggle.onValueChanged.RemoveListener(OnEffectMuteChanged);
        
        if (buttonSoundsToggle != null)
            buttonSoundsToggle.onValueChanged.RemoveListener(OnButtonSoundsChanged);
        
        if (panelSoundsToggle != null)
            panelSoundsToggle.onValueChanged.RemoveListener(OnPanelSoundsChanged);
        
        if (hoverSoundsToggle != null)
            hoverSoundsToggle.onValueChanged.RemoveListener(OnHoverSoundsChanged);
        
        // 기타 버튼 이벤트 정리
        if (resetButton != null)
            resetButton.onClick.RemoveListener(OnResetClicked);

        if (applyButton != null)
            applyButton.onClick.RemoveListener(OnApplyClicked);
        
        // VolumeController 이벤트 해제
        if (volumeController != null)
        {
            volumeController.OnVolumeChanged -= OnVolumeControllerChanged;
            volumeController.OnMuteChanged -= OnMuteControllerChanged;
        }
    }
    
    #endregion
    
    #region 볼륨 이벤트 핸들러
    
    /// <summary>
    /// 마스터 볼륨 변경
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        if (volumeController == null) return;
        
        volumeController.SetMasterVolumeNormalized(value);
        UpdateVolumeText(masterVolumeText, value);
        
        if (autoApplyChanges)
            volumeController.SaveVolumeSettings();
    }
    
    /// <summary>
    /// BGM 볼륨 변경
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        if (volumeController == null) return;
        
        volumeController.SetBGMVolumeNormalized(value);
        UpdateVolumeText(bgmVolumeText, value);
        
        if (autoApplyChanges)
            volumeController.SaveVolumeSettings();
    }
    
    /// <summary>
    /// 효과음 볼륨 변경
    /// </summary>
    private void OnEffectVolumeChanged(float value)
    {
        if (volumeController == null) return;
        
        volumeController.SetEffectVolumeNormalized(value);
        UpdateVolumeText(effectVolumeText, value);
        
        if (autoApplyChanges)
            volumeController.SaveVolumeSettings();
    }
    
    /// <summary>
    /// 마스터 음소거 변경
    /// </summary>
    private void OnMasterMuteChanged(bool isOn)
    {
        if (volumeController == null) return;

        volumeController.IsMasterMuted = !isOn;

        if(masterMuteToggleText != null)
        {
            masterMuteToggleText.text = isOn ? ON : OFF;
        }

        if (autoApplyChanges)
            volumeController.SaveVolumeSettings();
    }
    
    /// <summary>
    /// BGM 음소거 변경
    /// </summary>
    private void OnBGMMuteChanged(bool isOn)
    {
        if (volumeController == null) return;

        volumeController.IsBGMMuted = !isOn;

        if (bgmMuteToggleText != null)
        {
            bgmMuteToggleText.text = isOn ? ON : OFF;
        }

        if (autoApplyChanges)
            volumeController.SaveVolumeSettings();
    }
    
    /// <summary>
    /// 효과음 음소거 변경
    /// </summary>
    private void OnEffectMuteChanged(bool isOn)
    {
        if (volumeController == null) return;

        volumeController.IsEffectMuted = !isOn;

        if (effectMuteToggleText != null)
        {
            effectMuteToggleText.text = isOn ? ON : OFF;
        }

        if (autoApplyChanges)
            volumeController.SaveVolumeSettings();
    }
    
    #endregion
    
    #region UI 사운드 이벤트 핸들러
    
    /// <summary>
    /// 버튼 사운드 토글 변경
    /// </summary>
    private void OnButtonSoundsChanged(bool enabled)
    {
    }
    
    /// <summary>
    /// 패널 사운드 토글 변경
    /// </summary>
    private void OnPanelSoundsChanged(bool enabled)
    {
    }
    
    /// <summary>
    /// 호버 사운드 토글 변경
    /// </summary>
    private void OnHoverSoundsChanged(bool enabled)
    {
    }
    
    #endregion
    
    #region VolumeController 이벤트 핸들러
    
    /// <summary>
    /// VolumeController 볼륨 변경 이벤트
    /// </summary>
    private void OnVolumeControllerChanged(VolumeType volumeType, float volume)
    {
        // UI 업데이트 (무한 루프 방지를 위해 조건적 업데이트)
        switch (volumeType)
        {
            case VolumeType.Master:
                if (masterVolumeSlider != null && !Mathf.Approximately(masterVolumeSlider.value, volumeController.GetMasterVolumeNormalized()))
                {
                    masterVolumeSlider.value = volumeController.GetMasterVolumeNormalized();
                }
                break;
                
            case VolumeType.BGM:
                if (bgmVolumeSlider != null && !Mathf.Approximately(bgmVolumeSlider.value, volumeController.GetBGMVolumeNormalized()))
                {
                    bgmVolumeSlider.value = volumeController.GetBGMVolumeNormalized();
                }
                break;
                
            case VolumeType.Effect:
                if (effectVolumeSlider != null && !Mathf.Approximately(effectVolumeSlider.value, volumeController.GetEffectVolumeNormalized()))
                {
                    effectVolumeSlider.value = volumeController.GetEffectVolumeNormalized();
                }
                break;
        }
    }
    
    /// <summary>
    /// VolumeController 음소거 변경 이벤트
    /// </summary>
    private void OnMuteControllerChanged(VolumeType volumeType, bool isMuted)
    {
        // UI 업데이트
        switch (volumeType)
        {
            case VolumeType.Master:
                if (masterMuteToggle != null && masterMuteToggle.isOn != isMuted)
                {
                    masterMuteToggle.isOn = isMuted;
                }
                break;
                
            case VolumeType.BGM:
                if (bgmMuteToggle != null && bgmMuteToggle.isOn != isMuted)
                {
                    bgmMuteToggle.isOn = isMuted;
                }
                break;
                
            case VolumeType.Effect:
                if (effectMuteToggle != null && effectMuteToggle.isOn != isMuted)
                {
                    effectMuteToggle.isOn = isMuted;
                }
                break;
        }
    }
    
    #endregion
    
    #region 버튼 이벤트 핸들러
    
    /// <summary>
    /// 리셋 버튼 클릭
    /// </summary>
    private void OnResetClicked()
    {
        if (volumeController == null) return;
        
        Debug.Log("볼륨 설정 리셋");
        
        // 기본값으로 리셋
        volumeController.ResetToDefault();
        
        // UI 새로고침
        RefreshUI();
    }
    
    /// <summary>
    /// 적용 버튼 클릭
    /// </summary>
    private void OnApplyClicked()
    {
        if (volumeController == null) return;
        
        Debug.Log("볼륨 설정 적용");
        
        // 설정 저장
        volumeController.SaveVolumeSettings();
       
    }
    
    
    #endregion
    
    #region UI 업데이트
    
    /// <summary>
    /// 현재 설정 로드
    /// </summary>
    private void LoadCurrentSettings()
    {
        if (volumeController == null) return;
        
        // 초기 설정 저장 (리셋용)
        initialSettings = volumeController.GetCurrentSettings();
        
        // UI에 현재 설정 반영
        RefreshUI();
    }
    
    /// <summary>
    /// UI 새로고침
    /// </summary>
    private void RefreshUI()
    {
        if (volumeController == null) return;
        
        // 볼륨 슬라이더 업데이트
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = volumeController.GetMasterVolumeNormalized();
            UpdateVolumeText(masterVolumeText, masterVolumeSlider.value);
        }
        
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = volumeController.GetBGMVolumeNormalized();
            UpdateVolumeText(bgmVolumeText, bgmVolumeSlider.value);
        }
        
        if (effectVolumeSlider != null)
        {
            effectVolumeSlider.value = volumeController.GetEffectVolumeNormalized();
            UpdateVolumeText(effectVolumeText, effectVolumeSlider.value);
        }
        
        // 음소거 토글 업데이트
        if (masterMuteToggle != null)
            masterMuteToggle.isOn = !volumeController.IsMasterMuted;
        if(masterMuteToggleText != null)
            masterMuteToggleText.text = !volumeController.IsMasterMuted ? ON : OFF;

        if (bgmMuteToggle != null)
            bgmMuteToggle.isOn = !volumeController.IsBGMMuted;
        if(bgmMuteToggleText != null)
            bgmMuteToggleText.text = !volumeController.IsBGMMuted ? ON : OFF;

        if (effectMuteToggle != null)
            effectMuteToggle.isOn = !volumeController.IsEffectMuted;
        if(effectMuteToggleText != null)
            effectMuteToggleText.text = !volumeController.IsEffectMuted ? ON : OFF;
        
        // UI 사운드 설정 업데이트 (AudioIntegrator에서 가져오기)
        // 실제 구현에서는 AudioIntegrator에서 현재 설정을 가져오는 메서드 필요
    }
    
    /// <summary>
    /// 볼륨 텍스트 업데이트
    /// </summary>
    private void UpdateVolumeText(TextMeshProUGUI textComponent, float normalizedValue)
    {
        if (textComponent == null) return;
        
        if (showVolumePercentage)
        {
            int percentage = Mathf.RoundToInt(normalizedValue * 100);
            textComponent.text = $"{percentage}%";
        }
        else
        {
            textComponent.text = normalizedValue.ToString("F2");
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 특정 볼륨 타입의 설정만 표시 (옵션)
    /// </summary>
    public void ShowVolumeType(VolumeType volumeType)
    {
        // 특정 볼륨 타입만 표시하고 싶을 때 사용
        // 예: 게임 내에서 BGM 볼륨만 조정하고 싶을 때
    }
    
    /// <summary>
    /// 설정 패널 상태 확인
    /// </summary>
    public bool HasUnsavedChanges()
    {
        if (volumeController == null) return false;
        
        var currentSettings = volumeController.GetCurrentSettings();
        
        return !currentSettings.Equals(initialSettings);
    }
    
    #endregion
}