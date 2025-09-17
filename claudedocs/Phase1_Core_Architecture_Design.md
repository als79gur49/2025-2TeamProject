# Phase 1: Unity 카드 기반 전략 게임 코어 아키텍처 설계

## 아키텍처 개요

### 설계 원칙
- **데이터 기반 설계 (Data-Driven Design)**: ScriptableObject 기반의 콘텐츠 생산 시스템
- **느슨한 결합 (Loose Coupling)**: 이벤트 기반 시스템 간 통신
- **높은 응집도 (High Cohesion)**: 관련 기능의 논리적 그룹화
- **확장성 (Extensibility)**: 새로운 기능 추가 시 기존 코드 수정 최소화
- **캡슐화 (Encapsulation)**: 시스템 간 명확한 인터페이스 정의

### 핵심 디자인 패턴
1. **Entity-Component Pattern**: 게임 오브젝트의 모듈화된 구조
2. **State Machine Pattern**: 턴 기반 게임 흐름 제어
3. **Command Pattern**: 카드 액션의 실행 가능한 명령 구조
4. **Observer Pattern**: 이벤트 기반 시스템 간 통신
5. **Strategy Pattern**: 다양한 유닛 AI 동작 구현

---

## 1. 데이터 레이어 (Data Layer)

### 1.1 UnitStats 클래스
```csharp
[System.Serializable]
public class UnitStats
{
    [Header("Combat Stats")]
    public int maxHealth = 100;
    public int attackPower = 10;
    
    [Header("Movement")]
    public int movementRange = 1;
    
    // 복사 생성자 - 원본 데이터 보호
    public UnitStats(UnitStats original)
    {
        maxHealth = original.maxHealth;
        attackPower = original.attackPower;
        movementRange = original.movementRange;
    }
}
```

**설계 특징:**
- MonoBehaviour 비상속으로 순수 데이터 클래스 유지
- [System.Serializable]로 Unity Inspector 편집 가능
- 복사 생성자 제공으로 ScriptableObject 데이터 오염 방지

### 1.2 IDamageable 인터페이스
```csharp
public interface IDamageable
{
    void TakeDamage(int damage);
    int CurrentHealth { get; }
    bool IsAlive { get; }
}
```

**설계 특징:**
- 모든 피해 가능 오브젝트의 공통 계약 정의
- 다형성 지원으로 시스템 간 결합도 감소
- 향후 파괴 가능 장애물, 구조물 등 확장 가능

---

## 2. 데이터 구성 레이어 (Data Configuration Layer)

### 2.1 UnitData ScriptableObject
```csharp
[CreateAssetMenu(fileName = "New Unit", menuName = "Game Data/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Unit Identity")]
    public string unitName;
    public Sprite unitIcon;
    
    [Header("Game Stats")]
    public UnitStats baseStats;
    
    [Header("Prefab Reference")]
    public GameObject unitPrefab;
    
    [Header("AI Behavior")]
    public UnitAIType aiType = UnitAIType.Aggressive;
}

public enum UnitAIType
{
    Aggressive,     // 공격 우선
    Defensive,      // 방어 우선
    Support,        // 아군 지원 우선
    Balanced        // 균형잡힌 행동
}
```

### 2.2 CardData ScriptableObject
```csharp
[CreateAssetMenu(fileName = "New Card", menuName = "Game Data/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Card Identity")]
    public string cardName;
    public Sprite cardIllustration;
    public string description;
    
    [Header("Card Mechanics")]
    public CardType cardType;
    public int cost;
    
    [Header("Card Effects")]
    public UnitData unitToSummon; // Pawn 타입용
    public SpellEffectData spellEffect; // Spell 타입용
    
    // 카드 사용 가능 여부 검증
    public bool CanPlay(GameState gameState)
    {
        return gameState.CurrentMana >= cost;
    }
}

public enum CardType
{
    Pawn,
    Spell,
    Equipment,  // 향후 확장
    Enchantment // 향후 확장
}
```

### 2.3 SpellEffect 시스템
```csharp
public abstract class SpellEffectData : ScriptableObject
{
    public abstract void Execute(GameContext context);
    public abstract bool CanExecute(GameContext context);
}

[CreateAssetMenu(menuName = "Game Data/Spell Effects/Damage Spell")]
public class DamageSpellData : SpellEffectData
{
    public int damage;
    public TargetType targetType;
    
    public override void Execute(GameContext context)
    {
        // 피해 로직 구현
    }
}
```

