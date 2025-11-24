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
    /// 효과음 재생 (AudioPlayRequest 기반, runtime modifier 지원)
    /// </summary>
    /// <param name="request">재생 요청 객체</param>
    /// <returns>재생 성공 여부</returns>
    bool PlayEffect(AudioPlayRequest request);

    /// <summary>
    /// 루프 효과음 재생 (AudioData 기반)
    /// </summary>
    /// <param name="audioData">재생할 AudioData</param>
    /// <param name="owner">루프를 시작한 소유자 객체 (선택사항, Owner 기반 제어용)</param>
    /// <returns>루프 ID (정지용)</returns>
    int PlayEffectLoop(AudioData audioData, object owner = null);

    /// <summary>
    /// 루프 효과음 재생 (AudioPlayRequest 기반, runtime modifier 지원)
    /// </summary>
    /// <param name="request">재생 요청 객체</param>
    /// <returns>루프 ID (정지용)</returns>
    int PlayEffectLoop(AudioPlayRequest request);

    /// <summary>
    /// 루프 효과음 정지 (ID 기반)
    /// </summary>
    /// <param name="loopId">루프 ID</param>
    /// <param name="fadeTime">
    /// 페이드 아웃 시간 (초).
    /// 0 이하이면 즉시 정지 (기존 동작 유지),
    /// 0보다 크면 해당 시간 동안 볼륨을 서서히 줄인 뒤 정지합니다.
    /// </param>
    void StopEffectLoop(int loopId, float fadeTime = 0f);

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
    /// 루프 효과음 페이드 인 재생 (AudioData 기반)
    /// </summary>
    /// <param name="audioData">재생할 AudioData</param>
    /// <param name="fadeTime">
    /// 페이드 인 시간 (초).
    /// 0보다 크면 해당 시간 사용,
    /// 0 이하이면 AudioData.FadeInTime을 사용하며,
    /// 둘 다 0이면 기본값이 사용됩니다.
    /// </param>
    /// <param name="owner">루프를 시작한 소유자 객체 (선택사항, Owner 기반 제어용)</param>
    /// <returns>루프 ID (정지/페이드 아웃용)</returns>
    int FadeInEffectLoop(AudioData audioData, float fadeTime = -1f, object owner = null);

    /// <summary>
    /// 루프 효과음 페이드 인 재생 (AudioPlayRequest 기반)
    /// </summary>
    /// <param name="request">재생 요청 객체</param>
    /// <param name="fadeTime">
    /// 페이드 인 시간 (초).
    /// 0보다 크면 해당 시간 사용,
    /// 0 이하이면 AudioData.FadeInTime을 사용하며,
    /// 둘 다 0이면 기본값이 사용됩니다.
    /// </param>
    /// <returns>루프 ID (정지/페이드 아웃용)</returns>
    int FadeInEffectLoop(AudioPlayRequest request, float fadeTime = -1f);

    /// <summary>
    /// 루프 효과음 페이드 아웃 정지 (ID 기반)
    /// </summary>
    /// <param name="loopId">루프 ID</param>
    /// <param name="fadeTime">
    /// 페이드 아웃 시간 (초).
    /// 0보다 크면 해당 시간 사용,
    /// 0 이하이면 AudioData.FadeOutTime을 사용하며,
    /// 둘 다 0이면 기본값이 사용됩니다.
    /// </param>
    void FadeOutEffectLoop(int loopId, float fadeTime = -1f);

    /// <summary>
    /// [통합 메서드] 소유자 기반으로 루프 효과음을 페이드 아웃 시킵니다.
    /// audioDataToStop이 null이면 해당 소유자의 모든 루프를,
    /// 특정 AudioData가 주어지면 해당 사운드만 페이드 아웃합니다.
    /// </summary>
    /// <param name="owner">루프 사운드를 시작한 소유자 객체</param>
    /// <param name="fadeTime">
    /// 페이드 아웃 시간 (초).
    /// 0보다 크면 해당 시간 사용,
    /// 0 이하이면 각 루프의 AudioData.FadeOutTime을 사용하며,
    /// 둘 다 0이면 기본값이 사용됩니다.
    /// </param>
    /// <param name="audioDataToStop">페이드 아웃할 대상 AudioData (null일 경우 모두 대상)</param>
    void FadeOutLoopsByOwner(object owner, float fadeTime = -1f, AudioData audioDataToStop = null);

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
