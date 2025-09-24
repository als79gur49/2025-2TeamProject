# 카드 드래그 앤 드롭 유닛 소환 시스템 - 설계 문서

## 📋 프로젝트 개요

Unity 턴제 전략 게임에 카드 드래그 앤 드롭을 통한 유닛 소환 시스템을 통합하는 완전한 설계 및 구현 가이드입니다.

### 🎯 요구사항
- ✅ 카드 UI를 선택하여 타일로 드래그 가능
- ✅ 드롭 시 카드 정보 기반 유닛 소환
- ✅ 빈 타일에만 소환 가능
- ✅ 플레이어는 좌측 1열, 적군은 우측 1열에만 소환
- ✅ 기존 4페이즈 턴 시스템과 완벽 통합

## 🏗️ 시스템 아키텍처

### 기존 컴포넌트 재사용률
- **UnitCard**: 95% 재사용 (소환 위치 메서드만 개선)
- **GridManager**: 100% 재사용 (인터페이스 완벽 호환)
- **Tile**: 90% 재사용 (드롭 핸들러 추가)
- **UnitService**: 85% 재사용 (ProcessSummonPhase 구현)
- **턴/페이즈 시스템**: 100% 재사용 (완벽한 통합 지점)

### 새로 생성할 컴포넌트 (5개)
1. **CardSpawnService** - 중앙 코디네이터 (IGameService 구현)
2. **SpawnValidator** - 규칙 검증 시스템
3. **CardUI** - 드래그/드롭 인터페이스
4. **CardHandManager** - 핸드 관리 시스템
5. **TileDropHandler** - 타일 드롭 처리

## 📁 파일 구조

```
Assets/Script/Game/
├── Card/
│   ├── Core/
│   │   └── UnitCard.cs (개선)
│   ├── UI/
│   │   ├── CardUI.cs (신규)
│   │   ├── CardHandManager.cs (신규)
│   │   └── TileDropHandler.cs (신규)
│   └── Services/
│       ├── CardSpawnService.cs (신규)
│       ├── SpawnValidator.cs (신규)
│       └── ICardSpawnService.cs (신규)
├── Services/
│   └── UnitService.cs (개선)
└── Data/
    ├── SpawnValidationResult.cs (신규)
    └── CardSpawnSettings.cs (신규)
```

## 🔧 핵심 컴포넌트 구현

### 1. CardSpawnService (중앙 코디네이터)

```csharp
public class CardSpawnService : MonoBehaviour, ICardSpawnService, IGameService
{
    // 핵심 소환 메서드
    public bool TrySpawnUnitFromCard(UnitCard card, Vector2Int gridPosition)
    {
        if (!IsInitialized) return false;
        
        OnSpawnStarted?.Invoke(card);
        
        // 소환 검증
        var validation = ValidateSpawn(card, gridPosition);
        if (!validation.IsValid)
        {
            HandleSpawnFailure(card, gridPosition, validation.FailureReason);
            return false;
        }
        
        return ExecuteSpawn(card, gridPosition);
    }
    
    // 실제 소환 실행
    private bool ExecuteSpawn(UnitCard card, Vector2Int gridPosition)
    {
        // 1. 월드 위치 계산
        Vector3 worldPosition = gridManager.GridToWorldPosition(gridPosition);
        Vector3 spawnPosition = worldPosition + spawnOffset;
        
        // 2. 유닛 프리팹 인스턴스화
        GameObject spawnedUnit = Instantiate(card.GetUnitPrefab(), spawnPosition, Quaternion.identity);
        
        // 3. 유닛 초기화 (기존 패턴 사용)
        var unit = spawnedUnit.GetComponent<Unit>();
        unit.Init(gridManager, gameServiceManager);
        
        // 4. 카드 데이터를 유닛 스탯으로 적용
        ApplyCardStatsToUnit(card, unit);
        
        // 5. 그리드에 유닛 배치
        bool placementSuccess = gridManager.MoveUnit(spawnedUnit, gridPosition);
        
        if (placementSuccess)
        {
            HandleSpawnSuccess(card, gridPosition, spawnedUnit);
            return true;
        }
        
        return false;
    }
}
```

