using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 데미지 표시를 위한 불변 데이터 전송 객체 (DTO)
    /// Repository 패턴을 통해 데이터 정제 후 UI로 전달됩니다.
    /// </summary>
    public readonly struct DamageDisplayData
    {
        /// <summary>표시할 데미지 양 (검증 및 정제된 값)</summary>
        public readonly int Amount;

        /// <summary>크리티컬 히트 여부</summary>
        public readonly bool IsCritical;

        /// <summary>데미지가 발생한 월드 좌표</summary>
        public readonly Vector3 WorldPosition;

        /// <summary>
        /// 데미지 표시 데이터 생성자
        /// </summary>
        /// <param name="amount">데미지 양 (음수인 경우 0으로 보정)</param>
        /// <param name="isCritical">크리티컬 히트 여부</param>
        /// <param name="worldPosition">데미지 발생 위치</param>
        public DamageDisplayData(int amount, bool isCritical, Vector3 worldPosition)
        {
            // 데이터 검증: 음수 데미지는 0으로 처리
            Amount = Mathf.Max(0, amount);
            IsCritical = isCritical;
            WorldPosition = worldPosition;
        }

        /// <summary>
        /// 디버깅용 문자열 표현
        /// </summary>
        public override string ToString() =>
            $"Damage: {Amount} {(IsCritical ? "(CRITICAL)" : "")} at {WorldPosition}";
    }
}
