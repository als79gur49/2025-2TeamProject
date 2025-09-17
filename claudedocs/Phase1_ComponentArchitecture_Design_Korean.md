# Phase 1: 컴포넌트 기반 아키텍처 설계
## Unity 전술 카드 게임 - 핵심 게임플레이 루프

### 아키텍처 개요

이 문서는 Unity 전술 카드 게임을 위한 포괄적인 컴포넌트 기반 아키텍처를 제시하며, **확장성**, **캡슐화**, **데이터 기반 설계**를 강조합니다. 기존의 인터페이스 기반 상속 대신, 이 아키텍처는 최대한의 유연성과 유지보수성을 위해 **컴포넌트 조합**을 사용합니다.

## 🏗️ 아키텍처 레이어

### 1. 데이터 레이어 (순수 C# 클래스 & ScriptableObjects)
- **UnitStats**: 직렬화 가능한 데이터 컨테이너
- **UnitData**: ScriptableObject 청사진  
- **CardData**: 카드 정의 및 구성

### 2. 컴포넌트 레이어 (MonoBehaviour 컴포넌트)
- **HealthComponent**: 체력 및 피해 관리
- **MovementComponent**: 이동 능력 및 경로찾기
- **AttackComponent**: 전투 행동 및 타겟팅
- **ActionComponent**: 턴 기반 행동 조정
- **TeamComponent**: 팀 소속 및 충성도

### 3. 엔티티 레이어 (GameObject 조합)
- **Entity**: 컴포넌트 레지스트리를 가진 기본 클래스
- **Pawn**: 이동 가능한 전투 유닛
- **Base**: 고정 목표물

### 4. 시스템 레이어 (매니저 클래스)
- **GridManager**: 보드 상태 및 공간 쿼리
- **TurnManager**: 게임 흐름 상태 머신
- **CombatSystem**: 전투 해결
- **CardSystem**: 카드 플레이 처리

---

## 📊 데이터 레이어 구현

### UnitStats (순수 데이터 컨테이너)
```csharp
[System.Serializable]
public class UnitStats
{
    [Header("전투 능력치")]
    [SerializeField] private int maxHealth = 100;        // 최대 체력
    [SerializeField] private int attackPower = 10;       // 공격력
    
    [Header("이동")]
    [SerializeField] private int movementRange = 2;      // 이동 범위
    
    [Header("행동 경제")]
    [SerializeField] private int actionPointsPerTurn = 1; // 턴당 행동 포인트
    
    // 읽기 전용 프로퍼티 - 캡슐화된 접근으로 데이터 보호
    public int MaxHealth => maxHealth;
    public int AttackPower => attackPower;
    public int MovementRange => movementRange;
    public int ActionPointsPerTurn => actionPointsPerTurn;
    
    // 런타임 인스턴스를 위한 복사 생성자 - 원본 데이터 보호
    public UnitStats(UnitStats original)
    {
        maxHealth = original.maxHealth;
        attackPower = original.attackPower;
        movementRange = original.movementRange;
        actionPointsPerTurn = original.actionPointsPerTurn;
    }
    
    // 안전한 능력치 수정 메서드 - 유효성 검사 포함
    public void ModifyStats(int healthMod, int attackMod, int movementMod)
    {
        maxHealth = Mathf.Max(1, maxHealth + healthMod);
        attackPower = Mathf.Max(0, attackPower + attackMod);
        movementRange = Mathf.Max(1, movementRange + movementMod);
    }
    
    // 수정된 능력치를 위한 팩토리 메서드 (버프/디버프)
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
    [SerializeField] private int healthBonus;    // 체력 보너스
    [SerializeField] private int attackBonus;    // 공격력 보너스
    [SerializeField] private int movementBonus;  // 이동력 보너스
    
    // 읽기 전용 프로퍼티
    public int HealthBonus => healthBonus;
    public int AttackBonus => attackBonus;
    public int MovementBonus => movementBonus;
    
    public void Apply(UnitStats stats)
    {
        stats.ModifyStats(healthBonus, attackBonus, movementBonus);
    }
}
```

