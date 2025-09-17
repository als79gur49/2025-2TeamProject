# Phase 1: Component-Based Architecture Design
## Unity Tactical Card Game - Core Gameplay Loop

### Architecture Overview

This document presents a comprehensive component-based architecture for a Unity tactical card game, emphasizing **extensibility**, **encapsulation**, and **data-driven design**. Instead of traditional interface-based inheritance, this architecture uses **component composition** for maximum flexibility and maintainability.

## 🏗️ Architecture Layers

### 1. Data Layer (Pure C# Classes & ScriptableObjects)
- **UnitStats**: Serializable data containers
- **UnitData**: ScriptableObject blueprints  
- **CardData**: Card definitions and configurations

### 2. Component Layer (MonoBehaviour Components)
- **HealthComponent**: Health and damage management
- **MovementComponent**: Movement capabilities and pathfinding
- **AttackComponent**: Combat behavior and targeting
- **ActionComponent**: Turn-based action coordination
- **TeamComponent**: Team affiliation and allegiance

### 3. Entity Layer (GameObject Composition)
- **Entity**: Base class with component registry
- **Pawn**: Mobile combat units
- **Base**: Stationary objectives

### 4. System Layer (Manager Classes)
- **GridManager**: Board state and spatial queries
- **TurnManager**: Game flow state machine
- **CombatSystem**: Combat resolution
- **CardSystem**: Card play processing

---

## 📊 Data Layer Implementation

### UnitStats (Pure Data Container)
```csharp
[System.Serializable]
public class UnitStats
{
    [Header("Combat Stats")]
    public int maxHealth = 100;
    public int attackPower = 10;
    
    [Header("Movement")]
    public int movementRange = 2;
    
    [Header("Action Economy")]
    public int actionPointsPerTurn = 1;
    
    // Copy constructor for runtime instances
    public UnitStats(UnitStats original)
    {
        maxHealth = original.maxHealth;
        attackPower = original.attackPower;
        movementRange = original.movementRange;
        actionPointsPerTurn = original.actionPointsPerTurn;
    }
    
    // Factory method for modified stats (buffs/debuffs)
    public UnitStats CreateModified(StatModifier modifier)
    {
        var modified = new UnitStats(this);
        modifier.Apply(modified);
        return modified;
    }
}

[System.Serializable]
public class StatModifier
{
    public int healthBonus;
    public int attackBonus;
    public int movementBonus;
    
    public void Apply(UnitStats stats)
    {
        stats.maxHealth += healthBonus;
        stats.attackPower += attackBonus;
        stats.movementRange += movementBonus;
    }
}
```

### UnitData (ScriptableObject Blueprint)
```csharp
[CreateAssetMenu(fileName = "New Unit", menuName = "Game/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Identity")]
    public string unitName = "Default Unit";
    public Sprite unitIcon;
    
    [Header("Stats")]
    public UnitStats baseStats;
    
    [Header("Prefab")]
    public GameObject unitPrefab;
    
    [Header("Behavior")]
    public AIBehaviorType defaultBehavior = AIBehaviorType.Aggressive;
    
    [Header("Visual")]
    public Material unitMaterial;
    public AnimationClip[] animations;
    
    // Validation in editor
    private void OnValidate()
    {
        if (unitPrefab != null && unitPrefab.GetComponent<Entity>() == null)
        {
            Debug.LogWarning($"Unit prefab {unitPrefab.name} must have Entity component");
        }
    }
}

public enum AIBehaviorType
{
    Aggressive,    // Attack nearest enemy
    Defensive,     // Protect allies and base
    Support,       // Prioritize healing/buffing
    Cautious       // Avoid damage, attack when safe
}
```

### CardData (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "New Card", menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Card Identity")]
    public string cardName = "Default Card";
    public string description = "";
    public Sprite cardIllustration;
    
    [Header("Gameplay")]
    public CardType cardType;
    public int cost = 1;
    
    [Header("Unit Card Data")]
    [ShowIf("cardType", CardType.Pawn)]
    public UnitData unitToSummon;
    
    [Header("Spell Card Data")]
    [ShowIf("cardType", CardType.Spell)]
    public SpellEffect spellEffect;
    
    [Header("Rarity & Collection")]
    public CardRarity rarity = CardRarity.Common;
    public bool isCollectable = true;
}

public enum CardType
{
    Pawn,    // Summons a unit
    Spell    // Immediate effect
}

public enum CardRarity
{
    Common,
    Uncommon, 
    Rare,
    Epic,
    Legendary
}
```

---

## 🧩 Component Layer Implementation

### Component Base Pattern
```csharp
public abstract class GameComponent : MonoBehaviour
{
    protected Entity entity;
    protected ComponentRegistry registry;
    
    protected virtual void Awake()
    {
        entity = GetComponent<Entity>();
        registry = entity.Registry;
    }
    
    // Override for component-specific initialization
    protected virtual void OnEntityInitialized() { }
    
    // Called when component is added/removed
    protected virtual void OnComponentRegistered() { }
    protected virtual void OnComponentUnregistered() { }
}
```

### HealthComponent (Damage & Death Management)
```csharp
public class HealthComponent : GameComponent
{
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    
    // Events for loose coupling
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int, GameObject> OnDamageTaken;
    public UnityEvent OnDeath;
    public UnityEvent OnHealed;
    
    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;
    public float HealthPercentage => (float)currentHealth / maxHealth;
    
    public void Initialize(UnitStats stats)
    {
        maxHealth = stats.maxHealth;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public void TakeDamage(int damage, GameObject source = null)
    {
        if (IsDead) return;
        
        int actualDamage = Mathf.Min(damage, currentHealth);
        currentHealth -= actualDamage;
        
        // Fire events
        OnDamageTaken?.Invoke(actualDamage, source);
        OnHealthChanged?.Invoke(currentHealth);
        
        // Check for death
        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }
    
    public void Heal(int amount)
    {
        if (IsDead) return;
        
        int actualHeal = Mathf.Min(amount, maxHealth - currentHealth);
        currentHealth += actualHeal;
        
        OnHealed?.Invoke();
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }
}
```

### AttackComponent (Combat Behavior)
```csharp
public class AttackComponent : GameComponent
{
    [SerializeField] private int attackPower;
    [SerializeField] private int attackRange = 1;
    [SerializeField] private LayerMask targetLayers = -1;
    
