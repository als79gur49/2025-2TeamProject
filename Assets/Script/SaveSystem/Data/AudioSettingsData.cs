using System;

namespace Game.SaveSystem
{
    /// <summary>
    /// 오디오 설정 데이터
    /// 볼륨 레벨 및 뮤트 상태 저장
    /// </summary>
    [Serializable]
    public class AudioSettingsData
    {
        /// <summary>
        /// 마스터 볼륨 (-80 ~ 20 dB)
        /// </summary>
        public float masterVolume;

        /// <summary>
        /// BGM 볼륨 (-80 ~ 20 dB)
        /// </summary>
        public float bgmVolume;

        /// <summary>
        /// 효과음 볼륨 (-80 ~ 20 dB)
        /// </summary>
        public float effectVolume;

        /// <summary>
        /// 마스터 뮤트 상태
        /// </summary>
        public bool isMasterMuted;

        /// <summary>
        /// BGM 뮤트 상태
        /// </summary>
        public bool isBgmMuted;

        /// <summary>
        /// 효과음 뮤트 상태
        /// </summary>
        public bool isEffectMuted;

        /// <summary>
        /// 마지막 수정 시간
        /// </summary>
        public DateTime lastModified;

        /// <summary>
        /// 기본 생성자 - 기본 볼륨 설정 (dB 단위)
        /// 0dB = 정상 레벨, -5dB = 약간 작게
        /// </summary>
        public AudioSettingsData()
        {
            masterVolume = 0.0f;   // 0dB (정상 레벨)
            bgmVolume = -5.0f;     // -5dB (약간 작게)
            effectVolume = 0.0f;   // 0dB (정상 레벨)
            isMasterMuted = false;
            isBgmMuted = false;
            isEffectMuted = false;
            lastModified = DateTime.Now;
        }
    }
}
