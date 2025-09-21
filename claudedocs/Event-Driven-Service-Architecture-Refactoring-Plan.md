# Event-Driven Service Architecture 리팩토링 계획

## 📋 개요

현재 GameServiceManager, TurnService, UnitService, UIService, GameService를 **Event-driven + Service Manager** 패턴으로 리팩토링하여 확장성과 캡슐화를 개선하는 계획입니다.

---

## 🔍 현재 아키텍처 분석

### ❌ 현재 구조의 문제점

1. **강한 결합 (Tight Coupling)**
   ```csharp
   // 각 서비스가 ServiceLocator를 통해 직접 의존
   turnService = ServiceLocator.Get<ITurnService>();
   unitService = ServiceLocator.Get<IUnitService>();
   ```

2. **캡슐화 부족**
   - 외부에서 개별 서비스에 직접 접근 가능
   - GameServiceManager가 단순한 컴포넌트 생성만 담당

3. **확장성 제한**
   - 새 서비스 추가 시 기존 서비스들 모두 수정 필요
   - 서비스 간 통신 로직이 각 서비스에 분산

4. **테스트 어려움**
   - 서비스 간 의존성으로 인한 모킹 복잡성
   - 단위 테스트 시 전체 서비스 초기화 필요

### ✅ 현재 구조의 장점

1. **이벤트 기반 설계**
   - 각 서비스에 이미 이벤트 정의됨 (`OnTurnChanged`, `OnUnitRegistered` 등)

2. **인터페이스 분리**
   - `ITurnService`, `IUnitService` 등 잘 정의된 인터페이스

3. **ServiceLocator 패턴**
   - 의존성 주입 인프라 구축됨

---

## 🎯 리팩토링 목표

### 주요 목표
1. **Facade Pattern**: GameServiceManager가 모든 외부 접근점 역할
2. **Event-driven Communication**: 서비스 간 직접 호출 → 이벤트 통신
3. **캡슐화 강화**: 개별 서비스의 private 보호
4. **확장성 개선**: 새 서비스 추가 시 최소 수정
5. **테스트 용이성**: 각 서비스 독립 테스트 가능

---

## 📝 세부 리팩토링 계획

### 1️⃣ **Phase 1: Event System 설계**

#### 1.1 Central Event Hub 생성
```csharp
// 📂 Assets/Script/Game/Events/GameEvents.cs
public static class GameEvents
{
    // Turn Events
    public static UnityEvent<bool> OnTurnChangeRequested = new();
    public static UnityEvent<bool> OnTurnChanged = new();
    public static UnityEvent<int> OnTurnCountChanged = new();
    
    // Unit Events  
    public static UnityEvent<Unit> OnUnitRegistered = new();
    public static UnityEvent<Unit> OnUnitUnregistered = new();
    public static UnityEvent<Unit, Vector3> OnUnitMoveRequested = new();
    public static UnityEvent<Unit, Vector3> OnUnitMoved = new();
    
    // Game Events
    public static UnityEvent OnGameStartRequested = new();
    public static UnityEvent OnGameStarted = new();
    public static UnityEvent OnGameEndRequested = new();
    public static UnityEvent OnGameEnded = new();
    
    // UI Events
    public static UnityEvent<string> OnUIMessageRequested = new();
    public static UnityEvent OnUIUpdateRequested = new();
}
```

#### 1.2 Command Pattern 도입
```csharp
// 📂 Assets/Script/Game/Commands/ICommand.cs
public interface ICommand
{
    void Execute();
    void Undo(); // Optional for future
}

// 📂 Assets/Script/Game/Commands/TurnCommands.cs
public class EndTurnCommand : ICommand
{
    public void Execute()
    {
        GameEvents.OnTurnChangeRequested.Invoke(true);
    }
}

public class StartGameCommand : ICommand
{
    public void Execute()
    {
        GameEvents.OnGameStartRequested.Invoke();
    }
}
```