---

## 3. 로직 레이어 (Logic Layer)

### 3.1 Entity 추상 클래스
```csharp
public abstract class Entity : MonoBehaviour, IDamageable
{
    [Header("Unit Data")]
    [SerializeField] private UnitData unitData;
    
    [Header("Runtime Stats")]
    protected UnitStats currentStats;
    protected int currentHealth;
    
    [Header("Grid Position")]
    public Vector2Int gridPosition;
    public Player owner;
    
    // Events
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnDeath;
    
    protected virtual void Awake()
    {
        // 원본 데이터 복사로 오염 방지
        if (unitData != null)
        {
            currentStats = new UnitStats(unitData.baseStats);
            currentHealth = currentStats.maxHealth;
        }
    }
    
    public virtual void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    protected virtual void Die()
    {
        OnDeath?.Invoke();
        // 그리드에서 제거, 이펙트 재생 등
        GridManager.Instance.RemoveUnit(this);
        Destroy(gameObject);
    }
    
    // Properties
    public int CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;
    public UnitData UnitData => unitData;
    public UnitStats Stats => currentStats;
}
```

### 3.2 Pawn 클래스
```csharp
public class Pawn : Entity
{
    [Header("Turn System")]
    [SerializeField] private bool canAct = false;
    
    private UnitAI unitAI;
    
    protected override void Awake()
    {
        base.Awake();
        
        // AI 시스템 초기화
        unitAI = GetComponent<UnitAI>();
        if (unitAI == null)
        {
            unitAI = gameObject.AddComponent<UnitAI>();
        }
        unitAI.Initialize(this);
    }
    
    public void EnableAction()
    {
        canAct = true;
        // 비주얼 피드백 (글로우 이펙트 등)
        ShowCanActVisual(true);
    }
    
    public void ExecuteAction()
    {
        if (!canAct) return;
        
        // AI에게 행동 위임
        unitAI.PerformAction();
        
        canAct = false;
        ShowCanActVisual(false);
    }
    
    private void ShowCanActVisual(bool show)
    {
        // 행동 가능 상태 시각적 표시
    }
    
    public bool CanAct => canAct;
}
```

### 3.3 Base 클래스
```csharp
public class Base : Entity
{
    [Header("Base Specific")]
    public int startingHealth = 500;
    
    protected override void Awake()
    {
        base.Awake();
        currentHealth = startingHealth;
    }
    
    protected override void Die()
    {
        // 게임 오버 이벤트 발생
        GameManager.Instance.TriggerGameOver(owner);
        base.Die();
    }
}
```

---

## 4. 핵심 시스템 레이어 (Core System Layer)

### 4.1 GridManager
```csharp
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    
    [Header("Grid Configuration")]
    [SerializeField] private Vector2Int gridSize = new Vector2Int(6, 6);
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private GameObject tilePrefab;
    
    private Tile[,] grid;
    private Dictionary<Vector2Int, Entity> unitPositions;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        InitializeGrid();
    }
    
    private void InitializeGrid()
    {
        grid = new Tile[gridSize.x, gridSize.y];
        unitPositions = new Dictionary<Vector2Int, Entity>();
        
        // 타일 생성 및 초기화
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                Vector3 worldPos = GridToWorldPosition(coord);
                
                GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                Tile tile = tileObj.GetComponent<Tile>();
                tile.Initialize(coord, GetTileType(coord));
                
                grid[x, y] = tile;
            }
        }
    }
    
    public bool TryPlaceUnit(Pawn pawn, Vector2Int targetCoord)
    {
        // 유효성 검사
        if (!IsValidCoordinate(targetCoord)) return false;
        if (IsOccupied(targetCoord)) return false;
        if (!IsValidSpawnZone(targetCoord, pawn.owner)) return false;
        
        // 유닛 배치
        unitPositions[targetCoord] = pawn;
        pawn.gridPosition = targetCoord;
        pawn.transform.position = GridToWorldPosition(targetCoord);
        
        // 이벤트 발생
        EventManager.Instance.TriggerEvent("UnitPlaced", pawn);
        
        return true;
    }
    
    public bool TryMoveUnit(Entity unit, Vector2Int targetCoord)
    {
        if (!IsValidMove(unit, targetCoord)) return false;
        
        // 이전 위치에서 제거
        unitPositions.Remove(unit.gridPosition);
        
        // 새 위치에 배치
        unitPositions[targetCoord] = unit;
        unit.gridPosition = targetCoord;
        unit.transform.position = GridToWorldPosition(targetCoord);
        
        return true;
    }
    
    private TileType GetTileType(Vector2Int coord)
    {
        // 플레이어 소환 지역 (하단 2줄)
        if (coord.y <= 1) return TileType.PlayerSpawn;
        
        // 적군 소환 지역 (상단 2줄)  
        if (coord.y >= gridSize.y - 2) return TileType.EnemySpawn;
        
        // 중립 지역
        return TileType.Neutral;
    }
    
    // 유틸리티 메서드들
    public Vector3 GridToWorldPosition(Vector2Int gridPos) => new Vector3(gridPos.x * tileSize, 0, gridPos.y * tileSize);
    public bool IsValidCoordinate(Vector2Int coord) => coord.x >= 0 && coord.x < gridSize.x && coord.y >= 0 && coord.y < gridSize.y;
    public bool IsOccupied(Vector2Int coord) => unitPositions.ContainsKey(coord);
    public Entity GetUnitAt(Vector2Int coord) => unitPositions.GetValueOrDefault(coord);
}
```

