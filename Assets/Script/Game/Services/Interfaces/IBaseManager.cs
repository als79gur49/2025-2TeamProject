using System;
using UnityEngine;

namespace Game.Services
{
    /// <summary>
    /// Service responsible for managing Base lifecycle and game-ending conditions
    /// Handles Base instantiation, placement, and destruction monitoring
    /// </summary>
    public interface IBaseManager
    {
        /// <summary>
        /// Gets the Player's Base instance
        /// </summary>
        Base PlayerBase { get; }

        /// <summary>
        /// Gets the Enemy's Base instance
        /// </summary>
        Base EnemyBase { get; }

        /// <summary>
        /// Initializes both Player and Enemy bases on the grid
        /// Should be called during game initialization phase
        /// </summary>
        void InitializeBases();

        /// <summary>
        /// Cleans up Base objects for game restart
        /// Destroys existing bases and prepares for new game
        /// </summary>
        void CleanupBases();

        /// <summary>
        /// Event fired when the Player's Base is destroyed (Game Loss condition)
        /// </summary>
        event Action OnPlayerBaseDestroyed;

        /// <summary>
        /// Event fired when the Enemy's Base is destroyed (Game Victory condition)
        /// </summary>
        event Action OnEnemyBaseDestroyed;
    }
}