### UnitData (ScriptableObject 청사진)
```csharp
[CreateAssetMenu(fileName = "새 유닛", menuName = "게임/유닛 데이터")]
public class UnitData : ScriptableObject
{
    [Header("정체성")]
    public string unitName = "기본 유닛";
    public Sprite unitIcon;
    
    [Header("능력치")]
    public UnitStats baseStats;
    
    [Header("프리팹")]
    public GameObject unitPrefab;
    
    [Header("비주얼")]
    public Material unitMaterial;
    public AnimationClip[] animations;
    
    // 에디터에서 검증
    private void OnValidate()
    {
        if (unitPrefab != null && unitPrefab.GetComponent<Entity>() == null)
        {
            Debug.LogWarning($"유닛 프리팹 {unitPrefab.name}에 Entity 컴포넌트가 필요합니다");
        }
    }
}
```

### CardData (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "새 카드", menuName = "게임/카드 데이터")]
public class CardData : ScriptableObject
{
    [Header("카드 정체성")]
    public string cardName = "기본 카드";
    public string description = "";
    public Sprite cardIllustration;
    
    [Header("게임플레이")]
    public CardType cardType;
    public int cost = 1;
    
    [Header("유닛 카드 데이터")]
    [ShowIf("cardType", CardType.Pawn)]
    public UnitData unitToSummon;
    
    [Header("스펠 카드 데이터")]
    [ShowIf("cardType", CardType.Spell)]
    public SpellEffect spellEffect;
    
    [Header("희귀도 & 수집")]
    public CardRarity rarity = CardRarity.Common;
    public bool isCollectable = true;
}

public enum CardType
{
    Pawn,    // 유닛 소환
    Spell    // 즉시 효과
}

public enum CardRarity
{
    Common,    // 일반
    Uncommon,  // 고급
    Rare,      // 희귀
    Epic,      // 영웅
    Legendary  // 전설
}
```

---

## 🧩 컴포넌트 레이어 구현

### 컴포넌트 기본 패턴
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
    
    // 컴포넌트별 초기화를 위한 오버라이드
    protected virtual void OnEntityInitialized() { }
    
    // 컴포넌트가 추가/제거될 때 호출
    protected virtual void OnComponentRegistered() { }
    protected virtual void OnComponentUnregistered() { }
}
```

### HealthComponent (피해 & 죽음 관리)
```csharp
public class HealthComponent : GameComponent
{
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    
    // 느슨한 결합을 위한 이벤트
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int, GameObject> OnDamageTaken;
    public UnityEvent OnDeath;
    public UnityEvent OnHealed;
    
    // 속성
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
        
        // 이벤트 발생
        OnDamageTaken?.Invoke(actualDamage, source);
        OnHealthChanged?.Invoke(currentHealth);
        
        // 죽음 확인
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

### AttackComponent (전투 행동)
```csharp
public class AttackComponent : GameComponent
{
    [SerializeField] private int attackPower;
    [SerializeField] private int attackRange = 1;
    [SerializeField] private LayerMask targetLayers = -1;
    
    // 이벤트
    public UnityEvent<GameObject> OnAttackInitiated;
    public UnityEvent<GameObject, int> OnDamageDealt;
    public UnityEvent OnAttackMissed;
    
    // 속성
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
        
        // 팀 소속 확인
        var targetTeam = target.GetComponent<TeamComponent>();
        if (targetTeam == null || teamComponent.IsAlly(targetTeam)) return false;
        
        // 범위 확인
        var distance = gridManager.GetDistance(transform.position, target.transform.position);
        if (distance > attackRange) return false;
        