    // Events
    public UnityEvent<GameObject> OnAttackInitiated;
    public UnityEvent<GameObject, int> OnDamageDealt;
    public UnityEvent OnAttackMissed;
    
    // Properties
    public int AttackPower => attackPower;
    public int AttackRange => attackRange;
    
    private GridManager gridManager;
    private TeamComponent teamComponent;
    
    protected override void Awake()
    {
        base.Awake();
        gridManager = FindObjectOfType<GridManager>();
        teamComponent = GetComponent<TeamComponent>();
    }
    
    public void Initialize(UnitStats stats)
    {
        attackPower = stats.attackPower;
    }
    
    public bool CanAttackTarget(GameObject target)
    {
        if (target == null) return false;
        
        // Check team allegiance
        var targetTeam = target.GetComponent<TeamComponent>();
        if (targetTeam == null || teamComponent.IsAlly(targetTeam)) return false;
        
        // Check range
        var distance = gridManager.GetDistance(transform.position, target.transform.position);
        if (distance > attackRange) return false;
        
        // Check line of sight
        return gridManager.HasLineOfSight(transform.position, target.transform.position);
    }
    
    public void Attack(GameObject target)
    {
        if (!CanAttackTarget(target)) return;
        
        OnAttackInitiated?.Invoke(target);
        
        // Damage calculation and application handled by CombatSystem
        // This component just initiates the attack
    }
    
    public List<GameObject> GetTargetsInRange()
    {
        var targets = new List<GameObject>();
        var tilesInRange = gridManager.GetTilesInRange(transform.position, attackRange);
        
        foreach (var tile in tilesInRange)
        {
            if (tile.Occupant != null && CanAttackTarget(tile.Occupant.gameObject))
            {
                targets.Add(tile.Occupant.gameObject);
            }
        }
        
        return targets;
    }
}
```

### MovementComponent (Spatial Navigation)
```csharp
public class MovementComponent : GameComponent
{
    [SerializeField] private int movementRange;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool canMoveThrough = false;
    
    // Events
    public UnityEvent<Vector2Int> OnMoveStarted;
    public UnityEvent<Vector2Int> OnMoveCompleted;
    public UnityEvent OnMoveFailed;
    
    // State
    private bool isMoving = false;
    private Vector2Int currentGridPosition;
    
    public int MovementRange => movementRange;
    public bool IsMoving => isMoving;
    public Vector2Int GridPosition => currentGridPosition;
    
    private GridManager gridManager;
    
    protected override void Awake()
    {
        base.Awake();
        gridManager = FindObjectOfType<GridManager>();
    }
    
    public void Initialize(UnitStats stats)
    {
        movementRange = stats.movementRange;
        currentGridPosition = gridManager.WorldToGrid(transform.position);
    }
    
    public List<Vector2Int> GetValidMovePositions()
    {
        return gridManager.GetValidMovePositions(currentGridPosition, movementRange, canMoveThrough);
    }
    
    public bool CanMoveTo(Vector2Int targetPosition)
    {
        var validPositions = GetValidMovePositions();
        return validPositions.Contains(targetPosition);
    }
    
    public void MoveTo(Vector2Int targetPosition)
    {
        if (isMoving || !CanMoveTo(targetPosition)) 
        {
            OnMoveFailed?.Invoke();
            return;
        }
        
        StartCoroutine(MoveCoroutine(targetPosition));
    }
    
    private IEnumerator MoveCoroutine(Vector2Int targetPosition)
    {
        isMoving = true;
        OnMoveStarted?.Invoke(targetPosition);
        
        // Get path from GridManager
        var path = gridManager.GetPath(currentGridPosition, targetPosition);
        
        // Animate movement along path
        foreach (var point in path)
        {
            Vector3 worldPos = gridManager.GridToWorld(point);
            yield return StartCoroutine(MoveToPosition(worldPos));
        }
        
        // Update grid registration
        gridManager.UpdateUnitPosition(entity, currentGridPosition, targetPosition);
        currentGridPosition = targetPosition;
        
        isMoving = false;
        OnMoveCompleted?.Invoke(targetPosition);
    }
    
    private IEnumerator MoveToPosition(Vector3 targetWorldPos)
    {
        Vector3 startPos = transform.position;
        float journey = 0f;
        
        while (journey <= 1f)
        {
            journey += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, targetWorldPos, journey);
            yield return null;
        }
    }
}
```

### ActionComponent (Turn Coordination)
```csharp
public class ActionComponent : GameComponent
{
    [SerializeField] private bool canAct = false;
    [SerializeField] private bool hasActedThisTurn = false;
    [SerializeField] private int actionPointsRemaining = 0;
    [SerializeField] private int maxActionPoints = 1;
    
    // Events
    public UnityEvent OnActionEnabled;
    public UnityEvent OnActionExecuted;
    public UnityEvent OnTurnEnded;
    
    // Properties
    public bool CanAct => canAct && !hasActedThisTurn && actionPointsRemaining > 0;
    public bool HasActedThisTurn => hasActedThisTurn;
    public int ActionPointsRemaining => actionPointsRemaining;
    
    // Component references (cached)
    private MovementComponent movementComponent;
    private AttackComponent attackComponent;
    private TeamComponent teamComponent;
    
    protected override void OnEntityInitialized()
    {
        movementComponent = registry.GetComponent<MovementComponent>();
        attackComponent = registry.GetComponent<AttackComponent>();
        teamComponent = registry.GetComponent<TeamComponent>();
    }
    
    public void Initialize(UnitStats stats)
    {
        maxActionPoints = stats.actionPointsPerTurn;
        actionPointsRemaining = maxActionPoints;
    }
    
    public void EnableAction()
    {
        canAct = true;
        hasActedThisTurn = false;
        actionPointsRemaining = maxActionPoints;
        OnActionEnabled?.Invoke();
    }
    
