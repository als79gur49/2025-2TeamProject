using UnityEngine;
using Game.Interfaces;
using Game.Core;

public class InputManager : MonoBehaviour
{
    private Camera mainCamera;
    
    // Phase 3: Interface-based dependencies (Clean Architecture)
    [Inject(Required = false)]
    private IGridManager gridManager;
    
    [Inject(Required = false)]
    private IGridServices gridServices;
    
    private HandManager handManager;
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // Phase 3: ServiceLocator-based dependency injection
        InitializeDependencies();
        
        handManager = FindObjectOfType<HandManager>();
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
                Debug.LogError($"[InputManager] IGridManager not found in ServiceLocator. Please ensure GridManager is initialized first.");
                Debug.LogError($"[InputManager] GridManager should register itself through ServiceLocator.Register<IGridManager>() in Awake().");
            }
        }
        
        // Grid services 조회
        if (gridServices == null)
        {
            gridServices = ServiceLocator.Get<IGridServices>();
            if (gridServices == null)
            {
                Debug.LogError($"[InputManager] IGridServices not found in ServiceLocator. Please ensure GridManager is initialized first.");
            }
        }
        
        Debug.Log($"[InputManager] Dependencies initialized (Phase 3) - GridManager: {gridManager != null}, GridServices: {gridServices != null}");
    }
    
    private void Update()
    {
        HandleMouseInput();
    }
    
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }
    
    private void HandleMouseClick()
    {
        if (mainCamera == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            Tile clickedTile = hit.collider.GetComponent<Tile>();
            
            if (clickedTile != null)
            {
                HandleTileClick(clickedTile);
            }
        }
    }
    
    private void HandleTileClick(Tile tile)
    {
        Debug.Log($"Tile clicked: ({tile.X}, {tile.Y})");
        
        if (handManager != null && handManager.IsCardSelected)
        {
            if (IsValidPlacementPosition(tile))
            {
                bool success = handManager.TryPlaceUnit(tile);
                if (success)
                {
                    Debug.Log($"Unit successfully placed at ({tile.X}, {tile.Y})");
                }
                else
                {
                    Debug.Log($"Failed to place unit at ({tile.X}, {tile.Y})");
                }
            }
            else
            {
                Debug.Log($"Invalid placement position: ({tile.X}, {tile.Y})");
            }
        }
        else
        {
            Debug.Log("No card selected or HandManager not found");
        }
    }
    
    private bool IsValidPlacementPosition(Tile tile)
    {
        if (tile == null) return false;
        
        if (!tile.CanPlaceUnit())
        {
            Debug.Log($"Tile ({tile.X}, {tile.Y}) is already occupied");
            return false;
        }
        
        if (gridManager == null) return true;
        
        if (!gridManager.IsValidPosition(new Vector2Int(tile.X, tile.Y)))
        {
            Debug.Log($"Position ({tile.X}, {tile.Y}) is outside grid bounds");
            return false;
        }
        
        return true;
    }
    
    public Tile GetTileAtMousePosition()
    {
        if (mainCamera == null) return null;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.GetComponent<Tile>();
        }
        
        return null;
    }
}