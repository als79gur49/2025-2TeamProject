using UnityEngine;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    public class GameService : MonoBehaviour, IGameService
    {
        private ITurnService turnService;
        private IUnitService unitService;
        private IUIService uiService;
        
        public bool IsGameActive { get; private set; }
        
        public event System.Action OnGameStarted;
        public event System.Action OnGameEnded;
        
        private void Awake()
        {
            Debug.Log("[GameService] Awake() called - Registration handled by GameInitializer");
        }
        
        private void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            // Get dependencies from ServiceLocator
            turnService = ServiceLocator.Get<ITurnService>();
            unitService = ServiceLocator.Get<IUnitService>();
            uiService = ServiceLocator.Get<IUIService>();
            
            ValidateDependencies();
            SubscribeToEvents();
            
            // Auto-start game
            StartGame();
        }
        
        private void ValidateDependencies()
        {
            if (turnService == null)
                Debug.LogError("[GameService] ITurnService not found");
            if (unitService == null)
                Debug.LogError("[GameService] IUnitService not found");
            if (uiService == null)
                Debug.LogError("[GameService] IUIService not found");
        }
        
        private void SubscribeToEvents()
        {
            if (uiService != null)
            {
                uiService.OnEndTurnRequested += HandleEndTurnRequest;
                uiService.OnRestartRequested += RestartGame;
            }
        }
        
        public void StartGame()
        {
            Debug.Log("[GameService] Starting game...");
            
            IsGameActive = true;
            turnService?.StartGame();
            uiService?.UpdateDisplay();
            
            OnGameStarted?.Invoke();
        }
        
        public void RestartGame()
        {
            Debug.Log("[GameService] Restarting game...");
            StartGame();
        }
        
        public void Update()
        {
            // Handle any global game state updates
            HandleTestInput();
        }
        
        private void HandleEndTurnRequest()
        {
            if (!IsGameActive || turnService == null || unitService == null) return;
            
            Debug.Log("[GameService] Processing end turn request...");
            
            // Process units for current turn
            unitService.ProcessUnitsForCurrentPlayer(turnService.IsPlayerTurn);
            
            // End the turn
            turnService.EndTurn();
            
            // Update UI
            uiService?.UpdateDisplay();
        }
        
        private void HandleTestInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                HandleEndTurnRequest();
            }
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                unitService?.ProcessAllUnits();
            }
        }
    }
}