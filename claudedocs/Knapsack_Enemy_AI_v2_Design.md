# 🎯 전략적 가치 평가를 적용한 Knapsack AI 설계서 (v2.0)

## 📋 개요

이 문서는 기존 **0/1 Knapsack Problem (배낭 문제)** 기반의 적군 AI를 개선하여, 카드의 정적 데이터뿐만 아니라 **현재 필드 상황을 고려한 동적 가치 평가**를 통해 최적의 카드와 배치 위치를 동시에 결정하는 시스템의 설계 및 구현 내역을 설명합니다.

**구현 예정일**: 2025년 10월 11일
**프로젝트**: 2025-2TeamProject
**버전**: 2.0
**주요 개선사항**: 정적 가치 평가에서 **상황 인식(Context-Aware) 기반의 동적 가치 평가**로 전환

---

## 🔍 요구사항 분석

### 핵심 요구사항

1. 적군 AI가 카드를 사용할 때, 현재 필드 위 유닛들의 위치와 상태를 고려해야 한다.
2. 각 카드는 배치 가능한 모든 위치에 대해 **상황에 맞는 잠재적 가치**를 계산해야 한다.
3. **가치 계산 상세**:
   - **Summon**: 소환될 유닛의 스탯(체력, 공격력, 이동거리)을 가치에 합산한다.
   - **Heal / Damage**: 카드의 효과 범위(`AffectedRange`) 내에 실제로 영향을 받는 대상(`AffectedType`) 유닛 수를 계산하고, 이 수와 효과의 기본 값(`Value`)을 곱하여 가치를 산출한다.
4. 한 카드가 가질 수 있는 여러 위치의 잠재적 가치 중 **가장 높은 값**을 해당 카드의 이번 턴 최종 가치로 결정한다.
5. 이 최종 가치들을 Knapsack 알고리즘에 적용하여, 제한된 마나 내에서 최대의 총 가치를 내는 카드 조합을 선택한다.

---

## 🏗️ 시스템 아키텍처 (v2.0)

### 구성 요소

```
┌─────────────────────────────────────────────────────────┐
│           CardServiceManager                             │
│  (턴 페이즈 관리 및 서비스 통합)                          │
└────────────────┬────────────────────────────────────────┘
                 │
                 │ 초기화 및 호출
                 ↓
┌─────────────────────────────────────────────────────────┐
│           EnemyAIController (개선)                        │
│  • 손패 카드별 모든 유효 위치 탐색                       │
│  • 각 위치에서의 '상황 가치' 계산                        │
│  • 카드별 최고 가치 결정                                 │
│  • Knapsack 알고리즘 호출 및 카드 실행                   │
└────────┬────────────────────────────────────────────────┘
         │
         │ 카드-가치 쌍 전달
         ↓
┌─────────────────────────────────────────────────────────┐
│           KnapsackCardSelector                           │
│  • (변경 없음) 0/1 Knapsack 동적 계획법 구현             │
└─────────────────────────────────────────────────────────┘
```

### 데이터 흐름 (v2.0)

```
[EnemySummon Phase]
└─> CardServiceManager.HandleEnemySummonPhase()
    └─> EnemyAIController.ExecuteSummonPhase()
        ├─> 1. 손패의 각 카드(C)에 대해 루프 시작
        │   ├─> A. 카드(C)의 최고 가치(max_value)와 최고 위치(best_pos) 초기화
        │   ├─> B. 그리드의 모든 타일(T)에 대해 루프 시작
        │   │   ├─> i. 카드(C)를 타일(T)에 놓을 수 있는지 검증 (TargetType, TargetRange)
        │   │   ├─> ii. 유효하다면, 타일(T)에서의 상황 가치(current_value) 계산
        │   │   │   ├─> Summon: unit_stats_value
        │   │   │   └─> Damage/Heal: (hit_unit_count * effect_value)
        │   │   └─> iii. if (current_value > max_value) -> max_value = current_value, best_pos = T
        │   └─> C. 카드(C)와 계산된 최고 가치(max_value), 최고 위치(best_pos)를 리스트에 저장
        │
        ├─> 2. KnapsackCardSelector.SelectOptimalCards(가치 리스트, 현재 마나)
        │   └─> DP 테이블을 통해 최적의 카드 조합 반환
        │
        ├─> 3. 선택된 카드별 루프:
        │   ├─> 저장된 최고 위치(best_pos)에 카드 사용
        │   ├─> CardSpawnService.TryExecuteCard()
        │   └─> enemyHand에서 카드 제거
        │
        └─> 4. 페이즈 완료
```

