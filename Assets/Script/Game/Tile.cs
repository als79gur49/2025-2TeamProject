using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private int x;
    [SerializeField] private int y;
    [SerializeField] private Unit occupyingUnit;
    
    private bool isOccupied = false;
    private Renderer tileRenderer;
    private Color originalColor;
    
    public int X => x;
    public int Y => y;
    public bool IsOccupied => isOccupied;
    public Unit OccupyingUnit => occupyingUnit;
    
    public Vector2Int GetGridPosition()
    {
        return new Vector2Int(x, y);
    }
    
    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            originalColor = tileRenderer.material.color;
        }
    }
    
    public void Initialize(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
        gameObject.name = $"Tile_{x}_{y}";
    }
    
    public bool CanPlaceUnit()
    {
        return !isOccupied;
    }
    
    public bool PlaceUnit(Unit unit)
    {
        if (!CanPlaceUnit()) return false;
        
        occupyingUnit = unit;
        isOccupied = true;
        
        if (unit != null)
        {
            unit.transform.position = transform.position + Vector3.up * 0.5f;
            unit.SetCurrentTile(this);
        }
        
        UpdateVisuals();
        return true;
    }
    
    public void RemoveUnit()
    {
        occupyingUnit = null;
        isOccupied = false;
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (tileRenderer == null) return;
        
        if (isOccupied)
        {
            tileRenderer.material.color = Color.yellow;
        }
        else
        {
            tileRenderer.material.color = originalColor;
        }
    }
    
    private void OnMouseDown()
    {
        Debug.Log($"Tile clicked: ({x}, {y}) - Occupied: {isOccupied}");
    }
}