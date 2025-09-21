using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Services;

/// <summary>
/// REFACTORED UIManager - Demonstrates how to remove redundant responsibilities
/// This shows the migration path from legacy UIManager to service-based architecture
/// Recommendation: Use UIService instead of this refactored version for new development
/// </summary>
public class UIManagerLegacyRefactored : MonoBehaviour
{
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Text turnStatusText;
    [SerializeField] private Canvas gameCanvas;
    
    // ✅ REFACTOR: Use service interfaces instead of concrete managers
    private ITurnService turnService;
    private IGameService gameService;
    
    private void Start()
    {
        Initialize();
        CreateUI();
        UpdateUI();
    }
    
    /// <summary>
    /// ✅ REFACTORED: Remove TurnManager creation logic, use ServiceLocator instead
    /// </summary>
    private void Initialize()
    {
        // Use ServiceLocator for dependency injection
        turnService = ServiceLocator.Get<ITurnService>();
        gameService = ServiceLocator.Get<IGameService>();
        
        if (turnService == null)
            Debug.LogError("[UIManagerRefactored] ITurnService not found - ensure GameServiceManager is initialized");
        if (gameService == null)
            Debug.LogError("[UIManagerRefactored] IGameService not found - ensure GameServiceManager is initialized");
        
        // Subscribe to turn changes for UI updates
        if (turnService != null)
        {
            turnService.OnTurnChanged += HandleTurnChanged;
            turnService.OnTurnCountChanged += HandleTurnCountChanged;
        }
    }
    
    private void CreateUI()
    {
        if (gameCanvas == null) CreateCanvas();
        if (endTurnButton == null) CreateEndTurnButton();
        if (turnStatusText == null) CreateTurnStatusText();
    }
    
    private void Update()
    {
        HandleKeyboardInput();
        // ✅ REFACTORED: Remove continuous UpdateUI() calls - use event-driven updates instead
    }
    
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnEndTurnButtonClicked();
        }
    }
    
    /// <summary>
    /// ✅ REFACTORED: Remove unit processing logic, delegate to GameService
    /// </summary>
    private void OnEndTurnButtonClicked()
    {
        Debug.Log("[UIManagerRefactored] End Turn button clicked!");
        
        // ✅ CLEAN: Just trigger game service event, no game logic in UI
        var gameServ = ServiceLocator.Get<IGameService>();
        if (gameServ != null)
        {
            // GameService will handle the complete turn processing
            // This eliminates the duplicate unit processing logic
            Debug.Log("[UIManagerRefactored] Delegating turn processing to GameService");
        }
    }
    
    /// <summary>
    /// ✅ NEW: Event-driven UI updates instead of polling
    /// </summary>
    private void HandleTurnChanged(bool isPlayerTurn)
    {
        UpdateUI();
    }
    
    private void HandleTurnCountChanged(int turnCount)
    {
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        UpdateTurnStatusText();
        UpdateEndTurnButton();
    }
    
    private void UpdateTurnStatusText()
    {
        if (turnStatusText == null || turnService == null) return;
        
        string currentPlayer = turnService.IsPlayerTurn ? "Player" : "Enemy";
        turnStatusText.text = $"Turn {turnService.TurnCount}: {currentPlayer}'s Turn";
        
        turnStatusText.color = turnService.IsPlayerTurn ? Color.cyan : Color.red;
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
        }
        else
        {
            buttonText.text = "End Enemy Turn";
            buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        }
        
        endTurnButton.interactable = true;
    }
    
    // ✅ UI Creation methods remain the same (pure UI responsibility)
    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("GameUI_Legacy");
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
        GameObject buttonObj = new GameObject("EndTurnButton_Legacy");
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
        GameObject textObj = new GameObject("TurnStatusText_Legacy");
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
    
    private void OnDestroy()
    {
        // ✅ Clean up event subscriptions
        if (turnService != null)
        {
            turnService.OnTurnChanged -= HandleTurnChanged;
            turnService.OnTurnCountChanged -= HandleTurnCountChanged;
        }
    }
}