    public void ExecuteAction()
    {
        if (!CanAct) return;
        
        // AI behavior based on available components
        if (attackComponent != null)
        {
            var targets = attackComponent.GetTargetsInRange();
            if (targets.Count > 0)
            {
                // Attack nearest enemy
                var nearestTarget = GetNearestTarget(targets);
                attackComponent.Attack(nearestTarget);
                ConsumeActionPoint();
                return;
            }
        }
        
        if (movementComponent != null)
        {
            // Move towards enemy base or nearest enemy
            var targetPosition = GetOptimalMovePosition();
            if (targetPosition.HasValue)
            {
                movementComponent.MoveTo(targetPosition.Value);
                ConsumeActionPoint();
                return;
            }
        }
        
        // No valid actions, end turn
        EndTurn();
    }
    
    private GameObject GetNearestTarget(List<GameObject> targets)
    {
        // Simple nearest target selection
        GameObject nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var target in targets)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = target;
            }
        }
        
        return nearest;
    }
    
    private Vector2Int? GetOptimalMovePosition()
    {
        if (movementComponent == null) return null;
        
        var validPositions = movementComponent.GetValidMovePositions();
        if (validPositions.Count == 0) return null;
        
        // Move towards enemy base (simple AI)
        var enemyBase = FindEnemyBase();
        if (enemyBase != null)
        {
            return GetClosestPosition(validPositions, enemyBase.transform.position);
        }
        
        return validPositions[0]; // Fallback to any valid position
    }
    
    private GameObject FindEnemyBase()
    {
        var bases = FindObjectsOfType<Base>();
        foreach (var baseObj in bases)
        {
            var baseTeam = baseObj.GetComponent<TeamComponent>();
            if (baseTeam != null && !teamComponent.IsAlly(baseTeam))
            {
                return baseObj.gameObject;
            }
        }
        return null;
    }
    
    private Vector2Int GetClosestPosition(List<Vector2Int> positions, Vector3 target)
    {
        var gridManager = FindObjectOfType<GridManager>();
        Vector2Int closest = positions[0];
        float closestDistance = float.MaxValue;
        
        foreach (var pos in positions)
        {
            Vector3 worldPos = gridManager.GridToWorld(pos);
            float distance = Vector3.Distance(worldPos, target);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = pos;
            }
        }
        
        return closest;
    }
    
    private void ConsumeActionPoint()
    {
        actionPointsRemaining--;
        hasActedThisTurn = actionPointsRemaining <= 0;
        OnActionExecuted?.Invoke();
        
        if (hasActedThisTurn)
        {
            EndTurn();
        }
    }
    
    private void EndTurn()
    {
        canAct = false;
        OnTurnEnded?.Invoke();
    }
}
```

### TeamComponent (Allegiance Management)
```csharp
public class TeamComponent : GameComponent
{
    [SerializeField] private Team team;
    [SerializeField] private bool isNeutral = false;
    
    public Team Team => team;
    public bool IsNeutral => isNeutral;
    
    public bool IsAlly(TeamComponent other)
    {
        if (other == null) return false;
        if (isNeutral || other.isNeutral) return false;
        return team == other.team;
    }
    
    public bool IsEnemy(TeamComponent other)
    {
        if (other == null) return false;
        if (isNeutral || other.isNeutral) return false;
        return team != other.team;
    }
    
    public void SetTeam(Team newTeam)
    {
        team = newTeam;
    }
}

public enum Team
{
    Player,
    Enemy,
    Neutral
}
```

---

## 🎮 Entity Layer Implementation

### ComponentRegistry (Performance & Convenience)
```csharp
public class ComponentRegistry
{
    private readonly Dictionary<Type, GameComponent> components = new Dictionary<Type, GameComponent>();
    private readonly GameObject owner;
    
    public ComponentRegistry(GameObject owner)
    {
        this.owner = owner;
        CacheAllComponents();
    }
    
    private void CacheAllComponents()
    {
        var allComponents = owner.GetComponents<GameComponent>();
        foreach (var component in allComponents)
        {
            components[component.GetType()] = component;
        }
    }
    
    public T GetComponent<T>() where T : GameComponent
    {
        components.TryGetValue(typeof(T), out var component);
        return component as T;
    }
    
    public bool HasComponent<T>() where T : GameComponent
    {
        return components.ContainsKey(typeof(T));
    }
    
    public void RegisterComponent<T>(T component) where T : GameComponent
    {
        components[typeof(T)] = component;
    }
    
    public void UnregisterComponent<T>() where T : GameComponent
    {
        components.Remove(typeof(T));
    }
    
    public IEnumerable<GameComponent> GetAllComponents()
    {
        return components.Values;
    }
}
```

### Entity (Base GameObject Class)
```csharp
public class Entity : MonoBehaviour
{
    [SerializeField] private UnitData unitData;
    
    // Component Registry for performance
    public ComponentRegistry Registry { get; private set; }
    
    // Commonly used components (cached)
    public HealthComponent Health { get; private set; }
    public MovementComponent Movement { get; private set; }
    public AttackComponent Attack { get; private set; }
    public ActionComponent Action { get; private set; }
    public TeamComponent Team { get; private set; }
    
    // Events
    public UnityEvent OnEntityInitialized;
    public UnityEvent OnEntityDestroyed;
    
    protected virtual void Awake()
    {
        // Initialize component registry
        Registry = new ComponentRegistry(gameObject);
        
        // Cache common components
        Health = Registry.GetComponent<HealthComponent>();
        Movement = Registry.GetComponent<MovementComponent>();
        Attack = Registry.GetComponent<AttackComponent>();
        Action = Registry.GetComponent<ActionComponent>();
        Team = Registry.GetComponent<TeamComponent>();
        
        // Subscribe to health events
        if (Health != null)
        {
            Health.OnDeath.AddListener(HandleDeath);
        }
    }
    
    protected virtual void Start()
    {
        InitializeFromData();
        NotifyComponentsInitialized();
        OnEntityInitialized?.Invoke();
    }
    
