# Unit Animation System Design V2 - Animation Event Based
## 유닛 이동 및 공격 애니메이션 시스템 설계서 (Animation Event 방식)

**작성일**: 2025-10-02
**버전**: 2.0 (Animation Event Based)
**상태**: Design Complete

---

## 📋 Executive Summary

### 목적
유닛의 이동 및 공격 행동에 애니메이션을 적용하고, Unity Animation Event를 활용하여 정확한 타이밍 제어

### 설계 변경 사유
**기존 방식의 문제점**:
- 애니메이션 지속시간을 코드에 하드코딩 (`moveAnimationDuration = 0.5f`)
- 공격 타이밍을 코드로 계산 (`attackAnimationDuration * 0.6f`)
- 애니메이션 변경 시 코드 수정 필요 → 확장성 및 유지보수 불리

**새로운 방식의 장점**:
- Unity Animation Event 활용 → 애니메이션 프레임에 직접 함수 호출 삽입
- 코드 수정 없이 Animation Clip에서만 타이밍 조정
- 디자이너/아티스트가 직접 타이밍 제어 가능
- 다단 히트 공격 등 복잡한 애니메이션 쉽게 구현

### 핵심 요구사항
1. ✅ 유닛 이동 시 애니메이션 재생
2. ✅ 유닛 공격 시 애니메이션 재생 + 정확한 타이밍에 데미지 적용
3. ✅ 애니메이션 재생 중 다른 유닛 행동 차단
4. ✅ Animation Event를 통한 이벤트 기반 제어
5. ✅ 코드 수정 없이 타이밍 조정 가능

### 설계 원칙
- **Animation Event 중심**: 타이밍 제어는 애니메이션 클립에서
- **이벤트 기반 아키텍처**: 느슨한 결합, 높은 확장성
- **코드 분리**: 애니메이션 로직과 게임 로직 분리
- **디자이너 친화적**: 코드 수정 없이 타이밍 조정 가능
- **확장성**: 새로운 애니메이션 타입 쉽게 추가 가능

---

## 🚧 Current Implementation Status

**문서 작성일**: 2025-10-02
**현재 확인일**: 2025-10-03
**구현 상태**: ❌ **미구현** (설계 단계)

### ⚠️ 구현 상태 요약

| Phase | 항목 | 상태 | 비고 |
|-------|------|------|------|
| **Phase 1** | IAnimationController 인터페이스 | ❌ 미구현 | 파일 자체가 존재하지 않음 |
| **Phase 1** | UnitAnimationController 클래스 | ❌ 미구현 | 파일 자체가 존재하지 않음 |
| **Phase 1** | Unit.cs 애니메이션 컨트롤러 참조 | ❌ 미구현 | animationController 필드 없음 |
| **Phase 2** | CombatComponent 이벤트 구독 | ❌ 미구현 | OnAttackHit 구독 로직 없음 |
| **Phase 2** | MovementComponent 이벤트 구독 | ❌ 미구현 | OnMovementFinished 구독 로직 없음 |
| **Phase 2** | UnitService 애니메이션 대기 | ❌ 미구현 | OnAnimationComplete 대기 로직 없음 |
| **Phase 3** | Animation Event 설정 | ❌ 불가능 | 코드 구현 필요 |
| **Phase 4** | 최적화 및 폴리시 | ❌ 불가능 | 코드 구현 필요 |

### 🎯 즉시 구현 가능한 작업

**우선순위 순서로 다음 작업을 수행하세요:**

#### 1️⃣ **Phase 1.1: IAnimationController 인터페이스 작성** (30분)
```bash
# 파일 생성
touch Assets/Script/Game/Interfaces/IAnimationController.cs

# 설계 문서의 "1. IAnimationController Interface (Event-Driven)" 섹션 참고하여 코드 작성
```

**체크리스트:**
- [ ] `Assets/Script/Game/Interfaces/IAnimationController.cs` 파일 생성
- [ ] 인터페이스 멤버 정의 (Properties, Methods, Events)
- [ ] XML 주석 작성
- [ ] 컴파일 확인

#### 2️⃣ **Phase 1.2: UnitAnimationController 구현** (1-2시간)
```bash
# 파일 생성
touch Assets/Script/Game/Components/UnitAnimationController.cs

# 설계 문서의 "2. UnitAnimationController Implementation" 섹션 참고하여 코드 작성
```

**체크리스트:**
- [ ] `Assets/Script/Game/Components/UnitAnimationController.cs` 파일 생성
- [ ] IAnimationController 인터페이스 구현
- [ ] AnimEvent_* 콜백 메서드 작성
- [ ] Serialized Fields 설정
- [ ] 컴파일 확인

#### 3️⃣ **Phase 1.3: Unit.cs 수정** (15분)
```bash
# Unit.cs에 애니메이션 컨트롤러 참조 추가
```

**추가할 코드:**
```csharp
// Unit.cs - Component references 섹션에 추가
private IAnimationController animationController;
public IAnimationController GetAnimationController() => animationController;

// InitializeComponents() 메서드에 추가
private void InitializeComponents()
{
    // 기존 초기화 코드...

    // Animation Controller 초기화 추가
    animationController = GetComponent<IAnimationController>();
    if (animationController == null && autoAddMissingComponents)
    {
        animationController = gameObject.AddComponent<UnitAnimationController>();
        Debug.Log($"[Unit] Auto-added UnitAnimationController to {gameObject.name}");
    }
}
```

**체크리스트:**
- [ ] Unit.cs에 animationController 필드 추가
- [ ] GetAnimationController() 프로퍼티 추가
- [ ] InitializeComponents()에 초기화 로직 추가
- [ ] 컴파일 확인

#### 4️⃣ **Phase 2.1: CombatComponent 수정** (30분)
**체크리스트:**
- [ ] OnAttackHit 이벤트 구독 로직 추가 (Awake/OnDestroy)
- [ ] OnAnimationAttackHit 콜백 메서드 구현
- [ ] Attack 메서드 수정 (애니메이션 트리거)
- [ ] 컴파일 및 기본 테스트

#### 5️⃣ **Phase 2.2: MovementComponent 수정** (30분)
**체크리스트:**
- [ ] OnMovementFinished 이벤트 구독 로직 추가 (Awake/OnDestroy)
- [ ] OnAnimationMovementFinished 콜백 메서드 구현
- [ ] MoveToPosition 메서드 수정 (애니메이션 트리거)
- [ ] 컴파일 및 기본 테스트

#### 6️⃣ **Phase 2.3: UnitService 수정** (1시간)
**체크리스트:**
- [ ] ProcessUnitActionAsync에 애니메이션 대기 로직 추가
- [ ] 타임아웃 안전장치 구현
- [ ] 컴파일 및 통합 테스트

#### 7️⃣ **Phase 3: Animation Event 설정** (1시간)
**체크리스트:**
- [ ] Move Animation에 Start/End Event 추가
- [ ] Attack Animation에 Start/Impact/End Event 추가
- [ ] Unity Editor에서 타이밍 조정
- [ ] 플레이 테스트

#### 8️⃣ **Phase 4: 최적화 및 폴리시** (1-2시간)
**체크리스트:**
- [ ] 애니메이션 속도 제어 구현
- [ ] 애니메이션 스킵 기능 구현
- [ ] 성능 프로파일링
- [ ] 최종 QA

### 📊 예상 구현 시간

| Phase | 예상 시간 | 누적 시간 |
|-------|----------|----------|
| Phase 1.1 (Interface) | 30분 | 30분 |
| Phase 1.2 (Controller) | 1-2시간 | 2.5시간 |
| Phase 1.3 (Unit.cs) | 15분 | 2.75시간 |
| Phase 2.1 (CombatComponent) | 30분 | 3.25시간 |
| Phase 2.2 (MovementComponent) | 30분 | 3.75시간 |
| Phase 2.3 (UnitService) | 1시간 | 4.75시간 |
| Phase 3 (Animation Events) | 1시간 | 5.75시간 |
| Phase 4 (Polish) | 1-2시간 | **7시간** |

**총 예상 시간: 약 7시간 (1일 작업)**

### 🔴 Critical Blockers (현재 차단 요소)

1. **IAnimationController 인터페이스 부재** → Phase 1.1부터 시작 필수
2. **UnitAnimationController 미구현** → Phase 1.2 완료 전까지 테스트 불가
3. **Animation Event 설정 불가** → Phase 1-2 완료 전까지 Unity 작업 불가

### ✅ 구현 완료 후 검증 방법

**1. 코드 레벨 검증**
```bash
# IAnimationController 인터페이스 존재 확인
ls Assets/Script/Game/Interfaces/IAnimationController.cs

# UnitAnimationController 클래스 존재 확인
ls Assets/Script/Game/Components/UnitAnimationController.cs

# 컴파일 에러 확인
# Unity Console에서 에러 0개 확인
```

