using UnityEngine;
using Game.Services;
using Game.Core;
using System;

namespace Game.Services
{
    /// <summary>
    /// Enhanced GameServiceManager - Acts as a central coordinator for all game services
    /// Manages service lifecycle, dependency injection, and provides centralized event aggregation
    /// </summary>
    public class GameServiceManager : MonoBehaviour
    {
        [Header("Service Components")]
        [SerializeField] private TurnService turnService;
        [SerializeField] private UnitService unitService;
        [SerializeField] private UIService uiService;
        [SerializeField] private GameService gameService;
        [SerializeField] private TestInputHandler testInputHandler;
        
        [Header("Configuration")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool enableEventLogging = true;
        
        // 🎯 Public Event API - Centralized event aggregation
        #region Public Events
        
        /// <summary>Event fired when turn state changes between player and AI</summary>
        public event Action<bool> OnTurnChanged;
        
        /// <summary>Event fired when turn count changes</summary>
        public event Action<int> OnTurnCountChanged;
        
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
        
        #endregion
        
        // 🏗️ Service State Tracking
        #region Service State
        
        private bool isInitialized = false;
        private bool areServicesHealthy = false;
        private bool isDependencyInjectionComplete = false;
        private bool areEventsConnected = false;
        
        /// <summary>Gets whether the service manager is fully initialized</summary>
        public bool IsInitialized => isInitialized;
        
        /// <summary>Gets whether all services are healthy</summary>
        public bool AreServicesHealthy => areServicesHealthy;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (autoInitialize)
            {
                InitializeServices();
            }
        }
        
        private void OnDestroy()
        {
            DisconnectServiceEvents();
        }
        
        #endregion
        
        #region Initialization Pipeline
        
        /// <summary>
        /// Main initialization pipeline - coordinates the entire service setup process
        /// </summary>
        public void InitializeServices()
        {
            try
            {
                LogEvent("🏗️ Starting service initialization pipeline...");
                
                // Phase 1: Create service components
                CreateServiceComponents();
                LogEvent("✅ Service components created");
                
                // Phase 2: Register services with ServiceLocator
                RegisterServicesWithLocator();
                LogEvent("✅ Services registered with ServiceLocator");
                
                // Phase 3: Inject dependencies
                InjectServiceDependencies();
                LogEvent("✅ Dependencies injected");
                
                // Phase 4: Initialize individual services
                InitializeIndividualServices();
                LogEvent("✅ Individual services initialized");
                
                // Phase 5: Connect event system
                ConnectServiceEvents();
                LogEvent("✅ Event system connected");
                
                // Phase 6: Validate service health
                ValidateServiceHealth();
                LogEvent("✅ Service health validated");
                
                isInitialized = true;
                OnServicesInitialized?.Invoke();
                LogEvent("🎉 Service initialization pipeline completed successfully!");
            }
            catch (Exception ex)
            {
                string errorMessage = $"❌ Service initialization failed: {ex.Message}";
                LogEvent(errorMessage);
                OnServiceError?.Invoke(errorMessage);
                throw;
            }
        }
        
        /// <summary>
        /// Creates all required service components if they don't exist
        /// </summary>
        private void CreateServiceComponents()
        {
            if (turnService == null)
            {
                turnService = gameObject.AddComponent<TurnService>();
                LogEvent("🔄 Created TurnService component");
            }
            
            if (unitService == null)
            {
                unitService = gameObject.AddComponent<UnitService>();
                LogEvent("👥 Created UnitService component");
            }
            
            if (uiService == null)
            {
                uiService = gameObject.AddComponent<UIService>();
                LogEvent("🖼️ Created UIService component");
            }
            
            if (gameService == null)
            {
                gameService = gameObject.AddComponent<GameService>();
                LogEvent("🎮 Created GameService component");
            }
            
            if (testInputHandler == null)
            {
                testInputHandler = gameObject.AddComponent<TestInputHandler>();
                LogEvent("🎯 Created TestInputHandler component");
            }
        }
        
        /// <summary>
        /// Registers all services with the ServiceLocator
        /// </summary>
        private void RegisterServicesWithLocator()
        {
            // Register interfaces with their implementations
            ServiceLocator.Register<ITurnService>(turnService);
            ServiceLocator.Register<IUnitService>(unitService);
            ServiceLocator.Register<IUIService>(uiService);
            ServiceLocator.Register<IGameService>(gameService);
            
            LogEvent("📋 All services registered with ServiceLocator");
        }
        
        /// <summary>
        /// Injects dependencies into services that require them
        /// </summary>
        private void InjectServiceDependencies()
        {
            // Inject dependencies into GameService (requires all other services)
            gameService.InjectDependencies(turnService, unitService, uiService);
            LogEvent("💉 GameService dependencies injected");
            
            // Inject dependencies into UIService (requires TurnService and UnitService)
            uiService.InjectDependencies(turnService, unitService);
            LogEvent("💉 UIService dependencies injected");
            
            isDependencyInjectionComplete = true;
        }
        
        /// <summary>
        /// Initializes all individual services
        /// </summary>
        private void InitializeIndividualServices()
        {
            gameService.Initialize();
            uiService.Initialize();
            
            LogEvent("🚀 All services initialized individually");
        }
        
