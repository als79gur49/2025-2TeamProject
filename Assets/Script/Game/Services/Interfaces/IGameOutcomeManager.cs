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
        /// Injects required dependencies
        /// Called by GameInitializer during service registration
        /// </summary>
        /// <param name="baseManager">BaseManager instance to observe</param>
        void InjectDependencies(IBaseManager baseManager);

        /// <summary>
        /// Initializes the GameOutcomeManager and subscribes to BaseManager events
        /// Must be called after InjectDependencies
        /// </summary>
        void Initialize();
    }
}