#### 1.3 Event Channel 시스템
```csharp
// 📂 Assets/Script/Game/Events/EventChannel.cs
[CreateAssetMenu(menuName = "Events/Event Channel")]
public class EventChannel : ScriptableObject
{
    private UnityEvent listeners = new UnityEvent();
    
    public void Raise()
    {
        listeners.Invoke();
    }
    
    public void Subscribe(UnityAction listener)
    {
        listeners.AddListener(listener);
    }
    
    public void Unsubscribe(UnityAction listener)
    {
        listeners.RemoveListener(listener);
    }
}
```

---

### 2️⃣ **Phase 2: GameServiceManager Facade 구현**

#### 2.1 Facade Interface 정의
```csharp
// 📂 Assets/Script/Game/Services/Interfaces/IGameServiceManager.cs
public interface IGameServiceManager
{
    // Game Control
    void StartGame();
    void RestartGame();
    void EndGame();
    
    // Turn Management
    void EndCurrentTurn();
    bool IsPlayerTurn { get; }
    int CurrentTurn { get; }
    
    // Unit Operations
    void RegisterUnit(Unit unit);
    void UnregisterUnit(Unit unit);
    void MoveUnit(Unit unit, Vector3 position);
    List<Unit> GetActiveUnits(bool? isPlayerUnit = null);
    
    // UI Operations
    void ShowMessage(string message);
    void UpdateUI();
    
    // Events
    event System.Action<bool> OnTurnChanged;
    event System.Action OnGameStateChanged;
}
```

#### 2.2 Facade Implementation
```csharp
// 📂 Assets/Script/Game/Services/GameServiceManager.cs (리팩토링)
public class GameServiceManager : MonoBehaviour, IGameServiceManager
{
    [Header("Service References - Private")]
    [SerializeField] private TurnService turnService;
    [SerializeField] private UnitService unitService;
    [SerializeField] private UIService uiService;
    [SerializeField] private GameService gameService;
    
    [Header("Configuration")]
    [SerializeField] private bool autoInitialize = true;
    
    private bool isInitialized = false;
    
    // Public Events - Facade의 통합 이벤트
    public event System.Action<bool> OnTurnChanged;
    public event System.Action OnGameStateChanged;
    
    #region Facade Public Interface
    
    public void StartGame()
    {
        if (!isInitialized) InitializeServices();
        GameEvents.OnGameStartRequested.Invoke();
    }
    
    public void EndCurrentTurn()
    {
        GameEvents.OnTurnChangeRequested.Invoke(true);
    }
    
    public bool IsPlayerTurn => turnService?.IsPlayerTurn ?? true;
    public int CurrentTurn => turnService?.TurnCount ?? 0;
    
    public void RegisterUnit(Unit unit)
    {
        GameEvents.OnUnitRegistered.Invoke(unit);
    }
    
    public void MoveUnit(Unit unit, Vector3 position)
    {
        GameEvents.OnUnitMoveRequested.Invoke(unit, position);
    }
    
    public List<Unit> GetActiveUnits(bool? isPlayerUnit = null)
    {
        return unitService?.GetActiveUnits(isPlayerUnit) ?? new List<Unit>();
    }
    
    public void ShowMessage(string message)
    {
        GameEvents.OnUIMessageRequested.Invoke(message);
    }
    
    public void UpdateUI()
    {
        GameEvents.OnUIUpdateRequested.Invoke();
    }
    
    #endregion
    
    #region Initialization & Event Binding
    
    private void Awake()
    {
        if (autoInitialize)
        {
            InitializeServices();
        }
    }
    
    private void InitializeServices()
    {
        EnsureServiceComponents();
        SetupEventSubscriptions();
        isInitialized = true;
    }
    
    private void EnsureServiceComponents()
    {
        // 기존 코드 + 개선된 초기화
    }
    
    private void SetupEventSubscriptions()
    {
        // 서비스들의 이벤트를 Facade 이벤트로 매핑
        GameEvents.OnTurnChanged.AddListener(HandleTurnChanged);
        GameEvents.OnGameStarted.AddListener(HandleGameStarted);
        GameEvents.OnGameEnded.AddListener(HandleGameEnded);
    }
    
    private void HandleTurnChanged(bool isPlayerTurn)
    {
        OnTurnChanged?.Invoke(isPlayerTurn);
    }
    
    private void HandleGameStarted()
    {
        OnGameStateChanged?.Invoke();
    }
    
    #endregion
}
```