---

## 📁 구현 파일 구조

### 신규/주요 수정 파일

#### 1. CardValueInfo.cs (신규)

**경로**: `Assets/Script/Game/AI/CardValueInfo.cs`
**네임스페이스**: `Game.AI`

**목적**: 카드별 계산된 가치와 최적 위치 정보를 캡슐화

```csharp
using UnityEngine;
using Game.Data;

namespace Game.AI
{
    /// <summary>
    /// 카드별로 계산된 상황 가치와 최적 배치 위치를 저장하는 데이터 구조
    /// </summary>
    public class CardValueInfo
    {
        /// <summary>카드 데이터</summary>
        public CardData Card { get; set; }

        /// <summary>계산된 상황 가치 (위치별 최대값)</summary>
        public int Value { get; set; }

        /// <summary>최적 배치 위치</summary>
        public Vector2Int Position { get; set; }

        /// <summary>카드 비용 (마나 코스트)</summary>
        public int Cost => Card?.ManaCost ?? 0;

        public CardValueInfo(CardData card, int value, Vector2Int position)
        {
            Card = card;
            Value = value;
            Position = position;
        }
    }
}
```

#### 2. EnemyAIController.cs (대규모 수정)

**경로**: `Assets/Script/Game/AI/EnemyAIController.cs`
**네임스페이스**: `Game.AI`

**추가 의존성**:
- `IGridState` (그리드 정보 조회)
- `IUnitService` (필드 위 유닛 정보 확인)
- `IGridController` (타일 탐색)

**주요 메서드 변경**:

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;
using Game.Managers;
using Game.Services;

namespace Game.AI
{
    /// <summary>
    /// 적군 AI 컨트롤러 - 상황 인식 기반 Knapsack 알고리즘 적용
    /// </summary>
    public class EnemyAIController : MonoBehaviour
    {
        // 의존성
        private IResourceManager resourceManager;
        private ICardSpawnService cardSpawnService;
        private IGridController gridController;
        private ISpawnValidator spawnValidator;
        private IGridState gridState;
        private IUnitService unitService;

        // 손패 관리
        private List<CardData> enemyHand = new List<CardData>();

        /// <summary>
        /// 의존성 주입 및 초기화
        /// </summary>
        public void Initialize(
            IResourceManager resourceManager,
            ICardSpawnService cardSpawnService,
            IGridController gridController,
            ISpawnValidator spawnValidator,
            IGridState gridState,
            IUnitService unitService)
        {
            this.resourceManager = resourceManager;
            this.cardSpawnService = cardSpawnService;
            this.gridController = gridController;
            this.spawnValidator = spawnValidator;
            this.gridState = gridState;
            this.unitService = unitService;

            Debug.Log("✅ EnemyAIController initialized with context-aware dependencies");
        }

        /// <summary>
        /// 적군 소환 페이즈 실행 (v2.0 - 상황 인식 가치 평가)
        /// </summary>
        public void ExecuteSummonPhase()
        {
            if (enemyHand == null || enemyHand.Count == 0)
            {
                Debug.Log("⚠️ Enemy hand is empty, skipping summon phase");
                return;
            }

            // 1. 손패의 모든 카드에 대해 최고 가치와 위치를 계산
            var cardValueInfos = new List<CardValueInfo>();
            foreach (var card in enemyHand)
            {
                var valueInfo = CalculateBestSituationalValue(card);
                if (valueInfo.Value > 0)
                {
                    cardValueInfos.Add(valueInfo);
                    Debug.Log($"💎 Card '{card.CardName}': Value={valueInfo.Value}, BestPos={valueInfo.Position}");
                }
            }

            if (cardValueInfos.Count == 0)
            {
                Debug.Log("⚠️ No valid card placements found");
                return;
            }

            // 2. Knapsack 알고리즘 실행
            int currentMana = resourceManager?.EnemyMana ?? 0;
            var selectedCardInfos = KnapsackCardSelector.SelectOptimalCards(cardValueInfos, currentMana);

            Debug.Log($"🎯 Knapsack selected {selectedCardInfos.Count} cards (Total Value: {selectedCardInfos.Sum(c => c.Value)})");

            // 3. 선택된 카드들을 최고 위치에 실행
            foreach (var info in selectedCardInfos)
            {
                bool success = cardSpawnService.TryExecuteCard(info.Card, info.Position, TeamType.Enemy);
                if (success)
                {
                    enemyHand.Remove(info.Card);
                    Debug.Log($"✅ Executed '{info.Card.CardName}' at {info.Position}");
                }
                else
                {
                    Debug.LogWarning($"❌ Failed to execute '{info.Card.CardName}' at {info.Position}");
                }
            }
        }