**2. Unity Editor 검증**
- Unit GameObject에 UnitAnimationController 컴포넌트 자동 추가 확인
- Inspector에서 Animator 참조 확인
- Animation Event가 제대로 설정되었는지 Animation 창에서 확인

**3. 런타임 검증**
- Unit 이동 시 애니메이션 재생 확인
- Unit 공격 시 애니메이션 재생 확인
- Console 로그에서 AnimEvent 호출 확인
- 타격 타이밍이 자연스러운지 확인

---

## 🏗️ System Architecture Overview

### Animation Event 흐름도

```
┌─────────────────────────────────────────────────────────────┐
│                    Animator Controller                       │
│  - Animation Clips (Move, Attack, Idle)                     │
│  - Animation Events embedded in clips                       │
└──────────────────┬──────────────────────────────────────────┘
                   │ Triggers Animation Events at specific frames
                   ↓
┌─────────────────────────────────────────────────────────────┐
│              UnitAnimationController                         │
│  - AnimEvent_OnAnimationStart() ← Animation Event           │
│  - AnimEvent_OnAnimationEnd() ← Animation Event             │
│  - AnimEvent_OnAttackImpact() ← Animation Event             │
│  - AnimEvent_OnMoveComplete() ← Animation Event             │
└──────────────────┬──────────────────────────────────────────┘
                   │ Invokes C# Events
                   ↓
┌─────────────────────────────────────────────────────────────┐
│              IAnimationController Events                     │
│  - OnAnimationStarted (애니메이션 시작)                     │
│  - OnAnimationComplete (애니메이션 완료)                    │
│  - OnAttackHit (공격 타격 순간)                             │
│  - OnMovementFinished (이동 완료)                           │
└──────────────────┬──────────────────────────────────────────┘
                   │ Subscribed by Components
        ┌──────────┴──────────┐
        ↓                      ↓
┌──────────────────┐  ┌──────────────────┐
│ CombatComponent  │  │ MovementComponent│
│ - OnAttackHit    │  │ - OnMoveFinished │
│   구독하여       │  │   구독하여       │
│   데미지 적용    │  │   그리드 업데이트│
└──────────────────┘  └──────────────────┘
```

### 전체 구조도 (Animation Event Based)

```
┌─────────────────────────────────────────────────────────┐
│                     UnitService                          │
│  - ProcessUnitActionAsync()                             │
│  - Waits for OnAnimationComplete event                  │
└──────────────────┬──────────────────────────────────────┘
                   │ Controls
                   ↓
┌─────────────────────────────────────────────────────────┐
│                        Unit                              │
│  - Act() → AttackEnemy() or MoveForward()              │
│  - GetAnimationController()                             │
└──────────────────┬──────────────────────────────────────┘
                   │ Uses
        ┌──────────┴──────────┐
        ↓                      ↓
┌──────────────────┐  ┌──────────────────┐
│ MovementComponent│  │ CombatComponent  │
│ - MoveTo()       │  │ - Attack()       │
│ - Subscribe to   │  │ - Subscribe to   │
│   OnMoveFinished │  │   OnAttackHit    │
└────────┬─────────┘  └────────┬─────────┘
         │                      │
         └──────────┬───────────┘
                    ↓
         ┌─────────────────────┐
         │ IAnimationController │
         │ - IsAnimationPlaying │
         │ - PlayMoveAnimation  │ (void, not IEnumerator)
         │ - PlayAttackAnimation│ (void, not IEnumerator)
         │ - Events (6 types)   │
         └──────────┬───────────┘
                    ↓
         ┌─────────────────────────┐
         │ UnitAnimationController │
         │ - Animator integration  │
         │ - Animation Event       │
         │   callbacks (AnimEvent_*)│
         └──────────┬──────────────┘
                    ↑
                    │ Animation Events from clips
         ┌─────────────────────────┐
         │   Animator Controller   │
         │ - Animation Clips       │
         │ - Embedded Events       │
         └─────────────────────────┘
```

---

## ⚠️ Critical Architecture Migration Risk

### 🔴 동기(Synchronous) → 비동기(Asynchronous) 전환 충돌

#### 문제 분석

**기존 시스템 (동기 방식)**:
```csharp
// 기존: Attack() 호출 시 즉시 데미지 적용 완료
combat.Attack(target);
// ↑ 이 시점에 이미 데미지 적용, 체력 감소 완료
if (target.Health <= 0) {
    HandleDeath(target);  // 정상 동작
}
```

**새 시스템 (비동기 애니메이션 기반)**:
```csharp
// 새 시스템: Attack() 호출은 애니메이션 트리거만
combat.Attack(target);
// ↑ 이 시점에는 애니메이션만 시작, 데미지 적용 안 됨!
//   실제 데미지는 AnimEvent_OnAttackImpact()에서 적용 (몇 프레임 후)

if (target.Health <= 0) {  // ❌ 항상 false! 체력이 아직 안 줄어듦
    HandleDeath(target);
}
```

#### 근본 원인

Animation Event 기반 설계에서는 **타이밍이 분리**됩니다:
1. `Attack()` 호출 → 애니메이션 트리거
2. Animation Event (`AnimEvent_OnAttackImpact`) → 데미지 적용
3. 1번과 2번 사이에 **시간 간격(0.3-0.6초)** 존재

기존 코드가 "Attack() 호출 = 즉시 완료"를 가정하면 **모든 로직이 오작동**합니다.

#### 충돌하는 코드 패턴

```csharp
// ❌ 패턴 1: 즉시 결과 확인
combat.Attack(enemy);
if (enemy.Health <= 0) {  // 체력이 아직 안 줄어서 항상 false
    RemoveEnemy(enemy);
}

// ❌ 패턴 2: CombatResult 반환값 의존
CombatResult result = combat.Attack(target);
if (result.Damage > 0) {  // result는 임시값, 실제 데미지는 나중에 적용
    ApplyStatusEffect(target, StatusType.Bleeding);
}

// ❌ 패턴 3: 순차 실행 가정
combat.Attack(enemy1);
combat.Attack(enemy2);  // enemy1 애니메이션 중인데 enemy2 공격 시작 → 충돌

// ❌ 패턴 4: 콤보/연계 시스템
if (combat.Attack(target)) {
    if (CheckComboCondition()) {  // 조건이 아직 충족 안 됨
        ExecuteComboAttack();
    }
}
```

#### 해결책: 이벤트 구독 패턴 강제

**핵심 원칙**: Attack/MoveTo 호출 후 즉시 결과를 확인하지 말고, **이벤트를 구독**하여 완료 시점에 로직 실행

**예시 1: 공격 후 사망 처리**

```csharp
// ✅ Before (동기 방식) → After (이벤트 방식)

// Before
public void AttackEnemy(GameObject enemy) {
    combat.Attack(enemy);

    if (enemy.GetComponent<IHealthComponent>().Health <= 0) {
        HandleDeath(enemy);
    }
}

// After
public void AttackEnemy(GameObject enemy) {
    // 일회성 이벤트 구독
    Action<GameObject, CombatResult> onAttackComplete = null;
    onAttackComplete = (target, result) => {
        if (target == enemy) {
            // 이벤트 구독 해제 (메모리 누수 방지)
            combat.OnAttackPerformed -= onAttackComplete;

            // 이제 안전하게 체력 확인 가능
            if (target.GetComponent<IHealthComponent>().Health <= 0) {
                HandleDeath(target);
            }
        }
    };

    combat.OnAttackPerformed += onAttackComplete;
    combat.Attack(enemy);  // 트리거만
}
```

**예시 2: 상태 이상 적용**

```csharp
// Before
public void ApplyPoisonAttack(GameObject target) {
    var result = combat.Attack(target);

    if (result.IsHit) {
        target.GetComponent<StatusEffect>().ApplyPoison(3f);
    }
}

// After
public void ApplyPoisonAttack(GameObject target) {
    Action<GameObject, CombatResult> handler = null;
    handler = (t, result) => {
        if (t == target) {
            combat.OnAttackPerformed -= handler;

            // 실제 히트 여부 확인 후 독 적용
            if (result.IsHit) {
                target.GetComponent<StatusEffect>().ApplyPoison(3f);
            }
        }
    };

    combat.OnAttackPerformed += handler;
    combat.Attack(target);
}
```

**예시 3: Unit.Act() 패턴 (핵심!)**

