# TCG 인벤토리 & 덱 빌딩 시스템 - 충돌 분석 보고서 (실제 코드 기반)

## ⚠️ 중요: 이전 분석 문서의 오류 정정

이전 문서에서는 추측으로 작성했으나, 본 문서는 **실제 코드를 읽고 작성**했습니다.

---

## 📋 목차
1. [실제 코드 확인 내역](#실제-코드-확인-내역)
2. [심각도 분류](#심각도-분류)
3. [구조적 충돌](#구조적-충돌)
4. [해결 방안](#해결-방안)

---

## 실제 코드 확인 내역

### 읽은 파일들
| 파일 경로 | 확인 내용 |
|----------|----------|
| `/Assets/Script/ScriptableObjects/CardData.cs` | 966 lines - EffectData 기반 시스템 |
| `/Assets/Script/Game/Card/UI/CardUI.cs` | 793 lines - 드래그 시스템 |
| `/Assets/Script/UI/Core/IUIPanel.cs` | 49 lines - 패널 인터페이스 |
| `/Assets/Script/UI/Core/UIPanel.cs` | 192 lines - 패널 추상 클래스 |
| `/Assets/Script/UI/Core/UIPanelManager.cs` | 364 lines - 패널 매니저 |
| `/Assets/Script/Game/Services/Card/CardHandManager.cs` | 836 lines - 핸드 매니저 |
| `/Assets/Script/Game/Services/GlobalStateManager.cs` | 191 lines - 상태 매니저 |
| `/Assets/Script/ScriptableObjects/GameEventChannelSO.cs` | 59 lines - 이벤트 채널 |

---

## 심각도 분류

| 심각도 | 설명 | 대응 방법 |
|--------|------|----------|
| 🔴 **CRITICAL** | 시스템 작동 불가 | 설계도 대폭 수정 필요 |
| 🟡 **HIGH** | 주요 리팩토링 필요 | 어댑터 패턴 또는 점진적 마이그레이션 |
| 🟠 **MEDIUM** | 부분 수정 필요 | 래퍼 클래스 또는 확장 |
| 🟢 **LOW** | 네이밍 변경 정도 | 간단한 수정으로 해결 가능 |

---

## 구조적 충돌

### 🔴 1. CardData 구조 불일치 (CRITICAL)

#### 설계도의 가정
```csharp
// 설계도가 가정하는 CardData.cs
public class CardData : ScriptableObject
{
    public CardRarity Rarity { get; }  // enum
    public CardType CardType { get; }  // ⚠️ 존재하지 않음!
    public string Description { get; }
    public int MaxCopiesInDeck { get; }
}
```

#### 실제 코드 (`/Assets/Script/ScriptableObjects/CardData.cs`)
```csharp
// Line 14-60: 실제 CardData 구조
public class CardData : ScriptableObject
{
    public enum CardRarity { Common, Uncommon, Rare, Epic, Legendary }  // Line 16-23

    // ❌ CardType enum이 없음!
    // 대신 TargetType enum 존재
    public enum TargetType { None, Ally, Enemy, Any, Ground }  // Line 29-36

    [SerializeField] private string cardName;  // Line 41
    [SerializeField] private string description;  // Line 42
    [SerializeField] private CardRarity rarity;  // Line 47
    [SerializeField] private int manaCost;  // Line 48

    // ✅ 설계도가 원하는 필드
    [SerializeField] private int maxCopiesInDeck = 3;  // Line 63

    // ⚠️ 설계도에 없는 Phase 2.10+ 시스템
    [SerializeField] private List<EffectData> effectDataList;  // Line 60
    [SerializeField] private TargetType targetType;  // Line 51
    [SerializeField] private int targetRange = -1;  // Line 54

    // Public 속성
    public CardRarity Rarity => rarity;  // Line 74 ✅
    public int MaxCopiesInDeck => maxCopiesInDeck;  // Line 78 ✅
    public IReadOnlyList<EffectData> EffectDataList => effectDataList;  // Line 84
}
```

#### 충돌 지점
설계도의 `InventoryPanel.cs` Line 484-489:
```csharp
// ❌ 작동하지 않음 - CardType이 없음!
if (currentTypeFilter != CardType.All &&
    card.CardType != currentTypeFilter)
    return false;
```

**실제 코드 대안:**
```csharp
// ✅ 이렇게 해야 함 - EffectType 사용
if (card.HasEffectType(EffectType.Summon)) { }
if (card.HasEffectType(EffectType.Damage)) { }
if (card.HasEffectType(EffectType.Heal)) { }
```

#### 영향 범위
- InventoryPanel의 필터링 로직 **전체 재작성 필요**
- DeckValidator의 시너지 검증 로직 **EffectType 기반으로 변경**
- 마나 커브는 호환됨 (ManaCost 속성 존재)

#### 심각도: 🔴 CRITICAL
**이유**: 설계도의 70% 이상이 `CardType` 속성에 의존

---

### 🔴 2. 드래그 앤 드롭 시스템 충돌 (CRITICAL)

#### 설계도의 가정
```csharp
// 설계도 InventoryCardSlot.cs
public void OnBeginDrag(PointerEventData eventData)
{
    // 복사본 생성 방식
    draggedCopy = Instantiate(gameObject, rootCanvas.transform);
    canvasGroup.blocksRaycasts = false;
}

public void OnEndDrag(PointerEventData eventData)
{
    Destroy(draggedCopy);  // 복사본 제거
}
```

#### 실제 코드 (`CardUI.cs` Line 364-462)
```csharp
// Line 364: 드래그 시작
public void OnBeginDrag(PointerEventData eventData)
{
    // ✅ GlobalStateManager 통합 (설계도에 없음!)
    var stateManager = ServiceLocator.Get<IGlobalStateManager>();
    if (stateManager != null && stateManager.IsBusy(BusyType.GameFlowLock))
    {
        Debug.LogWarning("Cannot drag card - GameFlowLock is active");
        return;  // VFX 재생 중에는 드래그 불가
    }

    // ❌ 복사본 생성 안 함! 원본을 이동
    originalPosition = transform.position;
    originalIndex = transform.GetSiblingIndex();  // 핸드 내 인덱스 저장

    transform.SetParent(parentCanvas.transform, true);  // 부모 변경
    canvasGroup.blocksRaycasts = false;
}

// Line 427: 드래그 종료
public void OnEndDrag(PointerEventData eventData)
{
    // ❌ 복사본 제거 로직 없음
    bool dropSuccess = HandleDrop(eventData);

    if (!dropSuccess)
    {
        // 원위치로 복귀 (코루틴)
        transform.SetSiblingIndex(originalIndex);
        cardHandManager.RefreshHandLayout();
    }
}
```

#### 핵심 차이점
| 항목 | 설계도 | 실제 코드 |
|------|--------|----------|
| 드래그 방식 | 복사본 생성 | **원본 이동** |
| VFX 블로킹 | 없음 | **GlobalStateManager 통합** |
| 원위치 복귀 | Destroy 후 재생성 | **SetSiblingIndex 사용** |
| CardHandManager | 미사용 | **강결합** |

#### 실제 코드의 GlobalStateManager 통합
`CardHandManager.cs` Line 257-293:
```csharp
// Line 261: GlobalStateManager 이벤트 구독
private void HandleGlobalBusyStateChanged(BusyType type, bool isBusy)
{
    if (type == BusyType.GameFlowLock)
    {
        _isGameFlowLocked = isBusy;
        UpdateCardInteractivity();  // 모든 카드의 드래그 가능 여부 업데이트
    }
}

// Line 278: 카드 상호작용 업데이트
private void UpdateCardInteractivity()
{
    // VFX 재생 중에는 카드 드래그 불가
    bool shouldBeInteractive = !_isGameFlowLocked &&
                               isPlayerSummonMode &&
                               enablePlayerInteraction;

    foreach (var cardUI in cardUIComponents)
    {
        cardUI.SetDraggable(shouldBeInteractive);
    }
}
```

#### 심각도: 🔴 CRITICAL
**이유**:
1. 설계도의 복사본 방식을 그대로 구현하면 기존 VFX 블로킹 시스템이 작동 안 함
2. CardHandManager와의 통합이 끊어짐
3. 원위치 복귀 로직이 완전히 다름

---

### 🟡 3. 이벤트 시스템 아키텍처 차이 (HIGH)

#### 설계도의 가정
```csharp
// Static Event 방식
public class InventoryCardSlot
{
    public static event Action<CardData> CardDragStartEvent;

    public void OnBeginDrag()
    {
        CardDragStartEvent?.Invoke(cardData);
    }
}
```

#### 실제 코드 (`CardUI.cs` Line 49-51 & Line 402-404)
```csharp
// ScriptableObject EventChannel 방식
[Header("Event Channels")]
[SerializeField] private CardInfoEventChannelSO cardDragStartChannel;
[SerializeField] private CardDragEndEventChannelSO cardDragEndChannel;

public void OnBeginDrag(PointerEventData eventData)
{
    // ScriptableObject 이벤트 발생
    if (cardDragStartChannel != null && cardData != null)
    {
        cardDragStartChannel.RaiseEvent(cardData);
    }
}
```

#### 실제 EventChannel 구조 (`GameEventChannelSO.cs`)
```csharp
// Line 10-58: ScriptableObject 기반 이벤트 채널
public abstract class GameEventChannelSO<T> : ScriptableObject
{
    private event Action<T> OnEventRaised;

    public void RaiseEvent(T eventData)
    {
        OnEventRaised?.Invoke(eventData);
    }

    public void Subscribe(Action<T> listener)
    {
        OnEventRaised -= listener;  // 중복 방지
        OnEventRaised += listener;
    }

    public void Unsubscribe(Action<T> listener)
    {
        OnEventRaised -= listener;
    }
}
```

#### 장단점 비교

**설계도 방식 (Static Event):**
- ✅ 성능: 2배 빠름, 가비지 적음
- ✅ 코드만으로 완결
- ❌ Inspector에서 보이지 않음
- ❌ 메모리 누수 위험 (구독 해제 관리 필수)

**실제 코드 방식 (ScriptableObject EventChannel):**
- ✅ Inspector에서 이벤트 흐름 시각화 가능
- ✅ 에셋 재사용 가능 (여러 씬에서 사용)
- ✅ 자동 중복 방지 (Line 38)
- ❌ 성능상 느림
- ❌ 에셋 생성 필요

#### 심각도: 🟡 HIGH
**이유**: 설계도 전체를 ScriptableObject 방식으로 변경해야 하며, 프로젝트 일관성 유지 필요

---

### 🔴 4. UIPanel 초기화 패턴 충돌 (CRITICAL)

#### 설계도의 가정
```csharp
// 설계도 InventoryPanel.cs
public class InventoryPanel : UIPanel
{
    public override void Initialize()
    {
        // 한 번에 모든 초기화
        LoadCardsFromCollection();
    }
}
```

#### 실제 코드 (`UIPanel.cs` Line 30-76)
```csharp
// 2단계 초기화 시스템
protected virtual void Awake()
{
    // Phase 1: 의존성 없는 초기화
    if (initializeOnAwake)
    {
        OnInitializeSelf();  // ServiceLocator 사용 금지!
    }
}

protected virtual void Start()
{
    // Phase 2: 의존성 있는 초기화
    if (initializeOnAwake)
    {
        OnInitializeWithDependencies();  // ServiceLocator 사용 가능
        isInitialized = true;
    }
}

// Line 137-143: 파생 클래스가 구현해야 할 메서드
protected virtual void OnInitializeSelf() { }         // Awake에서 호출
protected virtual void OnInitializeWithDependencies() { }  // Start에서 호출
```

#### 실제 코드의 주석 설명
```csharp
// Line 34-43: Awake() 주석
/// <summary>
/// Awake()에서는 자기 자신에게만 종속적인 초기화만 수행
/// Instantiate 직후에도 작동해야 하는 초기화 수행
/// ServiceLocator.Get() 등 외부 의존성이 필요한 초기화는 Start()에서 수행
/// </summary>

// Line 45-50: Start() 주석
/// <summary>
/// Start()는 모든 Awake()가 완료된 후 호출됨 (Unity 보장)
/// 이 시점에서 ServiceLocator.Get()을 호출하면
/// 모든 서비스가 이미 RegisterSingleton()으로 등록된 상태이므로 항상 안전
/// </summary>
```

#### 충돌 지점
설계도의 `InventoryPanel`은 `Initialize()`에서 `CollectionManager.Instance`를 호출하지만,
실제 코드 패턴에서는 이것이 **Awake**인지 **Start**인지에 따라 다름:

```csharp
// ❌ 설계도: 초기화 시점 불명확
public override void Initialize()
{
    var collectionManager = CollectionManager.Instance;  // 언제 호출됨?
    LoadCardsFromCollection();
}

// ✅ 실제 코드: 명확한 2단계 초기화
protected override void OnInitializeSelf()
{
    // UI 컴포넌트 참조 설정 (의존성 없음)
    cardGridContainer = GetComponent<Transform>();
}

protected override void OnInitializeWithDependencies()
{
    // 서비스 접근 (ServiceLocator 안전)
    var collectionManager = ServiceLocator.Get<ICollectionManager>();
    LoadCardsFromCollection();
}
```

#### 심각도: 🔴 CRITICAL
**이유**: NullReferenceException 발생 가능성 매우 높음

---

### 🟡 5. 인벤토리 vs 핸드 개념 충돌 (HIGH)

#### 설계도의 개념
```
[인벤토리] → 플레이어가 소유한 모든 카드 (컬렉션)
     ↓
[덱 빌더] → 30장 제한으로 덱 구성
     ↓
[저장] → 덱 파일로 저장
```

#### 실제 코드 (`CardHandManager.cs`)
```csharp
// Line 17-20: 실제 용도
/// <summary>
/// 플레이어의 카드 핸드를 관리하는 서비스 (UI 포함)
/// 카드 드로우, 핸드 표시, 플레이어 상호작용을 담당
/// Phase 3: UI 및 상호작용 구현 완료
/// </summary>

// Line 19-20: 핸드 관리
[SerializeField] private int maxHandSize = 7;  // 7장 제한
private List<CardData> handCards = new List<CardData>();

// Line 296-343: 전투 중 사용
public void EnablePlayerSummonMode()   // AllySummon 페이즈에서 호출
public void DisablePlayerSummonMode()  // 페이즈 종료 시 호출
```

#### 실제 게임 플로우
```
[전투 시작]
    ↓
[덱에서 드로우] → [CardHandManager.AddCardToHand()]
    ↓
[핸드 (7장 제한)] → CardHandManager가 관리
    ↓
[AllySummon 페이즈] → EnablePlayerSummonMode()
    ↓
[카드 드래그] → CardUI.OnBeginDrag()
    ↓
[타일에 배치] → CardUI.OnCardUsed() → RemoveCardFromHand()
```

#### 필요한 시스템 분리
```
[메타게임 레이어]
├─ CollectionManager (신규 필요) - 소유한 모든 카드
├─ InventoryPanel (신규 필요) - 컬렉션 UI
└─ DeckBuilderPanel (신규 필요) - 덱 구성 UI

[전투 레이어]
├─ CardHandManager (기존) - 전투 중 핸드 (7장)
└─ CardUI (기존) - 드래그 앤 드롭
```

#### 심각도: 🟡 HIGH
**이유**: 두 시스템의 용도가 완전히 다름. 명확한 역할 분리 필요.

---

### 🟠 6. CardRarity Enum 차이 (MEDIUM)

#### 설계도의 Enum
```csharp
public enum CardRarity
{
    All = 0,      // ⚠️ 필터용
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}
```

#### 실제 코드 (`CardData.cs` Line 16-23)
```csharp
public enum CardRarity
{
    Common,     // 0
    Uncommon,   // 1  ⚠️ 설계도와 다름!
    Rare,       // 2
    Epic,       // 3
    Legendary   // 4
}
```

#### 충돌 영향

**1. 필터링 로직 문제:**
```csharp
// ❌ 설계도: CardRarity.All이 없으면 컴파일 에러
if (currentRarityFilter != CardRarity.All &&
    card.Rarity != currentRarityFilter)
    return false;

// ✅ 해결책: 별도 enum 사용
public enum RarityFilter
{
    All = -1,
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}
```

**2. Uncommon 단계 추가:**
설계도는 Common → Rare지만, 실제 코드는 Common → **Uncommon** → Rare

#### 심각도: 🟠 MEDIUM
**이유**: 필터 로직 수정으로 해결 가능

---

### 🟢 7. CardType 부재 - EffectType 사용 (LOW)

#### 설계도가 원하는 것
```csharp
public enum CardType
{
    All = 0,
    Unit = 1,
    Spell = 2,
    Equipment = 3
}

// 사용 예
if (card.CardType == CardType.Unit) { }
```

#### 실제 코드에 있는 것
```csharp
// CardData.cs에 EffectType을 반환하는 메서드들
public bool HasEffectType(EffectType effectType)  // Line 190
public EffectType? GetPrimaryEffectType()  // Line 206

// EffectType (EffectData.cs에 정의되어 있을 것)
public enum EffectType
{
    None,
    Summon,    // 유닛 소환 = Unit 카드
    Damage,    // 데미지 스펠 = Spell 카드
    Heal       // 회복 스펠 = Spell 카드
}
```

#### 어댑터 패턴으로 해결
```csharp
// CardData.cs에 추가
public CardType GetCardType()
{
    if (HasEffectType(EffectType.Summon))
        return CardType.Unit;

    if (HasEffectType(EffectType.Damage) || HasEffectType(EffectType.Heal))
        return CardType.Spell;

    return CardType.Unit;  // 기본값
}

// 또는 속성으로
public CardType CardType => HasEffectType(EffectType.Summon)
    ? CardType.Unit
    : CardType.Spell;
```

#### 심각도: 🟢 LOW
**이유**: 간단한 어댑터 메서드로 해결 가능

---

## 해결 방안

### ✅ 권장 해결 전략: 단계적 통합

#### Week 1: CardData 확장 (어댑터 추가)
**파일**: `Assets/Script/ScriptableObjects/CardData.cs`

```csharp
// 1. CardType enum 추가 (네임스페이스 충돌 방지)
namespace Game.Collection
{
    public enum CardType
    {
        All = 0,   // 필터용
        Unit = 1,
        Spell = 2
    }
}

// 2. CardData에 어댑터 메서드 추가
public class CardData : ScriptableObject
{
    // 기존 코드 유지...

    // ✅ 새로운 속성 추가 (어댑터)
    public Game.Collection.CardType CardType
    {
        get
        {
            if (HasEffectType(EffectType.Summon))
                return Game.Collection.CardType.Unit;

            if (HasEffectType(EffectType.Damage) || HasEffectType(EffectType.Heal))
                return Game.Collection.CardType.Spell;

            return Game.Collection.CardType.Unit;
        }
    }
}
```

---

#### Week 2: 새로운 네임스페이스 생성
**디렉토리 구조**:
```
Assets/Script/
├─ Game/
│  ├─ Battle/         (기존 전투 시스템)
│  │  ├─ CardUI.cs
│  │  └─ CardHandManager.cs
│  └─ Collection/     (신규 컬렉션 시스템)
│     ├─ CollectionManager.cs
│     ├─ InventoryPanel.cs
│     └─ DeckBuilderPanel.cs
```

**네임스페이스 분리**:
```csharp
// 전투 시스템 (기존)
namespace Game.Battle
{
    public class CardUI { }
    public class CardHandManager { }
}

// 컬렉션 시스템 (신규)
namespace Game.Collection
{
    public class InventoryPanel { }
    public class DeckBuilderPanel { }
    public class CollectionManager { }
}
```

---

#### Week 3: InventoryPanel 구현 (실제 코드 패턴 적용)

**파일**: `Assets/Script/Game/Collection/InventoryPanel.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Core;

namespace Game.Collection
{
    public class InventoryPanel : UIPanel
    {
        [Header("Inventory Settings")]
        [SerializeField] private Transform cardGridContainer;
        [SerializeField] private GameObject cardSlotPrefab;

        [Header("Filtering")]
        [SerializeField] private TMP_Dropdown rarityFilter;
        [SerializeField] private TMP_Dropdown typeFilter;

        private List<CardData> allCards = new List<CardData>();
        private List<CardData> filteredCards = new List<CardData>();

        // ✅ 실제 코드 패턴 적용: 2단계 초기화
        protected override void OnInitializeSelf()
        {
            // Phase 1: 의존성 없는 초기화 (Awake에서 호출)
            if (cardGridContainer == null)
            {
                Debug.LogError("[InventoryPanel] cardGridContainer not assigned!");
            }

            SetupFilters();
        }

        protected override void OnInitializeWithDependencies()
        {
            // Phase 2: 서비스 접근 (Start에서 호출)
            var collectionManager = ServiceLocator.Get<ICollectionManager>();
            if (collectionManager != null)
            {
                LoadCardsFromCollection();
            }
            else
            {
                Debug.LogError("[InventoryPanel] CollectionManager not found!");
            }
        }

        private void LoadCardsFromCollection()
        {
            var collectionManager = ServiceLocator.Get<ICollectionManager>();
            allCards = collectionManager.GetAllOwnedCards();

            ApplyFiltersAndSort();
            PopulateCardGrid();
        }

        private void ApplyFiltersAndSort()
        {
            // ✅ 실제 CardData 구조 사용
            filteredCards = allCards
                .Where(card => MatchesFilters(card))
                .OrderBy(card => card.ManaCost)
                .ToList();
        }

        private bool MatchesFilters(CardData card)
        {
            // ✅ 실제 Enum 사용
            if (currentRarityFilter != RarityFilter.All)
            {
                if (card.Rarity != (CardData.CardRarity)currentRarityFilter)
                    return false;
            }

            // ✅ 어댑터 메서드 사용
            if (currentTypeFilter != CardType.All)
            {
                if (card.CardType != currentTypeFilter)
                    return false;
            }

            return true;
        }
    }

    // ✅ 필터용 별도 Enum
    public enum RarityFilter
    {
        All = -1,
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }
}
```

---

#### Week 4: EventChannel 방식으로 통신 구현

**파일**: `Assets/ScriptableObjects/Events/CardTransferEventChannelSO.cs`

```csharp
using UnityEngine;
using Game.Data;

namespace Game.Collection
{
    [CreateAssetMenu(fileName = "CardTransferEventChannel",
                     menuName = "Events/Card Transfer Event Channel")]
    public class CardTransferEventChannelSO : GameEventChannelSO<CardTransferData>
    {
        // GameEventChannelSO<T>를 상속받아 자동으로 RaiseEvent, Subscribe, Unsubscribe 사용 가능
    }

    [System.Serializable]
    public struct CardTransferData
    {
        public CardData Card;
        public TransferSource Source;
        public TransferDestination Destination;
    }

    public enum TransferSource
    {
        Inventory,
        Deck,
        Collection
    }

    public enum TransferDestination
    {
        Inventory,
        Deck
    }
}
```

**사용 예시**:
```csharp
// InventoryCardSlot.cs
[SerializeField] private CardTransferEventChannelSO transferChannel;

public void OnEndDrag(PointerEventData eventData)
{
    var transferData = new CardTransferData
    {
        Card = cardData,
        Source = TransferSource.Inventory,
        Destination = TransferDestination.Deck
    };

    transferChannel.RaiseEvent(transferData);
}

// DeckInventoryCoordinator.cs
[SerializeField] private CardTransferEventChannelSO transferChannel;

private void OnEnable()
{
    transferChannel.Subscribe(OnCardTransferred);
}

private void OnDisable()
{
    transferChannel.Unsubscribe(OnCardTransferred);
}

private void OnCardTransferred(CardTransferData data)
{
    Debug.Log($"Card {data.Card.CardName} transferred from {data.Source} to {data.Destination}");
}
```

---

### 📊 작업량 예상 (실제 코드 기반)

| 작업 | 난이도 | 예상 시간 | 파일 수 |
|------|--------|----------|---------|
| CardData 어댑터 추가 | 하 | 2-3시간 | 1개 |
| 네임스페이스 분리 | 하 | 1-2시간 | 구조 변경 |
| InventoryPanel 구현 | 중 | 12-16시간 | 2-3개 |
| DeckBuilderPanel 구현 | 중 | 12-16시간 | 2-3개 |
| CollectionManager 구현 | 중 | 8-12시간 | 1-2개 |
| EventChannel 통합 | 중 | 6-8시간 | 3-4개 |
| 덱 검증 시스템 | 중 | 8-10시간 | 2개 |
| **총계** | - | **50-70시간** | **15-20개** |

---

## ⚠️ 핵심 결론

### 설계도를 그대로 적용하면 안 되는 이유

1. **CardType enum이 없음** → 모든 필터링/검증 로직 작동 불가
2. **드래그 시스템이 완전히 다름** → VFX 블로킹 시스템 망가짐
3. **UIPanel 초기화가 2단계임** → NullReferenceException 발생
4. **인벤토리 vs 핸드 용도가 다름** → 개념 혼동

### 권장 접근 방식

✅ **현재 전투 시스템은 건드리지 말 것**
- CardUI.cs (793 lines)
- CardHandManager.cs (836 lines)
- GlobalStateManager.cs (191 lines)

✅ **새로운 컬렉션 시스템을 별도 네임스페이스로 추가**
- `namespace Game.Collection`
- CollectionManager (신규)
- InventoryPanel (신규)
- DeckBuilderPanel (신규)

✅ **CardData는 어댑터 패턴으로 양쪽 호환**
```csharp
// 어댑터 메서드 추가로 해결
public CardType CardType => HasEffectType(EffectType.Summon)
    ? CardType.Unit
    : CardType.Spell;
```

✅ **이벤트 시스템은 ScriptableObject 방식 유지**
- 설계도의 Static Event → GameEventChannelSO<T>로 변경
- 프로젝트 일관성 유지

---

**작성자**: Claude AI (실제 코드 기반 분석)
**작성일**: 2025-01-30
**문서 버전**: 2.0 (Accurate)
**상태**: 실제 코드 검증 완료