        /// <summary>
        /// Connects all service events to the central event aggregation system
        /// </summary>
        private void ConnectServiceEvents()
        {
            // TurnService events
            turnService.OnTurnChanged += HandleTurnChanged;
            turnService.OnTurnCountChanged += HandleTurnCountChanged;
            
            // UnitService events
            unitService.OnUnitRegistered += HandleUnitRegistered;
            unitService.OnUnitUnregistered += HandleUnitUnregistered;
            unitService.OnUnitsProcessed += HandleUnitsProcessed;
            
            // GameService events
            gameService.OnGameStarted += HandleGameStarted;
            gameService.OnGameEnded += HandleGameEnded;
            
            // UIService events
            uiService.OnEndTurnRequested += HandleEndTurnRequested;
            uiService.OnRestartRequested += HandleRestartRequested;
            
            areEventsConnected = true;
            LogEvent("🔗 All service events connected");
        }
        
        /// <summary>
        /// Validates that all services are in a healthy state
        /// </summary>
        private void ValidateServiceHealth()
        {
            bool allHealthy = true;
            
            if (turnService == null)
            {
                LogEvent("❌ TurnService is null");
                allHealthy = false;
            }
            
            if (unitService == null)
            {
                LogEvent("❌ UnitService is null");
                allHealthy = false;
            }
            
            if (uiService == null)
            {
                LogEvent("❌ UIService is null");
                allHealthy = false;
            }
            
            if (gameService == null)
            {
                LogEvent("❌ GameService is null");
                allHealthy = false;
            }
            
            if (testInputHandler == null)
            {
                LogEvent("❌ TestInputHandler is null");
                allHealthy = false;
            }
            
            if (!isDependencyInjectionComplete)
            {
                LogEvent("❌ Dependency injection not completed");
                allHealthy = false;
            }
            
            if (!areEventsConnected)
            {
                LogEvent("❌ Events not connected");
                allHealthy = false;
            }
            
            areServicesHealthy = allHealthy;
            
            if (allHealthy)
            {
                LogEvent("💚 All services are healthy");
            }
            else
            {
                LogEvent("💔 Some services are unhealthy");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleTurnChanged(bool isPlayerTurn)
        {
            LogEvent($"🔄 Turn changed: Player={isPlayerTurn}");
            OnTurnChanged?.Invoke(isPlayerTurn);
        }
        
        private void HandleTurnCountChanged(int turnCount)
        {
            LogEvent($"📊 Turn count changed: {turnCount}");
            OnTurnCountChanged?.Invoke(turnCount);
        }
        
        private void HandleUnitRegistered(Unit unit)
        {
            LogEvent($"👥 Unit registered: {unit?.name}");
            OnUnitRegistered?.Invoke(unit);
        }
        
        private void HandleUnitUnregistered(Unit unit)
        {
            LogEvent($"👥 Unit unregistered: {unit?.name}");
            OnUnitUnregistered?.Invoke(unit);
        }
        
        private void HandleUnitsProcessed()
        {
            LogEvent("⚙️ Units processed");
            OnUnitsProcessed?.Invoke();
        }
        
        private void HandleGameStarted()
        {
            LogEvent("🎮 Game started");
            OnGameStarted?.Invoke();
        }
        
        private void HandleGameEnded()
        {
            LogEvent("🏁 Game ended");
            OnGameEnded?.Invoke();
        }
        
        private void HandleEndTurnRequested()
        {
            LogEvent("🔚 End turn requested");
            OnEndTurnRequested?.Invoke();
        }
        
        private void HandleRestartRequested()
        {
            LogEvent("🔄 Restart requested");
            OnRestartRequested?.Invoke();
        }
        
        #endregion
        
        #region Event Cleanup
        
        /// <summary>
        /// Disconnects all service events to prevent memory leaks
        /// </summary>
        private void DisconnectServiceEvents()
        {
            if (areEventsConnected)
            {
                // TurnService events
                if (turnService != null)
                {
                    turnService.OnTurnChanged -= HandleTurnChanged;
                    turnService.OnTurnCountChanged -= HandleTurnCountChanged;
                }
                
                // UnitService events
                if (unitService != null)
                {
                    unitService.OnUnitRegistered -= HandleUnitRegistered;
                    unitService.OnUnitUnregistered -= HandleUnitUnregistered;
                    unitService.OnUnitsProcessed -= HandleUnitsProcessed;
                }
                
                // GameService events
                if (gameService != null)
                {
                    gameService.OnGameStarted -= HandleGameStarted;
                    gameService.OnGameEnded -= HandleGameEnded;
                }
                
                // UIService events
                if (uiService != null)
                {
                    uiService.OnEndTurnRequested -= HandleEndTurnRequested;
                    uiService.OnRestartRequested -= HandleRestartRequested;
                }
                
                areEventsConnected = false;
                LogEvent("🔗 All service events disconnected");
            }
        }
        
        #endregion
        
        #region Event Logging System
        
        /// <summary>
        /// Logs events with timestamp for debugging and monitoring
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogEvent(string message)
        {
            if (enableEventLogging)
            {
                Debug.Log($"[GameServiceManager] {System.DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Gets the current status of all services for debugging
        /// </summary>
        /// <returns>Formatted string with service status</returns>
        public string GetServiceStatus()
        {
            return $"Services Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Healthy: {areServicesHealthy}\n" +
                   $"- Dependencies Injected: {isDependencyInjectionComplete}\n" +
                   $"- Events Connected: {areEventsConnected}\n" +
                   $"- TurnService: {(turnService != null ? "✅" : "❌")}\n" +
                   $"- UnitService: {(unitService != null ? "✅" : "❌")}\n" +
                   $"- UIService: {(uiService != null ? "✅" : "❌")}\n" +
                   $"- GameService: {(gameService != null ? "✅" : "❌")}\n" +
                   $"- TestInputHandler: {(testInputHandler != null ? "✅" : "❌")}";
        }
        
        #endregion
    }
}