# IUnitAI 리팩토링 요약

## 🎯 목적
Unit 클래스의 SRP 위반 문제 해결 및 IHealthComponent 기반 타겟팅으로 Unit과 Base 모두 공격 가능하도록 개선

## 📋 변경 사항

### 1. 새 파일 생성

#### `/Assets/Script/Game/Interfaces/IUnitAI.cs`
- **IUnitAI 인터페이스**: AI 의사결정 책임 분리
- **ActionDecision 구조체**: 행동 결정 정보 (Attack/Move/Idle)
- **ActionType 열거형**: 행동 타입 정의
- **AIStrategy 열거형**: AI 전략 (Basic/Aggressive/Defensive/Optimal)

**핵심 메서드:**
```csharp
IHealthComponent FindBestTarget();  // Unit과 Base 모두 타겟팅 가능
ActionDecision DecideAction();      // 행동 결정
void SetStrategy(AIStrategy);       // 전략 설정
```

#### `/Assets/Script/Game/Components/BasicUnitAI.cs`
- **BasicUnitAI 컴포넌트**: IUnitAI 기본 구현
- **CombatComponent.GetTargetsInRange() 활용**: 기존 전투 시스템 재사용
- **IHealthComponent 기반 타겟팅**: Unit과 Base 통합 공격

**주요 기능:**
- 공격 범위 내 적 검색 (Unit + Base)
- 적이 없으면 전진
- 팀 관계 검증
- 디버깅 지원 (Context Menu)

### 2. Unit.cs 수정

#### 컴포넌트 참조 추가
```csharp
private IUnitAI unitAI;
public IUnitAI GetUnitAI() => unitAI;
```

#### Act() 메서드 리팩토링
**Before (문제점):**
```csharp
public void Act()
{
    Unit enemy = SearchForNearbyEnemies();  // ❌ Unit만 검색
    if (enemy != null) AttackEnemy(enemy);
    else MoveForward();
}
```

**After (개선):**
```csharp
public void Act()
{
    if (useComponentSystem && unitAI != null)
    {
        var decision = unitAI.DecideAction();  // ✅ AI 컴포넌트에 위임
        ExecuteDecision(decision);              // ✅ Unit과 Base 모두 공격
    }
    else
    {
        // Fallback: 기존 로직 (하위 호환)
        Unit enemy = SearchForNearbyEnemies();
        if (enemy != null) AttackEnemy(enemy);
        else MoveForward();
    }
}
```

#### ExecuteDecision() 메서드 추가
```csharp
private void ExecuteDecision(ActionDecision decision)
{
    switch (decision.Type)
    {
        case ActionType.Attack:
            // GameObject 기반 공격 → Unit과 Base 모두 가능
            combatComponent.Attack(decision.TargetObject);
            break;
        case ActionType.Move:
            movementComponent.MoveTo(decision.MovePosition);
            break;
        case ActionType.Idle:
            // 대기
            break;
    }
}
```

#### 컴포넌트 초기화
```csharp
private void InitializeComponents()
{
    // 기존 컴포넌트들...
    unitAI = GetComponent<IUnitAI>();

    if (useComponentSystem)
    {
        // ...
        InitializeAIComponent();  // ✅ 추가
    }
}

private void InitializeAIComponent()
{
    if (unitAI == null && autoAddMissingComponents)
    {
        var comp = gameObject.AddComponent<BasicUnitAI>();
        unitAI = comp;
    }
}
```

#### 컴포넌트 시스템 상태 업데이트
```csharp
private void UpdateComponentSystemStatus()
{
    componentSystemReady = useComponentSystem &&
                          healthComponent != null &&
                          combatComponent != null &&
                          movementComponent != null &&
                          teamComponent != null &&
                          unitAI != null;  // ✅ 추가
}
```

#### Legacy 메서드 Deprecated 처리
```csharp
[System.Obsolete("Use IUnitAI component instead - this method only finds Units, not Bases", false)]
private Unit SearchForNearbyEnemies()
{
    // 기존 구현 유지 (하위 호환)
}
```

## 🎨 아키텍처 개선

