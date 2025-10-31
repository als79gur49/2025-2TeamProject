# 프로젝트 핵심 구조 분석 보고서

## 1. CardData 구조 및 속성

### 위치
`/Assets/Script/ScriptableObjects/CardData.cs`

### 핵심 역할
- ScriptableObject 기반 카드 데이터 컨테이너 (Unity Inspector 지원)
- Phase 2.10+ 의 새로운 EffectData 시스템 도입

### 주요 속성 (읽기 전용 프로퍼티로 제공)
```
기본 정보:
- CardName: 카드 이름
- Description: 카드 설명
- CardArt: 카드 이미지 스프라이트
- IconSprite: 아이콘 이미지

카드 속성:
- Rarity: 레어리티 (Common/Uncommon/Rare/Epic/Legendary)
- ManaCost: 마나 비용

배치 관련:
- Target (TargetType): None/Ally/Enemy/Any/Ground
- TargetRange: 배치 거리 제한 (-1: 무관, 0+: 해당 거리까지)

효과 시스템:
- EffectDataList: IReadOnlyList<EffectData> 효과 목록
- Keywords: IReadOnlyList<string> 키워드
- RequiredTags: IReadOnlyList<string> 요구사항 태그

제약사항:
- MaxCopiesInDeck: 데크에 보유 가능 최대 장수
- IsPlayableFromHand: 손에서 사용 가능 여부
```

### 주요 공개 API

**마나 관련:**
- `CanAfford(int availableMana)`: bool - 마나 비용 확인
- `GetTotalCost()`: int - 총 비용 반환

**검증 메서드:**
- `HasKeyword(string keyword)`: bool - 특정 키워드 보유 확인
- `HasAnyKeyword(params string[])`: bool - 여러 키워드 중 하나 확인
- `MeetsRequirements(List<string> availableTags)`: bool - 요구사항 만족 확인
- `IsValid()`: bool - 데이터 유효성 검증

**배치 위치 검증:**
- `IsValidTarget(Vector2Int casterPosition, Vector2Int targetPosition)`: bool
- `IsValidTargetWithContext(Vector2Int originPosition, Vector2Int targetPosition, bool isPlayerCard)`: bool
- `static CalculateManhattanDistance(Vector2Int from, Vector2Int to)`: int

**효과 관련:**
- `GetEffectsByType(EffectType effectType)`: List<EffectData>
- `HasEffectType(EffectType effectType)`: bool
- `GetTotalEffectValue(EffectType effectType)`: int
- `GetPrimaryEffectType()`: EffectType?
- `GetMaxAffectedRange()`: int
- `GetAffectedRangeForEffectType(EffectType)`: int
- `GetAffectedPositions(Vector2Int targetPosition, EffectData effectData)`: List<Vector2Int>
- `IsPositionInAffectedRange(Vector2Int targetPosition, Vector2Int checkPosition, EffectData effectData)`: bool

**효과 실행:**
- `CreateEffectInstances()`: List<ICardEffect> - 효과 인스턴스 생성
- `ExecuteEffects(Vector2Int targetPosition, GameContext context)`: int
- `ExecuteCard(Vector2Int targetPosition, GameContext context)`: bool
- `CanExecuteCard(Vector2Int targetPosition, GameContext context)`: bool
- `CanExecuteAllEffects(Vector2Int targetPosition, GameContext context)`: bool
- `GetExecutableEffects(Vector2Int targetPosition, GameContext context)`: List<ICardEffect>

**UI 생성:**
- `GetDetailedDescription()`: string - 상세 설명 생성
- `GetRarityColor()`: Color - 레어리티 색상 반환
- `CreateCopy()`: CardData - 카드 복사 생성

**팩토리 메서드:**
- `static CreateUnitCard(string name, int manaCost, UnitData unitData)`: CardData
- `static CreateDamageCard(string name, string desc, int manaCost, int damageValue, ...)`: CardData
- `static CreateHealCard(string name, string desc, int manaCost, int healValue, ...)`: CardData
- `static CreateMultiEffectCard(string name, string desc, int manaCost, params EffectData[])`: CardData

