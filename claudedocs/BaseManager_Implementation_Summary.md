# BaseManager Implementation Summary

## 📋 Overview

BaseManager 서비스가 성공적으로 구현되어 Base 객체의 라이프사이클 관리 책임을 중앙화했습니다.

**구현 날짜**: 2025-10-12
**주요 목표**: Base 객체 배치 및 파괴 모니터링을 통한 게임 승패 조건 구현

---

## 🏗️ Architecture Design

### Design Decision: BaseManager 생성 (선택됨)

**왜 BaseManager를 만들었는가?**

1. **아키텍처 일관성**
   - UnitService → Unit 관리
   - TurnService → Turn 관리
   - **BaseManager → Base 관리** (동일 패턴 적용)

2. **단일 책임 원칙 (SRP)**
   - GameService: 게임 lifecycle 조정
   - BaseManager: Base 객체 lifecycle 전담

3. **확장성**
   - 향후 Base 업그레이드, 방어 시설 추가 용이
   - Base 관련 로직이 한 곳에 집중

4. **기존 패턴 활용**
   - GridState.PlaceBase() 재사용
   - GameService 의존성 주입 패턴 재사용

**대안 (거부됨): GameService에 직접 추가**
- ❌ SRP 위반: GameService 책임 과다
- ❌ 확장성 부족
- ❌ 테스트 어려움

---

## 📂 Created Files

### 1. IBaseManager.cs
**경로**: `Assets/Script/Game/Services/Interfaces/IBaseManager.cs`

```csharp
public interface IBaseManager
{
    Base PlayerBase { get; }
    Base EnemyBase { get; }

    void InitializeBases();
    void CleanupBases();

    event Action OnPlayerBaseDestroyed;  // Game Loss
    event Action OnEnemyBaseDestroyed;   // Game Victory
}
```

**책임**:
- Base 인스턴스 접근
- Base 초기화 및 정리
- 승패 이벤트 발행

---

### 2. BaseManager.cs
**경로**: `Assets/Script/Game/Services/BaseManager.cs`

**주요 기능**:

#### 의존성
- `IGridManager`: Base 배치 및 그리드 위치 계산
- `ITeamConfigurationManager`: 팀별 Material 적용

#### 핵심 메서드

**InitializeBases()**
```csharp
1. 그리드 크기 조회
2. Player Base 생성 (좌측: x=0)
3. Enemy Base 생성 (우측: x=gridWidth-1)
4. GridState.PlaceBase() 호출
5. Base OnDeath 이벤트 구독
```

**CreateBase(TeamType, Vector2Int)**
```csharp
1. Base prefab 인스턴스화
2. Base.Initialize(gridPos, size, team)
3. GridState에 Base 배치
4. Team Material 적용
5. World position 설정
```

**Position Calculation**
- **Player Base**: `Vector2Int(0, centerY - baseSize.y/2)`
- **Enemy Base**: `Vector2Int(gridWidth-baseSize.x, centerY - baseSize.y/2)`

#### 이벤트 처리
```csharp
Base.OnDeath → BaseManager.HandleBaseDeath
                  → OnPlayerBaseDestroyed / OnEnemyBaseDestroyed
                       → GameServiceManager.HandleBaseDestroyed
                            → GameService.EndGame()
```

---

## 🔄 Modified Files

### 1. GameInitializer.cs

**변경 사항**:
```csharp
// 새 서비스 참조 추가
[SerializeField] private BaseManager baseManager;

// RegisterBaseServices() 메서드 추가
private void RegisterBaseServices()
{
    baseManager.InjectDependencies(gridManager, teamConfigurationManager);
    ServiceLocator.Register<IBaseManager>(baseManager);
}
```

**호출 순서**:
```
RegisterCoreServices()
  → RegisterGridServices()
  → RegisterGameServices()
  → RegisterTeamConfigurationServices()
  → RegisterBaseServices()  // ← 새로 추가
```

---

### 2. GameServiceManager.cs

**변경 사항**:

#### 1) 서비스 참조 추가
```csharp
[SerializeField] private BaseManager baseManager;
```

#### 2) 이벤트 추가
```csharp
public event Action OnPlayerBaseDestroyed;  // 게임 패배
public event Action OnEnemyBaseDestroyed;   // 게임 승리
```

#### 3) Start() 수정
```csharp
private void Start()
{
    if (baseManager != null)
    {
        baseManager.InitializeBases();  // ← 게임 시작 전 Base 생성
    }

    gameService.StartGame();
}
```

