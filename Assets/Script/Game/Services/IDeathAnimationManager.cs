using UnityEngine;

namespace Game.Services
{
    /// <summary>
    /// 유닛 죽음 애니메이션 관리 인터페이스
    /// 유닛 사망 시 애니메이션을 재생하고 GameObject 파괴 타이밍을 제어합니다.
    /// </summary>
    public interface IDeathAnimationManager
    {
        /// <summary>
        /// 유닛 죽음 처리 요청
        /// 애니메이션 재생 후 자동으로 GameObject를 파괴합니다.
        /// </summary>
        /// <param name="unit">죽는 유닛</param>
        /// <param name="customDuration">커스텀 애니메이션 시간 (-1이면 기본값 사용)</param>
        void ProcessUnitDeath(Unit unit, float customDuration = -1f);

        /// <summary>
        /// 현재 죽음 애니메이션 처리 중인지 확인
        /// </summary>
        bool IsProcessingDeath { get; }

        /// <summary>
        /// 대기 중인 죽음 처리 개수
        /// </summary>
        int PendingDeathCount { get; }
    }
}
