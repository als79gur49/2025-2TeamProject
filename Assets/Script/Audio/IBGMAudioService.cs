using System;
using UnityEngine;

/// <summary>
/// BGM 전용 오디오 서비스 인터페이스
/// AudioData ScriptableObject 기반 BGM 관리
/// </summary>
public interface IBGMAudioService : IAudioService
{
    /// <summary>
    /// 현재 재생 중인 BGM AudioData
    /// </summary>
    AudioData CurrentBGM { get; }

    /// <summary>
    /// 현재 재생 중인 BGM 이름 (하위 호환성용)
    /// </summary>
    string CurrentBGMName { get; }

    /// <summary>
    /// BGM이 재생 중인지 확인
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// BGM 재생 (AudioData 기반)
    /// </summary>
    /// <param name="audioData">재생할 AudioData</param>
    /// <returns>재생 성공 여부</returns>
    bool PlayBGM(AudioData audioData);

    /// <summary>
    /// BGM 정지
    /// </summary>
    void StopBGM();

    /// <summary>
    /// BGM 일시정지
    /// </summary>
    void PauseBGM();

    /// <summary>
    /// BGM 재개
    /// </summary>
    void ResumeBGM();

    /// <summary>
    /// BGM 페이드 인 (AudioData 기반)
    /// </summary>
    /// <param name="audioData">재생할 AudioData</param>
    /// <param name="fadeTime">페이드 시간(초)</param>
    void FadeInBGM(AudioData audioData, float fadeTime);

    /// <summary>
    /// BGM 페이드 아웃
    /// </summary>
    /// <param name="fadeTime">페이드 시간(초)</param>
    /// <param name="stopAfterFade">페이드 완료 후 정지 여부</param>
    void FadeOutBGM(float fadeTime = 1.0f, bool stopAfterFade = true);

    /// <summary>
    /// BGM 크로스페이드 (AudioData 기반)
    /// </summary>
    /// <param name="newAudioData">새로운 AudioData</param>
    /// <param name="crossFadeTime">크로스페이드 시간(초)</param>
    void CrossFadeBGM(AudioData newAudioData, float crossFadeTime);

    /// <summary>
    /// BGM 재생 완료 이벤트 (AudioData 기반)
    /// </summary>
    event Action<AudioData> OnBGMCompleted;

    /// <summary>
    /// BGM 페이드 완료 이벤트 (AudioData 기반)
    /// </summary>
    event Action<AudioData> OnFadeCompleted;
}
