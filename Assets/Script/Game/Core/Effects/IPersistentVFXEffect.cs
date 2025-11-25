using UnityEngine;

namespace Game.Core.Effects
{
    /// <summary>
    /// 지속형 상태 Effect가 VFX 시스템에 제공해야 하는 정보 인터페이스입니다.
    /// </summary>
    public enum VFXAnchorType
    {
        Default,
        Head,
        Body,
        Feet,
        Weapon
    }

    public interface IPersistentVFXEffect : IEffect
    {
        /// <summary>
        /// 전역 StatusVFXConfig에서 조회할 시각적 ID입니다. (예: "Stun", "Bleed")
        /// </summary>
        string GetPersistentVFXId();

        /// <summary>
        /// 이 인스턴스에서만 사용하는 커스텀 VFXData입니다. 없으면 null을 반환합니다.
        /// </summary>
        Game.VFX.VFXData GetVFXOverrideOrNull();

        /// <summary>
        /// 이 Effect의 기본 앵커 타입입니다. (Head/Body/Feet/Weapon 등)
        /// </summary>
        VFXAnchorType GetVFXAnchor();

        /// <summary>
        /// 전역 설정의 위치 오프셋 위에 추가로 적용할 개별 오프셋입니다.
        /// </summary>
        Vector3 GetVFXOffset();
    }
}

