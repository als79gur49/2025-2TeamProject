using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오디오 클립 저장소 인터페이스
/// 테스트 가능성을 위한 추상화 레이어
/// </summary>
public interface IAudioClipRepository
{
    /// <summary>
    /// 클립명으로 AudioClipData 가져오기
    /// </summary>
    AudioClipData GetClipData(string clipName);
    
    /// <summary>
    /// 클립명으로 AudioClip 가져오기 (빠른 접근)
    /// </summary>
    AudioClip GetClip(string clipName);
    
    /// <summary>
    /// 특정 타입의 모든 클립 가져오기
    /// </summary>
    IEnumerable<AudioClipData> GetClipsByType(AudioType audioType);
    
    /// <summary>
    /// 태그로 클립 검색
    /// </summary>
    IEnumerable<AudioClipData> GetClipsByTag(string tag);
    
    /// <summary>
    /// 모든 클립 가져오기
    /// </summary>
    IEnumerable<AudioClipData> GetAllClips();
    
    /// <summary>
    /// 클립이 존재하는지 확인
    /// </summary>
    bool HasClip(string clipName);
    
    /// <summary>
    /// 클립 추가 (런타임)
    /// </summary>
    void AddClip(AudioClipData clipData);
    
    /// <summary>
    /// 클립 제거 (런타임)
    /// </summary>
    bool RemoveClip(string clipName);
    
    /// <summary>
    /// 저장소 초기화 상태
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// 저장소 초기화
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 저장소 정리
    /// </summary>
    void Clear();
}