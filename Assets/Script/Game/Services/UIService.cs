using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    /// <summary>
    /// Refactored UIService - UI management without ServiceLocator dependencies
    /// Focuses on UI creation, updates, and user interaction handling
    /// </summary>
    public class UIService : MonoBehaviour, IUIService
    {
        [Header("UI Elements")]
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Text turnStatusText;
        [SerializeField] private Canvas gameCanvas;
        
        // 💉 Injected Dependencies - No more ServiceLocator
        private ITurnService turnService;
        private IUnitService unitService;
        private IGlobalStateManager _stateManager;

        // 📡 Events
        public event System.Action OnEndTurnRequested;
        public event System.Action OnRestartRequested;

        // 🔒 Global state tracking for visual feedback
        private bool _isGameFlowLocked = false;
        
        private void Awake()
        {
            Debug.Log("[UIService] Awake() called - Registration handled by GameInitializer");
        }
        
        private void Start()
        {
            // Initialization now handled manually by GameServiceManager
        }

        /// <summary>
        /// Manual initialization with dependency injection - called by GameServiceManager
        /// Injects dependencies, retrieves ServiceLocator dependencies, and initializes UI
        /// </summary>
        public void Init(ITurnService turnService, IUnitService unitService)
        {
            // Phase 1: Inject dependencies from parameters
            this.turnService = turnService;
            this.unitService = unitService;

            // Phase 2: Get ServiceLocator dependencies
            _stateManager = ServiceLocator.Get<IGlobalStateManager>();
            if (_stateManager == null)
            {
                Debug.LogWarning("[UIService] IGlobalStateManager not found - VFX visual feedback disabled");
            }

            // Phase 3: Validate and initialize
            ValidateDependencies();
            SubscribeToServiceEvents();
            CreateUIElements();
            UpdateDisplay();

            Debug.Log("[UIService] Initialized successfully");
        }
        
        /// <summary>
        /// Validates that all required dependencies are available
        /// </summary>
        private void ValidateDependencies()
        {
            if (turnService == null)
                Debug.LogError("[UIService] ITurnService is null after injection");
            if (unitService == null)
                Debug.LogError("[UIService] IUnitService is null after injection");
            if (_stateManager == null)
                Debug.LogWarning("[UIService] IGlobalStateManager is null - VFX visual feedback disabled");
        }
        
        /// <summary>
        /// Subscribes to service events for UI updates
        /// </summary>
        private void SubscribeToServiceEvents()
        {
            if (turnService != null)
            {
                turnService.OnTurnChanged += HandleTurnChanged;
                turnService.OnTurnCountChanged += HandleTurnCountChanged;
                turnService.OnPhaseChanged += HandlePhaseChanged;
                turnService.OnPhaseCountChanged += HandlePhaseCountChanged;
            }

            // Phase 4: Subscribe to UnitService phase execution events
            if (unitService != null)
            {
                unitService.OnPhaseStarted += HandlePhaseStarted;
                unitService.OnPhaseCompleted += HandlePhaseCompleted;
                unitService.OnPhaseCancelled += HandlePhaseCancelled;
                unitService.OnUnitProcessed += HandleUnitProcessed;
            }

            // Subscribe to GlobalStateManager for VFX blocking visual feedback
            if (_stateManager != null)
            {
                _stateManager.OnBusyStateChanged += HandleGlobalBusyStateChanged;
            }
        }
        
        private void CreateUIElements()
        {
            if (gameCanvas == null) CreateCanvas();
            if (endTurnButton == null) CreateEndTurnButton();
            if (turnStatusText == null) CreateTurnStatusText();

            // Register button events after UI elements exist (created or assigned)
            RegisterButtonEvents();
        }

        /// <summary>
        /// Registers button event listeners - handles both auto-created and Inspector-assigned buttons
        /// </summary>
        private void RegisterButtonEvents()
        {
            if (endTurnButton != null)
            {
                // Remove any existing listeners to prevent duplicates
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
                Debug.Log("[UIService] EndTurnButton onClick event registered");
            }
            else
            {
                Debug.LogWarning("[UIService] Cannot register button events - endTurnButton is null");
            }
        }
        
        private void Update()
        {
            HandleKeyboardInput();
        }
        
        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnEndTurnRequested?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnRestartRequested?.Invoke();
            }
        }
        
        public void UpdateDisplay()
        {
            UpdateTurnStatusText();
            UpdateEndTurnButton();
        }
        
        public void ShowMessage(string message)
        {
            Debug.Log($"[UIService] Message: {message}");
            // Could extend to show actual UI message
        }
        
        public void SetEndTurnButtonEnabled(bool enabled)
        {
            if (endTurnButton != null)
                endTurnButton.interactable = enabled;
        }
        
        /// <summary>
        /// Triggers the end turn request event - Used for test input
        /// </summary>
        public void TriggerEndTurnRequest()
        {
            OnEndTurnRequested?.Invoke();
        }
        
        #region Test Support Methods
        
        /// <summary>
        /// Test method to trigger end turn request for unit testing
        /// </summary>
        public void TestEndTurnRequest()
        {
            OnEndTurnRequested?.Invoke();
        }
        
        /// <summary>
        /// Test method to trigger restart request for unit testing
        /// </summary>
        public void TestRestartRequest()
        {
            OnRestartRequested?.Invoke();
        }
        
        #endregion
        
        private void HandleTurnChanged(bool isPlayerTurn)
        {
            UpdateDisplay();
        }
        
        private void HandleTurnCountChanged(int turnCount)
        {
            UpdateDisplay();
        }
        
        private void HandlePhaseChanged(TurnPhase phase)
        {
            UpdateDisplay();
            Debug.Log($"[UIService] Phase changed to: {phase}");
        }
        
        private void HandlePhaseCountChanged(int phaseCount)
        {
            UpdateDisplay();
            Debug.Log($"[UIService] Phase count changed to: {phaseCount}");
        }

        /// <summary>
        /// Handles GlobalStateManager busy state changes for visual feedback
        /// </summary>
        private void HandleGlobalBusyStateChanged(BusyType type, bool isBusy)
        {
            if (type == BusyType.GameFlowLock)
            {
                _isGameFlowLocked = isBusy;
                UpdateDisplay(); // Immediately update UI to reflect lock state
                Debug.Log($"[UIService] GameFlowLock state changed: {isBusy} - UI updated");
            }
        }

        private void OnEndTurnButtonClicked()
        {
            OnEndTurnRequested?.Invoke();
        }
        
        private void CreateCanvas()
        {
            GameObject canvasObj = new GameObject("GameUI");
            gameCanvas = canvasObj.AddComponent<Canvas>();
            gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameCanvas.sortingOrder = 10;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        private void CreateEndTurnButton()
        {
            GameObject buttonObj = new GameObject("EndTurnButton");
            buttonObj.transform.SetParent(gameCanvas.transform);
            
            endTurnButton = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(150, 60);
            buttonRect.anchorMin = new Vector2(1, 0);
            buttonRect.anchorMax = new Vector2(1, 0);
            buttonRect.anchoredPosition = new Vector2(-100, 80);
            
            GameObject textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(buttonObj.transform);
            
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = "End Turn";
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 18;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = Vector2.zero;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Event registration now handled by RegisterButtonEvents()
        }
        
        private void CreateTurnStatusText()
        {
            GameObject textObj = new GameObject("TurnStatusText");
            textObj.transform.SetParent(gameCanvas.transform);
            
            turnStatusText = textObj.AddComponent<Text>();
            turnStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            turnStatusText.fontSize = 24;
            turnStatusText.alignment = TextAnchor.MiddleCenter;
            turnStatusText.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(300, 100);
            textRect.anchorMin = new Vector2(0.5f, 1);
            textRect.anchorMax = new Vector2(0.5f, 1);
            textRect.anchoredPosition = new Vector2(0, -50);
            
            GameObject backgroundObj = new GameObject("TextBackground");
            backgroundObj.transform.SetParent(textObj.transform);
            
            Image backgroundImage = backgroundObj.AddComponent<Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.5f);
            
            RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(-10, -5);
            bgRect.offsetMax = new Vector2(10, 5);
            
            backgroundObj.transform.SetSiblingIndex(0);
        }
        
        
        #region Event Cleanup
        
        /// <summary>
        /// Clean up event subscriptions on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (turnService != null)
            {
                turnService.OnTurnChanged -= HandleTurnChanged;
                turnService.OnTurnCountChanged -= HandleTurnCountChanged;
                turnService.OnPhaseChanged -= HandlePhaseChanged;
                turnService.OnPhaseCountChanged -= HandlePhaseCountChanged;
            }

            // Phase 4: Unsubscribe from UnitService phase execution events
            if (unitService != null)
            {
                unitService.OnPhaseStarted -= HandlePhaseStarted;
                unitService.OnPhaseCompleted -= HandlePhaseCompleted;
                unitService.OnPhaseCancelled -= HandlePhaseCancelled;
                unitService.OnUnitProcessed -= HandleUnitProcessed;
            }

            // Unsubscribe from GlobalStateManager
            if (_stateManager != null)
            {
                _stateManager.OnBusyStateChanged -= HandleGlobalBusyStateChanged;
            }
        }
        
        #endregion
        
        private void UpdateTurnStatusText()
        {
            if (turnStatusText == null || turnService == null) return;
            
            string phaseText = GetPhaseDisplayText(turnService.CurrentPhase);
            string cycleInfo = $"Cycle {turnService.TurnCount + 1} | Phase {(int)turnService.CurrentPhase + 1}/6";
            turnStatusText.text = $"{cycleInfo}\n{phaseText}";
            
            // Set color based on current phase
            turnStatusText.color = GetPhaseColor(turnService.CurrentPhase);
        }
        
        private string GetPhaseDisplayText(TurnPhase phase)
        {
            return phase switch
            {
                TurnPhase.TurnStart => "Turn Start Phase",
                TurnPhase.EnemySummon => "Enemy Summon Phase",
                TurnPhase.AllySummon => "Ally Summon Phase", 
                TurnPhase.EnemyAction => "Enemy Action Phase",
                TurnPhase.AllyAction => "Ally Action Phase",
                TurnPhase.TurnEnd => "Turn End Phase",
                _ => "Unknown Phase"
            };
        }
        
        private Color GetPhaseColor(TurnPhase phase)
        {
            return phase switch
            {
                TurnPhase.TurnStart => new Color(1f, 1f, 0.4f),        // Yellow
                TurnPhase.EnemySummon => new Color(1f, 0.4f, 0.4f),    // Light red
                TurnPhase.AllySummon => new Color(0.4f, 0.8f, 1f),     // Light blue
                TurnPhase.EnemyAction => new Color(0.9f, 0.2f, 0.2f),  // Dark red
                TurnPhase.AllyAction => new Color(0.2f, 0.9f, 0.2f),   // Green
                TurnPhase.TurnEnd => new Color(0.6f, 0.6f, 0.6f),      // Gray
                _ => Color.white
            };
        }
        
        private void UpdateEndTurnButton()
        {
            if (endTurnButton == null || turnService == null) return;

            Text buttonText = endTurnButton.GetComponentInChildren<Text>();
            Image buttonImage = endTurnButton.GetComponent<Image>();

            string buttonLabel = GetPhaseButtonText(turnService.CurrentPhase);
            Color buttonColor = GetPhaseButtonColor(turnService.CurrentPhase);

            buttonText.text = buttonLabel;
            buttonImage.color = buttonColor;

            // Phase 4: Disable button during phase execution to prevent user interference
            bool isPhaseExecuting = unitService != null && unitService.IsPhaseExecuting;

            // Check if GameFlowLock is active (VFX playing)
            bool isBlocked = isPhaseExecuting || _isGameFlowLocked;
            endTurnButton.interactable = !isBlocked;

            // Visual feedback for disabled state
            if (isBlocked)
            {
                buttonImage.color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f);
                if (buttonText != null)
                {
                    if (_isGameFlowLocked)
                        buttonText.text = "VFX Playing...";
                    else if (isPhaseExecuting)
                        buttonText.text = "Processing...";
                }
            }
        }
        
        private string GetPhaseButtonText(TurnPhase phase)
        {
            return phase switch
            {
                TurnPhase.TurnStart => "End Turn Start",
                TurnPhase.EnemySummon => "End Enemy Summon",
                TurnPhase.AllySummon => "End Ally Summon",
                TurnPhase.EnemyAction => "End Enemy Action",
                TurnPhase.AllyAction => "End Ally Action",
                TurnPhase.TurnEnd => "End Turn",
                _ => "End Phase"
            };
        }
        
        private Color GetPhaseButtonColor(TurnPhase phase)
        {
            return phase switch
            {
                TurnPhase.TurnStart => new Color(0.9f, 0.9f, 0.3f, 0.8f),   // Yellow-ish
                TurnPhase.EnemySummon => new Color(0.8f, 0.3f, 0.3f, 0.8f),  // Red-ish
                TurnPhase.AllySummon => new Color(0.3f, 0.7f, 0.9f, 0.8f),   // Blue-ish
                TurnPhase.EnemyAction => new Color(0.9f, 0.2f, 0.2f, 0.8f),  // Dark red
                TurnPhase.AllyAction => new Color(0.2f, 0.8f, 0.2f, 0.8f),   // Green
                TurnPhase.TurnEnd => new Color(0.6f, 0.6f, 0.6f, 0.8f),      // Gray-ish
                _ => new Color(0.5f, 0.5f, 0.5f, 0.8f)                       // Gray
            };
        }
        
        // Phase 4: New event handlers for phase execution state management
        
        /// <summary>
        /// Handles phase start events - disables end turn button to prevent user interference
        /// </summary>
        private void HandlePhaseStarted(TurnPhase phase)
        {
            Debug.Log($"[UIService] Phase {phase} started - disabling end turn button");
            UpdateEndTurnButton(); // This will now disable the button since IsPhaseExecuting is true
            
            // Update status text to show phase is executing
            if (turnStatusText != null)
            {
                string phaseText = GetPhaseDisplayText(phase);
                string cycleInfo = turnService != null ? $"Cycle {turnService.TurnCount + 1} | Phase {(int)turnService.CurrentPhase + 1}/4" : "Processing...";
                turnStatusText.text = $"{cycleInfo}\n{phaseText} (Executing...)";
            }
        }
        
        /// <summary>
        /// Handles phase completion events - re-enables end turn button
        /// </summary>
        private void HandlePhaseCompleted(TurnPhase phase)
        {
            Debug.Log($"[UIService] Phase {phase} completed - re-enabling end turn button");
            UpdateEndTurnButton(); // This will now enable the button since IsPhaseExecuting is false
            UpdateTurnStatusText(); // Restore normal status display
        }
        
        /// <summary>
        /// Handles phase cancellation events - re-enables end turn button
        /// </summary>
        private void HandlePhaseCancelled(TurnPhase phase)
        {
            Debug.Log($"[UIService] Phase {phase} cancelled - re-enabling end turn button");
            UpdateEndTurnButton(); // This will now enable the button since IsPhaseExecuting is false
            UpdateTurnStatusText(); // Restore normal status display
        }
        
        /// <summary>
        /// Handles individual unit processing events - updates progress display
        /// </summary>
        private void HandleUnitProcessed(Unit unit, int currentIndex, int totalCount)
        {
            if (turnStatusText != null && unitService != null)
            {
                TurnPhase currentPhase = unitService.CurrentPhase ?? TurnPhase.TurnStart;
                string phaseText = GetPhaseDisplayText(currentPhase);
                string cycleInfo = turnService != null ? $"Cycle {turnService.TurnCount + 1} | Phase {(int)currentPhase + 1}/6" : "Processing...";
                string progressInfo = $"Processing {unit?.name}: {currentIndex}/{totalCount}";
                turnStatusText.text = $"{cycleInfo}\n{phaseText}\n{progressInfo}";
            }
        }
        
        // Phase 4: Additional UI helper methods for better phase state management
        
        /// <summary>
        /// Gets the current phase execution progress for UI display
        /// </summary>
        public float GetCurrentPhaseProgress()
        {
            return unitService?.GetPhaseProgress() ?? 0f;
        }
        
        /// <summary>
        /// Checks if we can safely end the current phase (no processing in progress)
        /// </summary>
        public bool CanEndCurrentPhase()
        {
            return unitService == null || !unitService.IsPhaseExecuting;
        }
        
        /// <summary>
        /// Enhanced end turn request that respects phase execution state
        /// </summary>
        public void TriggerSmartEndTurnRequest()
        {
            if (CanEndCurrentPhase())
            {
                OnEndTurnRequested?.Invoke();
            }
            else
            {
                Debug.Log("[UIService] Cannot end turn - phase execution in progress. Requesting immediate completion.");
                // Request immediate completion of current phase
                if (unitService != null && unitService.IsPhaseExecuting)
                {
                    unitService.CancelCurrentPhase(true); // Complete remaining actions instantly
                }
            }
        }
    }
}