---

## 2. UI 시스템 (UIPanel, UIPanelManager)

### 파일 위치
- `/Assets/Script/UI/Core/UIPanel.cs` - 추상 베이스 클래스
- `/Assets/Script/UI/Core/UIPanelManager.cs` - 싱글톤 매니저
- `/Assets/Script/UI/Core/IUIPanel.cs` - 인터페이스

### IUIPanel 인터페이스

```csharp
프로퍼티:
- IsActive: bool - 패널이 활성화 상태인지
- Priority: UIPanelPriority - 패널 우선순위
- CurrentState: UIPanelState - 현재 상태

이벤트:
- OnPanelShown: Action<IUIPanel> - 패널 표시 시 발생
- OnPanelHidden: Action<IUIPanel> - 패널 숨김 시 발생

생명주기 메서드:
- Initialize(): void
- OnShow(): void
- OnHide(): void
- Cleanup(): void
```

**우선순위 (UIPanelPriority):**
- Normal
- High
- AlwaysOnTop

**상태 (UIPanelState):**
- Inactive
- Initializing
- Showing
- Active
- Hiding

### UIPanel (추상 베이스 클래스)

**초기화 분리 구조:**
```
Awake()
├─ OnInitializeSelf() - 의존성 없는 초기화 (Instantiate 직후 사용 가능)
└─ ServiceLocator.Get() 금지

Start()
├─ OnInitializeWithDependencies() - 의존성 있는 초기화
└─ ServiceLocator.Get() 사용 가능
```

**주요 메서드:**
- `Initialize()`: 초기화 수행
- `OnShow()`: 패널 표시
- `OnHide()`: 패널 숨김
- `Cleanup()`: 정리 (이벤트 제거 등)
- `Toggle()`: 활성/비활성 토글
- `RaiseOnPanelShown()`: 수동 이벤트 발생 (애니메이션 완료 후 등)
- `RaiseOnPanelHidden()`: 수동 이벤트 발생

### UIPanelManager (싱글톤)

**싱글톤 접근:**
```csharp
public static UIPanelManager Instance { get; }
```

**패널 등록/관리:**
- `RegisterPanel(IUIPanel panel)`: 패널 등록 및 버튼 이벤트 자동 연결
- `UnregisterPanel<T>()`: 패널 등록 해제
- `AutoRegisterAllPanels()`: 씬의 모든 IUIPanel 자동 등록

**패널 접근:**
- `GetPanel<T>(): T` - 특정 타입 패널 반환
- `HasPanel<T>(): bool` - 특정 타입 패널 존재 확인

**패널 제어:**
- `ShowPanel<T>()`: 패널 표시
- `HidePanel<T>()`: 패널 숨김
- `TogglePanel<T>()`: 토글
- `HideAllPanels()`: 모든 패널 숨김

**스택 관리 (모달 창 등):**
- `PushPanel<T>()`: 스택에 추가
- `PopPanel()`: 스택에서 제거
- `GetTopPanel()`: 맨 위 패널 반환

**이벤트:**
- `OnPanelShown`: 패널 표시 시
- `OnPanelHidden`: 패널 숨김 시
- `OnPanelRegistered`: 패널 등록 시
- `OnPanelUnregistered`: 패널 등록 해제 시

**설정:**
- `initializeOnStart`: 시작 시 자동 초기화 여부
- `debugMode`: 디버그 로그 출력 여부

---

## 3. CardUI 드래그 시스템

### 위치
`/Assets/Script/Game/Card/UI/CardUI.cs`

### 역할
- 카드 UI 렌더링 및 인터랙션 처리
- IBeginDragHandler, IDragHandler, IEndDragHandler 구현

### 주요 속성

