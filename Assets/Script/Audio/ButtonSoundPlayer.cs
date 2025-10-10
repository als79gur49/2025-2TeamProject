using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼 클릭 시 사운드 재생을 위한 스크립트
/// Button의 OnClick() 이벤트에 연결하여 사용
///
/// 우선순위 시스템:
/// 1. Event Channel + AudioData (BEST - 완전 분리)
/// 2. AudioData만 (NEW - 직접 서비스 호출)
/// </summary>
public class ButtonSoundPlayer : MonoBehaviour
{
    [Header("Sound Configuration")]
    [Tooltip("Event Channel을 통한 완전 분리된 사운드 시스템")]
    [SerializeField] private SoundEventChannelSO soundChannel;

    [Tooltip("ScriptableObject 기반 AudioData")]
    [SerializeField] private AudioData audioData;

    [Header("자동 설정")]
    [SerializeField] private bool autoRegisterOnStart = true;

    private Button targetButton;
    private IEffectAudioService effectAudioService;
    
    /// <summary>
    /// Unity Start: 자동 등록 및 초기화
    /// </summary>
    private void Start()
    {
        // EffectAudioService 초기화 (AudioData 방식용)
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

        if (autoRegisterOnStart)
        {
            RegisterToButton();
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
        }
        else
        {
            Debug.LogWarning($"[ButtonSoundPlayer] Button 컴포넌트를 찾을 수 없습니다: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// 클릭 사운드 재생 (Public 메서드 - OnClick 이벤트용)
    /// 우선순위: Event Channel > AudioData
    /// </summary>
    public void PlayClickSound()
    {
        // 우선순위 1: Event Channel + AudioData (BEST - 완전 분리)
        if (soundChannel != null && audioData != null)
        {
            soundChannel.RaiseSoundEvent(audioData);
            Debug.Log($"[ButtonSoundPlayer] Event Channel로 사운드 재생: {audioData.name}");
            return;
        }

        // 우선순위 2: AudioData만 (직접 서비스 호출)
        if (audioData != null)
        {
            if (effectAudioService == null)
            {
                Debug.LogWarning("[ButtonSoundPlayer] EffectAudioService가 초기화되지 않았습니다.");
                return;
            }

            effectAudioService.PlayEffect(audioData);
            Debug.Log($"[ButtonSoundPlayer] AudioData로 사운드 재생: {audioData.name}");
            return;
        }

        Debug.LogWarning("[ButtonSoundPlayer] 재생할 사운드가 설정되지 않았습니다. Event Channel 또는 AudioData를 설정하세요.");
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