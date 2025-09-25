# 카드 드래그 앤 드롭 유닛 소환 시스템 - 설계 문서 (개정판)

## 📋 프로젝트 개요

Unity 턴제 전략 게임에 카드 드래그 앤 드롭을 통한 유닛 소환 시스템을 통합하는 완전한 설계 및 구현 가이드입니다. 이 문서는 `GameInitializer` 중심의 서비스 로케이터(Service Locator) 패턴을 따르며, 기존 아키텍처와의 완벽한 통합을 목표로 합니다.

### 🎯 요구사항

  - ✅ 카드 UI를 선택하여 타일로 드래그 가능
  - ✅ 드롭 시 카드 정보 기반 유닛 소환 및 주문 발동
  - ✅ 빈 타일, 지정된 소환 영역 등 규칙에 따른 소환/사용 가능
  - ✅ 플레이어는 좌측 1열, 적군은 우측 1열에만 유닛 소환
  - ✅ `GameServiceManager`가 관리하는 기존 4페이즈 턴 시스템과 완벽 통합

## 🏗️ 시스템 아키텍처

### 🏛️ 아키텍처 핵심 원칙

본 시스템은 `GameInitializer`에 의해 관리되는 **서비스 로케이터 패턴**을 따릅니다. 모든 핵심 서비스는 `GameInitializer`를 통해 `ServiceLocator`에 등록되며, 다른 서비스들은 이를 통해 필요한 의존성을 참조합니다.

### 📜 신규 서비스 구조

카드 관련 기능들은 **`CardServiceManager`** 라는 새로운 관리자 클래스 아래에 모듈화됩니다.

  * **`GameInitializer`** (최상위 초기화 담당)

      * `GridManager` 등록
      * `GameServiceManager` 등록
      * **`CardServiceManager` (신규)** 등록

  * **`GameServiceManager`** (기존 역할 유지: 턴, 유닛 관리)

      * `TurnService`
      * `UnitService`
      * `GameService`
      * `UIService`

  * **`CardServiceManager`** (신규: 카드 시스템 총괄)

      * **`CardHandManager`**: 플레이어의 카드 핸드를 관리 (UI 포함).
      * **`CardSpawnService`**: 카드의 유닛 소환 및 주문 발동을 처리.
      * **`SpawnValidator`**: 소환 및 주문 사용 위치의 유효성을 검증.

## 📁 파일 구조 (개정)

```
Assets/Script/Game/
├── Card/
│   ├── UI/
│   │   ├── CardUI.cs (신규)
│   │   └── TileDropHandler.cs (신규)
│   └── Core/
│       └── ... (CardData, UnitCard 등)
├── Managers/
│   ├── GameInitializer.cs (개선)
│   ├── GameServiceManager.cs (역할 명확화)
│   └── CardServiceManager.cs (신규) 
└── Services/
    ├── Card/ (신규 폴더)
    │   ├── CardHandManager.cs (신규)
    │   ├── CardSpawnService.cs (신규)
    │   └── SpawnValidator.cs (신규)
    ├── Unit/
    │   └── UnitService.cs (개선)
    └── Turn/
        └── TurnService.cs
```

## 💥 아키텍처 통합 전략 및 레거시 코드 처리 방안

새로운 카드 시스템을 기존 아키텍처에 안전하게 통합하기 위한 전략입니다.

### 🧐 신규 `CardServiceManager` 도입

기존 `GameServiceManager`가 유닛과 턴에 집중하는 역할을 유지하도록, 카드 관련 기능(핸드 관리, 소환, 유효성 검사)은 **별도의 `CardServiceManager`가 총괄**합니다. 이는 각 매니저의 책임을 명확하게 분리하여 코드의 유지보수성을 높입니다.

  * **`GameInitializer`의 역할**: `GameInitializer`는 `GameServiceManager`와 `CardServiceManager`를 포함한 모든 최상위 매니저들을 초기화하고 `ServiceLocator`에 등록하는 유일한 책임자입니다.
  * **서비스 간 통신**: `CardServiceManager`는 유닛을 성공적으로 소환한 후, `ServiceLocator.Get<IUnitService>()`를 통해 `UnitService`에 접근하여 생성된 유닛을 등록합니다. 이 방식은 서비스 간의 결합도를 낮춥니다.

### ⛔ 레거시 코드 처리

  * **`InputManager` 및 `HandManager` 제거**: 이 두 스크립트는 새로운 드래그 앤 드롭 시스템과 기능적으로 완전히 중복되며, `GameInitializer`의 관리 범위를 벗어나므로 **반드시 프로젝트에서 제거**해야 합니다. 이들의 역할은 `CardServiceManager`와 그 하위 서비스들이 대체합니다.
  * **`ServiceLocator` 패턴으로 통일**: `GameInitializer`에서 사용하는 `ServiceLocator` 등록 방식을 프로젝트의 표준 아키텍처로 삼습니다. 모든 신규 서비스는 이 패턴을 따라야 하며, 이를 통해 코드 전체의 일관성을 확보합니다.

## 🔧 핵심 컴포넌트 구현 (개정)

### 1\. CardServiceManager (신규 카드 시스템 허브)

`GameServiceManager`와 동일한 레벨에서 동작하며, 모든 카드 관련 서비스를 관리하고 초기화합니다.