```csharp
// Unit.cs 기존 코드 (동기 방식 가정)
public void Act() {
    var enemy = FindNearestEnemy();

    if (enemy != null && IsInAttackRange(enemy)) {
        AttackEnemy(enemy);
        // ❌ 여기서 enemy 상태 확인하면 안 됨!
    } else {
        MoveForward();
        // ❌ 여기서 위치 확인하면 안 됨!
    }
}

// ✅ 이벤트 기반으로 수정 불필요!
// UnitService가 애니메이션 완료를 대기하므로,
// Act() 호출 후 다음 유닛으로 넘어가기 전까지 애니메이션 완료됨
// 따라서 Unit.Act() 내부는 수정 불필요

// 단, Act() 외부에서 즉시 결과를 확인하는 코드는 수정 필요:
// ❌ unit.Act(); if (unit.Health <= 0) ...
// ✅ unit.Act(); → UnitService가 대기 → 애니메이션 완료 → 다음 유닛
```

#### 기존 코드 마이그레이션 체크리스트

프로젝트 전체를 검색하여 다음 패턴을 찾아 수정:

- [ ] **Attack 직후 상태 확인**: `Attack()` 호출 후 즉시 `Health`, `IsAlive` 등 확인
  - 검색 패턴: `Attack\(.*\).*\n.*Health`
  - 수정: 이벤트 구독으로 전환

- [ ] **MoveTo 직후 위치 확인**: `MoveTo()` 호출 후 즉시 그리드 위치 확인
  - 검색 패턴: `MoveTo\(.*\).*\n.*GetUnitPosition`
  - 수정: 이벤트 구독 또는 UnitService 대기 활용

- [ ] **CombatResult 반환값 의존**: `Attack()` 반환값으로 즉시 로직 실행
  - 검색 패턴: `var result = .*Attack\(`
  - 수정: 이벤트 구독으로 전환

- [ ] **순차 실행 가정**: 연속된 `Attack()` 또는 `MoveTo()` 호출
  - 검색 패턴: `Attack\(.*\).*\n.*Attack\(`
  - 수정: UnitService가 순차 실행 보장하므로 괜찮음 (Unit.Act() 내부는 안전)

- [ ] **콤보/연계 시스템**: Attack 결과에 따라 즉시 다음 행동 결정
  - 수정: 이벤트 체인으로 전환

#### 전환 가이드라인

**핵심 원칙**:
1. **Act() 내부**: 수정 불필요 (UnitService가 순차 실행 보장)
2. **Act() 외부**: `Attack()`/`MoveTo()` 호출 후 즉시 상태 확인 금지
3. **이벤트 구독**: 결과가 필요하면 반드시 이벤트 구독
4. **일회성 구독**: 이벤트 핸들러는 실행 후 즉시 구독 해제 (메모리 누수 방지)

**복잡한 로직 예시: 턴 기반 전투 시스템**

```csharp
// Before (동기)
public void ProcessTurn(Unit unit) {
    var target = FindTarget(unit);
    var result = unit.CombatComponent.Attack(target);

    if (result.IsHit) {
        ApplyStatusEffect(target);

        if (target.Health <= 0) {
            RemoveUnit(target);
            GainExperience(unit, target.Level);
        }
    }
}

// After (비동기 이벤트)
public void ProcessTurn(Unit unit) {
    var target = FindTarget(unit);

    // 일회성 이벤트 구독
    Action<GameObject, CombatResult> onComplete = null;
    onComplete = (t, result) => {
        if (t == target) {
            unit.CombatComponent.OnAttackPerformed -= onComplete;

            // 이제 안전하게 결과 처리
            if (result.IsHit) {
                ApplyStatusEffect(target);

                var targetHealth = target.GetComponent<IHealthComponent>();
                if (targetHealth != null && targetHealth.Health <= 0) {
                    var targetUnit = target.GetComponent<Unit>();
                    int targetLevel = targetUnit?.Level ?? 1;

                    RemoveUnit(target);
                    GainExperience(unit, targetLevel);
                }
            }
        }
    };

    unit.CombatComponent.OnAttackPerformed += onComplete;
    unit.CombatComponent.Attack(target);
}
```

#### 검증 방법

**1. 정적 분석**
```bash
# 프로젝트 내 위험 패턴 검색
grep -rn "Attack(.*)" --include="*.cs" | grep -A 2 "Health"
grep -rn "MoveTo(.*)" --include="*.cs" | grep -A 2 "Position"
```

**2. 통합 테스트**
```csharp
[Test]
public IEnumerator Attack_DoesNotApplyDamageImmediately() {
    var target = CreateTestUnit(100);
    combat.Attack(target.gameObject);

    // 즉시 확인 → 체력이 아직 안 줄어야 함
    Assert.AreEqual(100, target.Health);

    // 애니메이션 대기
    yield return new WaitForSeconds(1f);

    // 이제 체력 감소
    Assert.Less(target.Health, 100);
}
```

**3. 런타임 검증**
```csharp
// CombatComponent.cs에 디버그 로그 추가
public CombatResult Attack(GameObject target) {
    Debug.Log($"[Combat] Attack called on {target.name} at frame {Time.frameCount}");
    // ...
}

private void OnAnimationAttackHit(GameObject target) {
    Debug.Log($"[Combat] Damage applied to {target.name} at frame {Time.frameCount}");
    // ...
}
// → 두 로그의 프레임 번호 차이 확인 (몇 프레임 차이 나야 정상)
```

#### 마이그레이션 우선순위

1. **🔴 Critical**: Unit 외부에서 `Attack()`/`MoveTo()` 호출 후 즉시 상태 확인
2. **🟡 High**: `CombatResult` 반환값에 의존하는 로직
3. **🟢 Medium**: 콤보/연계 시스템
4. **⚪ Low**: Unit.Act() 내부 (UnitService가 처리)

---

## 📐 Component Specifications

### 1. IAnimationController Interface (Event-Driven)

**목적**: Animation Event 기반 애니메이션 제어 및 이벤트 통신

**주요 변경사항**:
- **코루틴 제거**: `IEnumerator` 반환 → `void` 반환 (Animation Event가 타이밍 제어)
- **타이밍 파라미터 제거**: duration 등 하드코딩된 값 제거
- **게임플레이 이벤트 추가**: `OnAttackHit`, `OnMovementFinished` 등
- **CurrentTarget 추가**: 공격 대상 추적용 프로퍼티

```csharp
namespace Game.Interfaces
{
    /// <summary>
    /// Animation Event 기반 유닛 애니메이션 제어 인터페이스
    /// Unity Animation Event를 통해 정확한 타이밍에 게임플레이 이벤트 발생
    /// </summary>
    public interface IAnimationController
    {
        // Properties
        bool IsAnimationPlaying { get; }
        float CurrentAnimationProgress { get; }
        GameObject CurrentTarget { get; } // 공격 대상 추적

        // Animation Methods (코루틴 제거, void 반환)
        void PlayMoveAnimation(Vector2Int from, Vector2Int to);
        void PlayAttackAnimation(GameObject target);
        void StopCurrentAnimation();
        void SetAnimationSpeed(float speed);

        // Animation Lifecycle Events (Animation Event에서 호출)
        event Action<string> OnAnimationStarted;      // AnimEvent_OnAnimationStart()
        event Action OnAnimationComplete;             // AnimEvent_OnAnimationEnd()
        event Action OnAnimationInterrupted;

        // Gameplay Events (특정 타이밍에 게임플레이 로직 실행)
        event Action<GameObject> OnAttackHit;         // AnimEvent_OnAttackImpact()
        event Action<Vector2Int> OnMovementFinished;  // AnimEvent_OnMoveComplete()
        event Action OnSkillCast;                     // AnimEvent_OnSkillCast() (추후 확장)
    }
}
```

**주요 멤버**:
- `IsAnimationPlaying`: 현재 애니메이션 재생 여부 (UnitService가 체크)
- `CurrentTarget`: 현재 공격 중인 대상 GameObject
- `PlayMoveAnimation()`: 이동 애니메이션 트리거 (void, 타이밍은 Animation Event가 제어)
- `PlayAttackAnimation()`: 공격 애니메이션 트리거 (void, 타이밍은 Animation Event가 제어)
- `OnAnimationComplete`: 애니메이션 완료 이벤트 (AnimEvent_OnAnimationEnd에서 호출)
- `OnAttackHit`: 공격 타격 순간 이벤트 (AnimEvent_OnAttackImpact에서 호출, CombatComponent가 구독)
- `OnMovementFinished`: 이동 완료 이벤트 (AnimEvent_OnMoveComplete에서 호출)

---

### 2. UnitAnimationController Implementation (Animation Event Based)

**목적**: Animation Event 수신 및 C# 이벤트로 변환하는 컨트롤러

**핵심 변경사항**:
- **코루틴 완전 제거**: `IEnumerator` 대신 `void` 메서드
- **Duration 필드 제거**: `moveAnimationDuration`, `attackAnimationDuration` 삭제
- **Animation Event 콜백 추가**: `AnimEvent_*` 메서드들 (Unity Animation Event에서 호출됨)
- **타겟 추적**: `currentTarget` 필드로 공격 대상 저장
- **이벤트 확장**: 게임플레이 이벤트 `OnAttackHit`, `OnMovementFinished` 추가

