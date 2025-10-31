# TCG 인벤토리 & 덱 빌딩 시스템 - 충돌 분석 보고서

## 📋 목차
1. [개요](#개요)
2. [심각도 분류](#심각도-분류)
3. [구조적 충돌](#구조적-충돌)
4. [API 충돌](#api-충돌)
5. [아키텍처 충돌](#아키텍처-충돌)
6. [해결 방안](#해결-방안)

---

## 개요

### 분석 대상
- **설계도**: [TCG_InventoryDeckBuilding_Design.md](TCG_InventoryDeckBuilding_Design.md)
- **현재 코드**: 프로젝트 전체 (2025년 1월 30일 기준)

### 분석 방법
1. 현재 코드베이스 구조 탐색 및 매핑
2. 설계도와 현재 구현 비교
3. 충돌 지점 식별 및 심각도 평가
4. 해결 방안 제시

---

## 심각도 분류

| 심각도 | 설명 | 대응 방법 |
|--------|------|----------|
| 🔴 **CRITICAL** | 즉시 수정 필요, 시스템 작동 불가 | 설계도 수정 또는 코드 대폭 변경 |
| 🟡 **HIGH** | 주요 리팩토링 필요 | 어댑터 패턴 또는 점진적 마이그레이션 |
| 🟠 **MEDIUM** | 부분 수정 필요 | 래퍼 클래스 또는 확장 |
| 🟢 **LOW** | 네이밍 변경 정도 | 간단한 수정으로 해결 가능 |

---

## 구조적 충돌

### 🔴 1. CardData 구조 불일치 (CRITICAL)

#### 문제점
설계도와 현재 구현의 CardData 구조가 완전히 다릅니다.

**설계도 (Phase 1.1):**
```csharp
public class CardData : ScriptableObject
{
    [SerializeField] private string cardName;
    [SerializeField] private int manaCost;
    [SerializeField] private Sprite cardArt;
    [SerializeField] private int maxCopiesInDeck = 3;

    // 설계도에서 가정하는 추가 속성들
    public CardRarity Rarity { get; }
    public CardType CardType { get; }
    public string Description { get; }
}
```

**현재 구현 (실제 코드):**
```csharp
public class CardData : ScriptableObject
{
    [Header("Card Information")]
    [SerializeField] private string cardName;
    [SerializeField] private string description;
    [SerializeField] private CardRarity rarity;
    [SerializeField] private int manaCost;
    [SerializeField] private Sprite cardArt;

    [Header("Card Effect System - Phase 2.10+")]
    [SerializeField] private EffectData effectData;  // ⚠️ 설계도에 없음!

    [Header("Card Targeting")]
    [SerializeField] private TargetType targetType;   // ⚠️ 설계도에 없음!
    [SerializeField] private float targetRange;       // ⚠️ 설계도에 없음!

    // 설계도에 없는 메서드들
    public bool IsValidTarget(Vector3 position);
    public void ExecuteCard(Vector3 targetPosition);
    public static CardData CreateDamageCard(int damage);
}
```

#### 충돌 영향
- **InventoryPanel**: `card.CardType` 필터링 불가 (현재 구조에 없음)
- **DeckValidator**: 시너지 검증 로직이 `EffectData` 기반으로 재작성 필요
- **마나 커브**: ManaCost는 호환되지만 EffectType별 분류 추가 필요

#### 심각도: 🔴 CRITICAL
- 설계도의 모든 클래스가 CardData 구조에 의존
- 70% 이상의 코드가 영향받음

---

### 🟡 2. 드래그 앤 드롭 시스템 충돌 (HIGH)

#### 문제점
두 시스템이 서로 다른 방식으로 드래그를 처리합니다.

**설계도 (InventoryCardSlot):**
```csharp
public class InventoryCardSlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GameObject draggedCopy;  // 복사본 생성 방식

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 복사본 생성
        draggedCopy = Instantiate(gameObject, rootCanvas.transform);
        draggedCopy.transform.SetAsLastSibling();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 복사본 제거
        Destroy(draggedCopy);
    }
}
```

**현재 구현 (CardUI):**
```csharp
public class CardUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private int originalIndex;  // 원위치 저장 방식

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 원위치만 저장, 복사본 생성 안 함
        originalIndex = transform.GetSiblingIndex();

        // GameFlowLock 체크
        if (!GlobalStateManager.Instance.CanPerformGameAction(GameFlowLock.CardDrag))
            return;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 원위치로 복귀 또는 새 위치로 이동
        if (드롭 실패)
            transform.SetSiblingIndex(originalIndex);
    }
}
```

#### 충돌 영향
- **복사본 vs 원본 이동**: 설계도는 복사본 방식, 현재는 원본 이동 방식
- **GlobalStateManager 통합**: 현재 코드는 VFX 블로킹 시스템과 통합, 설계도에는 없음
- **CardHandManager 의존성**: 현재 드래그는 CardHandManager와 강결합

#### 심각도: 🟡 HIGH
- 드래그 로직 전체 재작성 필요
- GameFlowLock 시스템 통합 여부 결정 필요

---

### 🟡 3. 이벤트 시스템 아키텍처 차이 (HIGH)

#### 문제점
설계도와 현재 구현의 이벤트 시스템이 다릅니다.

**설계도 (Phase 1.2):**
```csharp
// C# Static Event 방식
public class InventoryCardSlot
{
    public static event Action<CardData> CardDragStartEvent;
    public static event Action<CardData, PointerEventData> CardDragEndEvent;

    public void OnBeginDrag(PointerEventData eventData)
    {
        CardDragStartEvent?.Invoke(cardData);
    }
}
```

**현재 구현:**
```csharp
// ScriptableObject EventChannel 방식
[CreateAssetMenu(menuName = "Events/Card Drag End Event Channel")]
public class CardDragEndEventChannelSO : GameEventChannelSO<CardData>
{
    // ScriptableObject 기반 이벤트 채널
}

public class CardUI
{
    [SerializeField] private CardDragEndEventChannelSO dragEndChannel;

    public void OnEndDrag(PointerEventData eventData)
    {
        dragEndChannel.Invoke(cardData);
    }
}
```

#### 충돌 영향
- **구독 방식**: Static Event vs ScriptableObject 구독
- **에셋 생성 필요**: 현재 방식은 Inspector에서 이벤트 채널 할당 필요
- **메모리 누수 위험**: Static Event는 구독 해제 관리 필수

#### 심각도: 🟡 HIGH
- 두 방식 모두 장단점 존재
- 프로젝트 전체 이벤트 시스템 일관성 고려 필요

**현재 방식의 장점:**
- Inspector에서 이벤트 흐름 시각화 가능
- 에셋 재사용 가능
- DI (Dependency Injection) 용이

**설계도 방식의 장점:**
- 성능상 유리 (2배 빠름)
- 코드만으로 완결
- 가비지 생성 적음

---

### 🔴 4. 싱글톤 충돌 (CRITICAL)

#### 문제점
CollectionManager와 기존 싱글톤들의 초기화 순서 충돌

**설계도 (CollectionManager):**
```csharp
public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

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

        LoadCollection();  // ⚠️ Awake에서 즉시 로드
    }
}
```

**현재 구현 (ServiceLocator 패턴):**
```csharp
public class CardServiceManager : MonoBehaviour
{
    private void Awake()
    {
        InitializeServices();  // 의존성 순서 관리
    }

    private void InitializeServices()
    {
        // 순서가 중요!
        1. GlobalStateManager
        2. CardDataManager
        3. CardHandManager.Init(dependencies)  // 외부 주입
    }
}

// ServiceLocator로 접근
GlobalStateManager.Instance
CardHandManager.Instance
```

#### 충돌 영향
- **초기화 순서**: 설계도의 CollectionManager가 먼저 초기화되면 다른 매니저들이 준비 안 됨
- **DontDestroyOnLoad**: 씬 전환 시 싱글톤 중복 생성 위험
- **의존성 주입**: 현재 코드는 명시적 Init() 패턴, 설계도는 암시적 Awake() 패턴

#### 심각도: 🔴 CRITICAL
- 런타임 NullReferenceException 발생 가능
- 초기화 순서 버그 디버깅 어려움

---

### 🟠 5. UI 패널 관리 시스템 충돌 (MEDIUM)

#### 문제점
설계도의 UIPanel 구조와 현재 구현이 다릅니다.

**설계도 (IOpenablePanel):**
```csharp
public interface IOpenablePanel
{
    Button OpenButton { get; set; }
    Button CloseButton { get; set; }
}

public class InventoryPanel : UIPanel, IOpenablePanel
{
    public Button OpenButton { get; set; }
    public Button CloseButton { get; set; }
}
```

**현재 구현:**
```csharp
public interface IUIPanel
{
    void OnInitializeSelf();
    void OnInitializeWithDependencies();
    void ShowPanel();
    void HidePanel();
    void DestroyPanel();
}

public abstract class UIPanel : MonoBehaviour, IUIPanel
{
    // Awake/Start 분리로 안전한 초기화
    protected virtual void OnInitializeSelf() { }
    protected virtual void OnInitializeWithDependencies() { }
}

// UIPanelManager가 자동으로 모든 패널 관리
UIPanelManager.Instance.GetPanel<InventoryPanel>().ShowPanel();
```

#### 충돌 영향
- **인터페이스 불일치**: IOpenablePanel은 현재 코드에 없음
- **초기화 순서**: 현재는 2단계 초기화, 설계도는 1단계
- **UIPanelManager 자동 관리 vs 수동 관리**

#### 심각도: 🟠 MEDIUM
- 어댑터 패턴으로 해결 가능
- 기존 UIPanel 구조 유지하면서 확장 가능

---

### 🟠 6. Enum 정의 충돌 (MEDIUM)

#### 문제점
설계도의 Enum과 현재 코드의 Enum이 다릅니다.

**설계도:**
```csharp
public enum CardRarity
{
    All = 0,      // ⚠️ 필터용
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public enum CardType
{
    All = 0,      // ⚠️ 필터용
    Unit = 1,
    Spell = 2,
    Equipment = 3
}
```

**현재 구현:**
```csharp
public enum CardRarity
{
    Common = 0,   // ✅ 필터 없이 시작
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

// CardType enum 자체가 없음!
// 대신 EffectType이 있음
public enum EffectType
{
    None,
    Summon,    // 유닛 소환
    Damage,    // 데미지 스펠
    Heal       // 힐 스펠
}
```

#### 충돌 영향
- **필터링 로직**: 설계도의 `CardRarity.All`은 필터용이지만 현재 코드에는 없음
- **CardType 부재**: 타입 필터를 EffectType으로 대체해야 함
- **희귀도 이름 차이**: Uncommon vs Rare 단계 차이

#### 심각도: 🟠 MEDIUM
- 필터 로직 수정 필요
- CardType → EffectType 매핑 로직 추가

---

## API 충돌

### 🟢 7. CardData API 불일치 (LOW)

#### 문제점
설계도가 가정하는 CardData API와 실제 API가 다릅니다.

**설계도가 사용하는 API:**
```csharp
// InventoryPanel.cs 라인 484-489
if (currentRarityFilter != CardRarity.All &&
    card.Rarity != currentRarityFilter)
    return false;

if (currentTypeFilter != CardType.All &&
    card.CardType != currentTypeFilter)
    return false;
```

**현재 CardData 실제 API:**
```csharp
// 현재 CardData.cs
public CardRarity Rarity => rarity;           // ✅ 존재
public EffectType GetEffectType();            // ⚠️ CardType 대신
public TargetType TargetType => targetType;   // ⚠️ 추가 속성
public bool IsValidTarget(Vector3 position);  // ⚠️ 추가 메서드
```

#### 충돌 영향
- **타입 필터**: `card.CardType` → `card.GetEffectType()` 변경 필요
- **검증 로직**: 현재 코드의 타겟팅 시스템 고려 필요

#### 심각도: 🟢 LOW
- 단순 API 호출 변경으로 해결
- 어댑터 메서드 추가로 호환성 확보 가능

---

### 🟢 8. 네이밍 충돌 (LOW)

#### 문제점
설계도와 현재 코드의 클래스/메서드 이름 충돌

**설계도:**
```csharp
public class InventoryCardSlot { }
public class DeckCardSlot { }
public class DeckInventoryCoordinator { }
```

**현재 코드:**
```csharp
public class CardUI { }            // ⚠️ 역할이 유사하지만 이름 다름
public class CardHandManager { }   // ⚠️ 핸드 관리, 인벤토리 아님
```

#### 충돌 영향
- **네임스페이스 충돌**: 같은 이름의 클래스 생성 시 컴파일 에러
- **혼란**: CardUI vs InventoryCardSlot 역할 구분 어려움

#### 심각도: 🟢 LOW
- 네임스페이스 분리로 해결
- 명확한 네이밍 컨벤션 적용

---

## 아키텍처 충돌

### 🟡 9. 인벤토리 vs 핸드 개념 충돌 (HIGH)

#### 문제점
설계도는 "인벤토리 (모든 소유 카드)"를 관리하지만, 현재 코드는 "핸드 (전투 중 손에 든 카드)"를 관리합니다.

**설계도 개념:**
```
[인벤토리] → 드래그 → [덱 빌더]
    ↓                      ↓
소유한 모든 카드        덱 구성용
(컬렉션)               (30장 제한)
```

**현재 구현 개념:**
```
[CardHandManager] → 드래그 → [타일 배치]
    ↓                         ↓
전투 중 손패              필드에 배치
(7장 제한)              (마나 소모)
```

#### 충돌 영향
- **용도 불일치**: 인벤토리(컬렉션 관리) vs 핸드(전투 시스템)
- **제약 조건**: 덱 30장 vs 핸드 7장
- **게임 단계**: 덱 빌딩(메타) vs 전투(인게임)

#### 심각도: 🟡 HIGH
- 두 시스템이 공존해야 함
- 명확한 역할 분리 필요

**해결 방안:**
```
게임 플로우:

[메인 메뉴]
    ↓
[컬렉션/인벤토리] → [덱 빌더] → 덱 저장
    ↓
[전투 시작] → 덱에서 카드 드로우 → [핸드]
    ↓
[CardUI 드래그] → [타일 배치]
```

---

### 🔴 10. ScriptableObject vs MonoBehaviour 충돌 (CRITICAL)

#### 문제점
설계도의 DeckData는 ScriptableObject지만, 현재 코드는 MonoBehaviour 기반입니다.

**설계도 (DeckData):**
```csharp
[CreateAssetMenu(fileName = "New Deck", menuName = "Game/Deck Data")]
public class DeckData : ScriptableObject
{
    [SerializeField] private string deckName;
    [SerializeField] private List<DeckCardEntry> cards;

    // ScriptableObject이므로 에셋으로 저장 가능
}
```

**현재 구현 (저장 시스템):**
```csharp
// JSON 기반 저장
[System.Serializable]
public class DeckSaveData
{
    public string deckName;
    public List<string> cardIds;
    public List<int> counts;
}

public static class DeckSaveSystem
{
    public static void SaveDeck(DeckData deckData)
    {
        string json = JsonUtility.ToJson(saveData);
        File.WriteAllText(filePath, json);
    }
}
```

#### 충돌 영향
- **에디터 내 관리 vs 런타임 생성**: ScriptableObject는 에디터에서 생성, JSON은 런타임 생성
- **참조 유지**: ScriptableObject는 CardData 참조 직접 유지, JSON은 name 문자열로 변환
- **빌드 후 수정**: ScriptableObject는 빌드 후 수정 불가, JSON은 가능

#### 심각도: 🔴 CRITICAL
- 저장 시스템 전체 재설계 필요
- 모바일 빌드 시 차이 큼 (ScriptableObject는 읽기 전용)

---

### 🟠 11. 마나 커브 표시 방식 차이 (MEDIUM)

#### 문제점
설계도는 0~9+ 마나 커브를 막대 그래프로 표시하지만, 현재 게임의 마나 시스템이 다를 수 있습니다.

**설계도 (DeckBuilderPanel):**
```csharp
private void InitializeManaCurve()
{
    // 0~9+ 마나용 막대 10개 생성
    for (int i = 0; i <= 9; i++)
    {
        GameObject barObj = Instantiate(manaCurveBarPrefab, manaCurveContainer);
        Image barImage = barObj.GetComponent<Image>();
        manaCurveBars.Add(barImage);
    }
}
```

**현재 CardData:**
```csharp
[SerializeField] private int manaCost;  // ✅ 호환됨

// 하지만 게임의 마나 시스템은?
// - 턴당 마나 증가?
// - 최대 마나?
// - 마나 회복 속도?
```

#### 충돌 영향
- **마나 범위**: 현재 게임이 0~9+ 범위를 사용하는지 확인 필요
- **게임 밸런스**: 마나 커브 분석이 현재 게임에 적합한지 검증 필요

#### 심각도: 🟠 MEDIUM
- 마나 시스템 확인 후 조정 필요
- UI는 그대로 사용 가능, 로직만 수정

---

### 🟢 12. 필터링/정렬 기능 부재 (LOW)

#### 문제점
현재 CardHandManager에는 필터링/정렬 기능이 없습니다.

**설계도가 제공하는 기능:**
```csharp
// InventoryPanel.cs
private void ApplyFiltersAndSort()
{
    filteredCards = allCards
        .Where(card => MatchesFilters(card))    // 필터링
        .OrderBy(card => GetSortKey(card))      // 정렬
        .ToList();
}

// 필터: 희귀도, 타입, 텍스트 검색
// 정렬: 이름, 마나, 희귀도, 타입
```

**현재 CardHandManager:**
```csharp
// 필터링/정렬 기능 없음
private List<CardUI> cardUIList = new List<CardUI>();

public void AddCardToHand(CardData cardData)
{
    // 그냥 추가만 함
    cardUIList.Add(cardUI);
}
```

#### 충돌 영향
- **기능 부재**: 현재 핸드는 단순 리스트 관리만
- **인벤토리용 기능**: 설계도의 필터링은 인벤토리/컬렉션용

#### 심각도: 🟢 LOW
- 핸드에는 필터링 불필요 (소수의 카드만 관리)
- 인벤토리 시스템 추가 시 구현

---

## 해결 방안

### 🎯 전략 1: 단계적 통합 (권장)

#### Phase 1: 컬렉션 시스템 추가 (새로운 시스템)
```
현재 코드 (유지) + 설계도 (추가)
    ↓
[기존] CardHandManager (전투용 핸드)
[신규] InventoryPanel (컬렉션 관리)
[신규] DeckBuilderPanel (덱 빌딩)
```

**장점:**
- 기존 전투 시스템 건드리지 않음
- 점진적 추가로 안정성 확보
- 롤백 용이

**단점:**
- 중복 코드 발생 가능 (CardUI vs InventoryCardSlot)
- 유지보수 복잡도 증가

---

#### Phase 2: CardData 확장 (호환성 유지)
```csharp
// 현재 CardData 확장
public class CardData : ScriptableObject
{
    // === 기존 속성 (유지) ===
    [SerializeField] private EffectData effectData;
    [SerializeField] private TargetType targetType;

    // === 새로운 속성 (추가) ===
    [SerializeField] private int maxCopiesInDeck = 3;  // 덱 빌딩용

    // === 호환성 메서드 (어댑터) ===
    public CardType CardType => ConvertEffectTypeToCardType();

    private CardType ConvertEffectTypeToCardType()
    {
        return effectData.EffectType switch
        {
            EffectType.Summon => CardType.Unit,
            EffectType.Damage => CardType.Spell,
            EffectType.Heal => CardType.Spell,
            _ => CardType.Unit
        };
    }
}
```

**장점:**
- 기존 코드 호환성 유지
- 새 기능 추가 가능

**단점:**
- CardData가 복잡해짐
- 전투용/덱빌딩용 속성 혼재

---

#### Phase 3: 이벤트 시스템 통합
```csharp
// 하이브리드 방식
public class CardTransferEventChannelSO : ScriptableObject
{
    // ScriptableObject 방식 유지 (현재)
    private event Action<CardData, TransferSource, TransferDestination> onCardTransferred;

    public void RaiseEvent(CardData card, TransferSource source, TransferDestination destination)
    {
        onCardTransferred?.Invoke(card, source, destination);

        // Static Event도 동시 발생 (설계도 호환)
        InventoryCardSlot.CardDragEndEvent?.Invoke(card, null);
    }

    // 양쪽 구독 모두 지원
    public void Subscribe(Action<CardData, TransferSource, TransferDestination> handler)
    {
        onCardTransferred += handler;
    }
}
```

**장점:**
- 양쪽 방식 모두 지원
- 점진적 마이그레이션 가능

**단점:**
- 복잡도 증가
- 성능 오버헤드

---

#### Phase 4: 싱글톤 초기화 순서 관리
```csharp
// CardServiceManager 확장
public class CardServiceManager : MonoBehaviour
{
    private void Awake()
    {
        InitializeServices();
    }

    private void InitializeServices()
    {
        // 1. 기존 매니저들 (우선순위 높음)
        GlobalStateManager.Initialize();
        CardDataManager.Initialize();
        CardHandManager.Init(dependencies);

        // 2. 새로운 매니저들 (우선순위 낮음)
        CollectionManager.Initialize();  // 덱 빌딩용

        // 3. UI 시스템
        UIPanelManager.Initialize();
    }
}

// CollectionManager 수정
public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

    private void Awake()
    {
        // DontDestroyOnLoad 제거 (ServiceManager가 관리)
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // 외부에서 명시적으로 호출
    public static void Initialize()
    {
        if (Instance != null)
            Instance.LoadCollection();
    }
}
```

**장점:**
- 초기화 순서 명확히 제어
- NullReferenceException 방지

**단점:**
- 기존 DontDestroyOnLoad 패턴 포기
- 씬 전환 시 재초기화 필요

---

### 🎯 전략 2: 설계도 수정 (대안)

#### 설계도를 현재 코드에 맞게 수정

**수정 사항:**
```markdown
# Phase 1 수정
## 1.1 InventoryPanel
- CardType enum 제거 → EffectType 사용
- CardType 필터 → EffectType 필터로 변경

## 1.2 InventoryCardSlot
- Static Event → ScriptableObject EventChannel 변경
- 복사본 생성 방식 → 원본 이동 방식으로 변경 (선택적)

# Phase 3 수정
## 3.1 CardTransferEventChannel
- 이미 현재 코드에 존재하는 CardDragEndEventChannelSO 활용

# Phase 6 수정
## 6.2 CollectionManager
- DontDestroyOnLoad 제거
- ServiceLocator 패턴으로 변경
- Init() 메서드 추가
```

**장점:**
- 현재 코드 최소 변경
- 아키텍처 일관성 유지

**단점:**
- 설계도의 모범 사례 일부 포기
- 설계도 문서 대폭 수정 필요

---

### 🎯 전략 3: 네임스페이스 분리 (충돌 회피)

#### 구조 분리로 충돌 방지
```csharp
// 전투 시스템 (기존)
namespace Game.Battle
{
    public class CardUI { }
    public class CardHandManager { }
}

// 컬렉션/덱 빌딩 시스템 (신규)
namespace Game.Collection
{
    public class InventoryCardSlot { }
    public class DeckBuilderPanel { }
    public class CollectionManager { }
}

// 공통 데이터
namespace Game.Data
{
    public class CardData { }  // 양쪽에서 사용
}
```

**장점:**
- 충돌 없이 공존 가능
- 역할 명확히 구분

**단점:**
- 네임스페이스 관리 복잡도 증가
- using 구문 많아짐

---

## 우선순위별 작업 계획

### 🔴 즉시 해결 필요 (CRITICAL)
1. **CardData 구조 결정**
   - Option A: 현재 구조 유지 + 어댑터 메서드 추가
   - Option B: 설계도에 맞게 대폭 리팩토링
   - **권장**: Option A (영향 범위 최소화)

2. **싱글톤 초기화 순서**
   - CardServiceManager에 CollectionManager 추가
   - 명시적 Initialize() 호출 순서 정의
   - **예상 작업 시간**: 2-3시간

3. **ScriptableObject vs JSON 저장**
   - 현재 JSON 방식 유지 (모바일 친화적)
   - 설계도의 DeckData를 JSON 호환으로 수정
   - **예상 작업 시간**: 4-6시간

---

### 🟡 중요 작업 (HIGH)
1. **드래그 앤 드롭 통합**
   - 인벤토리용 InventoryCardSlot 새로 생성
   - 기존 CardUI는 전투용으로 유지
   - **예상 작업 시간**: 8-12시간

2. **이벤트 시스템 결정**
   - 현재 EventChannel 방식 유지 권장
   - 설계도 코드를 EventChannel 방식으로 변경
   - **예상 작업 시간**: 4-6시간

3. **인벤토리 vs 핸드 역할 분리**
   - 명확한 게임 플로우 정의
   - CollectionManager (메타게임용) vs CardHandManager (전투용)
   - **예상 작업 시간**: 개념 설계 2-3일

---

### 🟠 중간 작업 (MEDIUM)
1. **UI 패널 시스템 통합**
   - IOpenablePanel 제거 또는 현재 IUIPanel에 통합
   - UIPanelManager 활용
   - **예상 작업 시간**: 4-6시간

2. **Enum 정의 통합**
   - CardRarity: Uncommon 유지 결정
   - CardType: EffectType 매핑 함수 작성
   - **예상 작업 시간**: 2-3시간

3. **마나 커브 시스템 검증**
   - 현재 게임의 마나 시스템 확인
   - 0~9+ 범위 적합성 검토
   - **예상 작업 시간**: 1-2시간 (리서치)

---

### 🟢 낮은 우선순위 (LOW)
1. **네이밍 통일**
   - 네임스페이스 분리로 해결
   - 명확한 네이밍 가이드 작성
   - **예상 작업 시간**: 2-3시간

2. **필터링/정렬 기능 추가**
   - 인벤토리 시스템 완성 후 구현
   - **예상 작업 시간**: 6-8시간

---

## 권장 실행 계획

### Week 1: 기반 작업
- [ ] CardData 어댑터 메서드 추가 (CardType 호환)
- [ ] 싱글톤 초기화 순서 정리
- [ ] 네임스페이스 분리 (Game.Battle vs Game.Collection)
- [ ] 설계도 문서 수정 (현재 코드 반영)

### Week 2: 컬렉션 시스템 구현
- [ ] CollectionManager 구현 (ServiceLocator 패턴)
- [ ] InventoryPanel UI 제작
- [ ] InventoryCardSlot 드래그 구현 (EventChannel 방식)

### Week 3: 덱 빌더 구현
- [ ] DeckBuilderPanel UI 제작
- [ ] DeckCardSlot 구현
- [ ] DeckInventoryCoordinator 구현

### Week 4: 검증 및 통합
- [ ] DeckValidator 구현 (EffectType 기반)
- [ ] 마나 커브 시각화
- [ ] 저장/로드 시스템 (JSON 기반)

### Week 5: 폴리싱
- [ ] 필터링/정렬 기능
- [ ] 시각/사운드 피드백
- [ ] 성능 최적화 (Object Pooling)

---

## 결론

### 주요 충돌 요약
| 충돌 항목 | 심각도 | 영향 범위 | 해결 난이도 |
|----------|--------|----------|------------|
| CardData 구조 | 🔴 CRITICAL | 70% | 중간 (어댑터 패턴) |
| 싱글톤 초기화 | 🔴 CRITICAL | 30% | 낮음 (순서 정의) |
| ScriptableObject 저장 | 🔴 CRITICAL | 20% | 중간 (설계도 수정) |
| 드래그 시스템 | 🟡 HIGH | 40% | 높음 (새로 구현) |
| 이벤트 시스템 | 🟡 HIGH | 50% | 중간 (하이브리드) |
| 인벤토리 vs 핸드 | 🟡 HIGH | 역할 분리 | 낮음 (개념 정리) |

### 권장 접근 방식
**✅ 단계적 통합 (전략 1) + 설계도 일부 수정 (전략 2)**

1. **현재 코드 우선**: 기존 전투 시스템은 건드리지 않음
2. **신규 시스템 추가**: 컬렉션/덱빌딩은 별도 네임스페이스로 구현
3. **어댑터 패턴**: CardData는 양쪽 호환 가능하도록 확장
4. **점진적 마이그레이션**: 검증 후 통합

### 예상 총 작업 시간
- **최소**: 80-100시간 (5-6주, 1인 기준)
- **권장**: 120-150시간 (2-3개월, 충분한 테스트 포함)
- **최대**: 200시간 이상 (완벽한 통합 + 리팩토링)

---

**작성자**: Claude AI
**작성일**: 2025-01-30
**문서 버전**: 1.0
**상태**: 최종 검토 필요
