using System;

namespace Game.Services
{
    /// <summary>
    /// Service responsible for managing turn-based game flow
    /// </summary>
    public interface ITurnService
    {
        // 페이즈 관리 속성
        TurnPhase CurrentPhase { get; }
        int TurnCount { get; }          // 전체 턴 사이클 (4 페이즈마다 1 증가)
        int PhaseCount { get; }         // 진행된 총 페이즈 수
        
        // 호환성 속성
        bool IsPlayerTurn { get; }      // AllySummon & AllyAction 페이즈에서 true

        // 페이즈 제어 메서드
        void StartGame();               // EnemySummon 페이즈로 초기화
        void StartCurrentPhase();       // 현재 페이즈 처리 시작
        void EndCurrentPhase();         // 현재 페이즈 종료 및 다음 페이즈로 전환
        
        // 페이즈 질의 메서드
        bool IsSummonPhase { get; }     // 소환 페이즈인지 확인
        bool IsActionPhase { get; }     // 행동 페이즈인지 확인
        bool IsEnemyPhase { get; }      // 적의 페이즈인지 확인
        bool IsAllyPhase { get; }       // 아군의 페이즈인지 확인
        
        // 이벤트
        event Action<bool> OnTurnChanged;           // 호환용
        event Action<int> OnTurnCountChanged;       // 턴 사이클 변경 시
        event Action<TurnPhase> OnPhaseChanged;     // 신규: 페이즈 변경 시
        event Action<int> OnPhaseCountChanged;      // 신규: 페이즈 카운트 변경 시
    }
}