using System;
using UnityEngine;

/// <summary>
/// 오디오 서비스 전체에서 사용되는 이벤트 및 데이터 구조체 정의
/// </summary>

/// <summary>
/// 오디오 재생 정보 구조체
/// AudioData 기반으로 마이그레이션됨
/// </summary>
[Serializable]
public struct AudioPlayInfo
{
    public AudioData audioData;
    public float volume;
    public float pitch;
    public bool loop;
    public Vector3 position;
    public float startTime;

    public AudioPlayInfo(AudioData audioData, float volume = 1.0f, float pitch = 1.0f,
                        bool loop = false, Vector3 position = default)
    {
        this.audioData = audioData;
        this.volume = volume;
        this.pitch = pitch;
        this.loop = loop;
        this.position = position;
        this.startTime = Time.time;
    }

    /// <summary>
    /// 클립 이름 반환 (하위 호환성용)
    /// </summary>
    public string ClipName => audioData != null ? audioData.name : "None";
}

/// <summary>
/// 오디오 페이드 정보 구조체
/// AudioData 기반으로 마이그레이션됨
/// </summary>
[Serializable]
public struct AudioFadeInfo
{
    public AudioData audioData;
    public float fadeTime;
    public float targetVolume;
    public bool stopAfterFade;

    public AudioFadeInfo(AudioData audioData, float fadeTime, float targetVolume = 1.0f, bool stopAfterFade = false)
    {
        this.audioData = audioData;
        this.fadeTime = fadeTime;
        this.targetVolume = targetVolume;
        this.stopAfterFade = stopAfterFade;
    }

    /// <summary>
    /// 클립 이름 반환 (하위 호환성용)
    /// </summary>
    public string ClipName => audioData != null ? audioData.name : "None";
}

/// <summary>
/// 볼륨 설정 정보 구조체
/// </summary>
[Serializable]
public struct VolumeSettings : System.IEquatable<VolumeSettings>
{
    public float masterVolume;
    public float bgmVolume;
    public float effectVolume;
    public bool masterMuted;
    public bool bgmMuted;
    public bool effectMuted;
    
    public VolumeSettings(float master, float bgm, float effect, bool masterMute = false, bool bgmMute = false, bool effectMute = false)
    {
        masterVolume = master;
        bgmVolume = bgm;
        effectVolume = effect;
        masterMuted = masterMute;
        bgmMuted = bgmMute;
        effectMuted = effectMute;
    }
    
    public static VolumeSettings Default => new VolumeSettings
    {
        masterVolume = 0.0f,    // 0dB
        bgmVolume = 0.0f,       // 0dB
        effectVolume = 0.0f,    // 0dB
        masterMuted = false,
        bgmMuted = false,
        effectMuted = false
    };
    
    public bool Equals(VolumeSettings other)
    {
        return Mathf.Approximately(masterVolume, other.masterVolume) &&
               Mathf.Approximately(bgmVolume, other.bgmVolume) &&
               Mathf.Approximately(effectVolume, other.effectVolume) &&
               masterMuted == other.masterMuted &&
               bgmMuted == other.bgmMuted &&
               effectMuted == other.effectMuted;
    }
    
    public override bool Equals(object obj)
    {
        return obj is VolumeSettings other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return System.HashCode.Combine(masterVolume, bgmVolume, effectVolume, masterMuted, bgmMuted, effectMuted);
    }
    
    public static bool operator ==(VolumeSettings left, VolumeSettings right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(VolumeSettings left, VolumeSettings right)
    {
        return !left.Equals(right);
    }
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
    /// 오디오 재생 에러 이벤트 (AudioData 기반)
    /// </summary>
    public static event Action<AudioData, string> OnAudioPlayError;
    
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
    internal static void NotifyAudioPlayError(AudioData audioData, string error) => OnAudioPlayError?.Invoke(audioData, error);
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