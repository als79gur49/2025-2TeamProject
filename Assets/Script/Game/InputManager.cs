using UnityEngine;
using Game.Interfaces;
using Game.Core;

public class InputManager : MonoBehaviour
{
    private Camera mainCamera;
    
    // Phase 2: Interface-based dependencies
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
        
        // Phase 2: ServiceLocator-based dependency injection
        InitializeDependencies();
        
        handManager = FindObjectOfType<HandManager>();
    }
    
    /// <summary>
    /// Phase 2: ServiceLocator 기반 의존성 주입
    /// </summary>
    private void InitializeDependencies()
    {
        // ServiceLocator를 통한 의존성 주입
        this.InjectDependencies();
        
        // Fallback: 서비스 로케이터에서 직접 조회
        if (gridManager == null)
        {
            gridManager = ServiceLocator.Get<IGridManager>();
            if (gridManager == null)
            {
                Debug.LogWarning($"[InputManager] IGridManager not found in ServiceLocator. Falling back to FindObjectOfType.");
                var legacyGridManager = FindObjectOfType<GridManager>();
                gridManager = legacyGridManager;
            }
        }
        
        // Grid services 조회
        if (gridServices == null)
        {
            gridServices = ServiceLocator.Get<IGridServices>();
        }
        
        Debug.Log($"[InputManager] Dependencies initialized - GridManager: {gridManager != null}, GridServices: {gridServices != null}");
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
        
        if (!gridManager.IsValidPosition(tile.X, tile.Y))
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