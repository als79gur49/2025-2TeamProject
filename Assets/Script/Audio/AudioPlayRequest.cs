using UnityEngine;

/// <summary>
/// 런타임 오디오 재생 요청을 표현하는 값 타입입니다.
/// AudioData ScriptableObject의 기본 설정은 유지하고,
/// 호출 시점에 volume/pitch 배율을 곱해 최종 재생 파라미터를 제어합니다.
/// </summary>
[System.Serializable]
public readonly struct AudioPlayRequest
{
    public readonly AudioData audioData;
    public readonly object owner;
    public readonly float volumeMultiplier;
    public readonly float pitchMultiplier;

    private AudioPlayRequest(
        AudioData data,
        object owner,
        float volumeMultiplier,
        float pitchMultiplier)
    {
        this.audioData = data;
        this.owner = owner;
        this.volumeMultiplier = volumeMultiplier;
        this.pitchMultiplier = pitchMultiplier;
    }

    /// <summary>
    /// AudioPlayRequest 생성 시작점입니다.
    /// volumeMultiplier, pitchMultiplier는 기본값 1.0으로 초기화됩니다.
    /// </summary>
    public static AudioPlayRequest Create(AudioData data, object owner = null)
    {
        return new AudioPlayRequest(data, owner, 1.0f, 1.0f);
    }

    /// <summary>
    /// 볼륨 배율을 설정한 새로운 요청을 반환합니다.
    /// </summary>
    public AudioPlayRequest WithVolume(float mult)
    {
        return new AudioPlayRequest(audioData, owner, mult, pitchMultiplier);
    }

    /// <summary>
    /// 피치 배율을 설정한 새로운 요청을 반환합니다.
    /// </summary>
    public AudioPlayRequest WithPitch(float mult)
    {
        return new AudioPlayRequest(audioData, owner, volumeMultiplier, mult);
    }
}

