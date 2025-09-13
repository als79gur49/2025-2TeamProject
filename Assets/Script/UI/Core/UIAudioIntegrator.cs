using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI와 오디오 시스템을 통합하는 컴포넌트
/// AudioServiceContainer와 연동하여 UI 사운드 자동 재생
/// </summary>
public class UIAudioIntegrator : MonoBehaviour
{
    [Header("UI Audio Settings")]
    [SerializeField] private bool enableButtonSounds = true;
    [SerializeField] private bool enablePanelSounds = true;
    [SerializeField] private bool enableHoverSounds = false;
    
    [Header("Sound Configuration")]
    [SerializeField] private string defaultButtonClickSound = "ButtonClick";
    [SerializeField] private string defaultPanelOpenSound = "PanelOpen";
    [SerializeField] private string defaultPanelCloseSound = "PanelClose";
    [SerializeField] private string defaultHoverSound = "ButtonHover";
    
    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float buttonSoundVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float panelSoundVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float hoverSoundVolume = 0.6f;
    
    // 오디오 서비스 참조
    private IEffectAudioService effectAudioService;
    
    // Singleton 접근 (옵션)
    private static UIAudioIntegrator instance;
    public static UIAudioIntegrator Instance => instance;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Singleton 설정 (옵션)
        if (instance == null)
        {
            instance = this;
        }
        
