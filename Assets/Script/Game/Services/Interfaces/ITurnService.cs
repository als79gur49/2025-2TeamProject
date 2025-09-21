using System;

namespace Game.Services
{
    public interface ITurnService
    {
        bool IsPlayerTurn { get; }
        int TurnCount { get; }
        
        void StartTurn();
        void EndTurn();
        void StartGame();
        
        event Action<bool> OnTurnChanged;
        event Action<int> OnTurnCountChanged;
    }
}