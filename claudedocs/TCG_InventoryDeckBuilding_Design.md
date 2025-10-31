# TCG 인벤토리 & 덱 빌딩 시스템 설계도

## 목차
1. [프로젝트 개요](#프로젝트-개요)
2. [현재 시스템 분석](#현재-시스템-분석)
3. [산업 표준 분석](#산업-표준-분석)
4. [아키텍처 설계](#아키텍처-설계)
5. [Phase별 구현 계획](#phase별-구현-계획)
6. [구현 우선순위](#구현-우선순위)
7. [기술적 고려사항](#기술적-고려사항)

---

## 프로젝트 개요

### 목표
플레이어가 소유한 카드를 관리하고, 이를 활용하여 전략적인 덱을 구성할 수 있는 인벤토리 & 덱 빌딩 시스템 구축

### 핵심 기능
- **인벤토리 시스템**: 소유한 모든 카드 조회, 필터링, 정렬
- **덱 빌더**: 드래그 앤 드롭으로 덱 구성
- **덱 검증**: 규칙 기반 덱 유효성 검사
- **실시간 피드백**: 마나 커브, 시너지 분석
- **데이터 영속성**: 덱 저장/로드

### 기술 스택
- Unity 2021.3+
- C# 9.0
- Unity UI Toolkit (UGUI)
- ScriptableObject 기반 데이터 아키텍처
- 이벤트 채널 패턴

---

## 현재 시스템 분석

### 기존 UI 아키텍처

#### 1. IUIPanel 인터페이스
모든 UI 패널의 기본 계약 정의
```csharp
public interface IUIPanel
{
    void Initialize();
    void Show();
    void Hide();
    void UpdatePanel();
}
```

#### 2. UIPanel 추상 클래스
생명주기 관리 및 공통 기능 제공
- 패널 활성화/비활성화 로직
- 애니메이션 통합 지점
- 이벤트 구독 관리

#### 3. UIPanelManager
싱글톤 패턴으로 모든 패널 생명주기 관리
- 패널 등록 및 조회
- 패널 간 전환 관리
- 스택 기반 네비게이션

#### 4. CardUI
드래그 앤 드롭 기능이 구현된 카드 UI 컴포넌트
- `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler` 구현
- CanvasGroup을 통한 레이캐스팅 제어
- 원래 위치 저장 및 복원 메커니즘
- 드래그 중 시각적 피드백 (투명도, 스케일 조정)

#### 5. CardData
완전히 캡슐화된 카드 데이터 구조
```csharp
[CreateAssetMenu(fileName = "New Card", menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    [SerializeField] private string cardName;
    [SerializeField] private int manaCost;
    [SerializeField] private Sprite cardArt;
    [SerializeField] private int maxCopiesInDeck = 3;
    // ... 추가 속성들
}
```

### 현재 카드 드래그 시스템 요약

| 기능 | 구현 상태 | 비고 |
|------|----------|------|
| 드래그 시작 감지 | ✅ | IBeginDragHandler |
| 드래그 중 이동 | ✅ | IDragHandler |
| 드래그 종료 처리 | ✅ | IEndDragHandler |
| 시각적 피드백 | ✅ | CanvasGroup 알파/스케일 |
| 원위치 복원 | ✅ | Transform 저장/복원 |
| 드롭 존 감지 | ⚠️ | 부분 구현 필요 |
| 드롭 유효성 검사 | ❌ | 신규 구현 필요 |

---

## 산업 표준 분석

### 1. Hearthstone UI 접근법

#### 핵심 설계 원칙
1. **물리성 (Physicality)**: 모든 UI 요소를 실제 만질 수 있는 물건처럼 느껴지게 함
2. **Box 컨셉**: 모든 UI를 하나의 상자(Box) 안에 담긴 것처럼 구성
3. **최소주의 디자인**: 필요한 정보만 표시하면서도 미적 매력 유지
4. **즉각적인 피드백**: 모든 상호작용에 명확한 사운드와 애니메이션 제공

#### 컬렉션 & 덱 빌더 UI 특징
- **카드 바인더 스타일**: 실제 카드 컬렉션북을 모방한 레이아웃
- **다양한 필터링**: 영웅 타입, 세트, 마나 비용별 필터
- **두 가지 뷰 모드**:
  - 전체 뷰: 카드 디테일 확인 용이
  - 컴팩트 뷰: 빠른 비교를 위한 작은 카드
- **스크롤 가능한 덱 리스트**
- **실시간 마나 커브 시각화**: 막대 그래프 형태

#### 적용 가능한 디자인 요소
```
[인벤토리 패널]
┌─────────────────────────────────┐
│ 📚 컬렉션                [X]     │
├─────────────────────────────────┤
│ 필터: [전체▼] [마나▼] [검색]   │
├─────────────────────────────────┤
│ ┌─┬─┬─┬─┐ ┌─┬─┬─┬─┐           │
│ │카│카│카│카│ │카│카│카│카│    │
│ │드│드│드│드│ │드│드│드│드│    │
│ │1│2│3│4│ │5│6│7│8│    [스크롤]
│ └─┴─┴─┴─┘ └─┴─┴─┴─┘           │
│ ┌─┬─┬─┬─┐ ┌─┬─┬─┬─┐           │
│ │ ... (스크롤 가능)             │
└─────────────────────────────────┘
```

### 2. Magic: The Gathering Arena

#### 고급 필터링 시스템
- **Scryfall 구문 지원**:
  - `t:elf` - 타입이 Elf인 카드
  - `t:enchantment t:creature` - 인챈트먼트이면서 크리처인 카드
  - `o:"draw a card"` - 텍스트에 "draw a card"가 포함된 카드

#### 컬렉션 통합 기능
- **덱 완성도 표시**: 부족한 카드 수량 시각화
- **와일드카드 소요량**: 덱 완성에 필요한 리소스 표시
- **자동/수동 정렬**: 마나별 자동 정렬 또는 드래그로 수동 배치

#### 메타게임 통합
- 승률 기반 덱 추천
- 인기 덱 아키타입 제공
- 덱 시너지 분석

#### 적용 가능한 기능
```csharp
// 고급 필터 예시
public class CardFilter
{
    public int? MinManaCost { get; set; }
    public int? MaxManaCost { get; set; }
    public List<CardType> AllowedTypes { get; set; }
    public List<CardRarity> AllowedRarities { get; set; }
    public string TextSearch { get; set; }

    public bool Matches(CardData card)
    {
        // 필터 로직
    }
}
```

### 3. Unity 드래그 앤 드롭 모범 사례

#### 일반적인 패턴
```csharp
// 드래그 가능한 아이템
public class DraggableItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 레이캐스팅 비활성화
        canvasGroup.blocksRaycasts = false;
        // 원래 위치 저장
        originalParent = transform.parent;
        originalPosition = transform.position;
        // 시각적 피드백
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 마우스 위치로 이동
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }
}

// 드롭 영역
public class DropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        // 드롭 처리 로직
    }
}
```

#### 성능 최적화 팁
- **Object Pooling**: 카드 슬롯 재사용으로 가비지 생성 최소화
- **Lazy Loading**: 스크롤 영역에 보이는 카드만 렌더링
- **Raycast 최적화**: 드래그 중에만 Raycast Target 활성화
- **Batch Rendering**: 같은 스프라이트 아틀라스 사용

---

## 아키텍처 설계

### 설계 철학: View와 로직 분리

#### 핵심 원칙

**CardUI의 책임은 "시각적 표현"과 "사용자 인터랙션 보고"에 한정됩니다.**

- ❌ **잘못된 설계**: CardUI가 "카드를 플레이한다" 또는 "카드를 덱에 넣는다"는 도메인 로직을 직접 수행
- ✅ **올바른 설계**: CardUI는 자신의 상태(CardData, CardUIMode)와 드래그 이벤트만 **ScriptableObject Event Channel**에 방송

#### 왜 별도의 InventoryCardSlot을 만들지 않는가?

**문제점: View 컴포넌트 중복**
```
CardUI (게임 내) + InventoryCardSlot (인벤토리)
→ 95% 동일한 시각적 요소 + 90% 동일한 인터랙션 로직
→ DRY 원칙 위반 → 유지보수 악몽
```

**예시: 시각적 변경**
- "모든 레전더리 카드에 반짝이는 VFX 추가" 요청 시
- 별도 컴포넌트 방식: `CardUI.prefab` 수정 + `InventoryCardSlot.prefab` 수정 **(작업량 2배)**
- CardUI 확장 방식: `CardUI.prefab` 수정 **(작업량 1회)**

#### 이벤트 기반 아키텍처

**CardUI는 이벤트만 발생**
```csharp
public enum CardUIMode
{
    InHand,      // 전투 중 Hand → Field 타일 드래그
    InInventory, // 인벤토리 → 덱빌더 드래그
    InDeck       // 덱빌더 내 재정렬
}

public class CardUI : MonoBehaviour
{
    [SerializeField] private CardUIMode mode;
    [SerializeField] private CardDragStartEventChannelSO dragStartChannel;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 이벤트 발생 (도메인 로직 없음)
        dragStartChannel.RaiseEvent(new CardDragData {
            cardData = this.cardData,
            mode = this.mode,
            sourceTransform = this.transform
        });
    }
}
```

**각 Manager가 이벤트를 구독하여 처리**
```csharp
// 전투 중 카드 플레이 담당
public class GamePlayManager : MonoBehaviour
{
    [SerializeField] private CardDragStartEventChannelSO dragStartChannel;

    private void OnEnable()
    {
        dragStartChannel.OnEventRaised += HandleCardDragStart;
    }

    private void HandleCardDragStart(CardDragData data)
    {
        // InHand 모드일 때만 처리
        if (data.mode != CardUIMode.InHand) return;

        // 도메인 로직: 타일 배치 가능 여부 검사
        ValidateFieldPlacement(data.cardData);
    }
}

// 덱 빌딩 담당
public class DeckBuilderService : MonoBehaviour
{
    [SerializeField] private CardDragStartEventChannelSO dragStartChannel;

    private void OnEnable()
    {
        dragStartChannel.OnEventRaised += HandleCardDragStart;
    }

    private void HandleCardDragStart(CardDragData data)
    {
        // InInventory 모드일 때만 처리
        if (data.mode != CardUIMode.InInventory) return;

        // 도메인 로직: 덱 추가 가능 여부 검사
        ValidateDeckAddition(data.cardData);
    }
}
```

#### 설계의 장점

| 측면 | 별도 컴포넌트 방식 | CardUI 확장 방식 |
|------|------------------|-----------------|
| **DRY 준수** | ❌ 95% 코드 중복 | ✅ 단일 컴포넌트 재사용 |
| **유지보수** | ❌ 2배 작업량 | ✅ 1회 수정으로 모든 곳 적용 |
| **확장성** | ❌ 새 기능 시 새 컴포넌트 생성 | ✅ enum 값만 추가 |
| **테스트** | ❌ 2개 컴포넌트 테스트 | ✅ 1개 컴포넌트 + 이벤트 구독자 테스트 |
| **SRP 준수** | ⚠️ 로직과 View 혼재 위험 | ✅ View는 이벤트만, 로직은 Manager가 처리 |

#### 확장 시나리오

**미래에 "카드 제작(Crafting)" 기능 추가 시**
```csharp
// 1. enum에 값만 추가
public enum CardUIMode
{
    InHand, InInventory, InDeck, InCrafting // ← 추가
}

// 2. CraftingManager가 이벤트 구독
public class CraftingManager : MonoBehaviour
{
    private void HandleCardDragStart(CardDragData data)
    {
        if (data.mode != CardUIMode.InCrafting) return;
        // 제작 로직 처리
    }
}

// 3. 기존 CardUI.prefab 재사용 (새 prefab 불필요)
```

### 시스템 컴포넌트 다이어그램

```
┌─────────────────────────────────────────────────────────┐
│                    UI Layer                              │
│  ┌──────────────────┐        ┌──────────────────┐      │
│  │ InventoryPanel   │◄──────►│ DeckBuilderPanel │      │
│  │                  │        │                  │      │
│  │ - CardGrid       │        │ - DeckList       │      │
│  │ - Filters        │        │ - ManaCurve      │      │
│  │ - Sorting        │        │ - Validation UI  │      │
│  └──────────────────┘        └──────────────────┘      │
│         ▲                            ▲                  │
│         │                            │                  │
└─────────┼────────────────────────────┼──────────────────┘
          │                            │
┌─────────┼────────────────────────────┼──────────────────┐
│         │     Coordination Layer     │                  │
│  ┌──────▼────────────────────────────▼──────┐          │
│  │   DeckInventoryCoordinator               │          │
│  │                                           │          │
│  │ - Event Subscription Management          │          │
│  │ - Drag/Drop Coordination                 │          │
│  │ - Panel Communication                    │          │
│  └──────────────────────────────────────────┘          │
│         ▲                            ▲                  │
└─────────┼────────────────────────────┼──────────────────┘
          │                            │
┌─────────┼────────────────────────────┼──────────────────┐
│         │      Business Logic Layer  │                  │
│  ┌──────▼──────────┐        ┌────────▼─────────┐       │
│  │ CollectionMgr   │        │  DeckValidator   │       │
│  │                 │        │                  │       │
│  │ - Owned Cards   │        │ - Size Rules     │       │
│  │ - Add/Remove    │        │ - Copy Limits    │       │
│  │ - Save/Load     │        │ - Mana Curve     │       │
│  └─────────────────┘        │ - Synergy Check  │       │
│         ▲                   └──────────────────┘       │
└─────────┼──────────────────────────────────────────────┘
          │
┌─────────▼──────────────────────────────────────────────┐
│                    Data Layer                           │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐  │
│  │  CardData    │  │   DeckData   │  │ EventChannel│  │
│  │  (SO)        │  │   (SO)       │  │  (SO)       │  │
│  └──────────────┘  └──────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### 클래스 다이어그램

```
┌─────────────────────────────────────────────────────────┐
│                 <<interface>>                            │
│                   IUIPanel                               │
│  + Initialize() : void                                   │
│  + Show() : void                                         │
│  + Hide() : void                                         │
│  + UpdatePanel() : void                                  │
└─────────────────┬───────────────────────────────────────┘
                  △
                  │
     ┌────────────┴────────────┐
     │                         │
┌────▼──────────────┐  ┌──────▼────────────────┐
│  InventoryPanel   │  │  DeckBuilderPanel     │
├───────────────────┤  ├───────────────────────┤
│ - cardGrid        │  │ - deckList            │
│ - filterUI        │  │ - manaCurve           │
│ - sortUI          │  │ - validationUI        │
├───────────────────┤  ├───────────────────────┤
│ + Initialize()    │  │ + Initialize()        │
│ + Show()          │  │ + OnDrop()            │
│ + Hide()          │  │ + AddCardToDeck()     │
│ + ApplyFilter()   │  │ + RemoveCardFromDeck()│
│ + SortCards()     │  │ + ValidateDeck()      │
└───────────────────┘  └───────────────────────┘
        │                       │
        │                       │
        │  ┌───────────────────▼────────────────┐
        │  │  DeckInventoryCoordinator          │
        │  ├────────────────────────────────────┤
        └─►│ - inventoryPanel                   │
           │ - deckBuilderPanel                 │
           │ - transferEventChannel             │
           ├────────────────────────────────────┤
           │ + OnInventoryCardDragEnd()         │
           │ + OnDeckCardDragStart()            │
           │ + OnCardAddedToDeck()              │
           │ + OnCardRemovedFromDeck()          │
           └────────────────────────────────────┘
```

### 이벤트 플로우 (CardUI 중심 이벤트 기반 아키텍처)

#### 1. 인벤토리 → 덱빌더 드래그 시나리오

```
[플레이어 액션] 인벤토리에서 카드 드래그 시작
        │
        ▼
[CardUI] OnBeginDrag() (mode = InInventory)
        │
        ├─► CanvasGroup.blocksRaycasts = false
        ├─► 시각적 피드백 (알파, 스케일 조정)
        └─► CardDragStartEventChannelSO.RaiseEvent(CardDragData)
        │       ├─ cardData: CardData
        │       ├─ mode: CardUIMode.InInventory
        │       └─ sourceTransform: Transform
        │
        ▼
[DeckBuilderService] HandleCardDragStart() ← 이벤트 구독
        │
        ├─► if (data.mode != InInventory) return; // 필터링
        ├─► 덱 추가 가능 여부 사전 검증
        │   ├─► DeckValidator.CanAddCard(data.cardData)
        │   └─► 유효성 결과를 UI에 시각적 피드백
        │
        └─► (검증 결과 저장, 드롭 대기)
        │
        ▼
[플레이어 액션] 덱 빌더 패널 위로 드래그 중
        │
        ▼
[CardUI] OnDrag()
        │
        └─► transform.position = eventData.position
        │
        ▼
[플레이어 액션] 덱 빌더 패널에 드롭
        │
        ▼
[CardUI] OnEndDrag()
        │
        ├─► CardDragEndEventChannelSO.RaiseEvent(CardDragData, eventData)
        ├─► CanvasGroup.blocksRaycasts = true
        └─► 시각적 피드백 복원
        │
        ▼
[DeckBuilderService] HandleCardDragEnd() ← 이벤트 구독
        │
        ├─► if (data.mode != InInventory) return; // 필터링
        │
        ├─► Raycast로 드롭 위치 확인
        │   └─► IsDraggedOverDeckPanel(eventData.position)
        │
        ├─► DeckValidator.CanAddCard() 최종 검증
        │   ├─► 덱 크기 체크 (≤ MaxDeckSize)
        │   ├─► 카드 중복 제한 체크 (≤ MaxCopiesInDeck)
        │   └─► 결과 반환
        │
        └─► 유효한 경우:
            │
            ├─► CollectionManager.AddCardToDeck(data.cardData)
            │   │
            │   └─► DeckChangedEventChannelSO.RaiseEvent()
            │
            ├─► InventoryPanel.UpdateCardAvailability() ← 이벤트 구독
            │   └─► 카드 개수 UI 업데이트
            │
            └─► PlayAddCardFeedback()
                ├─► 파티클 효과
                ├─► 사운드 재생
                └─► 스케일 펄스 애니메이션
```

#### 2. 게임 내 Hand → Field 타일 드래그 시나리오 (비교)

```
[플레이어 액션] Hand에서 카드 드래그 시작
        │
        ▼
[CardUI] OnBeginDrag() (mode = InHand)
        │
        └─► CardDragStartEventChannelSO.RaiseEvent(CardDragData)
        │       ├─ mode: CardUIMode.InHand
        │       └─ ...
        │
        ▼
[GamePlayManager] HandleCardDragStart() ← 이벤트 구독
        │
        ├─► if (data.mode != InHand) return; // 필터링
        ├─► SpawnValidator.CanSpawnUnit() 사전 검증
        └─► GridRenderer.ShowCardPreview() // 타일 하이라이트
        │
        ▼
[CardUI] OnDrag()
        │
        └─► transform.position = eventData.position
        │
        ▼
[CardUI] OnEndDrag()
        │
        └─► CardDragEndEventChannelSO.RaiseEvent(...)
        │
        ▼
[GamePlayManager] HandleCardDragEnd() ← 이벤트 구독
        │
        ├─► if (data.mode != InHand) return; // 필터링
        ├─► Physics Raycast로 3D 타일 검출
        ├─► TileDropHandler.HandleCardDrop()
        └─► CardSpawnService.SpawnUnit()
```

#### 설계 포인트

**✅ CardUI는 컨텍스트를 알지 못함**
- CardUI는 자신의 `mode` 값만 알고, 이벤트에 포함시켜 전달
- "이 카드가 인벤토리에서 왔는지, Hand에서 왔는지"는 **CardUI의 관심사가 아님**

**✅ 각 Manager가 자신의 도메인만 처리**
- `DeckBuilderService`: `mode == InInventory`인 이벤트만 필터링하여 덱 빌딩 로직 수행
- `GamePlayManager`: `mode == InHand`인 이벤트만 필터링하여 전투 로직 수행
- 서로 간섭하지 않음

**✅ 확장성**
- 새로운 컨텍스트 추가 시: enum 값 추가 + 새 Manager가 이벤트 구독
- CardUI 코드 수정 불필요

---

## Phase별 구현 계획

### Phase 1: 인벤토리 패널 기본 구조

#### 1.1 InventoryPanel 클래스
**파일**: `Assets/Scripts/UI/Panels/InventoryPanel.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class InventoryPanel : UIPanel, IOpenablePanel
{
    [Header("Inventory Settings")]
    [SerializeField] private Transform cardGridContainer;
    [SerializeField] private GameObject cardSlotPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Filtering")]
    [SerializeField] private TMP_Dropdown rarityFilter;
    [SerializeField] private TMP_Dropdown typeFilter;
    [SerializeField] private TMP_InputField searchField;

    [Header("Sorting")]
    [SerializeField] private TMP_Dropdown sortDropdown;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI totalCardsText;

    // 카드 컬렉션 데이터
    private List<CardData> allCards = new List<CardData>();
    private List<CardData> filteredCards = new List<CardData>();
    private Dictionary<CardData, InventoryCardSlot> cardSlots =
        new Dictionary<CardData, InventoryCardSlot>();

    // 현재 필터/정렬 상태
    private CardRarity currentRarityFilter = CardRarity.All;
    private CardType currentTypeFilter = CardType.All;
    private string currentSearchText = "";
    private SortCriteria currentSort = SortCriteria.Name;

    // IOpenablePanel 구현
    public Button OpenButton { get; set; }
    public Button CloseButton { get; set; }

    #region Lifecycle

    public override void Initialize()
    {
        base.Initialize();

        // 필터 드롭다운 설정
        SetupFilters();

        // 정렬 드롭다운 설정
        SetupSorting();

        // 검색 필드 이벤트 구독
        searchField.onValueChanged.AddListener(OnSearchTextChanged);

        // 컬렉션 매니저에서 카드 로드
        LoadCardsFromCollection();
    }

    public override void Show()
    {
        base.Show();
        RefreshDisplay();
    }

    public override void Hide()
    {
        base.Hide();
    }

    #endregion

    #region Card Management

    private void LoadCardsFromCollection()
    {
        var collectionManager = CollectionManager.Instance;
        allCards = collectionManager.GetAllOwnedCards();

        ApplyFiltersAndSort();
        PopulateCardGrid();
    }

    private void ApplyFiltersAndSort()
    {
        filteredCards = allCards
            .Where(card => MatchesFilters(card))
            .OrderBy(card => GetSortKey(card))
            .ToList();
    }

    private bool MatchesFilters(CardData card)
    {
        // 희귀도 필터
        if (currentRarityFilter != CardRarity.All &&
            card.Rarity != currentRarityFilter)
            return false;

        // 타입 필터
        if (currentTypeFilter != CardType.All &&
            card.CardType != currentTypeFilter)
            return false;

        // 텍스트 검색
        if (!string.IsNullOrEmpty(currentSearchText))
        {
            string searchLower = currentSearchText.ToLower();
            if (!card.CardName.ToLower().Contains(searchLower) &&
                !card.Description.ToLower().Contains(searchLower))
                return false;
        }

        return true;
    }

    private object GetSortKey(CardData card)
    {
        return currentSort switch
        {
            SortCriteria.Name => card.CardName,
            SortCriteria.ManaCost => card.ManaCost,
            SortCriteria.Rarity => (int)card.Rarity,
            SortCriteria.Type => (int)card.CardType,
            _ => card.CardName
        };
    }

    private void PopulateCardGrid()
    {
        // 기존 슬롯 클리어
        foreach (var slot in cardSlots.Values)
        {
            Destroy(slot.gameObject);
        }
        cardSlots.Clear();

        // 필터링된 카드로 슬롯 생성
        foreach (var card in filteredCards)
        {
            GameObject slotObj = Instantiate(cardSlotPrefab, cardGridContainer);
            InventoryCardSlot slot = slotObj.GetComponent<InventoryCardSlot>();

            int ownedCount = CollectionManager.Instance.GetOwnedCount(card);
            slot.Setup(card, ownedCount, this);

            cardSlots[card] = slot;
        }

        UpdateTotalCardsText();
    }

    public void UpdateCardAvailability(CardData card)
    {
        if (cardSlots.TryGetValue(card, out var slot))
        {
            int ownedCount = CollectionManager.Instance.GetOwnedCount(card);
            slot.UpdateCount(ownedCount);
        }
    }

    #endregion

    #region Filtering & Sorting

    private void SetupFilters()
    {
        // 희귀도 필터 옵션 추가
        rarityFilter.ClearOptions();
        rarityFilter.AddOptions(new List<string>
        {
            "전체", "커먼", "레어", "에픽", "레전더리"
        });
        rarityFilter.onValueChanged.AddListener(OnRarityFilterChanged);

        // 타입 필터 옵션 추가
        typeFilter.ClearOptions();
        typeFilter.AddOptions(new List<string>
        {
            "전체", "유닛", "스펠", "장비"
        });
        typeFilter.onValueChanged.AddListener(OnTypeFilterChanged);
    }

    private void SetupSorting()
    {
        sortDropdown.ClearOptions();
        sortDropdown.AddOptions(new List<string>
        {
            "이름", "마나 비용", "희귀도", "타입"
        });
        sortDropdown.onValueChanged.AddListener(OnSortChanged);
    }

    private void OnRarityFilterChanged(int index)
    {
        currentRarityFilter = (CardRarity)index;
        RefreshDisplay();
    }

    private void OnTypeFilterChanged(int index)
    {
        currentTypeFilter = (CardType)index;
        RefreshDisplay();
    }

    private void OnSearchTextChanged(string searchText)
    {
        currentSearchText = searchText;
        RefreshDisplay();
    }

    private void OnSortChanged(int index)
    {
        currentSort = (SortCriteria)index;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        ApplyFiltersAndSort();
        PopulateCardGrid();
    }

    #endregion

    #region UI Updates

    private void UpdateTotalCardsText()
    {
        int totalOwned = allCards.Sum(card =>
            CollectionManager.Instance.GetOwnedCount(card));
        int uniqueCards = allCards.Count;

        totalCardsText.text = $"총 {totalOwned}장 ({uniqueCards}종)";
    }

    #endregion
}

public enum CardRarity
{
    All = 0,
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public enum CardType
{
    All = 0,
    Unit = 1,
    Spell = 2,
    Equipment = 3
}

public enum SortCriteria
{
    Name = 0,
    ManaCost = 1,
    Rarity = 2,
    Type = 3
}
```

#### 1.2 CardUI 확장 설계 (InventoryCardSlot 대체)

**설계 원칙**: 별도의 `InventoryCardSlot` 클래스를 만들지 않고, 기존 `CardUI`를 확장하여 재사용합니다.

**파일 수정**: `Assets/Scripts/Game/Card/UI/CardUI.cs` (기존 파일 확장)

##### 1.2.1 CardUIMode Enum 추가

```csharp
/// <summary>
/// CardUI가 사용되는 컨텍스트를 구분하는 enum
/// 이벤트 구독자(Manager/Service)가 자신의 도메인 이벤트만 필터링하는 데 사용
/// </summary>
public enum CardUIMode
{
    InHand,      // 전투 중: Hand → Field 타일 드래그
    InInventory, // 인벤토리: 인벤토리 → 덱빌더 드래그
    InDeck       // 덱빌더: 덱 내 카드 재정렬
}
```

##### 1.2.2 CardDragData 구조체 (이벤트 페이로드)

```csharp
/// <summary>
/// 카드 드래그 이벤트에 전달되는 데이터
/// </summary>
public struct CardDragData
{
    public CardData cardData;           // 드래그 중인 카드 데이터
    public CardUIMode mode;             // 드래그 컨텍스트 (InHand, InInventory, InDeck)
    public Transform sourceTransform;   // 드래그 시작 위치
    public int sourceIndex;             // 원래 인덱스 (Hand 또는 Deck 내)
}
```

##### 1.2.3 CardUI 클래스 확장 코드

**기존 CardUI에 추가할 필드**
```csharp
public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ========== 신규 추가: 컨텍스트 모드 ==========
    [Header("Context Mode")]
    [SerializeField] private CardUIMode mode = CardUIMode.InHand;

    // ========== 신규 추가: 이벤트 채널 ==========
    [Header("Event Channels")]
    [SerializeField] private CardDragStartEventChannelSO cardDragStartChannel;
    [SerializeField] private CardDragEndEventChannelSO cardDragEndChannel;

    // 기존 필드들...
    [Header("UI References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI manaCostText;
    // ... 기존 코드 유지
}
```

**OnBeginDrag 수정 (이벤트 발생 추가)**
```csharp
public void OnBeginDrag(PointerEventData eventData)
{
    if (!isDraggable || cardData == null) return;

    // 기존 코드: 상태 관리, 시각적 피드백
    isDragging = true;
    originalPosition = transform.position;
    originalScale = transform.localScale;
    originalIndex = transform.GetSiblingIndex();

    if (canvasGroup != null)
        canvasGroup.alpha = dragAlpha;

    transform.localScale = originalScale * dragScale;

    if (canvasGroup != null)
        canvasGroup.blocksRaycasts = false;

    if (parentCanvas != null)
        transform.SetParent(parentCanvas.transform, true);

    SetPanelsActive(true);

    // ========== 신규 추가: 이벤트 발생 (도메인 로직 없음) ==========
    if (cardDragStartChannel != null)
    {
        var dragData = new CardDragData
        {
            cardData = this.cardData,
            mode = this.mode,
            sourceTransform = this.transform,
            sourceIndex = originalIndex
        };

        cardDragStartChannel.RaiseEvent(dragData);
    }

    Debug.Log($"[CardUI] Drag started: {cardData.CardName}, Mode: {mode}");
}
```

**OnEndDrag 수정 (이벤트 발생 추가)**
```csharp
public void OnEndDrag(PointerEventData eventData)
{
    if (!isDragging) return;

    isDragging = false;

    // 기존 코드: 드롭 처리 (mode에 따라 분기 없음!)
    gridRenderer?.ClearCardPreview();

    // ========== 신규 추가: 이벤트 발생 먼저 ==========
    if (cardDragEndChannel != null)
    {
        var dragData = new CardDragData
        {
            cardData = this.cardData,
            mode = this.mode,
            sourceTransform = this.transform,
            sourceIndex = originalIndex
        };

        // 이벤트 발생 후 각 Manager가 자신의 도메인 로직 처리
        cardDragEndChannel.RaiseEvent(dragData, eventData);
    }

    // mode == InHand일 때만 기존 HandleDrop 실행
    bool dropSuccess = false;
    if (mode == CardUIMode.InHand)
    {
        dropSuccess = HandleDrop(eventData);
    }
    // mode == InInventory일 때는 DeckBuilderService가 이벤트로 처리

    // 기존 코드: 레이캐스팅 복원
    if (canvasGroup != null)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    // 드롭 실패 시 원위치 복귀
    if (!dropSuccess && returnToOriginalPosition)
    {
        StartCoroutine(ReturnToOriginalPosition());
    }

    SetPanelsActive(false);

    Debug.Log($"[CardUI] Drag ended: {cardData.CardName}, Mode: {mode}, Success: {dropSuccess}");
}
```

##### 1.2.4 인벤토리 전용 기능 추가 (선택 사항)

인벤토리에서만 필요한 "소유 개수" 표시를 위한 추가 코드:

```csharp
public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Inventory Mode Settings")]
    [SerializeField] private TextMeshProUGUI ownedCountText; // 인벤토리 모드에서만 활성화

    private int ownedCount; // 소유 개수 (인벤토리 모드에서만 사용)

    /// <summary>
    /// 인벤토리 모드용 Setup (소유 개수 포함)
    /// </summary>
    public void SetupForInventory(CardData card, int count)
    {
        SetupCardData(card); // 기존 Setup 로직 재사용

        ownedCount = count;
        if (ownedCountText != null)
        {
            ownedCountText.gameObject.SetActive(true);
            ownedCountText.text = count.ToString();
        }

        // 소유하지 않은 카드는 회색 처리
        if (count <= 0)
        {
            if (cardImage != null)
                cardImage.color = Color.gray;
            if (canvasGroup != null)
                canvasGroup.interactable = false;
        }
    }

    /// <summary>
    /// Hand 모드용 Setup (기존 코드)
    /// </summary>
    public void SetupCardData(CardData card)
    {
        cardData = card;

        if (ownedCountText != null)
            ownedCountText.gameObject.SetActive(false); // 인벤토리 UI 숨김

        UpdateDisplay();
    }
}
```

##### 1.2.5 Prefab 구성 방법

**CardUI.prefab 구조 (모든 모드에서 재사용)**
```
CardUI (CardUI 컴포넌트)
├─ CardImageContainer (Image)
├─ CardNameText (TMP_Text)
├─ ManaCostText (TMP_Text)
├─ AttackText (TMP_Text)        // InHand 모드에서 표시
├─ HPText (TMP_Text)            // InHand 모드에서 표시
├─ OwnedCountText (TMP_Text)    // InInventory 모드에서 표시
└─ RarityBorder (Image)
```

**InventoryPanel에서 CardUI 생성 예시**
```csharp
public class InventoryPanel : UIPanel
{
    [SerializeField] private GameObject cardUIPrefab; // CardUI.prefab

    private void PopulateCardGrid()
    {
        foreach (var cardData in filteredCards)
        {
            var cardUIObj = Instantiate(cardUIPrefab, cardGridContainer);
            var cardUI = cardUIObj.GetComponent<CardUI>();

            // 인벤토리 모드로 설정
            cardUI.SetupForInventory(cardData, collectionManager.GetOwnedCount(cardData));

            // Inspector에서 mode를 InInventory로 설정
            // 또는 코드로: cardUI.SetMode(CardUIMode.InInventory);
        }
    }
}
```

##### 1.2.6 설계의 장점 요약

| 항목 | 기존 설계 (InventoryCardSlot 별도) | 새 설계 (CardUI 확장) |
|------|----------------------------------|---------------------|
| **Prefab 수** | 2개 (CardUI + InventoryCardSlot) | 1개 (CardUI만) |
| **시각적 변경 작업량** | 2배 (두 prefab 수정) | 1회 (CardUI만 수정) |
| **코드 중복** | ❌ 95% 중복 | ✅ 0% 중복 |
| **SRP 준수** | ⚠️ View와 로직 혼재 위험 | ✅ View는 이벤트만, 로직은 Manager |
| **확장성** | ❌ 새 컨텍스트 시 새 클래스 | ✅ enum 값만 추가 |
| **테스트** | 2개 컴포넌트 테스트 | 1개 컴포넌트 + 이벤트 구독자 테스트 |

---

### Phase 2: 덱 빌더 패널

#### 2.1 DeckBuilderPanel 클래스
**파일**: `Assets/Scripts/UI/Panels/DeckBuilderPanel.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

public class DeckBuilderPanel : UIPanel, IOpenablePanel, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Deck Settings")]
    [SerializeField] private Transform deckListContainer;
    [SerializeField] private GameObject deckCardSlotPrefab;
    [SerializeField] private ScrollRect deckScrollRect;

    [Header("Deck Info")]
    [SerializeField] private TMP_InputField deckNameInput;
    [SerializeField] private TextMeshProUGUI deckCountText;
    [SerializeField] private TextMeshProUGUI validationStatusText;

    [Header("Mana Curve")]
    [SerializeField] private Transform manaCurveContainer;
    [SerializeField] private GameObject manaCurveBarPrefab;
    private List<Image> manaCurveBars = new List<Image>();

    [Header("Deck Validation")]
    [SerializeField] private int minDeckSize = 30;
    [SerializeField] private int maxDeckSize = 30;
    [SerializeField] private int maxCopiesPerCard = 3;

    [Header("Visual Feedback")]
    [SerializeField] private Image dropZoneHighlight;
    [SerializeField] private Color validDropColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color invalidDropColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f);

    [Header("Buttons")]
    [SerializeField] private Button saveDeckButton;
    [SerializeField] private Button clearDeckButton;
    [SerializeField] private Button loadDeckButton;

    // 덱 데이터
    private Dictionary<CardData, int> deckCards = new Dictionary<CardData, int>();
    private List<DeckCardSlot> deckSlots = new List<DeckCardSlot>();
    private DeckValidator deckValidator;

    // 현재 드래그 중인 카드 (검증용)
    private CardData currentDraggedCard = null;

    // IOpenablePanel 구현
    public Button OpenButton { get; set; }
    public Button CloseButton { get; set; }

    // 이벤트
    public event Action<CardData> OnCardAddedToDeck;
    public event Action<CardData> OnCardRemovedFromDeck;
    public event Action<Dictionary<CardData, int>> OnDeckChanged;

    #region Lifecycle

    public override void Initialize()
    {
        base.Initialize();

        deckValidator = new DeckValidator(minDeckSize, maxDeckSize, maxCopiesPerCard);

        // 버튼 이벤트 설정
        saveDeckButton.onClick.AddListener(OnSaveDeckClicked);
        clearDeckButton.onClick.AddListener(OnClearDeckClicked);
        loadDeckButton.onClick.AddListener(OnLoadDeckClicked);

        // 덱 이름 기본값
        deckNameInput.text = "새로운 덱";

        // 마나 커브 초기화
        InitializeManaCurve();

        // 외부 이벤트 구독
        InventoryCardSlot.CardDragStartEvent += OnCardDragStart;

        UpdateDeckDisplay();
    }

    private void OnDestroy()
    {
        InventoryCardSlot.CardDragStartEvent -= OnCardDragStart;
    }

    public override void Show()
    {
        base.Show();
        UpdateDeckDisplay();
    }

    #endregion

    #region Card Management

    public bool CanAddCardToDeck(CardData card)
    {
        // 덱 크기 제한 체크
        if (GetTotalCardCount() >= maxDeckSize)
        {
            Debug.Log($"덱이 가득 찼습니다 ({maxDeckSize}장)");
            return false;
        }

        // 카드 중복 제한 체크
        if (deckCards.ContainsKey(card))
        {
            int currentCount = deckCards[card];

            if (currentCount >= card.MaxCopiesInDeck)
            {
                Debug.Log($"{card.CardName}은(는) 최대 {card.MaxCopiesInDeck}장까지만 가능합니다");
                return false;
            }

            if (currentCount >= maxCopiesPerCard)
            {
                Debug.Log($"카드당 최대 {maxCopiesPerCard}장까지만 가능합니다");
                return false;
            }
        }

        return true;
    }

    public void AddCardToDeck(CardData card)
    {
        if (!CanAddCardToDeck(card))
            return;

        if (deckCards.ContainsKey(card))
        {
            deckCards[card]++;
            UpdateExistingSlot(card);
        }
        else
        {
            deckCards[card] = 1;
            CreateNewSlot(card);
        }

        UpdateDeckDisplay();
        OnCardAddedToDeck?.Invoke(card);
        OnDeckChanged?.Invoke(deckCards);
    }

    public void RemoveCardFromDeck(CardData card)
    {
        if (!deckCards.ContainsKey(card))
            return;

        deckCards[card]--;

        if (deckCards[card] <= 0)
        {
            deckCards.Remove(card);
            RemoveSlot(card);
        }
        else
        {
            UpdateExistingSlot(card);
        }

        UpdateDeckDisplay();
        OnCardRemovedFromDeck?.Invoke(card);
        OnDeckChanged?.Invoke(deckCards);
    }

    private void CreateNewSlot(CardData card)
    {
        GameObject slotObj = Instantiate(deckCardSlotPrefab, deckListContainer);
        DeckCardSlot slot = slotObj.GetComponent<DeckCardSlot>();

        slot.Setup(card, deckCards[card], this);
        deckSlots.Add(slot);

        // 마나 비용 순으로 정렬
        SortDeckSlots();
    }

    private void UpdateExistingSlot(CardData card)
    {
        DeckCardSlot slot = deckSlots.Find(s => s.GetCardData() == card);
        if (slot != null)
        {
            slot.UpdateCount(deckCards[card]);
        }
    }

    private void RemoveSlot(CardData card)
    {
        DeckCardSlot slot = deckSlots.Find(s => s.GetCardData() == card);
        if (slot != null)
        {
            deckSlots.Remove(slot);
            Destroy(slot.gameObject);
        }
    }

    private void SortDeckSlots()
    {
        // 마나 비용 → 이름 순으로 정렬
        deckSlots = deckSlots
            .OrderBy(slot => slot.GetCardData().ManaCost)
            .ThenBy(slot => slot.GetCardData().CardName)
            .ToList();

        // Transform 순서 재정렬
        for (int i = 0; i < deckSlots.Count; i++)
        {
            deckSlots[i].transform.SetSiblingIndex(i);
        }
    }

    private int GetTotalCardCount()
    {
        return deckCards.Values.Sum();
    }

    #endregion

    #region Drag & Drop

    private void OnCardDragStart(CardData card)
    {
        currentDraggedCard = card;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentDraggedCard != null)
        {
            // 드롭 가능 여부에 따라 색상 변경
            bool canDrop = CanAddCardToDeck(currentDraggedCard);
            dropZoneHighlight.color = canDrop ? validDropColor : invalidDropColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        dropZoneHighlight.color = normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        dropZoneHighlight.color = normalColor;

        if (currentDraggedCard == null)
            return;

        if (CanAddCardToDeck(currentDraggedCard))
        {
            AddCardToDeck(currentDraggedCard);
        }
        else
        {
            // 실패 피드백 재생
            // TODO: 사운드/애니메이션
        }

        currentDraggedCard = null;
    }

    #endregion

    #region UI Updates

    private void UpdateDeckDisplay()
    {
        UpdateDeckCountText();
        UpdateManaCurve();
        UpdateValidationStatus();
        UpdateSaveButtonState();
    }

    private void UpdateDeckCountText()
    {
        int totalCards = GetTotalCardCount();
        deckCountText.text = $"{totalCards} / {maxDeckSize}";

        // 색상 변경
        if (totalCards < minDeckSize)
            deckCountText.color = Color.yellow;
        else if (totalCards > maxDeckSize)
            deckCountText.color = Color.red;
        else
            deckCountText.color = Color.green;
    }

    private void InitializeManaCurve()
    {
        // 0~9+ 마나용 막대 생성
        for (int i = 0; i <= 9; i++)
        {
            GameObject barObj = Instantiate(manaCurveBarPrefab, manaCurveContainer);
            Image barImage = barObj.GetComponent<Image>();
            manaCurveBars.Add(barImage);
        }
    }

    private void UpdateManaCurve()
    {
        // 마나별 카드 수 계산
        int[] manaCounts = new int[10]; // 0~9+

        foreach (var kvp in deckCards)
        {
            int manaCost = Mathf.Min(kvp.Key.ManaCost, 9); // 9+ 처리
            manaCounts[manaCost] += kvp.Value;
        }

        // 최대값 찾기 (정규화용)
        int maxCount = manaCounts.Max();
        if (maxCount == 0) maxCount = 1; // 0으로 나누기 방지

        // 막대 높이 업데이트
        for (int i = 0; i < manaCurveBars.Count; i++)
        {
            float normalizedHeight = (float)manaCounts[i] / maxCount;
            RectTransform rectTransform = manaCurveBars[i].rectTransform;

            // 높이 조정 (최대 100px)
            rectTransform.sizeDelta = new Vector2(
                rectTransform.sizeDelta.x,
                normalizedHeight * 100f
            );

            // 색상 (마나별)
            manaCurveBars[i].color = GetManaColor(i);
        }
    }

    private Color GetManaColor(int manaCost)
    {
        // 마나 비용에 따른 그라데이션
        float t = manaCost / 9f;
        return Color.Lerp(new Color(0.2f, 1f, 0.2f), new Color(1f, 0.2f, 0.2f), t);
    }

    private void UpdateValidationStatus()
    {
        DeckValidationResult result = deckValidator.ValidateDeck(deckCards);

        if (result.IsValid)
        {
            validationStatusText.text = "✓ 유효한 덱";
            validationStatusText.color = Color.green;
        }
        else
        {
            validationStatusText.text = "✗ " + result.Errors[0];
            validationStatusText.color = Color.red;
        }

        // 경고 표시 (선택적)
        if (result.Warnings.Count > 0)
        {
            Debug.LogWarning("덱 경고: " + string.Join(", ", result.Warnings));
        }
    }

    private void UpdateSaveButtonState()
    {
        DeckValidationResult result = deckValidator.ValidateDeck(deckCards);
        saveDeckButton.interactable = result.IsValid;
    }

    #endregion

    #region Button Handlers

    private void OnSaveDeckClicked()
    {
        DeckValidationResult result = deckValidator.ValidateDeck(deckCards);
        if (!result.IsValid)
        {
            Debug.LogWarning("유효하지 않은 덱입니다: " + string.Join(", ", result.Errors));
            return;
        }

        // DeckData ScriptableObject 생성 또는 업데이트
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>();
        deckData.SetDeckName(deckNameInput.text);

        foreach (var kvp in deckCards)
        {
            deckData.AddCard(kvp.Key, kvp.Value);
        }

        // 저장 로직 (예: Resources 폴더 또는 PlayerPrefs)
        DeckSaveSystem.SaveDeck(deckData);

        Debug.Log($"덱 '{deckNameInput.text}' 저장 완료!");
    }

    private void OnClearDeckClicked()
    {
        // 확인 다이얼로그 표시 (선택적)

        List<CardData> cardsToRemove = new List<CardData>(deckCards.Keys);

        foreach (var card in cardsToRemove)
        {
            while (deckCards.ContainsKey(card))
            {
                RemoveCardFromDeck(card);
            }
        }

        Debug.Log("덱이 초기화되었습니다.");
    }

    private void OnLoadDeckClicked()
    {
        // 덱 선택 UI 표시
        // TODO: 덱 목록 패널 열기
    }

    #endregion
}
```

#### 2.2 DeckCardSlot 클래스
**파일**: `Assets/Scripts/UI/Cards/DeckCardSlot.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class DeckCardSlot : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI manaCostText;
    [SerializeField] private TextMeshProUGUI cardCountText;
    [SerializeField] private Button removeButton;
    [SerializeField] private CanvasGroup canvasGroup;

    private CardData cardData;
    private int count;
    private DeckBuilderPanel parentPanel;

    // 드래그 상태
    private bool isDragging = false;

    // 이벤트
    public static event Action<CardData> DeckCardDragStartEvent;

    #region Setup

    public void Setup(CardData card, int cardCount, DeckBuilderPanel parent)
    {
        cardData = card;
        count = cardCount;
        parentPanel = parent;

        UpdateDisplay();

        // 제거 버튼 이벤트
        removeButton.onClick.AddListener(OnRemoveButtonClicked);
    }

    private void UpdateDisplay()
    {
        cardImage.sprite = cardData.CardArt;
        cardNameText.text = cardData.CardName;
        manaCostText.text = cardData.ManaCost.ToString();
        cardCountText.text = $"x{count}";
    }

    public void UpdateCount(int newCount)
    {
        count = newCount;
        cardCountText.text = $"x{count}";
    }

    #endregion

    #region Event Handlers

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 우클릭: 1장 제거
            RemoveOneCard();
        }
    }

    private void OnRemoveButtonClicked()
    {
        // 버튼 클릭: 1장 제거
        RemoveOneCard();
    }

    private void RemoveOneCard()
    {
        parentPanel.RemoveCardFromDeck(cardData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        canvasGroup.alpha = 0.6f;

        DeckCardDragStartEvent?.Invoke(cardData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 드래그 중 시각적 피드백만 제공
        // 실제 이동은 하지 않음 (덱 내에서는 순서 유지)
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.alpha = 1f;

        // 인벤토리 패널로 드래그한 경우 제거 처리
        // (DeckInventoryCoordinator에서 처리)
    }

    #endregion

    #region Public API

    public CardData GetCardData() => cardData;
    public int GetCount() => count;

    #endregion
}
```

---

### Phase 3: 패널 간 통신 시스템

#### 3.1 CardTransferEventChannel
**파일**: `Assets/ScriptableObjects/Events/CardTransferEventChannelSO.cs`

```csharp
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "CardTransferEventChannel",
    menuName = "Events/Card Transfer Event Channel")]
public class CardTransferEventChannelSO : ScriptableObject
{
    public event Action<CardData, TransferSource, TransferDestination> OnCardTransferred;

    public void RaiseEvent(CardData card, TransferSource source, TransferDestination destination)
    {
        OnCardTransferred?.Invoke(card, source, destination);

        Debug.Log($"[CardTransfer] {card.CardName}: {source} → {destination}");
    }
}

public enum TransferSource
{
    Inventory,
    Deck,
    Hand,
    Collection
}

public enum TransferDestination
{
    Inventory,
    Deck,
    Discard,
    Hand
}
```

#### 3.2 DeckInventoryCoordinator
**파일**: `Assets/Scripts/UI/Coordinators/DeckInventoryCoordinator.cs`

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DeckInventoryCoordinator : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private DeckBuilderPanel deckPanel;

    [Header("Event Channels")]
    [SerializeField] private CardTransferEventChannelSO transferChannel;

    [Header("Feedback")]
    [SerializeField] private CardTransferFeedback feedbackSystem;

    #region Lifecycle

    private void OnEnable()
    {
        // 인벤토리 → 덱
        InventoryCardSlot.CardDragEndEvent += OnInventoryCardDragEnd;

        // 덱 → 인벤토리
        DeckCardSlot.DeckCardDragStartEvent += OnDeckCardDragStart;

        // 덱 변경 이벤트
        deckPanel.OnCardAddedToDeck += OnCardAddedToDeck;
        deckPanel.OnCardRemovedFromDeck += OnCardRemovedFromDeck;
    }

    private void OnDisable()
    {
        InventoryCardSlot.CardDragEndEvent -= OnInventoryCardDragEnd;
        DeckCardSlot.DeckCardDragStartEvent -= OnDeckCardDragStart;
        deckPanel.OnCardAddedToDeck -= OnCardAddedToDeck;
        deckPanel.OnCardRemovedFromDeck -= OnCardRemovedFromDeck;
    }

    #endregion

    #region Event Handlers

    private void OnInventoryCardDragEnd(CardData card, PointerEventData eventData)
    {
        // 덱 패널 위에 드롭했는지 체크
        if (IsDraggedOverPanel(eventData, deckPanel))
        {
            if (deckPanel.CanAddCardToDeck(card))
            {
                deckPanel.AddCardToDeck(card);

                // 이벤트 채널로 전파
                transferChannel?.RaiseEvent(card,
                    TransferSource.Inventory,
                    TransferDestination.Deck);
            }
            else
            {
                // 실패 피드백
                feedbackSystem?.PlayInvalidActionFeedback();
            }
        }
    }

    private void OnDeckCardDragStart(CardData card)
    {
        // 덱에서 인벤토리로 드래그 시작
        // (필요시 추가 처리)
    }

    private void OnCardAddedToDeck(CardData card)
    {
        // 인벤토리 UI 업데이트
        inventoryPanel.UpdateCardAvailability(card);

        // 성공 피드백
        feedbackSystem?.PlayAddCardFeedback(deckPanel.transform.position);
    }

    private void OnCardRemovedFromDeck(CardData card)
    {
        // 인벤토리 UI 업데이트
        inventoryPanel.UpdateCardAvailability(card);

        // 제거 피드백
        feedbackSystem?.PlayRemoveCardFeedback(deckPanel.transform.position);
    }

    #endregion

    #region Utility

    private bool IsDraggedOverPanel(PointerEventData eventData, UIPanel panel)
    {
        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject.GetComponentInParent<UIPanel>() == panel)
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}
```

---

### Phase 4: 덱 검증 시스템

#### 4.1 DeckValidator
**파일**: `Assets/Scripts/Systems/DeckValidator.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeckValidator
{
    private int minDeckSize;
    private int maxDeckSize;
    private int maxCopiesPerCard;

    public DeckValidator(int min, int max, int maxCopies)
    {
        minDeckSize = min;
        maxDeckSize = max;
        maxCopiesPerCard = maxCopies;
    }

    public DeckValidationResult ValidateDeck(Dictionary<CardData, int> deckCards)
    {
        var result = new DeckValidationResult();

        // 1. 덱 크기 검증
        ValidateDeckSize(deckCards, result);

        // 2. 카드 중복 제한 검증
        ValidateCardCopies(deckCards, result);

        // 3. 마나 커브 검증
        ValidateManaCurve(deckCards, result);

        // 4. 시너지 검증 (선택적)
        ValidateSynergies(deckCards, result);

        return result;
    }

    private void ValidateDeckSize(Dictionary<CardData, int> deckCards, DeckValidationResult result)
    {
        int totalCards = deckCards.Values.Sum();

        if (totalCards < minDeckSize)
        {
            result.AddError($"덱 크기 부족: {totalCards}/{minDeckSize}");
        }
        else if (totalCards > maxDeckSize)
        {
            result.AddError($"덱 크기 초과: {totalCards}/{maxDeckSize}");
        }
    }

    private void ValidateCardCopies(Dictionary<CardData, int> deckCards, DeckValidationResult result)
    {
        foreach (var kvp in deckCards)
        {
            // 카드별 최대 장수 제한
            if (kvp.Value > kvp.Key.MaxCopiesInDeck)
            {
                result.AddError(
                    $"{kvp.Key.CardName}: 최대 {kvp.Key.MaxCopiesInDeck}장까지만 가능");
            }

            // 전역 카드당 최대 장수
            if (kvp.Value > maxCopiesPerCard)
            {
                result.AddError(
                    $"{kvp.Key.CardName}: 카드당 최대 {maxCopiesPerCard}장까지만 가능");
            }
        }
    }

    private void ValidateManaCurve(Dictionary<CardData, int> deckCards, DeckValidationResult result)
    {
        var manaCurve = new Dictionary<int, int>();

        foreach (var kvp in deckCards)
        {
            int manaCost = kvp.Key.ManaCost;
            if (!manaCurve.ContainsKey(manaCost))
                manaCurve[manaCost] = 0;
            manaCurve[manaCost] += kvp.Value;
        }

        // 저비용 카드 (0-2 마나) 체크
        int lowCostCards = 0;
        for (int i = 0; i <= 2; i++)
        {
            if (manaCurve.ContainsKey(i))
                lowCostCards += manaCurve[i];
        }

        if (lowCostCards < 8)
        {
            result.AddWarning(
                "저비용 카드(0-2 마나)가 부족합니다. 초반 대응이 어려울 수 있습니다.");
        }

        // 고비용 카드 (7+ 마나) 체크
        int highCostCards = 0;
        for (int i = 7; i <= 10; i++)
        {
            if (manaCurve.ContainsKey(i))
                highCostCards += manaCurve[i];
        }

        if (highCostCards > 5)
        {
            result.AddWarning(
                "고비용 카드(7+ 마나)가 너무 많습니다. 초반에 손이 막힐 수 있습니다.");
        }
    }

    private void ValidateSynergies(Dictionary<CardData, int> deckCards, DeckValidationResult result)
    {
        // 특정 키워드나 타입 조합 검증
        // 예: "진화" 카드가 있으면 기본 유닛도 필요

        bool hasEvolutionCard = deckCards.Keys.Any(card =>
            card.Description.Contains("진화"));

        bool hasBasicUnit = deckCards.Keys.Any(card =>
            card.CardType == CardType.Unit && !card.Description.Contains("진화"));

        if (hasEvolutionCard && !hasBasicUnit)
        {
            result.AddWarning(
                "진화 카드가 있지만 기본 유닛이 없습니다. 진화 대상이 부족할 수 있습니다.");
        }
    }
}

public class DeckValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; private set; } = new List<string>();
    public List<string> Warnings { get; private set; } = new List<string>();

    public void AddError(string error)
    {
        Errors.Add(error);
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    public void Clear()
    {
        Errors.Clear();
        Warnings.Clear();
    }
}
```

---

### Phase 5: 시각적 피드백 시스템

#### 5.1 CardTransferFeedback
**파일**: `Assets/Scripts/UI/Feedback/CardTransferFeedback.cs`

```csharp
using UnityEngine;
using System.Collections;

public class CardTransferFeedback : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem addCardParticles;
    [SerializeField] private ParticleSystem removeCardParticles;
    [SerializeField] private AnimationCurve scalePulse = AnimationCurve.EaseInOut(0, 1, 1, 1.2f);

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip cardAddSound;
    [SerializeField] private AudioClip cardRemoveSound;
    [SerializeField] private AudioClip invalidActionSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Screen Shake")]
    [SerializeField] private float shakeIntensity = 5f;
    [SerializeField] private float shakeDuration = 0.3f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlayAddCardFeedback(Vector3 worldPosition)
    {
        // 파티클 효과
        if (addCardParticles != null)
        {
            Instantiate(addCardParticles, worldPosition, Quaternion.identity);
        }

        // 사운드
        if (audioSource != null && cardAddSound != null)
        {
            audioSource.PlayOneShot(cardAddSound);
        }

        // 스케일 펄스 애니메이션
        StartCoroutine(ScalePulseAnimation(worldPosition));
    }

    public void PlayRemoveCardFeedback(Vector3 worldPosition)
    {
        // 파티클 효과
        if (removeCardParticles != null)
        {
            Instantiate(removeCardParticles, worldPosition, Quaternion.identity);
        }

        // 사운드
        if (audioSource != null && cardRemoveSound != null)
        {
            audioSource.PlayOneShot(cardRemoveSound);
        }
    }

    public void PlayInvalidActionFeedback()
    {
        // 사운드
        if (audioSource != null && invalidActionSound != null)
        {
            audioSource.PlayOneShot(invalidActionSound);
        }

        // 화면 흔들림
        StartCoroutine(ShakeScreen());
    }

    private IEnumerator ScalePulseAnimation(Vector3 position)
    {
        // UI 요소 찾기
        // (실제 구현에서는 월드 좌표를 UI 좌표로 변환)

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = scalePulse.Evaluate(t);

            // 스케일 적용 로직

            yield return null;
        }
    }

    private IEnumerator ShakeScreen()
    {
        Vector3 originalPosition = mainCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;

            mainCamera.transform.localPosition = originalPosition + new Vector3(x, y, 0);

            yield return null;
        }

        mainCamera.transform.localPosition = originalPosition;
    }
}
```

---

### Phase 6: 데이터 관리 시스템

#### 6.1 DeckData ScriptableObject
**파일**: `Assets/Scripts/Data/DeckData.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "New Deck", menuName = "Game/Deck Data")]
public class DeckData : ScriptableObject
{
    [SerializeField] private string deckName = "새로운 덱";
    [SerializeField] private List<DeckCardEntry> cards = new List<DeckCardEntry>();

    public string DeckName => deckName;
    public IReadOnlyList<DeckCardEntry> Cards => cards.AsReadOnly();

    public void SetDeckName(string name)
    {
        deckName = name;
    }

    public void AddCard(CardData card, int count = 1)
    {
        var existing = cards.Find(e => e.Card == card);
        if (existing != null)
        {
            existing.Count += count;
        }
        else
        {
            cards.Add(new DeckCardEntry(card, count));
        }
    }

    public void RemoveCard(CardData card, int count = 1)
    {
        var existing = cards.Find(e => e.Card == card);
        if (existing != null)
        {
            existing.Count -= count;
            if (existing.Count <= 0)
            {
                cards.Remove(existing);
            }
        }
    }

    public void Clear()
    {
        cards.Clear();
    }

    public int GetTotalCardCount()
    {
        return cards.Sum(e => e.Count);
    }

    public Dictionary<CardData, int> ToDictionary()
    {
        return cards.ToDictionary(e => e.Card, e => e.Count);
    }

    public void FromDictionary(Dictionary<CardData, int> deckCards)
    {
        cards.Clear();
        foreach (var kvp in deckCards)
        {
            cards.Add(new DeckCardEntry(kvp.Key, kvp.Value));
        }
    }
}

[System.Serializable]
public class DeckCardEntry
{
    [SerializeField] private CardData card;
    [SerializeField] private int count;

    public CardData Card => card;
    public int Count
    {
        get => count;
        set => count = value;
    }

    public DeckCardEntry(CardData card, int count)
    {
        this.card = card;
        this.count = count;
    }
}
```

#### 6.2 CollectionManager
**파일**: `Assets/Scripts/Managers/CollectionManager.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

    [Header("Available Cards")]
    [SerializeField] private List<CardData> allAvailableCards = new List<CardData>();

    // 플레이어가 소유한 카드 (카드 → 소유 개수)
    private Dictionary<CardData, int> ownedCards = new Dictionary<CardData, int>();

    // 이벤트
    public event Action OnCollectionChanged;

    #region Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadCollection();
    }

    #endregion

    #region Card Management

    public void AddCardToCollection(CardData card, int count = 1)
    {
        if (ownedCards.ContainsKey(card))
        {
            ownedCards[card] += count;
        }
        else
        {
            ownedCards[card] = count;
        }

        OnCollectionChanged?.Invoke();
        SaveCollection();
    }

    public void RemoveCardFromCollection(CardData card, int count = 1)
    {
        if (!ownedCards.ContainsKey(card))
            return;

        ownedCards[card] -= count;

        if (ownedCards[card] <= 0)
        {
            ownedCards.Remove(card);
        }

        OnCollectionChanged?.Invoke();
        SaveCollection();
    }

    public int GetOwnedCount(CardData card)
    {
        return ownedCards.ContainsKey(card) ? ownedCards[card] : 0;
    }

    public List<CardData> GetAllOwnedCards()
    {
        return ownedCards.Keys.ToList();
    }

    public bool HasCard(CardData card)
    {
        return ownedCards.ContainsKey(card) && ownedCards[card] > 0;
    }

    public int GetTotalCardCount()
    {
        return ownedCards.Values.Sum();
    }

    public int GetUniqueCardCount()
    {
        return ownedCards.Count;
    }

    #endregion

    #region Save/Load

    public void SaveCollection()
    {
        var saveData = new CollectionSaveData
        {
            cardIds = ownedCards.Keys.Select(c => c.name).ToList(),
            counts = ownedCards.Values.ToList()
        };

        string json = JsonUtility.ToJson(saveData, true);
        PlayerPrefs.SetString("PlayerCollection", json);
        PlayerPrefs.Save();

        Debug.Log($"컬렉션 저장 완료: {GetUniqueCardCount()}종 {GetTotalCardCount()}장");
    }

    public void LoadCollection()
    {
        string json = PlayerPrefs.GetString("PlayerCollection", "");

        if (string.IsNullOrEmpty(json))
        {
            // 처음 실행 시 기본 카드 지급
            GiveStarterCards();
            return;
        }

        var saveData = JsonUtility.FromJson<CollectionSaveData>(json);

        ownedCards.Clear();

        for (int i = 0; i < saveData.cardIds.Count; i++)
        {
            var card = allAvailableCards.Find(c => c.name == saveData.cardIds[i]);
            if (card != null)
            {
                ownedCards[card] = saveData.counts[i];
            }
        }

        OnCollectionChanged?.Invoke();

        Debug.Log($"컬렉션 로드 완료: {GetUniqueCardCount()}종 {GetTotalCardCount()}장");
    }

    private void GiveStarterCards()
    {
        // 시작 카드 지급 (예시)
        foreach (var card in allAvailableCards.Take(10))
        {
            AddCardToCollection(card, 3);
        }

        Debug.Log("스타터 카드 지급 완료");
    }

    #endregion
}

[System.Serializable]
public class CollectionSaveData
{
    public List<string> cardIds;
    public List<int> counts;
}
```

#### 6.3 DeckSaveSystem
**파일**: `Assets/Scripts/Systems/DeckSaveSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class DeckSaveSystem
{
    private static readonly string SaveDirectory = Application.persistentDataPath + "/Decks/";

    public static void SaveDeck(DeckData deckData)
    {
        // 디렉토리 생성
        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
        }

        // JSON 변환
        var saveData = new DeckSaveData
        {
            deckName = deckData.DeckName,
            cardIds = deckData.Cards.Select(e => e.Card.name).ToList(),
            counts = deckData.Cards.Select(e => e.Count).ToList()
        };

        string json = JsonUtility.ToJson(saveData, true);

        // 파일명 생성 (공백 제거)
        string fileName = deckData.DeckName.Replace(" ", "_") + ".json";
        string filePath = Path.Combine(SaveDirectory, fileName);

        // 파일 쓰기
        File.WriteAllText(filePath, json);

        Debug.Log($"덱 저장 완료: {filePath}");
    }

    public static DeckData LoadDeck(string deckName)
    {
        string fileName = deckName.Replace(" ", "_") + ".json";
        string filePath = Path.Combine(SaveDirectory, fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"덱 파일을 찾을 수 없습니다: {filePath}");
            return null;
        }

        // 파일 읽기
        string json = File.ReadAllText(filePath);
        var saveData = JsonUtility.FromJson<DeckSaveData>(json);

        // DeckData 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>();
        deckData.SetDeckName(saveData.deckName);

        // 카드 로드
        for (int i = 0; i < saveData.cardIds.Count; i++)
        {
            CardData card = Resources.Load<CardData>($"Cards/{saveData.cardIds[i]}");
            if (card != null)
            {
                deckData.AddCard(card, saveData.counts[i]);
            }
        }

        Debug.Log($"덱 로드 완료: {deckName}");
        return deckData;
    }

    public static List<string> GetSavedDeckNames()
    {
        if (!Directory.Exists(SaveDirectory))
            return new List<string>();

        var files = Directory.GetFiles(SaveDirectory, "*.json");

        return files.Select(f =>
            Path.GetFileNameWithoutExtension(f).Replace("_", " ")
        ).ToList();
    }

    public static void DeleteDeck(string deckName)
    {
        string fileName = deckName.Replace(" ", "_") + ".json";
        string filePath = Path.Combine(SaveDirectory, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"덱 삭제 완료: {deckName}");
        }
    }
}

[System.Serializable]
public class DeckSaveData
{
    public string deckName;
    public List<string> cardIds;
    public List<int> counts;
}
```

---

## 구현 우선순위

### 1단계: 기본 인프라 (1-2주)
**목표**: 패널 기본 구조와 데이터 흐름 구축

#### 작업 항목
- [ ] InventoryPanel 클래스 구현 및 기본 UI 레이아웃
  - Grid Layout Group 설정
  - 스크롤 뷰 설정
  - 필터/정렬 UI 배치
- [ ] DeckBuilderPanel 클래스 구현 및 기본 UI 레이아웃
  - Vertical Layout Group 설정
  - 마나 커브 UI 배치
  - 버튼 배치
- [ ] CardTransferEventChannelSO 생성
- [ ] DeckInventoryCoordinator 기본 구조
- [ ] CollectionManager 구현
- [ ] 테스트용 더미 카드 데이터 생성 (10-20장)

#### 검증 기준
- 두 패널이 정상적으로 열리고 닫힘
- 인벤토리에 카드가 표시됨
- CollectionManager가 카드 데이터를 로드함

---

### 2단계: 드래그 앤 드롭 (1주)
**목표**: 카드 이동 기능 구현

#### 작업 항목
- [ ] InventoryCardSlot 드래그 기능 구현
  - OnBeginDrag: 복사본 생성, 시각적 피드백
  - OnDrag: 마우스 위치 추적
  - OnEndDrag: 이벤트 발생
- [ ] DeckBuilderPanel의 IDropHandler 구현
  - OnPointerEnter/Exit: 드롭 존 하이라이트
  - OnDrop: 카드 추가 로직
- [ ] 드래그 프리뷰 시각화
  - 알파 조정
  - 스케일 조정
  - 레이어 순서 관리
- [ ] 드롭 유효성 검사 실시간 피드백
  - 색상 변경 (초록/빨강)
  - 툴팁 표시

#### 검증 기준
- 인벤토리에서 카드를 덱으로 드래그 가능
- 드롭 가능/불가능 상태가 시각적으로 구분됨
- 유효하지 않은 드롭 시 원위치로 복원됨

---

### 3단계: 덱 관리 (1주)
**목표**: 덱 편집 및 표시 기능

#### 작업 항목
- [ ] DeckCardSlot 구현
  - 카드 정보 표시 (이름, 마나, 개수)
  - 우클릭/버튼으로 제거 기능
- [ ] 덱에서 인벤토리로 드래그 백 기능
- [ ] DeckData ScriptableObject 구현
- [ ] 덱 저장/로드 기능 (DeckSaveSystem)
- [ ] 덱 정렬 기능 (마나 비용 순)
- [ ] 덱 초기화 기능

#### 검증 기준
- 덱에 추가된 카드가 정상 표시됨
- 카드 개수가 올바르게 업데이트됨
- 덱을 파일로 저장하고 다시 로드 가능

---

### 4단계: 검증 및 UI 개선 (1-2주)
**목표**: 게임 규칙 적용 및 사용자 경험 향상

#### 작업 항목
- [ ] DeckValidator 구현
  - 덱 크기 검증
  - 카드 중복 제한 검증
  - 마나 커브 검증
  - 시너지 검증
- [ ] 실시간 덱 검증 피드백
  - 검증 상태 텍스트
  - 저장 버튼 활성화/비활성화
  - 에러/경고 메시지 표시
- [ ] 마나 커브 시각화
  - 막대 그래프 구현
  - 색상 그라데이션
  - 마나별 카드 수 표시
- [ ] 필터링 기능 구현
  - 희귀도 필터
  - 타입 필터
  - 텍스트 검색
- [ ] 정렬 기능 구현
  - 이름, 마나, 희귀도, 타입별 정렬

#### 검증 기준
- 잘못된 덱 구성 시 명확한 에러 메시지 표시
- 마나 커브가 실시간으로 업데이트됨
- 필터링/정렬이 즉시 적용됨

---

### 5단계: 폴리싱 (1주)
**목표**: 게임 완성도 향상

#### 작업 항목
- [ ] 시각적 피드백 시스템 완성 (CardTransferFeedback)
  - 파티클 효과
  - 스케일 펄스 애니메이션
  - 화면 흔들림
- [ ] 사운드 효과 추가
  - 카드 추가 사운드
  - 카드 제거 사운드
  - 실패 사운드
- [ ] 애니메이션 개선
  - 패널 열림/닫힘 애니메이션
  - 카드 슬롯 생성 애니메이션
  - 호버 효과
- [ ] 성능 최적화
  - Object Pooling (카드 슬롯)
  - Lazy Loading (스크롤 뷰)
  - Raycast 최적화
- [ ] UI/UX 개선
  - 툴팁 시스템
  - 키보드 단축키
  - 확인 다이얼로그

#### 검증 기준
- 부드러운 애니메이션과 전환
- 명확한 오디오 피드백
- 500+ 카드에서도 원활한 성능

---

## 기술적 고려사항

### 이벤트 시스템 선택

#### C# Event vs UnityEvent 성능 비교

| 항목 | C# Event | UnityEvent |
|------|----------|------------|
| 실행 속도 | **2배 이상 빠름** | 느림 |
| 가비지 생성 | **최소** | 많음 |
| Inspector 연동 | 불가능 | **가능** |
| 타입 안정성 | **강함** | 약함 |
| 직렬화 | 불가능 | **가능** |

#### 권장 사용 패턴

```csharp
// ✅ 좋은 예: 코드 간 통신은 C# Event
public class InventoryCardSlot
{
    public static event Action<CardData> CardDragStartEvent; // C# Event
}

// ✅ 좋은 예: Designer가 Inspector에서 설정해야 하는 경우만 UnityEvent
public class Button
{
    public UnityEvent onClick; // UnityEvent - Inspector 설정 필요
}

// ❌ 나쁜 예: 성능에 민감한 곳에 UnityEvent 사용
public class GameManager
{
    public UnityEvent<float> OnHealthChanged; // 매 프레임 호출 - 느림!
}
```

#### 프로젝트 적용 전략
- **패널 간 통신**: C# Event + EventChannel (ScriptableObject)
- **버튼 클릭**: UnityEvent (Inspector에서 설정)
- **드래그 앤 드롭**: C# Event (성능 중요)
- **검증 결과**: C# Event (빈번한 호출)

---

### Object Pooling 전략

#### 카드 슬롯 풀링
```csharp
public class CardSlotPool : MonoBehaviour
{
    [SerializeField] private GameObject cardSlotPrefab;
    [SerializeField] private int initialPoolSize = 50;

    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Start()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obj = Instantiate(cardSlotPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            return Instantiate(cardSlotPrefab);
        }
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

---

### Lazy Loading (스크롤 뷰 최적화)

```csharp
// 보이는 영역의 카드만 렌더링
public class VirtualizedScrollView : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;

    private List<CardData> allCards;
    private List<GameObject> visibleSlots = new List<GameObject>();

    private void Update()
    {
        UpdateVisibleRange();
    }

    private void UpdateVisibleRange()
    {
        // 뷰포트 범위 계산
        Rect viewportRect = viewport.rect;

        // 보이는 카드만 활성화
        for (int i = 0; i < allCards.Count; i++)
        {
            // 카드 위치가 뷰포트 안에 있는지 체크
            bool isVisible = IsInViewport(i, viewportRect);

            if (isVisible)
            {
                // 슬롯 활성화 또는 생성
            }
            else
            {
                // 슬롯 비활성화
            }
        }
    }
}
```

---

### 메모리 관리

#### 카드 스프라이트 관리
```csharp
// Sprite Atlas 사용
[SerializeField] private SpriteAtlas cardAtlas;

public Sprite GetCardSprite(string cardId)
{
    return cardAtlas.GetSprite(cardId);
}

// Resources.Load 대신 Addressables 사용 (선택적)
public async Task<Sprite> LoadCardSpriteAsync(string cardId)
{
    var handle = Addressables.LoadAssetAsync<Sprite>($"Cards/{cardId}");
    return await handle.Task;
}
```

---

### 네이밍 컨벤션

#### 파일 구조
```
Assets/
├── Scripts/
│   ├── UI/
│   │   ├── Panels/
│   │   │   ├── InventoryPanel.cs
│   │   │   └── DeckBuilderPanel.cs
│   │   ├── Cards/
│   │   │   ├── InventoryCardSlot.cs
│   │   │   └── DeckCardSlot.cs
│   │   ├── Coordinators/
│   │   │   └── DeckInventoryCoordinator.cs
│   │   └── Feedback/
│   │       └── CardTransferFeedback.cs
│   ├── Data/
│   │   ├── CardData.cs
│   │   └── DeckData.cs
│   ├── Managers/
│   │   └── CollectionManager.cs
│   └── Systems/
│       ├── DeckValidator.cs
│       └── DeckSaveSystem.cs
├── ScriptableObjects/
│   ├── Events/
│   │   └── CardTransferEventChannelSO.cs
│   ├── Cards/
│   │   └── (개별 CardData 에셋)
│   └── Decks/
│       └── (개별 DeckData 에셋)
└── Prefabs/
    ├── UI/
    │   ├── Panels/
    │   │   ├── InventoryPanel.prefab
    │   │   └── DeckBuilderPanel.prefab
    │   └── Cards/
    │       ├── InventoryCardSlot.prefab
    │       └── DeckCardSlot.prefab
    └── VFX/
        ├── CardAddParticles.prefab
        └── CardRemoveParticles.prefab
```

---

### UI 레이아웃 가이드

#### Canvas 설정
```
Canvas
├─ Render Mode: Screen Space - Overlay
├─ Pixel Perfect: true
├─ Sort Order: 10 (게임 UI보다 위)
└─ Canvas Scaler
   ├─ UI Scale Mode: Scale With Screen Size
   ├─ Reference Resolution: 1920 x 1080
   └─ Match: 0.5 (Width와 Height 균형)
```

#### InventoryPanel 레이아웃
```
InventoryPanel (RectTransform: Anchor - Stretch, Pivot - 0.5, 0.5)
├─ Background (Image: 반투명 검은색)
├─ Header (VerticalLayoutGroup)
│  ├─ TitleText (TextMeshPro)
│  ├─ FilterPanel (HorizontalLayoutGroup)
│  │  ├─ RarityDropdown
│  │  ├─ TypeDropdown
│  │  └─ SearchInputField
│  └─ SortDropdown
├─ CardGridScrollView (ScrollRect)
│  └─ CardGridContainer (GridLayoutGroup)
│     ├─ Cell Size: 150 x 200
│     ├─ Spacing: 10 x 10
│     ├─ Constraint: Flexible
│     └─ InventoryCardSlot (Prefab) × N
└─ Footer (HorizontalLayoutGroup)
   └─ TotalCardsText
```

#### DeckBuilderPanel 레이아웃
```
DeckBuilderPanel (RectTransform: Anchor - Right, Pivot - 1, 0.5)
├─ Background (Image: 반투명 파란색)
├─ Header
│  ├─ DeckNameInputField
│  └─ ValidationStatusText
├─ DeckListScrollView (ScrollRect + IDropHandler)
│  └─ DeckListContainer (VerticalLayoutGroup)
│     ├─ Spacing: 5
│     └─ DeckCardSlot (Prefab) × N
├─ ManaCurvePanel
│  └─ ManaCurveContainer (HorizontalLayoutGroup)
│     └─ ManaCurveBar (Image) × 10
└─ Footer (HorizontalLayoutGroup)
   ├─ DeckCountText
   ├─ SaveButton
   ├─ ClearButton
   └─ LoadButton
```

---

## 다음 단계

### 즉시 구현 가능한 작업
1. **CardData 확장**: 현재 CardData에 필요한 필드 추가
   - `MaxCopiesInDeck` 속성
   - `Rarity` 속성
   - `CardType` 속성

2. **프리팹 생성**: UI 요소 프리팹 제작
   - InventoryCardSlot 프리팹
   - DeckCardSlot 프리팹
   - 패널 프리팹

3. **이벤트 채널 생성**: CardTransferEventChannelSO 에셋 생성

### 장기 개선 사항
1. **멀티플레이어 지원**: 온라인 덱 공유 기능
2. **덱 분석 도구**: 승률 통계, 메타 분석
3. **AI 추천 시스템**: 덱 빌딩 제안
4. **모바일 최적화**: 터치 입력 지원

---

## 참고 자료

### 산업 표준 UI 패턴
- [Hearthstone GDC Talk - UI Design Principles](https://www.youtube.com/watch?v=axkPXCNjOh8)
- [MTG Arena UX Case Study](https://magic.wizards.com/en/articles/archive/feature/mtg-arena-ux-design-2018-03-08)
- [Unity UI Best Practices](https://docs.unity3d.com/Manual/UIBestPractices.html)

### Unity 드래그 앤 드롭 참고
- [Unity Manual - Drag and Drop](https://docs.unity3d.com/Manual/script-DragAndDrop.html)
- [IBeginDragHandler Interface](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.IBeginDragHandler.html)

### 성능 최적화
- [Unity Performance Optimization](https://docs.unity3d.com/Manual/MobileOptimizationPracticalGuide.html)
- [Object Pooling Pattern](https://github.com/Unity-Technologies/ObjectPoolingPattern)

---

## 버전 히스토리

| 버전 | 날짜 | 변경사항 |
|------|------|----------|
| 1.0 | 2025-01-XX | 초기 설계 문서 작성 |

---

**작성자**: Claude
**문서 유형**: 기술 설계 문서
**상태**: 초안
**최종 수정**: 2025-01-XX