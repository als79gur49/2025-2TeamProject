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
    
    /// <summary>
    /// 유닛을 타일에 배치 (Transform 포함, 초기 배치용)
    /// 이동 중에는 SetOccupyingUnitLogic() 사용 권장
    /// </summary>
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

    /// <summary>
    /// 유닛의 논리적 상태만 업데이트 (Transform 변경 없음)
    /// MovementComponent 애니메이션 중 사용
    /// </summary>
    public void SetOccupyingUnitLogic(Unit unit)
    {
        occupyingUnit = unit;
        isOccupied = (unit != null);

        if (unit != null)
        {
            // Transform 이동 제거: MovementComponent의 SyncTransformWithAnimation()에서 처리
            unit.SetCurrentTile(this);
        }

        UpdateVisuals();
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