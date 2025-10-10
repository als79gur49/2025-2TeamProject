using System;
using UnityEngine;

/// <summary>
/// 효과음 전용 오디오 서비스 인터페이스
/// AudioData ScriptableObject 기반 효과음 관리
/// </summary>
public interface IEffectAudioService : IAudioService
{
    /// <summary>
    /// 현재 재생 중인 효과음 개수
    /// </summary>
    int ActiveEffectsCount { get; }

    /// <summary>
    /// 효과음 재생 (AudioData 기반)
    /// </summary>
    /// <param name="audioData">재생할 AudioData</param>
    /// <returns>재생 성공 여부</returns>
    bool PlayEffect(AudioData audioData);

    /// <summary>
    /// 루프 효과음 재생 (AudioData 기반)
    /// </summary>
    /// <param name="audioData">재생할 AudioData</param>
    /// <returns>루프 ID (정지용)</returns>
    int PlayEffectLoop(AudioData audioData);

    /// <summary>
    /// 루프 효과음 정지
    /// </summary>
    /// <param name="loopId">루프 ID</param>
    void StopEffectLoop(int loopId);

    /// <summary>
    /// 지연 효과음 재생 취소
    /// </summary>
    /// <param name="playId">재생 ID</param>
    void CancelDelayedEffect(int playId);

    /// <summary>
    /// 모든 효과음 정지
    /// </summary>
    void StopAllEffects();

    /// <summary>
    /// 효과음 풀 크기 설정
    /// </summary>
    /// <param name="poolSize">풀 크기</param>
    void SetPoolSize(int poolSize);

    /// <summary>
    /// 효과음 재생 완료 이벤트 (AudioData 기반)
    /// </summary>
    event Action<AudioData> OnEffectCompleted;

    /// <summary>
    /// 효과음 재생 시작 이벤트 (AudioData 기반)
    /// </summary>
    event Action<AudioData> OnEffectStarted;
}
