# CardData 리팩토링 계획서

## 📋 프로젝트 개요

### 목표
CardData 클래스를 효과 기반 아키텍처로 전환하여 팩토리 패턴을 통한 확장 가능한 카드 시스템 구현

### 핵심 변경사항
- CardType → EffectType으로 전환 (Damage, Heal, Summon)
- TargetType 역할 변경: 배치 가능 위치 검증
- 새로운 타겟팅 시스템: AffectedType + Range 기반 검증
- 레거시 주문 시스템 제거 및 통합

## 🎯 현재 구조 분석

### 기존 시스템
```csharp
// 현재 CardType
public enum CardType
{
    Unit,       // 유닛 소환
    Spell,      // 주문
}

// 현재 TargetType (주문 대상 지정)
public enum TargetType
{
    None, Self, Ally, Enemy, Any, Ground, AllAllies, AllEnemies, All
}

// 현재 SpellType (주문 효과 종류)
public enum SpellType
{
    Damage, Heal, Buff, Debuff, Shield, Teleport, Summon
}
```

### 문제점
- Unit 카드와 SpellType.Summon 중복
- 타겟팅 로직이 배치와 효과에 혼재
- 확장성 부족한 스펠 시스템

## 🔄 새로운 구조 설계

### 1. EffectType (기존 CardType 대체)
```csharp
public enum EffectType
{
    Damage,     // 대상에게 데미지를 준다
    Heal,       // 대상을 회복시킨다
    Summon      // 대상 위치에 유닛을 소환한다
}
```

### 2. TargetType (배치 가능 위치)
```csharp
public enum TargetType
{
    None,       // 타일이 없는 곳에서도 가능
    Ally,       // 아군 위치에서만 소환 가능
    Enemy,      // 적군 위치에서만 소환 가능
    Any,        // 적군, 아군 모두 가능
    Ground      // 타일이 있는 곳 어디든 가능
}
```

### 3. TargetRange (배치 거리 제한)
- **플레이어**: 가장 왼쪽 기준 최대 거리
- **적군**: 가장 오른쪽 기준 최대 거리
- **값**: -1(거리 무관), 0+(해당 거리까지)

### 4. AffectedType (효과 대상)
```csharp
public enum AffectedType
{
    None,       // 아무에게도 영향 없음
    Ally,       // 아군에게만 영향
    Enemy,      // 적군에게만 영향
    Any         // 모두에게 영향
}
```

### 5. AffectedRange (효과 범위)
- 목표 위치에서 주변 거리까지 영향
- 0: 단일 대상, 1+: 범위 효과

## 🏗️ 아키텍처 설계

### Factory Pattern 구조
```csharp
public interface ICardEffect
{
    bool CanExecute(Vector2Int targetPos, GameContext context);
    void Execute(Vector2Int targetPos, GameContext context);
}

public class DamageEffect : ICardEffect { }
public class HealEffect : ICardEffect { }
public class SummonEffect : ICardEffect { }
```

### 타겟팅 시스템 분리
- **TargetType + TargetRange**: 배치 가능성 검증
- **AffectedType + AffectedRange**: 효과 적용 대상

## 📋 상세 작업 계획 (18단계)

### Phase 1: 준비 및 설계 (1-3)
1. **백업 및 브랜치 생성**
   - 현재 CardData 시스템 백업
   - feature/carddata-refactoring 브랜치 생성

2. **아키텍처 설계**
   - ICardEffect 인터페이스 설계
   - 팩토리 패턴 구조 계획

3. **마이그레이션 전략 수립**
   - 기존 카드 데이터 변환 로직 설계
   - 호환성 유지 방안 검토

### Phase 2: 핵심 리팩토링 (4-12)
4. **EffectType 도입**
   - CardType → EffectType 변경
   - Damage, Heal, Summon 정의

5. **TargetType 의미 변경**
   - 배치 가능 위치 검증으로 역할 변경
   - None, Ally, Enemy, Any, Ground 재정의

6. **TargetRange 필드 추가**
   - int 타입 필드 추가
   - -1(무제한), 0+(거리 제한) 구현

7. **AffectedType 추가**
   - 새로운 enum 생성
   - None, Ally, Enemy, Any 정의

8. **AffectedRange 필드 추가**
   - 효과 범위 계산용 int 필드
   - 기존 areaOfEffect와 통합 고려

9. **레거시 필드 제거**
   - SpellType enum 제거
   - unitToSummon 필드 제거
   - 주문 관련 필드들 정리 (spellEffectValue, spellRange, spellCooldown, spellEffectPrefab)

