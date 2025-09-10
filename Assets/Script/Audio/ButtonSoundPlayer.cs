using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼 클릭 시 사운드 재생을 위한 스크립트
/// Button의 OnClick() 이벤트에 연결하여 사용
/// </summary>
public class ButtonSoundPlayer : MonoBehaviour
{
    [Header("사운드 설정")]
    [SerializeField] private string clickSoundName = "ButtonClick";
    [SerializeField] private float volume = 1.0f;
    [SerializeField] private float pitch = 1.0f;
    
    [Header("버튼 타입별 사운드")]
    [SerializeField] private ButtonSoundType buttonType = ButtonSoundType.Default;
    
    [Header("자동 설정")]
    [SerializeField] private bool autoRegisterOnStart = true;
    [SerializeField] private bool enableHoverSound = false;
    [SerializeField] private string hoverSoundName = "ButtonHover";
    
    private Button targetButton;
    private IEffectAudioService effectAudioService;
    
    /// <summary>
    /// Unity Start: 자동 등록 및 초기화
    /// </summary>
    private void Start()
    {
        InitializeAudioService();
        
        if (autoRegisterOnStart)
        {
            RegisterToButton();
        }
        
        // 버튼 타입에 따른 기본 사운드 설정
        SetDefaultSoundByType();
    }
    
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
                Debug.LogWarning($"[ButtonSoundPlayer] EffectAudioService를 찾을 수 없습니다: {gameObject.name}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ButtonSoundPlayer] AudioService 초기화 실패: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 버튼 컴포넌트에 자동 등록
    /// </summary>
    private void RegisterToButton()
    {
        targetButton = GetComponent<Button>();
        
        if (targetButton != null)
        {
            // OnClick 이벤트에 사운드 재생 함수 등록
            targetButton.onClick.AddListener(PlayClickSound);
            
            Debug.Log($"[ButtonSoundPlayer] 버튼에 클릭 사운드 등록: {gameObject.name}");
            
            // 호버 사운드 설정 (EventTrigger 사용)
            if (enableHoverSound)
            {
                SetupHoverSound();
            }
        }
        else
        {
            Debug.LogWarning($"[ButtonSoundPlayer] Button 컴포넌트를 찾을 수 없습니다: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// 버튼 타입에 따른 기본 사운드 설정
    /// </summary>
    private void SetDefaultSoundByType()
    {
        switch (buttonType)
        {
            case ButtonSoundType.Default:
                clickSoundName = "ButtonClick";
                break;
            case ButtonSoundType.Confirm:
                clickSoundName = "ButtonConfirm";
                break;
            case ButtonSoundType.Cancel:
                clickSoundName = "ButtonCancel";
                break;
            case ButtonSoundType.Warning:
                clickSoundName = "ButtonWarning";
                break;
            case ButtonSoundType.Success:
                clickSoundName = "ButtonSuccess";
                break;
            case ButtonSoundType.Navigation:
                clickSoundName = "ButtonNavigation";
                break;
        }
    }
    
    /// <summary>
    /// 클릭 사운드 재생 (Public 메서드 - OnClick 이벤트용)
    /// </summary>
    public void PlayClickSound()
    {
        if (effectAudioService == null)
        {
            Debug.LogWarning("[ButtonSoundPlayer] EffectAudioService가 초기화되지 않았습니다.");
            return;
        }
        
        if (string.IsNullOrEmpty(clickSoundName))
        {
            Debug.LogWarning("[ButtonSoundPlayer] 클릭 사운드 이름이 설정되지 않았습니다.");
            return;
        }
        
        // 효과음 재생
        effectAudioService.PlayEffect(clickSoundName, volume, pitch);
        
        Debug.Log($"[ButtonSoundPlayer] 클릭 사운드 재생: {clickSoundName}");
    }
    
    /// <summary>
    /// 호버 사운드 재생 (Public 메서드)
    /// </summary>
    public void PlayHoverSound()
    {
        if (!enableHoverSound || effectAudioService == null || string.IsNullOrEmpty(hoverSoundName))
            return;
        
        effectAudioService.PlayEffect(hoverSoundName, volume * 0.7f, pitch);
        Debug.Log($"[ButtonSoundPlayer] 호버 사운드 재생: {hoverSoundName}");
    }
    
    /// <summary>
    /// 커스텀 사운드 재생 (Public 메서드)
    /// </summary>
    /// <param name="soundName">재생할 사운드 이름</param>
    /// <param name="customVolume">커스텀 볼륨 (선택적)</param>
    /// <param name="customPitch">커스텀 피치 (선택적)</param>
    public void PlayCustomSound(string soundName, float customVolume = -1f, float customPitch = -1f)
    {
        if (effectAudioService == null || string.IsNullOrEmpty(soundName))
            return;
        
        float finalVolume = customVolume > 0 ? customVolume : volume;
        float finalPitch = customPitch > 0 ? customPitch : pitch;
        
        effectAudioService.PlayEffect(soundName, finalVolume, finalPitch);
        Debug.Log($"[ButtonSoundPlayer] 커스텀 사운드 재생: {soundName}");
    }
    
    /// <summary>
    /// 런타임에 사운드 설정 변경
    /// </summary>
    /// <param name="newSoundName">새로운 사운드 이름</param>
    /// <param name="newVolume">새로운 볼륨</param>
    /// <param name="newPitch">새로운 피치</param>
    public void UpdateSoundSettings(string newSoundName, float newVolume = -1f, float newPitch = -1f)
    {
        if (!string.IsNullOrEmpty(newSoundName))
            clickSoundName = newSoundName;
        
        if (newVolume >= 0)
            volume = newVolume;
        
        if (newPitch > 0)
            pitch = newPitch;
        
        Debug.Log($"[ButtonSoundPlayer] 사운드 설정 업데이트: {clickSoundName}, Vol: {volume}, Pitch: {pitch}");
    }
    
    /// <summary>
    /// 호버 사운드 EventTrigger 설정
    /// </summary>
    private void SetupHoverSound()
    {
        var eventTrigger = targetButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = targetButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }
        
        // PointerEnter 이벤트 추가
        var pointerEnterEvent = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnterEvent.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnterEvent.callback.AddListener((data) => PlayHoverSound());
        
        eventTrigger.triggers.Add(pointerEnterEvent);
    }
    
    /// <summary>
    /// Unity OnDestroy: 이벤트 정리
    /// </summary>
    private void OnDestroy()
    {
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(PlayClickSound);
        }
    }
}

/// <summary>
/// 버튼 사운드 타입 열거형
/// </summary>
public enum ButtonSoundType
{
    Default,        // 기본 클릭
    Confirm,        // 확인/승인
    Cancel,         // 취소/닫기
    Warning,        // 경고
    Success,        // 성공/완료
    Navigation      // 네비게이션/메뉴
}