---

### 3️⃣ **Phase 3: 각 Service의 Event-driven 전환**

#### 3.1 TurnService 리팩토링
```csharp
// 현재: 직접 호출 방식
// gameService.EndTurnRequested += HandleEndTurnRequest;

// 리팩토링: 이벤트 구독 방식
public class TurnService : MonoBehaviour, ITurnService
{
    private void Start()
    {
        // 이벤트 구독만 하고, 다른 서비스는 직접 참조하지 않음
        GameEvents.OnTurnChangeRequested.AddListener(HandleTurnChangeRequest);
        GameEvents.OnGameStartRequested.AddListener(HandleGameStartRequest);
    }
    
    private void HandleTurnChangeRequest(bool forcePlayerTurn)
    {
        EndTurn();
        // 변경 완료를 이벤트로 알림
        GameEvents.OnTurnChanged.Invoke(isPlayerTurn);
        GameEvents.OnTurnCountChanged.Invoke(turnCount);
    }
    
    private void HandleGameStartRequest()
    {
        StartGame();
        GameEvents.OnGameStarted.Invoke();
    }
    
    // 기존 이벤트들은 GameEvents로 리다이렉트
    private void NotifyTurnChanged()
    {
        OnTurnChanged?.Invoke(isPlayerTurn); // 기존 호환성
        GameEvents.OnTurnChanged.Invoke(isPlayerTurn); // 새 이벤트 시스템
    }
}
```

#### 3.2 UnitService 리팩토링
```csharp
public class UnitService : MonoBehaviour, IUnitService
{
    private void Start()
    {
        GameEvents.OnUnitRegistered.AddListener(HandleUnitRegistration);
        GameEvents.OnUnitMoveRequested.AddListener(HandleUnitMoveRequest);
        GameEvents.OnTurnChanged.AddListener(HandleTurnChanged);
    }
    
    private void HandleUnitRegistration(Unit unit)
    {
        RegisterUnit(unit);
    }
    
    private void HandleUnitMoveRequest(Unit unit, Vector3 position)
    {
        // 유닛 이동 로직 실행
        MoveUnit(unit, position);
        // 이동 완료 알림
        GameEvents.OnUnitMoved.Invoke(unit, position);
    }
    
    private void HandleTurnChanged(bool isPlayerTurn)
    {
        ProcessUnitsForCurrentPlayer(isPlayerTurn);
    }
}
```

#### 3.3 UIService 리팩토링
```csharp
public class UIService : MonoBehaviour, IUIService
{
    private void Start()
    {
        // ServiceLocator 의존성 제거
        GameEvents.OnTurnChanged.AddListener(HandleTurnChanged);
        GameEvents.OnUIMessageRequested.AddListener(ShowMessage);
        GameEvents.OnUIUpdateRequested.AddListener(UpdateDisplay);
    }
    
    private void HandleTurnChanged(bool isPlayerTurn)
    {
        UpdateDisplay();
    }
    
    private void OnEndTurnButtonClicked()
    {
        // 직접 서비스 호출 대신 이벤트 발생
        GameEvents.OnTurnChangeRequested.Invoke(true);
    }
}
```

