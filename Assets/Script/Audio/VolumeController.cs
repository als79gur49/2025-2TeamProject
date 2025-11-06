using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 볼륨 제어 구현 클래스
/// Unity AudioMixer와 PlayerPrefs를 활용한 볼륨 설정 관리
/// </summary>
public class VolumeController : MonoBehaviour, IVolumeController
{
    [SerializeField] private AudioMixer audioMixer;
    
    // Unity Inspector에서 설정 가능한 기본값
    [Header("기본 볼륨 설정 (dB)")]
    [SerializeField] [Range(-80f, 20f)] private float defaultMasterVolume = 0f;
    [SerializeField] [Range(-80f, 20f)] private float defaultBGMVolume = 0f;
    [SerializeField] [Range(-80f, 20f)] private float defaultEffectVolume = 0f;
    
    [Header("PlayerPrefs 키 설정")]
    [SerializeField] private string masterVolumeKey = "AudioSettings_MasterVolume";
    [SerializeField] private string bgmVolumeKey = "AudioSettings_BGMVolume";
    [SerializeField] private string effectVolumeKey = "AudioSettings_EffectVolume";
    [SerializeField] private string masterMuteKey = "AudioSettings_MasterMute";
    [SerializeField] private string bgmMuteKey = "AudioSettings_BGMMute";
    [SerializeField] private string effectMuteKey = "AudioSettings_EffectMute";
    
    // 내부 볼륨 값 저장 (dB 단위)
    private float currentMasterVolume;
    private float currentBGMVolume;
    private float currentEffectVolume;
    
    // 음소거 상태
    private bool isMasterMuted;
    private bool isBGMMuted;
    private bool isEffectMuted;
    
    // 음소거 전 볼륨 백업
    private float masterVolumeBeforeMute;
    private float bgmVolumeBeforeMute;
    private float effectVolumeBeforeMute;

    // 초기화 상태 플래그 (중복 초기화 방지)
    private bool isVolumeInitialized = false;

    // AudioMixer 파라미터 이름
    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string EFFECT_VOLUME_PARAM = "EffectVolume";
    
    #region 인터페이스 프로퍼티 구현
    
    /// <summary>
    /// 마스터 볼륨 (dB 단위)
    /// Unity AudioMixer.SetFloat와 연동
    /// </summary>
    public float MasterVolume 
    { 
        get => currentMasterVolume;
        set
        {
            float oldValue = currentMasterVolume;
            currentMasterVolume = Mathf.Clamp(value, -80f, 20f);
            
            if (!isMasterMuted)
            {
                ApplyVolumeToMixer(MASTER_VOLUME_PARAM, currentMasterVolume);
            }
            
            OnVolumeChanged?.Invoke(VolumeType.Master, currentMasterVolume);
            AudioServiceEvents.NotifyGlobalVolumeChanged(VolumeType.Master, oldValue, currentMasterVolume);
        }
    }
    
    /// <summary>
    /// BGM 볼륨 (dB 단위)
    /// </summary>
    public float BGMVolume 
    { 
        get => currentBGMVolume;
        set
        {
            float oldValue = currentBGMVolume;
            currentBGMVolume = Mathf.Clamp(value, -80f, 20f);
            
            if (!isBGMMuted)
            {
                ApplyVolumeToMixer(BGM_VOLUME_PARAM, currentBGMVolume);
            }
            
            OnVolumeChanged?.Invoke(VolumeType.BGM, currentBGMVolume);
            AudioServiceEvents.NotifyGlobalVolumeChanged(VolumeType.BGM, oldValue, currentBGMVolume);
        }
    }
    
    /// <summary>
    /// 효과음 볼륨 (dB 단위)
    /// </summary>
    public float EffectVolume 
    { 
        get => currentEffectVolume;
        set
        {
            float oldValue = currentEffectVolume;
            currentEffectVolume = Mathf.Clamp(value, -80f, 20f);
            
            if (!isEffectMuted)
            {
                ApplyVolumeToMixer(EFFECT_VOLUME_PARAM, currentEffectVolume);
            }
            
            OnVolumeChanged?.Invoke(VolumeType.Effect, currentEffectVolume);
            AudioServiceEvents.NotifyGlobalVolumeChanged(VolumeType.Effect, oldValue, currentEffectVolume);
        }
    }
    
