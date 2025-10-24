using UnityEngine;

namespace Game.Repositories
{
    /// <summary>
    /// 데미지 표시를 위한 Repository 인터페이스
    /// Repository Pattern을 통해 데이터 검증 및 정제를 담당합니다.
    ///
    /// 책임:
    /// - 데미지 데이터 검증 (유효성 확인)
    /// - 데이터 정제 (클램핑, 포맷팅)
    /// - 비즈니스 규칙 적용 (최소/최대값 제한)
    /// - EventChannel을 통한 이벤트 발송
    /// </summary>
    public interface IDamageDisplayRepository
    {
        /// <summary>
        /// 데미지 표시 요청을 전송합니다.
        /// Repository가 데이터를 검증 및 정제한 후 EventChannel로 전달합니다.
        /// </summary>
        /// <param name="amount">데미지 양</param>
        /// <param name="isCritical">크리티컬 히트 여부</param>
        /// <param name="worldPosition">데미지 발생 위치 (월드 좌표)</param>
        void SendDamageDisplay(int amount, bool isCritical, Vector3 worldPosition);
    }
}