### 4.2 TurnManager
```csharp
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    
    [Header("Turn Configuration")]
    public float betweenPhaseDelay = 1.0f;
    
    public TurnPhase currentPhase { get; private set; }
    public Player currentPlayer { get; private set; }
    
    // Events
    public UnityEvent<TurnPhase> OnPhaseChanged;
    public UnityEvent<Player> OnPlayerTurnStart;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        StartGame();
    }
    
    public void StartGame()
    {
        currentPhase = TurnPhase.EnemyTurn;
        currentPlayer = GameManager.Instance.EnemyPlayer;
        StartTurn();
    }
    
    private void StartTurn()
    {
        OnPhaseChanged?.Invoke(currentPhase);
        OnPlayerTurnStart?.Invoke(currentPlayer);
        
        // 현재 플레이어의 모든 유닛 활성화
        EnableAllUnitsForPlayer(currentPlayer);
    }
    
    public void EndTurn()
    {
        TransitionToNextPhase();
    }
    
    private void TransitionToNextPhase()
    {
        switch (currentPhase)
        {
            case TurnPhase.EnemyTurn:
                currentPhase = TurnPhase.PlayerTurn;
                currentPlayer = GameManager.Instance.HumanPlayer;
                StartTurn();
                break;
                
            case TurnPhase.PlayerTurn:
                currentPhase = TurnPhase.BetweenPhase;
                StartCoroutine(ExecuteBetweenPhase());
                break;
                
            case TurnPhase.BetweenPhase:
                currentPhase = TurnPhase.EnemyTurn;
                currentPlayer = GameManager.Instance.EnemyPlayer;
                StartTurn();
                break;
        }
    }
    
    private IEnumerator ExecuteBetweenPhase()
    {
        OnPhaseChanged?.Invoke(currentPhase);
        
        // 1. 적군 유닛 행동 실행
        yield return StartCoroutine(ExecuteUnitsActions(GameManager.Instance.EnemyPlayer));
        
        yield return new WaitForSeconds(betweenPhaseDelay);
        
        // 2. 아군 유닛 행동 실행
        yield return StartCoroutine(ExecuteUnitsActions(GameManager.Instance.HumanPlayer));
        
        // 3. 다음 턴으로 전환
        TransitionToNextPhase();
    }
    
    private IEnumerator ExecuteUnitsActions(Player player)
    {
        List<Pawn> activeUnits = GetActiveUnitsForPlayer(player);
        
        foreach (Pawn unit in activeUnits)
        {
            if (unit.CanAct)
            {
                unit.ExecuteAction();
                yield return new WaitForSeconds(0.5f); // 액션 간 딜레이
            }
        }
    }
    
    private void EnableAllUnitsForPlayer(Player player)
    {
        List<Pawn> units = GetAllUnitsForPlayer(player);
        foreach (Pawn unit in units)
        {
            unit.EnableAction();
        }
    }
}

public enum TurnPhase
{
    EnemyTurn,
    PlayerTurn,
    BetweenPhase
}
```

---

## 5. 플레이어 상호작용 레이어 (Player Interaction Layer)

