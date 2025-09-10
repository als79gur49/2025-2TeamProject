using UnityEngine;

/// <summary>
/// 오디오 서비스의 기본 인터페이스
/// 모든 오디오 관련 서비스가 구현해야 하는 공통 기능 정의
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// 서비스가 초기화되었는지 확인
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// 서비스 초기화
    /// Unity 생명주기에 맞춘 비동기 초기화 지원
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 서비스 정리 및 리소스 해제
    /// </summary>
    void Cleanup();
    
    /// <summary>
    /// 지정된 클립명이 존재하는지 확인
    /// </summary>
    /// <param name="clipName">확인할 클립 이름</param>
    /// <returns>클립 존재 여부</returns>
    bool HasClip(string clipName);
}