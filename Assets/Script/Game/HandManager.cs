using UnityEngine;
using UnityEngine.UI;
using Game.Interfaces;
using Game.Core;
using Game.Services;

public class HandManager : MonoBehaviour
{
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private Button cardButton;
    [SerializeField] private Canvas uiCanvas;
    
    // Phase 3: Interface-based dependencies (Clean Architecture)
    [Inject(Required = false)]
    private IGridManager gridManager;
    
    [Inject(Required = false)]
    private IGridServices gridServices;
    
    private InputManager inputManager;
    private GameServiceManager gameServiceManager;
    
    private bool isCardSelected = false;
    
    public bool IsCardSelected => isCardSelected;
    public GameObject UnitPrefab => unitPrefab;
    
    private void Start()
    {
        Initialize();
        CreateUI();
    }
    
    private void Initialize()
    {
        // Phase 2: ServiceLocator-based dependency injection
        InitializeDependencies();
        
        inputManager = FindObjectOfType<InputManager>();
        gameServiceManager = FindObjectOfType<GameServiceManager>();
        
        if (inputManager == null)
        {
            GameObject inputObj = new GameObject("InputManager");
            inputManager = inputObj.AddComponent<InputManager>();
        }
        
        if (gameServiceManager == null)
        {
            GameObject gameServiceObj = new GameObject("GameServiceManager");
            gameServiceManager = gameServiceObj.AddComponent<GameServiceManager>();
        }
    }
    
    /// <summary>
    /// Phase 3: ServiceLocator 기반 의존성 주입 (Clean Architecture)
    /// </summary>
    private void InitializeDependencies()
    {
        // ServiceLocator를 통한 의존성 주입
        this.InjectDependencies();
        
        // 현재 Phase 3 방식: ServiceLocator에서 직접 조회
        if (gridManager == null)
        {
            gridManager = ServiceLocator.Get<IGridManager>();
            if (gridManager == null)
            {
                Debug.LogError($"[HandManager] IGridManager not found in ServiceLocator. Please ensure GridManager is initialized first.");
                Debug.LogError($"[HandManager] GridManager should register itself through ServiceLocator.Register<IGridManager>() in Awake().");
            }
        }
        
        // Grid services 조회
        if (gridServices == null)
        {
            gridServices = ServiceLocator.Get<IGridServices>();
            if (gridServices == null)
            {
                Debug.LogError($"[HandManager] IGridServices not found in ServiceLocator. Please ensure GridManager is initialized first.");
            }
        }
        
        Debug.Log($"[HandManager] Dependencies initialized (Phase 3) - GridManager: {gridManager != null}, GridServices: {gridServices != null}");
    }
    
    private void CreateUI()
    {
        if (uiCanvas == null)
        {
            GameObject canvasObj = new GameObject("HandUI");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        if (cardButton == null)
        {
            GameObject buttonObj = new GameObject("CardButton");
            buttonObj.transform.SetParent(uiCanvas.transform);
            
            cardButton = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = Color.blue;
            
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 50);
            rectTransform.anchoredPosition = new Vector2(-200, -200);
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = "Place Unit";
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = Vector2.zero;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        
        cardButton.onClick.AddListener(OnCardButtonClicked);
    }
    
    private void CreateDefaultUnitPrefab()
    {
        if (unitPrefab != null) return;
        
        GameObject unit = new GameObject("DefaultUnit");
        unit.AddComponent<Unit>();
        
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(unit.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.GetComponent<Renderer>().material.color = Color.magenta;
        
        unitPrefab = unit;
    }
    
    private void OnCardButtonClicked()
    {
        // Note: Turn validation is now handled by GameServiceManager
        // For now, allow card selection and let GameServiceManager handle turn logic
        Debug.Log("Card selection requested via GameServiceManager");
        
        isCardSelected = !isCardSelected;
        
        if (isCardSelected)
        {
            Debug.Log("Card selected! Click on a tile to place unit.");
            cardButton.GetComponent<Image>().color = Color.yellow;
        }
        else
        {
            Debug.Log("Card deselected.");
            cardButton.GetComponent<Image>().color = Color.blue;
        }
    }
    
    public bool TryPlaceUnit(Tile targetTile)
    {
        if (!isCardSelected) return false;
        if (targetTile == null) return false;
        if (!targetTile.CanPlaceUnit()) return false;
        
        if (unitPrefab == null)
        {
            CreateDefaultUnitPrefab();
        }
        
        GameObject newUnitObj = Instantiate(unitPrefab);
        Unit newUnit = newUnitObj.GetComponent<Unit>();
        
        if (newUnit == null)
        {
            newUnit = newUnitObj.AddComponent<Unit>();
        }
        
        bool success = targetTile.PlaceUnit(newUnit);
        
        if (success)
        {
            isCardSelected = false;
            cardButton.GetComponent<Image>().color = Color.blue;
            Debug.Log($"Unit placed at ({targetTile.X}, {targetTile.Y})");
            return true;
        }
        else
        {
            Destroy(newUnitObj);
            Debug.Log("Failed to place unit!");
            return false;
        }
    }
}