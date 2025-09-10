using System;
using UnityEngine;

/// <summary>
/// 볼륨 제어 전용 인터페이스
/// 마스터, BGM, 효과음 볼륨 관리 및 설정 저장/로드
/// </summary>
public interface IVolumeController
{
    /// <summary>
    /// 마스터 볼륨 (모든 오디오에 영향)
    /// </summary>
    float MasterVolume { get; set; }
    
    /// <summary>
    /// BGM 볼륨
    /// </summary>
    float BGMVolume { get; set; }
    
    /// <summary>
    /// 효과음 볼륨
    /// </summary>
    float EffectVolume { get; set; }
    
    /// <summary>
    /// 마스터 음소거 상태
    /// </summary>
    bool IsMasterMuted { get; set; }
    
    /// <summary>
    /// BGM 음소거 상태
    /// </summary>
    bool IsBGMMuted { get; set; }
    
    /// <summary>
    /// 효과음 음소거 상태
    /// </summary>
    bool IsEffectMuted { get; set; }
    
    /// <summary>
    /// 볼륨 초기화 (기본값으로 복원)
    /// </summary>
    void InitializeVolume();
    
    /// <summary>
    /// 마스터 볼륨 설정 (UI 슬라이더용, 0~1 범위)
    /// </summary>
    /// <param name="normalizedValue">정규화된 볼륨값 (0~1)</param>
    void SetMasterVolumeNormalized(float normalizedValue);
    
    /// <summary>
    /// BGM 볼륨 설정 (UI 슬라이더용, 0~1 범위)
    /// </summary>
    /// <param name="normalizedValue">정규화된 볼륨값 (0~1)</param>
    void SetBGMVolumeNormalized(float normalizedValue);
    
    /// <summary>
    /// 효과음 볼륨 설정 (UI 슬라이더용, 0~1 범위)
    /// </summary>
    /// <param name="normalizedValue">정규화된 볼륨값 (0~1)</param>
    void SetEffectVolumeNormalized(float normalizedValue);
    
    /// <summary>
    /// 정규화된 마스터 볼륨 값 가져오기 (UI용, 0~1 범위)
    /// </summary>
    /// <returns>정규화된 볼륨값</returns>
    float GetMasterVolumeNormalized();
    
    /// <summary>
    /// 정규화된 BGM 볼륨 값 가져오기 (UI용, 0~1 범위)
    /// </summary>
    /// <returns>정규화된 볼륨값</returns>
    float GetBGMVolumeNormalized();
    
    /// <summary>
    /// 정규화된 효과음 볼륨 값 가져오기 (UI용, 0~1 범위)
    /// </summary>
    /// <returns>정규화된 볼륨값</returns>
    float GetEffectVolumeNormalized();
    
    /// <summary>
    /// 모든 오디오 음소거/해제
    /// </summary>
    /// <param name="mute">음소거 여부</param>
    void MuteAll(bool mute);
    
    /// <summary>
    /// 볼륨 설정 저장
    /// </summary>
    void SaveVolumeSettings();
    
    /// <summary>
    /// 볼륨 설정 로드
    /// </summary>
    void LoadVolumeSettings();
    
    /// <summary>
    /// 볼륨 설정이 기본값인지 확인
    /// </summary>
    /// <returns>기본값 여부</returns>
    bool IsDefaultSettings();
    
    /// <summary>
    /// 볼륨 설정을 기본값으로 초기화
    /// </summary>
    void ResetToDefault();
    
    /// <summary>
    /// 볼륨 변경 이벤트
    /// </summary>
    event Action<VolumeType, float> OnVolumeChanged;
    
    /// <summary>
    /// 음소거 상태 변경 이벤트
    /// </summary>
    event Action<VolumeType, bool> OnMuteChanged;
}

/// <summary>
/// 볼륨 타입 열거형
/// </summary>
public enum VolumeType
{
    Master,
    BGM,
    Effect
}