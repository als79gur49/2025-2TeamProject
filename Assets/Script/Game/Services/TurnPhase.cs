namespace Game.Services
{
    /// <summary>
    /// 전체 턴 사이클을 구성하는 6단계 페이즈를 정의합니다.
    /// TurnStart -> EnemySummon -> AllySummon -> EnemyAction -> AllyAction -> TurnEnd
    /// </summary>
    public enum TurnPhase
    {
        TurnStart = 0,      // 턴 시작 (Cost 증가, 카드 드로우 등)
        EnemySummon = 1,    // 적군 소환 페이즈
        AllySummon = 2,     // 아군 소환 페이즈  
        EnemyAction = 3,    // 적군 행동 페이즈
        AllyAction = 4,     // 아군 행동 페이즈
        TurnEnd = 5         // 턴 종료 (정보 수정, 효과 정리 등)
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