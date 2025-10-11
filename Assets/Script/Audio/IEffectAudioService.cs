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
    /// <param name="owner">루프를 시작한 소유자 객체 (선택사항, Owner 기반 제어용)</param>
    /// <returns>루프 ID (정지용)</returns>
    int PlayEffectLoop(AudioData audioData, object owner = null);

    /// <summary>
    /// 루프 효과음 정지 (ID 기반)
    /// </summary>
    /// <param name="loopId">루프 ID</param>
    void StopEffectLoop(int loopId);

    /// <summary>
    /// [통합 메서드] 소유자 기반으로 루프 효과음을 정지시킵니다.
    /// audioDataToStop이 null이면 해당 소유자의 모든 루프를,
    /// 특정 AudioData가 주어지면 해당 사운드만 정지시킵니다.
    /// </summary>
    /// <param name="owner">루프 사운드를 시작한 소유자 객체</param>
    /// <param name="audioDataToStop">정지할 대상 AudioData (null일 경우 모두 정지)</param>
    void StopLoopsByOwner(object owner, AudioData audioDataToStop = null);

    /// <summary>
    /// [오버로드] 소유자의 모든 루프 효과음을 정지시킵니다.
    /// </summary>
    /// <param name="owner">루프 사운드를 시작한 소유자 객체</param>
    void StopAllLoopsByOwner(object owner);

    /// <summary>
    /// [오버로드] 소유자의 특정 AudioData에 해당하는 루프 효과음만 정지시킵니다.
    /// </summary>
    /// <param name="owner">루프 사운드를 시작한 소유자 객체</param>
    /// <param name="audioData">정지할 대상 AudioData</param>
    void StopSpecificLoopByOwner(object owner, AudioData audioData);

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