**UI 컴포넌트:**
```
cardImage: Image - 카드 이미지
itemImage: Image - 카드 아트
cardNameText: TextMeshProUGUI - 카드 이름
costText: TextMeshProUGUI - 마나 비용
descriptionText: TextMeshProUGUI - 설명

유닛 스탯 패널:
attackParent/attackText: 공격력
hpParent/hpText: 체력
movementParent/movementText: 이동거리

드래그 설정:
dragAlpha: 0.6f - 드래그 중 투명도
dragScale: 0.7f - 드래그 중 스케일
returnSpeed: 10f - 원위치 복귀 속도
returnToOriginalPosition: true

이벤트 채널:
cardDragStartChannel: CardInfoEventChannelSO
cardDragEndChannel: CardDragEndEventChannelSO
```

### 드래그 상태 관리

```
originalPosition: Vector3 - 드래그 시작 시 위치
originalScale: Vector3 - 드래그 시작 시 스케일
originalParent: Transform - 드래그 시작 시 부모
originalIndex: int - 핸드 내 원래 인덱스 (매우 중요!)

isDraggable: bool - 드래그 가능 여부
isDragging: bool - 현재 드래그 중인지
```

### 의존성 주입

**서비스 의존성:**
```csharp
ICardSpawnService cardSpawnService
ISpawnValidator spawnValidator
ICardHandManager cardHandManager
IGridRenderer gridRenderer
```

**InjectDependencies():** ServiceLocator에서 자동 주입

### 드래그 앤 드롭 이벤트

**OnBeginDrag(PointerEventData):**
1. 드래그 가능 여부 확인
2. GameFlowLock 상태 확인 (VFX 재생 중 차단)
3. 원래 위치/부모/인덱스 저장
4. 시각적 변경 (투명도, 스케일, 캔버스 재배치)
5. CardInfoEventChannelSO 발생 (카드 정보 표시)

**OnDrag(PointerEventData):**
1. 마우스 위치로 이동
2. CheckDropValidation() - Physics Raycast로 실시간 검사
3. 드롭 가능 여부 시각적 피드백 (색상 변경)

**OnEndDrag(PointerEventData):**
1. 프리뷰 정리 (gridRenderer.ClearCardPreview())
2. HandleDrop() - 드롭 처리 (레이캐스팅이 비활성화된 상태에서)
3. 레이캐스팅 재활성화
4. 실패 시 원위치 복귀 코루틴
5. CardDragEndEventChannelSO 발생 (카드 정보 숨김)

### 드롭 처리

**CheckDropValidation():**
- Physics.RaycastAll()로 타일 검출
- 거리순 정렬 (대각선 카메라 대응)
- TileDropHandler 컴포넌트로 유효성 검사

**HandleDrop():**
- TileDropHandler.HandleCardDrop() 위임
- 성공 여부 반환

**ValidateCardDrop():**
```
EffectData 기반 검증:
├─ ValidateEffectBasedCardDrop()
│  ├─ Summon: ValidateSummonEffect()
│  ├─ Damage: ValidateDamageEffect()
│  └─ Heal: ValidateHealEffect()
└─ 폴백: ValidateLegacyCardDrop()
```

### 원위치 복귀 (ReturnToOriginalPosition 코루틴)

1. 원래 부모로 복귀
2. 원래 인덱스로 복귀 (카드 순서 유지)
3. CardHandManager.RefreshHandLayout() 호출
4. 부드러운 이동 및 스케일 복원

### 공개 API

**카드 데이터:**
- `SetCardData(CardData data)`: void
- `GetCardData()`: CardData

**드래그 상태:**
- `SetDraggable(bool draggable)`: void
- `IsDraggable`: bool - 프로퍼티
- `IsDragging`: bool - 프로퍼티

**카드 사용 완료:**
- `OnCardUsed()`: void - 카드 사용 시 호출 (핸드에서 제거)

---

## 4. 이벤트 시스템 (EventChannel)

### 기본 이벤트 채널 구조

#### GameEventChannelSO<T> (제네릭)

**위치:** `/Assets/Script/ScriptableObjects/GameEventChannelSO.cs`

