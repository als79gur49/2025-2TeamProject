using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// VFX 설정 데이터 (ScriptableObject)
    /// EffectData에 참조되어 VFX 정보를 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "New VFXData", menuName = "Game/VFX/VFXData")]
    public class VFXData : ScriptableObject
    {
        [Header("VFX Prefab")]
        [SerializeField] private GameObject vfxPrefab;

        [Header("Trigger Settings")]
        [SerializeField] [Range(0f, 1f)] private float triggerNormalizedTime = 0.7f;

        [Header("Duration Settings")]
        [Tooltip("자동으로 ParticleSystem/Animator에서 지속시간을 계산할지 여부")]
        [SerializeField] private bool useAutoDuration = true;

        [Tooltip("수동 지속시간 설정 (useAutoDuration=false일 때만 사용)")]
        [SerializeField] private float manualDuration = 2f;

        [SerializeField] private bool isLooping = false;

        [Header("Playback Settings")]
        [SerializeField] [Range(0.1f, 3f)] private float playbackSpeed = 1.0f;

        /// <summary>VFX 프리팹</summary>
        public GameObject VFXPrefab => vfxPrefab;

        /// <summary>트리거 발생 정규화 시간 (0.0 ~ 1.0)</summary>
        public float TriggerNormalizedTime => triggerNormalizedTime;

        /// <summary>자동 지속시간 계산 사용 여부</summary>
        public bool UseAutoDuration => useAutoDuration;

        /// <summary>수동 설정된 VFX 지속 시간 (초) - useAutoDuration=false일 때만 사용</summary>
        public float ManualDuration => manualDuration;

        /// <summary>루핑 VFX 여부</summary>
        public bool IsLooping => isLooping;

        /// <summary>VFX 재생 속도 배율 (0.1 ~ 3.0)</summary>
        public float PlaybackSpeed => playbackSpeed;

        /// <summary>
        /// 데이터 유효성 검증
        /// </summary>
        public bool IsValid()
        {
            if (vfxPrefab == null)
                return false;

            // 수동 Duration 모드일 때만 검증
            if (!useAutoDuration && manualDuration <= 0f)
                return false;

            return true;
        }

        public override string ToString()
        {
            string durationInfo = useAutoDuration ? "Auto" : $"{manualDuration}s";
            return $"VFXData[Prefab: {vfxPrefab?.name ?? "None"}, TriggerTime: {triggerNormalizedTime:F2}, Duration: {durationInfo}, Looping: {isLooping}]";
        }
    }
}