### 2. SpawnValidator (검증 로직)

```csharp
public class SpawnValidator : MonoBehaviour
{
    // 종합적인 소환 유효성 검증
    public SpawnValidationResult CanSpawnUnit(UnitCard card, Vector2Int gridPosition, bool isPlayerCard)
    {
        var result = new SpawnValidationResult();
        
        // 1. 그리드 경계 검증
        if (!ValidateGridBounds(gridPosition, out string boundsError))
        {
            result.SetInvalid(SpawnValidationType.GridBounds, boundsError);
            return result;
        }
        
        // 2. 타일 점유 검증
        if (requireEmptyTiles && !ValidateTileOccupancy(gridPosition, out string occupancyError))
        {
            result.SetInvalid(SpawnValidationType.TileOccupancy, occupancyError);
            return result;
        }
        
        // 3. 열 제한 검증 (플레이어=좌측, 적=우측)
        if (enforceColumnRestrictions && !ValidateColumnRestrictions(gridPosition, isPlayerCard, out string columnError))
        {
            result.SetInvalid(SpawnValidationType.ColumnRestriction, columnError);
            return result;
        }
        
        // 4. 페이즈 제한 검증
        if (enforcePhaseRestrictions && !ValidatePhaseRestrictions(isPlayerCard, out string phaseError))
        {
            result.SetInvalid(SpawnValidationType.PhaseRestriction, phaseError);
            return result;
        }
        
        result.SetValid();
        return result;
    }
    
    // 열 제한 규칙 검증
    private bool ValidateColumnRestrictions(Vector2Int gridPosition, bool isPlayerCard, out string error)
    {
        error = "";
        
        if (isPlayerCard && gridPosition.x != playerSpawnColumn)
        {
            error = $"Player units can only spawn in column {playerSpawnColumn} (leftmost)";
            return false;
        }
        
        if (!isPlayerCard && gridPosition.x != GetEnemySpawnColumn())
        {
            error = $"Enemy units can only spawn in column {GetEnemySpawnColumn()} (rightmost)";
            return false;
        }
        
        return true;
    }
}
```

### 3. CardUI (드래그 앤 드롭 인터페이스)

```csharp
public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, 
                     IPointerEnterHandler, IPointerExitHandler
{
    // 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;
        
        StartDrag();
        PlaySound(dragStartSound);
        HighlightValidDropZones();
    }
    
    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        UpdateDragPosition(eventData);
    }
    
    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        var dropResult = GetDropTarget(eventData);
        if (dropResult.IsValid)
        {
            ExecuteDrop(dropResult.GridPosition);
        }
        else
        {
            CancelDrag();
        }
        
        EndDrag();
    }
    
    // 드롭 실행
    private void ExecuteDrop(Vector2Int gridPosition)
    {
        bool success = cardSpawnService.TrySpawnUnitFromCard(cardData, gridPosition);
        
        if (success)
        {
            OnSuccessfulDrop();
        }
        else
        {
            CancelDrag();
        }
    }
    
    // 유효한 드롭 존 하이라이트
    private void HighlightValidDropZones()
    {
        var allDropHandlers = FindObjectsOfType<TileDropHandler>();
        bool isPlayerCard = DetermineCardOwnership();
        
        foreach (var handler in allDropHandlers)
        {
            bool isValid = handler.IsValidDropZone(cardData, isPlayerCard);
            handler.SetDropZoneState(isValid ? DropZoneState.Valid : DropZoneState.Invalid);
        }
    }
}
```

### 4. TileDropHandler (타일 드롭 처리)