### 5.1 CardManager
```csharp
public class CardManager : MonoBehaviour
{
    [Header("Hand Management")]
    public int maxHandSize = 7;
    public List<CardData> playerHand = new List<CardData>();
    
    [Header("Card Selection")]
    private CardData selectedCard;
    private bool waitingForTileSelection = false;
    
    public void PlayCard(CardData card)
    {
        if (!CanPlayCard(card)) return;
        
        selectedCard = card;
        
        switch (card.cardType)
        {
            case CardType.Pawn:
                waitingForTileSelection = true;
                HighlightValidSpawnTiles();
                break;
                
            case CardType.Spell:
                ExecuteSpellCard(card);
                break;
        }
    }
    
    public void OnTileClicked(Vector2Int tileCoord)
    {
        if (!waitingForTileSelection || selectedCard == null) return;
        
        if (selectedCard.cardType == CardType.Pawn)
        {
            TrySpawnUnit(selectedCard, tileCoord);
        }
        
        ClearSelection();
    }
    
    private void TrySpawnUnit(CardData card, Vector2Int coord)
    {
        // 마나 소모
        if (!GameManager.Instance.TrySpendMana(card.cost)) return;
        
        // 유닛 생성
        GameObject unitObj = Instantiate(card.unitToSummon.unitPrefab);
        Pawn pawn = unitObj.GetComponent<Pawn>();
        pawn.owner = GameManager.Instance.HumanPlayer;
        
        // 배치 시도
        if (GridManager.Instance.TryPlaceUnit(pawn, coord))
        {
            RemoveCardFromHand(card);
            EventManager.Instance.TriggerEvent("UnitSummoned", pawn);
        }
        else
        {
            Destroy(unitObj);
            // 마나 환불
            GameManager.Instance.RefundMana(card.cost);
        }
    }
    
    private void ClearSelection()
    {
        selectedCard = null;
        waitingForTileSelection = false;
        ClearTileHighlights();
    }
}
```

### 5.2 EventManager (중앙 이벤트 시스템)
```csharp
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }
    
    private Dictionary<string, UnityEvent<object>> eventDictionary;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        eventDictionary = new Dictionary<string, UnityEvent<object>>();
    }
    
    public void Subscribe(string eventName, UnityAction<object> listener)
    {
        if (!eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] = new UnityEvent<object>();
        }
        eventDictionary[eventName].AddListener(listener);
    }
    
    public void Unsubscribe(string eventName, UnityAction<object> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName].RemoveListener(listener);
        }
    }
    
    public void TriggerEvent(string eventName, object data = null)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName]?.Invoke(data);
        }
    }
}
```

---

## 6. 확장성 및 통합 패턴

### 6.1 주요 확장 지점
1. **새로운 유닛 타입**: Entity 상속 + ExecuteAction() 오버라이드
2. **새로운 카드 타입**: CardType enum 확장 + CardManager 처리 로직 추가
3. **새로운 스펠 효과**: SpellEffectData 상속 클래스 생성
4. **새로운 게임 페이즈**: TurnPhase enum 확장 + TurnManager 상태 처리
5. **새로운 타일 타입**: TileType enum 확장 + GridManager 로직 추가

### 6.2 아키텍처 품질 검증 기준
- ✅ **단일 책임**: 각 클래스가 명확한 하나의 목적 수행
- ✅ **개방-폐쇄**: 확장에는 열려있고 수정에는 닫혀있음
- ✅ **의존성 역전**: 고수준 모듈이 저수준 모듈에 의존하지 않음
- ✅ **느슨한 결합**: 시스템 간 이벤트 기반 통신
- ✅ **높은 응집도**: 관련 기능의 논리적 그룹화

### 6.3 위험 요소 및 완화 전략
1. **ScriptableObject 참조 관리**: 에셋 할당 검증 로직 추가
2. **턴 상태 동기화**: 이벤트 시스템을 통한 중앙집중식 상태 관리
3. **그리드 좌표 시스템**: 경계 검사 강화 및 예외 처리
4. **이벤트 시스템 오버헤드**: 리스너 수 제한 및 정리 로직
5. **메모리 관리**: 이벤트 구독 해제 자동화

---

## 결론

본 아키텍처는 확장성, 유지보수성, 테스트 가능성을 중심으로 설계되었습니다. 각 레이어 간의 명확한 책임 분리와 이벤트 기반 통신을 통해 시스템 간 결합도를 최소화하였으며, 데이터 기반 설계를 통해 콘텐츠 생산성을 극대화하였습니다.

이 설계를 기반으로 Phase 1 구현을 진행하면, 향후 확장 요구사항에 유연하게 대응할 수 있는 견고한 기반을 구축할 수 있습니다.