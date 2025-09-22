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
}