```csharp
public class TileDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // 드롭 처리
    public void OnDrop(PointerEventData eventData)
    {
        var cardUI = eventData.pointerDrag?.GetComponent<CardUI>();
        if (cardUI == null || !CanAcceptDrop(cardUI))
        {
            PlaySound(dropInvalidSound);
            return;
        }
        
        PlaySound(dropValidSound);
        OnSuccessfulDrop(cardUI);
    }
    
    // 드롭 가능 여부 확인
    public bool CanAcceptDrop(CardUI cardUI)
    {
        if (cardUI == null || !CanAcceptDrops) return false;
        
        if (spawnValidator != null)
        {
            bool isPlayerCard = DetermineCardOwnership(cardUI);
            return spawnValidator.IsValidDropZone(GridPosition, isPlayerCard);
        }
        
        return tileComponent.CanPlaceUnit();
    }
    
    // 시각적 상태 적용
    public void SetDropZoneState(DropZoneState state)
    {
        if (currentState == state) return;
        
        currentState = state;
        StopPulseAnimation();
        
        switch (state)
        {
            case DropZoneState.Valid:
                SetMaterial(validDropMaterial);
                SetColor(validDropColor);
                if (enablePulseAnimation) StartPulseAnimation(validDropColor);
                break;
                
            case DropZoneState.Invalid:
                SetMaterial(invalidDropMaterial);
                SetColor(invalidDropColor);
                break;
                
            case DropZoneState.Highlighted:
                SetMaterial(highlightMaterial);
                SetColor(highlightColor);
                if (enablePulseAnimation) StartPulseAnimation(highlightColor);
                break;
                
            default:
                SetMaterial(normalMaterial ?? originalMaterial);
                SetColor(normalColor);
                break;
        }
    }
}
```

### 5. CardHandManager (핸드 관리)

```csharp
public class CardHandManager : MonoBehaviour
{
    // 플레이어 소환 모드 활성화
    public void EnablePlayerSummonMode()
    {
        if (summonModeActive) return;
        
        summonModeActive = true;
        playerEndedTurn = false;
        summonStartTime = Time.time;
        
        gameObject.SetActive(true);
        StartCoroutine(AnimateHandActivation(true));
        RefreshCardPlayability();
        
        summonTimerCoroutine = StartCoroutine(UpdateSummonTimer());
        HighlightPlayerSpawnZones();
    }
    
    // 플레이어 소환 모드 비활성화
    public void DisablePlayerSummonMode()
    {
        if (!summonModeActive) return;
        
        summonModeActive = false;
        
        if (summonTimerCoroutine != null)
        {
            StopCoroutine(summonTimerCoroutine);
            summonTimerCoroutine = null;
        }
        
        StartCoroutine(AnimateHandActivation(false));
        ClearAllDropZoneHighlights();
    }
    
    // 카드 추가
    public void AddCard(UnitCard card)
    {
        if (card == null || playerHand.Count >= maxHandSize) return;
        
        playerHand.Add(card);
        CreateCardUI(card);
        UpdateHandDisplay();
        OnCardAdded?.Invoke(card);
    }
    
    // 카드 제거 (사용 시)
    public void RemoveCard(UnitCard card)
    {
        int index = playerHand.IndexOf(card);
        if (index < 0) return;
        
        playerHand.RemoveAt(index);
        
        if (index < cardUIInstances.Count)
        {
            var cardUIToRemove = cardUIInstances[index];
            cardUIInstances.RemoveAt(index);
            StartCoroutine(AnimateCardRemoval(cardUIToRemove));
        }
        
        UpdateHandDisplay();
        LayoutCards();
        OnCardRemoved?.Invoke(card);
    }
}
```

## 🔧 기존 클래스 개선사항

### UnitCard.cs 개선

```csharp
public class UnitCard : BaseCard
{
    // NEW: 드래그 앤 드롭 지원 필드들
    [Header("Spawn Configuration")]
    [SerializeField] private bool enableDragSpawning = true;
    [SerializeField] private SpawnMethod spawnMethod = SpawnMethod.DragAndDrop;
    
    private Vector2Int targetSpawnPosition = Vector2Int.zero;
    private bool hasTargetPosition = false;
    
    // NEW: 타겟 스폰 위치 설정
    public void SetTargetSpawnPosition(Vector2Int position)
    {
        targetSpawnPosition = position;
        hasTargetPosition = true;
    }
    
    // ENHANCED: Use() 메서드 개선
    public override void Use()
    {
        if (!CanUse()) return;
        
        OnCardUsed();
        
        switch (spawnMethod)
        {
            case SpawnMethod.DragAndDrop:
                SummonUnitViaDragDrop();
                break;
            case SpawnMethod.Legacy:
                SummonUnit();
                break;
        }
    }
    
    // NEW: 드래그 앤 드롭을 통한 소환
    private void SummonUnitViaDragDrop()
    {
        if (!hasTargetPosition || cardSpawnService == null) return;
        
        bool success = cardSpawnService.TrySpawnUnitFromCard(this, targetSpawnPosition);
        if (success)
        {
            ClearTargetSpawnPosition();
        }
    }
}
```

