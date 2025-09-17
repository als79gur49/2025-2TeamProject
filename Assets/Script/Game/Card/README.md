# TCG Card System

Unity 프로젝트용 TCG (Trading Card Game) 카드 시스템입니다.

## 구조

### Core Components
- `ICard.cs` - 카드 인터페이스 및 CardType 열거형
- `BaseCard.cs` - 모든 카드의 기본 추상 클래스
- `UnitStats.cs` - 유닛 스탯, 희귀도, 스펠 타입 정의

### Card Types
- `UnitCard.cs` - 유닛 카드 구현체
- `CardUnitController.cs` - 소환된 유닛의 컨트롤러
- `Spell.cs` - 스펠 카드 구현체

### Effects System
- `ISpellEffect.cs` - 스펠 효과 인터페이스
- `SpellEffects.cs` - 구체적인 스펠 효과 구현들
- `SpellEffectFactory.cs` - 스펠 효과 팩토리 패턴

### Test Scripts
- `CardUnitTest.cs` - 유닛 카드 테스트
- `CardSpellTest.cs` - 스펠 카드 테스트
- `CardSystemIntegrationTest.cs` - 통합 테스트
- `CardSystemDemo.cs` - 데모 및 예시

## 주요 기능

### 카드 시스템
- 다형성을 활용한 확장 가능한 카드 구조
- 마나 비용, 사용 조건 검증
- 이벤트 기반 카드 상호작용

### 유닛 시스템
- HP, 공격력, 이동거리, 공격거리 스탯
- 데미지, 회복, 이동, 공격 기능
- 희귀도 시스템 (Common ~ Legendary)

### 스펠 시스템
- 7가지 스펠 타입 (Damage, Heal, Buff, Debuff, Shield, Teleport, Summon)
- 쿨다운 시스템
- 확장 가능한 효과 시스템

## 통합된 시스템들
- UI 시스템 (PopupManager)
- 오디오 시스템 연동 가능
- 기존 게임 시스템과 호환

## 사용 방법

1. 유닛 카드 생성:
```csharp
GameObject cardObject = new GameObject("MyUnitCard");
UnitCard unitCard = cardObject.AddComponent<UnitCard>();
unitCard.Use(); // 유닛 소환
```

2. 스펠 카드 생성:
```csharp
GameObject spellObject = new GameObject("MySpell");
Spell spellCard = spellObject.AddComponent<Spell>();
if (spellCard.CanUse()) {
    spellCard.Use(); // 스펠 시전
}
```

3. 테스트 실행:
- CardSystemDemo 컴포넌트를 씬에 추가하여 자동 데모 실행
- 개별 테스트 스크립트들을 사용하여 특정 기능 테스트

## 확장성
- 새로운 카드 타입 추가 가능
- 새로운 스펠 효과 추가 가능
- 기존 게임 로직과 쉽게 통합 가능