```csharp
namespace Game.Components
{
    /// <summary>
    /// Animation Event 기반 유닛 애니메이션 컨트롤러
    /// Unity Animator의 Animation Event를 수신하여 게임 로직에 전달
    /// </summary>
    public class UnitAnimationController : MonoBehaviour, IAnimationController
    {
        #region Serialized Fields

        [Header("Animator Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool useAnimator = true;

        [Header("Advanced Settings")]
        [SerializeField] private float animationSpeedMultiplier = 1f;
        [SerializeField] private bool skipAnimations = false;

        [Header("Debug")]
        [SerializeField] private bool logAnimationEvents = true;

        #endregion

        #region Animation Parameter Names

        private const string MOVE_TRIGGER = "Move";
        private const string ATTACK_TRIGGER = "Attack";
        private const string IDLE_TRIGGER = "Idle";
        private const string ANIMATION_SPEED = "AnimSpeed";

        #endregion

        #region Runtime State

        private bool isAnimationPlaying;
        private float currentAnimationProgress;
        private GameObject currentTarget;              // 공격 대상 추적
        private Vector2Int moveStartPosition;
        private Vector2Int moveTargetPosition;
        private string currentAnimationType;           // "Move", "Attack" 등

        #endregion

        #region IAnimationController Events

        public event Action<string> OnAnimationStarted;
        public event Action OnAnimationComplete;
        public event Action OnAnimationInterrupted;
        public event Action<GameObject> OnAttackHit;
        public event Action<Vector2Int> OnMovementFinished;
        public event Action OnSkillCast;

        #endregion

        #region IAnimationController Properties

        public bool IsAnimationPlaying => isAnimationPlaying;
        public float CurrentAnimationProgress => currentAnimationProgress;
        public GameObject CurrentTarget => currentTarget;

        #endregion

        #region IAnimationController Methods

        /// <summary>
        /// 이동 애니메이션 재생 (트리거만, Animation Event가 나머지 처리)
        /// </summary>
        public void PlayMoveAnimation(Vector2Int from, Vector2Int to)
        {
            if (skipAnimations)
            {
                // 애니메이션 스킵 시 즉시 완료 이벤트 발생
                OnMovementFinished?.Invoke(to);
                return;
            }

            moveStartPosition = from;
            moveTargetPosition = to;
            currentAnimationType = "Move";

            if (useAnimator && animator != null)
            {
                animator.SetTrigger(MOVE_TRIGGER);
                animator.SetFloat(ANIMATION_SPEED, animationSpeedMultiplier);
            }

            // isAnimationPlaying은 AnimEvent_OnAnimationStart()에서 true로 설정됨
        }

        /// <summary>
        /// 공격 애니메이션 재생 (트리거만, Animation Event가 나머지 처리)
        /// </summary>
        public void PlayAttackAnimation(GameObject target)
        {
            if (skipAnimations)
            {
                // 애니메이션 스킵 시 즉시 공격 히트 및 완료
                currentTarget = target;
                OnAttackHit?.Invoke(target);
                currentTarget = null;
                return;
            }

            currentTarget = target;
            currentAnimationType = "Attack";

            if (useAnimator && animator != null)
            {
                animator.SetTrigger(ATTACK_TRIGGER);
                animator.SetFloat(ANIMATION_SPEED, animationSpeedMultiplier);
            }

            // 순서: AnimEvent_OnAnimationStart → AnimEvent_OnAttackImpact → AnimEvent_OnAnimationEnd
        }

        public void StopCurrentAnimation()
        {
            isAnimationPlaying = false;
            currentTarget = null;

            if (useAnimator && animator != null)
            {
                animator.SetTrigger(IDLE_TRIGGER);
            }

            OnAnimationInterrupted?.Invoke();
        }

        public void SetAnimationSpeed(float speed)
        {
            animationSpeedMultiplier = Mathf.Max(0.1f, speed);

            if (useAnimator && animator != null)
            {
                animator.SetFloat(ANIMATION_SPEED, animationSpeedMultiplier);
            }
        }

        #endregion

        #region Animation Event Callbacks
        // 🎬 이 메서드들은 Unity Animation Event에서 호출됨

        /// <summary>
        /// Animation Event: 애니메이션 시작 시 호출
        /// Animation Clip의 첫 프레임에 설정
        /// </summary>
        public void AnimEvent_OnAnimationStart()
        {
            isAnimationPlaying = true;
            currentAnimationProgress = 0f;

            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Animation Started - {currentAnimationType}");

            OnAnimationStarted?.Invoke(currentAnimationType);
        }

        /// <summary>
        /// Animation Event: 애니메이션 종료 시 호출
        /// Animation Clip의 마지막 프레임에 설정
        /// </summary>
        public void AnimEvent_OnAnimationEnd()
        {
            isAnimationPlaying = false;
            currentAnimationProgress = 1f;

            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Animation Ended - {currentAnimationType}");

            OnAnimationComplete?.Invoke();

            // 이동 애니메이션 완료 시 추가 이벤트
            if (currentAnimationType == "Move")
            {
                OnMovementFinished?.Invoke(moveTargetPosition);
            }

            // 정리
            currentAnimationType = null;
            currentTarget = null;
        }

        /// <summary>
        /// Animation Event: 공격이 실제로 타격하는 프레임에 호출
        /// Attack Animation Clip의 타격 프레임 (보통 60% 지점)에 설정
        /// CombatComponent가 이 이벤트를 구독하여 데미지 적용
        /// </summary>
        public void AnimEvent_OnAttackImpact()
        {
            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Attack Impact on {currentTarget?.name}");

            if (currentTarget != null)
            {
                OnAttackHit?.Invoke(currentTarget);
            }
            else
            {
                Debug.LogWarning($"[AnimationController] {gameObject.name}: Attack impact but no target set!");
            }
        }

        /// <summary>
        /// Animation Event: 스킬 발동 시점 (추후 확장용)
        /// </summary>
        public void AnimEvent_OnSkillCast()
        {
            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Skill Cast");

            OnSkillCast?.Invoke();
        }

        /// <summary>
        /// Animation Event: 이동 완료 시점 (선택적, AnimEvent_OnAnimationEnd와 별도 타이밍 필요시)
        /// </summary>
        public void AnimEvent_OnMoveComplete()
        {
            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Move Complete to {moveTargetPosition}");

            OnMovementFinished?.Invoke(moveTargetPosition);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogWarning($"[UnitAnimationController] No Animator on {gameObject.name}, animations will be skipped");
                skipAnimations = true;
            }
        }

        private void OnDestroy()
        {
            StopCurrentAnimation();
        }

        #endregion
    }
}
```

**핵심 기능**:
- **Animation Event 통합**: Unity Animation Event를 C# 이벤트로 변환
- **타이밍 제어 분리**: 애니메이션 클립에서 타이밍 제어, 코드는 이벤트만 처리
- **게임플레이 이벤트**: `OnAttackHit`, `OnMovementFinished` 등으로 정확한 타이밍에 로직 실행
- **확장성**: 새로운 Animation Event 추가만으로 기능 확장 가능

---

### 3. UnitService Integration

**목적**: 애니메이션 완료 대기 로직 구현

#### 수정: ProcessUnitActionAsync()

```csharp
private IEnumerator ProcessUnitActionAsync(Unit unit, TurnPhase phase)
{
    try
    {
        // 1. 애니메이션 컨트롤러 캐싱
        var animController = unit?.GetComponent<IAnimationController>();

        // 2. 페이즈별 액션 실행 (기존 로직)
        switch (phase)
        {
            case TurnPhase.TurnStart:
                unit.OnTurnStart();
                break;

            case TurnPhase.EnemyAction:
            case TurnPhase.AllyAction:
                Debug.Log($"[UnitService] Processing action for unit {unit.name}");

                // 유닛 행동 실행 (이동 or 공격 → 애니메이션 트리거)
                unit.Act();

                // 3. 애니메이션 완료 대기 (핵심 로직)
                if (animController != null && animController.IsAnimationPlaying)
                {
                    Debug.Log($"[UnitService] Waiting for {unit.name} animation to complete...");

                    float timeout = 5f; // 5초 타임아웃 (안전장치)
                    float elapsed = 0f;

                    // IsAnimationPlaying이 false가 될 때까지 대기
                    while (animController.IsAnimationPlaying && elapsed < timeout)
                    {
                        yield return null; // 다음 프레임까지 대기
                        elapsed += Time.deltaTime;
                    }

                    // 타임아웃 처리
                    if (elapsed >= timeout)
                    {
                        Debug.LogWarning($"[UnitService] Animation timeout for {unit.name}, forcing completion");
                        animController.StopCurrentAnimation();
                    }
                    else
                    {
                        Debug.Log($"[UnitService] Animation completed for {unit.name}");
                    }
                }
                break;

            case TurnPhase.TurnEnd:
                unit.OnTurnEnd();
                break;
        }
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[UnitService] Error processing unit {unit?.name} in phase {phase}: {ex.Message}");
    }

    // 4. 기존 딜레이 유지 (필요 시)
    yield return null;
}
```

