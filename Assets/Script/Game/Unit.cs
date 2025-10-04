using UnityEngine;
using Game.Interfaces;
using Game.Components;
using Game.Data;
using Game.Core;
using Game;
using Game.Services;
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
    
    [SerializeField, Tooltip("Shows if Unit has been initialized with ServiceLocator")]
    private bool isInitialized = false;
    
    [SerializeField, Tooltip("Shows if death notification has been sent to prevent duplicates")]
    private bool deathNotificationSent = false;
    
    // Component references
    private IHealthComponent healthComponent;
    private ICombatSystem combatComponent;
    private IMovementSystem movementComponent;
    private ITeamComponent teamComponent;
    private IAnimationController animationController;
    
    // Legacy system support
    private int legacyMaxHealth;
    
    // Phase 3: Clean Architecture - ServiceLocator pattern  
    private IGridManager gridManager;

    [SerializeField]
    private Tile currentTile;
    
    // Public properties with component delegation
    public int Health => useComponentSystem && healthComponent != null ? healthComponent.CurrentHealth : health;
    public int MaxHealth => useComponentSystem && healthComponent != null ? healthComponent.MaxHealth : legacyMaxHealth;
    public int AttackPower => useComponentSystem && combatComponent != null ? combatComponent.CurrentAttackPower : attackPower;
    public int MovementRange => useComponentSystem && movementComponent != null ? movementComponent.MovementRange : movementRange;
    public bool IsAlive => useComponentSystem && healthComponent != null ? healthComponent.IsAlive : health > 0;
    public bool IsPlayerUnit => useComponentSystem && teamComponent != null ? teamComponent.Team == TeamType.Player : isPlayerUnit;
    public Tile CurrentTile => currentTile;
    
    // Legacy grid position properties
    public int X => CurrentTile?.X ?? (gridManager != null ? gridManager.GetUnitPosition(gameObject).x : -1);
    public int Y => CurrentTile?.Y ?? (gridManager != null ? gridManager.GetUnitPosition(gameObject).y : -1);
    
    // Initialization status properties
    public bool IsInitialized => isInitialized;
    public bool IsReadyForGame => isInitialized && gridManager != null;

    // Component access methods
    public IAnimationController GetAnimationController() => animationController;
    
    private void Awake()
    {
        // Initialize component system
        InitializeComponents();
        
        // Legacy system fallback
        legacyMaxHealth = health;
    }
    
    /// <summary>
    /// Unit을 ServiceLocator와 연결하고 초기화 수행
    /// 외부에서 호출하여 초기화 타이밍 제어 가능
    /// </summary>
    public void Init(UnitData unitData, Vector2Int position, bool isPlayerUnit)
    {
        if (isInitialized)
        {
            Debug.LogWarning($"[Unit] {gameObject.name} already initialized");
            return;
        }

        // UnitData로부터 스탯 설정
        if (unitData != null)
        {
            health = unitData.MaxHealth;
            legacyMaxHealth = unitData.MaxHealth;
            attackPower = unitData.AttackPower;
            movementRange = unitData.MovementRange;
            this.isPlayerUnit = isPlayerUnit;

            // 컴포넌트 시스템 사용 시 컴포넌트에도 적용
            if (useComponentSystem)
            {
                if (healthComponent != null)
                {
                    healthComponent.SetMaxHealth(unitData.MaxHealth);
                    healthComponent.SetHealth(unitData.MaxHealth);
                }
                if (combatComponent != null)
                {
                    combatComponent.SetBaseAttackPower(unitData.AttackPower);
                }
                if (movementComponent != null)
                {
                    movementComponent.SetMovementRange(unitData.MovementRange);
                }
                if (teamComponent != null)
                {
                    teamComponent.Team = isPlayerUnit ? TeamType.Player : TeamType.Enemy;
                }
            }

            Debug.Log($"[Unit] {gameObject.name} initialized with UnitData: HP={unitData.MaxHealth}, ATK={unitData.AttackPower}, MOV={unitData.MovementRange}, Team={isPlayerUnit}");
        }

        // ServiceLocator에서 GridManager 가져오기 (등록은 외부에서 처리)
        var gridManager = ServiceLocator.Get<IGridManager>();

        if (gridManager != null)
        {
            InitializeGridManager(gridManager);
        }
        else
        {
            Debug.LogWarning($"[Unit] {gameObject.name} could not get GridManager from ServiceLocator");
        }

        isInitialized = true;

        Debug.Log($"[Unit] {gameObject.name} initialization completed at position ({position.x}, {position.y}) - 프레임: {Time.frameCount}");
    }

    /// <summary>
    /// ServiceLocator를 통한 직접 GridManager 초기화
    /// </summary>
    private void InitializeGridManager(IGridManager igridManager)
    {
        gridManager = igridManager;
        if (gridManager != null)
        {
            Debug.Log($"[Unit] GridManager initialized directly for {gameObject.name}");

            SetupEventSubscriptions();
            InitializeCurrentTile();
        }
        else
        {
            Debug.LogWarning($"[Unit] Failed to initialize GridManager for {gameObject.name}");
        }
    }
    
    /// <summary>
    /// 초기화 시 currentTile 설정 - 이미 그리드에 배치된 유닛을 위한 처리
    /// </summary>
    private void InitializeCurrentTile()
    {
        if (gridManager == null)
        {
            Debug.LogWarning($"[Unit] {gameObject.name} InitializeCurrentTile called but gridManager is null");
            return;
        }
        
        // currentTile이 이미 설정되어 있으면 스킵
        if (currentTile != null) return;
        
        // GridManager에서 현재 유닛의 위치 확인
        if (gridManager.TryGetUnitPosition(gameObject, out Vector2Int currentPosition))
        {
            Debug.Log($"[Unit] Found {gameObject.name} at position ({currentPosition.x}, {currentPosition.y}) during initialization");
            UpdateCurrentTile(currentPosition);
        }
        else
        {
            Debug.Log($"[Unit] {gameObject.name} not found in GridManager during initialization - currentTile will be set when placed");
        }
    }
    
    /// <summary>
    /// Phase 3: 이벤트 기반 통신 설정 - GridManager 이벤트 구독
    /// </summary>
    private void SetupEventSubscriptions()
    {
        // GridManager 이벤트 구독
        if (gridManager != null)
        {
            gridManager.OnUnitMoved += OnUnitMovedInGrid;
        }
        else
        {
            Debug.LogWarning($"[Unit] GridManager not available for {gameObject.name} - event subscriptions skipped");
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
            UpdateCurrentTile(newPos);
        }
    }
    
    
    /// <summary>
    /// 지정된 그리드 위치의 Tile 컴포넌트를 찾아서 currentTile을 업데이트
    /// </summary>
    private void UpdateCurrentTile(Vector2Int gridPosition)
    {
        if (gridManager == null)
        {
            Debug.LogWarning($"[Unit] Cannot update currentTile for {gameObject.name} - gridManager is null");
            return;
        }

        // 월드 위치로 변환
        Vector3 worldPosition = gridManager.GridToWorldPosition(gridPosition);
        
        // 해당 위치 근처에서 Tile 컴포넌트를 찾기
        Tile foundTile = FindTileAtPosition(worldPosition, gridPosition);
        
        if (foundTile != null)
        {
            currentTile = foundTile;
            Debug.Log($"[Unit] {gameObject.name} currentTile updated to {foundTile.name} at ({gridPosition.x}, {gridPosition.y})");
        }
        else
        {
            Debug.LogWarning($"[Unit] Could not find Tile component at grid position ({gridPosition.x}, {gridPosition.y}) for {gameObject.name}");
            
            // Fallback: Create a virtual tile data if no physical tile is found
            CreateVirtualTile(gridPosition);
        }
    }
    
    /// <summary>
    /// 지정된 위치에서 Tile 컴포넌트를 찾기
    /// </summary>
    private Tile FindTileAtPosition(Vector3 worldPosition, Vector2Int gridPosition)
    {
        // Method 1: 반경 내에서 Tile 검색
        Collider[] colliders = Physics.OverlapSphere(worldPosition, gridManager.TileSize * 0.6f);
        foreach (var collider in colliders)
        {
            Tile tile = collider.GetComponent<Tile>();
            if (tile != null && tile.X == gridPosition.x && tile.Y == gridPosition.y)
            {
                return tile;
            }
        }
        
        // Method 2: 이름으로 검색 (GridRenderer가 생성한 타일들)
        GameObject tileObject = GameObject.Find($"Tile_{gridPosition.x}_{gridPosition.y}");
        if (tileObject != null)
        {
            Tile tile = tileObject.GetComponent<Tile>();
            if (tile != null)
            {
                return tile;
            }
        }
        
        // Method 3: 모든 Tile에서 위치 매칭 검색
        Tile[] allTiles = FindObjectsOfType<Tile>();
        foreach (var tile in allTiles)
        {
            if (tile.X == gridPosition.x && tile.Y == gridPosition.y)
            {
                return tile;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 물리적 타일이 없을 때 가상 타일 생성
    /// </summary>
    private void CreateVirtualTile(Vector2Int gridPosition)
    {
        // 임시 GameObject 생성하여 Tile 컴포넌트 추가
        GameObject virtualTileObject = new GameObject($"VirtualTile_{gridPosition.x}_{gridPosition.y}");
        virtualTileObject.transform.position = gridManager.GridToWorldPosition(gridPosition);
        
        Tile virtualTile = virtualTileObject.AddComponent<Tile>();
        virtualTile.Initialize(gridPosition.x, gridPosition.y);
        
        currentTile = virtualTile;
        
        Debug.Log($"[Unit] Created virtual tile for {gameObject.name} at ({gridPosition.x}, {gridPosition.y})");
    }
    
    private void InitializeComponents()
    {
        // Cache component references for performance
        healthComponent = GetComponent<IHealthComponent>();
        combatComponent = GetComponent<ICombatSystem>();
        movementComponent = GetComponent<IMovementSystem>();
        teamComponent = GetComponent<ITeamComponent>();
        animationController = GetComponent<IAnimationController>();

        // Initialize component system if available
        if (useComponentSystem)
        {
            InitializeHealthComponent();
            InitializeCombatComponent();
            InitializeMovementComponent();
            InitializeTeamComponent();
            InitializeAnimationController();

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

    private void InitializeAnimationController()
    {
        if (animationController == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add UnitAnimationController if not present
            var comp = gameObject.AddComponent<UnitAnimationController>();
            animationController = comp;
            Debug.Log($"[Unit] Auto-added UnitAnimationController to {gameObject.name}");
        }
    }
    
    public void SetCurrentTile(Tile tile)
    {
        currentTile = tile;
    }
    
    public void OnTurnStart()
    {
        if (!IsAlive) return;
        
        // Ensure currentTile is set before initializing turn
        if (currentTile == null)
        {
            Debug.LogWarning($"[Unit] {gameObject.name} OnTurnStart called but currentTile is null - attempting to initialize");
            InitializeCurrentTile();
            
            if (currentTile == null)
            {
                Debug.LogError($"[Unit] {gameObject.name} cannot initialize turn - currentTile is still null after initialization attempt");
                return;
            }
        }
        
        // Initialize turn for components - 턴 시작 시 필요한 초기화만 수행
        if (useComponentSystem && movementComponent != null)
        {
            movementComponent.StartTurn();
        }
        
        Debug.Log($"[Unit] {gameObject.name} OnTurnStart - currentTile: {currentTile?.name} at ({currentTile?.X}, {currentTile?.Y}) - Turn initialized");
        // Act() 호출 제거 - 실제 행동은 Action Phase에서 별도로 처리
    }
    
    public void Act()
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
        
        int[] dx = {0, 0 };
        int[] dy = {-1, 1 };
        
        for (int i = 0; i < dx.Length; i++)
        {
            int newX = currentTile.X + dx[i];
            int newY = currentTile.Y + dy[i];
            
            // Phase 3: Clean interface-based grid access
            var adjacentPos = new Vector2Int(newX, newY);
            var adjacentUnit = gridManager.GetUnitAtPosition(adjacentPos);
            if (adjacentUnit != null)
            {
                var unit = adjacentUnit.GetComponent<Unit>();
                if (unit != null && unit.IsPlayerUnit != this.IsPlayerUnit && unit.IsAlive)
                {
                    return unit;
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
        if (currentTile == null || gridManager == null) return;
        
        if (useComponentSystem && movementComponent != null)
        {
            // Use advanced movement system
            int direction = IsPlayerUnit ? 1 : -1;
            var targetPosition = new Vector2Int(currentTile.X, currentTile.Y + (direction * MovementRange));
            
            var result = movementComponent.MoveTo(targetPosition);
            if (result.Success)
            {
                Debug.Log($"{gameObject.name} moved to ({targetPosition.x}, {targetPosition.y})");
            }
            else
            {
                Debug.Log($"{gameObject.name} cannot move forward - {result.Message}");
            }
        }
        else
        {
           // // Phase 3: Clean interface-based movement
           // int targetX = currentTile.X;
           // int targetY = currentTile.Y + (isPlayerUnit ? movementRange : -movementRange);
           // var targetPos = new Vector2Int(targetX, targetY);
           // 
           // if (gridManager.CanMoveUnit(gameObject, targetPos))
           // {
           //     var result = gridManager.MoveUnit(gameObject, targetPos);
           //     if (result)
           //     {
           //         Debug.Log($"{gameObject.name} moved to ({targetX}, {targetY})");
           //     }
           //     else
           //     {
           //         Debug.Log($"{gameObject.name} movement failed");
           //     }
           // }
           // else
           // {
           //     Debug.Log($"{gameObject.name} cannot move forward - path blocked or out of bounds");
           // }
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
        Debug.Log($"[Unit] {gameObject.name} has been destroyed!");
        
        // UnitService에 사망을 알림 (아직 하지 않았다면)
        if (!deathNotificationSent)
        {
            NotifyUnitServiceOfDeath();
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// HealthComponent가 사망을 감지했을 때 호출되는 메서드
    /// </summary>
    public void OnHealthComponentDeath()
    {
        Debug.Log($"[Unit] {gameObject.name} received death notification from HealthComponent");
        
        // UnitService에 사망을 알림 (즉시 정리를 위해)
        NotifyUnitServiceOfDeath();
        
        // GameObject 파괴
        Die();
    }
    
    /// <summary>
    /// UnitService에 유닛 사망을 알려서 즉시 정리하도록 요청
    /// </summary>
    private void NotifyUnitServiceOfDeath()
    {
        if (deathNotificationSent)
        {
            Debug.Log($"[Unit] Death notification already sent for {gameObject.name}, skipping");
            return;
        }

        // Unit 사망 시 currentTile 정리
        CleanupCurrentTile();

        // GameServiceManager를 통해 UnitService에 접근
        //var gameServiceManager = FindObjectOfType<GameServiceManager>();
        var gameServiceManager = ServiceLocator.Get<IGameServiceManager>();
        if (gameServiceManager != null)
        {
            Debug.Log($"[Unit] Notifying UnitService of {gameObject.name} death for immediate cleanup");
            gameServiceManager.UnregisterUnit(this);
            deathNotificationSent = true;
        }
        else
        {
            Debug.LogWarning($"[Unit] Could not find GameServiceManager to notify UnitService of {gameObject.name} death");
        }
    }
    
    /// <summary>
    /// Unit 사망 시 currentTile을 정리합니다.
    /// Unit의 currentTile 참조를 직접 활용하여 간단하게 처리합니다.
    /// 🔧 FIX: GridState에서도 유닛을 제거하여 positionUnits 딕셔너리 정리
    /// </summary>
    private void CleanupCurrentTile()
    {
        if (currentTile != null)
        {
            Debug.Log($"[Unit] Cleaning up currentTile for dying unit {gameObject.name} at ({currentTile.X}, {currentTile.Y})");
            
            // Tile의 occupying unit이 자신인지 확인 후 정리
            if (currentTile.OccupyingUnit == this)
            {
                currentTile.RemoveUnit();
                Debug.Log($"[Unit] Successfully removed {gameObject.name} from Tile ({currentTile.X}, {currentTile.Y})");
            }
            else
            {
                Debug.LogWarning($"[Unit] Tile occupancy mismatch - expected {gameObject.name}, found {currentTile.OccupyingUnit?.name}");
            }
            
            // 🔧 FIX: GridState에서 유닛 제거 (positionUnits 딕셔너리 정리)
            if (gridManager != null)
            {
                bool gridStateRemoved = gridManager.RemoveUnit(gameObject);
                if (gridStateRemoved)
                {
                    Debug.Log($"[Unit] Successfully removed {gameObject.name} from GridState position tracking");
                }
                else
                {
                    Debug.LogWarning($"[Unit] Failed to remove {gameObject.name} from GridState - unit may not have been tracked");
                }
            }
            else
            {
                Debug.LogWarning($"[Unit] Cannot cleanup GridState for {gameObject.name} - gridManager is null");
            }
            
            // currentTile 참조 정리
            currentTile = null;
        }
        else
        {
            Debug.Log($"[Unit] No currentTile to cleanup for {gameObject.name}");
        }
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
    
    
    public bool CanMoveTo(int x, int y)
    {
        // Check if this unit can move to the specified position
        if (useComponentSystem && movementComponent != null)
        {
            return movementComponent.CanMoveTo(new Vector2Int(x, y));
        }
        else if (gridManager != null)
        {
            // Phase 3: Clean interface-based check
            return gridManager.CanMoveUnit(gameObject, new Vector2Int(x, y));
        }
        
        return false;
    }
    
    /// <summary>
    /// Legacy position setter for backward compatibility
    /// </summary>
    public void SetPosition(int x, int y)
    {
        if (gridManager != null)
        {
            var targetPos = new Vector2Int(x, y);
            gridManager.MoveUnit(gameObject, targetPos);
        }
        else if (currentTile != null)
        {
            // Fallback: just update current tile reference if available
            Debug.LogWarning($"[Unit] SetPosition called without GridManager - position update may not be complete");
        }
        else
        {
            Debug.LogWarning($"[Unit] SetPosition called without GridManager and currentTile 프레임: {Time.frameCount}");
        }
    }
    
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
        Debug.Log($"Current Tile: {(currentTile != null ? $"{currentTile.name} ({currentTile.X}, {currentTile.Y})" : "NULL")}");
        Debug.Log($"Grid Position: ({X}, {Y})");
        Debug.Log($"Initialization Status - Initialized: {isInitialized}, Ready for Game: {IsReadyForGame}");
    }
    
    [ContextMenu("Force Update Current Tile")]
    private void ForceUpdateCurrentTile()
    {
        if (gridManager != null && gridManager.TryGetUnitPosition(gameObject, out Vector2Int position))
        {
            Debug.Log($"[Unit] Forcing currentTile update for {gameObject.name} at position ({position.x}, {position.y})");
            UpdateCurrentTile(position);
        }
        else
        {
            Debug.LogWarning($"[Unit] Cannot force update currentTile for {gameObject.name} - not found in GridManager");
        }
    }
    
    [ContextMenu("Test OnTurnStart")]
    private void TestOnTurnStart()
    {
        Debug.Log($"[Unit] Testing OnTurnStart for {gameObject.name}");
        OnTurnStart();
    }
    
    [ContextMenu("Test Death System")]
    private void TestDeathSystem()
    {
        Debug.Log($"[Unit] Testing death system for {gameObject.name}");
        if (useComponentSystem && healthComponent != null)
        {
            Debug.Log($"[Unit] Using component system - setting health to 0");
            healthComponent.TakeDamage(healthComponent.CurrentHealth + 100);
        }
        else
        {
            Debug.Log($"[Unit] Using legacy system - calling Die()");
            Die();
        }
    }
    
    /// <summary>
    /// Phase 2: 이벤트 구독 해제 - 메모리 누수 방지
    /// </summary>
    private void OnDestroy()
    {
        CleanupEventSubscriptions();
    }
    
    /// <summary>
    /// Phase 3: 이벤트 구독 정리 - Clean Architecture
    /// </summary>
    private void CleanupEventSubscriptions()
    {
        // GridManager 이벤트 구독 해제
        if (gridManager != null)
        {
            gridManager.OnUnitMoved -= OnUnitMovedInGrid;
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