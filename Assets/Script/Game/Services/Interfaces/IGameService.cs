using System;

namespace Game.Services
{
    public interface IGameService
    {
        bool IsGameActive { get; }
        void Initialize();
        void StartGame();
        void RestartGame();
        void Update();
        
        event Action OnGameStarted;
        event Action OnGameEnded;
    }
}