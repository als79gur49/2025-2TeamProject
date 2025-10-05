# CombatComponent BlendTree 통합 가이드

## 개요

CombatComponent를 MovementComponent 패턴과 일치하도록 BlendTree 애니메이션 시스템과 통합했습니다.

## 주요 변경사항

### 1. 공격 상태 추적 필드 추가

```csharp
// 공격 상태 추적 (BlendTree 애니메이션 동기화용)
private bool isAttacking = false;
private GameObject currentAttackTarget = null;
private bool isSpecialAttackActive = false;
```

**목적**: BlendTree 애니메이션 생명주기 동안 공격 상태를 추적하여 중복 공격 방지 및 타겟 검증

### 2. BlendTree 이벤트 구독 (MovementComponent 패턴)

```csharp
private void Awake()
{
    // BlendTree 애니메이션 이벤트 구독
    if (animationController != null)
    {
        animationController.OnAttackStart += OnAnimationAttackStart;
        animationController.OnAttackHit += OnAnimationAttackHit;
        animationController.OnAttackEnd += OnAnimationAttackEnd;
    }
}

private void OnDestroy()
{
    // BlendTree 애니메이션 이벤트 구독 해제
    if (animationController != null)
    {
        animationController.OnAttackStart -= OnAnimationAttackStart;
        animationController.OnAttackHit -= OnAnimationAttackHit;
        animationController.OnAttackEnd -= OnAnimationAttackEnd;
    }
}
```

**변경점**:
- 기존: `OnAttackHit` 이벤트만 구독
- 신규: `OnAttackStart`, `OnAttackHit`, `OnAttackEnd` 세 단계 모두 구독

### 3. Attack() 메서드 리팩터링

```csharp
public CombatResult Attack(GameObject target)
{
    if (!CanAttackTarget(target))
    {
        return CombatResult.Failed("Cannot attack target");
    }

    if (isAttacking)
    {
        Debug.LogWarning($"[CombatComponent] {gameObject.name} is already attacking!");
        return CombatResult.Failed("Already attacking");
    }

    // 공격 상태 시작
    isAttacking = true;
    currentAttackTarget = target;
    isSpecialAttackActive = false;
    lastAttackTime = Time.time;
    EnterCombat();

    // BlendTree 애니메이션 재생 (데미지는 OnAnimationAttackHit에서 적용)
    if (animationController != null)
    {
        animationController.PlayAttackAnimation(target);
        return CombatResult.Hit(0, target, attackType, false, "Attack animation started");
    }
    else
    {
        // 애니메이션 없으면 즉시 데미지 적용 (fallback)
        isAttacking = false;
        currentAttackTarget = null;
        return ApplyDamageToTarget(target, false);
    }
}
```

**핵심 변경**:
1. 중복 공격 방지 검사 추가
2. 공격 상태 플래그 설정 (`isAttacking`, `currentAttackTarget`)
3. 애니메이션 재생만 담당 (데미지는 이벤트 핸들러에서 적용)
4. 임시 결과 반환 (데미지 0, 실제 결과는 이벤트로 전달)

### 4. PerformSpecialAttack() 메서드 리팩터링

```csharp
public CombatResult PerformSpecialAttack(GameObject target)
{
    if (!CanUseSpecialAttack)
    {
        return CombatResult.Failed("Special attack not available");
    }

    if (!CanAttackTarget(target))
    {
        return CombatResult.Failed("Cannot attack target with special attack");
    }

    if (isAttacking)
    {
        Debug.LogWarning($"[CombatComponent] {gameObject.name} is already attacking!");
        return CombatResult.Failed("Already attacking");
    }

    // 공격 상태 시작 (특수 공격)
    isAttacking = true;
    currentAttackTarget = target;
    isSpecialAttackActive = true;
    lastSpecialAttackTurn = (int)Time.fixedTime;
    lastAttackTime = Time.time;
    EnterCombat();

    // BlendTree 애니메이션 재생
    if (animationController != null)
    {
        animationController.PlayAttackAnimation(target);
        return CombatResult.Hit(0, target, attackType, false, "Special attack animation started");
    }
    else
    {
        // 애니메이션 없으면 즉시 데미지 적용
        isAttacking = false;
        currentAttackTarget = null;
        isSpecialAttackActive = false;
        var result = ApplyDamageToTarget(target, true);

        if (result.Success)
        {
            OnSpecialAttack?.Invoke(target, result);
        }

        return result;
    }
}
```

**특징**:
- `isSpecialAttackActive` 플래그로 일반 공격과 특수 공격 구분
- OnAnimationAttackHit에서 이 플래그를 보고 데미지 배율 적용

