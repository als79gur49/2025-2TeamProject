# DeckBuilder Coordinator Pattern 리팩토링 요약

**작업 일자**: 2025-11-08
**브랜치**: feature/carddata-refactoring

## 📌 목적

DeckBuilderPanel이 ISaveDataAdapter를 직접 사용하는 아키텍처 불일치 해결:
- **문제**: 저장은 Coordinator가 담당하지만, 로딩은 DeckBuilderPanel이 직접 수행
- **해결**: 모든 저장/로드 로직을 CardInventorySaveCoordinator로 중앙화

## 🎯 적용한 패턴

### Complete Coordinator Pattern (방안 1)
- UI 계층과 영속성 계층의 완전한 분리
- Separated Presentation 패턴 준수
- 이벤트 기반 아키텍처로 느슨한 결합

## 📝 주요 변경 사항

### 1. DeckBuilderPanel.cs

#### ➕ 추가된 요소

**이벤트**:
```csharp
public event Action<string, Dictionary<CardData, int>> OnDeckSaveRequested;
public event Action<string> OnDeckLoadRequested;
```

**Public API**:
```csharp
public void LoadDeck(string deckName, Dictionary<CardData, int> deckCards)
public void SetAvailableDecks(List<string> deckNames)
public void SyncDropdownToDeck(string deckName)
```

#### ➖ 제거된 요소

**필드**:
- `private ISaveDataAdapter saveAdapter`

**메서드**:
- `OnInitializeWithDependencies()` - ServiceLocator 의존성 제거
- `TryLoadLastUsedDeck()` - Coordinator가 자동 처리
- `OnLoadDeckClicked()` - 드롭다운으로 대체
- `LoadDeckFromFile()` - Coordinator가 담당
- `RefreshDeckDropdown()` - SetAvailableDecks()로 대체
- `private SyncDropdownToCurrentDeck()` - public API로 대체

**기타**:
- `loadDeckButton` 리스너 등록 제거 (드롭다운으로 대체)

#### 🔄 수정된 요소

**OnShowPanel()**:
```csharp
// 변경 전: SaveAdapter에서 직접 로드
saveAdapter.LoadSpecific(SaveFileType.CardCollection);
TryLoadLastUsedDeck();

// 변경 후: Coordinator가 자동 로드하므로 UI 업데이트만
UpdateDeckDisplay();
Debug.Log("[DeckBuilderPanel] Panel shown, waiting for Coordinator to load deck");
```

**OnSaveDeckClicked()**:
```csharp
// 변경 전: SaveAdapter에 직접 저장
saveAdapter.SaveDeck(deckName, deckCards);

// 변경 후: 이벤트 발생
OnDeckSaveRequested?.Invoke(deckName, new Dictionary<CardData, int>(deckCards));
```

**OnDeckDropdownChanged()**:
```csharp
// 변경 전: LoadDeckFromFile() 직접 호출
LoadDeckFromFile(selectedDeckName);

// 변경 후: 이벤트 발생
OnDeckLoadRequested?.Invoke(selectedDeckName);
```

### 2. CardInventorySaveCoordinator.cs

#### 🔄 확장된 HandlePanelShown()

```csharp
private void HandlePanelShown(IUIPanel panel)
{
    // 1. 카드 컬렉션 데이터 로드
    saveAdapter.LoadSpecific(SaveFileType.CardCollection);

    // 2. 저장된 덱 목록 가져오기 및 드롭다운 갱신
    var savedDecks = saveAdapter.GetSavedDeckNames();
    deckBuilderPanel.SetAvailableDecks(savedDecks);

    // 3. 마지막 사용 덱 자동 로드
    string lastDeckName = saveAdapter.LoadLastUsedDeckName();

    if (!string.IsNullOrEmpty(lastDeckName) && savedDecks.Contains(lastDeckName))
    {
        var deckCards = saveAdapter.LoadDeck(lastDeckName);
        deckBuilderPanel.LoadDeck(lastDeckName, deckCards);
        deckBuilderPanel.SyncDropdownToDeck(lastDeckName);
    }
    else if (savedDecks.Count > 0)
    {
        // Fallback: 첫 번째 덱 로드
        var firstDeck = saveAdapter.LoadDeck(savedDecks[0]);
        deckBuilderPanel.LoadDeck(savedDecks[0], firstDeck);
        deckBuilderPanel.SyncDropdownToDeck(savedDecks[0]);
    }
}
```

#### ➕ 추가된 이벤트 핸들러

**HandleDeckSaveRequest()**:
```csharp
private void HandleDeckSaveRequest(string deckName, Dictionary<CardData, int> deckCards)
{
    bool success = saveAdapter.SaveDeck(deckName, deckCards);

    if (success)
    {
        saveAdapter.SaveLastUsedDeckName(deckName);

        // 드롭다운 갱신 (새로 저장된 덱 포함)
        var updatedDecks = saveAdapter.GetSavedDeckNames();
        deckBuilderPanel.SetAvailableDecks(updatedDecks);
        deckBuilderPanel.SyncDropdownToDeck(deckName);
    }
}
```

