using System;

namespace Game.Services
{
    /// <summary>
    /// Service responsible for managing turn-based game flow
    /// </summary>
    public interface ITurnService
    {
        /// <summary>
        /// Gets whether it's currently the player's turn
        /// </summary>
        bool IsPlayerTurn { get; }
        
        /// <summary>
        /// Gets the current turn count
        /// </summary>
        int TurnCount { get; }
        
        /// <summary>
        /// Starts a new turn
        /// </summary>
        void StartTurn();
        
        /// <summary>
        /// Ends the current turn
        /// </summary>
        void EndTurn();
        
        /// <summary>
        /// Starts the game (resets turn state)
        /// </summary>
        void StartGame();
        
        /// <summary>
        /// Event fired when the turn changes between player and AI
        /// </summary>
        event Action<bool> OnTurnChanged;
        
        /// <summary>
        /// Event fired when the turn count changes
        /// </summary>
        event Action<int> OnTurnCountChanged;
    }
}