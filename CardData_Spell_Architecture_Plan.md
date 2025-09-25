# CardData Spell Property Architecture Plan

## 🎯 문제 정의

### 현재 아키텍처 이슈
**문제점:** `CardSpawnService.ExecuteSpellEffect()` 내부에서 다음 속성들을 참조합니다:
- `cardData.SpellType`
- `cardData.SpellEffectValue` 
- `cardData.SpellRange`

하지만 이러한 속성들은 `CardData` 클래스에 존재하지 않으며, 별도의 `Spell` 클래스에서 관리되고 있습니다.

**근본 원인:** CardData(ScriptableObject 기반)와 Spell 클래스(MonoBehaviour 기반) 간의 아키텍처 불일치

## 🏗️ 해결방안: Option 1 - CardData 확장 (권장)

### 설계 원칙
- **단일 진실 공급원:** CardData가 모든 카드 데이터의 중심이 되어야 함
- **일관성:** 기존 UnitData 처리 방식과 동일한 패턴 적용
- **단순성:** 복잡한 참조 관계 제거

### 📋 구현 세부사항

#### 1. CardData.cs 확장

```csharp
[Header("주문 관련 (주문 카드인 경우)")]
[SerializeField] private SpellType spellType = SpellType.Damage;
[SerializeField] private int spellEffectValue = 0;
[SerializeField] private float spellRange = 0f;
[SerializeField] private float spellCooldown = 0f;
[SerializeField] private GameObject spellEffectPrefab;

// 읽기 전용 속성 추가
public SpellType SpellType => spellType;
public int SpellEffectValue => spellEffectValue;
public float SpellRange => spellRange;
public float SpellCooldown => spellCooldown;
public GameObject SpellEffectPrefab => spellEffectPrefab;

// 주문 데이터 유효성 검사
public bool HasValidSpellData => cardType == CardType.Spell && spellEffectValue > 0;
```

#### 2. OnValidate() 메서드 확장

```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    // 기존 검증 로직...
    
    // 주문 카드 검증 추가
    if (cardType == CardType.Spell)
    {
        spellEffectValue = Mathf.Max(0, spellEffectValue);
        spellRange = Mathf.Max(0f, spellRange);
        spellCooldown = Mathf.Max(0f, spellCooldown);
    }
    else
    {
        // 주문이 아닌 카드의 주문 데이터 초기화
        spellType = SpellType.Damage;
        spellEffectValue = 0;
        spellRange = 0f;
        spellCooldown = 0f;
        spellEffectPrefab = null;
    }
    
    // 유닛 카드가 아니면 unitToSummon을 null로 설정 (기존)
    if (cardType != CardType.Unit)
    {
        unitToSummon = null;
    }
}
#endif
```

#### 3. 팩토리 메서드 추가

```csharp
/// <summary>
/// 주문 카드 생성을 위한 팩토리 메서드 (확장)
/// </summary>
public static CardData CreateSpellCard(string name, string desc, int manaCost, int actionCost, 
    SpellType spellType, int effectValue, float range = 0f, float cooldown = 0f)
{
    var card = CreateInstance<CardData>();
    card.cardName = name;
    card.description = desc;
    card.cardType = CardType.Spell;
    card.manaCost = manaCost;
    card.actionCost = actionCost;
    card.spellType = spellType;
    card.spellEffectValue = effectValue;
    card.spellRange = range;
    card.spellCooldown = cooldown;
    
    return card;
}
```

#### 4. 유효성 검사 메서드 확장

```csharp
/// <summary>
/// 데이터 유효성 검증 (확장)
/// </summary>
public bool IsValid()
{
    bool baseValid = !string.IsNullOrEmpty(cardName) &&
                    manaCost >= 0 &&
                    actionCost >= 0 &&
                    range >= 0 &&
                    areaOfEffect >= 0 &&
                    maxCopiesInDeck > 0;

    // 카드 타입별 추가 검증
    bool typeValid = cardType switch
    {
        CardType.Unit => unitToSummon != null,
        CardType.Spell => spellEffectValue > 0,
        _ => true
    };

    return baseValid && typeValid;
}
```

#### 5. 주문 관련 헬퍼 메서드 추가