**핵심 변경사항**:
1. `IAnimationController` 참조 획득
2. `unit.Act()` 호출 (애니메이션 트리거)
3. `WaitUntil(() => !animController.IsAnimationPlaying)` 대신 timeout이 있는 while 루프 사용
4. 타임아웃 안전장치 (5초)
5. 애니메이션 완료 후 다음 유닛으로 진행

---

### 4. MovementComponent Integration (Event-Driven)

**목적**: `OnMovementFinished` 이벤트 구독하여 이동 완료 처리 (선택적)

**핵심 변경사항**:
- **이벤트 구독**: `OnMovementFinished` 이벤트 구독 (추가 처리 필요시)
- **코루틴 제거**: `StartCoroutine` 호출 제거 → `void` 메서드 직접 호출
- **즉시 그리드 업데이트**: 애니메이션은 시각적 효과만, 로직은 즉시 처리

#### 수정: Awake/OnDestroy (이벤트 구독/해제)

```csharp
// MovementComponent.cs 수정 부분

private IAnimationController animationController;

private void Awake()
{
    // ... 기존 초기화 코드 ...

    animationController = GetComponent<IAnimationController>();

    if (animationController != null)
    {
        // 이동 완료 이벤트 구독 (선택적, 추가 처리 필요시)
        animationController.OnMovementFinished += OnAnimationMovementFinished;
    }
}

private void OnDestroy()
{
    if (animationController != null)
    {
        animationController.OnMovementFinished -= OnAnimationMovementFinished;
    }
}

/// <summary>
/// Animation Event에서 호출되는 이동 완료 처리 (선택적)
/// </summary>
private void OnAnimationMovementFinished(Vector2Int targetPosition)
{
    Debug.Log($"[MovementComponent] Movement animation finished to {targetPosition}");

    // 추가 처리 필요시 여기서 수행
    // 예: 이동 완료 사운드, 먼지 효과 등
}
```

#### 수정: MoveToPosition()

```csharp
public MovementResult MoveToPosition(Vector2Int targetPosition, bool useMovementPoints = true)
{
    var startPosition = gridManager?.GetUnitPosition(gameObject) ?? Vector2Int.zero;

    if (!CanMoveTo(targetPosition))
    {
        return MovementResult.Failed(startPosition, "Cannot move to target position");
    }

    OnMovementStarted?.Invoke(startPosition, targetPosition);
    isMoving = true;

    try
    {
        // 그리드 위치 업데이트 (즉시)
        // 애니메이션은 시각적 효과만, 로직은 즉시 처리
        if (gridManager.MoveUnit(gameObject, startPosition, targetPosition))
        {
            // 🎬 이동 애니메이션 재생 (void 메서드 직접 호출)
            if (animationController != null)
            {
                animationController.PlayMoveAnimation(startPosition, targetPosition);
            }

            if (useMovementPoints)
            {
                int movementCost = CalculateMovementCost(startPosition, targetPosition);
                ConsumeMovementPoints(movementCost);
            }

            hasMovedThisTurn = true;

            var result = MovementResult.Succeeded(startPosition, targetPosition, null,
                                                1, 0f, "Movement successful");

            OnMovementCompleted?.Invoke(startPosition, targetPosition);
            return result;
        }
        else
        {
            return MovementResult.Failed(startPosition, "Failed to move unit on grid");
        }
    }
    finally
    {
        isMoving = false;
    }
}
```

**핵심 변경사항**:
1. **코루틴 제거**: `StartCoroutine` → 직접 `PlayMoveAnimation()` 호출
2. **즉시 그리드 업데이트**: 애니메이션 전에 그리드 위치 변경
3. **이벤트 구독**: `OnMovementFinished` 구독으로 추가 처리 가능 (선택적)
4. **간결한 코드**: 타이밍 계산 코드 완전 제거

---

### 5. CombatComponent Integration (Event-Driven)

**목적**: `OnAttackHit` 이벤트 구독하여 정확한 타이밍에 데미지 적용

**핵심 변경사항**:
- **이벤트 구독**: `OnAttackHit` 이벤트 구독 (Animation Event가 호출)
- **타이밍 분리**: 애니메이션 시작과 데미지 적용 완전 분리
- **코루틴 제거**: 타이밍 계산 코드 완전 제거
- **정확한 타이밍**: Animation Clip의 정확한 타격 프레임에서 데미지 적용

#### 수정: Awake/OnDestroy (이벤트 구독/해제)

```csharp
// CombatComponent.cs 수정 부분

private IAnimationController animationController;

private void Awake()
{
    // ... 기존 초기화 코드 ...

    animationController = GetComponent<IAnimationController>();

    if (animationController != null)
    {
        // 공격 히트 이벤트 구독 (중요!)
        animationController.OnAttackHit += OnAnimationAttackHit;
    }
}

private void OnDestroy()
{
    if (animationController != null)
    {
        animationController.OnAttackHit -= OnAnimationAttackHit;
    }
}
```

#### 수정: OnAnimationAttackHit (Animation Event 콜백)

```csharp
/// <summary>
/// Animation Event에서 호출되는 실제 데미지 적용 메서드
/// AnimEvent_OnAttackImpact() → OnAttackHit 이벤트 → 이 메서드 호출
/// </summary>
private void OnAnimationAttackHit(GameObject target)
{
    if (target == null) return;

    var targetHealth = target.GetComponent<IHealthComponent>();
    if (targetHealth == null || !targetHealth.IsAlive)
    {
        Debug.LogWarning($"[CombatComponent] Attack hit but target {target.name} has no health or is dead");
        return;
    }

    // 데미지 계산 및 적용
    bool isCritical = RollCritical();
    int baseDamage = CurrentAttackPower;
    int finalDamage = CalculateFinalDamage(baseDamage, isCritical);

    targetHealth.TakeDamage(finalDamage);

    Debug.Log($"[CombatComponent] {gameObject.name} hit {target.name} for {finalDamage} damage" +
              (isCritical ? " (CRITICAL!)" : ""));

    // 이벤트 발생
    var result = CombatResult.Hit(finalDamage, target, attackType, isCritical,
                                  isCritical ? "Critical hit!" : "Attack hit!");
    OnAttackPerformed?.Invoke(target, result);

    if (isCritical)
    {
        OnCriticalAttack?.Invoke(target, result);
    }
}
```

#### 수정: Attack (공격 메서드)

```csharp
/// <summary>
/// 공격 수행 (애니메이션만 트리거, 실제 데미지는 OnAnimationAttackHit에서)
/// </summary>
public CombatResult Attack(GameObject target, bool forceCritical = false)
{
    if (!CanAttack(target))
    {
        return CombatResult.Failed("Cannot attack target");
    }

    OnAttackStarted?.Invoke(target);
    lastAttackTime = Time.time;
    EnterCombat();

    // 🎬 애니메이션 재생 (void 메서드 직접 호출)
    // 데미지는 OnAnimationAttackHit에서 적용됨 (Animation Event 타이밍)
    if (animationController != null)
    {
        animationController.PlayAttackAnimation(target);

        // 임시 결과 반환 (실제 결과는 OnAnimationAttackHit 이벤트로 전달)
        return CombatResult.Hit(0, target, attackType, false, "Attack animation started");
    }
    else
    {
        // 애니메이션 없으면 즉시 데미지 적용 (fallback)
        OnAnimationAttackHit(target);
        return CombatResult.Hit(CurrentAttackPower, target, attackType, false, "Instant attack");
    }
}
```

**핵심 변경사항**:
1. **이벤트 구독**: `OnAttackHit` 이벤트 구독하여 데미지 적용
2. **타이밍 정확성**: Animation Clip의 정확한 타격 프레임에서 데미지 (코드 수정 없음)
3. **코루틴 완전 제거**: `ApplyDamageAfterDelay` 등 타이밍 계산 코드 삭제
4. **Fallback**: 애니메이션 없을 시 즉시 데미지 적용
5. **확장성**: 다단 히트 공격도 Animation Event 추가만으로 구현 가능