        /// <summary>
        /// 카드의 모든 가능한 위치를 탐색하여 최고 가치와 위치를 계산
        /// </summary>
        private CardValueInfo CalculateBestSituationalValue(CardData card)
        {
            int maxValue = 0;
            Vector2Int bestPosition = -Vector2Int.one; // 유효하지 않은 위치로 초기화

            // 그리드의 모든 타일을 순회하며 유효한 위치 탐색
            var allTiles = gridController?.GetAllTiles();
            if (allTiles == null || !allTiles.Any())
            {
                Debug.LogWarning("⚠️ No tiles available for card placement");
                return new CardValueInfo(card, 0, bestPosition);
            }

            foreach (var tile in allTiles)
            {
                var currentPosition = tile.GridPosition;

                // 1. 배치 가능 위치 검증
                if (!CanPlaceCardAtPosition(card, currentPosition))
                    continue;

                // 2. 해당 위치에서의 가치 계산
                int currentValue = CalculateValueAtPosition(card, currentPosition);

                // 3. 최고 가치 갱신
                if (currentValue > maxValue)
                {
                    maxValue = currentValue;
                    bestPosition = currentPosition;
                }
            }

            return new CardValueInfo(card, maxValue, bestPosition);
        }

        /// <summary>
        /// 특정 위치에 카드를 배치할 수 있는지 검증
        /// </summary>
        private bool CanPlaceCardAtPosition(CardData card, Vector2Int position)
        {
            if (card == null || spawnValidator == null)
                return false;

            // Summon 카드는 SpawnValidator 사용
            if (card.HasEffect(EffectType.Summon))
            {
                return spawnValidator.CanSpawnUnit(card, position);
            }

            // Spell 카드는 TargetType과 TargetRange 확인
            // (추가 검증 로직은 SpawnValidator 또는 별도 메서드로 구현)
            return true; // 임시: 모든 Spell 위치 허용
        }

        /// <summary>
        /// 특정 위치에서 카드의 총 가치 계산
        /// </summary>
        private int CalculateValueAtPosition(CardData card, Vector2Int position)
        {
            int totalValue = 0;

            if (card?.EffectDataList == null)
                return 0;

            foreach (var effect in card.EffectDataList)
            {
                totalValue += CalculateEffectValueAtPosition(card, effect, position);
            }

            return totalValue;
        }