#### 3.4 GameService 리팩토링
```csharp
public class GameService : MonoBehaviour, IGameService
{
    private void Start()
    {
        // ServiceLocator 의존성 제거
        GameEvents.OnGameStartRequested.AddListener(HandleGameStartRequest);
        GameEvents.OnTurnChangeRequested.AddListener(HandleTurnChangeRequest);
    }
    
    private void HandleGameStartRequest()
    {
        StartGame();
        GameEvents.OnGameStarted.Invoke();
    }
    
    private void HandleTurnChangeRequest(bool forcePlayerTurn)
    {
        // 게임 로직 검증 후 턴 변경 승인
        if (IsGameActive)
        {
            // 실제 턴 변경은 TurnService가 처리
            // GameService는 게임 상태 관리만
        }
    }
}
```

---

### 4️⃣ **Phase 4: 외부 접근점 통일**

#### 4.1 기존 외부 호출 식별
```csharp
// 현재 문제 상황들:
// 1. GridController에서 직접 서비스 접근
var unitService = ServiceLocator.Get<IUnitService>();

// 2. UI에서 직접 서비스 호출  
turnService.EndTurn();

// 3. GameInitializer에서 개별 서비스 초기화
ServiceLocator.Register<ITurnService>(turnService);
```

#### 4.2 통합 접근점으로 변경
```csharp
// 리팩토링 후:
// 1. GridController → GameServiceManager 접근
var gameManager = ServiceLocator.Get<IGameServiceManager>();
gameManager.MoveUnit(unit, position);

// 2. UI → GameServiceManager 접근
gameManager.EndCurrentTurn();

// 3. GameInitializer → GameServiceManager만 등록
ServiceLocator.Register<IGameServiceManager>(gameServiceManager);
```

#### 4.3 Legacy 호환성 유지
```csharp
// 점진적 마이그레이션을 위한 호환성 래퍼
[System.Obsolete("Use IGameServiceManager instead")]
public static class ServiceCompatibility
{
    public static ITurnService GetTurnService()
    {
        var manager = ServiceLocator.Get<IGameServiceManager>();
        return new TurnServiceWrapper(manager);
    }
}

// 래퍼 클래스로 기존 인터페이스 지원
public class TurnServiceWrapper : ITurnService
{
    private IGameServiceManager manager;
    
    public bool IsPlayerTurn => manager.IsPlayerTurn;
    
    public void EndTurn() => manager.EndCurrentTurn();
    
    public event System.Action<bool> OnTurnChanged
    {
        add => manager.OnTurnChanged += value;
        remove => manager.OnTurnChanged -= value;
    }
}
```

---

### 5️⃣ **Phase 5: 테스트 전략**

#### 5.1 Unit Test 구조
```csharp
// 📂 Assets/Script/Game/Tests/GameServiceManagerTests.cs
[TestFixture]
public class GameServiceManagerTests
{
    private GameServiceManager gameManager;
    private TestEventListener eventListener;
    
    [SetUp]
    public void Setup()
    {
        gameManager = new GameObject().AddComponent<GameServiceManager>();
        eventListener = new TestEventListener();
        
        // 이벤트 구독
        gameManager.OnTurnChanged += eventListener.OnTurnChanged;
    }
    
    [Test]
    public void StartGame_ShouldTriggerGameStartedEvent()
    {
        // Arrange
        bool gameStarted = false;
        gameManager.OnGameStateChanged += () => gameStarted = true;
        
        // Act
        gameManager.StartGame();
        
        // Assert
        Assert.IsTrue(gameStarted);
    }
    
    [Test]
    public void EndCurrentTurn_ShouldChangePlayerTurn()
    {
        // Arrange
        gameManager.StartGame();
        bool initialTurn = gameManager.IsPlayerTurn;
        
        // Act
        gameManager.EndCurrentTurn();
        
        // Assert
        Assert.AreNotEqual(initialTurn, gameManager.IsPlayerTurn);
    }
}
```