```csharp
/// <summary>
/// 주문 타입별 설명 텍스트 생성
/// </summary>
public string GetSpellDescription()
{
    if (!IsSpellCard) return "";
    
    return spellType switch
    {
        SpellType.Damage => $"{spellEffectValue} 피해를 입힙니다",
        SpellType.Heal => $"{spellEffectValue} 체력을 회복시킵니다",
        SpellType.Buff => $"{spellEffectValue}만큼 강화합니다",
        SpellType.Debuff => $"{spellEffectValue}만큼 약화시킵니다",
        SpellType.Shield => $"{spellEffectValue} 보호막을 생성합니다",
        SpellType.Teleport => $"최대 {spellRange} 거리만큼 이동시킵니다",
        SpellType.Summon => $"{spellEffectValue}개의 유닛을 소환합니다",
        _ => "알 수 없는 주문 효과"
    };
}

/// <summary>
/// 주문 범위 유효성 검사
/// </summary>
public bool IsSpellInRange(Vector2Int casterPosition, Vector2Int targetPosition)
{
    if (!IsSpellCard) return false;
    if (spellRange <= 0) return true; // 범위 제한 없음
    
    float distance = Vector2Int.Distance(casterPosition, targetPosition);
    return distance <= spellRange;
}
```

### 🔄 CardSpawnService 호환성

기존 `CardSpawnService.ExecuteSpellEffect()` 코드는 **변경 불필요:**

```csharp
// 이제 정상적으로 동작
SpellType spellType = cardData.SpellType;     // ✅
int effectValue = cardData.SpellEffectValue;  // ✅
float effectRange = cardData.SpellRange;      // ✅
```

### 📈 장점 분석

#### ✅ 주요 이점
1. **단일 데이터 소스:** 모든 카드 데이터가 CardData에 집중
2. **코드 단순화:** CardSpawnService에서 복잡한 참조 불필요
3. **일관성:** UnitData 처리 방식과 동일한 패턴
4. **Unity 통합:** Inspector에서 직관적인 편집 가능
5. **성능 향상:** 추가 객체 참조 해결 과정 불필요

#### ⚠️ 고려사항
1. **CardData 크기 증가:** 필드 추가로 메모리 사용량 소폭 증가
2. **기존 데이터 마이그레이션:** 기존 주문 카드 데이터 이전 필요
3. **Spell 클래스 재정의:** 기존 Spell 클래스의 역할 재검토 필요

### 🛠️ 마이그레이션 계획

#### Phase 1: CardData 확장
1. 주문 관련 필드 및 속성 추가
2. 검증 메서드 확장
3. 팩토리 메서드 구현

#### Phase 2: 기존 코드 호환성 확인
1. CardSpawnService 동작 테스트
2. 기존 주문 시스템과의 통합 검증

#### Phase 3: 데이터 마이그레이션
1. 기존 Spell 객체에서 CardData로 데이터 이전
2. ScriptableObject 에셋 업데이트
3. 테스트 및 검증

#### Phase 4: 리팩토링
1. 불필요한 Spell 클래스 참조 제거
2. SpellEffectFactory와의 통합 최적화
3. 최종 테스트 및 문서화

### 🧪 테스트 전략

#### 단위 테스트
```csharp
[Test]
public void CardData_SpellProperties_ShouldBeValid()
{
    var spellCard = CardData.CreateSpellCard("Fire Bolt", "Burns target", 2, 1, 
        SpellType.Damage, 50, 3f);
    
    Assert.AreEqual(SpellType.Damage, spellCard.SpellType);
    Assert.AreEqual(50, spellCard.SpellEffectValue);
    Assert.AreEqual(3f, spellCard.SpellRange);
    Assert.IsTrue(spellCard.HasValidSpellData);
}
```

#### 통합 테스트
```csharp
[Test]
public void CardSpawnService_ExecuteSpellEffect_ShouldUseCardDataProperties()
{
    // CardSpawnService에서 cardData.SpellType 등이 정상 동작하는지 확인
}
```

### 📚 구현 우선순위

1. **High Priority:** CardData 필드 및 속성 추가
2. **High Priority:** OnValidate() 및 IsValid() 확장
3. **Medium Priority:** 팩토리 메서드 구현
4. **Medium Priority:** 헬퍼 메서드 추가
5. **Low Priority:** 기존 Spell 클래스 리팩토링

### 🎯 최종 권고사항

Option 1 구현을 통해:
- **일관된 아키텍처** 달성
- **코드 복잡성 감소**
- **유지보수성 향상**
- **Unity 워크플로우 최적화**

이 접근법은 CardData를 중심으로 한 단순하고 확장 가능한 구조를 제공하며, 향후 새로운 카드 타입 추가 시에도 동일한 패턴을 적용할 수 있습니다.