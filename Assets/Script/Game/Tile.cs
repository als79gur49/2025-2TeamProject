using UnityEngine;
using Game.Components;
using Game;

public class Tile : MonoBehaviour
{
    [SerializeField] private int x;
    [SerializeField] private int y;
    [SerializeField] private Unit occupyingUnit;
    [SerializeField] private Base occupyingBase;  // ✅ Base 추가

    private bool isOccupied = false;
    private bool hasBase = false;  // ✅ 기지 존재 플래그
    private Renderer tileRenderer;
    private Color originalColor;

    public int X => x;
    public int Y => y;
    public bool IsOccupied => isOccupied;
    public Unit OccupyingUnit => occupyingUnit;
    public Base OccupyingBase => occupyingBase;  // ✅ Base 프로퍼티
    public bool HasBase => hasBase;  // ✅ 기지 존재 프로퍼티

    public Vector2Int GetGridPosition()
    {
        return new Vector2Int(x, y);
    }

    private void Awake()
    {
        // 자식 오브젝트에서 Renderer 컴포넌트 찾기
        tileRenderer = GetComponentInChildren<Renderer>();
        if (tileRenderer != null)
        {
            originalColor = tileRenderer.material.color;
        }
        else
        {
            Debug.LogWarning($"[Tile] Renderer not found in children of {gameObject.name}");
        }
    }
    
    public void Initialize(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
        gameObject.name = $"Tile_{x}_{y}";
    }
    
    /// <summary>
    /// 공격 우선순위에 따라 HealthComponent 반환
    /// 유닛 우선 → 기지 후순위
    /// ⚠️ 중요: 범위 공격 시 동일한 HealthComponent가 반환될 수 있음
    /// 호출하는 쪽에서 HashSet으로 중복 제거 필요
    ///
    /// ✅ CombatComponent.AttackTiles()와 DamageEffect.Execute()에서 사용
    /// </summary>
    public HealthComponent GetDamageableTarget()
    {
        // 유닛이 있으면 유닛의 HealthComponent (각 유닛은 고유 인스턴스)
        if (occupyingUnit != null)
        {
            var unitHealth = occupyingUnit.GetComponent<HealthComponent>();
            if (unitHealth != null && unitHealth.IsAlive)
                return unitHealth;
        }

        // 유닛이 없으면 기지의 HealthComponent
        // ⚠️ 여러 타일이 같은 Base를 참조하므로 동일한 HealthComponent 반환 가능
        if (occupyingBase != null)
        {
            var baseHealth = occupyingBase.HealthComponent;
            if (baseHealth != null && baseHealth.IsAlive)
                return baseHealth;
        }

        return null;
    }

    public bool CanPlaceUnit()
    {
        return !isOccupied;  // 기지가 있어도 유닛은 배치 가능
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
            unit.OnPlaced(this);
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

    /// <summary>
    /// 기지 배치 (유닛과 독립적)
    /// Base는 여러 타일에서 동일한 인스턴스를 참조 가능
    /// </summary>
    public bool PlaceBase(Base baseUnit)
    {
        if (hasBase)
        {
            Debug.LogWarning($"[Tile] ({x}, {y}) already has a base");
            return false;
        }

        occupyingBase = baseUnit;
        hasBase = true;

        if (baseUnit != null)
        {
            // Base에 타일 추가 (양방향 참조)
            baseUnit.AddOccupiedTile(this);
        }

        UpdateVisuals();
        return true;
    }

    /// <summary>
    /// 기지 제거
    /// </summary>
    public void RemoveBase()
    {
        if (occupyingBase != null)
        {
            occupyingBase.RemoveOccupiedTile(this);
        }

        occupyingBase = null;
        hasBase = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (tileRenderer == null) return;

        // 시각화 우선순위: 유닛 > 기지 > 빈 타일
        if (isOccupied)
        {
            tileRenderer.material.color = Color.yellow;  // 유닛 있음
        }
        else if (hasBase)
        {
            tileRenderer.material.color = Color.cyan;    // 기지만 있음
        }
        else
        {
            tileRenderer.material.color = originalColor;  // 비어있음
        }
    }
    
    private void OnMouseDown()
    {
        Debug.Log($"Tile clicked: ({x}, {y}) - Occupied: {isOccupied}");
    }
}
