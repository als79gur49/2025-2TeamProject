using System;
using UnityEngine;

/// <summary>
/// BGM 전용 오디오 서비스 인터페이스
/// 단일 BGM 재생, 페이드, 루프 등 BGM 특화 기능 제공
/// </summary>
public interface IBGMAudioService : IAudioService
{
    /// <summary>
    /// 현재 재생 중인 BGM 정보
    /// </summary>
    string CurrentBGMName { get; }
    
    /// <summary>
    /// BGM이 재생 중인지 확인
    /// </summary>
    bool IsPlaying { get; }
    
    /// <summary>
    /// BGM 재생
    /// </summary>
    /// <param name="clipName">재생할 BGM 클립명</param>
    /// <param name="startRate">재생 시작 지점 (0.0~1.0)</param>
    /// <param name="loop">루프 재생 여부</param>
    /// <returns>재생 성공 여부</returns>
    bool PlayBGM(string clipName, float startRate = 0.0f, bool loop = true);
    
    /// <summary>
    /// BGM 재생 (AudioClip 참조 반환)
    /// </summary>
    /// <param name="clipName">재생할 BGM 클립명</param>
    /// <param name="audioClip">재생된 AudioClip 참조</param>
    /// <param name="startRate">재생 시작 지점 (0.0~1.0)</param>
    /// <param name="loop">루프 재생 여부</param>
    /// <returns>재생 성공 여부</returns>
    bool PlayBGM(string clipName, out AudioClip audioClip, float startRate = 0.0f, bool loop = true);
    
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
    /// BGM 페이드 인
    /// </summary>
    /// <param name="clipName">재생할 BGM 클립명</param>
    /// <param name="fadeTime">페이드 시간(초)</param>
    /// <param name="startRate">재생 시작 지점</param>
    void FadeInBGM(string clipName, float fadeTime = 1.0f, float startRate = 0.0f);
    
    /// <summary>
    /// BGM 페이드 아웃
    /// </summary>
    /// <param name="fadeTime">페이드 시간(초)</param>
    /// <param name="stopAfterFade">페이드 완료 후 정지 여부</param>
    void FadeOutBGM(float fadeTime = 1.0f, bool stopAfterFade = true);
    
    /// <summary>
    /// BGM 크로스페이드 (현재 BGM을 페이드 아웃하며 새 BGM을 페이드 인)
    /// </summary>
    /// <param name="newClipName">새로운 BGM 클립명</param>
    /// <param name="crossFadeTime">크로스페이드 시간(초)</param>
    void CrossFadeBGM(string newClipName, float crossFadeTime = 2.0f);
    
    /// <summary>
    /// BGM 재생 완료 이벤트
    /// </summary>
    event Action<string> OnBGMCompleted;
    
    /// <summary>
    /// BGM 페이드 완료 이벤트
    /// </summary>
    event Action<string> OnFadeCompleted;
}