        /// <summary>
        /// 개별 효과의 특정 위치에서의 가치 계산
        /// </summary>
        private int CalculateEffectValueAtPosition(CardData card, EffectData effect, Vector2Int position)
        {
            switch (effect.Type)
            {
                case EffectType.Summon:
                    return CalculateSummonValue(effect);

                case EffectType.Damage:
                    return CalculateDamageValue(card, effect, position);

                case EffectType.Heal:
                    return CalculateHealValue(card, effect, position);

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Summon 효과의 가치 계산: 유닛 스탯 합산
        /// </summary>
        private int CalculateSummonValue(EffectData effect)
        {
            var unit = effect.UnitToSummon;
            if (unit == null)
                return 0;

            return unit.MaxHealth + unit.AttackPower + unit.MovementRange;
        }

        /// <summary>
        /// Damage 효과의 가치 계산: 타격 가능한 적군 수 × 데미지
        /// </summary>
        private int CalculateDamageValue(CardData card, EffectData effect, Vector2Int position)
        {
            var affectedPositions = GetAffectedPositions(card, effect, position);
            int hitCount = 0;

            foreach (var pos in affectedPositions)
            {
                var unitOnTile = unitService?.GetUnitAt(pos);
                if (unitOnTile != null)
                {
                    // 적군 AI 입장에서 플레이어 유닛은 적
                    bool isEnemy = unitOnTile.TeamType == TeamType.Player;

                    if ((effect.AffectedType == AffectedType.Enemy && isEnemy) ||
                        (effect.AffectedType == AffectedType.Any))
                    {
                        hitCount++;
                    }
                }
            }

            return hitCount * effect.Value;
        }

        /// <summary>
        /// Heal 효과의 가치 계산: 치유 가능한 아군 수 × 치유량
        /// </summary>
        private int CalculateHealValue(CardData card, EffectData effect, Vector2Int position)
        {
            var affectedPositions = GetAffectedPositions(card, effect, position);
            int healCount = 0;

            foreach (var pos in affectedPositions)
            {
                var unitOnTile = unitService?.GetUnitAt(pos);
                if (unitOnTile != null)
                {
                    // 적군 AI 입장에서 적군 유닛은 아군
                    bool isAlly = unitOnTile.TeamType == TeamType.Enemy;

                    if ((effect.AffectedType == AffectedType.Ally && isAlly) ||
                        (effect.AffectedType == AffectedType.Any))
                    {
                        // 현재 체력이 최대 체력보다 낮은 경우에만 가치 부여
                        if (unitOnTile.CurrentHealth < unitOnTile.MaxHealth)
                        {
                            healCount++;
                        }
                    }
                }
            }

            return healCount * effect.Value;
        }

        /// <summary>
        /// 효과 범위 내의 영향받는 위치 목록 가져오기
        /// </summary>
        private List<Vector2Int> GetAffectedPositions(CardData card, EffectData effect, Vector2Int centerPosition)
        {
            // CardData 또는 별도 유틸리티 메서드에서 구현
            // 임시 구현: AffectedRange에 따라 주변 타일 반환
            var positions = new List<Vector2Int>();
            int range = effect.AffectedRange;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= range) // 맨해튼 거리
                    {
                        positions.Add(centerPosition + new Vector2Int(x, y));
                    }
                }
            }

            return positions;
        }

        // 손패 관리 메서드들 (기존 유지)
        public void AddCardToHand(CardData card) => enemyHand.Add(card);
        public void ClearHand() => enemyHand.Clear();
        public List<CardData> GetHand() => new List<CardData>(enemyHand);
    }
}
```

#### 3. KnapsackCardSelector.cs (수정)

**경로**: `Assets/Script/Game/AI/KnapsackCardSelector.cs`

**수정 사항**: `CardValueInfo` 객체를 받아 외부에서 계산된 가치를 사용

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 0/1 Knapsack 동적 계획법을 사용한 최적 카드 선택 알고리즘
    /// </summary>
    public static class KnapsackCardSelector
    {
        /// <summary>
        /// 주어진 마나 제약 하에서 최대 가치를 내는 카드 조합 선택
        /// </summary>
        /// <param name="availableCards">가치가 계산된 카드 정보 리스트</param>
        /// <param name="maxMana">사용 가능한 최대 마나</param>
        /// <returns>선택된 카드 정보 리스트</returns>
        public static List<CardValueInfo> SelectOptimalCards(List<CardValueInfo> availableCards, int maxMana)
        {
            if (availableCards == null || availableCards.Count == 0 || maxMana <= 0)
            {
                return new List<CardValueInfo>();
            }

            int cardCount = availableCards.Count;

            // DP 테이블 초기화: dp[i][w] = i번째 카드까지 고려, w 마나 사용 시 최대 가치
            int[,] dp = new int[cardCount + 1, maxMana + 1];

            // DP 테이블 채우기
            for (int i = 1; i <= cardCount; i++)
            {
                var card = availableCards[i - 1];
                int cost = card.Cost;
                int value = card.Value;

                for (int mana = 0; mana <= maxMana; mana++)
                {
                    // 카드를 선택하지 않는 경우
                    dp[i, mana] = dp[i - 1, mana];

                    // 카드를 선택하는 경우 (마나가 충분할 때)
                    if (mana >= cost)
                    {
                        int newValue = dp[i - 1, mana - cost] + value;
                        dp[i, mana] = Mathf.Max(dp[i, mana], newValue);
                    }
                }
            }

            // 역추적하여 선택된 카드 찾기
            var selectedCards = new List<CardValueInfo>();
            int remainingMana = maxMana;

            for (int i = cardCount; i > 0; i--)
            {
                // 이 카드를 선택한 경우
                if (dp[i, remainingMana] != dp[i - 1, remainingMana])
                {
                    var selectedCard = availableCards[i - 1];
                    selectedCards.Add(selectedCard);
                    remainingMana -= selectedCard.Cost;
                }
            }

            selectedCards.Reverse(); // 원래 순서로 복원
            return selectedCards;
        }
    }
}
```

