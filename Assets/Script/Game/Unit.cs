using UnityEngine;
using Game.Interfaces;
using Game.Components;
using Game.Data;

public class Unit : MonoBehaviour
{
    [Header("Legacy Configuration (for Inspector compatibility)")]
    [SerializeField] private int health = 100;
    [SerializeField] private int attackPower = 10;
    [SerializeField] private int movementRange = 1;
    [SerializeField] private bool isPlayerUnit = true;
    
    [Header("Component System")]
    [SerializeField] private bool useComponentSystem = true;
    [SerializeField] private bool autoAddMissingComponents = true;
    
    [Header("Runtime Status (Read Only)")]
    [SerializeField, Tooltip("Shows if all components are properly initialized")]
    private bool componentSystemReady = false;
    
    // Component references
    private IHealthComponent healthComponent;
    private ICombatSystem combatComponent;
    private IMovementSystem movementComponent;
    private ITeamComponent teamComponent;
    
    // Legacy system support
    private int legacyMaxHealth;
    
    // Phase 2: Interface-based dependencies (ServiceLocator pattern)
    [Inject(Required = false)]
    private IGridManager gridManager;
    
    [Inject(Required = false)]
    private IGridServices gridServices;
    
    private Tile currentTile;
    
    // Public properties with component delegation
    public int Health => useComponentSystem && healthComponent != null ? healthComponent.CurrentHealth : health;
    public int MaxHealth => useComponentSystem && healthComponent != null ? healthComponent.MaxHealth : legacyMaxHealth;
    public int AttackPower => useComponentSystem && combatComponent != null ? combatComponent.CurrentAttackPower : attackPower;
    public int MovementRange => useComponentSystem && movementComponent != null ? movementComponent.MovementRange : movementRange;
    public bool IsAlive => useComponentSystem && healthComponent != null ? healthComponent.IsAlive : health > 0;
    public bool IsPlayerUnit => useComponentSystem && teamComponent != null ? teamComponent.Team == TeamType.Player : isPlayerUnit;
    public Tile CurrentTile => currentTile;
    
    // Legacy position properties for backward compatibility with GridDataBridge
    public int X => currentTile?.X ?? 0;
    public int Y => currentTile?.Y ?? 0;
    
