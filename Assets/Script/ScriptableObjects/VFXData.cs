using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// VFX 설정 데이터 (ScriptableObject)
    /// EffectDefinition에 참조되어 VFX 정보를 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "New VFXData", menuName = "Game/VFX/VFXData")]
    public class VFXData : ScriptableObject
    {
        [Header("VFX Prefab")]
        [SerializeField] private GameObject vfxPrefab;

        [Header("Trigger Settings")]
        [SerializeField] [Range(0f, 1f)] private float triggerNormalizedTime = 0.7f;

        [Header("Playback Settings")]
        [SerializeField] [Range(0.1f, 3f)] private float playbackSpeed = 1.0f;

        [Header("Position Settings")]
        [Tooltip("VFX 생성 위치 오프셋 (시각적 위치 조정용)")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        /// <summary>VFX 프리팹</summary>
        public GameObject VFXPrefab => vfxPrefab;

        /// <summary>트리거 발생 정규화 시간 (0.0 ~ 1.0)</summary>
        public float TriggerNormalizedTime => triggerNormalizedTime;

        /// <summary>VFX 재생 속도 배율 (0.1 ~ 3.0)</summary>
        public float PlaybackSpeed => playbackSpeed;

        /// <summary>VFX 생성 위치 오프셋 (시각적 위치 조정용)</summary>
        public Vector3 PositionOffset => positionOffset;

        /// <summary>
        /// 데이터 유효성 검증
        /// </summary>
        public bool IsValid()
        {
            if (vfxPrefab == null)
                return false;

            return true;
        }

        public override string ToString()
        {
            string offsetInfo = positionOffset != Vector3.zero ? $", Offset: {positionOffset}" : "";
            return $"VFXData[Prefab: {vfxPrefab?.name ?? "None"}, TriggerTime: {triggerNormalizedTime:F2}, PlaybackSpeed: {playbackSpeed:F2}{offsetInfo}]";
        }
    }
}