### 5. PerformAttack → ApplyDamageToTarget 메서드명 변경

```csharp
/// <summary>
/// 실제 데미지 적용 메서드 (애니메이션 없이 즉시 적용)
/// </summary>
private CombatResult ApplyDamageToTarget(GameObject target, bool isSpecialAttack, bool forceCritical = false)
{
    if (target == null)
    {
        return CombatResult.Failed("Target is null");
    }

    var targetHealth = target.GetComponent<IHealthComponent>();
    if (targetHealth == null || !targetHealth.IsAlive)
    {
        return CombatResult.Failed("Target has no health or is dead");
    }

    // 크리티컬 판정
    bool isCritical = forceCritical || this.RollCritical();

    // 피해량 계산
    int baseDamage = isSpecialAttack ? CurrentAttackPower * specialAttackDamageMultiplier : CurrentAttackPower;
    int finalDamage = this.CalculateFinalDamage(baseDamage, isCritical);

    // 방어력 관통 적용
    if (CanPierceArmor && targetHealth is IAdvancedHealthComponent advancedHealth)
    {
        var damageInfo = new DamageInfo(finalDamage, attackType, gameObject, isCritical, armorPenetration > 0.5f);
        targetHealth.TakeDamage(finalDamage);
    }
    else
    {
        targetHealth.TakeDamage(finalDamage);
    }

    Debug.Log($"[CombatComponent] {gameObject.name} hit {target.name} for {finalDamage} damage" +
              (isCritical ? " (CRITICAL!)" : "") + (isSpecialAttack ? " (SPECIAL!)" : ""));

    var result = CombatResult.Hit(finalDamage, target, attackType, isCritical,
        isSpecialAttack ? "Special attack hit!" : (isCritical ? "Critical hit!" : "Attack hit!"));

    OnAttackPerformed?.Invoke(target, result);

    if (isCritical)
    {
        OnCriticalAttack?.Invoke(target, result);
    }

    if (isSpecialAttack)
    {
        OnSpecialAttack?.Invoke(target, result);
    }

    return result;
}
```

**변경 이유**:
- 메서드명이 역할을 더 명확히 표현 (`PerformAttack`은 애니메이션 포함, `ApplyDamageToTarget`은 순수 데미지만)
- 특수 공격 이벤트 발생 로직 추가

### 6. BlendTree 이벤트 핸들러 추가 (핵심)

#### OnAnimationAttackStart

```csharp
/// <summary>
/// BlendTree 공격 시작 핸들러
/// UnitAnimationController.OnAttackStart 이벤트 구독
/// </summary>
private void OnAnimationAttackStart(GameObject target)
{
    if (target == null)
    {
        Debug.LogWarning($"[CombatComponent] {gameObject.name}: Attack start but target is null");
        return;
    }

    Debug.Log($"[CombatComponent] {gameObject.name}: Attack animation started on {target.name}");

    // OnAttackStarted 이벤트 발생 (외부 시스템에 알림)
    OnAttackStarted?.Invoke(target);
}
```

**역할**:
- BlendTree 공격 애니메이션 시작 시점 감지
- 외부 시스템(VFX, SFX)에 공격 시작 알림

#### OnAnimationAttackHit (데미지 적용 시점)

```csharp
/// <summary>
/// BlendTree 공격 타격 핸들러 (데미지 적용 시점)
/// UnitAnimationController.OnAttackHit 이벤트 구독
/// 공격 진행도 60% 지점에서 호출됨
/// </summary>
private void OnAnimationAttackHit(GameObject target)
{
    // currentAttackTarget 검증 (애니메이션 이벤트의 target과 일치해야 함)
    if (currentAttackTarget == null)
    {
        Debug.LogWarning($"[CombatComponent] {gameObject.name}: Attack hit but no current target");
        return;
    }

    if (target != currentAttackTarget)
    {
        Debug.LogWarning($"[CombatComponent] {gameObject.name}: Attack hit target mismatch! " +
                         $"Expected {currentAttackTarget.name}, got {target?.name}");
        return;
    }

    // 실제 데미지 적용
    ApplyDamageToTarget(currentAttackTarget, isSpecialAttackActive);

    Debug.Log($"[CombatComponent] {gameObject.name}: Damage applied to {currentAttackTarget.name} " +
              $"(isSpecial: {isSpecialAttackActive})");
}
```

**핵심 로직**:
1. `currentAttackTarget` 검증 (공격 중단 또는 타겟 변경 감지)
2. 애니메이션 이벤트 target과 저장된 target 일치 검사
3. `isSpecialAttackActive` 플래그 기반 데미지 배율 적용
4. 실제 데미지 적용 (`ApplyDamageToTarget`)