#### 4) 이벤트 연결
```csharp
// ConnectServiceEvents()
baseManager.OnPlayerBaseDestroyed += HandlePlayerBaseDestroyed;
baseManager.OnEnemyBaseDestroyed += HandleEnemyBaseDestroyed;

// DisconnectServiceEvents()
baseManager.OnPlayerBaseDestroyed -= HandlePlayerBaseDestroyed;
baseManager.OnEnemyBaseDestroyed -= HandleEnemyBaseDestroyed;
```

#### 5) 이벤트 핸들러
```csharp
private void HandlePlayerBaseDestroyed()
{
    LogEvent("💀 Player Base destroyed - Game Loss");
    OnPlayerBaseDestroyed?.Invoke();
    gameService.EndGame();
}

private void HandleEnemyBaseDestroyed()
{
    LogEvent("🎉 Enemy Base destroyed - Game Victory");
    OnEnemyBaseDestroyed?.Invoke();
    gameService.EndGame();
}
```

---

### 3. Base.cs

**변경 사항**:

#### 1) OnDeath 이벤트 추가
```csharp
/// <summary>Base 파괴 시 발생하는 이벤트 (BaseManager가 구독)</summary>
public event System.Action<GameObject> OnDeath;
```

#### 2) Initialize 오버로드 추가
```csharp
// BaseManager용 초기화 (크기 포함)
public void Initialize(Vector2Int startPos, Vector2Int size, TeamType team)
{
    startPosition = startPos;
    baseSize = size;  // ← 크기 설정 추가

    if (teamComponent != null)
    {
        teamComponent.Team = team;
    }
}
```

#### 3) OnBaseDestroyed() 수정
```csharp
private void OnBaseDestroyed()
{
    Debug.Log($"[Base] Team {Team} base destroyed at {startPosition}!");

    // BaseManager에 사망 알림 추가
    OnDeath?.Invoke(gameObject);  // ← 새로 추가

    // 타일 정리
    foreach (var tile in occupiedTiles)
    {
        if (tile != null)
        {
            tile.RemoveBase();
        }
    }
    occupiedTiles.Clear();

    // 오브젝트 파괴
    Destroy(gameObject, 1f);
}
```

---

## 🔗 Integration Flow

### Initialization Pipeline

```
1. GameInitializer.InitializeGame()
   ├─ RegisterCoreServices()
   │   ├─ RegisterGridServices()
   │   ├─ RegisterTeamConfigurationServices()
   │   └─ RegisterBaseServices()
   │       └─ BaseManager.InjectDependencies()
   └─ ValidateServices()

2. GameServiceManager.Start()
   ├─ BaseManager.InitializeBases()
   │   ├─ CreateBase(Player, leftPos)
   │   │   ├─ Instantiate(basePrefab)
   │   │   ├─ Base.Initialize(pos, size, team)
   │   │   ├─ GridState.PlaceBase()
   │   │   ├─ ApplyTeamVisuals()
   │   │   └─ Subscribe to Base.OnDeath
   │   └─ CreateBase(Enemy, rightPos)
   └─ GameService.StartGame()
```

### Destruction Event Flow

```
1. Base takes damage
   └─ HealthComponent.TakeDamage()

2. Health reaches 0
   └─ HealthComponent.OnDeath event fires

3. Base.OnBaseDestroyed() called
   ├─ Base.OnDeath?.Invoke(gameObject)
   └─ Cleanup tiles

4. BaseManager receives OnDeath
   └─ HandlePlayerBaseDeath() / HandleEnemyBaseDeath()

5. BaseManager fires team-specific event
   └─ OnPlayerBaseDestroyed / OnEnemyBaseDestroyed

6. GameServiceManager.HandleBaseDestroyed()
   ├─ Log game result
   ├─ Invoke public event
   └─ GameService.EndGame()

7. GameService.EndGame()
   ├─ IsGameActive = false
   └─ OnGameEnded?.Invoke()
```

---

## 🎯 Configuration Requirements

### Inspector Settings

**GameInitializer**:
- ✅ BaseManager 참조 할당 필요

**GameServiceManager**:
- ✅ BaseManager 참조 할당 필요