    /// <summary>
    /// 마스터 음소거 상태
    /// </summary>
    public bool IsMasterMuted 
    { 
        get => isMasterMuted;
        set
        {
            if (isMasterMuted != value)
            {
                isMasterMuted = value;
                
                if (isMasterMuted)
                {
                    masterVolumeBeforeMute = currentMasterVolume;
                    ApplyVolumeToMixer(MASTER_VOLUME_PARAM, -80f); // 완전 음소거
                }
                else
                {
                    ApplyVolumeToMixer(MASTER_VOLUME_PARAM, masterVolumeBeforeMute);
                }
                
                OnMuteChanged?.Invoke(VolumeType.Master, isMasterMuted);
            }
        }
    }
    
    /// <summary>
    /// BGM 음소거 상태
    /// </summary>
    public bool IsBGMMuted 
    { 
        get => isBGMMuted;
        set
        {
            if (isBGMMuted != value)
            {
                isBGMMuted = value;
                
                if (isBGMMuted)
                {
                    bgmVolumeBeforeMute = currentBGMVolume;
                    ApplyVolumeToMixer(BGM_VOLUME_PARAM, -80f);
                }
                else
                {
                    ApplyVolumeToMixer(BGM_VOLUME_PARAM, bgmVolumeBeforeMute);
                }
                
                OnMuteChanged?.Invoke(VolumeType.BGM, isBGMMuted);
            }
        }
    }
    
    /// <summary>
    /// 효과음 음소거 상태
    /// </summary>
    public bool IsEffectMuted 
    { 
        get => isEffectMuted;
        set
        {
            if (isEffectMuted != value)
            {
                isEffectMuted = value;
                
                if (isEffectMuted)
                {
                    effectVolumeBeforeMute = currentEffectVolume;
                    ApplyVolumeToMixer(EFFECT_VOLUME_PARAM, -80f);
                }
                else
                {
                    ApplyVolumeToMixer(EFFECT_VOLUME_PARAM, effectVolumeBeforeMute);
                }
                
                OnMuteChanged?.Invoke(VolumeType.Effect, isEffectMuted);
            }
        }
    }
    
    #endregion
    
    #region 이벤트 구현
    
    public event Action<VolumeType, float> OnVolumeChanged;
    public event Action<VolumeType, bool> OnMuteChanged;
    
    #endregion
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Unity Awake: AudioMixer 유효성 검사
    /// </summary>
    private void Awake()
    {
        
    }
    
    /// <summary>
    /// Unity Start: 저장된 볼륨 설정 로드
    /// </summary>
    private void Start()
    {
        if (audioMixer == null)
        {
            Debug.LogError("VolumeController: AudioMixer가 설정되지 않았습니다.");
        }
        //InitializeVolume();
    }
    