```csharp
public abstract class GameEventChannelSO<T> : ScriptableObject
{
    private event Action<T> OnEventRaised;
    
    // API:
    public void RaiseEvent(T eventData)
    public void Subscribe(Action<T> listener)
    public void Unsubscribe(Action<T> listener)
    public int GetListenerCount(): int // 디버깅용
}
```

**특징:**
- 데이터를 함께 전달 가능
- Subscribe 시 자동으로 기존 리스너 제거 후 재등록 (중복 방지)
- 개발/디버그 빌드에서만 로그 출력

#### VoidEventChannelSO (파라미터 없음)

**위치:** `/Assets/Script/ScriptableObjects/VoidEventChannelSO.cs`

```csharp
public abstract class VoidEventChannelSO : ScriptableObject
{
    private event Action OnEventRaised;
    
    // API:
    public void RaiseEvent()
    public void Subscribe(Action listener)
    public void Unsubscribe(Action listener)
    public int GetListenerCount(): int
}
```

### 프로젝트에서 사용 중인 이벤트 채널들

**CardInfoEventChannelSO** (데이터 전달)
- 위치: `/Assets/Script/UI/Events/CardInfoEventChannelSO.cs`
- 타입: `GameEventChannelSO<CardData>`
- 용도: 카드 드래그 시작 시 카드 정보 표시
- 발행자: `CardUI.OnBeginDrag()`
- 구독자: 카드 정보 UI 패널

**CardDragEndEventChannelSO** (파라미터 없음)
- 위치: `/Assets/Script/UI/Events/CardDragEndEventChannelSO.cs`
- 타입: `VoidEventChannelSO`
- 용도: 카드 드래그 종료 시 카드 정보 숨김
- 발행자: `CardUI.OnEndDrag()`
- 구독자: 카드 정보 UI 패널

**기타 이벤트 채널:**
- `GameEventChannelSO`: 게임 이벤트
- `SoundEventChannelSO`: 사운드 재생 이벤트
- `DamageDisplayEventChannelSO`: 데미지 표시 이벤트

---

## 5. 싱글톤 매니저들

### CardHandManager

**위치:** `/Assets/Script/Game/Services/Card/CardHandManager.cs`

**역할:** 플레이어의 카드 핸드 관리 (데이터 + UI)

**초기화 패턴:**
```
CardServiceManager.InitializeAndRegisterServices()
  └─ CardHandManager.Init(ITurnService)
     ├─ InjectDependencies()
     ├─ InitializeUI()
     ├─ SetupEventSystem()
     ├─ SetupInitialHand()
     └─ isInitialized = true
```

**핸드 관리 API:**
```
카드 추가/제거:
- AddCardToHand(CardData): bool
- RemoveCardFromHand(CardData): bool
- RemoveCardFromHand(CardData, CardUI): bool ⭐ 중요!
- ClearHand(): void

카드 조회:
- GetHandCards(): List<CardData>
- GetCardAt(int index): CardData
- HasCard(CardData): bool
- IsHandFull(): bool

상태 조회:
- IsInitialized: bool
- HandSize: int
- IsPlayerSummonMode: bool
```

**플레이어 상호작용:**
```
- EnablePlayerSummonMode(): void
- DisablePlayerSummonMode(): void
- DrawRandomCard(): void
- RefreshHandLayout(): void
```

**GlobalStateManager 통합:**
```
_stateManager.OnBusyStateChanged += HandleGlobalBusyStateChanged

GameFlowLock (VFX 재생) 시:
- _isGameFlowLocked = true
- UpdateCardInteractivity() → 모든 카드 비드래그 가능
```

**레이아웃 매니저:**
```
CardHandLayoutManager 사용:
- ArrangeInArc() - 호형 배치
- ArrangeInLine() - 직선 배치
- ArrangeVerticalCentered() - 수직 중앙 배치
```

### UIPanelManager (싱글톤)