#### OnAnimationAttackEnd

```csharp
/// <summary>
/// BlendTree 공격 완료 핸들러
/// UnitAnimationController.OnAttackEnd 이벤트 구독
/// </summary>
private void OnAnimationAttackEnd(GameObject target)
{
    if (currentAttackTarget == null)
    {
        Debug.LogWarning($"[CombatComponent] {gameObject.name}: Attack end but no current target");
        return;
    }

    Debug.Log($"[CombatComponent] {gameObject.name}: Attack animation ended on {currentAttackTarget.name}");

    // 공격 상태 초기화
    isAttacking = false;
    currentAttackTarget = null;
    isSpecialAttackActive = false;
}
```

**역할**:
- BlendTree 공격 애니메이션 완료 시점 감지
- 공격 상태 플래그 초기화 (다음 공격 허용)

## 동작 흐름

### 일반 공격 흐름

```
1. CombatComponent.Attack(target)
   ├─ isAttacking = true
   ├─ currentAttackTarget = target
   ├─ isSpecialAttackActive = false
   └─ animationController.PlayAttackAnimation(target)

2. UnitAnimationController.PlayAttackAnimation(target)
   └─ BlendTreeAnimationController.StartBlendTreeAttack(target)
       └─ OnBlendTreeAttackStart 이벤트 발생

3. UnitAnimationController.HandleBlendTreeAttackStart(target)
   └─ OnAttackStart 이벤트 재발행

4. CombatComponent.OnAnimationAttackStart(target)
   └─ OnAttackStarted 이벤트 발생 (외부 알림)

5. BlendTreeAnimationController (진행도 60%)
   └─ (Coroutine) OnAttackHit 이벤트 발생

6. CombatComponent.OnAnimationAttackHit(target)
   ├─ currentAttackTarget 검증
   └─ ApplyDamageToTarget(currentAttackTarget, false)
       ├─ 크리티컬 판정
       ├─ 데미지 계산
       ├─ targetHealth.TakeDamage(finalDamage)
       ├─ OnAttackPerformed 이벤트
       └─ OnCriticalAttack 이벤트 (크리티컬 시)

7. BlendTreeAnimationController (진행도 100%)
   └─ OnBlendTreeAttackEnd 이벤트 발생

8. UnitAnimationController.HandleBlendTreeAttackEnd(target)
   └─ OnAttackEnd 이벤트 재발행

9. CombatComponent.OnAnimationAttackEnd(target)
   ├─ isAttacking = false
   ├─ currentAttackTarget = null
   └─ isSpecialAttackActive = false
```

### 특수 공격 흐름

```
1. CombatComponent.PerformSpecialAttack(target)
   ├─ isAttacking = true
   ├─ currentAttackTarget = target
   ├─ isSpecialAttackActive = true  ← 차이점
   └─ animationController.PlayAttackAnimation(target)

2~5. (일반 공격과 동일)

6. CombatComponent.OnAnimationAttackHit(target)
   └─ ApplyDamageToTarget(currentAttackTarget, true)  ← isSpecialAttack = true
       ├─ baseDamage = CurrentAttackPower * specialAttackDamageMultiplier
       ├─ 데미지 계산 및 적용
       ├─ OnAttackPerformed 이벤트
       ├─ OnCriticalAttack 이벤트 (크리티컬 시)
       └─ OnSpecialAttack 이벤트  ← 특수 공격 이벤트

7~9. (일반 공격과 동일)
```

## MovementComponent와의 패턴 일치

| MovementComponent | CombatComponent | 공통점 |
|-------------------|-----------------|--------|
| `OnMoveStart` | `OnAttackStart` | 애니메이션 시작 알림 |
| `OnMoveEnd` | `OnAttackHit` | 핵심 동작 실행 (이동 완료 / 데미지 적용) |
| `OnMoveEnd` (최종) | `OnAttackEnd` | 애니메이션 완료 및 상태 초기화 |
| `isMoving` | `isAttacking` | 동작 중 플래그 |
| 목표 위치 추적 | `currentAttackTarget` | 동작 대상 추적 |
| Grid 좌표 변환 | 타겟 검증 | 동작 유효성 검사 |

## 장점

### 1. 애니메이션-로직 동기화 정확도
- **이전**: AnimationEvent 타이밍에 의존 (불확실)
- **현재**: BlendTree 진행도 기반 정확한 타이밍 (60% 지점 보장)