**다단 히트 공격 예시**:
```
Attack Animation Clip:
- Frame 0.2: AnimEvent_OnAttackImpact() → 첫 번째 히트
- Frame 0.4: AnimEvent_OnAttackImpact() → 두 번째 히트
- Frame 0.6: AnimEvent_OnAttackImpact() → 세 번째 히트

코드 수정 불필요! OnAttackHit 이벤트가 3번 호출됨
```

---

## 🔄 Sequence Diagrams (Animation Event Based)

### Movement Animation Flow (Event-Driven)

```
User/AI     Unit    MovementComp   AnimController   Animator   UnitService
   |          |           |              |             |            |
   |--Act()-->|           |              |             |            |
   |          |--MoveTo()->|              |             |            |
   |          |           |--PlayMove()-->|             |            |
   |          |           |              |--Trigger--->|            |
   |          |           |              |             |            |
   |          |           |              | [Animation Playing]      |
   |          |           |              |<-Frame 0-   |            |
   |          |           |              |-AnimEvent_  |            |
   |          |           |              | OnStart()   |            |
   |          |           |<-OnAnimationStarted--------|            |
   |          |           |              |-IsPlaying=T |            |
   |          |           |              |             |            |--WaitUntil
   |          |           |              |             |            |  (!IsPlaying)
   |          |           |              |             |            |
   |          |           |              |<-Frame End--|            |
   |          |           |              |-AnimEvent_  |            |
   |          |           |              | OnEnd()     |            |
   |          |           |<-OnAnimationComplete-------|            |
   |          |           |<-OnMovementFinished--------|            |
   |          |           |              |-IsPlaying=F |            |
   |          |<-Result---|              |             |            |--NextUnit-->
   |          |           |              |             |            |
```

### Attack Animation Flow (Event-Driven)

```
User/AI     Unit    CombatComp     AnimController   Animator   HealthComp
   |          |           |              |             |            |
   |--Act()-->|           |              |             |            |
   |          |--Attack()->|              |             |            |
   |          |           |--PlayAttack>|             |            |
   |          |           |  (target)    |--Trigger--->|            |
   |          |           |              |             |            |
   |          |           |              | [Animation Playing]      |
   |          |           |              |<-Frame 0-   |            |
   |          |           |              |-AnimEvent_  |            |
   |          |           |              | OnStart()   |            |
   |          |           |<-OnAnimationStarted--------|            |
   |          |           |              |-IsPlaying=T |            |
   |          |           |              |             |            |
   |          |           |              |<-타격Frame- |            |
   |          |           |              |-AnimEvent_  |            |
   |          |           |              | OnAttackImpact()         |
   |          |           |<-OnAttackHit(target)-------|            |
   |          |           |--TakeDamage(finalDamage)-->|            |
   |          |           |              |             |            |--Health-=
   |          |           |              |             |            |
   |          |           |              |<-Frame End--|            |
   |          |           |              |-AnimEvent_  |            |
   |          |           |              | OnEnd()     |            |
   |          |           |<-OnAnimationComplete-------|            |
   |          |           |              |-IsPlaying=F |            |
   |          |<-Result---|              |             |            |
   |          |           |              |             |            |
```

**핵심 차이점**:
- **타이밍 제어**: Animation Clip의 프레임에서 직접 Event 호출
- **코드 간결성**: 타이밍 계산 로직 완전 제거
- **정확성**: 애니메이션 프레임과 게임플레이 로직 완벽 동기화
- **확장성**: 새로운 Animation Event 추가만으로 복잡한 타이밍 구현

---

## 🚀 Implementation Phases (Animation Event Based)

### Phase 1: Core Animation System (1-2일)
**Priority: High**

1. **IAnimationController 인터페이스 작성 (Event-Driven)**
   - 파일: `Assets/Script/Game/Interfaces/IAnimationController.cs`
   - 내용: 이벤트 기반 인터페이스 정의, 게임플레이 이벤트 추가

2. **UnitAnimationController 구현 (Animation Event 콜백)**
   - 파일: `Assets/Script/Game/Components/UnitAnimationController.cs`
   - 내용: `AnimEvent_*` 콜백 메서드, 이벤트 발생 로직

3. **Unit.cs에 참조 추가**
   ```csharp
   private IAnimationController animationController;
   public IAnimationController GetAnimationController() => animationController;

   private void InitializeComponents()
   {
       animationController = GetComponent<IAnimationController>();
   }
   ```

4. **기본 테스트 (Animation Event 없이)**
   - Unity Editor에서 Unit에 UnitAnimationController 추가
   - 애니메이션 트리거 동작 확인

---

### Phase 2: Component Integration (1-2일)
**Priority: Critical**

1. **CombatComponent 수정 (이벤트 구독)**
   - `OnAttackHit` 이벤트 구독
   - `OnAnimationAttackHit` 메서드 구현

2. **MovementComponent 수정 (선택적)**
   - `OnMovementFinished` 이벤트 구독 (필요시)

3. **UnitService 수정**
   - `OnAnimationComplete` 대기 로직

---

### Phase 3: Animation Event Setup (1일) ⭐ **핵심!**
**Priority: High**

1. **Move Animation에 Event 추가**
   - Animation 창 열기 (Window → Animation → Animation)
   - Move Animation Clip 선택
   - Frame 0: `AnimEvent_OnAnimationStart` 추가
   - 마지막 Frame: `AnimEvent_OnAnimationEnd` 추가

2. **Attack Animation에 Event 추가**
   - Attack Animation Clip 선택
   - Frame 0: `AnimEvent_OnAnimationStart` 추가
   - 타격 Frame (60% 지점): `AnimEvent_OnAttackImpact` 추가
   - 마지막 Frame: `AnimEvent_OnAnimationEnd` 추가

3. **타이밍 조정 및 테스트**
   - 공격 히트 타이밍 최적화
   - 느낌 개선 (Animation Event 위치 드래그)
   - **코드 수정 없이 타이밍 조정 가능!**

---

### Phase 4: Polish & Optimization (1일)
**Priority: Medium**

1. **애니메이션 속도 제어**
   - Inspector에서 animationSpeedMultiplier 조정
   - 게임 설정에서 속도 옵션 추가

2. **애니메이션 스킵 기능**
   - skipAnimations 플래그
   - 플레이어 설정과 연동

3. **VFX 통합 준비**
   - 이동/공격 시 파티클 효과 훅 추가
   - OnAnimationStarted 이벤트 활용

4. **최종 테스트**
   - 성능 프로파일링
   - 엣지 케이스 테스트
   - QA 검증

---

## ⚠️ Edge Cases & Error Handling

### 1. 애니메이션 중단 (Unit Death)
**문제**: 유닛이 애니메이션 재생 중 사망
**해결책**:
```csharp
// UnitAnimationController.OnDestroy()
private void OnDestroy()
{
    StopCurrentAnimation();
}

// Unit.Die()에서도 호출
private void Die()
{
    var animController = GetComponent<IAnimationController>();
    animController?.StopCurrentAnimation();

    Destroy(gameObject);
}
```

### 2. Animator 누락
**문제**: Animator 컴포넌트가 없는 유닛
**해결책**:
```csharp
if (animator == null)
{
    Debug.LogWarning($"No Animator on {gameObject.name}, using instant actions");
    skipAnimations = true;
}
```

### 3. 애니메이션 타임아웃
**문제**: 애니메이션이 예상보다 길게 재생
**해결책**: UnitService의 timeout 메커니즘 (이미 구현됨)

### 4. 빠른 연속 액션
**문제**: 애니메이션 재생 중 다시 Act() 호출
**해결책**:
```csharp
public void Act()
{
    if (animationController != null && animationController.IsAnimationPlaying)
    {
        Debug.LogWarning($"{gameObject.name} is still animating, ignoring action");
        return;
    }

    // 기존 로직...
}
```

### 5. 성능 저하
**문제**: 많은 유닛 애니메이션으로 프레임 드롭
**해결책**:
- LOD 시스템: 멀리 있는 유닛은 간단한 애니메이션
- 애니메이션 풀링
- GPU Skinning 활용

### 6. 유닛 죽음 애니메이션 (Death Animation)
**문제**: 유닛이 사망할 때 즉시 제거되어 시각적으로 부자연스러움
**해결책**: Death Animation Event 추가 및 처리

#### Death Animation Event 설정
```
Death Animation (1.0초):
[●═══════════════════════●]
 0.0s                   1.0s
 ↑                       ↓
 AnimEvent_              AnimEvent_
 OnAnimationStart        OnDeathComplete
```