    private void Awake()
    {
        // Initialize component system
        InitializeComponents();
        
        // Phase 2: Dependency injection via ServiceLocator
        InitializeDependencies();
        
        // Legacy system fallback
        legacyMaxHealth = health;
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
                Debug.LogWarning($"[Unit] IGridManager not found in ServiceLocator. Falling back to FindObjectOfType.");
                var legacyGridManager = FindObjectOfType<GridManager>();
                if (legacyGridManager != null)
                {
                    gridManager = legacyGridManager.GetComponent<IGridManager>();
                }
            }
        }
        
        // Grid services 조회
        if (gridServices == null)
        {
            gridServices = ServiceLocator.Get<IGridServices>();
        }
        
        // Phase 2: 이벤트 기반 통신 설정
        SetupEventSubscriptions();
        
        Debug.Log($"[Unit] Dependencies initialized - GridManager: {gridManager != null}, GridServices: {gridServices != null}");
    }
    
    /// <summary>
    /// Phase 2: 이벤트 기반 통신 설정 - 결합도 감소
    /// </summary>
    private void SetupEventSubscriptions()
    {
        // GridState 이벤트 구독
        if (gridServices?.GridState != null)
        {
            gridServices.GridState.OnUnitMoved += OnUnitMovedInGrid;
            gridServices.GridState.OnUnitPlaced += OnUnitPlacedInGrid;
            gridServices.GridState.OnUnitRemoved += OnUnitRemovedFromGrid;
        }
        
        // GridManager 이벤트 구독 (fallback)
        if (gridManager != null)
        {
            gridManager.OnUnitMoved += OnUnitMovedInGrid;
            gridManager.OnUnitPlaced += OnUnitPlacedInGrid;
            gridManager.OnUnitRemoved += OnUnitRemovedFromGrid;
        }
    }
    
    /// <summary>
    /// 그리드에서 유닛이 이동했을 때 호출되는 이벤트 핸들러
    /// </summary>
    private void OnUnitMovedInGrid(GameObject movedUnit, Vector2Int oldPos, Vector2Int newPos)
    {
        // 자신의 이동이면 위치 업데이트
        if (movedUnit == gameObject)
        {
            Debug.Log($"[Unit] {gameObject.name} moved from {oldPos} to {newPos} via event");
            // 타일 정보 업데이트는 그리드 시스템에서 처리하므로 여기서는 로깅만
        }
    }
    
    /// <summary>
    /// 그리드에서 유닛이 배치되었을 때 호출되는 이벤트 핸들러
    /// </summary>
    private void OnUnitPlacedInGrid(Vector2Int position, GameObject placedUnit)
    {
        if (placedUnit == gameObject)
        {
            Debug.Log($"[Unit] {gameObject.name} placed at {position} via event");
        }
    }
    
    /// <summary>
    /// 그리드에서 유닛이 제거되었을 때 호출되는 이벤트 핸들러
    /// </summary>
    private void OnUnitRemovedFromGrid(Vector2Int position, GameObject removedUnit)
    {
        if (removedUnit == gameObject)
        {
            Debug.Log($"[Unit] {gameObject.name} removed from {position} via event");
        }
    }
    
    private void InitializeComponents()
    {
        // Cache component references for performance
        healthComponent = GetComponent<IHealthComponent>();
        combatComponent = GetComponent<ICombatSystem>();
        movementComponent = GetComponent<IMovementSystem>();
        teamComponent = GetComponent<ITeamComponent>();
        
        // Initialize component system if available
        if (useComponentSystem)
        {
            InitializeHealthComponent();
            InitializeCombatComponent();
            InitializeMovementComponent();
            InitializeTeamComponent();
            
            // Update runtime status
            UpdateComponentSystemStatus();
        }
    }
    
    private void UpdateComponentSystemStatus()
    {
        componentSystemReady = useComponentSystem && 
                              healthComponent != null && 
                              combatComponent != null && 
                              movementComponent != null && 
                              teamComponent != null;
    }
    
    private void InitializeHealthComponent()
    {
        if (healthComponent == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add HealthComponent if not present
            var comp = gameObject.AddComponent<HealthComponent>();
            healthComponent = comp;
            Debug.Log($"[Unit] Auto-added HealthComponent to {gameObject.name}");
        }
        
        // Apply legacy values to component
        if (healthComponent != null)
        {
            healthComponent.SetMaxHealth(health);
            healthComponent.SetHealth(health);
        }
    }
    
    private void InitializeCombatComponent()
    {
        if (combatComponent == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add CombatComponent if not present
            var comp = gameObject.AddComponent<CombatComponent>();
            combatComponent = comp;
            Debug.Log($"[Unit] Auto-added CombatComponent to {gameObject.name}");
        }
        
        // Apply legacy values to component
        if (combatComponent != null)
        {
            combatComponent.SetBaseAttackPower(attackPower);
        }
    }
    
    private void InitializeMovementComponent()
    {
        if (movementComponent == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add MovementComponent if not present
            var comp = gameObject.AddComponent<MovementComponent>();
            movementComponent = comp;
            Debug.Log($"[Unit] Auto-added MovementComponent to {gameObject.name}");
        }
        
        // Apply legacy values to component
        if (movementComponent != null)
        {
            movementComponent.SetMovementRange(movementRange);
        }
    }
    
    private void InitializeTeamComponent()
    {
        if (teamComponent == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add TeamComponent if not present
            var comp = gameObject.AddComponent<TeamComponent>();
            teamComponent = comp;
            Debug.Log($"[Unit] Auto-added TeamComponent to {gameObject.name}");
        }
        
        // Apply legacy values to component
        if (teamComponent != null)
        {
            // Set team based on legacy isPlayerUnit flag
            teamComponent.Team = isPlayerUnit ? TeamType.Player : TeamType.Enemy;
        }
    }
    
    public void SetCurrentTile(Tile tile)
    {
        currentTile = tile;
    }
    
    public void OnTurnStart()
    {
        if (!IsAlive) return;
        
        // Initialize turn for components
        if (useComponentSystem && movementComponent != null)
        {
            movementComponent.StartTurn();
        }
        
        Act();
    }
    
    private void Act()
    {
        Unit enemy = SearchForNearbyEnemies();
        
        if (enemy != null)
        {
            Debug.Log("Act:Attack");
            AttackEnemy(enemy);
        }
        else
        {
            Debug.Log("Act:MoveTo");
            MoveForward();
        }
    }
    
    private Unit SearchForNearbyEnemies()
    {
        if (currentTile == null || gridManager == null) return null;
        
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        
        for (int i = 0; i < dx.Length; i++)
        {
            int newX = currentTile.X + dx[i];
            int newY = currentTile.Y + dy[i];
            
            // Phase 2: Use interface-based grid access
            if (gridServices?.GridState != null)
            {
                var adjacentPos = new Vector2Int(newX, newY);
                var adjacentUnit = gridServices.GridState.GetUnitAtPosition(adjacentPos);
                if (adjacentUnit != null)
                {
                    var unit = adjacentUnit.GetComponent<Unit>();
                    if (unit != null && unit.IsPlayerUnit != this.IsPlayerUnit)
                    {
                        return unit;
                    }
                }
            }
            else
            {
                // Fallback to legacy system
                var legacyGridManager = gridManager as GridManager;
                if (legacyGridManager != null)
                {
                    Tile adjacentTile = legacyGridManager.GetTile(newX, newY);
                    if (adjacentTile != null && adjacentTile.IsOccupied)
                    {
                        Unit adjacentUnit = adjacentTile.OccupyingUnit;
                        if (adjacentUnit != null && adjacentUnit.IsPlayerUnit != this.IsPlayerUnit)
                        {
                            return adjacentUnit;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    private void AttackEnemy(Unit enemy)
    {
        if (enemy == null || !enemy.IsAlive) return;
        
        if (useComponentSystem && combatComponent != null)
        {
            // Use advanced combat system
            var result = combatComponent.Attack(enemy.gameObject);
            if (result.Success && result.IsHit)
            {
                Debug.Log($"{gameObject.name} attacks {enemy.gameObject.name} for {result.DamageDealt} damage!");
            }
            else
            {
                Debug.Log($"{gameObject.name} attack on {enemy.gameObject.name} failed!");
            }
        }
        else
        {
            // Legacy attack system
            Debug.Log($"{gameObject.name} attacks {enemy.gameObject.name} for {attackPower} damage!");
            enemy.TakeDamage(attackPower);
        }
    }
    
    private void MoveForward()
    {
        Debug.Log("MoveForward1");
        if (currentTile == null || gridManager == null) return;
        Debug.Log("MoveForward2");
        if (useComponentSystem && movementComponent != null)
        {
            // Use advanced movement system
            int direction = IsPlayerUnit ? 1 : -1;
            var targetPosition = new Vector2Int(currentTile.X, currentTile.Y + (direction * MovementRange));
            
            var result = movementComponent.MoveTo(targetPosition);
            if (result.Success)
            {
                Debug.Log($"{gameObject.name} moved to ({targetPosition.x}, {targetPosition.y})");
                // Note: Tile management should be handled by the grid system integration
            }
            else
            {
                Debug.Log($"{gameObject.name} cannot move forward - {result.Message}");
            }
        }
        else
        {
            // Legacy movement system
            // Phase 2: Try interface-based movement first
            int targetX = currentTile.X;
            int targetY = currentTile.Y + (isPlayerUnit ? movementRange : -movementRange);
            var targetPos = new Vector2Int(targetX, targetY);
            
            if (gridManager != null && gridManager.CanMoveUnit(gameObject, targetPos))
            {
                var result = gridManager.MoveUnit(gameObject, targetPos);
                if (result)
                {
                    Debug.Log($"{gameObject.name} moved to ({targetX}, {targetY}) using interface");
                    // Note: Tile management should be handled by the grid system integration
                    return;
                }
            }
            
            // Fallback to legacy system
            var legacyGridManager = gridManager as GridManager;
            if (legacyGridManager != null && legacyGridManager.CanPlaceUnitAt(targetX, targetY))
            {
                Tile targetTile = legacyGridManager.GetTile(targetX, targetY);
                if (targetTile != null)
                {
                    currentTile.RemoveUnit();
                    
                    targetTile.PlaceUnit(this);
                    SetCurrentTile(targetTile);
                    
                    Debug.Log($"{gameObject.name} moved to ({targetX}, {targetY}) using legacy system");
                }
            }
            else
            {
                Debug.Log($"{gameObject.name} cannot move forward - path blocked or out of bounds");
            }
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (useComponentSystem && healthComponent != null)
        {
            // Delegate to HealthComponent
            healthComponent.TakeDamage(damage);
        }
        else
        {
            // Legacy system
            if (!IsAlive) return;
            
            health -= damage;
            health = Mathf.Max(0, health);
            
            Debug.Log($"{gameObject.name} took {damage} damage. Current health: {health}/{legacyMaxHealth}");
            
            if (health <= 0)
            {
                Die();
            }
        }
    }
    
    private void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");
        Destroy(gameObject);
    }
    
    public void Heal(int healAmount)
    {
        if (useComponentSystem && healthComponent != null)
        {
            // Delegate to HealthComponent
            healthComponent.Heal(healAmount);
        }
        else
        {
            // Legacy system
            if (!IsAlive) return;
            
            health += healAmount;
            health = Mathf.Min(legacyMaxHealth, health);
            
            Debug.Log($"{gameObject.name} healed {healAmount}. Current health: {health}/{legacyMaxHealth}");
        }
    }
    
    // Helper methods for component system management
    public void EnableComponentSystem(bool enable)
    {
        useComponentSystem = enable;
        if (enable)
        {
            InitializeComponents();
        }
    }
    
    public bool IsUsingComponentSystem()
    {
        return useComponentSystem && healthComponent != null && combatComponent != null && movementComponent != null;
    }
    
    // Enhanced functionality through components
    public void OnTurnEnd()
    {
        if (useComponentSystem && movementComponent != null)
        {
            movementComponent.EndTurn();
        }
    }
    
    // Component access methods for advanced features
    public IHealthComponent GetHealthComponent() => healthComponent;
    public ICombatSystem GetCombatComponent() => combatComponent;
    public IMovementSystem GetMovementComponent() => movementComponent;
    public ITeamComponent GetTeamComponent() => teamComponent;
    
    // Legacy methods for backward compatibility with GridDataBridge
    public void SetPosition(int x, int y)
    {
        // This method is used by the GridDataBridge to update unit position
        // The actual position is managed by the grid system, this is just for legacy compatibility
        Debug.Log($"[Unit] Legacy SetPosition called: ({x}, {y}) for {gameObject.name}");
        
        // If we have a movement component, try to use it
        if (useComponentSystem && movementComponent != null)
        {
            var result = movementComponent.MoveTo(new Vector2Int(x, y));
            if (!result.Success)
            {
                Debug.LogWarning($"[Unit] SetPosition failed: {result.Message}");
            }
        }
        // Legacy system relies on grid manager to handle the actual movement
    }
    
    public bool CanMoveTo(int x, int y)
    {
        // Check if this unit can move to the specified position
        if (useComponentSystem && movementComponent != null)
        {
            return movementComponent.CanMoveTo(new Vector2Int(x, y));
        }
        else
        {
            // Phase 2: Use interface-based check first
            if (gridManager != null)
            {
                return gridManager.CanMoveUnit(gameObject, new Vector2Int(x, y));
            }
            
            // Fallback to legacy system  
            var legacyGridManager = gridManager as GridManager;
            if (legacyGridManager != null)
            {
                return legacyGridManager.CanPlaceUnitAt(x, y);
            }
        }
        
        return false;
    }

    // Inspector utility methods
    [ContextMenu("Initialize Components")]
    private void ForceInitializeComponents()
    {
        InitializeComponents();
        Debug.Log($"[Unit] Components initialized. System ready: {componentSystemReady}");
    }
    
    [ContextMenu("Toggle Component System")]
    private void ToggleComponentSystem()
    {
        EnableComponentSystem(!useComponentSystem);
        Debug.Log($"[Unit] Component system {(useComponentSystem ? "enabled" : "disabled")}");
    }
    
    [ContextMenu("Log Component Status")]
    private void LogComponentStatus()
    {
        Debug.Log($"=== Unit Component Status for {gameObject.name} ===");
        Debug.Log($"Use Component System: {useComponentSystem}");
        Debug.Log($"Component System Ready: {componentSystemReady}");
        Debug.Log($"Health Component: {healthComponent != null} ({healthComponent?.GetType().Name})");
        Debug.Log($"Combat Component: {combatComponent != null} ({combatComponent?.GetType().Name})");
        Debug.Log($"Movement Component: {movementComponent != null} ({movementComponent?.GetType().Name})");
        Debug.Log($"Team Component: {teamComponent != null} ({teamComponent?.GetType().Name})");
        Debug.Log($"Current Stats - Health: {Health}/{MaxHealth}, Attack: {AttackPower}, Movement: {MovementRange}");
        Debug.Log($"Team: {(teamComponent?.Team.ToString() ?? "Legacy")} | Player Unit: {IsPlayerUnit}");
    }
    
    /// <summary>
    /// Phase 2: 이벤트 구독 해제 - 메모리 누수 방지
    /// </summary>
    private void OnDestroy()
    {
        CleanupEventSubscriptions();
    }
    
    /// <summary>
    /// 이벤트 구독 정리
    /// </summary>
    private void CleanupEventSubscriptions()
    {
        // GridState 이벤트 구독 해제
        if (gridServices?.GridState != null)
        {
            gridServices.GridState.OnUnitMoved -= OnUnitMovedInGrid;
            gridServices.GridState.OnUnitPlaced -= OnUnitPlacedInGrid;
            gridServices.GridState.OnUnitRemoved -= OnUnitRemovedFromGrid;
        }
        
        // GridManager 이벤트 구독 해제 (fallback)
        if (gridManager != null)
        {
            gridManager.OnUnitMoved -= OnUnitMovedInGrid;
            gridManager.OnUnitPlaced -= OnUnitPlacedInGrid;
            gridManager.OnUnitRemoved -= OnUnitRemovedFromGrid;
        }
    }
    
    // Validation for Editor
    private void OnValidate()
    {
        if (Application.isPlaying && useComponentSystem)
        {
            UpdateComponentSystemStatus();
        }
    }
}