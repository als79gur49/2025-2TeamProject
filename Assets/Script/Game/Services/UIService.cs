using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    public class UIService : MonoBehaviour, IUIService
    {
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Text turnStatusText;
        [SerializeField] private Canvas gameCanvas;
        
        private ITurnService turnService;
        private IUnitService unitService;
        
        public event System.Action OnEndTurnRequested;
        public event System.Action OnRestartRequested;
        
        private void Awake()
        {
            Debug.Log("[UIService] Awake() called - Registration handled by GameInitializer");
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
            
            if (turnService == null)
                Debug.LogError("[UIService] ITurnService not found in ServiceLocator");
            if (unitService == null)
                Debug.LogError("[UIService] IUnitService not found in ServiceLocator");
            
            // Subscribe to events
            if (turnService != null)
            {
                turnService.OnTurnChanged += HandleTurnChanged;
                turnService.OnTurnCountChanged += HandleTurnCountChanged;
            }
            
            CreateUIElements();
            UpdateDisplay();
        }
        
        private void CreateUIElements()
        {
            if (gameCanvas == null) CreateCanvas();
            if (endTurnButton == null) CreateEndTurnButton();
            if (turnStatusText == null) CreateTurnStatusText();
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
        
        private void HandleTurnChanged(bool isPlayerTurn)
        {
            UpdateDisplay();
        }
        
        private void HandleTurnCountChanged(int turnCount)
        {
            UpdateDisplay();
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
            
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
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
            textRect.sizeDelta = new Vector2(300, 50);
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
        
        private void UpdateTurnStatusText()
        {
            if (turnStatusText == null || turnService == null) return;
            
            string currentPlayer = turnService.IsPlayerTurn ? "Player" : "Enemy";
            turnStatusText.text = $"Turn {turnService.TurnCount}: {currentPlayer}'s Turn";
            
            if (turnService.IsPlayerTurn)
            {
                turnStatusText.color = Color.cyan;
            }
            else
            {
                turnStatusText.color = Color.red;
            }
        }
        
        private void UpdateEndTurnButton()
        {
            if (endTurnButton == null || turnService == null) return;
            
            Text buttonText = endTurnButton.GetComponentInChildren<Text>();
            Image buttonImage = endTurnButton.GetComponent<Image>();
            
            if (turnService.IsPlayerTurn)
            {
                buttonText.text = "End Player Turn";
                buttonImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
                endTurnButton.interactable = true;
            }
            else
            {
                buttonText.text = "End Enemy Turn";
                buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
                endTurnButton.interactable = true;
            }
        }
    }
}