    private void InitializeFromData()
    {
        if (unitData == null) return;
        
        // Initialize components with data
        Health?.Initialize(unitData.baseStats);
        Movement?.Initialize(unitData.baseStats);
        Attack?.Initialize(unitData.baseStats);
        Action?.Initialize(unitData.baseStats);
    }
    
    private void NotifyComponentsInitialized()
    {
        foreach (var component in Registry.GetAllComponents())
        {
            if (component is GameComponent gameComponent)
            {
                gameComponent.SendMessage("OnEntityInitialized", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
    
    protected virtual void HandleDeath()
    {
        // Default death behavior
        OnEntityDestroyed?.Invoke();
        
        // Remove from grid
        var gridManager = FindObjectOfType<GridManager>();
        gridManager?.RemoveUnit(this);
        
        // Destroy after delay for death animation
        StartCoroutine(DestroyAfterDelay(1f));
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    public void SetUnitData(UnitData data)
    {
        unitData = data;
        if (Application.isPlaying)
        {
            InitializeFromData();
        }
    }
}
```

### Pawn (Mobile Combat Unit)
```csharp
public class Pawn : Entity
{
    [Header("Pawn Settings")]
    [SerializeField] private bool summonSickness = true;
    
    protected override void Start()
    {
        base.Start();
        
        // Handle summoning sickness
        if (summonSickness && Action != null)
        {
            Action.enabled = false;
        }
    }
    
    public void CureSummonSickness()
    {
        if (Action != null)
        {
            Action.enabled = true;
            Action.EnableAction();
        }
    }
    
    protected override void HandleDeath()
    {
        // Pawn-specific death effects
        PlayDeathAnimation();
        SpawnDeathEffects();
        
        base.HandleDeath();
    }
    
    private void PlayDeathAnimation()
    {
        // Animation logic
        var animator = GetComponent<Animator>();
        animator?.SetTrigger("Death");
    }
    
    private void SpawnDeathEffects()
    {
        // Particle effects, sound, etc.
    }
}
```

### Base (Stationary Objective)
```csharp
public class Base : Entity
{
    [Header("Base Settings")]
    [SerializeField] private bool isMainBase = true;
    
    // Events
    public UnityEvent OnBaseDestroyed;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Bases don't move or take normal actions
        if (Movement != null) Movement.enabled = false;
        if (Action != null) Action.enabled = false;
    }
    
    protected override void HandleDeath()
    {
        OnBaseDestroyed?.Invoke();
        
        if (isMainBase)
        {
            // Trigger game over
            var gameManager = FindObjectOfType<GameManager>();
            gameManager?.TriggerGameOver(Team.Team);
        }
        
        base.HandleDeath();
    }
}
```

---

## ⚙️ System Layer Implementation

### GridManager (Spatial Management)
```csharp
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 6;
    [SerializeField] private int gridHeight = 6;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private GameObject tilePrefab;
    
    [Header("Zone Configuration")]
    [SerializeField] private int playerSpawnRows = 2;  // Bottom 2 rows
    [SerializeField] private int enemySpawnRows = 2;   // Top 2 rows
    
    // Grid data
    private Tile[,] grid;
    private Dictionary<Vector2Int, Entity> occupants = new Dictionary<Vector2Int, Entity>();
    
    // Pathfinding cache
    private Dictionary<Vector2Int, List<Vector2Int>> pathCache = new Dictionary<Vector2Int, List<Vector2Int>>();
    
    public int Width => gridWidth;
    public int Height => gridHeight;
    
    // Events
    public UnityEvent<Vector2Int> OnTileClicked;
    public UnityEvent<Entity, Vector2Int> OnUnitPlaced;
    public UnityEvent<Entity, Vector2Int> OnUnitMoved;
    
    private void Awake()
    {
        InitializeGrid();
    }
    
    private void InitializeGrid()
    {
        grid = new Tile[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = GridToWorld(new Vector2Int(x, y));
                GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                
                Tile tile = tileObj.GetComponent<Tile>();
                tile.Initialize(new Vector2Int(x, y), GetTileType(y));
                tile.OnTileClicked.AddListener(HandleTileClick);
                
                grid[x, y] = tile;
            }
        }
    }
    
    private TileType GetTileType(int row)
    {
        if (row < playerSpawnRows)
            return TileType.PlayerSpawn;
        else if (row >= gridHeight - enemySpawnRows)
            return TileType.EnemySpawn;
        else
            return TileType.Neutral;
    }
    
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * tileSize - (gridWidth - 1) * tileSize * 0.5f,
            0,
            gridPos.y * tileSize - (gridHeight - 1) * tileSize * 0.5f
        );
    }
    
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x + (gridWidth - 1) * tileSize * 0.5f) / tileSize);
        int z = Mathf.RoundToInt((worldPos.z + (gridHeight - 1) * tileSize * 0.5f) / tileSize);
        return new Vector2Int(x, z);
    }
    
    public bool IsValidPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth && 
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }
    
    public bool IsOccupied(Vector2Int gridPos)
    {
        return occupants.ContainsKey(gridPos);
    }
    
    public bool TryPlaceUnit(Entity entity, Vector2Int gridPos, Team team)
    {
        if (!IsValidPosition(gridPos) || IsOccupied(gridPos))
            return false;
        
        Tile tile = grid[gridPos.x, gridPos.y];
        if (!tile.CanPlaceUnit(team))
            return false;
        
        // Place unit
        occupants[gridPos] = entity;
        tile.SetOccupant(entity);
        entity.transform.position = GridToWorld(gridPos);
        
        OnUnitPlaced?.Invoke(entity, gridPos);
        return true;
    }
    
    public void RemoveUnit(Entity entity)
    {
        Vector2Int gridPos = WorldToGrid(entity.transform.position);
        if (occupants.ContainsKey(gridPos))
        {
            occupants.Remove(gridPos);
            grid[gridPos.x, gridPos.y].SetOccupant(null);
        }
    }
    
    public void UpdateUnitPosition(Entity entity, Vector2Int fromPos, Vector2Int toPos)
    {
        if (occupants.ContainsKey(fromPos))
        {
            occupants.Remove(fromPos);
            grid[fromPos.x, fromPos.y].SetOccupant(null);
        }
        
        occupants[toPos] = entity;
        grid[toPos.x, toPos.y].SetOccupant(entity);
        OnUnitMoved?.Invoke(entity, toPos);
    }
    
    public List<Vector2Int> GetValidMovePositions(Vector2Int startPos, int range, bool canMoveThrough = false)
    {
        var validPositions = new List<Vector2Int>();
        
        for (int x = startPos.x - range; x <= startPos.x + range; x++)
        {
            for (int y = startPos.y - range; y <= startPos.y + range; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                
                if (!IsValidPosition(pos) || pos == startPos)
                    continue;
                
                int distance = Mathf.Abs(x - startPos.x) + Mathf.Abs(y - startPos.y);
                if (distance > range)
                    continue;
                
                if (!canMoveThrough && IsOccupied(pos))
                    continue;
                
                validPositions.Add(pos);
            }
        }
        
        return validPositions;
    }
    
    public List<Vector2Int> GetTilesInRange(Vector3 worldPos, int range)
    {
        Vector2Int centerPos = WorldToGrid(worldPos);
        return GetValidMovePositions(centerPos, range, true);
    }
    
    public int GetDistance(Vector3 pos1, Vector3 pos2)
    {
        Vector2Int grid1 = WorldToGrid(pos1);
        Vector2Int grid2 = WorldToGrid(pos2);
        return Mathf.Abs(grid1.x - grid2.x) + Mathf.Abs(grid1.y - grid2.y);
    }
    
    public bool HasLineOfSight(Vector3 pos1, Vector3 pos2)
    {
        // Simple line of sight - can be enhanced with raycasting
        Vector2Int grid1 = WorldToGrid(pos1);
        Vector2Int grid2 = WorldToGrid(pos2);
        
        // For now, assume clear line of sight within range
        return GetDistance(pos1, pos2) <= 3;
    }
    
    public List<Vector2Int> GetPath(Vector2Int start, Vector2Int end)
    {
        // Simple pathfinding - can be replaced with A* for complex obstacles
        var path = new List<Vector2Int>();
        Vector2Int current = start;
        
        while (current != end)
        {
            if (current.x < end.x) current.x++;
            else if (current.x > end.x) current.x--;
            else if (current.y < end.y) current.y++;
            else if (current.y > end.y) current.y--;
            
            path.Add(current);
        }
        
        return path;
    }
    
    private void HandleTileClick(Vector2Int gridPos)
    {
        OnTileClicked?.Invoke(gridPos);
    }
    
    public Entity GetEntityAt(Vector2Int gridPos)
    {
        occupants.TryGetValue(gridPos, out Entity entity);
        return entity;
    }
    
    public List<Entity> GetAllEntitiesOfTeam(Team team)
    {
        var entities = new List<Entity>();
        foreach (var entity in occupants.Values)
        {
            var teamComponent = entity.GetComponent<TeamComponent>();
            if (teamComponent != null && teamComponent.Team == team)
            {
                entities.Add(entity);
            }
        }
        return entities;
    }
}