이미 위에서 설명함. [2번 섹션 참고](#2-ui-시스템-uipanel-uipanelmanager)

### ServiceLocator

**패턴:** 싱글톤 기반 서비스 로케이터
```csharp
ServiceLocator.Register<T>(T service)
T service = ServiceLocator.Get<T>()
bool exists = ServiceLocator.IsInitialized
```

**등록된 주요 서비스:**
- `ICardServiceManager`
- `IGridManager`
- `IGlobalStateManager`
- `ITurnService`
- 기타 매니저들

---

## 6. 아키텍처 요약

### 계층 구조

```
UI Layer (CardUI)
    ↓ (드래그 이벤트)
Business Logic Layer
    ├─ CardData (효과 실행)
    ├─ CardHandManager (핸드 관리)
    ├─ CardSpawnService (유닛 소환)
    └─ SpawnValidator (배치 검증)
    ↓
Grid Layer (GridManager)
    ├─ GridState (상태)
    ├─ GridController (로직)
    └─ GridRenderer (시각화)
    ↓
Tile System & Units
```

### 주요 패턴

1. **Observer Pattern (이벤트 채널)**
   - CardInfoEventChannelSO
   - CardDragEndEventChannelSO
   - 느슨한 결합

2. **Singleton Pattern**
   - UIPanelManager
   - ServiceLocator
   - 전역 접근 지점

3. **Factory Pattern**
   - CardData.CreateDamageCard()
   - CardData.CreateHealCard()
   - CardEffectFactory.CreateEffect()

4. **Strategy Pattern (효과 시스템)**
   - EffectData + ICardEffect
   - 다양한 효과 타입 확장 가능

5. **Dependency Injection**
   - CardUI.InjectDependencies()
   - CardHandManager.Init(ITurnService)
   - ServiceLocator 기반

---

## 7. 중요한 통신 흐름

### 카드 드래그 → 배치 완료

```
CardUI.OnBeginDrag()
  ├─ originalIndex 저장 ⭐
  └─ CardInfoEventChannelSO.RaiseEvent(cardData)

CardUI.OnDrag()
  └─ CheckDropValidation()
     └─ Physics.Raycast → TileDropHandler 검색

CardUI.OnEndDrag()
  ├─ HandleDrop()
  │  └─ TileDropHandler.HandleCardDrop(cardData, cardUI)
  │     └─ CardSpawnService.SpawnCard(cardData, position)
  ├─ 성공 시: CardUI.OnCardUsed()
  │  └─ CardHandManager.RemoveCardFromHand(cardData, cardUI)
  │     └─ RefreshHandLayout()
  ├─ 실패 시: ReturnToOriginalPosition()
  │  ├─ SetParent(originalParent)
  │  ├─ SetSiblingIndex(originalIndex) ⭐
  │  └─ RefreshHandLayout()
  └─ CardDragEndEventChannelSO.RaiseEvent()
```

### 플레이어 상호작용 활성화

```
Turn Phase
  └─ AllySummon Phase
     ├─ CardHandManager.EnablePlayerSummonMode()
     │  └─ UpdateCardInteractivity()
     │     └─ 모든 CardUI에 SetDraggable(true)
     └─ GlobalStateManager 이벤트 구독

VFX 재생 중:
  GlobalStateManager.SetBusyState(GameFlowLock, true)
    └─ CardHandManager.HandleGlobalBusyStateChanged()
       └─ UpdateCardInteractivity()
          └─ 모든 CardUI에 SetDraggable(false)
```

---

## 8. 주요 의존성 관계

```
CardUI
  ├─ ICardSpawnService (카드 소환)
  ├─ ISpawnValidator (배치 검증)
  ├─ ICardHandManager (핸드 관리)
  ├─ IGridRenderer (타일 프리뷰)
  └─ IGlobalStateManager (VFX 차단 상태)

CardHandManager
  ├─ ITurnService (턴 관리)
  ├─ IGlobalStateManager (VFX 차단 상태)
  └─ CardHandLayoutManager (레이아웃)

CardData
  ├─ EffectData (효과 데이터)
  ├─ CardEffectFactory (효과 인스턴스 생성)
  └─ GameContext (효과 실행 컨텍스트)
```

