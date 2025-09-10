using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음 전용 오디오 서비스 인터페이스
/// 다중 효과음 중첩 재생, 풀링, 3D 사운드 등 효과음 특화 기능 제공
/// </summary>
public interface IEffectAudioService : IAudioService
{
    /// <summary>
    /// 현재 재생 중인 효과음 개수
    /// </summary>
    int ActiveEffectsCount { get; }
    
    /// <summary>
    /// 효과음 재생
    /// </summary>
    /// <param name="clipName">재생할 효과음 클립명</param>
    /// <param name="volume">재생 볼륨 (0.0~1.0+)</param>
    /// <param name="pitch">재생 피치 (0.1~3.0, 기본값 1.0)</param>
    /// <returns>재생 성공 여부</returns>
    bool PlayEffect(string clipName, float volume = 1.0f, float pitch = 1.0f);
    
    /// <summary>
    /// 효과음 재생 (AudioClip 참조 반환)
    /// </summary>
    /// <param name="clipName">재생할 효과음 클립명</param>
    /// <param name="audioClip">재생된 AudioClip 참조</param>
    /// <param name="volume">재생 볼륨</param>
    /// <param name="pitch">재생 피치</param>
    /// <returns>재생 성공 여부</returns>
    bool PlayEffect(string clipName, out AudioClip audioClip, float volume = 1.0f, float pitch = 1.0f);
    
    /// <summary>
    /// 지연 효과음 재생
    /// </summary>
    /// <param name="clipName">재생할 효과음 클립명</param>
    /// <param name="delay">지연 시간(초)</param>
    /// <param name="volume">재생 볼륨</param>
    /// <param name="pitch">재생 피치</param>
    /// <returns>재생 ID (취소용)</returns>
    int PlayEffectDelayed(string clipName, float delay, float volume = 1.0f, float pitch = 1.0f);
    
    /// <summary>
    /// 3D 위치 기반 효과음 재생
    /// </summary>
    /// <param name="clipName">재생할 효과음 클립명</param>
    /// <param name="position">3D 위치</param>
    /// <param name="volume">재생 볼륨</param>
    /// <param name="minDistance">최소 거리</param>
    /// <param name="maxDistance">최대 거리</param>
    /// <returns>재생 성공 여부</returns>
    bool PlayEffect3D(string clipName, Vector3 position, float volume = 1.0f, 
                     float minDistance = 1.0f, float maxDistance = 500.0f);
    
    /// <summary>
    /// 루프 효과음 재생
    /// </summary>
    /// <param name="clipName">재생할 효과음 클립명</param>
    /// <param name="volume">재생 볼륨</param>
    /// <returns>루프 ID (정지용)</returns>
    int PlayEffectLoop(string clipName, float volume = 1.0f);
    
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
    /// 특정 클립명의 모든 효과음 정지
    /// </summary>
    /// <param name="clipName">정지할 클립명</param>
    void StopAllEffects(string clipName);
    
    /// <summary>
    /// 효과음 풀 크기 설정
    /// </summary>
    /// <param name="poolSize">풀 크기</param>
    void SetPoolSize(int poolSize);
    
    /// <summary>
    /// 현재 재생 중인 효과음 목록 조회
    /// </summary>
    /// <returns>재생 중인 효과음 클립명 목록</returns>
    IReadOnlyList<string> GetActiveEffects();
    
    /// <summary>
    /// 효과음 재생 완료 이벤트
    /// </summary>
    event Action<string> OnEffectCompleted;
    
    /// <summary>
    /// 효과음 재생 시작 이벤트
    /// </summary>
    event Action<string> OnEffectStarted;
}