    /// <summary>
    /// Unity OnValidate: Inspector 값 변경 시 실시간 적용
    /// 개발 중 Inspector에서 볼륨 조정 가능
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && audioMixer != null)
        {
            // Inspector에서 변경된 기본값을 현재 값에 적용
            MasterVolume = defaultMasterVolume;
            BGMVolume = defaultBGMVolume;
            EffectVolume = defaultEffectVolume;
        }
    }
    
    #endregion
    
    #region IVolumeController 구현
    
    /// <summary>
    /// 볼륨 초기화 - 기본값 설정
    /// 실제 저장된 설정은 SaveDataAdapter가 자동으로 로드합니다.
    /// 중복 호출 시 자동으로 건너뜁니다 (멱등성 보장).
    /// </summary>
    public void InitializeVolume()
    {
        // 이미 초기화되었으면 건너뛰기 (중복 초기화 방지)
        if (isVolumeInitialized)
        {
            Debug.Log("VolumeController 이미 초기화됨 - 기본값 설정 건너뜀");
            Debug.Log("실제 설정은 SaveDataAdapter에서 로드된 상태입니다.");
            return;
        }

        try
        {
            // 기본값으로 초기화 (SaveDataAdapter가 나중에 실제 값을 로드함)
            currentMasterVolume = defaultMasterVolume;
            currentBGMVolume = defaultBGMVolume;
            currentEffectVolume = defaultEffectVolume;

            isMasterMuted = false;
            isBGMMuted = false;
            isEffectMuted = false;

            // AudioMixer에 기본값 적용
            ApplyAllVolumesToMixer();

            isVolumeInitialized = true;  // 초기화 완료 플래그 설정

            Debug.Log($"VolumeController 기본값 초기화 완료 - Master: {currentMasterVolume:F1}dB, BGM: {currentBGMVolume:F1}dB, Effect: {currentEffectVolume:F1}dB");
            Debug.Log("실제 설정은 SaveDataAdapter에서 자동으로 로드됩니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"VolumeController 초기화 실패: {ex.Message}");
            ResetToDefault();
        }
    }
    
    /// <summary>
    /// UI 슬라이더용 정규화된 마스터 볼륨 설정 (0~1)
    /// Unity UI Slider.value와 직접 연동 가능
    /// 로그 스케일 변환: 인간의 청각 특성 반영
    /// </summary>
    public void SetMasterVolumeNormalized(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        // 0~1 범위를 -80~20 dB로 변환 (로그 스케일)
        // 공식: dB = 20 * log10(value)
        float dbValue = normalizedValue > 0.0001f ?
            20f * Mathf.Log10(normalizedValue) :
            -80f; // 매우 작은 값이면 -80dB로 설정

        MasterVolume = dbValue;
    }
    
    /// <summary>
    /// UI 슬라이더용 정규화된 BGM 볼륨 설정 (0~1)
    /// 로그 스케일 변환: 인간의 청각 특성 반영
    /// </summary>
    public void SetBGMVolumeNormalized(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        // 로그 스케일 변환
        float dbValue = normalizedValue > 0.0001f ?
            20f * Mathf.Log10(normalizedValue) :
            -80f;

        BGMVolume = dbValue;
    }
    
    /// <summary>
    /// UI 슬라이더용 정규화된 효과음 볼륨 설정 (0~1)
    /// 로그 스케일 변환: 인간의 청각 특성 반영
    /// </summary>
    public void SetEffectVolumeNormalized(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        // 로그 스케일 변환
        float dbValue = normalizedValue > 0.0001f ?
            20f * Mathf.Log10(normalizedValue) :
            -80f;

        EffectVolume = dbValue;
    }
    
    /// <summary>
    /// 정규화된 마스터 볼륨 값 반환 (0~1)
    /// UI 슬라이더 초기화용
    /// </summary>
    public float GetMasterVolumeNormalized()
    {
        return DbToNormalized(currentMasterVolume);
    }
    
    /// <summary>
    /// 정규화된 BGM 볼륨 값 반환 (0~1)
    /// </summary>
    public float GetBGMVolumeNormalized()
    {
        return DbToNormalized(currentBGMVolume);
    }
    
    /// <summary>
    /// 정규화된 효과음 볼륨 값 반환 (0~1)
    /// </summary>
    public float GetEffectVolumeNormalized()
    {
        return DbToNormalized(currentEffectVolume);
    }
    
    /// <summary>
    /// 모든 오디오 음소거/해제
    /// 마스터 음소거를 통한 전체 제어
    /// </summary>
    public void MuteAll(bool mute)
    {
        IsMasterMuted = mute;
        Debug.Log($"전체 오디오 {(mute ? "음소거" : "음소거 해제")}");
    }
    
    /// <summary>
    /// 볼륨 설정이 기본값인지 확인
    /// </summary>
    public bool IsDefaultSettings()
    {
        return Mathf.Approximately(currentMasterVolume, defaultMasterVolume) &&
               Mathf.Approximately(currentBGMVolume, defaultBGMVolume) &&
               Mathf.Approximately(currentEffectVolume, defaultEffectVolume) &&
               !isMasterMuted && !isBGMMuted && !isEffectMuted;
    }
    
    /// <summary>
    /// 볼륨 설정을 기본값으로 초기화
    /// </summary>
    public void ResetToDefault()
    {
        Debug.Log("볼륨 설정을 기본값으로 초기화");

        currentMasterVolume = defaultMasterVolume;
        currentBGMVolume = defaultBGMVolume;
        currentEffectVolume = defaultEffectVolume;

        isMasterMuted = false;
        isBGMMuted = false;
        isEffectMuted = false;

        ApplyAllVolumesToMixer();

        // 리셋 후에도 초기화된 것으로 표시 (중복 초기화 방지)
        isVolumeInitialized = true;

        // 이벤트 알림
        OnVolumeChanged?.Invoke(VolumeType.Master, currentMasterVolume);
        OnVolumeChanged?.Invoke(VolumeType.BGM, currentBGMVolume);
        OnVolumeChanged?.Invoke(VolumeType.Effect, currentEffectVolume);

        OnMuteChanged?.Invoke(VolumeType.Master, isMasterMuted);
        OnMuteChanged?.Invoke(VolumeType.BGM, isBGMMuted);
        OnMuteChanged?.Invoke(VolumeType.Effect, isEffectMuted);
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// AudioMixer에 볼륨 적용
    /// Unity AudioMixer.SetFloat 사용
    /// </summary>
    private void ApplyVolumeToMixer(string parameterName, float dbValue)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning($"AudioMixer가 없어서 {parameterName} 볼륨을 적용할 수 없습니다.");
            return;
        }
        
        try
        {
            bool success = audioMixer.SetFloat(parameterName, dbValue);
            if (!success)
            {
                Debug.LogWarning($"AudioMixer 파라미터를 찾을 수 없습니다: {parameterName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"AudioMixer 볼륨 적용 실패 ({parameterName}): {ex.Message}");
        }
    }
    
    /// <summary>
    /// 모든 볼륨을 AudioMixer에 적용
    /// </summary>
    private void ApplyAllVolumesToMixer()
    {
        if (audioMixer == null) return;
        
        // 음소거 상태 고려하여 적용
        ApplyVolumeToMixer(MASTER_VOLUME_PARAM, isMasterMuted ? -80f : currentMasterVolume);
        ApplyVolumeToMixer(BGM_VOLUME_PARAM, isBGMMuted ? -80f : currentBGMVolume);
        ApplyVolumeToMixer(EFFECT_VOLUME_PARAM, isEffectMuted ? -80f : currentEffectVolume);
    }
    
    /// <summary>
    /// dB 값을 정규화된 값(0~1)으로 변환
    /// UI 표시용 (로그 스케일 역변환)
    /// </summary>
    private float DbToNormalized(float dbValue)
    {
        if (dbValue <= -79f) return 0f; // 거의 무음

        // 로그 스케일 역변환: 10^(dB/20)
        return Mathf.Pow(10f, dbValue / 20f);
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// 현재 볼륨 설정 정보 반환 (디버깅용)
    /// </summary>
    public VolumeSettings GetCurrentSettings()
    {
        return new VolumeSettings(
            currentMasterVolume,
            currentBGMVolume,
            currentEffectVolume,
            isMasterMuted,
            isBGMMuted,
            isEffectMuted
        );
    }
    
    /// <summary>
    /// 볼륨 설정 적용 (외부에서 VolumeSettings 구조체로 일괄 설정)
    /// </summary>
    public void ApplySettings(VolumeSettings settings)
    {
        MasterVolume = settings.masterVolume;
        BGMVolume = settings.bgmVolume;
        EffectVolume = settings.effectVolume;
        
        IsMasterMuted = settings.masterMuted;
        IsBGMMuted = settings.bgmMuted;
        IsEffectMuted = settings.effectMuted;
        
        Debug.Log("볼륨 설정 일괄 적용 완료");
    }
    
    /// <summary>
    /// Unity AudioMixer 파라미터 존재 여부 확인 (디버깅용)
    /// </summary>
    public bool ValidateAudioMixerParameters()
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioMixer가 설정되지 않았습니다.");
            return false;
        }
        
        bool allValid = true;
        string[] parameters = { MASTER_VOLUME_PARAM, BGM_VOLUME_PARAM, EFFECT_VOLUME_PARAM };
        
        foreach (string param in parameters)
        {
            float testValue;
            bool exists = audioMixer.GetFloat(param, out testValue);
            if (!exists)
            {
                Debug.LogError($"AudioMixer 파라미터를 찾을 수 없습니다: {param}");
                allValid = false;
            }
            else
            {
                Debug.Log($"AudioMixer 파라미터 확인됨: {param} = {testValue:F2}dB");
            }
        }
        
        return allValid;
    }
    
    #endregion
}