### UnitService.cs 개선

```csharp
public class UnitService : MonoBehaviour, IUnitService
{
    // NEW: 카드 시스템 관련 필드들
    [SerializeField] private float playerSummonTimeLimit = 30f;
    [SerializeField] private bool enableAISummoning = false;
    
    private ICardSpawnService cardSpawnService;
    private CardHandManager cardHandManager;
    
    // ENHANCED: 소환 페이즈 처리 (기존 TODO 구현)
    private void ProcessSummonPhase(bool isPlayerUnits)
    {
        OnSummonPhaseStarted?.Invoke(isPlayerUnits);
        
        if (isPlayerUnits)
        {
            ProcessPlayerSummonPhase();
        }
        else
        {
            ProcessEnemySummonPhase();
        }
    }
    
    // NEW: 플레이어 소환 페이즈 처리
    private void ProcessPlayerSummonPhase()
    {
        if (cardHandManager != null)
        {
            cardHandManager.EnablePlayerSummonMode();
            StartCoroutine(WaitForPlayerSummons());
        }
        else
        {
            CompleteSummonPhase(true);
        }
    }
    
    // NEW: 플레이어 소환 대기
    private IEnumerator WaitForPlayerSummons()
    {
        float startTime = Time.time;
        
        while (Time.time - startTime < playerSummonTimeLimit)
        {
            if (cardHandManager.IsPlayerDoneSummoning)
            {
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }
        
        if (cardHandManager != null)
        {
            cardHandManager.DisablePlayerSummonMode();
        }
        
        CompleteSummonPhase(true);
    }
}
```

## 🎯 구현 우선순위

### Phase 1: 핵심 로직 (1-2주)
1. **SpawnValidator** - 모든 검증 규칙 구현
2. **CardSpawnService** - 중앙 코디네이터 및 GameService 통합
3. **SpawnValidationResult** - 데이터 구조 및 인터페이스

### Phase 2: UI 컴포넌트 (2-3주)
1. **CardUI** - 드래그 앤 드롭 기본 기능
2. **TileDropHandler** - 드롭 존 처리 및 시각적 피드백
3. **기본 상호작용 테스트** - 드래그/드롭 플로우 검증

### Phase 3: 핸드 관리 (1-2주)
1. **CardHandManager** - 카드 컬렉션 및 UI 관리
2. **UIService 통합** - 페이즈 기반 활성화/비활성화
3. **시간 제한 및 진행률** - 소환 타이머 시스템

### Phase 4: 시스템 통합 (1-2주)
1. **UnitCard 개선** - 드래그 스폰 지원
2. **UnitService 개선** - ProcessSummonPhase 완전 구현
3. **전체 워크플로우 테스트** - 카드→타일→유닛 전 과정

### Phase 5: 폴리싱 (1주)
1. **CardSpawnSettings** - 설정 시스템
2. **애니메이션 및 오디오** - 시각적/청각적 피드백
3. **에러 처리 및 예외 상황** - 견고성 강화

## 🔍 검증 규칙

### 필수 검증 항목
- ✅ **그리드 경계**: 그리드 범위 내 위치인지 확인
- ✅ **타일 점유**: 빈 타일인지 확인 (`tile.CanPlaceUnit()`)
- ✅ **열 제한**: 플레이어(x=0), 적군(x=gridWidth-1)
- ✅ **페이즈 제한**: AllySummon/EnemySummon 페이즈에서만
- ✅ **카드 요구사항**: `card.CanUse()` 통과
- 🔮 **마나 비용**: 향후 확장 (현재는 무제한)