#### 4. CardServiceManager.cs (수정)

**경로**: `Assets/Script/Game/Managers/CardServiceManager.cs`

**수정 사항**: `EnemyAIController` 초기화 시 추가 의존성 주입

```csharp
// InitializeCardServices() 메서드 내부
private void InitializeCardServices()
{
    // ... 기존 초기화 코드 ...

    if (enemyAIController != null)
    {
        // 추가 의존성 가져오기
        var gridState = gridManager?.GetGridState();
        var unitService = gameServiceManager?.GetService<IUnitService>();

        // 의존성 주입
        enemyAIController.Initialize(
            resourceManager,
            cardSpawnService,
            gridController,
            spawnValidator,
            gridState,
            unitService
        );

        Debug.Log("💉 EnemyAIController dependencies (with context-aware services) injected");
    }
    else
    {
        Debug.LogWarning("⚠️ EnemyAIController is null during initialization");
    }
}
```

---

## 🎮 실행 흐름 예시 (v2.0)

### 시나리오: 적군 마나 4, 필드에 플레이어 유닛 2기 밀집

**초기 상태**:

```
enemyMana = 4
enemyHand = [ "고블린 소환"(2M), "파이어볼"(4M, Damage: 20, Range: 1) ]
Field:
  - PlayerUnit at (3,3): HP=30
  - PlayerUnit at (3,4): HP=25
```

**가치 평가 실행**:

1. **"고블린 소환" 카드 평가**:
   - **위치 탐색**: 적군 진영의 빈 타일들 (예: (7,3), (7,4), (7,5))
   - **위치 (7,3)에서의 가치 계산**:
     - Summon Effect: `Health(10) + Attack(5) + Move(3)` = **18**
   - **다른 위치**: 동일한 유닛이므로 가치 동일 (**18**)
   - **최종 가치**: 18, **최적 위치**: (7,3) (첫 번째 유효 위치)

2. **"파이어볼" 카드 평가**:
   - **위치 탐색**: 그리드 전체 (8x8 = 64 타일)
   - **위치 (4,3)에서의 가치 계산**:
     - Damage Effect (Range: 1, AffectedType: Enemy)
     - 영향 범위: (3,3), (4,3), (5,3), (4,2), (4,4)
     - 타격 대상: PlayerUnit at (3,3), PlayerUnit at (3,4) → **2기**
     - 가치: `hitCount(2) × value(20)` = **40**
   - **위치 (3,3)에서의 가치 계산**:
     - 영향 범위: (2,3), (3,3), (4,3), (3,2), (3,4)
     - 타격 대상: PlayerUnit at (3,3), PlayerUnit at (3,4) → **2기**
     - 가치: **40**
   - **위치 (5,5)에서의 가치 계산**:
     - 영향 범위: (4,5), (5,5), (6,5), (5,4), (5,6)
     - 타격 대상: 없음 → **0**
   - **최종 가치**: 40, **최적 위치**: (4,3) (첫 번째 최대 가치 위치)

**Knapsack 입력**:

```csharp
cardValueInfos = [
  { Card: "고블린 소환", Cost: 2, Value: 18, Position: (7,3) },
  { Card: "파이어볼", Cost: 4, Value: 40, Position: (4,3) }
]
maxMana = 4
```

**Knapsack DP 테이블**:

```
      Mana:  0   1   2   3   4
Card 0:      0   0   0   0   0
고블린(2M,18): 0   0  18  18  18
파이어볼(4M,40):0   0  18  18  40
```

**Knapsack 결과**:
- 선택된 카드: `[ { Card: "파이어볼", Cost: 4, Value: 40, Position: (4,3) } ]`
- 총 마나 사용: 4
- 총 가치: 40

**카드 실행**:

```csharp
CardSpawnService.TryExecuteCard("파이어볼", (4,3), TeamType.Enemy)
결과: ✅ 플레이어 유닛 2기에 각 20 데미지 → AI가 상황에 맞는 최적의 선택을 함.
```

**실행 결과**:
- PlayerUnit at (3,3): HP 30 → 10
- PlayerUnit at (3,4): HP 25 → 5
- AI는 "고블린 소환" 대신 더 높은 가치의 "파이어볼"을 선택하여 적에게 최대 피해를 입힘.

---

## 🎯 알고리즘 복잡도 분석 (v2.0)

### 시간 복잡도

- **가치 평가 단계**: O(n × T × (U + E))
  - n: 손패 카드 개수 (예: 7)
  - T: 그리드 타일 총 개수 (예: 8×8 = 64)
  - U: 효과 범위 내 최대 유닛 수 (예: ~5)
  - E: 카드당 효과 개수 (예: ~2)
  - **실제 계산량**: 7 × 64 × 7 ≈ **3,136회** (여전히 실시간 처리 가능)

- **Knapsack 알고리즘**: O(n × m)
  - n: 카드 개수 (7)
  - m: 최대 마나 (예: 10)
  - **실제 계산량**: 7 × 10 = **70회**

- **전체 복잡도**: **O(n × T × U)** 가 지배적
  - 이전 정적 평가 대비 계산량 증가 (~64배), 하지만 현대 CPU에서는 밀리초 단위로 처리 가능

### 공간 복잡도

- **DP 테이블**: O(n × m) = 7 × 10 = **70개** 정수
- **가치 정보 리스트**: O(n) = 7개 `CardValueInfo` 객체
- **영향 범위 임시 리스트**: O(U) = ~5개 `Vector2Int` (재사용)
- **전체**: O(n × m) ≈ **수백 바이트** (미미한 메모리 사용량)

### 최적화 가능성