```csharp
// In CardServiceManager.cs
public class CardServiceManager : MonoBehaviour, ICardServiceManager // 새로운 인터페이스
{
    [SerializeField] private CardHandManager cardHandManager;
    [SerializeField] private CardSpawnService cardSpawnService;
    [SerializeField] private SpawnValidator spawnValidator;

    // GameInitializer에 의해 호출될 초기화 메서드
    public void InitializeAndRegisterServices()
    {
        // 1. 하위 서비스들 생성 및 초기화
        cardSpawnService.Initialize(); // ServiceLocator 의존성 주입

        // 2. ServiceLocator에 자신과 하위 서비스들 등록
        ServiceLocator.Register<ICardServiceManager>(this);
        ServiceLocator.Register<ICardHandManager>(cardHandManager);
        ServiceLocator.Register<ICardSpawnService>(cardSpawnService);
        // ...

        // 3. 다른 서비스의 이벤트 구독
        var turnService = ServiceLocator.Get<ITurnService>();
        if (turnService != null)
        {
            turnService.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(TurnPhase newPhase)
    {
        // 소환 페이즈일 때만 핸드 매니저 활성화
        if (newPhase == TurnPhase.AllySummon)
        {
            cardHandManager.EnablePlayerSummonMode();
        }
        else
        {
            cardHandManager.DisablePlayerSummonMode();
        }
    }
}
```

### 2\. CardSpawnService (소환 및 등록 책임)

유닛 소환을 실행하고, `UnitService`에 해당 유닛을 등록하여 게임 월드에 편입시키는 핵심적인 역할을 합니다.

```csharp
// In CardSpawnService.cs
public class CardSpawnService : MonoBehaviour, ICardSpawnService
{
    private IUnitService unitService; // ServiceLocator를 통해 주입받을 의존성

    public void Initialize()
    {
        // 의존성 해결
        this.unitService = ServiceLocator.Get<IUnitService>();
    }

    public bool TrySpawnUnitFromCard(CardData card, Vector2Int gridPosition)
    {
        // ... (1. 소환 위치 및 조건 검증 by SpawnValidator)

        // ... (2. 유닛 프리팹 인스턴스화)
        GameObject spawnedUnitGO = Instantiate(card.GetUnitToSummon().Prefab, ...);
        Unit newUnit = spawnedUnitGO.GetComponent<Unit>();

        // ... (3. 유닛 초기화)

        // 4. UnitService에 신규 유닛 등록 (핵심)
        if (unitService != null)
        {
            unitService.RegisterUnit(newUnit);
            Debug.Log($"유닛 {newUnit.name}이(가) UnitService에 성공적으로 등록되었습니다.");
            return true;
        }
        else
        {
            Debug.LogError("UnitService를 찾을 수 없어 유닛을 등록할 수 없습니다!");
            Destroy(spawnedUnitGO);
            return false;
        }
    }
}
```

## 🔧 기존 클래스 개선사항

### GameInitializer.cs 개선

새로운 `CardServiceManager`를 초기화하고 등록하는 책임을 추가합니다.

```csharp
// In GameInitializer.cs
public class GameInitializer : MonoBehaviour
{
    [Header("서비스 참조")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameServiceManager gameServiceManager;
    [SerializeField] private CardServiceManager cardServiceManager; // 신규 참조 추가

    // ...

    private void RegisterCoreServices()
    {
        // ... (기존 GridManager, GameServiceManager 등록 로직)

        // CardServiceManager 등록
        if (cardServiceManager != null)
        {
            cardServiceManager.InitializeAndRegisterServices();
            Log("✅ Card services registered via CardServiceManager");
        }
        else
        {
            LogError("❌ CardServiceManager not found - Card services not registered");
        }
    }
}
```

## 🎯 구현 우선순위

### Phase 1: 아키텍처 기반 구축 (1주)

1.  **`CardServiceManager`** 클래스 생성 및 `GameInitializer`에 등록 로직 구현.
2.  **`CardHandManager`, `CardSpawnService`, `SpawnValidator`** 기본 클래스 생성 및 `CardServiceManager`의 하위 서비스로 편입.
3.  **레거시 코드 제거**: `InputManager`, `HandManager` 제거.

### Phase 2: 핵심 로직 구현 (1-2주)

1.  **`SpawnValidator`**: 모든 검증 규칙 (비용, 위치, 페이즈 등) 구현.
2.  **`CardSpawnService`**: 유닛 소환 및 `UnitService` 등록 로직 구현.
3.  **자원 관리 시스템**: 플레이어의 `Mana`, `ActionPoint`를 관리하는 `ResourceManager` 서비스 신설 및 `GameInitializer`에 등록.

### Phase 3: UI 및 상호작용 구현 (2-3주)

1.  **`CardUI`**: 드래그 앤 드롭 기능 구현.
2.  **`TileDropHandler`**: 타일 위에서의 드롭 이벤트 처리 및 시각적 피드백 구현.
3.  **`CardHandManager`**: `TurnService`의 `OnPhaseChanged` 이벤트에 따라 UI를 활성화/비활성화하는 로직 구현.

### Phase 4: 시스템 통합 및 테스트 (1-2주)

1.  **전체 워크플로우 테스트**: 카드 드로우 → 핸드 표시 → 드래그 → 유효성 검사 → 드롭 → 소환 → `UnitService` 등록까지의 전 과정 테스트.
2.  **주문(Spell) 카드** 로직 확장.

-----

*이 문서는 `GameInitializer` 중심의 서비스 로케이터 아키텍처를 기반으로, 새로운 카드 시스템을 모듈화하여 안전하게 통합하는 방안을 제시합니다.*