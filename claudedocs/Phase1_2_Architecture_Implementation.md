# Phase 1.2 Architecture Implementation - CardData Refactoring

## 📋 구현 개요

CardData 리팩토링 계획서의 Phase 1.2 (아키텍처 설계) 단계를 완료했습니다. 이번 구현에서는 효과 기반 아키텍처의 핵심 컴포넌트들을 생성했습니다.

## 🏗️ 생성된 구조

### 1. 핵심 인터페이스

#### `ICardEffect.cs`
- 모든 카드 효과의 기본 인터페이스
- `CanExecute()`, `Execute()` 메서드로 효과 검증 및 실행
- 효과 타입과 우선순위 속성 정의

```csharp
public interface ICardEffect
{
    bool CanExecute(Vector2Int targetPos, GameContext context);
    void Execute(Vector2Int targetPos, GameContext context);
    EffectType EffectType { get; }
    int Priority { get; }
}
```

### 2. 효과 타입 정의

#### `EffectType.cs`
- 기존 CardType을 대체하는 새로운 효과 타입
- Damage, Heal, Summon 세 가지 기본 효과 정의

### 3. 게임 컨텍스트

#### `GameContext.cs`
- 카드 효과 실행에 필요한 모든 서비스 참조를 포함
- 의존성 주입을 통한 서비스 접근 제공
- UnitService, GridController, CardSpawnService, SpawnValidator 등 포함

```csharp
public class GameContext
{
    public IUnitService UnitService { get; }
    public IGridController GridController { get; }
    public ICardSpawnService CardSpawnService { get; }
    public ISpawnValidator SpawnValidator { get; }
    public int PlayerId { get; }
    public Vector2Int OriginPosition { get; }
}
```

### 4. 직렬화 가능한 데이터 구조

#### `EffectData.cs`
- Unity 인스펙터에서 설정 가능한 효과 데이터
- 기존 `unitToSummon`, `spellEffectValue` 등 레거시 필드 대체
- `AffectedType` 열거형으로 효과 대상 명확화

```csharp
[Serializable]
public class EffectData
{
    public EffectType Type { get; }
    public int Value { get; }
    public AffectedType AffectedType { get; }
    public int AffectedRange { get; }
    public UnitData UnitToSummon { get; } // Summon 전용
    // ... 기타 속성들
}
```

### 5. 팩토리 패턴

#### `CardEffectFactory.cs`
- `EffectData`를 기반으로 `ICardEffect` 인스턴스 생성
- 타입별 생성자 등록 및 관리
- 우선순위 기반 효과 정렬 기능

```csharp
public static class CardEffectFactory
{
    public static ICardEffect CreateEffect(EffectData effectData);
    public static List<ICardEffect> CreateEffects(IEnumerable<EffectData> effectDataList);
    public static void RegisterEffectType(EffectType effectType, Func<EffectData, ICardEffect> creator);
}
```

### 6. 구체적 효과 구현

#### `DamageEffect.cs`
- 데미지 효과 구현
- 방어력 고려 및 방어력 무시 옵션
- 단일/범위 대상 지원

#### `HealEffect.cs`
- 회복 효과 구현
- 오버힐 방지 로직
- 회복 가능 대상 필터링

#### `SummonEffect.cs`
- 소환 효과 구현
- 소환 위치 유효성 검증
- 다중 유닛 소환 지원

## 🔗 아키텍처 설계 특징

### 의존성 주입 패턴
- `GameContext`를 통한 서비스 접근
- 테스트 가능한 구조 제공
- 각 효과는 필요한 서비스만 사용

### 팩토리 패턴
- 효과 타입별 동적 생성
- 확장 가능한 구조
- 런타임 효과 등록 지원

### 타겟팅 시스템 분리
- **TargetType**: 배치 가능 위치 검증 (기존 역할 유지)
- **AffectedType**: 효과 적용 대상 (새로운 역할)
- **AffectedRange**: 효과 범위 (새로운 개념)

### 직렬화 친화적 설계
- Unity 인스펙터 완전 지원
- ScriptableObject와 호환
- 에디터에서 직관적 설정 가능

## 🚀 다음 단계 준비

이번 Phase 1.2 구현으로 다음 단계들을 위한 기반이 완성되었습니다:

1. **Phase 2**: 기존 CardData 클래스에 EffectData 리스트 추가
2. **Phase 3**: 레거시 필드 제거 및 마이그레이션
3. **Phase 4**: 서비스 클래스 업데이트

## ⚠️ 주의사항

현재 구현은 아키텍처 설계에 중점을 두었으며, 일부 메서드들은 TODO 주석으로 표시된 placeholder 구현을 포함합니다. 이는 실제 서비스 클래스들과의 연동 시점에서 완성될 예정입니다.

## 📁 파일 구조

```
Assets/Script/Game/Card/Effects/
├── ICardEffect.cs          # 효과 인터페이스
├── EffectType.cs          # 효과 타입 열거형
├── EffectData.cs          # 직렬화 가능한 효과 데이터
├── GameContext.cs         # 게임 컨텍스트
├── CardEffectFactory.cs   # 팩토리 클래스
├── DamageEffect.cs        # 데미지 효과 구현
├── HealEffect.cs          # 회복 효과 구현
└── SummonEffect.cs        # 소환 효과 구현
```

이제 Phase 1.2의 핵심 아키텍처가 완성되었으며, 다음 단계인 CardData 클래스 업데이트를 진행할 준비가 되었습니다.