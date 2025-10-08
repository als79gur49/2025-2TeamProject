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

        [Header("Duration Settings 수동 필수")]
        [SerializeField] private float duration = 2f;
        [SerializeField] private bool isLooping = false;

        /// <summary>VFX 프리팹</summary>
        public GameObject VFXPrefab => vfxPrefab;

        /// <summary>트리거 발생 정규화 시간 (0.0 ~ 1.0)</summary>
        public float TriggerNormalizedTime => triggerNormalizedTime;

        /// <summary>VFX 지속 시간 (초)</summary>
        public float Duration => duration;

        /// <summary>루핑 VFX 여부</summary>
        public bool IsLooping => isLooping;

        /// <summary>
        /// 데이터 유효성 검증
        /// </summary>
        public bool IsValid()
        {
            return vfxPrefab != null && duration > 0f;
        }

        public override string ToString()
        {
            return $"VFXData[Prefab: {vfxPrefab?.name ?? "None"}, TriggerTime: {triggerNormalizedTime:F2}, Duration: {duration}s, Looping: {isLooping}]";
        }
    }
}