public enum TileType
{
    Neutral,
    PlayerSpawn,
    EnemySpawn,
    Blocked
}
```

### Tile Component
```csharp
public class Tile : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private TileType tileType;
    [SerializeField] private Entity occupant;
    
    [Header("Visual")]
    [SerializeField] private Renderer tileRenderer;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material playerSpawnMaterial;
    [SerializeField] private Material enemySpawnMaterial;
    
    // Events
    public UnityEvent<Vector2Int> OnTileClicked;
    
    public Vector2Int GridPosition => gridPosition;
    public TileType TileType => tileType;
    public Entity Occupant => occupant;
    public bool IsOccupied => occupant != null;
    
    public void Initialize(Vector2Int position, TileType type)
    {
        gridPosition = position;
        tileType = type;
        UpdateVisual();
    }
    
    public bool CanPlaceUnit(Team team)
    {
        if (IsOccupied) return false;
        
        switch (tileType)
        {
            case TileType.PlayerSpawn:
                return team == Team.Player;
            case TileType.EnemySpawn:
                return team == Team.Enemy;
            case TileType.Neutral:
                return true;
            case TileType.Blocked:
                return false;
            default:
                return false;
        }
    }
    
    public void SetOccupant(Entity newOccupant)
    {
        occupant = newOccupant;
        UpdateVisual();
    }
    
    public void SetHighlight(bool highlighted)
    {
        if (highlighted)
        {
            tileRenderer.material = highlightMaterial;
        }
        else
        {
            UpdateVisual();
        }
    }
    
    private void UpdateVisual()
    {
        switch (tileType)
        {
            case TileType.PlayerSpawn:
                tileRenderer.material = playerSpawnMaterial;
                break;
            case TileType.EnemySpawn:
                tileRenderer.material = enemySpawnMaterial;
                break;
            default:
                tileRenderer.material = defaultMaterial;
                break;
        }
    }
    
    private void OnMouseDown()
    {
        OnTileClicked?.Invoke(gridPosition);
    }
}
```

### TurnManager (Game Flow State Machine)
```csharp
public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private float betweenPhaseDelay = 1f;
    [SerializeField] private float actionDelay = 0.5f;
    
    // Current state
    [SerializeField] private GamePhase currentPhase = GamePhase.PlayerTurn;
    [SerializeField] private int turnNumber = 1;
    
    // Events
    public UnityEvent<GamePhase> OnPhaseChanged;
    public UnityEvent<int> OnTurnStarted;
    public UnityEvent OnGameEnded;
    
    public GamePhase CurrentPhase => currentPhase;
    public int TurnNumber => turnNumber;
    
    private GridManager gridManager;
    private CardSystem cardSystem;
    
    private void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
        cardSystem = FindObjectOfType<CardSystem>();
    }
    
    private void Start()
    {
        StartTurn();
    }
    
    public void EndCurrentPhase()
    {
        switch (currentPhase)
        {
            case GamePhase.PlayerTurn:
                StartCoroutine(TransitionToBetweenPhase());
                break;
            case GamePhase.EnemyTurn:
                StartCoroutine(TransitionToBetweenPhase());
                break;
            case GamePhase.BetweenPhase:
                AdvanceToNextTurn();
                break;
        }
    }
    
    private void StartTurn()
    {
        turnNumber++;
        OnTurnStarted?.Invoke(turnNumber);
        
        // Start with enemy turn
        SetPhase(GamePhase.EnemyTurn);
        PreparePhase();
    }
    
    private void SetPhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        OnPhaseChanged?.Invoke(currentPhase);
    }
    
    private void PreparePhase()
    {
        switch (currentPhase)
        {
            case GamePhase.PlayerTurn:
                PreparePlayerTurn();
                break;
            case GamePhase.EnemyTurn:
                PrepareEnemyTurn();
                break;
            case GamePhase.BetweenPhase:
                StartCoroutine(ExecuteBetweenPhase());
                break;
        }
    }
    
    private void PreparePlayerTurn()
    {
        var playerEntities = gridManager.GetAllEntitiesOfTeam(Team.Player);
        EnableActionsForEntities(playerEntities);
        
        // Draw card, add mana, etc.
        cardSystem?.StartPlayerTurn();
    }
    
    private void PrepareEnemyTurn()
    {
        var enemyEntities = gridManager.GetAllEntitiesOfTeam(Team.Enemy);
        EnableActionsForEntities(enemyEntities);
        
        // AI card playing logic
        cardSystem?.StartEnemyTurn();
    }
    
    private void EnableActionsForEntities(List<Entity> entities)
    {
        foreach (var entity in entities)
        {
            var actionComponent = entity.Action;
            if (actionComponent != null)
            {
                actionComponent.EnableAction();
            }
        }
    }
    
    private IEnumerator TransitionToBetweenPhase()
    {
        yield return new WaitForSeconds(betweenPhaseDelay);
        SetPhase(GamePhase.BetweenPhase);
        PreparePhase();
    }
    
    private IEnumerator ExecuteBetweenPhase()
    {
        // Execute actions for all units that can act
        var allEntities = new List<Entity>();
        allEntities.AddRange(gridManager.GetAllEntitiesOfTeam(Team.Enemy));
        allEntities.AddRange(gridManager.GetAllEntitiesOfTeam(Team.Player));
        
        foreach (var entity in allEntities)
        {
            var actionComponent = entity.Action;
            if (actionComponent != null && actionComponent.CanAct)
            {
                actionComponent.ExecuteAction();
                yield return new WaitForSeconds(actionDelay);
            }
        }
        
        // End between phase
        EndCurrentPhase();
    }
    
    private void AdvanceToNextTurn()
    {
        // Alternate between player and enemy turns
        if (currentPhase == GamePhase.BetweenPhase)
        {
            SetPhase(GamePhase.PlayerTurn);
            PreparePhase();
        }
    }
    
    public void ForceEndPhase()
    {
        EndCurrentPhase();
    }
    
    public void EndGame(Team winner)
    {
        SetPhase(GamePhase.GameEnded);
        OnGameEnded?.Invoke();
    }
}