#### UnitAnimationController에 Death 이벤트 추가
```csharp
// Animation Parameter 추가
private const string DEATH_TRIGGER = "Death";

/// <summary>
/// 죽음 애니메이션 재생
/// </summary>
public void PlayDeathAnimation()
{
    if (skipAnimations)
    {
        // 애니메이션 스킵 시 즉시 완료
        OnDeathComplete?.Invoke();
        return;
    }

    currentAnimationType = "Death";

    if (useAnimator && animator != null)
    {
        animator.SetTrigger(DEATH_TRIGGER);
        animator.SetFloat(ANIMATION_SPEED, animationSpeedMultiplier);
    }
}

/// <summary>
/// Animation Event: 죽음 애니메이션 완료 시 호출
/// Death Animation Clip의 마지막 프레임에 설정
/// </summary>
public void AnimEvent_OnDeathComplete()
{
    if (logAnimationEvents)
        Debug.Log($"[AnimationController] {gameObject.name}: Death animation complete");

    OnDeathComplete?.Invoke();
}

// IAnimationController 인터페이스에 추가
public event Action OnDeathComplete; // 죽음 애니메이션 완료
```

#### HealthComponent 통합
```csharp
// HealthComponent.cs 수정 부분

private IAnimationController animationController;

private void Awake()
{
    animationController = GetComponent<IAnimationController>();

    if (animationController != null)
    {
        animationController.OnDeathComplete += OnDeathAnimationComplete;
    }
}

private void OnDestroy()
{
    if (animationController != null)
    {
        animationController.OnDeathComplete -= OnDeathAnimationComplete;
    }
}

/// <summary>
/// 죽음 처리 (애니메이션과 함께)
/// </summary>
private void Die()
{
    if (isDying) return; // 중복 방지
    isDying = true;

    isAlive = false;

    Debug.Log($"[HealthComponent] {gameObject.name} has died");
    OnDeath?.Invoke();

    // 죽음 애니메이션 재생
    if (animationController != null)
    {
        animationController.PlayDeathAnimation();
        // 실제 제거는 OnDeathAnimationComplete에서
    }
    else
    {
        // 애니메이션 없으면 즉시 제거
        DestroyUnit();
    }
}

/// <summary>
/// 죽음 애니메이션 완료 후 호출
/// </summary>
private void OnDeathAnimationComplete()
{
    DestroyUnit();
}

/// <summary>
/// 유닛 제거 처리
/// </summary>
private void DestroyUnit()
{
    // GridManager에서 제거
    var gridManager = ServiceLocator.Instance?.Get<GridManager>();
    if (gridManager != null)
    {
        var position = gridManager.GetUnitPosition(gameObject);
        if (position != GridManager.INVALID_POSITION)
        {
            gridManager.RemoveUnit(gameObject, position);
        }
    }

    // GameObject 제거
    Destroy(gameObject, 0.1f); // 약간의 딜레이로 안전하게 제거
}

private bool isDying = false; // 중복 처리 방지 플래그
```

#### Death Animation Timeline 예시
```
Death Animation 상세 타이밍:
[●══════════════════════════════●]
 0.0s                         1.0s
 ↑                             ↓
 AnimEvent_OnAnimationStart    AnimEvent_OnDeathComplete
 - isAlive = false             - RemoveUnit()
 - Disable collider            - Destroy GameObject
 - Play sound
 - Trigger VFX
```

#### 죽음 처리 플로우
```
Unit HP → 0
    ↓
HealthComponent.Die()
    ↓
PlayDeathAnimation() → Animator Trigger
    ↓
[Death Animation Playing 1.0s]
    ↓
AnimEvent_OnDeathComplete()
    ↓
OnDeathAnimationComplete()
    ↓
RemoveUnit() from Grid
    ↓
Destroy(gameObject)
```

#### 중요 고려사항
1. **중복 방지**: `isDying` 플래그로 Die() 중복 호출 방지
2. **콜라이더 비활성화**: 죽음 애니메이션 중 상호작용 차단
3. **그리드 제거 타이밍**: 애니메이션 완료 후 그리드에서 제거
4. **타임아웃**: 애니메이션이 완료되지 않을 경우를 대비한 안전장치 추가 권장
5. **VFX/SFX**: 죽음 이펙트는 AnimEvent_OnAnimationStart에서 트리거

---

## 🧪 Testing Strategy

### Unit Tests
```csharp
[Test]
public void AnimationController_PlayMoveAnimation_SetsIsPlayingTrue()
{
    // Arrange
    var controller = CreateTestAnimationController();

    // Act
    StartCoroutine(controller.PlayMoveAnimation(Vector2Int.zero, Vector2Int.one));

    // Assert
    Assert.IsTrue(controller.IsAnimationPlaying);
}

[Test]
public void UnitService_WaitsForAnimationCompletion()
{
    // Arrange
    var unit = CreateTestUnitWithAnimation();
    var service = CreateTestUnitService();

    // Act
    float startTime = Time.time;
    yield return service.ProcessUnitActionAsync(unit, TurnPhase.AllyAction);
    float endTime = Time.time;

    // Assert
    Assert.GreaterOrEqual(endTime - startTime, 0.5f); // 최소 애니메이션 시간
}
```

### Integration Tests
1. 단일 유닛 이동 + 애니메이션
2. 단일 유닛 공격 + 애니메이션
3. 여러 유닛 순차 실행
4. 애니메이션 중 유닛 사망
5. Animator 없는 유닛 처리

### Manual Tests
1. 플레이어 유닛 이동 시 애니메이션 확인
2. 적 유닛 공격 시 애니메이션 확인
3. 연속된 유닛 행동 시 차단 확인
4. 애니메이션 속도 조정 테스트
5. 애니메이션 스킵 토글 테스트

---

## ⚡ Performance Considerations

### 예상 성능 영향
- **애니메이션 추가 시간**: 유닛당 0.5-1.0초
- **총 턴 시간 증가**: 10 유닛 × 1초 = ~10초/턴
- **프레임레이트**: 최소 60 FPS 유지 목표

### 최적화 전략
1. **애니메이션 길이 조절**
   - 기본: 0.5초 (빠른 템포)
   - 설정 옵션으로 0.3-1.0초 범위 제공

2. **LOD (Level of Detail)**
   - 카메라 거리에 따라 애니메이션 품질 조절
   - 먼 유닛: Simple Animation
   - 가까운 유닛: Full Animation

3. **애니메이션 스킵**
   - 플레이어 설정에 "빠른 애니메이션" 옵션
   - skipAnimations = true 시 즉시 실행

4. **배치 최적화**
   - Animator Culling Mode: Always Animate (중요 유닛)
   - Animator Culling Mode: Cull Update Transforms (배경 유닛)

---

## 🎮 Configuration Options

### Inspector 설정 (UnitAnimationController)
```csharp
[Header("Animation Durations")]
[Range(0.1f, 2.0f)]
public float moveAnimationDuration = 0.5f;

[Range(0.1f, 2.0f)]
public float attackAnimationDuration = 0.6f;

[Header("Performance")]
[Tooltip("Skip all animations for this unit")]
public bool skipAnimations = false;

[Range(0.1f, 3.0f)]
[Tooltip("Animation speed multiplier (1.0 = normal)")]
public float animationSpeedMultiplier = 1.0f;
```

### 게임 설정 연동
```csharp
// GameSettings.cs
public class GameSettings
{
    public static float GlobalAnimationSpeed = 1.0f;
    public static bool EnableUnitAnimations = true;
}

// UnitAnimationController.cs
private void Awake()
{
    skipAnimations = !GameSettings.EnableUnitAnimations;
    animationSpeedMultiplier = GameSettings.GlobalAnimationSpeed;
}
```

---

## 🔮 Future Enhancements

### Phase 5: Advanced Features (추후 구현)
1. **Animation Queueing**
   - 여러 액션을 큐에 저장
   - 순차 실행

2. **Camera Follow**
   - 행동하는 유닛으로 카메라 자동 이동
   - Cinemachine 통합

3. **VFX Integration**
   - 이동 시 먼지 효과
   - 공격 시 타격 효과
   - 크리티컬 히트 특수 효과

4. **Sound Effects**
   - 이동 사운드 (발소리)
   - 공격 사운드 (무기 소리)
   - AudioSource 통합

5. **Advanced Timing**
   - AnimationEvent를 통한 정밀한 타이밍
   - 다단 히트 공격 지원
   - 연계 애니메이션

6. **Procedural Animation**
   - IK (Inverse Kinematics) for foot placement
   - Look-at target during attack
   - Dynamic pose adjustment

---

## 🎬 Animation Event Setup Guide

### Unity Editor에서 Animation Event 설정하기

#### 1. Animation 창 열기
```
Window → Animation → Animation
```

#### 2. Move Animation Event 설정

**Step 1**: Move Animation Clip 선택
- Project 창에서 Move Animation Clip 선택
- 또는 Animator Controller에서 Move State 더블클릭