1. **타일 탐색 최적화**:
   - 모든 타일을 탐색하는 대신, 카드 타입별로 유효 범위만 탐색
   - Summon: 적군 진영 타일만 탐색
   - Spell: TargetRange 내 타일만 탐색
   - **개선 후**: O(n × T') where T' << T

2. **조기 종료**:
   - 이미 찾은 최대 가치가 이론적 최대값에 도달하면 탐색 중단

3. **병렬 처리**:
   - 각 카드의 가치 평가를 병렬로 수행 (멀티스레딩)
   - Unity Job System 활용 가능

---

## 🔧 확장 가능성 (v2.0)

새로운 시스템은 AI의 지능을 향상시킬 강력한 기반을 제공합니다.

### 1. 정교한 가치 평가

#### 타겟 우선순위 시스템

```csharp
// 체력이 낮은 적에게 더 높은 가치 부여 (킬 확정)
private int CalculateDamageValueWithPriority(CardData card, EffectData effect, Vector2Int position)
{
    var affectedPositions = GetAffectedPositions(card, effect, position);
    int totalValue = 0;

    foreach (var pos in affectedPositions)
    {
        var unitOnTile = unitService?.GetUnitAt(pos);
        if (unitOnTile != null && unitOnTile.TeamType == TeamType.Player)
        {
            int damageValue = effect.Value;

            // 킬 가능한 적: 가치 2배
            if (unitOnTile.CurrentHealth <= damageValue)
            {
                totalValue += damageValue * 2;
            }
            // 고위험 적 (높은 공격력): 가치 1.5배
            else if (unitOnTile.AttackPower > 15)
            {
                totalValue += Mathf.RoundToInt(damageValue * 1.5f);
            }
            // 일반 적
            else
            {
                totalValue += damageValue;
            }
        }
    }

    return totalValue;
}
```

#### 위치 가중치 시스템

```csharp
// 전략적 요충지에 유닛 소환 시 보너스
private int CalculateSummonValueWithPosition(EffectData effect, Vector2Int position)
{
    int baseValue = CalculateSummonValue(effect);

    // 전방 라인 (적 진영에 가까움): +50% 보너스
    if (position.x >= 5)
    {
        baseValue = Mathf.RoundToInt(baseValue * 1.5f);
    }

    // 중앙 위치 (기동성 유리): +25% 보너스
    if (position.y >= 3 && position.y <= 5)
    {
        baseValue = Mathf.RoundToInt(baseValue * 1.25f);
    }

    return baseValue;
}
```

### 2. AI 성향(Personality) 시스템

```csharp
public enum AIPersonality
{
    Aggressive,  // 공격적: Damage 효과 선호
    Defensive,   // 방어적: Summon/Heal 선호
    Balanced     // 균형: 상황에 맞게
}

public class EnemyAIController : MonoBehaviour
{
    [SerializeField] private AIPersonality personality = AIPersonality.Balanced;

    private int CalculateEffectValueAtPosition(CardData card, EffectData effect, Vector2Int position)
    {
        int baseValue = 0;

        switch (effect.Type)
        {
            case EffectType.Damage:
                baseValue = CalculateDamageValue(card, effect, position);
                // 공격적 AI: Damage 가치 1.5배
                if (personality == AIPersonality.Aggressive)
                    baseValue = Mathf.RoundToInt(baseValue * 1.5f);
                break;

            case EffectType.Summon:
                baseValue = CalculateSummonValue(effect);
                // 방어적 AI: Summon 가치 1.5배
                if (personality == AIPersonality.Defensive)
                    baseValue = Mathf.RoundToInt(baseValue * 1.5f);
                break;

            case EffectType.Heal:
                baseValue = CalculateHealValue(card, effect, position);
                // 방어적 AI: Heal 가치 2배
                if (personality == AIPersonality.Defensive)
                    baseValue = Mathf.RoundToInt(baseValue * 2f);
                break;
        }

        return baseValue;
    }
}
```

### 3. 생존성 고려 시스템

```csharp
// 치유 시 잃은 체력 비율에 따라 가치 차등 부여
private int CalculateHealValueWithSurvivability(CardData card, EffectData effect, Vector2Int position)
{
    var affectedPositions = GetAffectedPositions(card, effect, position);
    int totalValue = 0;

    foreach (var pos in affectedPositions)
    {
        var unitOnTile = unitService?.GetUnitAt(pos);
        if (unitOnTile != null && unitOnTile.TeamType == TeamType.Enemy)
        {
            int missingHealth = unitOnTile.MaxHealth - unitOnTile.CurrentHealth;
            if (missingHealth > 0)
            {
                int healAmount = Mathf.Min(effect.Value, missingHealth);

                // 위기 상황 (HP < 30%): 가치 3배
                float healthRatio = (float)unitOnTile.CurrentHealth / unitOnTile.MaxHealth;
                if (healthRatio < 0.3f)
                {
                    totalValue += healAmount * 3;
                }
                // 보통 상황 (HP < 70%): 가치 1.5배
                else if (healthRatio < 0.7f)
                {
                    totalValue += Mathf.RoundToInt(healAmount * 1.5f);
                }
                // 가벼운 부상: 기본 가치
                else
                {
                    totalValue += healAmount;
                }
            }
        }
    }

    return totalValue;
}
```

### 4. 미래 예측 시스템 (Advanced)

```csharp
// 한 수 앞을 내다보는 AI (간단한 Minimax)
private int CalculateValueWithPrediction(CardData card, Vector2Int position)
{
    // 현재 턴 가치
    int currentValue = CalculateValueAtPosition(card, position);

    // 다음 턴 예측: 플레이어의 가능한 대응 시뮬레이션
    // (복잡도 증가: O(n × T × T') - 실전 적용 시 제한적 탐색 필요)
    int predictedCounterValue = SimulatePlayerResponse(card, position);

    // 최종 가치: 현재 이익 - 예상 손실
    return currentValue - predictedCounterValue;
}
```

---

## ✅ 구현 체크리스트

### Phase 1: 기본 구조 구현

- [ ] `CardValueInfo.cs` 클래스 생성
- [ ] `EnemyAIController.cs` 의존성 주입 확장
- [ ] `CalculateBestSituationalValue()` 메서드 구현
- [ ] `CanPlaceCardAtPosition()` 검증 로직 구현
- [ ] `CalculateValueAtPosition()` 통합 메서드 구현

### Phase 2: 효과별 가치 계산

- [ ] `CalculateSummonValue()` 구현
- [ ] `CalculateDamageValue()` 구현
- [ ] `CalculateHealValue()` 구현
- [ ] `GetAffectedPositions()` 유틸리티 메서드 구현

### Phase 3: Knapsack 통합

- [ ] `KnapsackCardSelector.SelectOptimalCards()` 수정
- [ ] `ExecuteSummonPhase()` 전체 흐름 통합
- [ ] `CardServiceManager` 의존성 주입 업데이트

### Phase 4: 테스트 및 검증

- [ ] 단위 테스트: 각 효과별 가치 계산 검증
- [ ] 통합 테스트: 전체 AI 실행 흐름 검증
- [ ] 시나리오 테스트: 다양한 필드 상황에서 AI 행동 확인
- [ ] 성능 테스트: 프레임 드롭 없이 실시간 실행 확인

### Phase 5: 확장 기능 (Optional)

- [ ] 타겟 우선순위 시스템 구현
- [ ] AI 성향(Personality) 시스템 구현
- [ ] 생존성 고려 로직 구현
- [ ] 타일 탐색 최적화 (유효 범위만 탐색)

---

## 📊 예상 성능 지표

### 실행 시간 (Intel i5-9400F 기준)

| 시나리오 | 손패 카드 | 필드 유닛 | 예상 실행 시간 |
|---------|----------|----------|---------------|
| 간단한 상황 | 3장 | 2기 | ~1ms |
| 보통 상황 | 5장 | 5기 | ~3ms |
| 복잡한 상황 | 7장 | 10기 | ~8ms |
| 최악의 경우 | 10장 | 15기 | ~20ms |

**목표**: 모든 상황에서 **< 16ms** (60 FPS 유지)

### 메모리 사용량

- **DP 테이블**: ~400 bytes (10장 × 10 마나)
- **가치 정보 리스트**: ~280 bytes (10장 × 28 bytes/object)
- **영향 범위 리스트**: ~80 bytes (재사용)
- **전체**: **< 1KB** (무시할 수 있는 수준)

---

## 🐛 알려진 제한사항 및 향후 개선

### 현재 제한사항

1. **전역 최적화 부재**: Knapsack은 개별 카드의 가치만 고려하며, 카드 간 시너지를 평가하지 못함.
   - 예: "체력 증가 버프" + "강력한 유닛 소환"의 조합 효과 미고려

2. **단일 턴 평가**: 한 턴만 내다보며, 장기 전략 부재.
   - 예: 마나를 아껴서 다음 턴에 강력한 카드를 쓰는 전략 불가

3. **정적 가중치**: 모든 스탯에 동일한 가중치 (HP=1, ATK=1, MOVE=1).
   - 실제로는 상황에 따라 HP가 더 중요할 수 있음.

### 향후 개선 방향

1. **카드 시너지 평가**: 선택된 카드 조합의 추가 가치 계산.
2. **미래 턴 시뮬레이션**: Minimax 또는 Monte Carlo Tree Search 적용.
3. **동적 가중치**: 게임 상황(초반/중반/후반)에 따라 스탯 가중치 조정.
4. **학습 기반 AI**: 플레이어 패턴 학습 및 대응 전략 개발.

---

## 📝 관련 문서

- [Knapsack_Enemy_AI_Implementation.md](./Knapsack_Enemy_AI_Implementation.md) - v1.0 기본 구현
- [Card System Architecture](../docs/CardSystemArchitecture.md) - 카드 시스템 전체 구조
- [Spawn Validator Documentation](../docs/SpawnValidatorDoc.md) - 배치 검증 규칙

---

## 📞 문의 및 지원

**작성자**: Claude Code AI Assistant
**작성일**: 2025년 10월 11일
**버전**: 2.0
**문서 상태**: 설계 완료 - 구현 대기 중

---

## 🎓 설계 요약

이 v2.0 설계는 다음을 달성합니다:

✅ **상황 인식**: 필드 위 유닛 배치를 고려한 동적 가치 평가
✅ **최적 위치 결정**: 각 카드를 가장 효과적인 위치에 사용
✅ **효율적 자원 관리**: Knapsack 알고리즘으로 마나 최적화
✅ **확장 가능한 구조**: AI 성향, 우선순위, 예측 시스템 추가 용이
✅ **실시간 성능**: 60 FPS 유지 가능한 계산 복잡도

이 설계를 따라 구현하면, **전략적으로 사고하는 지능형 AI**를 갖춘 게임을 완성할 수 있습니다.
