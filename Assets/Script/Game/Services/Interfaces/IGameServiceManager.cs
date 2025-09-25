using Game.Services;
using System;

namespace Game
{
    public interface IGameServiceManager
    {
        // Properties
        /// <summary>Gets whether the service manager is fully initialized</summary>
        bool IsInitialized { get; }
        
        /// <summary>Gets whether all services are healthy</summary>
        bool AreServicesHealthy { get; }
        
        // Events
        /// <summary>Event fired when turn state changes between player and AI</summary>
        public event Action<bool> OnTurnChanged;

        /// <summary>Event fired when turn count changes</summary>
        public event Action<int> OnTurnCountChanged;

        /// <summary>Event fired when phase changes</summary>
        public event Action<TurnPhase> OnPhaseChanged;

        /// <summary>Event fired when phase count changes</summary>
        public event Action<int> OnPhaseCountChanged;

        /// <summary>Event fired when a unit is registered</summary>
        public event Action<Unit> OnUnitRegistered;

        /// <summary>Event fired when a unit is unregistered</summary>
        public event Action<Unit> OnUnitUnregistered;

        /// <summary>Event fired when unit processing is completed</summary>
        public event Action OnUnitsProcessed;

        /// <summary>Event fired when a game is started</summary>
        public event Action OnGameStarted;

        /// <summary>Event fired when a game is ended</summary>
        public event Action OnGameEnded;

        /// <summary>Event fired when user requests to end turn</summary>
        public event Action OnEndTurnRequested;

        /// <summary>Event fired when user requests to restart game</summary>
        public event Action OnRestartRequested;

        /// <summary>Event fired when all services are initialized successfully</summary>
        public event Action OnServicesInitialized;

        /// <summary>Event fired when a service error occurs</summary>
        public event Action<string> OnServiceError;
        
        // Unit Registration API
        /// <summary>
        /// Registers a unit with the game service system
        /// </summary>
        /// <param name="unit">Unit to register</param>
        void RegisterUnit(Unit unit);
        
        /// <summary>
        /// Unregisters a unit from the game service system
        /// </summary>
        /// <param name="unit">Unit to unregister</param>
        void UnregisterUnit(Unit unit);

        // Service Access API
        /// <summary>턴 서비스 반환</summary>
        ITurnService GetTurnService();

        /// <summary>유닛 서비스 반환</summary>
        IUnitService GetUnitService();

        /// <summary>UI 서비스 반환</summary>
        IUIService GetUIService();

        /// <summary>게임 서비스 반환</summary>
        IGameService GetGameService();

        /// <summary>서비스 상태 정보 반환 (디버깅용)</summary>
        string GetServiceStatus();
    }
}
