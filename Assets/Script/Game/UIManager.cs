using UnityEngine;
using UnityEngine.UI;
using Game.Services;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Text turnStatusText;
    [SerializeField] private Canvas gameCanvas;
    
    private GameServiceManager gameServiceManager;
    private UnitController unitController;
    
    private void Start()
    {
        Initialize();
        CreateUI();
        UpdateUI();
    }
    
    private void Initialize()
    {
        gameServiceManager = FindObjectOfType<GameServiceManager>();
        unitController = FindObjectOfType<UnitController>();
        
        if (gameServiceManager == null)
        {
            GameObject gameServiceObj = new GameObject("GameServiceManager");
            gameServiceManager = gameServiceObj.AddComponent<GameServiceManager>();
        }
    }
    
    private void CreateUI()
    {
        if (gameCanvas == null)
        {
            CreateCanvas();
        }
        
        if (endTurnButton == null)
        {
            CreateEndTurnButton();
        }
        
        if (turnStatusText == null)
        {
            CreateTurnStatusText();
        }
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
    
    private void Update()
    {
        UpdateUI();
        HandleKeyboardInput();
    }
    
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnEndTurnButtonClicked();
        }
    }
    
    private void OnEndTurnButtonClicked()
    {
        if (gameServiceManager == null) return;
        
        Debug.Log("End Turn button clicked!");
        
        if (unitController != null)
        {
            ProcessCurrentTurnUnits();
        }
        
        // Request turn end through GameServiceManager event system
        //gameServiceManager.OnEndTurnRequested?.Invoke();
        UpdateUI();
    }
    
    private void ProcessCurrentTurnUnits()
    {
        Unit[] allUnits = FindObjectsOfType<Unit>();
        
        // Unit processing logic is now handled by GameServiceManager's UnitService
        Debug.Log("Processing all units before ending turn...");
        foreach (Unit unit in allUnits)
        {
            if (unit != null && unit.IsAlive)
            {
                unit.OnTurnStart();
            }
        }
    }
    
    private void UpdateUI()
    {
        if (gameServiceManager == null) return;
        
        UpdateTurnStatusText();
        UpdateEndTurnButton();
    }
    
    private void UpdateTurnStatusText()
    {
        if (turnStatusText == null) return;
        
        // Note: Turn information will be provided through GameServiceManager events
        // For now, show basic status until event system is properly connected
        turnStatusText.text = "Game Active - Use GameServiceManager Events";
        turnStatusText.color = Color.white;
    }
    
    private void UpdateEndTurnButton()
    {
        if (endTurnButton == null) return;
        
        Text buttonText = endTurnButton.GetComponentInChildren<Text>();
        Image buttonImage = endTurnButton.GetComponent<Image>();
        
        // Simplified button state - GameServiceManager will handle turn logic
        buttonText.text = "End Turn";
        buttonImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        endTurnButton.interactable = true;
    }
    
    public void SetGameServiceManager(GameServiceManager manager)
    {
        gameServiceManager = manager;
        UpdateUI();
    }
    
    public void SetUnitController(UnitController controller)
    {
        unitController = controller;
    }
}