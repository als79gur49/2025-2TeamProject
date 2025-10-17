using Game.AI;

namespace Game.Interfaces
{
    /// <summary>
    /// 적군 카드 핸드 뷰 인터페이스
    /// 적군의 카드를 화면에 표시 (View Only, 상호작용 없음)
    /// </summary>
    public interface IEnemyCardHandView
    {
        /// <summary>적군 카드 뷰가 초기화되었는지 여부</summary>
        bool IsInitialized { get; }

        /// <summary>현재 표시 중인 적군 카드 수</summary>
        int CardCount { get; }

        /// <summary>EnemyAIController와 함께 초기화</summary>
        /// <param name="enemyAI">적군 AI 컨트롤러</param>
        void Init(EnemyAIController enemyAI);

        /// <summary>적군 핸드 동기화 - EnemyAIController의 enemyHand와 UI를 동기화</summary>
        void RefreshEnemyHand();

        /// <summary>적군 카드 뷰 상태 정보 반환</summary>
        string GetStatus();
    }
}
