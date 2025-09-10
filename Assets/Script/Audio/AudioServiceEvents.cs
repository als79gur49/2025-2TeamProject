using System;
using UnityEngine;

/// <summary>
/// 오디오 서비스 전체에서 사용되는 이벤트 및 데이터 구조체 정의
/// </summary>

/// <summary>
/// 오디오 재생 정보 구조체
/// </summary>
[Serializable]
public struct AudioPlayInfo
{
    public string clipName;
    public float volume;
    public float pitch;
    public bool loop;
    public Vector3 position;
    public float startTime;
    
    public AudioPlayInfo(string clipName, float volume = 1.0f, float pitch = 1.0f, 
                        bool loop = false, Vector3 position = default)
    {
        this.clipName = clipName;
        this.volume = volume;
        this.pitch = pitch;
        this.loop = loop;
        this.position = position;
        this.startTime = Time.time;
    }
}

/// <summary>
/// 오디오 페이드 정보 구조체
/// </summary>
[Serializable]
public struct AudioFadeInfo
{
    public string clipName;
    public float fadeTime;
    public float targetVolume;
    public bool stopAfterFade;
    
    public AudioFadeInfo(string clipName, float fadeTime, float targetVolume = 1.0f, bool stopAfterFade = false)
    {
        this.clipName = clipName;
        this.fadeTime = fadeTime;
        this.targetVolume = targetVolume;
        this.stopAfterFade = stopAfterFade;
    }
}

/// <summary>
/// 볼륨 설정 정보 구조체
/// </summary>
[Serializable]
public struct VolumeSettings
{
    public float masterVolume;
    public float bgmVolume;
    public float effectVolume;
    public bool masterMuted;
    public bool bgmMuted;
    public bool effectMuted;
    
    public static VolumeSettings Default => new VolumeSettings
    {
        masterVolume = 0.0f,    // 0dB
        bgmVolume = 0.0f,       // 0dB
        effectVolume = 0.0f,    // 0dB
        masterMuted = false,
        bgmMuted = false,
        effectMuted = false
    };
}

/// <summary>
/// 오디오 서비스 전역 이벤트
/// </summary>
public static class AudioServiceEvents
{
    /// <summary>
    /// 서비스 초기화 완료 이벤트
    /// </summary>
    public static event Action<Type> OnServiceInitialized;
    
    /// <summary>
    /// 서비스 정리 완료 이벤트
    /// </summary>
    public static event Action<Type> OnServiceCleaned;
    
    /// <summary>
    /// 오디오 재생 시작 이벤트
    /// </summary>
    public static event Action<AudioPlayInfo> OnAudioPlayStarted;
    
    /// <summary>
    /// 오디오 재생 완료 이벤트
    /// </summary>
    public static event Action<AudioPlayInfo> OnAudioPlayCompleted;
    
    /// <summary>
    /// 오디오 재생 에러 이벤트
    /// </summary>
    public static event Action<string, string> OnAudioPlayError;
    
    /// <summary>
    /// 볼륨 변경 이벤트
    /// </summary>
    public static event Action<VolumeType, float, float> OnGlobalVolumeChanged;
    
    /// <summary>
    /// 이벤트 발생 메소드들
    /// </summary>
    internal static void NotifyServiceInitialized(Type serviceType) => OnServiceInitialized?.Invoke(serviceType);
    internal static void NotifyServiceCleaned(Type serviceType) => OnServiceCleaned?.Invoke(serviceType);
    internal static void NotifyAudioPlayStarted(AudioPlayInfo playInfo) => OnAudioPlayStarted?.Invoke(playInfo);
    internal static void NotifyAudioPlayCompleted(AudioPlayInfo playInfo) => OnAudioPlayCompleted?.Invoke(playInfo);
    internal static void NotifyAudioPlayError(string clipName, string error) => OnAudioPlayError?.Invoke(clipName, error);
    internal static void NotifyGlobalVolumeChanged(VolumeType type, float oldValue, float newValue) => OnGlobalVolumeChanged?.Invoke(type, oldValue, newValue);
    
    /// <summary>
    /// 모든 이벤트 구독 해제 (테스트용)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnServiceInitialized = null;
        OnServiceCleaned = null;
        OnAudioPlayStarted = null;
        OnAudioPlayCompleted = null;
        OnAudioPlayError = null;
        OnGlobalVolumeChanged = null;
    }
}