        InitializeAudioService();
    }
    
    private void Start()
    {
        SetupUIEventListeners();
    }
    
    private void OnDestroy()
    {
        CleanupUIEventListeners();
        
        if (instance == this)
        {
            instance = null;
        }
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// 오디오 서비스 초기화
    /// </summary>
    private void InitializeAudioService()
    {
        try
        {
            var container = AudioServiceContainer.Instance;
            effectAudioService = container.GetService<IEffectAudioService>();
            
            if (effectAudioService == null)
            {
                Debug.LogWarning("UIAudioIntegrator: EffectAudioService를 찾을 수 없습니다.");
            }
            else
            {
                Debug.Log("UIAudioIntegrator: 오디오 서비스 초기화 완료");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UIAudioIntegrator: 오디오 서비스 초기화 실패 - {ex.Message}");
        }
    }
    
    /// <summary>
    /// UI 이벤트 리스너 설정
    /// </summary>
    private void SetupUIEventListeners()
    {
        if (enablePanelSounds)
        {
            // UIPanelManager 이벤트 구독
            UIPanelManager.OnPanelShown += OnPanelShown;
            UIPanelManager.OnPanelHidden += OnPanelHidden;
        }
        
        if (enableButtonSounds)
        {
            // 씬의 모든 버튼에 사운드 자동 연결
            AutoSetupButtonSounds();
        }
    }
    
    /// <summary>
    /// UI 이벤트 리스너 정리
    /// </summary>
    private void CleanupUIEventListeners()
    {
        UIPanelManager.OnPanelShown -= OnPanelShown;
        UIPanelManager.OnPanelHidden -= OnPanelHidden;
    }
    
    #endregion
    
    #region Button Sound Setup
    
    /// <summary>
    /// 씬의 모든 버튼에 사운드 자동 설정
    /// </summary>
    private void AutoSetupButtonSounds()
    {
        var buttons = FindObjectsOfType<Button>(true); // 비활성 오브젝트 포함
        int setupCount = 0;
        
        foreach (var button in buttons)
        {
            // 이미 ButtonSoundPlayer가 있는지 확인
            if (button.GetComponent<ButtonSoundPlayer>() != null)
                continue;
                
            // 버튼에 사운드 리스너 추가
            button.onClick.AddListener(() => PlayButtonClickSound(button.name));
            
            // 호버 사운드 설정 (옵션)
            if (enableHoverSounds)
            {
                SetupButtonHoverSound(button);
            }
            
            setupCount++;
        }
        
        Debug.Log($"UIAudioIntegrator: {setupCount}개 버튼에 사운드 설정 완료");
    }
    
    /// <summary>
    /// 버튼 호버 사운드 설정
    /// </summary>
    private void SetupButtonHoverSound(Button button)
    {
        var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }
        
        // PointerEnter 이벤트 추가
        var pointerEnterEvent = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnterEvent.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnterEvent.callback.AddListener((data) => PlayHoverSound());
        
        eventTrigger.triggers.Add(pointerEnterEvent);
    }
    
    #endregion
    
    #region Sound Playback Methods
    
    /// <summary>
    /// 버튼 클릭 사운드 재생
    /// </summary>
    /// <param name="buttonName">버튼 이름 (사운드 선택용)</param>
    public void PlayButtonClickSound(string buttonName = null)
    {
        if (!enableButtonSounds || effectAudioService == null) return;
        
        string soundName = GetButtonSoundName(buttonName);
        effectAudioService.PlayEffect(soundName, buttonSoundVolume);
        
        Debug.Log($"버튼 클릭 사운드 재생: {soundName}");
    }
    
    /// <summary>
    /// 버튼 타입별 클릭 사운드 재생
    /// </summary>
    public void PlayButtonClickSound(ButtonSoundType soundType)
    {
        if (!enableButtonSounds || effectAudioService == null) return;
        
        string soundName = GetButtonSoundNameByType(soundType);
        effectAudioService.PlayEffect(soundName, buttonSoundVolume);
        
        Debug.Log($"버튼 사운드 재생: {soundName} (타입: {soundType})");
    }
    
    /// <summary>
    /// 호버 사운드 재생
    /// </summary>
    public void PlayHoverSound()
    {
        if (!enableHoverSounds || effectAudioService == null) return;
        
        effectAudioService.PlayEffect(defaultHoverSound, hoverSoundVolume);
    }
    
    /// <summary>
    /// 패널 오픈 사운드 재생
    /// </summary>
    public void PlayPanelOpenSound(string panelName = null)
    {
        if (!enablePanelSounds || effectAudioService == null) return;
        
        string soundName = GetPanelSoundName(panelName, true);
        effectAudioService.PlayEffect(soundName, panelSoundVolume);
        
        Debug.Log($"패널 오픈 사운드 재생: {soundName}");
    }
    
    /// <summary>
    /// 패널 클로즈 사운드 재생
    /// </summary>
    public void PlayPanelCloseSound(string panelName = null)
    {
        if (!enablePanelSounds || effectAudioService == null) return;
        
        string soundName = GetPanelSoundName(panelName, false);
        effectAudioService.PlayEffect(soundName, panelSoundVolume);
        
        Debug.Log($"패널 클로즈 사운드 재생: {soundName}");
    }
    
    /// <summary>
    /// 커스텀 UI 사운드 재생
    /// </summary>
    public void PlayCustomUISound(string soundName, float volume = 1f)
    {
        if (effectAudioService == null) return;
        
        effectAudioService.PlayEffect(soundName, volume);
        Debug.Log($"커스텀 UI 사운드 재생: {soundName}");
    }
    
    #endregion
    
    #region Event Handlers
    
    /// <summary>
    /// 패널 표시 이벤트 핸들러
    /// </summary>
    private void OnPanelShown(IUIPanel panel)
    {
        PlayPanelOpenSound(panel.PanelID);
    }
    
    /// <summary>
    /// 패널 숨김 이벤트 핸들러
    /// </summary>
    private void OnPanelHidden(IUIPanel panel)
    {
        PlayPanelCloseSound(panel.PanelID);
    }
    
    #endregion
    
    #region Sound Name Resolution
    
    /// <summary>
    /// 버튼 이름에 따른 사운드 이름 결정
    /// </summary>
    private string GetButtonSoundName(string buttonName)
    {
        if (string.IsNullOrEmpty(buttonName))
            return defaultButtonClickSound;
        
        // 버튼 이름 기반 사운드 매핑
        string lowerName = buttonName.ToLower();
        
        if (lowerName.Contains("confirm") || lowerName.Contains("ok"))
            return "ButtonConfirm";
        else if (lowerName.Contains("cancel") || lowerName.Contains("close"))
            return "ButtonCancel";
        else if (lowerName.Contains("warning") || lowerName.Contains("danger"))
            return "ButtonWarning";
        else if (lowerName.Contains("success") || lowerName.Contains("complete"))
            return "ButtonSuccess";
        else if (lowerName.Contains("nav") || lowerName.Contains("menu"))
            return "ButtonNavigation";
        
        return defaultButtonClickSound;
    }
    
    /// <summary>
    /// 버튼 타입에 따른 사운드 이름 결정
    /// </summary>
    private string GetButtonSoundNameByType(ButtonSoundType soundType)
    {
        return soundType switch
        {
            ButtonSoundType.Confirm => "ButtonConfirm",
            ButtonSoundType.Cancel => "ButtonCancel",
            ButtonSoundType.Warning => "ButtonWarning",
            ButtonSoundType.Success => "ButtonSuccess",
            ButtonSoundType.Navigation => "ButtonNavigation",
            _ => defaultButtonClickSound
        };
    }
    
    /// <summary>
    /// 패널 이름에 따른 사운드 이름 결정
    /// </summary>
    private string GetPanelSoundName(string panelName, bool isOpen)
    {
        if (string.IsNullOrEmpty(panelName))
            return isOpen ? defaultPanelOpenSound : defaultPanelCloseSound;
        
        // 패널별 특별한 사운드가 있다면 여기서 매핑
        string baseName = isOpen ? "PanelOpen" : "PanelClose";
        
        // 예: SettingsPanel -> SettingsPanelOpen
        string customSoundName = panelName.Replace("Panel", "") + baseName;
        
        // 기본 사운드 사용
        return isOpen ? defaultPanelOpenSound : defaultPanelCloseSound;
    }
    
    #endregion
    
    #region Public Configuration Methods
    
    /// <summary>
    /// 버튼 사운드 활성화/비활성화
    /// </summary>
    public void SetButtonSoundsEnabled(bool enabled)
    {
        enableButtonSounds = enabled;
    }
    
    /// <summary>
    /// 패널 사운드 활성화/비활성화
    /// </summary>
    public void SetPanelSoundsEnabled(bool enabled)
    {
        enablePanelSounds = enabled;
    }
    
    /// <summary>
    /// 호버 사운드 활성화/비활성화
    /// </summary>
    public void SetHoverSoundsEnabled(bool enabled)
    {
        enableHoverSounds = enabled;
    }
    
    /// <summary>
    /// 볼륨 설정 업데이트
    /// </summary>
    public void UpdateVolumes(float buttonVol, float panelVol, float hoverVol)
    {
        buttonSoundVolume = Mathf.Clamp01(buttonVol);
        panelSoundVolume = Mathf.Clamp01(panelVol);
        hoverSoundVolume = Mathf.Clamp01(hoverVol);
    }
    
    #endregion
}