#### 5.2 Integration Test
```csharp
// 📂 Assets/Script/Game/Tests/ServiceIntegrationTests.cs
[TestFixture]
public class ServiceIntegrationTests
{
    [Test]
    public void FullGameFlow_ShouldWorkCorrectly()
    {
        // 전체 게임 플로우 테스트
        // StartGame → RegisterUnit → MoveUnit → EndTurn
    }
}
```

#### 5.3 Event System Test
```csharp
// 📂 Assets/Script/Game/Tests/EventSystemTests.cs
[TestFixture]
public class EventSystemTests
{
    [Test]
    public void GameEvents_ShouldPropagateCorrectly()
    {
        // 이벤트 전파 테스트
        int eventCount = 0;
        GameEvents.OnTurnChanged.AddListener(_ => eventCount++);
        
        GameEvents.OnTurnChanged.Invoke(true);
        
        Assert.AreEqual(1, eventCount);
    }
}
```

---

### 6️⃣ **Phase 6: 점진적 마이그레이션**

#### 6.1 Migration Steps
1. **Week 1**: Event System 구현 및 GameEvents 클래스 생성
2. **Week 2**: GameServiceManager Facade 패턴 적용
3. **Week 3**: 각 Service의 Event-driven 전환 (하나씩)
4. **Week 4**: 외부 접근점 통일 및 호환성 검증
5. **Week 5**: 테스트 작성 및 Legacy 코드 제거

#### 6.2 Risk Mitigation
- **Feature Branch**: 각 Phase별 별도 브랜치
- **Backward Compatibility**: 기존 인터페이스 유지
- **Gradual Rollout**: 서비스별 점진적 적용
- **Testing**: 각 Phase별 회귀 테스트

#### 6.3 Success Metrics
- **Coupling Reduction**: ServiceLocator.Get 호출 90% 감소
- **Test Coverage**: 새 구조 80% 이상 커버리지
- **Performance**: 이벤트 오버헤드 5% 이내
- **Code Quality**: 복잡도 지수 30% 개선

---

## 🚀 기대 효과

### 즉시 효과
1. **캡슐화 강화**: 외부에서 개별 서비스 직접 접근 불가
2. **결합도 감소**: 서비스 간 직접 의존성 제거
3. **확장성 개선**: 새 서비스 추가 시 기존 코드 수정 최소화

### 장기적 효과
1. **유지보수성**: 변경 파급효과 최소화
2. **테스트 용이성**: 각 서비스 독립 테스트 가능
3. **코드 품질**: 단일 책임 원칙 준수
4. **팀 협업**: 서비스별 병렬 개발 가능

---

## ⚠️ 주의사항

### Performance Considerations
- **Event Overhead**: 이벤트 기반 통신의 성능 오버헤드 모니터링 필요
- **Memory Leaks**: 이벤트 구독 해제 누락 방지
- **Event Flooding**: 과도한 이벤트 발생 제어

### Implementation Risks  
- **Breaking Changes**: 기존 코드와의 호환성 문제
- **Complexity**: 초기 이벤트 시스템 학습 곡선
- **Debugging**: 이벤트 기반 플로우 디버깅 어려움

### Mitigation Strategies
- **Documentation**: 이벤트 플로우 다이어그램 작성
- **Logging**: 이벤트 발생/처리 로그 추가
- **Validation**: 이벤트 구독/해제 검증 로직

---

## 📊 구현 우선순위

### High Priority
1. GameEvents 클래스 구현
2. GameServiceManager Facade 패턴 적용
3. TurnService Event-driven 전환

### Medium Priority  
1. UnitService, UIService Event-driven 전환
2. 외부 접근점 통일
3. 기본 테스트 작성

### Low Priority
1. GameService Event-driven 전환 (상대적으로 단순)
2. Legacy 호환성 래퍼
3. 고급 테스트 및 성능 최적화

---

이 계획을 통해 현재의 강한 결합 구조를 느슨한 결합의 이벤트 기반 아키텍처로 전환하여, 확장성과 유지보수성을 크게 개선할 수 있을 것입니다.