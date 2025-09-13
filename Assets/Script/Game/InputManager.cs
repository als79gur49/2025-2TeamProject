using UnityEngine;

public class InputManager : MonoBehaviour
{
    private Camera mainCamera;
    private GridManager gridManager;
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
        
        gridManager = FindObjectOfType<GridManager>();
        handManager = FindObjectOfType<HandManager>();
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