### Before (SRP 위반)
```
Unit
├─ SearchForNearbyEnemies() ❌ (Unit 직접 구현)
│   └─ Unit만 검색 ❌
├─ Act() → 공격/이동 판단 ❌ (Unit 직접 구현)
└─ AttackEnemy(), MoveForward()
```

### After (책임 분리)
```
Unit (Orchestrator)
├─ IUnitAI.DecideAction() ✅
└─ ExecuteDecision() ✅
    ├─ ICombatSystem.Attack() ✅
    └─ IMovementSystem.MoveTo() ✅

IUnitAI Component
├─ FindBestTarget() → IHealthComponent ✅
│   └─ ICombatSystem.GetTargetsInRange() ✅
│       ├─ Unit → HealthComponent ✅
│       └─ Base → HealthComponent ✅
└─ DecideAction() → ActionDecision ✅
```

## ✅ 개선 효과

### 1. 단일 책임 원칙 (SRP) 준수
- **Unit**: 행동 실행 (Orchestrator)
- **IUnitAI**: 의사결정 (Decision Maker)
- **ICombatSystem**: 전투 실행 (Executor)

### 2. IHealthComponent 기반 타겟팅
- ✅ Unit 공격 가능
- ✅ Base 공격 가능
- ✅ 모든 IHealthComponent를 가진 적 통합

### 3. 기존 시스템 재사용
- ✅ CombatComponent.GetTargetsInRange() 활용
- ✅ MovementComponent.GetValidMovePositions() 활용
- ✅ 중복 코드 제거

### 4. 확장성 향상
- Strategy Pattern 지원
- 다양한 AI 전략 구현 가능 (Aggressive, Defensive, Optimal)
- 플레이어/적군 공통 사용

### 5. 테스트 용이성
- AI 로직 독립 테스트 가능
- Mock 컴포넌트 주입 가능
- 컴포넌트별 격리 테스트

### 6. 하위 호환성 유지
- Legacy 시스템 Fallback 제공
- 기존 Unit 동작 보존
- 점진적 마이그레이션 가능

## 🔧 사용 방법

### 자동 컴포넌트 추가 (권장)
```csharp
// Unit Prefab에 설정
[SerializeField] private bool useComponentSystem = true;
[SerializeField] private bool autoAddMissingComponents = true;

// Awake()에서 자동으로 BasicUnitAI 추가됨
```

### 수동 컴포넌트 추가
```csharp
// Unity Inspector에서
gameObject.AddComponent<BasicUnitAI>();

// 또는 코드로
var ai = gameObject.AddComponent<BasicUnitAI>();
ai.SetStrategy(AIStrategy.Aggressive);
```

### 디버깅
```csharp
// Unity Context Menu
- "Test DecideAction": AI 결정 테스트
- "Log AI Status": AI 상태 확인
- "Log Component Status": 전체 컴포넌트 상태

// BasicUnitAI 로깅 활성화
[SerializeField] private bool enableLogging = true;
```

## 📝 향후 확장 계획

### 1. 다양한 AI 전략 구현
- **AggressiveAI**: 항상 가장 가까운 적 공격
- **DefensiveAI**: HP 낮은 아군 보호
- **OptimalAI**: 피해 최대화 계산 (EnemyAIController 패턴)

### 2. AI 전략 동적 변경
```csharp
public void SetStrategy(AIStrategy strategy)
{
    unitAI.SetStrategy(strategy);
}
```

### 3. 고급 타겟팅 로직
- 우선순위 기반 타겟 선택
- 위협도 평가
- 팀 협력 AI

## 🚀 결론

**Unit.SearchForNearbyEnemies()는 Unit의 책임이 아닙니다.**

IUnitAI 컴포넌트 생성으로:
- ✅ SRP 준수
- ✅ IHealthComponent 기반 통합 타겟팅 (Unit + Base)
- ✅ 기존 시스템 재사용
- ✅ 확장성 및 테스트 용이성 확보
- ✅ 하위 호환성 유지

---

**작성일**: 2025-10-12
**작성자**: Claude (SuperClaude Framework)
**관련 파일**:
- `/Assets/Script/Game/Interfaces/IUnitAI.cs` (신규)
- `/Assets/Script/Game/Components/BasicUnitAI.cs` (신규)
- `/Assets/Script/Game/Unit.cs` (수정)