**Step 2**: Animation Event 추가
```
Frame 0 (애니메이션 시작):
   1. 타임라인 상단의 Event 트랙 클릭
   2. "Add Animation Event" 버튼 클릭
   3. Function: AnimEvent_OnAnimationStart

마지막 Frame (애니메이션 종료):
   1. 타임라인 끝으로 이동
   2. "Add Animation Event" 버튼 클릭
   3. Function: AnimEvent_OnAnimationEnd
   4. (선택적) Function: AnimEvent_OnMoveComplete
```

**Timeline 예시**:
```
Move Animation (0.5초):
[●═════════════════════════════●]
 0.0s                         0.5s
 ↑                             ↑
 AnimEvent_                   AnimEvent_
 OnAnimationStart             OnAnimationEnd
                              AnimEvent_OnMoveComplete
```

#### 3. Attack Animation Event 설정

**Step 1**: Attack Animation Clip 선택

**Step 2**: Animation Event 추가
```
Frame 0 (애니메이션 시작):
   - Function: AnimEvent_OnAnimationStart

Frame 타격 프레임 (약 60% 지점, 예: 0.36초):
   - Function: AnimEvent_OnAttackImpact
   - 💡 Tip: 애니메이터의 실제 타격 모션이 나오는 프레임에 정확히 배치

마지막 Frame (애니메이션 종료, 예: 0.6초):
   - Function: AnimEvent_OnAnimationEnd
```

**Timeline 예시**:
```
Attack Animation (0.6초):
[●═══════════●═══════════●]
 0.0s       0.36s       0.6s
 ↑           ↑           ↑
 AnimEvent_  AnimEvent_  AnimEvent_
 OnStart     OnAttackImpact  OnEnd
```

#### 4. Animation Event Inspector 설정

Animation Event를 클릭하면 Inspector에 상세 설정이 표시됩니다:

```yaml
Animation Event:
  Time: 0.0          # 이벤트 발생 시점 (초)
  Function: AnimEvent_OnAnimationStart  # 호출할 메서드 이름

  # 파라미터 (필요시)
  # Float: 0
  # Int: 0
  # String: ""
  # Object: None
```

#### 5. 타이밍 조정 방법

**드래그로 조정**:
- Timeline의 Event 마커를 드래그하여 위치 조정
- 정확한 프레임에 배치 가능

**Inspector에서 조정**:
- Time 필드에 직접 초 단위 입력
- 예: `0.36` → 0.36초 시점

**재생하며 테스트**:
1. Animation 창에서 재생 버튼 클릭
2. 타격 느낌이 맞는지 확인
3. 어색하면 Event 위치 조정
4. **코드 수정 없이 즉시 반영!**

#### 6. 다단 히트 공격 설정 예시

**3-Hit Combo Attack Animation**:
```
Combo Attack Animation (1.0초):
[●══●══●══●]
 0.0 0.3 0.6 1.0
 ↑   ↑   ↑   ↑
 Start  Hit1 Hit2 Hit3
       Impact Impact End
```

**Event 설정**:
```yaml
Events:
  - Time: 0.0
    Function: AnimEvent_OnAnimationStart

  - Time: 0.3
    Function: AnimEvent_OnAttackImpact  # 첫 번째 히트

  - Time: 0.6
    Function: AnimEvent_OnAttackImpact  # 두 번째 히트

  - Time: 0.85
    Function: AnimEvent_OnAttackImpact  # 세 번째 히트

  - Time: 1.0
    Function: AnimEvent_OnAnimationEnd
```

**결과**: `OnAttackHit` 이벤트가 3번 호출되어 3번 데미지 적용!

#### 7. 스킬 애니메이션 설정 예시 (추후 확장)

```
Skill Animation (1.2초):
[●══●══════●]
 0.0 0.5    1.2
 ↑   ↑      ↑
 Start Cast  End

Events:
  - Time: 0.0
    Function: AnimEvent_OnAnimationStart

  - Time: 0.5
    Function: AnimEvent_OnSkillCast  # 스킬 발동 시점

  - Time: 1.2
    Function: AnimEvent_OnAnimationEnd
```

#### 8. 검증 체크리스트

Animation Event 설정 후 확인:
- [ ] Function 이름이 정확한가? (오타 없음)
- [ ] 모든 Animation Clip에 Start/End Event가 있는가?
- [ ] Attack Animation에 Impact Event가 있는가?
- [ ] 타격 타이밍이 자연스러운가?
- [ ] 재생 시 Console에 로그가 출력되는가?

#### 9. 문제 해결

**Animation Event가 호출되지 않을 때**:
1. Function 이름 오타 확인 (`AnimEvent_OnAttackImpact`)
2. UnitAnimationController 컴포넌트가 Unit에 붙어있는지 확인
3. Animator에 Animation Clip이 올바르게 연결되어 있는지 확인
4. `logAnimationEvents = true` 설정하여 Console 로그 확인

**타이밍이 맞지 않을 때**:
1. Animation Event Time 값 확인
2. AnimationSpeed 배율 확인
3. Animation Clip 자체의 길이 확인

---

## 📝 Implementation Checklist (Animation Event Based)

### Phase 1: Core System (Event-Driven)
- [ ] IAnimationController 인터페이스 작성 (이벤트 기반)
- [ ] UnitAnimationController 구현 (AnimEvent 콜백)
- [ ] Unit.cs에 참조 추가
- [ ] 기본 동작 테스트 (Animation Event 없이)

### Phase 2: Component Integration
- [ ] CombatComponent 수정 (OnAttackHit 구독)
- [ ] MovementComponent 수정 (OnMovementFinished 구독)
- [ ] UnitService 수정 (OnAnimationComplete 대기)
- [ ] 이벤트 구독 테스트

### Phase 3: Animation Event Setup ⭐
- [ ] Move Animation에 Start/End Event 추가
- [ ] Attack Animation에 Start/Impact/End Event 추가
- [ ] Animation Event 동작 확인
- [ ] 타이밍 조정 및 최적화

### Phase 4: Testing & Polish
- [ ] 통합 테스트 (이동 + 공격)
- [ ] 다단 히트 공격 테스트 (확장성 검증)
- [ ] 애니메이션 속도 제어 테스트
- [ ] 최종 QA

---

## 🎯 Success Criteria (Animation Event Based)

### 필수 요구사항
✅ 유닛 이동 시 애니메이션 재생
✅ 유닛 공격 시 애니메이션 재생
✅ 애니메이션 재생 중 다른 유닛 차단
✅ UnitService가 애니메이션 완료 대기
✅ **Animation Event로 정확한 타이밍 제어**
✅ **코드 수정 없이 타이밍 조정 가능**

### 품질 기준
✅ 60 FPS 유지
✅ 애니메이션 부드러움 (no jitter)
✅ 타임아웃 안전장치 동작
✅ 애니메이션 없을 시 graceful degradation
✅ 플레이어가 애니메이션 속도/스킵 제어 가능
✅ **타격 타이밍이 Animation Clip과 완벽 동기화**

### 코드 품질
✅ SOLID 원칙 준수
✅ 기존 코드 최소 변경
✅ 이벤트 기반 설계 (느슨한 결합)
✅ 적절한 에러 핸들링
✅ 테스트 가능한 구조
✅ **타이밍 로직 분리 (애니메이션 vs 게임플레이)**

### 확장성
✅ 다단 히트 공격 지원 (Animation Event 추가만으로)
✅ 스킬 애니메이션 확장 가능
✅ 새로운 애니메이션 타입 쉽게 추가

---

## 📚 References

### Unity Documentation
- [Animator Component](https://docs.unity3d.com/Manual/class-Animator.html)
- [Animation Events](https://docs.unity3d.com/Manual/script-AnimationWindowEvent.html)
- [Coroutines](https://docs.unity3d.com/Manual/Coroutines.html)

### Third-Party Assets
- [Cinemachine](https://unity.com/unity/features/editor/art-and-design/cinemachine) - Camera system

### Project Files
- `UnitService.cs` - Phase execution system
- `MovementComponent.cs` - Movement logic
- `CombatComponent.cs` - Combat logic
- `Unit.cs` - Unit coordination

---

## 📄 Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-02 | Claude Code | Initial design document (코루틴 기반, 하드코딩 duration) |
| 2.0 | 2025-10-02 | Claude Code | **Major redesign**: Animation Event 기반 아키텍처로 전환<br>- 모든 duration 필드 제거<br>- IEnumerator → void 메서드 변경<br>- Animation Event 콜백 추가 (AnimEvent_*)<br>- 이벤트 기반 아키텍처 적용<br>- Animation Event Setup Guide 추가<br>- 타이밍 제어를 코드에서 Animation Clip으로 이전<br>- 확장성 및 유지보수성 대폭 향상 |

---

## 🤝 Contributors & Review

**설계자**: Claude Code
**검토자**: [To be assigned]
**승인자**: [To be assigned]

---

**End of Document**
