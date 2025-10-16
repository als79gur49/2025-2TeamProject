using System;

namespace Game.Services
{
    /// <summary>
    /// 전역 게임 상태 관리 인터페이스.
    /// 시스템 전체의 Busy/Idle 상태를 관리하고, 상태 변경 시 이벤트를 발생시킵니다.
    /// </summary>
    public interface IGlobalStateManager
    {
        /// <summary>
        /// 상태 변경 시 발생하는 이벤트.
        /// </summary>
        /// <param name="type">변경된 BusyType</param>
        /// <param name="isBusy">true: Busy 상태로 전환, false: Idle 상태로 전환</param>
        event Action<BusyType, bool> OnBusyStateChanged;

        /// <summary>
        /// 특정 BusyType에 대해 Busy 상태를 설정합니다.
        /// 여러 requester가 동시에 요청할 수 있으며, 모두 해제될 때까지 Busy 상태 유지.
        /// </summary>
        /// <param name="requester">상태를 요청하는 객체 (메모리 누수 방지용 추적)</param>
        /// <param name="type">설정할 BusyType</param>
        /// <param name="timeout">타임아웃 시간 (초) - 기본값 10초</param>
        void SetBusy(object requester, BusyType type, float timeout = 10f);

        /// <summary>
        /// 특정 BusyType에 대해 Idle 상태로 전환합니다.
        /// </summary>
        /// <param name="requester">상태를 해제하는 객체</param>
        /// <param name="type">해제할 BusyType</param>
        void SetIdle(object requester, BusyType type);

        /// <summary>
        /// 특정 BusyType이 현재 Busy 상태인지 확인합니다.
        /// </summary>
        /// <param name="type">확인할 BusyType</param>
        /// <returns>하나라도 요청자가 있으면 true, 없으면 false</returns>
        bool IsBusy(BusyType type);

        /// <summary>
        /// 어떤 BusyType이든 하나라도 Busy 상태인지 확인합니다.
        /// </summary>
        /// <returns>모든 타입이 Idle이면 false, 하나라도 Busy면 true</returns>
        bool IsSystemBusy();
    }
}