**HandleDeckLoadRequest()**:
```csharp
private void HandleDeckLoadRequest(string deckName)
{
    var deckCards = saveAdapter.LoadDeck(deckName);

    if (deckCards != null)
    {
        deckBuilderPanel.LoadDeck(deckName, deckCards);
        saveAdapter.SaveLastUsedDeckName(deckName);
    }
}
```

#### 🔄 Start() 및 OnDestroy() 수정

```csharp
private void Start()
{
    // ... 기존 코드 ...

    // DeckBuilderPanel 이벤트 구독
    if (deckBuilderPanel != null)
    {
        deckBuilderPanel.OnDeckSaveRequested += HandleDeckSaveRequest;
        deckBuilderPanel.OnDeckLoadRequested += HandleDeckLoadRequest;
    }
}

private void OnDestroy()
{
    // ... 기존 코드 ...

    // DeckBuilderPanel 이벤트 구독 해제
    if (deckBuilderPanel != null)
    {
        deckBuilderPanel.OnDeckSaveRequested -= HandleDeckSaveRequest;
        deckBuilderPanel.OnDeckLoadRequested -= HandleDeckLoadRequest;
    }
}
```

## 🏗️ 아키텍처 개선

### Before (문제점)

```
┌─────────────────────────┐
│  DeckBuilderPanel       │
├─────────────────────────┤
│ - saveAdapter ❌        │ ← 직접 접근
│                         │
│ - TryLoadLastUsedDeck() │ → saveAdapter.LoadDeck()
│ - LoadDeckFromFile()    │ → saveAdapter.LoadDeck()
│ - RefreshDeckDropdown() │ → saveAdapter.GetSavedDeckNames()
│ - OnSaveDeckClicked()   │ → saveAdapter.SaveDeck()
└─────────────────────────┘
         ↓ (혼재된 책임)
┌─────────────────────────┐
│ CardInventorySaveCoord  │
├─────────────────────────┤
│ - HandlePanelHidden()   │ → saveAdapter.SaveDeck() (저장만)
└─────────────────────────┘
```

**문제점**:
- 저장은 Coordinator, 로딩은 Panel에서 처리 → 일관성 없음
- UI가 영속성 계층에 직접 의존 → 결합도 높음
- 테스트 어려움 (SaveAdapter mock 필요)

### After (개선점)

```
┌─────────────────────────┐
│  DeckBuilderPanel       │  ← UI만 담당
├─────────────────────────┤
│ + OnDeckSaveRequested   │ ─┐
│ + OnDeckLoadRequested   │ ─┤ 이벤트
│                         │  │
│ + LoadDeck()            │ ←┤
│ + SetAvailableDecks()   │ ←┤ Public API
│ + SyncDropdownToDeck()  │ ←┘
└─────────────────────────┘
         ↑ 이벤트/호출
         │
┌─────────────────────────┐
│ CardInventorySaveCoord  │  ← 모든 저장/로드 중앙화
├─────────────────────────┤
│ - HandlePanelShown()    │ → 자동 로드
│ - HandleDeckSaveRequest()│ → saveAdapter.SaveDeck()
│ - HandleDeckLoadRequest()│ → saveAdapter.LoadDeck()
│ - HandlePanelHidden()   │ → saveAdapter.SaveDeck()
└─────────────────────────┘
         ↓
┌─────────────────────────┐
│   ISaveDataAdapter      │
└─────────────────────────┘
```

**개선점**:
- ✅ 단일 책임 원칙 (SRP): UI는 표현만, Coordinator는 저장/로드만
- ✅ 의존성 역전 (DIP): UI가 구체적인 SaveAdapter에 의존하지 않음
- ✅ 테스트 용이성: Coordinator만 Mock 하면 됨
- ✅ 일관성: 모든 저장/로드가 한 곳에서 관리

## 🔄 데이터 흐름

### 시나리오 1: 패널 열기 (자동 로드)

```
CardInventoryPanel.OnShow()
  → CardInventorySaveCoordinator.HandlePanelShown()
    → saveAdapter.LoadSpecific(CardCollection)
    → savedDecks = saveAdapter.GetSavedDeckNames()
    → deckBuilderPanel.SetAvailableDecks(savedDecks)  // 드롭다운 갱신
    → lastDeckName = saveAdapter.LoadLastUsedDeckName()
    → deckCards = saveAdapter.LoadDeck(lastDeckName)
    → deckBuilderPanel.LoadDeck(lastDeckName, deckCards)  // UI 업데이트
    → deckBuilderPanel.SyncDropdownToDeck(lastDeckName)  // 드롭다운 선택
```

### 시나리오 2: Save 버튼 클릭 (수동 저장)