        // 시야 확인
        return gridManager.HasLineOfSight(transform.position, target.transform.position);
    }
    
    public void Attack(GameObject target)
    {
        if (!CanAttackTarget(target)) return;
        
        OnAttackInitiated?.Invoke(target);
        
        // 피해 계산과 적용은 CombatSystem에서 처리
        // 이 컴포넌트는 공격을 개시하기만 함
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

### MovementComponent (공간 이동)
```csharp
public class MovementComponent : GameComponent
{
    [SerializeField] private int movementRange;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool canMoveThrough = false;
    
    // 이벤트
    public UnityEvent<Vector2Int> OnMoveStarted;
    public UnityEvent<Vector2Int> OnMoveCompleted;
    public UnityEvent OnMoveFailed;
    
    // 상태
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
        
        // GridManager에서 경로 가져오기
        var path = gridManager.GetPath(currentGridPosition, targetPosition);
        
        // 경로를 따라 이동 애니메이션
        foreach (var point in path)
        {
            Vector3 worldPos = gridManager.GridToWorld(point);
            yield return StartCoroutine(MoveToPosition(worldPos));
        }
        
        // 그리드 등록 업데이트
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

### ActionComponent (턴 조정)
```csharp
public class ActionComponent : GameComponent
{
    [SerializeField] private bool canAct = false;
    [SerializeField] private bool hasActedThisTurn = false;
    [SerializeField] private int actionPointsRemaining = 0;
    [SerializeField] private int maxActionPoints = 1;
    
    // 이벤트
    public UnityEvent OnActionEnabled;
    public UnityEvent OnActionExecuted;
    public UnityEvent OnTurnEnded;
    
    // 속성
    public bool CanAct => canAct && !hasActedThisTurn && actionPointsRemaining > 0;
    public bool HasActedThisTurn => hasActedThisTurn;
    public int ActionPointsRemaining => actionPointsRemaining;
    
    // 컴포넌트 참조 (캐시됨)
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
        
        // 사용 가능한 컴포넌트에 기반한 AI 행동
        if (attackComponent != null)
        {
            var targets = attackComponent.GetTargetsInRange();
            if (targets.Count > 0)
            {
                // 가장 가까운 적 공격
                var nearestTarget = GetNearestTarget(targets);
                attackComponent.Attack(nearestTarget);
                ConsumeActionPoint();
                return;
            }
        }
        
        if (movementComponent != null)
        {
            // 적 기지나 가장 가까운 적으로 이동
            var targetPosition = GetOptimalMovePosition();
            if (targetPosition.HasValue)
            {
                movementComponent.MoveTo(targetPosition.Value);
                ConsumeActionPoint();
                return;
            }
        }
        
        // 유효한 행동이 없으면 턴 종료
        EndTurn();
    }
    
    private GameObject GetNearestTarget(List<GameObject> targets)
    {
        // 간단한 가장 가까운 타겟 선택
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
        
        // 적 기지 방향으로 이동 (간단한 AI)
        var enemyBase = FindEnemyBase();
        if (enemyBase != null)
        {
            return GetClosestPosition(validPositions, enemyBase.transform.position);
        }
        
        return validPositions[0]; // 임의의 유효한 위치로 폴백
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

### TeamComponent (소속 관리)
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
    Player,   // 플레이어
    Enemy,    // 적
    Neutral   // 중립
}
```

---

## 🎮 엔티티 레이어 구현

### ComponentRegistry (성능 & 편의성)
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

### Entity (기본 GameObject 클래스)
```csharp
public class Entity : MonoBehaviour
{
    [SerializeField] private UnitData unitData;
    
    // 성능을 위한 컴포넌트 레지스트리
    public ComponentRegistry Registry { get; private set; }
    
    // 자주 사용되는 컴포넌트 (캐시됨)
    public HealthComponent Health { get; private set; }
    public MovementComponent Movement { get; private set; }
    public AttackComponent Attack { get; private set; }
    public ActionComponent Action { get; private set; }
    public TeamComponent Team { get; private set; }
    
    // 이벤트
    public UnityEvent OnEntityInitialized;
    public UnityEvent OnEntityDestroyed;
    
    protected virtual void Awake()
    {
        // 컴포넌트 레지스트리 초기화
        Registry = new ComponentRegistry(gameObject);
        
        // 일반적인 컴포넌트 캐시
        Health = Registry.GetComponent<HealthComponent>();
        Movement = Registry.GetComponent<MovementComponent>();
        Attack = Registry.GetComponent<AttackComponent>();
        Action = Registry.GetComponent<ActionComponent>();
        Team = Registry.GetComponent<TeamComponent>();
        
        // 체력 이벤트 구독
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
        
        // 원본 데이터를 복사해서 런타임 사용 - 원본 보호
        var runtimeStats = new UnitStats(unitData.baseStats);
        
        // 복사본으로 컴포넌트 초기화 - 데이터 무결성 보장
        Health?.Initialize(runtimeStats);
        Movement?.Initialize(runtimeStats);
        Attack?.Initialize(runtimeStats);
        Action?.Initialize(runtimeStats);
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
        // 기본 죽음 행동
        OnEntityDestroyed?.Invoke();
        
        // 그리드에서 제거
        var gridManager = FindObjectOfType<GridManager>();
        gridManager?.RemoveUnit(this);
        
        // 죽음 애니메이션을 위한 지연 후 파괴
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

### Pawn (이동 가능한 전투 유닛)
```csharp
public class Pawn : Entity
{
    [Header("폰 설정")]
    [SerializeField] private bool summonSickness = true; // 소환 후유증
    
    protected override void Start()
    {
        base.Start();
        
        // 소환 후유증 처리
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
        // 폰 특화 죽음 효과
        PlayDeathAnimation();
        SpawnDeathEffects();
        
        base.HandleDeath();
    }
    
    private void PlayDeathAnimation()
    {
        // 애니메이션 로직
        var animator = GetComponent<Animator>();
        animator?.SetTrigger("Death");
    }
    
    private void SpawnDeathEffects()
    {
        // 파티클 효과, 사운드 등
    }
}
```

### Base (고정 목표물)
```csharp
public class Base : Entity
{
    [Header("기지 설정")]
    [SerializeField] private bool isMainBase = true; // 주 기지인지
    
    // 이벤트
    public UnityEvent OnBaseDestroyed;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 기지는 이동하거나 일반적인 행동을 하지 않음
        if (Movement != null) Movement.enabled = false;
        if (Action != null) Action.enabled = false;
    }
    
    protected override void HandleDeath()
    {
        OnBaseDestroyed?.Invoke();
        
        if (isMainBase)
        {
            // 게임 오버 트리거
            var gameManager = FindObjectOfType<GameManager>();
            gameManager?.TriggerGameOver(Team.Team);
        }
        
        base.HandleDeath();
    }
}
```

---

## ⚙️ 시스템 레이어 구현

### GridManager (공간 관리)
```csharp
public class GridManager : MonoBehaviour
{
    [Header("그리드 설정")]
    [SerializeField] private int gridWidth = 6;     // 그리드 너비
    [SerializeField] private int gridHeight = 6;    // 그리드 높이
    [SerializeField] private float tileSize = 1f;   // 타일 크기
    [SerializeField] private GameObject tilePrefab; // 타일 프리팹
    
    [Header("구역 구성")]
    [SerializeField] private int playerSpawnRows = 2;  // 하단 2줄
    [SerializeField] private int enemySpawnRows = 2;   // 상단 2줄
    
    // 그리드 데이터
    private Tile[,] grid;
    private Dictionary<Vector2Int, Entity> occupants = new Dictionary<Vector2Int, Entity>();
    
    // 경로찾기 캐시
    private Dictionary<Vector2Int, List<Vector2Int>> pathCache = new Dictionary<Vector2Int, List<Vector2Int>>();
    
    public int Width => gridWidth;
    public int Height => gridHeight;
    
    // 이벤트
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
        
        // 유닛 배치
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
        // 간단한 시야 - 레이캐스팅으로 향상 가능
        Vector2Int grid1 = WorldToGrid(pos1);
        Vector2Int grid2 = WorldToGrid(pos2);
        
        // 현재는 범위 내에서 깨끗한 시야라고 가정
        return GetDistance(pos1, pos2) <= 3;
    }
    
    public List<Vector2Int> GetPath(Vector2Int start, Vector2Int end)
    {
        // 간단한 경로찾기 - 복잡한 장애물을 위해 A*로 교체 가능
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
    Neutral,      // 중립
    PlayerSpawn,  // 플레이어 소환 지역
    EnemySpawn,   // 적 소환 지역
    Blocked       // 차단됨
}
```

### Tile 컴포넌트
```csharp
public class Tile : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private TileType tileType;
    [SerializeField] private Entity occupant;
    
    [Header("비주얼")]
    [SerializeField] private Renderer tileRenderer;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material playerSpawnMaterial;
    [SerializeField] private Material enemySpawnMaterial;
    
    // 이벤트
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

### TurnManager (게임 흐름 상태 머신)
```csharp
public class TurnManager : MonoBehaviour
{
    [Header("턴 설정")]
    [SerializeField] private float betweenPhaseDelay = 1f; // 중간 페이즈 지연
    [SerializeField] private float actionDelay = 0.5f;     // 행동 지연
    
    // 현재 상태
    [SerializeField] private GamePhase currentPhase = GamePhase.PlayerTurn;
    [SerializeField] private int turnNumber = 1;
    
    // 이벤트
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
        
        // 적 턴으로 시작
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
        
        // 카드 뽑기, 마나 추가 등
        cardSystem?.StartPlayerTurn();
    }
    
    private void PrepareEnemyTurn()
    {
        var enemyEntities = gridManager.GetAllEntitiesOfTeam(Team.Enemy);
        EnableActionsForEntities(enemyEntities);
        
        // AI 카드 플레이 로직
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
        // 행동할 수 있는 모든 유닛의 행동 실행
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
        
        // 중간 페이즈 종료
        EndCurrentPhase();
    }
    
    private void AdvanceToNextTurn()
    {
        // 플레이어와 적 턴 번갈아 진행
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
    PlayerTurn,   // 플레이어 턴
    EnemyTurn,    // 적 턴
    BetweenPhase, // 중간 페이즈
    GameEnded     // 게임 종료
}
```

### CombatSystem (이벤트 기반 전투 해결)
```csharp
public class CombatSystem : MonoBehaviour
{
    [Header("전투 설정")]
    [SerializeField] private float combatAnimationDuration = 1f; // 전투 애니메이션 지속시간
    [SerializeField] private LayerMask combatLayers = -1;        // 전투 레이어
    
    // 이벤트
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
        // 기존의 모든 공격 컴포넌트를 찾아 구독
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
        // 피해 계산
        int damage = CalculateDamage(attacker, target);
        
        // 전투 애니메이션 재생
        yield return StartCoroutine(PlayCombatAnimation(attacker.gameObject, target));
        
        // 피해 적용
        var targetHealth = target.GetComponent<HealthComponent>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, attacker.gameObject);
            
            // 이벤트 발생
            attacker.OnDamageDealt?.Invoke(target, damage);
            OnCombatResolved?.Invoke(attacker.gameObject, target, damage);
            
            // 타겟이 파괴되었는지 확인
            if (targetHealth.IsDead)
            {
                OnUnitDestroyed?.Invoke(target);
            }
        }
    }
    
    private int CalculateDamage(AttackComponent attacker, GameObject target)
    {
        int baseDamage = attacker.AttackPower;
        
        // 수정자, 방어력 등 적용
        // 여기에 복잡한 전투 공식을 추가할 수 있습니다
        
        return baseDamage;
    }
    
    private IEnumerator PlayCombatAnimation(GameObject attacker, GameObject target)
    {
        // 간단한 애니메이션 - 공격자가 타겟 쪽으로 이동 후 돌아옴
        Vector3 attackerStart = attacker.transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 attackPos = Vector3.Lerp(attackerStart, targetPos, 0.7f);
        
        // 타겟으로 이동
        float elapsed = 0f;
        while (elapsed < combatAnimationDuration * 0.5f)
        {
            attacker.transform.position = Vector3.Lerp(attackerStart, attackPos, elapsed / (combatAnimationDuration * 0.5f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 돌아가기
        elapsed = 0f;
        while (elapsed < combatAnimationDuration * 0.5f)
        {
            attacker.transform.position = Vector3.Lerp(attackPos, attackerStart, elapsed / (combatAnimationDuration * 0.5f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        attacker.transform.position = attackerStart;
    }
    
    // 새 엔티티가 생성될 때 호출
    public void RegisterAttackComponent(AttackComponent attackComponent)
    {
        attackComponent.OnAttackInitiated.AddListener(HandleAttackInitiated);
    }
    
    // 엔티티가 파괴될 때 호출
    public void UnregisterAttackComponent(AttackComponent attackComponent)
    {
        attackComponent.OnAttackInitiated.RemoveListener(HandleAttackInitiated);
    }
}
```

### CardSystem (카드 플레이 처리)
```csharp
public class CardSystem : MonoBehaviour
{
    [Header("카드 설정")]
    [SerializeField] private int startingHandSize = 5; // 시작 손패 크기
    [SerializeField] private int maxHandSize = 7;      // 최대 손패 크기
    [SerializeField] private int cardsPerTurn = 1;     // 턴당 카드 뽑기 수
    
    [Header("리소스")]
    [SerializeField] private int startingMana = 3;     // 시작 마나
    [SerializeField] private int maxMana = 10;         // 최대 마나
    [SerializeField] private int manaPerTurn = 1;      // 턴당 마나
    
    // 게임 상태
    [SerializeField] private int currentMana;
    [SerializeField] private List<CardData> playerHand = new List<CardData>();
    [SerializeField] private List<CardData> playerDeck = new List<CardData>();
    [SerializeField] private List<CardData> enemyHand = new List<CardData>();
    [SerializeField] private List<CardData> enemyDeck = new List<CardData>();
    
    // 이벤트
    public UnityEvent<CardData> OnCardPlayed;
    public UnityEvent<CardData> OnCardDrawn;
    public UnityEvent<int> OnManaChanged;
    
    // 상태
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
        // 그리드 이벤트 구독
        gridManager.OnTileClicked.AddListener(HandleTileClicked);
        
        // 시작 손패 뽑기
        DrawCards(playerHand, playerDeck, startingHandSize);
        DrawCards(enemyHand, enemyDeck, startingHandSize);
    }
    
    public void StartPlayerTurn()
    {
        // 마나 추가
        currentMana = Mathf.Min(currentMana + manaPerTurn, maxMana);
        OnManaChanged?.Invoke(currentMana);
        
        // 카드 뽑기
        if (playerHand.Count < maxHandSize)
        {
            DrawCards(playerHand, playerDeck, cardsPerTurn);
        }
    }
    
    public void StartEnemyTurn()
    {
        // AI가 자동으로 카드 플레이
        StartCoroutine(ExecuteEnemyTurn());
    }
    
    private IEnumerator ExecuteEnemyTurn()
    {
        // 간단한 AI: 사용 가능한 모든 카드 플레이
        var playableCards = enemyHand.Where(card => card.cost <= currentMana).ToList();
        
        foreach (var card in playableCards)
        {
            if (TryPlayCard(card, Team.Enemy))
            {
                enemyHand.Remove(card);
                yield return new WaitForSeconds(1f);
            }
        }
        
        // 적 턴 종료
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
            // 스펠 즉시 플레이
            PlaySpell(card);
            playerHand.Remove(card);
            currentMana -= card.cost;
            OnManaChanged?.Invoke(currentMana);
            OnCardPlayed?.Invoke(card);
        }
        else if (card.cardType == CardType.Pawn)
        {
            // 타일 선택 대기
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
            // AI가 유효한 소환 위치를 랜덤으로 선택
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
        
        // 유닛 인스턴스화
        GameObject unitObj = Instantiate(card.unitToSummon.unitPrefab);
        Entity entity = unitObj.GetComponent<Entity>();
        entity.SetUnitData(card.unitToSummon);
        
        // 팀 설정
        var teamComponent = entity.GetComponent<TeamComponent>();
        if (teamComponent != null)
        {
            teamComponent.SetTeam(team);
        }
        
        // 그리드에 배치
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
                    // 이 팀이 소환 가능한 타일인지 확인
                    // GridManager에서 구현이 필요합니다
                    validTiles.Add(pos);
                }
            }
        }
        
        return validTiles;
    }
    
    private void HighlightValidSpawnTiles(Team team)
    {
        var validTiles = GetValidSpawnTiles(team);
        // UI에서 타일 하이라이트
        // 타일 시각적 피드백으로 구현될 예정
    }
    
    private void ClearTileSelection()
    {
        waitingForTileSelection = false;
        pendingCard = null;
        // 타일 하이라이트 지우기
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

## 🔧 확장성 & 미래 확장

### 컴포넌트 기반 확장성
컴포넌트 아키텍처는 쉬운 기능 확장을 가능하게 합니다:

```csharp
// 예시: 실드 메커니즘 추가
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

// 실드를 확인하도록 HealthComponent 수정
public class HealthComponent : GameComponent
{
    private ShieldComponent shieldComponent;
    
    protected override void OnEntityInitialized()
    {
        shieldComponent = registry.GetComponent<ShieldComponent>();
    }
    
    public void TakeDamage(int damage, GameObject source = null)
    {
        // 실드 흡수 적용
        if (shieldComponent != null)
        {
            shieldComponent.AbsorbDamage(ref damage);
        }
        
        // 나머지 피해 로직...
    }
}
```

### 이벤트 기반 아키텍처의 장점
1. **느슨한 결합**: 컴포넌트가 직접 참조를 필요로 하지 않음
2. **쉬운 테스팅**: 단위 테스트를 위한 모의 이벤트
3. **런타임 유연성**: 동적으로 이벤트 리스너 추가/제거
4. **UI 통합**: UI가 게임 로직과 같은 이벤트를 수신할 수 있음

### AI 전략 확장성
```csharp
public abstract class AIStrategy : ScriptableObject
{
    public abstract void ExecuteTurn(Entity entity, GridManager grid);
}

[CreateAssetMenu(menuName = "AI/공격적 전략")]
public class AggressiveAI : AIStrategy
{
    public override void ExecuteTurn(Entity entity, GridManager grid)
    {
        // 가장 가까운 적 공격 구현
    }
}

// ActionComponent가 다른 전략들을 사용할 수 있음
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

### 스펠 효과 시스템
```csharp
public abstract class SpellEffect : ScriptableObject
{
    public abstract void Execute();
}

[CreateAssetMenu(menuName = "스펠/피해 스펠")]
public class DamageSpell : SpellEffect
{
    [SerializeField] private int damage = 10;
    [SerializeField] private int range = 2;
    
    public override void Execute()
    {
        // 타겟 선택 및 피해 적용
    }
}

[CreateAssetMenu(menuName = "스펠/치유 스펠")]
public class HealSpell : SpellEffect
{
    [SerializeField] private int healAmount = 15;
    
    public override void Execute()
    {
        // 치유 로직
    }
}
```

---

## 🎯 개선된 구현 가이드라인

### 🚨 Critical Priority - 즉시 수정 (1-2일)
**목표**: 데이터 무결성 및 캡슐화 확보

1. **데이터 캡슐화 완성**
   ```csharp
   // Before (문제 있는 코드)
   [System.Serializable]
   public class UnitData : ScriptableObject
   {
       public string unitName;  // ❌ 외부에서 직접 수정 가능
       public UnitStats baseStats;  // ❌ 원본 데이터 손상 위험
   }
   
   // After (개선된 코드)
   [CreateAssetMenu(fileName = "New Unit", menuName = "Game/Unit Data")]
   public class UnitData : ScriptableObject
   {
       [SerializeField] private string unitName;  // ✅ Unity Inspector 지원
       [SerializeField] private UnitStats baseStats;  // ✅ private 접근
       
       public string UnitName => unitName;  // ✅ 읽기 전용 접근
       public UnitStats GetBaseStats() => new UnitStats(baseStats);  // ✅ 복사본 반환
   }
   ```

2. **StatModifier 캡슐화**
   ```csharp
   [System.Serializable]
   public class StatModifier
   {
       [SerializeField] private ModifierType type;  // private 변경
       [SerializeField] private float value;        // private 변경
       [SerializeField] private float duration;     // private 변경
       
       public ModifierType Type => type;
       public float Value => value;
       public float Duration => duration;
       
       // 안전한 수정 메서드
       public void UpdateDuration(float deltaTime)
       {
           duration = Mathf.Max(0f, duration - deltaTime);
       }
   }
   ```

### ⚡ High Priority - 단기 개선 (3-5일)
**목표**: 시스템 간 결합도 감소 및 SOLID 원칙 적용

3. **의존성 주입 패턴 도입**
   ```csharp
   // 인터페이스 정의
   public interface IGridManager
   {
       bool IsValidPosition(Vector2Int position);
       void MoveUnit(Entity unit, Vector2Int newPosition);
       Vector2Int GetUnitPosition(Entity unit);
   }
   
   // 의존성 주입 적용
   public class AttackComponent : GameComponent
   {
       private IGridManager gridManager;  // 인터페이스 의존
       
       public void Initialize(UnitStats stats, IGridManager gridManager)
       {
           this.gridManager = gridManager;  // 주입받기
           attackPower = stats.AttackPower;
       }
   }
   ```

4. **단일 책임 원칙 적용**
   ```csharp
   // ActionComponent 분리
   public class ActionHandler : MonoBehaviour  // 행동 실행 담당
   public class ActionValidator : MonoBehaviour  // 행동 검증 담당
   
   // GridManager 분리
   public class GridState : MonoBehaviour      // 그리드 상태 관리
   public class GridOperations : MonoBehaviour // 그리드 연산 담당
   ```

### 📋 Medium Priority - 중기 개선 (1-2주)
**목표**: 확장성 및 유지보수성 향상

5. **이벤트 시스템 강화**
   ```csharp
   // 타입 안전 이벤트 시스템
   public class TypedEventSystem
   {
       private static Dictionary<Type, List<Delegate>> eventListeners = new Dictionary<Type, List<Delegate>>();
       
       public static void Subscribe<T>(Action<T> listener)
       public static void Unsubscribe<T>(Action<T> listener)
       public static void Publish<T>(T eventData)
   }
   ```

6. **컴포넌트 풀링 시스템**
   ```csharp
   public class ComponentPool<T> where T : Component
   {
       private Queue<T> pool = new Queue<T>();
       private GameObject prefab;
       
       public T Get() { /* 풀에서 가져오기 */ }
       public void Return(T component) { /* 풀에 반환 */ }
   }
   ```

### 🔄 개선된 개발 순서
1. **Critical 수정**: 데이터 캡슐화 → StatModifier 개선
2. **High 수정**: 의존성 주입 → SOLID 원칙 적용
3. **핵심 컴포넌트**: 개선된 HealthComponent, TeamComponent
4. **그리드 시스템**: 분리된 GridState + GridOperations
5. **기본 엔티티**: 의존성 주입 지원하는 Entity 클래스
6. **이동 & 전투**: 인터페이스 기반 MovementComponent, AttackComponent
7. **턴 관리**: 상태 패턴 적용한 TurnManager
8. **카드 시스템**: 분리된 CardManager + CardRenderer
9. **AI & 완성도**: Strategy 패턴 기반 AI, 애니메이션, 효과

### 🧪 개선된 테스팅 전략

**1. 데이터 캡슐화 테스트**
```csharp
[Test]
public void UnitData_GetBaseStats_ReturnsNewInstance()
{
    // 준비
    var unitData = ScriptableObject.CreateInstance<UnitData>();
    
    // 실행
    var stats1 = unitData.GetBaseStats();
    var stats2 = unitData.GetBaseStats();
    
    // 검증 - 서로 다른 인스턴스여야 함
    Assert.AreNotSame(stats1, stats2);
}
```

**2. 의존성 주입 테스트**
```csharp
[Test]
public void AttackComponent_WithMockGridManager_WorksCorrectly()
{
    // 준비 - Mock 객체 사용
    var mockGridManager = new Mock<IGridManager>();
    mockGridManager.Setup(x => x.IsValidPosition(It.IsAny<Vector2Int>())).Returns(true);
    
    var attackComponent = gameObject.AddComponent<AttackComponent>();
    var stats = new UnitStats();
    
    // 실행
    attackComponent.Initialize(stats, mockGridManager.Object);
    
    // 검증
    Assert.IsTrue(attackComponent.CanAttackPosition(Vector2Int.zero));
}
```

**3. 단일 책임 원칙 테스트**
```csharp
[Test]
public void ActionValidator_OnlyValidatesActions_DoesNotExecute()
{
    // ActionValidator는 검증만 하고 실행하지 않음을 확인
    var validator = gameObject.AddComponent<ActionValidator>();
    var mockAction = new Mock<IGameAction>();
    
    var result = validator.ValidateAction(mockAction.Object);
    
    // 실행 메서드가 호출되지 않았는지 확인
    mockAction.Verify(x => x.Execute(), Times.Never);
}
```

### ⚡ 성능 최적화 전략

**1. 메모리 최적화**
```csharp
// 데이터 복사 최소화
public class EntityPool
{
    private static readonly Dictionary<Type, Queue<Component>> componentPools 
        = new Dictionary<Type, Queue<Component>>();
    
    public static T GetComponent<T>() where T : Component
    {
        if (!componentPools.ContainsKey(typeof(T)))
            componentPools[typeof(T)] = new Queue<Component>();
        
        var pool = componentPools[typeof(T)];
        return pool.Count > 0 ? (T)pool.Dequeue() : CreateNewComponent<T>();
    }
}
```

**2. 의존성 주입 성능 최적화**
```csharp
// 서비스 로케이터 패턴으로 빠른 의존성 해결
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
    
    public static void Register<T>(T service)
    {
        services[typeof(T)] = service;
    }
    
    public static T Get<T>()
    {
        return (T)services[typeof(T)];
    }
}
```

**3. 이벤트 시스템 최적화**
```csharp
// 메모리 누수 방지 자동 구독 해제
public class AutoUnsubscribeComponent : MonoBehaviour
{
    private List<Action> unsubscribeActions = new List<Action>();
    
    public void RegisterUnsubscribe(Action unsubscribeAction)
    {
        unsubscribeActions.Add(unsubscribeAction);
    }
    
    private void OnDestroy()
    {
        foreach (var action in unsubscribeActions)
            action?.Invoke();
    }
}
```

### 📊 성능 목표
- **메모리 사용량**: 20% 감소 (풀링 시스템)
- **프레임 드롭**: 90% 감소 (FindObjectOfType 제거)
- **로딩 시간**: 30% 단축 (의존성 주입 최적화)
- **개발 속도**: 40% 향상 (SOLID 원칙 적용)

### 🎯 아키텍처 품질 목표
- **캡슐화**: 100% (모든 데이터 필드 private)
- **결합도**: 70% 감소 (인터페이스 기반 설계)
- **테스트 커버리지**: 80% 이상
- **코드 복잡도**: 30% 감소 (단일 책임 원칙)

이 개선된 컴포넌트 기반 아키텍처는 **견고한 데이터 보호**, **낮은 시스템 결합도**, **높은 확장성**을 제공하여 장기적으로 안정적이고 유지보수 가능한 전술 카드 게임 기반을 구축합니다. SOLID 원칙과 디자인 패턴의 적용을 통해 **깨끗한 코드 구성**과 **쉬운 테스팅**을 보장합니다.