10. **팩토리 패턴 구현**
    - DamageEffect, HealEffect, SummonEffect 클래스 구현
    - CardData에서 effect 생성 팩토리 메서드 추가

11. **IsValidTarget() 업데이트**
    - 새로운 TargetType과 TargetRange 검증 로직
    - 플레이어/적군 위치 기반 거리 계산

12. **GetAffectedUnits() 메서드 생성**
    - AffectedType과 AffectedRange 기반
    - 효과 적용 대상 유닛 리스트 반환

### Phase 3: 시스템 통합 (13-16)
13. **CardDataMigrator 생성**
    - 기존 카드 데이터 변환 유틸리티
    - Unit → Summon, Spell → Damage/Heal 매핑

14. **CardSpawnService 업데이트**
    - 새로운 효과 시스템 적용
    - 기존 주문 처리 로직 제거

15. **GridController 연동**
    - TargetRange 검증을 위한 위치 계산
    - 플레이어/적군 기준점 연동

16. **UI 시스템 업데이트**
    - GetDetailedDescription() 메서드 수정
    - 새로운 카드 구조 반영

### Phase 4: 테스팅 및 검증 (17-18)
17. **단위 테스트 작성**
    - 모든 새로운 검증 로직 테스트
    - TargetRange, AffectedRange 계산 검증
    - 팩토리 패턴 동작 테스트

18. **통합 테스트 작성**
    - 실제 게임 시나리오 테스트
    - 마이그레이션 테스트
    - 성능 테스트

## ⚠️ 주요 고려사항

### Breaking Changes
- **CardType 완전 변경**: Unit/Spell → Damage/Heal/Summon
- **TargetType 의미 변화**: 주문 대상 → 배치 위치
- **다수 필드 제거**: SpellType, unitToSummon 등
- **메서드 시그니처 변경**: 검증 로직 전면 수정

### 마이그레이션 전략
- **데이터 변환**: Unit cards → Summon effects
- **매핑 로직**: Spell cards → Damage/Heal (SpellType 기반)
- **호환성 유지**: 가능한 범위에서 기존 설정 보존
- **검증 테스트**: 변환 결과 정확성 확인

### 리스크 관리
- **기능 플래그**: 점진적 배포를 위한 feature toggle
- **백업 시스템**: 원본 데이터 안전 보관
- **롤백 계획**: 문제 발생 시 복구 방안
- **의존성 추적**: 영향받는 모든 시스템 식별

### 의존성 업데이트 필요
- **CardSpawnService**: 주문 효과 적용 로직
- **GridController**: 위치 검증 통합
- **UI 시스템**: 카드 설명 생성
- **저장/로드**: 직렬화 호환성

## 📊 예상 영향도

### 높음 (즉시 수정 필요)
- CardData 직접 사용하는 모든 스크립트
- 카드 효과 처리 로직
- UI 표시 시스템

### 중간 (검토 후 수정)
- 저장/로드 시스템
- 에디터 도구
- 테스트 코드

### 낮음 (영향 최소)
- 오디오 시스템
- 네트워크 코드
- 기타 독립적 시스템

## 🚀 성공 기준

### 기능적 요구사항
- [ ] 모든 기존 카드가 새 시스템에서 정상 동작
- [ ] TargetRange/AffectedRange 거리 계산 정확성
- [ ] 팩토리 패턴을 통한 효과 실행 성공

### 비기능적 요구사항
- [ ] 성능 저하 없음 (기존 대비 ±5% 이내)
- [ ] 메모리 사용량 증가 최소화
- [ ] 코드 가독성 및 유지보수성 향상

### 품질 요구사항
- [ ] 단위 테스트 커버리지 90% 이상
- [ ] 통합 테스트 모든 시나리오 통과
- [ ] 코드 리뷰 승인 완료

## 📅 일정 계획

- **Week 1**: Phase 1 (준비 및 설계)
- **Week 2-3**: Phase 2 (핵심 리팩토링)
- **Week 4**: Phase 3 (시스템 통합)
- **Week 5**: Phase 4 (테스팅 및 검증)

## 👥 팀 협업

### 코드 리뷰 체크포인트
- Phase 1 완료 후: 아키텍처 설계 검토
- Phase 2 중간: 핵심 구조 변경 검토
- Phase 3 완료 후: 통합 테스트 결과 검토
- Phase 4 완료 후: 최종 배포 검토

### 문서화 요구사항
- API 변경사항 문서
- 마이그레이션 가이드
- 새로운 카드 생성 가이드
- 트러블슈팅 가이드