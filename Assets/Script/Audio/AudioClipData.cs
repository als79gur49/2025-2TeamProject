using System;
using UnityEngine;

/// <summary>
/// 오디오 클립과 관련된 모든 데이터를 포함하는 구조체
/// Pair 클래스를 대체하며 더 많은 메타데이터를 포함
/// Unity Inspector에서 직렬화 가능
/// </summary>
[Serializable]
public struct AudioClipData
{
    [Header("기본 정보")]
    [SerializeField] public string clipName;
    [SerializeField] public AudioClip clip;
    
    [Header("재생 설정")]
    [Range(0f, 1f)]
    [SerializeField] public float defaultVolume;
    [Range(0.1f, 3f)]
    [SerializeField] public float defaultPitch;
    [SerializeField] public bool defaultLoop;
    
    [Header("분류 및 태그")]
    [SerializeField] public AudioType audioType;
    [SerializeField] public string[] tags;
    
    [Header("고급 설정")]
    [Range(0f, 1f)]
    [SerializeField] public float fadeInTime;
    [Range(0f, 1f)] 
    [SerializeField] public float fadeOutTime;
    [SerializeField] public int priority;
    [SerializeField] public bool spatialAudio;
    [SerializeField] public float maxDistance;
    
    /// <summary>
    /// 기본 생성자
    /// </summary>
    public AudioClipData(string name, AudioClip audioClip)
    {
        clipName = name;
        clip = audioClip;
        defaultVolume = 1.0f;
        defaultPitch = 1.0f;
        defaultLoop = false;
        audioType = AudioType.Effect;
        tags = new string[0];
        fadeInTime = 0f;
        fadeOutTime = 0f;
        priority = 128;
        spatialAudio = false;
        maxDistance = 500f;
    }
    
    /// <summary>
    /// 완전한 생성자
    /// </summary>
    public AudioClipData(string name, AudioClip audioClip, float volume, float pitch, 
                        bool loop, AudioType type = AudioType.Effect)
    {
        clipName = name;
        clip = audioClip;
        defaultVolume = volume;
        defaultPitch = pitch;
        defaultLoop = loop;
        audioType = type;
        tags = new string[0];
        fadeInTime = 0f;
        fadeOutTime = 0f;
        priority = 128;
        spatialAudio = false;
        maxDistance = 500f;
    }
    
    
    /// <summary>
    /// AudioPlayInfo로 변환 (런타임 재생용)
    /// </summary>
    public AudioPlayInfo ToAudioPlayInfo(Vector3 position = default, 
                                       float? volumeOverride = null, 
                                       float? pitchOverride = null,
                                       bool? loopOverride = null)
    {
        return new AudioPlayInfo(
            clipName,
            volumeOverride ?? defaultVolume,
            pitchOverride ?? defaultPitch,
            loopOverride ?? defaultLoop,
            position
        );
    }
    
    /// <summary>
    /// 유효성 검사
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(clipName) && clip != null;
    
    /// <summary>
    /// 특정 태그를 가지고 있는지 확인
    /// </summary>
    public bool HasTag(string tag)
    {
        return tags != null && Array.Exists(tags, t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public override string ToString()
    {
        return $"AudioClipData[{clipName}]: Vol={defaultVolume:F2}, Pitch={defaultPitch:F2}, Loop={defaultLoop}, Type={audioType}";
    }
}

/// <summary>
/// 오디오 타입 열거형 (BGM, 효과음 등을 구분)
/// </summary>
public enum AudioType
{
    BGM,
    Effect,
    Voice,
    Ambient,
    UI
}