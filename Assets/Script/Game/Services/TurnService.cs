using UnityEngine;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    public class TurnService : MonoBehaviour, ITurnService
    {
        [Header("Turn State")]
        [SerializeField] private TurnPhase currentPhase = TurnPhase.EnemySummon;
        [SerializeField] private int turnCount = 0;
        [SerializeField] private int phaseCount = 0;
        
        // 속성 구현
        public TurnPhase CurrentPhase => currentPhase;
        public int TurnCount => turnCount;
        public int PhaseCount => phaseCount;
        
        // 호환성
        public bool IsPlayerTurn => IsAllyPhase;
        
        // 페이즈 질의 속성
        public bool IsSummonPhase => currentPhase == TurnPhase.EnemySummon || currentPhase == TurnPhase.AllySummon;
        public bool IsActionPhase => currentPhase == TurnPhase.EnemyAction || currentPhase == TurnPhase.AllyAction;
        public bool IsEnemyPhase => currentPhase == TurnPhase.EnemySummon || currentPhase == TurnPhase.EnemyAction;
        public bool IsAllyPhase => currentPhase == TurnPhase.AllySummon || currentPhase == TurnPhase.AllyAction;
        
        // 이벤트
        public event System.Action<bool> OnTurnChanged;
        public event System.Action<int> OnTurnCountChanged;
        public event System.Action<TurnPhase> OnPhaseChanged;
        public event System.Action<int> OnPhaseCountChanged;
        
        private void Awake()
        {
        }
        
        public void StartGame()
        {
            currentPhase = TurnPhase.EnemySummon;
            turnCount = 0;
            phaseCount = 0;
            
            // 초기 이벤트 발생
            OnPhaseChanged?.Invoke(currentPhase);
            OnPhaseCountChanged?.Invoke(phaseCount);
            OnTurnChanged?.Invoke(IsPlayerTurn);
            OnTurnCountChanged?.Invoke(turnCount);
            
            Debug.Log($"[TurnService] Game started - Phase: {currentPhase}");
        }
        
        public void StartCurrentPhase()
        {
            Debug.Log($"[TurnService] Phase {currentPhase} started");
            // 페이즈별 초기화 로직 추가 가능
        }
        
        public void EndCurrentPhase()
        {
            var previousPhase = currentPhase;
            var wasPlayerTurn = IsPlayerTurn;
            
            // 다음 페이즈로 전환
            currentPhase = GetNextPhase(currentPhase);
            phaseCount++;
            
            // 4 페이즈마다 턴 카운트 증가
            if (phaseCount % 4 == 0)
            {
                turnCount++;
                OnTurnCountChanged?.Invoke(turnCount);
            }
            
            // 이벤트 발생
            OnPhaseChanged?.Invoke(currentPhase);
            OnPhaseCountChanged?.Invoke(phaseCount);
            
            // 플레이어 턴 상태가 변경되었을 때만 이벤트 발생
            if (wasPlayerTurn != IsPlayerTurn)
            {
                OnTurnChanged?.Invoke(IsPlayerTurn);
            }
            
            Debug.Log($"[TurnService] Phase changed: {previousPhase} → {currentPhase}");
        }
        
        private TurnPhase GetNextPhase(TurnPhase current)
        {
            return current switch
            {
                TurnPhase.EnemySummon => TurnPhase.AllySummon,
                TurnPhase.AllySummon => TurnPhase.EnemyAction,
                TurnPhase.EnemyAction => TurnPhase.AllyAction,
                TurnPhase.AllyAction => TurnPhase.EnemySummon,
                _ => TurnPhase.EnemySummon
            };
        }
    }
}