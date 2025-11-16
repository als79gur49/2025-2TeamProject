using System;
using UnityEngine;
using Game.Interfaces;

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
        /// Initializes the BaseManager with dependency injection
        /// Injects dependencies and initializes both Player and Enemy bases
        /// </summary>
        /// <param name="gridManager">Grid management service</param>
        void Init(IGridManager gridManager);

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
