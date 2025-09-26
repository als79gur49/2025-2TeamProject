# CardServiceManager 시스템 종합 설명서

## 목차
1. [전체적인 역할과 구조](#1-전체적인-역할과-구조)
2. [하위 서비스들의 역할과 기능](#2-하위-서비스들의-역할과-기능)
3. [초기화 과정과 의존성 주입 패턴](#3-초기화-과정과-의존성-주입-패턴)
4. [턴 시스템과의 연동 방식](#4-턴-시스템과의-연동-방식)
5. [실제 사용 시나리오와 코드 실행 순서](#5-실제-사용-시나리오와-코드-실행-순서)
6. [서비스 간 상호작용과 데이터 흐름](#6-서비스-간-상호작용과-데이터-흐름)
7. [Unity 개발자를 위한 활용 가이드](#7-unity-개발자를-위한-활용-가이드)

---

## 1. 전체적인 역할과 구조

### 1.1 CardServiceManager의 핵심 역할

**CardServiceManager**는 Unity 게임에서 **카드 시스템의 중앙 허브**로 동작하는 핵심 매니저입니다:

```csharp
/// <summary>
/// 카드 시스템 총괄 매니저 - 모든 카드 관련 서비스를 관리하고 초기화
/// GameServiceManager와 동일한 레벨에서 동작하며, 카드 시스템의 중앙 허브 역할
/// </summary>
public class CardServiceManager : MonoBehaviour, ICardServiceManager
```

**주요 책임:**
- 🎯 **서비스 통합 관리**: 모든 카드 관련 서비스의 생명주기 관리
- 🔄 **턴 시스템 연동**: 게임 턴과 페이즈에 따른 카드 시스템 제어
- 💉 **의존성 주입**: 하위 서비스들에 필요한 의존성 주입
- 📋 **이벤트 중계**: 턴 시스템과 카드 서비스 간 이벤트 연결

### 1.2 시스템 아키텍처

```
GameInitializer
    ├── GridManager (그리드 시스템)
    ├── GameServiceManager (게임 서비스)
    ├── ResourceManager (자원 관리)
    └── CardServiceManager (카드 시스템) ← 여기!
            ├── CardHandManager (핸드 관리)
            ├── CardSpawnService (소환/주문)
            └── SpawnValidator (검증)
```

**설계 철학:**
- **단일 책임**: 각 서비스는 명확한 역할만 담당
- **의존성 주입**: ServiceLocator 패턴으로 느슨한 결합
- **이벤트 기반**: 서비스 간 직접 참조 대신 이벤트 통신

---

## 2. 하위 서비스들의 역할과 기능

### 2.1 CardHandManager: 핸드 카드 관리와 UI 처리

**역할:** 플레이어의 카드 핸드를 관리하고 UI 상호작용을 처리

```csharp
/// <summary>
/// 플레이어의 카드 핸드를 관리하는 서비스 (UI 포함)
/// 카드 드로우, 핸드 표시, 플레이어 상호작용을 담당
/// </summary>
public class CardHandManager : MonoBehaviour, ICardHandManager
```

**주요 기능:**

#### 📱 UI 관리
- **카드 레이아웃**: 호형 배열 또는 일직선 배열 지원
- **드래그앤드롭**: 소환 페이즈에서 카드 드래그 활성화
- **상호작용 제어**: 페이즈에 따른 카드 상호작용 on/off

```csharp
// 소환 모드 활성화 (AllySummon 페이즈에서 호출)
public void EnablePlayerSummonMode()
{
    isPlayerSummonMode = true;
    enablePlayerInteraction = true;

    foreach (var cardUI in cardUIComponents)
    {
        cardUI.SetDraggable(true); // 드래그 가능하게 설정
    }
}
```

#### 🃏 핸드 관리
- **카드 추가/제거**: 동적 핸드 관리
- **최대 핸드 크기**: 기본 7장 제한
- **자동 레이아웃**: 카드 추가/제거 시 자동 재배치

### 2.2 CardSpawnService: 유닛 소환과 주문 발동 처리

**역할:** 카드로부터 유닛 소환 및 주문 발동의 핵심 로직 처리

```csharp
/// <summary>
/// 카드의 유닛 소환 및 주문 발동을 처리하는 서비스
/// 소환 후 UnitService에 유닛을 등록하여 게임 월드에 편입시키는 핵심 역할
/// </summary>
public class CardSpawnService : MonoBehaviour, ICardSpawnService
```

**주요 기능:**

#### ⭐ 유닛 소환
```csharp
public bool TrySpawnUnitFromCard(CardData cardData, Vector2Int gridPosition, bool isPlayerUnit)
{
    // 1. 소환 검증 (SpawnValidator 사용)
    if (!spawnValidator.CanSpawnUnit(cardData, gridPosition, isPlayerUnit))
        return false;

    // 2. 자원 소모 (ResourceManager 사용)
    if (!resourceManager.SpendResources(isPlayerUnit, cardData.ManaCost, cardData.ActionCost))
        return false;

    // 3. 유닛 생성 및 배치
    GameObject spawnedUnit = Instantiate(unitData.Prefab, worldPosition, Quaternion.identity);

    // 4. UnitService에 등록 (핵심!)
    unitService.RegisterUnit(unitComponent);

    return true;
}
```

#### 🔮 주문 발동
```csharp
public bool TryActivateSpellFromCard(CardData cardData, Vector2Int targetPosition, bool isPlayerSpell)
{
    // 1. 주문 검증
    // 2. 자원 소모
    // 3. 주문 효과 실행
    ExecuteSpellEffect(cardData, targetPosition, isPlayerSpell);
}
```

**주문 타입별 처리:**
- `SpellType.Damage`: 데미지 적용
- `SpellType.Heal`: 체력 회복
- `SpellType.Buff/Debuff`: 버프/디버프 적용
- `SpellType.Shield`: 보호막 생성
- `SpellType.Teleport`: 위치 이동

### 2.3 SpawnValidator: 소환 유효성 검증

**역할:** 모든 소환/주문 사용의 유효성을 검증하는 게이트키퍼

```csharp
/// <summary>
/// 소환 및 주문 사용 위치의 유효성을 검증하는 서비스
/// 비용, 위치, 페이즈 등 모든 검증 규칙을 담당
/// </summary>
public class SpawnValidator : MonoBehaviour, ISpawnValidator
```

**검증 항목:**

#### 🕐 페이즈 검증
```csharp
private bool ValidatePhaseForSpawn(bool isPlayerUnit)
{
    var currentPhase = turnService.CurrentPhase;

    // 플레이어는 AllySummon 페이즈에서만
    if (isPlayerUnit && currentPhase != TurnPhase.AllySummon)
        return false;

    // 적군은 EnemySummon 페이즈에서만
    if (!isPlayerUnit && currentPhase != TurnPhase.EnemySummon)
        return false;

    return true;
}
```

#### 📍 위치 검증
```csharp
private bool ValidateSpawnPosition(Vector2Int gridPosition, bool isPlayerUnit)
{
    // 1. 그리드 범위 내 확인
    if (!gridController.IsValidPosition(gridPosition))
        return false;

    // 2. 빈 타일 확인
    if (gridController.IsPositionOccupied(gridPosition))
        return false;

    // 3. 소환 영역 검증
    if (isPlayerUnit && gridPosition.x != 0) // 플레이어는 좌측 첫 번째 열만
        return false;

    if (!isPlayerUnit && gridPosition.x != rightmostColumn) // 적군은 우측 마지막 열만
        return false;

    return true;
}
```

#### 💰 비용 검증
```csharp
private bool ValidateSpawnCost(CardData cardData, bool isPlayerUnit)
{
    return resourceManager.CanAfford(isPlayerUnit, cardData.ManaCost, cardData.ActionCost);
}
```

---

## 3. 초기화 과정과 의존성 주입 패턴

### 3.1 초기화 파이프라인

CardServiceManager는 GameInitializer에 의해 체계적으로 초기화됩니다:

```csharp
// GameInitializer.cs
private void RegisterCardServices()
{
    if (cardServiceManager != null)
    {
        cardServiceManager.InitializeAndRegisterServices(); // 핵심 초기화 호출
    }
}
```

**CardServiceManager 초기화 순서:**

```csharp
public void InitializeAndRegisterServices()
{
    // 1. 하위 서비스 컴포넌트 생성
    CreateCardServiceComponents();

    // 2. 의존성 주입
    InitializeCardServices();

    // 3. ServiceLocator 등록
    RegisterServicesWithLocator();

    // 4. 게임 서비스 이벤트 연결
    ConnectToGameServiceEvents();

    // 5. 서비스 상태 검증
    ValidateServiceHealth();

    isInitialized = true;
}
```

### 3.2 의존성 주입 패턴

**Unit의 Init() 패턴을 따른 외부 의존성 주입:**

```csharp
// CardServiceManager가 하위 서비스들을 초기화
private void InitializeCardServices()
{
    // ServiceLocator에서 필요한 서비스들 가져오기
    var gridManager = ServiceLocator.Get<IGridManager>();
    var gameServiceManager = ServiceLocator.Get<IGameServiceManager>();
    var resourceManager = ServiceLocator.Get<IResourceManager>();

    // 각 하위 서비스에 의존성 주입
    spawnValidator.Init(gridController, turnService, resourceManager);
    cardSpawnService.Init(unitService, gridController, gridState, spawnValidator, resourceManager);
    cardHandManager.Init(turnService);
}
```

**장점:**
- **명확한 의존성**: 필요한 의존성이 Init() 시그니처에 명시
- **테스트 용이성**: Mock 객체 주입 가능
- **초기화 순서 제어**: 의존성 순서에 따른 초기화 가능

### 3.3 ServiceLocator 등록 전략

```csharp
private void RegisterServicesWithLocator()
{
    // CardServiceManager 자신만 등록
    ServiceLocator.Register<ICardServiceManager>(this);

    // 하위 서비스들은 Get 메서드를 통해 접근
    // GridManager 패턴과 일치
}

// 외부 접근 방식
public ICardHandManager GetCardHandManager() => cardHandManager;
public ICardSpawnService GetCardSpawnService() => cardSpawnService;
public ISpawnValidator GetSpawnValidator() => spawnValidator;
```

---

## 4. 턴 시스템과의 연동 방식

### 4.1 TurnPhase 기반 제어

CardServiceManager는 게임의 6단계 턴 시스템과 완전히 연동됩니다:

```csharp
public enum TurnPhase
{
    TurnStart,    // 턴 시작
    EnemySummon,  // 적군 소환
    AllySummon,   // 아군 소환
    EnemyAction,  // 적군 행동
    AllyAction,   // 아군 행동
    TurnEnd       // 턴 종료
}
```

### 4.2 페이즈별 처리 로직

```csharp
private void HandlePhaseChanged(TurnPhase newPhase)
{
    // 소환 모드 제어
    if (newPhase == TurnPhase.AllySummon)
    {
        cardHandManager.EnablePlayerSummonMode();  // 플레이어 카드 드래그 가능
    }
    else
    {
        cardHandManager.DisablePlayerSummonMode(); // 카드 드래그 불가
    }

    // 페이즈별 세부 처리
    switch (newPhase)
    {
        case TurnPhase.TurnStart:
            HandleTurnStartPhase(); // 매 턴 마나 증가, 카드 드로우
            break;
        case TurnPhase.AllySummon:
            HandleAllySummonPhase(); // 플레이어 소환 준비
            break;
        // ... 기타 페이즈들
    }
}
```

### 4.3 자원 관리 연동

**턴마다 마나 증가 (Hearthstone 스타일):**
```csharp
private void HandleTurnStartPhase()
{
    // ResourceManager를 통한 마나 증가
    resourceManager.IncreaseTurnlyMana(); // 매 턴 최대 마나 +1

    // 카드 드로우
    cardHandManager.DrawRandomCard();
}
```

**ResourceManager의 턴별 마나 시스템:**
```csharp
public void IncreaseTurnlyMana()
{
    if (playerMaxMana < 10) // 최대 10마나까지
    {
        playerMaxMana++;
        playerMana = playerMaxMana; // 현재 마나를 최대치로 회복
    }

    // 적군도 동일하게 증가
    if (enemyMaxMana < 10)
    {
        enemyMaxMana++;
        enemyMana = enemyMaxMana;
    }
}
```

---

## 5. 실제 사용 시나리오와 코드 실행 순서

### 5.1 시나리오 1: 플레이어가 유닛 카드를 소환하는 경우

**상황:** 플레이어가 AllySummon 페이즈에서 "전사" 카드를 (0, 2) 위치에 드래그앤드롭

**실행 순서:**

1. **페이즈 확인** (TurnService → CardServiceManager)
   ```csharp
   TurnService.CurrentPhase == TurnPhase.AllySummon
   → CardHandManager.EnablePlayerSummonMode() 호출됨
   ```

2. **UI 상호작용** (CardHandManager)
   ```csharp
   // 카드 UI가 드래그 가능 상태로 설정됨
   cardUI.SetDraggable(true);
   ```

3. **드래그앤드롭 완료** → 소환 시도
   ```csharp
   // UI에서 CardSpawnService 호출
   cardSpawnService.TrySpawnUnitFromCard(warriorCard, new Vector2Int(0, 2), true);
   ```

4. **검증 단계** (SpawnValidator)
   ```csharp
   spawnValidator.CanSpawnUnit(warriorCard, (0, 2), true)
   │
   ├─ ValidatePhaseForSpawn(true) → AllySummon 페이즈 확인 ✅
   ├─ ValidateSpawnPosition((0, 2), true) → 좌측 첫 번째 열 확인 ✅
   └─ ValidateSpawnCost(warriorCard, true) → 마나/행동력 확인 ✅
   ```

5. **자원 소모** (ResourceManager)
   ```csharp
   resourceManager.SpendPlayerResources(2, 1) // 2마나, 1행동력 소모
   ```

6. **유닛 생성** (CardSpawnService)
   ```csharp
   GameObject warrior = Instantiate(warriorPrefab, worldPos, Quaternion.identity);
   Unit unitComponent = warrior.GetComponent<Unit>();
   ```

7. **게임 월드 편입** (UnitService 등록)
   ```csharp
   unitService.RegisterUnit(unitComponent); // 핵심! 유닛이 게임에 참여
   gridState.SetUnitPosition(warrior, (0, 2)); // 그리드에 위치 설정
   ```

**결과:** 전사 유닛이 (0, 2) 위치에 소환되고, 다음 페이즈에서 행동 가능한 상태가 됨

### 5.2 시나리오 2: 플레이어가 주문 카드를 사용하는 경우

**상황:** 플레이어가 "파이어볼" 주문을 적군 유닛에게 사용

**실행 순서:**

1. **주문 대상 선택** (UI 시스템)
   ```csharp
   Vector2Int targetPos = enemyUnit.GridPosition;
   ```

2. **주문 발동** (CardSpawnService)
   ```csharp
   cardSpawnService.TryActivateSpellFromCard(fireballCard, targetPos, true);
   ```

3. **검증 및 자원 소모**
   ```csharp
   spawnValidator.CanUseSpell(fireballCard, targetPos) // 검증
   resourceManager.SpendPlayerResources(3, 1) // 비용 지불
   ```

4. **주문 효과 실행**
   ```csharp
   ExecuteSpellEffect(fireballCard, targetPos, true)
   │
   ├─ GetUnitsInRange(worldPos, 5f) // 범위 내 유닛 검색
   ├─ ApplySpellEffectToUnit(enemyUnit, SpellType.Damage, 25, true)
   └─ ShowSpellVisualEffect(fireballCard, worldPos) // 시각적 효과
   ```

5. **데미지 적용**
   ```csharp
   enemyUnit.GetComponent<HealthComponent>().TakeDamage(25);
   ```

**결과:** 적군 유닛이 25 데미지를 받고, 체력이 0 이하면 파괴됨

### 5.3 시나리오 3: 새 턴 시작

**상황:** 턴이 종료되고 새로운 턴이 시작됨

**실행 순서:**

1. **페이즈 변경** (TurnService)
   ```csharp
   TurnService.ChangePhase(TurnPhase.TurnStart);
   ```

2. **이벤트 전파** (CardServiceManager)
   ```csharp
   HandlePhaseChanged(TurnPhase.TurnStart)
   → HandleTurnStartPhase() 호출
   ```

3. **자원 회복** (ResourceManager)
   ```csharp
   resourceManager.IncreaseTurnlyMana(); // 최대 마나 +1, 현재 마나 전체 회복
   ```

4. **카드 드로우** (CardHandManager)
   ```csharp
   cardHandManager.DrawRandomCard(); // 랜덤 카드 1장 드로우
   ```

**결과:**
- 플레이어와 적군 모두 최대 마나 +1 증가
- 현재 마나가 최대치로 회복
- 행동력이 최대치로 회복
- 플레이어가 새 카드 1장 획득

---

## 6. 서비스 간 상호작용과 데이터 흐름

### 6.1 서비스 의존성 다이어그램

```
CardServiceManager (중앙 허브)
    ├── depends on ──→ GameServiceManager (TurnService, UnitService)
    ├── depends on ──→ GridManager (GridController, GridState)
    ├── depends on ──→ ResourceManager (자원 관리)
    │
    ├── manages ──→ CardHandManager
    │               └── depends on → TurnService (페이즈 이벤트)
    │
    ├── manages ──→ CardSpawnService
    │               ├── depends on → UnitService (유닛 등록)
    │               ├── depends on → GridController, GridState
    │               ├── depends on → SpawnValidator (검증)
    │               └── depends on → ResourceManager (비용 처리)
    │
    └── manages ──→ SpawnValidator
                    ├── depends on → GridController (위치 검증)
                    ├── depends on → TurnService (페이즈 검증)
                    └── depends on → ResourceManager (비용 검증)
```

### 6.2 데이터 흐름 분석

#### 🔄 소환 프로세스 데이터 흐름

```
[Player Input] 카드 드래그앤드롭
    ↓
[CardHandManager] UI 이벤트 처리
    ↓
[CardSpawnService] TrySpawnUnitFromCard() 호출
    ↓
[SpawnValidator] 검증 수행
    ├─ [TurnService] → 현재 페이즈 확인
    ├─ [GridController] → 위치 유효성 확인
    └─ [ResourceManager] → 비용 확인
    ↓
[ResourceManager] 자원 소모
    ↓
[CardSpawnService] 유닛 생성
    ↓
[GridState] 그리드에 위치 등록
    ↓
[UnitService] 유닛을 게임에 등록 ← 핵심!
    ↓
[Result] 소환 완료
```

#### 🎯 턴 시스템 데이터 흐름

```
[TurnService] 페이즈 변경
    ↓
[GameServiceManager] OnPhaseChanged 이벤트 발생
    ↓
[CardServiceManager] HandlePhaseChanged() 수신
    ↓
[페이즈별 분기]
├─ TurnStart → [ResourceManager] 마나 증가 + [CardHandManager] 카드 드로우
├─ AllySummon → [CardHandManager] 플레이어 상호작용 활성화
├─ EnemySummon → AI 소환 로직 (향후 구현)
├─ AllyAction → 아군 유닛 행동 단계
├─ EnemyAction → 적군 유닛 행동 단계
└─ TurnEnd → 효과 정리 및 다음 턴 준비
```

### 6.3 이벤트 기반 통신

**느슨한 결합을 위한 이벤트 시스템:**

```csharp
// TurnService에서 이벤트 발생
turnService.OnPhaseChanged += HandlePhaseChanged;

// ResourceManager에서 자원 변경 이벤트
resourceManager.OnPlayerResourcesChanged += (mana, actionPoints) => {
    // UI 업데이트 등
};

// UnitService에서 유닛 등록 이벤트
unitService.OnUnitRegistered += (unit) => {
    // 소환 완료 처리
};
```

---

## 7. Unity 개발자를 위한 활용 가이드

### 7.1 프로젝트 설정 가이드

#### Inspector 설정

**GameInitializer 컴포넌트:**
```
[SerializeField] private CardServiceManager cardServiceManager;
[SerializeField] private ResourceManager resourceManager;
```

**CardServiceManager 컴포넌트:**
```
[Header("카드 서비스 컴포넌트")]
// 이 필드들은 런타임에 자동 생성됨 - Inspector 설정 불필요
private CardHandManager cardHandManager;
private CardSpawnService cardSpawnService;
private SpawnValidator spawnValidator;

[Header("초기화 설정")]
[SerializeField] private bool enableEventLogging = true; // 디버깅용 로그 활성화
```

**CardHandManager 컴포넌트:**
```
[Header("핸드 관리 설정")]
[SerializeField] private int maxHandSize = 7; // 최대 핸드 크기

[Header("UI 설정")]
[SerializeField] private Transform handUIParent; // 핸드 UI 부모 (자동 찾기 가능)
[SerializeField] private GameObject cardUIPrefab; // 카드 UI 프리팹 필수!
[SerializeField] private float cardSpacing = 120f; // 카드 간격
[SerializeField] private bool arrangeCardsInArc = true; // 호형 배열 여부
[SerializeField] private float arcRadius = 800f; // 호형 반지름
```

#### 필수 프리팹 설정

**CardUI 프리팹 요구사항:**
```csharp
// CardUI 컴포넌트가 반드시 필요
public class CardUI : MonoBehaviour
{
    public void SetCardData(CardData cardData) { ... }
    public void SetDraggable(bool draggable) { ... }
    // 드래그앤드롭 기능 구현 필요
}
```

### 7.2 카드 데이터 설정

#### CardData 생성 예시

```csharp
// 유닛 카드 생성
var unitCard = CardData.CreateUnitCard(
    "전사",                    // 카드 이름
    "근접 전투 유닛입니다",        // 설명
    2,                        // 마나 비용
    1,                        // 행동력 비용
    warriorUnitData           // UnitData 참조
);

// 주문 카드 생성
var spellCard = CardData.CreateSpellCard(
    "파이어볼",                // 카드 이름
    "적에게 화염 피해를 입힙니다", // 설명
    3, 1,                     // 마나/행동력 비용
    SpellType.Damage,         // 주문 타입
    25,                       // 효과값 (데미지)
    5f,                       // 범위
    2f                        // 시전 시간
);
```

### 7.3 확장 가능한 구조

#### 새로운 카드 타입 추가

```csharp
// 1. SpellType enum 확장
public enum SpellType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Shield,
    Teleport,
    NewSpellType  // ← 새 타입 추가
}

// 2. CardSpawnService에서 처리 로직 추가
private void ApplySpellEffectToUnit(Unit unit, SpellType spellType, int effectValue, bool isPlayerSpell)
{
    switch (spellType)
    {
        // ... 기존 케이스들
        case SpellType.NewSpellType:
            // 새로운 주문 효과 구현
            HandleNewSpellEffect(unit, effectValue);
            break;
    }
}
```

#### AI 시스템 연동

```csharp
// CardServiceManager에서 AI 소환 지원
private void HandleEnemySummonPhase()
{
    // AI 시스템 연동 지점
    var aiSystem = ServiceLocator.Get<IAISystem>();
    if (aiSystem != null)
    {
        aiSystem.ExecuteEnemySummonPhase();
    }
}
```

### 7.4 디버깅 및 테스트

#### 에디터 도구 활용

```csharp
#if UNITY_EDITOR
// CardHandManager에서 제공하는 에디터 도구들
[SerializeField] private bool showHandDebugInfo = false;
[SerializeField] private CardData testCardData;

// 런타임에서 테스트 가능한 기능들:
// - Add Test Card: 테스트 카드 추가
// - Generate Test Cards: 다양한 테스트 카드 생성
// - Add Sample Spell Cards: 샘플 주문 카드들
// - Add Sample Unit Cards: 샘플 유닛 카드들
// - Clear Hand: 핸드 초기화
// - Toggle Arc Layout: 레이아웃 토글
#endif
```

#### 상태 모니터링

```csharp
// 각 서비스의 상태 확인
Debug.Log(cardServiceManager.GetServiceStatus());
Debug.Log(cardHandManager.GetStatus());
Debug.Log(cardSpawnService.GetStatus());
Debug.Log(spawnValidator.GetStatus());
```

### 7.5 성능 최적화 팁

#### 메모리 관리
```csharp
// CardUI 오브젝트 풀링
public class CardUIPool : MonoBehaviour
{
    private Queue<CardUI> cardUIPool = new Queue<CardUI>();

    public CardUI GetCardUI()
    {
        if (cardUIPool.Count > 0)
            return cardUIPool.Dequeue();
        else
            return Instantiate(cardUIPrefab).GetComponent<CardUI>();
    }

    public void ReturnCardUI(CardUI cardUI)
    {
        cardUI.gameObject.SetActive(false);
        cardUIPool.Enqueue(cardUI);
    }
}
```

#### 이벤트 구독 해제
```csharp
// CardServiceManager에서 자동으로 처리됨
private void OnDestroy()
{
    DisconnectServiceEvents(); // 메모리 누수 방지
}
```

---

## 결론

CardServiceManager 시스템은 Unity에서 카드 게임을 개발할 때 필요한 **모든 카드 관련 기능을 체계적으로 관리**하는 강력한 아키텍처입니다.

### 핵심 장점:
- **🏗️ 모듈화**: 각 서비스가 명확한 역할 분담
- **🔄 확장성**: 새로운 카드 타입이나 기능 추가 용이
- **🎯 의존성 관리**: ServiceLocator 패턴으로 느슨한 결합
- **📱 UI 통합**: 게임 로직과 UI가 자연스럽게 연동
- **🚀 Unity 친화적**: MonoBehaviour 기반의 Unity 네이티브 구조

### 활용 권장 사항:
1. **프로토타입 단계**: 에디터 디버깅 도구를 활용해 빠른 테스트
2. **개발 단계**: 각 서비스의 인터페이스를 통한 Mock 테스트
3. **완성 단계**: 이벤트 로깅을 통한 성능 모니터링

이 시스템을 기반으로 하면 복잡한 카드 게임도 체계적이고 유지보수하기 쉬운 구조로 개발할 수 있습니다.