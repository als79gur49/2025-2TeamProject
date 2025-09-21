using UnityEngine;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    public class TurnService : MonoBehaviour, ITurnService
    {
        [SerializeField] private bool isPlayerTurn = true;
        private int turnCount = 0;
        
        public bool IsPlayerTurn => isPlayerTurn;
        public int TurnCount => turnCount;
        
        public event System.Action<bool> OnTurnChanged;
        public event System.Action<int> OnTurnCountChanged;
        
        private void Awake()
        {
        }
        
        public void StartGame()
        {
            isPlayerTurn = true;
            turnCount = 0;
            OnTurnChanged?.Invoke(isPlayerTurn);
            OnTurnCountChanged?.Invoke(turnCount);
        }
        
        public void StartTurn()
        {
            Debug.Log($"[TurnService] Turn {turnCount + 1} Started - {(isPlayerTurn ? "Player" : "Enemy")}");
        }
        
        public void EndTurn()
        {
            isPlayerTurn = !isPlayerTurn;
            turnCount++;
            
            Debug.Log($"[TurnService] Turn {turnCount} Complete - Next: {(isPlayerTurn ? "Player" : "Enemy")}");
            
            OnTurnChanged?.Invoke(isPlayerTurn);
            OnTurnCountChanged?.Invoke(turnCount);
        }
    }
}