### 2. 중복 공격 방지
- `isAttacking` 플래그로 애니메이션 중 중복 공격 차단
- 연타 공격 버그 방지

### 3. 타겟 검증 강화
- `currentAttackTarget` 저장 및 검증
- 애니메이션 중 타겟 변경/사망 감지 가능

### 4. 일반/특수 공격 구분
- `isSpecialAttackActive` 플래그로 데미지 배율 차별화
- 동일한 애니메이션 흐름에서 로직만 분기

### 5. 이벤트 생명주기 명확화
- Start → Hit → End 단계별 이벤트
- 외부 시스템(VFX, SFX, UI) 연동 용이

## 테스트 체크리스트

### 기본 공격 테스트
- [ ] 일반 공격 데미지 정상 적용
- [ ] 크리티컬 공격 정상 작동
- [ ] 공격 중 중복 공격 차단 확인
- [ ] 공격 완료 후 다음 공격 가능 확인

### 특수 공격 테스트
- [ ] 특수 공격 데미지 배율 적용 (specialAttackDamageMultiplier)
- [ ] 특수 공격 쿨다운 정상 작동
- [ ] OnSpecialAttack 이벤트 발생 확인

### 애니메이션 동기화 테스트
- [ ] 공격 애니메이션 60% 지점에서 데미지 적용
- [ ] 공격 애니메이션 완료 시 상태 초기화
- [ ] 애니메이션 중단 시 상태 정리 확인

### 타겟 검증 테스트
- [ ] 공격 중 타겟 사망 시 정상 처리
- [ ] 공격 중 타겟 변경 시 경고 로그
- [ ] null 타겟 공격 시 실패 반환

### 이벤트 테스트
- [ ] OnAttackStarted 이벤트 발생 확인
- [ ] OnAttackPerformed 이벤트 발생 확인
- [ ] OnCriticalAttack 이벤트 발생 확인 (크리티컬 시)
- [ ] OnSpecialAttack 이벤트 발생 확인 (특수 공격 시)

### Edge Case 테스트
- [ ] 애니메이션 없는 유닛 공격 (fallback 로직)
- [ ] 공격 범위 밖 타겟 공격 시도
- [ ] 공격 쿨다운 중 공격 시도
- [ ] 죽은 유닛 공격 시도

## 향후 개선 가능성

### 1. 강제 크리티컬 공격
`PerformCriticalAttack()` 메서드에서 강제 크리티컬 플래그 지원:

```csharp
// CombatComponent에 필드 추가
private bool forceCriticalAttack = false;

public CombatResult PerformCriticalAttack(GameObject target)
{
    forceCriticalAttack = true;
    var result = Attack(target);
    return result;
}

// OnAnimationAttackHit에서 사용
private void OnAnimationAttackHit(GameObject target)
{
    ApplyDamageToTarget(currentAttackTarget, isSpecialAttackActive, forceCriticalAttack);
    forceCriticalAttack = false;
}
```

### 2. 공격 애니메이션 속도 동기화
`attackSpeed` 필드를 BlendTreeAnimationController의 `attackDuration`과 연동:

```csharp
// BlendTreeAnimationController에 동적 duration 설정
public void SetAttackDuration(float duration)
{
    attackDuration = duration;
}

// CombatComponent.Attack()에서 호출
var blendTreeController = animationController.GetComponent<BlendTreeAnimationController>();
if (blendTreeController != null)
{
    blendTreeController.SetAttackDuration(1f / attackSpeed);
}
```

### 3. 공격 취소 기능
공격 중 상태 변화 시 애니메이션 중단:

```csharp
public void CancelAttack()
{
    if (isAttacking)
    {
        animationController?.StopCurrentAnimation();
        isAttacking = false;
        currentAttackTarget = null;
        isSpecialAttackActive = false;
        Debug.Log($"[CombatComponent] Attack cancelled");
    }
}
```

## 요약

CombatComponent가 MovementComponent와 동일한 BlendTree 이벤트 기반 패턴을 따르도록 리팩터링되었습니다.

**핵심 개선사항**:
1. ✅ 공격 상태 추적 (`isAttacking`, `currentAttackTarget`)
2. ✅ BlendTree 3단계 이벤트 구독 (Start/Hit/End)
3. ✅ 타겟 검증 강화 (mismatch 감지)
4. ✅ 일반/특수 공격 구분 (`isSpecialAttackActive`)
5. ✅ 애니메이션-데미지 정확한 동기화 (60% 타이밍)

이제 이동과 공격 모두 일관된 BlendTree 기반 시스템으로 통합되어 유지보수성과 확장성이 크게 향상되었습니다.