public enum GamePhase
{
    PlayerTurn,
    EnemyTurn, 
    BetweenPhase,
    GameEnded
}
```

### CombatSystem (Event-Driven Combat Resolution)
```csharp
public class CombatSystem : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float combatAnimationDuration = 1f;
    [SerializeField] private LayerMask combatLayers = -1;
    
    // Events
    public UnityEvent<GameObject, GameObject, int> OnCombatResolved;
    public UnityEvent<GameObject> OnUnitDestroyed;
    
    private GridManager gridManager;
    
    private void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
        SubscribeToAttackEvents();
    }
    
    private void SubscribeToAttackEvents()
    {
        // Find all existing attack components and subscribe
        var attackComponents = FindObjectsOfType<AttackComponent>();
        foreach (var attack in attackComponents)
        {
            attack.OnAttackInitiated.AddListener(HandleAttackInitiated);
        }
    }
    
    private void HandleAttackInitiated(GameObject target)
    {
        var attacker = GetComponent<AttackComponent>();
        if (attacker == null) return;
        
        StartCoroutine(ResolveCombat(attacker, target));
    }
    
    private IEnumerator ResolveCombat(AttackComponent attacker, GameObject target)
    {
        // Calculate damage
        int damage = CalculateDamage(attacker, target);
        
        // Play combat animation
        yield return StartCoroutine(PlayCombatAnimation(attacker.gameObject, target));
        
        // Apply damage
        var targetHealth = target.GetComponent<HealthComponent>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, attacker.gameObject);
            
            // Fire events
            attacker.OnDamageDealt?.Invoke(target, damage);
            OnCombatResolved?.Invoke(attacker.gameObject, target, damage);
            
            // Check if target was destroyed
            if (targetHealth.IsDead)
            {
                OnUnitDestroyed?.Invoke(target);
            }
        }
    }
    
    private int CalculateDamage(AttackComponent attacker, GameObject target)
    {
        int baseDamage = attacker.AttackPower;
        
        // Apply modifiers, armor, etc.
        // This is where you'd add complex combat formulas
        
        return baseDamage;
    }
    
    private IEnumerator PlayCombatAnimation(GameObject attacker, GameObject target)
    {
        // Simple animation - move attacker towards target and back
        Vector3 attackerStart = attacker.transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 attackPos = Vector3.Lerp(attackerStart, targetPos, 0.7f);
        
        // Move towards target
        float elapsed = 0f;
        while (elapsed < combatAnimationDuration * 0.5f)
        {
            attacker.transform.position = Vector3.Lerp(attackerStart, attackPos, elapsed / (combatAnimationDuration * 0.5f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Move back
        elapsed = 0f;
        while (elapsed < combatAnimationDuration * 0.5f)
        {
            attacker.transform.position = Vector3.Lerp(attackPos, attackerStart, elapsed / (combatAnimationDuration * 0.5f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        attacker.transform.position = attackerStart;
    }
    
    // Called when new entities are spawned
    public void RegisterAttackComponent(AttackComponent attackComponent)
    {
        attackComponent.OnAttackInitiated.AddListener(HandleAttackInitiated);
    }
    
    // Called when entities are destroyed
    public void UnregisterAttackComponent(AttackComponent attackComponent)
    {
        attackComponent.OnAttackInitiated.RemoveListener(HandleAttackInitiated);
    }
}
```

### CardSystem (Card Play Processing)
```csharp
public class CardSystem : MonoBehaviour
{
    [Header("Card Settings")]
    [SerializeField] private int startingHandSize = 5;
    [SerializeField] private int maxHandSize = 7;
    [SerializeField] private int cardsPerTurn = 1;
    
    [Header("Resources")]
    [SerializeField] private int startingMana = 3;
    [SerializeField] private int maxMana = 10;
    [SerializeField] private int manaPerTurn = 1;
    
    // Game state
    [SerializeField] private int currentMana;
    [SerializeField] private List<CardData> playerHand = new List<CardData>();
    [SerializeField] private List<CardData> playerDeck = new List<CardData>();
    [SerializeField] private List<CardData> enemyHand = new List<CardData>();
    [SerializeField] private List<CardData> enemyDeck = new List<CardData>();
    
    // Events
    public UnityEvent<CardData> OnCardPlayed;
    public UnityEvent<CardData> OnCardDrawn;
    public UnityEvent<int> OnManaChanged;
    
    // State
    private bool waitingForTileSelection = false;
    private CardData pendingCard;
    
    private GridManager gridManager;
    private TurnManager turnManager;
    
    public int CurrentMana => currentMana;
    public List<CardData> PlayerHand => new List<CardData>(playerHand);
    public bool WaitingForTileSelection => waitingForTileSelection;
    
    private void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
        turnManager = FindObjectOfType<TurnManager>();
        
        currentMana = startingMana;
    }
    
    private void Start()
    {
        // Subscribe to grid events
        gridManager.OnTileClicked.AddListener(HandleTileClicked);
        
        // Draw starting hands
        DrawCards(playerHand, playerDeck, startingHandSize);
        DrawCards(enemyHand, enemyDeck, startingHandSize);
    }
    
    public void StartPlayerTurn()
    {
        // Add mana
        currentMana = Mathf.Min(currentMana + manaPerTurn, maxMana);
        OnManaChanged?.Invoke(currentMana);
        
        // Draw card
        if (playerHand.Count < maxHandSize)
        {
            DrawCards(playerHand, playerDeck, cardsPerTurn);
        }
    }
    
    public void StartEnemyTurn()
    {
        // AI plays cards automatically
        StartCoroutine(ExecuteEnemyTurn());
    }
    
    private IEnumerator ExecuteEnemyTurn()
    {
        // Simple AI: play all affordable cards
        var playableCards = enemyHand.Where(card => card.cost <= currentMana).ToList();
        
        foreach (var card in playableCards)
        {
            if (TryPlayCard(card, Team.Enemy))
            {
                enemyHand.Remove(card);
                yield return new WaitForSeconds(1f);
            }
        }
        
        // End enemy turn
        yield return new WaitForSeconds(1f);
        turnManager.EndCurrentPhase();
    }
    
    public bool CanPlayCard(CardData card)
    {
        if (card == null || currentMana < card.cost)
            return false;
        
        if (turnManager.CurrentPhase != GamePhase.PlayerTurn)
            return false;
        
        return true;
    }
    
    public void RequestPlayCard(CardData card)
    {
        if (!CanPlayCard(card)) return;
        
        if (card.cardType == CardType.Spell)
        {
            // Play spell immediately
            PlaySpell(card);
            playerHand.Remove(card);
            currentMana -= card.cost;
            OnManaChanged?.Invoke(currentMana);
            OnCardPlayed?.Invoke(card);
        }
        else if (card.cardType == CardType.Pawn)
        {
            // Wait for tile selection
            pendingCard = card;
            waitingForTileSelection = true;
            HighlightValidSpawnTiles(Team.Player);
        }
    }
    
    private void HandleTileClicked(Vector2Int gridPos)
    {
        if (!waitingForTileSelection || pendingCard == null) return;
        
        if (TrySpawnUnit(pendingCard, gridPos, Team.Player))
        {
            playerHand.Remove(pendingCard);
            currentMana -= pendingCard.cost;
            OnManaChanged?.Invoke(currentMana);
            OnCardPlayed?.Invoke(pendingCard);
            
            ClearTileSelection();
        }
    }
    
    private bool TryPlayCard(CardData card, Team team)
    {
        if (card.cardType == CardType.Spell)
        {
            PlaySpell(card);
            return true;
        }
        else if (card.cardType == CardType.Pawn)
        {
            // AI chooses random valid spawn location
            var validTiles = GetValidSpawnTiles(team);
            if (validTiles.Count > 0)
            {
                var randomTile = validTiles[Random.Range(0, validTiles.Count)];
                return TrySpawnUnit(card, randomTile, team);
            }
        }
        
        return false;
    }
    
    private bool TrySpawnUnit(CardData card, Vector2Int gridPos, Team team)
    {
        if (card.unitToSummon == null) return false;
        
        // Instantiate unit
        GameObject unitObj = Instantiate(card.unitToSummon.unitPrefab);
        Entity entity = unitObj.GetComponent<Entity>();
        entity.SetUnitData(card.unitToSummon);
        
        // Set team
        var teamComponent = entity.GetComponent<TeamComponent>();
        if (teamComponent != null)
        {
            teamComponent.SetTeam(team);
        }
        
        // Place on grid
        bool placed = gridManager.TryPlaceUnit(entity, gridPos, team);
        if (!placed)
        {
            Destroy(unitObj);
            return false;
        }
        
        return true;
    }
    
    private void PlaySpell(CardData card)
    {
        if (card.spellEffect != null)
        {
            card.spellEffect.Execute();
        }
    }
    
    private List<Vector2Int> GetValidSpawnTiles(Team team)
    {
        var validTiles = new List<Vector2Int>();
        
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!gridManager.IsOccupied(pos))
                {
                    // Check if tile allows spawning for this team
                    // This would need to be implemented in GridManager
                    validTiles.Add(pos);
                }
            }
        }
        
        return validTiles;
    }
    
    private void HighlightValidSpawnTiles(Team team)
    {
        var validTiles = GetValidSpawnTiles(team);
        // Highlight tiles in UI
        // This would be implemented with tile visual feedback
    }
    
    private void ClearTileSelection()
    {
        waitingForTileSelection = false;
        pendingCard = null;
        // Clear tile highlights
    }
    
    private void DrawCards(List<CardData> hand, List<CardData> deck, int count)
    {
        for (int i = 0; i < count && deck.Count > 0; i++)
        {
            CardData drawnCard = deck[0];
            deck.RemoveAt(0);
            hand.Add(drawnCard);
            OnCardDrawn?.Invoke(drawnCard);
        }
    }
}
```

---

## 🔧 Extensibility & Future Expansion

### Component-Based Extensibility
The component architecture enables easy feature expansion:

```csharp
// Example: Add shield mechanics
public class ShieldComponent : GameComponent
{
    [SerializeField] private int shieldPoints;
    [SerializeField] private int maxShieldPoints;
    
    public void AbsorbDamage(ref int damage)
    {
        int absorbed = Mathf.Min(damage, shieldPoints);
        shieldPoints -= absorbed;
        damage -= absorbed;
    }
}

// Modify HealthComponent to check for shields
public class HealthComponent : GameComponent
{
    private ShieldComponent shieldComponent;
    
    protected override void OnEntityInitialized()
    {
        shieldComponent = registry.GetComponent<ShieldComponent>();
    }
    
    public void TakeDamage(int damage, GameObject source = null)
    {
        // Apply shield absorption
        if (shieldComponent != null)
        {
            shieldComponent.AbsorbDamage(ref damage);
        }
        
        // Rest of damage logic...
    }
}
```

### Event-Driven Architecture Benefits
1. **Loose Coupling**: Components don't need direct references
2. **Easy Testing**: Mock events for unit testing
3. **Runtime Flexibility**: Add/remove event listeners dynamically
4. **UI Integration**: UI can listen to same events as game logic

### AI Strategy Extensibility
```csharp
public abstract class AIStrategy : ScriptableObject
{
    public abstract void ExecuteTurn(Entity entity, GridManager grid);
}

[CreateAssetMenu(menuName = "AI/Aggressive Strategy")]
public class AggressiveAI : AIStrategy
{
    public override void ExecuteTurn(Entity entity, GridManager grid)
    {
        // Attack nearest enemy implementation
    }
}

// ActionComponent can use different strategies
public class ActionComponent : GameComponent
{
    [SerializeField] private AIStrategy aiStrategy;
    
    public void ExecuteAction()
    {
        if (aiStrategy != null)
        {
            aiStrategy.ExecuteTurn(entity, gridManager);
        }
    }
}
```

### Spell Effect System
```csharp
public abstract class SpellEffect : ScriptableObject
{
    public abstract void Execute();
}

[CreateAssetMenu(menuName = "Spells/Damage Spell")]
public class DamageSpell : SpellEffect
{
    [SerializeField] private int damage = 10;
    [SerializeField] private int range = 2;
    
    public override void Execute()
    {
        // Target selection and damage application
    }
}

[CreateAssetMenu(menuName = "Spells/Heal Spell")]
public class HealSpell : SpellEffect
{
    [SerializeField] private int healAmount = 15;
    
    public override void Execute()
    {
        // Healing logic
    }
}
```

---

## 🎯 Implementation Guidelines

### Development Order
1. **Data Structures**: Implement UnitStats, UnitData, CardData
2. **Core Components**: HealthComponent, TeamComponent first
3. **Grid System**: GridManager and Tile setup
4. **Basic Entity**: Entity class with component registry
5. **Movement & Combat**: MovementComponent, AttackComponent
6. **Turn Management**: TurnManager state machine
7. **Card System**: Basic card playing mechanics
8. **AI & Polish**: ActionComponent AI, animations, effects

### Testing Strategy
```csharp
// Example component test
[Test]
public void HealthComponent_TakeDamage_ReducesHealth()
{
    // Arrange
    var gameObject = new GameObject();
    var health = gameObject.AddComponent<HealthComponent>();
    var stats = new UnitStats { maxHealth = 100 };
    health.Initialize(stats);
    
    // Act
    health.TakeDamage(30);
    
    // Assert
    Assert.AreEqual(70, health.CurrentHealth);
}
```

### Performance Considerations
1. **Component Caching**: Use ComponentRegistry for fast lookups
2. **Event Management**: Unsubscribe from events on destroy
3. **Object Pooling**: Pool frequently created/destroyed entities
4. **Spatial Partitioning**: Grid provides efficient spatial queries

This component-based architecture provides a solid foundation for your tactical card game while maintaining flexibility for future features and expansions. The separation of data, behavior, and systems ensures clean code organization and easy testing.

<function_calls>
<invoke name="TodoWrite">
<parameter name="todos">[{"content": "Analyze requirements and design core architecture layers", "status": "completed", "activeForm": "Analyzing requirements and designing core architecture layers"}, {"content": "Design component-based system architecture with data/component/entity/system layers", "status": "completed", "activeForm": "Designing component-based system architecture"}, {"content": "Create detailed component specifications and interaction patterns", "status": "completed", "activeForm": "Creating detailed component specifications"}, {"content": "Design system layer architecture (GridManager, TurnManager, CombatSystem, CardSystem)", "status": "completed", "activeForm": "Designing system layer architecture"}, {"content": "Specify data structures and ScriptableObject implementations", "status": "completed", "activeForm": "Specifying data structures and ScriptableObject implementations"}, {"content": "Design extensibility patterns and future expansion considerations", "status": "in_progress", "activeForm": "Designing extensibility patterns"}, {"content": "Create comprehensive code examples and implementation guidance", "status": "pending", "activeForm": "Creating comprehensive code examples"}]