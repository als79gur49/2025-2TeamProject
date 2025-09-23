namespace Game.Services
{
    /// <summary>
    /// 전체 턴 사이클을 구성하는 네 가지 페이즈를 정의합니다.
    /// </summary>
    public enum TurnPhase
    {
        EnemySummon = 0,    // 적군 소환 턴
        AllySummon = 1,     // 아군 소환 턴  
        EnemyAction = 2,    // 적군 행동 턴
        AllyAction = 3      // 아군 행동 턴
    }

    /// <summary>
    /// 페이즈 실행 상태를 나타내는 열거형입니다.
    /// </summary>
    public enum PhaseExecutionState
    {
        Idle,           // 대기 상태
        Executing,      // 페이즈 실행 중
        Cancelling      // 취소 진행 중
    }
}