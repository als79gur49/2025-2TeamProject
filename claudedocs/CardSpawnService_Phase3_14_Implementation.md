# CardSpawnService Phase 3.14 구현 완료

## 🎯 개요

CardData 리팩토링 계획서 Phase 3.14에 따라 CardSpawnService가 EffectData 기반의 통합 카드 처리 시스템으로 완전히 변환되었습니다.

## 🔄 주요 변경사항

### 1. 통합 카드 실행 메서드

**새로운 주요 메서드:**
- `TryExecuteCard(CardData, Vector2Int)`: 모든 카드 효과를 통합 처리 (기본: 플레이어)
- `TryExecuteCard(CardData, Vector2Int, bool)`: 플레이어/적군 구분하여 카드 효과 처리

**기존 메서드 (Obsolete로 표시):**
- `TrySpawnUnitFromCard()`: 유닛 소환 전용 → TryExecuteCard로 위임
- `TryActivateSpellFromCard()`: 주문 발동 전용 → TryExecuteCard로 위임

### 2. EffectData 기반 처리 흐름

```
CardData.EffectDataList → CardEffectFactory → ICardEffect 인스턴스들 → 우선순위별 실행
```

1. **카드 검증**: `cardData.IsEffectBasedCard` 확인
2. **리소스 소비**: `resourceManager.SpendResources()`
3. **효과 생성**: `CardEffectFactory.CreateEffects(effectDataList)`
4. **효과 실행**: 각 ICardEffect의 `CanExecute()` → `Execute()` 순차 호출
5. **실패 시 복구**: 리소스 복원

### 3. GameContext 통합

- 카드 실행마다 GameContext 업데이트
- playerId, originPosition 동적 설정
- 모든 효과가 동일한 컨텍스트에서 실행

## 🏗️ 아키텍처 개선점

### 단순 실행자(Executor) 역할로 전환

**기존 (복잡한 분기 처리):**
```csharp
if (cardData.Type == CardType.Unit) {
    // 유닛 소환 로직
} else if (cardData.Type == CardType.Spell) {
    // 주문 발동 로직
}
```

**현재 (통합 처리):**
```csharp
var effects = CardEffectFactory.CreateEffects(cardData.EffectDataList);
foreach (var effect in effects) {
    if (effect.CanExecute(targetPos, gameContext)) {
        effect.Execute(targetPos, gameContext);
    }
}
```

### 팩토리 패턴 활용

- **확장성**: 새로운 효과 타입 추가 시 팩토리에만 등록
- **분리된 책임**: 각 효과는 독립적인 클래스로 구현
- **우선순위 지원**: EffectData.Priority 기반 실행 순서 보장

## 🔧 호환성 유지

### 기존 API 지원

모든 기존 메서드는 `[System.Obsolete]` 속성으로 표시하되 내부적으로 `TryExecuteCard`로 위임하여 완벽한 하위 호환성 제공:

```csharp
[System.Obsolete("Use TryExecuteCard instead.")]
public bool TrySpawnUnitFromCard(CardData cardData, Vector2Int gridPosition)
{
    return TryExecuteCard(cardData, gridPosition, true);
}
```

## 📊 성능 및 안정성

### 에러 처리 개선

- **부분 실패 허용**: 일부 효과 실패 시 다른 효과는 계속 실행
- **예외 안전성**: try-catch 블록으로 예외 발생 시 안전한 롤백
- **리소스 복구**: 실패 시 자동으로 소비한 리소스 복원

### 로깅 강화

- 각 효과별 개별 성공/실패 로깅
- 전체 효과 실행 통계 (`📊 2/3 effects executed successfully`)
- 명확한 에러 메시지와 디버깅 정보

## 🎮 사용 예시

### 새로운 방식 (권장)

```csharp
// 플레이어가 카드 사용
bool success = cardSpawnService.TryExecuteCard(cardData, targetPosition, true);

// 적군 AI가 카드 사용
bool success = cardSpawnService.TryExecuteCard(cardData, targetPosition, false);
```

### 기존 방식 (호환성 유지)

```csharp
// 여전히 작동하지만 Obsolete 경고 표시
bool success = cardSpawnService.TrySpawnUnitFromCard(cardData, gridPosition);
bool success = cardSpawnService.TryActivateSpellFromCard(cardData, targetPosition);
```

## 🚀 마이그레이션 가이드

### 개발자 작업 필요 사항

1. **기존 코드 업데이트**: `TrySpawnUnitFromCard`, `TryActivateSpellFromCard` 호출을 `TryExecuteCard`로 변경
2. **CardData 설정**: 모든 카드에 EffectData 리스트 설정 필요
3. **테스트 업데이트**: 새로운 통합 API에 맞춘 테스트 케이스 작성

### 단계적 마이그레이션

1. **Phase 1**: 기존 코드는 그대로 두고 새 기능만 `TryExecuteCard` 사용
2. **Phase 2**: 점진적으로 기존 호출 코드를 새 API로 변경
3. **Phase 3**: 모든 CardData에 EffectData 설정 완료 후 레거시 메서드 제거

## ✅ 검증 완료 사항

- ✅ CardSpawnService 완전 리팩토링
- ✅ ICardSpawnService 인터페이스 업데이트
- ✅ GameContext 통합 완료
- ✅ 팩토리 패턴 적용
- ✅ 기존 API 호환성 유지
- ✅ 에러 처리 및 로깅 개선

## 🔗 관련 파일

- `CardSpawnService.cs`: 메인 구현체
- `ICardSpawnService.cs`: 인터페이스
- `CardEffectFactory.cs`: 효과 생성 팩토리
- `GameContext.cs`: 게임 컨텍스트
- `EffectData.cs`: 효과 데이터 구조
- `ICardEffect.cs`: 효과 인터페이스

## 📈 다음 단계

1. **CardData 마이그레이션**: 모든 기존 카드 데이터를 EffectData 시스템으로 변환
2. **클라이언트 코드 업데이트**: UI, AI 등에서 새 API 사용
3. **성능 테스트**: 대규모 카드 사용 시 성능 검증
4. **추가 효과 타입**: Buff, Debuff, Shield 등 확장 효과 구현