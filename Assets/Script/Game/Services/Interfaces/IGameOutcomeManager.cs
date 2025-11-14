using System;

namespace Game.Services
{
    /// <summary>
    /// Service responsible for managing game victory and defeat conditions
    /// Observes BaseManager events and broadcasts game outcome to subscribers
    /// </summary>
    public interface IGameOutcomeManager
    {
        /// <summary>
        /// Event fired when victory conditions are met (Enemy Base destroyed)
        /// </summary>
        event Action OnVictory;

        /// <summary>
        /// Event fired when defeat conditions are met (Player Base destroyed)
        /// </summary>
        event Action OnDefeat;

        /// <summary>
        /// Initializes the GameOutcomeManager by retrieving dependencies from ServiceLocator
        /// and subscribing to BaseManager events
        /// </summary>
        void Initialize();
    }
}