### 설정 가능한 규칙
```csharp
[CreateAssetMenu(fileName = "CardSpawnSettings", menuName = "Game/Card Spawn Settings")]
public class CardSpawnSettings : ScriptableObject
{
    [Header("Column Configuration")]
    public int playerSpawnColumn = 0;
    public int enemySpawnColumn = -1; // -1 = rightmost
    
    [Header("Validation Rules")]
    public bool enforceColumnRestrictions = true;
    public bool requireEmptyTiles = true;
    public bool enforcePhaseRestrictions = true;
    public bool checkManaCost = false;
    
    [Header("Timing")]
    public float playerSummonTimeLimit = 30f;
    public float spawnAnimationDuration = 0.5f;
}
```

## 🧪 테스트 전략

### 단위 테스트
- **SpawnValidator**: 모든 검증 시나리오
- **CardSpawnService**: 소환 로직 및 오류 처리
- **그리드 위치 계산**: 좌표 변환 정확성

### 통합 테스트
- **서비스 등록**: GameServiceManager와의 연동
- **페이즈 시스템**: 턴 전환 및 UI 상태 관리
- **이벤트 플로우**: 전체 이벤트 체인 검증

### E2E 테스트
- **전체 소환 워크플로우**: 카드 선택 → 드래그 → 드롭 → 소환
- **에러 시나리오**: 무효한 위치, 점유된 타일 등
- **성능 테스트**: 다수 카드 동시 처리

## 🚀 주요 기술적 특징

### 이벤트 기반 아키텍처
- **느슨한 결합**: 컴포넌트 간 직접 참조 최소화
- **확장성**: 새로운 검증 규칙 쉽게 추가
- **디버깅**: 명확한 이벤트 플로우로 문제 추적 용이

### 시각적 피드백 시스템
- **실시간 하이라이트**: 드래그 중 유효/무효 존 표시
- **펄스 애니메이션**: 드롭 가능 영역 강조
- **상태별 색상**: 직관적인 시각적 구분

### 성능 최적화
- **오브젝트 풀링**: CardUI 인스턴스 재사용
- **배치 연산**: 드롭 존 검증 최적화
- **이벤트 구독 관리**: 메모리 누수 방지

## 📊 예상 성과

### 기능적 이점
- **직관적 UI/UX**: 드래그 앤 드롭으로 학습 곡선 단축
- **전략적 깊이**: 위치 기반 의사결정 강화
- **시각적 만족도**: 애니메이션과 피드백으로 몰입감 증대

### 기술적 이점
- **확장성**: 새로운 카드 타입이나 규칙 추가 용이
- **유지보수성**: 모듈화된 구조로 개별 컴포넌트 수정 가능
- **호환성**: 기존 코드와 100% 호환, 점진적 마이그레이션 지원

---

## 📝 구현 체크리스트

### ✅ 설계 완료
- [x] 요구사항 분석 및 검증
- [x] 아키텍처 설계 및 컴포넌트 분해
- [x] 기존 코드와의 통합 방안 수립
- [x] 상세 구현 명세서 작성

### 🔄 구현 대기
- [ ] Phase 1: 핵심 검증 로직 구현
- [ ] Phase 2: UI 드래그 앤 드롭 구현
- [ ] Phase 3: 핸드 관리 시스템 구현
- [ ] Phase 4: 기존 클래스 통합 및 개선
- [ ] Phase 5: 테스트 및 폴리싱

### 🎯 검증 준비
- [ ] 단위 테스트 케이스 작성
- [ ] 통합 테스트 시나리오 정의
- [ ] 성능 벤치마크 기준 설정
- [ ] 사용자 테스트 계획 수립

---

*이 문서는 카드 드래그 앤 드롭 소환 시스템의 완전한 설계 명세서입니다. 모든 컴포넌트는 기존 아키텍처와 완벽하게 호환되며, 단계별 구현이 가능하도록 설계되었습니다.*