```
User clicks Save Button
  → DeckBuilderPanel.OnSaveDeckClicked()
    → DeckValidator.ValidateDeck(deckCards)  // UI 레벨 검증
    → OnDeckSaveRequested?.Invoke(deckName, deckCards)  // 이벤트 발생
  → CardInventorySaveCoordinator.HandleDeckSaveRequest()
    → saveAdapter.SaveDeck(deckName, deckCards)
    → saveAdapter.SaveLastUsedDeckName(deckName)
    → savedDecks = saveAdapter.GetSavedDeckNames()
    → deckBuilderPanel.SetAvailableDecks(savedDecks)  // 드롭다운 갱신
    → deckBuilderPanel.SyncDropdownToDeck(deckName)  // 드롭다운 선택
```

### 시나리오 3: 드롭다운에서 덱 선택 (로드)

```
User selects deck from dropdown
  → DeckBuilderPanel.OnDeckDropdownChanged(index)
    → selectedDeckName = dropdown.options[index].text
    → OnDeckLoadRequested?.Invoke(selectedDeckName)  // 이벤트 발생
  → CardInventorySaveCoordinator.HandleDeckLoadRequest()
    → deckCards = saveAdapter.LoadDeck(selectedDeckName)
    → deckBuilderPanel.LoadDeck(selectedDeckName, deckCards)  // UI 업데이트
    → saveAdapter.SaveLastUsedDeckName(selectedDeckName)
```

### 시나리오 4: 패널 닫기 (자동 저장)

```
CardInventoryPanel.OnHide()
  → CardInventorySaveCoordinator.HandlePanelHidden()
    → saveAdapter.SaveSpecific(CardCollection)
    → currentDeckName = deckBuilderPanel.GetCurrentDeckName()
    → deckCards = deckBuilderPanel.GetCurrentDeckCards()
    → saveAdapter.SaveDeck(currentDeckName, deckCards)
    → saveAdapter.SaveLastUsedDeckName(currentDeckName)
```

## ✅ 테스트 체크리스트

### Unity 컴파일
- [ ] Unity 에디터에서 에러 없이 컴파일
- [ ] 경고 메시지 확인 및 해결

### 런타임 테스트

**기본 동작**:
- [ ] CardInventoryPanel 열기 → 마지막 사용 덱 자동 로드
- [ ] 드롭다운에서 다른 덱 선택 → 정상 로드
- [ ] 카드 추가/제거 → 덱 카운트 및 UI 업데이트
- [ ] Save 버튼 클릭 → 덱 저장 및 드롭다운 갱신
- [ ] 패널 닫기 → 현재 덱 자동 저장
- [ ] 패널 재오픈 → 마지막 덱 복원

**엣지 케이스**:
- [ ] 저장된 덱이 없을 때 → "저장된 덱 없음" 표시
- [ ] 마지막 덱 파일이 삭제되었을 때 → 첫 번째 덱 로드
- [ ] 빈 덱 상태에서 패널 닫기 → 저장하지 않음
- [ ] 덱 이름 없이 저장 시도 → 경고 메시지
- [ ] 유효하지 않은 덱 저장 시도 → 검증 실패

**드롭다운 동기화**:
- [ ] 덱 저장 후 드롭다운에 추가됨
- [ ] 덱 로드 후 드롭다운 선택 상태 동기화
- [ ] 새 덱 생성 → 드롭다운에 즉시 반영

## 📊 변경 통계

**DeckBuilderPanel.cs**:
- 제거된 코드: ~200 라인
- 추가된 코드: ~120 라인
- 순 감소: ~80 라인

**CardInventorySaveCoordinator.cs**:
- 추가된 코드: ~150 라인

**총계**:
- 순 증가: ~70 라인 (중앙화된 로직으로 인한 증가)

## 🎓 배운 점

### 아키텍처 패턴
- **Coordinator Pattern**: UI와 비즈니스 로직의 완전한 분리
- **Event-Driven Architecture**: 느슨한 결합으로 테스트 용이성 증가
- **Separated Presentation**: UI는 표현만, 로직은 별도 계층

### 리팩토링 원칙
- **Single Responsibility**: 각 클래스는 하나의 책임만
- **Dependency Inversion**: 구체 타입이 아닌 추상화에 의존
- **Don't Repeat Yourself**: 중복 로직 제거

## 📚 참고 문서

- [CardInventorySaveCoordinator.cs](../Assets/Script/UI/Coordinators/CardInventorySaveCoordinator.cs)
- [DeckBuilderPanel.cs](../Assets/Script/UI/Panels/DeckBuilderPanel.cs)
- [UIPanel.cs](../Assets/Script/UI/Core/UIPanel.cs)

## 🔗 관련 커밋

- **이전 커밋**: `dcce0fdf` - fix: 덱 패널 자동 로딩 시스템 구현
- **현재 작업**: Coordinator Pattern 완전 적용