**BaseManager**:
- ✅ `basePrefab`: Base 프리팹 할당 필요
- ✅ `baseSize`: 기본값 (1, 3) 사용 가능
- ✅ `baseCenterYOffset`: 0 (그리드 중앙 사용)
- ✅ `enableLogging`: true (디버깅용)

---

## ✅ Testing Checklist

### Unit Tests
- [ ] BaseManager.InitializeBases() creates 2 bases
- [ ] Player Base positioned at (0, centerY)
- [ ] Enemy Base positioned at (gridWidth-1, centerY)
- [ ] Base.OnDeath event fires on destruction
- [ ] BaseManager routes events correctly

### Integration Tests
- [ ] GameInitializer registers BaseManager successfully
- [ ] BaseManager receives GridManager dependency
- [ ] BaseManager receives TeamConfigurationManager dependency
- [ ] Bases placed on GridState correctly
- [ ] Team materials applied correctly

### End-to-End Tests
- [ ] Player Base destruction → Game Loss
- [ ] Enemy Base destruction → Game Victory
- [ ] GameService.EndGame() called on Base destruction
- [ ] OnGameEnded event fires
- [ ] UI displays game result

---

## 🚀 Future Enhancements

### Phase 1: Basic Extensions
- [ ] Base health display (UI bar)
- [ ] Base destruction VFX
- [ ] Base destruction audio

### Phase 2: Gameplay Extensions
- [ ] Base regeneration over time
- [ ] Base upgrades (health, armor)
- [ ] Multiple base levels

### Phase 3: Strategic Extensions
- [ ] Defensive structures around base
- [ ] Base abilities (shields, repair)
- [ ] Base-specific cards

---

## 📊 Performance Considerations

**Memory**:
- 2 Base instances per game
- Minimal overhead from BaseManager service

**CPU**:
- Base initialization: O(baseSize.x * baseSize.y) for grid placement
- Event subscription: O(1)
- Base destruction: O(occupiedTiles) for cleanup

**Optimizations**:
- Bases are static (no Update() loops)
- Event-driven architecture (no polling)
- Efficient grid state management

---

## 🎓 Lessons Learned

### Architectural Insights

1. **Service Pattern Consistency**
   - 기존 패턴(UnitService, TurnService)을 따라 BaseManager 구현
   - 일관된 의존성 주입 패턴 사용
   - ServiceLocator 기반 등록

2. **Event-Driven Design**
   - Base → BaseManager → GameServiceManager → GameService
   - 계층 간 느슨한 결합 유지
   - 각 계층이 독립적으로 테스트 가능

3. **Separation of Concerns**
   - Base: 자신의 상태 관리 (Health, Team, Position)
   - BaseManager: Base lifecycle 관리
   - GameService: 게임 상태 전환
   - GameServiceManager: 이벤트 라우팅

### Best Practices Applied

✅ **SOLID Principles**
- Single Responsibility: BaseManager는 Base만 관리
- Open/Closed: 확장 가능하도록 인터페이스 정의
- Dependency Inversion: IBaseManager, IGridManager 사용

✅ **Clean Architecture**
- Data Layer: GridState
- Business Logic: BaseManager
- Presentation: GameServiceManager
- Unity Integration: MonoBehaviour components

✅ **Testing Readiness**
- Interface-based design (mocking 가능)
- Event-driven (테스트에서 구독 가능)
- Dependency injection (테스트 의존성 주입 가능)

---

## 📝 Notes

### Known Issues
- ⚠️ Base prefab must be manually assigned in Inspector
- ⚠️ GridState.PlaceBase() assumes grid is already initialized

### Dependencies
- ✅ GridManager must be initialized first
- ✅ TeamConfigurationManager must be registered
- ✅ ServiceLocator must be initialized

### Compatibility
- ✅ Unity 2021.3+
- ✅ Compatible with existing service architecture
- ✅ No breaking changes to existing code

---

## 🔍 Implementation Summary

**Total Files Created**: 2
- IBaseManager.cs
- BaseManager.cs

**Total Files Modified**: 4
- GameInitializer.cs
- GameServiceManager.cs
- GameService.cs (no changes needed - already compatible)
- Base.cs

**Lines of Code Added**: ~350 lines
**Service Integration**: Fully integrated with existing architecture
**Testing Status**: Ready for testing

---

**Implementation Status**: ✅ **COMPLETE**

BaseManager 시스템이 성공적으로 구현되었으며, 게임의 승패 조건을 Base 파괴를 통해 구현할 수 있는 기반이 마련되었습니다.
