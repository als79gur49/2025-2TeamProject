namespace Game.Services
{
    /// <summary>
    /// 시스템 블로킹 상태의 유형을 정의합니다.
    /// 각 타입은 다른 영역의 게임 흐름을 제어합니다.
    /// </summary>
    public enum BusyType
    {
        /// <summary>Busy 상태 없음 (기본값)</summary>
        None,

        /// <summary>
        /// 게임 흐름 전체 잠금 - 카드 플레이 및 턴 진행 모두 차단
        /// 사용 예: 스펠 VFX 재생, 긴 애니메이션, 컷신
        /// </summary>
        GameFlowLock,

        /// <summary>
        /// 입력만 잠금 - UI 상호작용 차단, 내부 로직은 진행 가능
        /// 사용 예: 로딩 화면, 튜토리얼 연출
        /// </summary>
        InputLock,

        /// <summary>
        /// 페이즈/턴 진행만 잠금 - 카드 플레이는 가능, 턴 종료 차단
        /// 사용 예: 유닛 처리 중, 페이즈 전환 애니메이션
        /// </summary>
        PhaseLock,

        /// <summary>
        /// 사망 애니메이션 재생 중 - 유닛 파괴 지연 및 행동 체인 일시정지
        /// 사용 예: 유닛 사망 시 애니메이션 완료까지 GameObject 유지, 다른 유닛 대기
        /// </summary>
        DeathAnimation
    }
}
