using System;

namespace Game.Services
{
    /// <summary>
    /// Service responsible for managing overall game state and coordination
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// Gets whether the game is currently active
        /// </summary>
        bool IsGameActive { get; }
        
        /// <summary>
        /// Injects dependencies required by the GameService
        /// </summary>
        /// <param name="turnService">Turn management service</param>
        /// <param name="unitService">Unit management service</param>
        /// <param name="uiService">UI management service</param>
        void InjectDependencies(ITurnService turnService, IUnitService unitService, IUIService uiService);
        
        /// <summary>
        /// Initializes the service
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Starts a new game
        /// </summary>
        void StartGame();
        
        /// <summary>
        /// Restarts the current game
        /// </summary>
        void RestartGame();
        
        /// <summary>
        /// Updates the service state (called per frame)
        /// </summary>
        void Update();
        
        /// <summary>
        /// Event fired when a game is started
        /// </summary>
        event Action OnGameStarted;
        
        /// <summary>
        /// Event fired when a game is ended
        /// </summary>
        event Action OnGameEnded;
    }
}