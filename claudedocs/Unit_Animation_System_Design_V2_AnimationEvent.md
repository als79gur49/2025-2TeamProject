# Unit Animation System Design V2 - Animation Event Based
## 유닛 이동 및 공격 애니메이션 시스템 설계서 (Animation Event 방식)

**작성일**: 2025-10-02
**버전**: 2.0
**상태**: Design Complete - Animation Event Based

---

## 📋 Executive Summary

### 설계 변경 사유
**기존 방식 (V1)의 문제점**:
- 애니메이션 지속시간을 코드에 하드코딩 (`moveAnimationDuration = 0.5f`)
- 공격 타이밍을 코드로 계산 (`attackAnimationDuration * 0.6f`)
- 애니메이션 변경 시 코드 수정 필요
- 확장성 및 유지보수 매우 불리

**새로운 방식 (V2)의 장점**:
- Unity Animation Event 활용
- 애니메이션 프레임에 직접 함수 호출 삽입
- 코드 수정 없이 Animation Clip에서만 타이밍 조정
- 디자이너/아티스트가 직접 타이밍 제어 가능
- 확장성 및 유지보수 우수

### 핵심 요구사항
1. ✅ 유닛 이동 시 애니메이션 재생
2. ✅ 유닛 공격 시 애니메이션 재생 + 정확한 타이밍에 데미지 적용
3. ✅ 애니메이션 재생 중 다른 유닛 행동 차단
4. ✅ Animation Event를 통한 이벤트 기반 제어

### 설계 원칙
- **Animation Event 중심**: 타이밍 제어는 애니메이션 클립에서
- **이벤트 기반 아키텍처**: 느슨한 결합, 높은 확장성
- **코드 분리**: 애니메이션 로직과 게임 로직 분리
- **디자이너 친화적**: 코드 수정 없이 타이밍 조정 가능

---

## 🏗️ System Architecture Overview

### Animation Event 흐름도

```
┌─────────────────────────────────────────────────────────────┐
│                    Animator Controller                       │
│  - Animation Clips (Move, Attack, Idle)                     │
│  - Animation Events embedded in clips                       │
└──────────────────┬──────────────────────────────────────────┘
                   │ Triggers Animation Events
                   ↓
┌─────────────────────────────────────────────────────────────┐
│              UnitAnimationController                         │
│  - OnAnimationStart() ← Animation Event                     │
│  - OnAnimationEnd() ← Animation Event                       │
│  - OnAttackImpact() ← Animation Event                       │
│  - OnMoveComplete() ← Animation Event                       │
└──────────────────┬──────────────────────────────────────────┘
                   │ Invokes C# Events
                   ↓
┌─────────────────────────────────────────────────────────────┐
│              IAnimationController Events                     │
│  - OnAnimationStarted                                        │
│  - OnAnimationComplete                                       │
│  - OnAttackHit (공격 타이밍)                                │
│  - OnMovementFinished                                        │
└──────────────────┬──────────────────────────────────────────┘
                   │ Subscribed by
        ┌──────────┴──────────┐
        ↓                      ↓
┌──────────────────┐  ┌──────────────────┐
│ CombatComponent  │  │ MovementComponent│
│ - Applies damage │  │ - Updates grid   │
│   on OnAttackHit │  │   on OnMoveEnd   │
└──────────────────┘  └──────────────────┘
```

### 전체 구조도

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
│   OnMoveComplete │  │   OnAttackHit    │
└────────┬─────────┘  └────────┬─────────┘
         │                      │
         └──────────┬───────────┘
                    ↓
         ┌─────────────────────┐
         │ IAnimationController │
         │ - IsAnimationPlaying │
         │ - PlayMoveAnimation  │
         │ - PlayAttackAnimation│
         │ - Events (6 types)   │
         └──────────┬───────────┘
                    ↓
         ┌─────────────────────────┐
         │ UnitAnimationController │
         │ - Animator integration  │
         │ - Animation Event       │
         │   callbacks             │
         └─────────────────────────┘
                    ↑
                    │ Animation Events
         ┌─────────────────────────┐
         │   Animator Controller   │
         │ - Animation Clips       │
         │ - Embedded Events       │
         └─────────────────────────┘
```

---

## 📐 Component Specifications

### 1. IAnimationController Interface (Event-Driven)

**목적**: 애니메이션 제어 및 이벤트 기반 통신

```csharp
namespace Game.Interfaces
{
    public interface IAnimationController
    {
        // Properties
        bool IsAnimationPlaying { get; }
        float CurrentAnimationProgress { get; }
        GameObject CurrentTarget { get; } // 공격 대상 저장

        // Animation Methods (코루틴 제거, 직접 트리거만)
        void PlayMoveAnimation(Vector2Int from, Vector2Int to);
        void PlayAttackAnimation(GameObject target);
        void StopCurrentAnimation();
        void SetAnimationSpeed(float speed);

        // Events - Animation Event에서 호출됨
        event Action<string> OnAnimationStarted;      // 애니메이션 시작
        event Action OnAnimationComplete;             // 애니메이션 완료
        event Action OnAnimationInterrupted;          // 애니메이션 중단

        // Gameplay Events - 특정 타이밍에 게임플레이 로직 실행
        event Action<GameObject> OnAttackHit;         // 공격이 타격하는 순간
        event Action<Vector2Int> OnMovementFinished;  // 이동 완료 시점
        event Action OnSkillCast;                     // 스킬 발동 시점 (추후 확장)
    }
}
```

**주요 변경사항**:
- `IEnumerator` 반환 제거 → `void` 반환 (Animation Event 기반)
- 타이밍 관련 파라미터 제거 (duration 등)
- 게임플레이 이벤트 추가 (`OnAttackHit`, `OnMovementFinished`)
- `CurrentTarget` 프로퍼티 추가 (공격 대상 추적)

---

### 2. UnitAnimationController Implementation (Animation Event Based)

**목적**: Animation Event 수신 및 C# 이벤트로 변환

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
        private GameObject currentTarget;  // 공격 대상 추적
        private Vector2Int moveStartPosition;
        private Vector2Int moveTargetPosition;
        private string currentAnimationType; // "Move", "Attack", etc.

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

            // Animation Event "OnAnimationStart"가 호출되어 isAnimationPlaying = true 설정
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

            // Animation Event "OnAnimationStart" → "OnAttackImpact" → "OnAnimationEnd" 순서로 호출
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
        // 이 메서드들은 Unity Animation Event에서 호출됨

        /// <summary>
        /// Animation Event: 애니메이션 시작 시 호출
        /// Animation Clip의 첫 프레임에 설정
        /// </summary>
        public void AnimEvent_OnAnimationStart()
        {
            isAnimationPlaying = true;
            currentAnimationProgress = 0f;

            if (logAnimationEvents)
                Debug.Log($"[AnimationController] Animation Started: {currentAnimationType}");

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
                Debug.Log($"[AnimationController] Animation Ended: {currentAnimationType}");

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
        /// </summary>
        public void AnimEvent_OnAttackImpact()
        {
            if (logAnimationEvents)
                Debug.Log($"[AnimationController] Attack Impact on {currentTarget?.name}");

            if (currentTarget != null)
            {
                OnAttackHit?.Invoke(currentTarget);
            }
            else
            {
                Debug.LogWarning("[AnimationController] Attack impact but no target set!");
            }
        }

        /// <summary>
        /// Animation Event: 스킬 발동 시점 (추후 확장용)
        /// </summary>
        public void AnimEvent_OnSkillCast()
        {
            if (logAnimationEvents)
                Debug.Log($"[AnimationController] Skill Cast");

            OnSkillCast?.Invoke();
        }

        /// <summary>
        /// Animation Event: 이동 완료 시점 (선택적, 이동 애니메이션에 별도 타이밍 필요시)
        /// </summary>
        public void AnimEvent_OnMoveComplete()
        {
            if (logAnimationEvents)
                Debug.Log($"[AnimationController] Move Complete to {moveTargetPosition}");

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

        #region Debug & Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (animationSpeedMultiplier <= 0f)
            {
                Debug.LogWarning($"[UnitAnimationController] animationSpeedMultiplier must be > 0, resetting to 1.0f");
                animationSpeedMultiplier = 1f;
            }
        }

        [ContextMenu("Test Play Move Animation")]
        private void TestPlayMoveAnimation()
        {
            PlayMoveAnimation(Vector2Int.zero, Vector2Int.one);
        }

        [ContextMenu("Test Play Attack Animation")]
        private void TestPlayAttackAnimation()
        {
            PlayAttackAnimation(gameObject); // Self-attack for testing
        }
#endif

        #endregion
    }
}
```

**핵심 변경사항**:
1. **코루틴 제거**: `IEnumerator` 대신 `void` 메서드
2. **Animation Event 콜백 추가**: `AnimEvent_*` 메서드들
3. **타이밍 제거**: 코드에서 duration 계산 완전 제거
4. **이벤트 확장**: `OnAttackHit`, `OnMovementFinished` 추가

---

### 3. CombatComponent Integration (Event-Driven)

**목적**: `OnAttackHit` 이벤트 구독하여 정확한 타이밍에 데미지 적용

```csharp
// CombatComponent.cs 수정 부분

private IAnimationController animationController;

private void Awake()
{
    // ... 기존 초기화 코드 ...

    animationController = GetComponent<IAnimationController>();

    if (animationController != null)
    {
        // 공격 히트 이벤트 구독
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

/// <summary>
/// Animation Event에서 호출되는 실제 데미지 적용 메서드
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
    var result = CombatResult.Hit(finalDamage, target, attackType, isCritical, "Attack hit!");
    OnAttackPerformed?.Invoke(target, result);

    if (isCritical)
    {
        OnCriticalAttack?.Invoke(target, result);
    }
}

/// <summary>
/// 공격 수행 (애니메이션만 트리거, 실제 데미지는 Animation Event에서)
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

    // 애니메이션 재생 (데미지는 OnAnimationAttackHit에서 적용)
    if (animationController != null)
    {
        animationController.PlayAttackAnimation(target);

        // 임시 결과 반환 (실제 결과는 이벤트로 전달)
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
1. **이벤트 구독**: `OnAttackHit` 이벤트 구독
2. **타이밍 분리**: 애니메이션 시작과 데미지 적용 완전 분리
3. **코루틴 제거**: 타이밍 계산 코드 완전 제거
4. **Fallback**: 애니메이션 없을 시 즉시 데미지 적용

---

### 4. MovementComponent Integration (Event-Driven)

**목적**: `OnMovementFinished` 이벤트 구독하여 이동 완료 처리

```csharp
// MovementComponent.cs 수정 부분

private IAnimationController animationController;

private void Awake()
{
    // ... 기존 초기화 코드 ...

    animationController = GetComponent<IAnimationController>();

    if (animationController != null)
    {
        // 이동 완료 이벤트 구독
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
/// Animation Event에서 호출되는 이동 완료 처리
/// </summary>
private void OnAnimationMovementFinished(Vector2Int targetPosition)
{
    Debug.Log($"[MovementComponent] Movement animation finished to {targetPosition}");

    // 추가 처리 필요시 여기서 수행
    // 예: 이동 완료 이벤트, 사운드 재생 등

    OnMovementCompleted?.Invoke(gridManager.GetUnitPosition(gameObject), targetPosition);
}

/// <summary>
/// 이동 수행 (애니메이션만 트리거)
/// </summary>
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
        // 그리드 위치 업데이트 (즉시 or 애니메이션 후)
        // Option A: 즉시 업데이트 (애니메이션은 시각적 효과만)
        if (gridManager.MoveUnit(gameObject, startPosition, targetPosition))
        {
            // 이동 애니메이션 재생
            if (animationController != null)
            {
                animationController.PlayMoveAnimation(startPosition, targetPosition);
            }

            if (useMovementPoints)
            {
                ConsumeMovementPoints(CalculateMovementCost(startPosition, targetPosition));
            }

            hasMovedThisTurn = true;

            return MovementResult.Succeeded(startPosition, targetPosition, null,
                                          1, 0f, "Movement successful");
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

---

## 🎬 Animation Event Setup Guide

### Unity Editor에서 Animation Event 설정하기

#### 1. Move Animation Setup
```
1. Animation 창 열기 (Window → Animation → Animation)
2. Move Animation Clip 선택
3. Animation Event 추가:

   Frame 0 (시작):
   - Function: AnimEvent_OnAnimationStart

   Frame [마지막] (종료):
   - Function: AnimEvent_OnAnimationEnd
   - Function: AnimEvent_OnMoveComplete (선택적)
```

#### 2. Attack Animation Setup
```
1. Attack Animation Clip 선택
2. Animation Event 추가:

   Frame 0 (시작):
   - Function: AnimEvent_OnAnimationStart

   Frame [타격 프레임, 보통 60%]:
   - Function: AnimEvent_OnAttackImpact

   Frame [마지막] (종료):
   - Function: AnimEvent_OnAnimationEnd
```

#### 3. Visual Guide

```
Move Animation Timeline:
[===================================]
↑                                  ↑
Start                             End
AnimEvent_OnAnimationStart        AnimEvent_OnAnimationEnd
                                  AnimEvent_OnMoveComplete


Attack Animation Timeline:
[===================================]
↑                 ↑                ↑
Start            Impact            End
AnimEvent_        AnimEvent_       AnimEvent_
OnAnimationStart  OnAttackImpact   OnAnimationEnd
```

### Animation Event Inspector 설정 예시

**Move Animation**:
```yaml
Events:
  - Time: 0.0
    Function: AnimEvent_OnAnimationStart

  - Time: 0.5 (마지막 프레임)
    Function: AnimEvent_OnAnimationEnd

  - Time: 0.5
    Function: AnimEvent_OnMoveComplete
```

**Attack Animation**:
```yaml
Events:
  - Time: 0.0
    Function: AnimEvent_OnAnimationStart

  - Time: 0.36 (타격 프레임)
    Function: AnimEvent_OnAttackImpact

  - Time: 0.6 (마지막 프레임)
    Function: AnimEvent_OnAnimationEnd
```

---

## 🔄 Sequence Diagrams

### Movement Animation Flow (Event-Driven)

```
User/AI     Unit    MovementComp   AnimController   Animator    UnitService
   |          |           |              |              |            |
   |--Act()-->|           |              |              |            |
   |          |--MoveTo()->|              |              |            |
   |          |           |--PlayMove()->|              |            |
   |          |           |              |--Trigger---->|            |
   |          |           |              |              |            |
   |          |           |              |    [Animation Playing]    |
   |          |           |              |<-Frame 0-    |            |
   |          |           |<-OnAnimationStarted---------|            |
   |          |           |              |              |            |--IsPlaying=T
   |          |           |              |              |            |  WaitUntil
   |          |           |              |              |            |
   |          |           |              |<-Frame End---|            |
   |          |           |<-OnAnimationEnd-------------|            |
   |          |           |<-OnMovementFinished---------|            |
   |          |           |              |              |            |--IsPlaying=F
   |          |<-Result---|              |              |            |
   |          |           |              |              |            |--NextUnit-->
```

### Attack Animation Flow (Event-Driven)

```
User/AI     Unit    CombatComp     AnimController   Animator    HealthComp
   |          |           |              |              |            |
   |--Act()-->|           |              |              |            |
   |          |--Attack()->|              |              |            |
   |          |           |--PlayAttack>|              |            |
   |          |           |  (target)    |--Trigger---->|            |
   |          |           |              |              |            |
   |          |           |              |    [Animation Playing]    |
   |          |           |              |<-Frame 0-    |            |
   |          |           |<-OnAnimationStarted---------|            |
   |          |           |              |              |            |
   |          |           |              |<-Frame 60%---|            |
   |          |           |<-OnAttackHit(target)--------|            |
   |          |           |--TakeDamage(finalDamage)--->|            |
   |          |           |              |              |            |--Health-=
   |          |           |              |              |            |
   |          |           |              |<-Frame End---|            |
   |          |           |<-OnAnimationEnd-------------|            |
   |          |<-Result---|              |              |            |
```

---

## 📊 Benefits of Animation Event Approach

### 확장성
```yaml
새로운 애니메이션 추가:
  단계:
    1. Animation Clip 생성
    2. Animation Event 추가 (Inspector)
    3. 코드 수정 불필요!

다단 히트 공격:
  예시: "3-hit combo attack"
  - Frame 0.2: AnimEvent_OnAttackImpact (1st hit)
  - Frame 0.4: AnimEvent_OnAttackImpact (2nd hit)
  - Frame 0.6: AnimEvent_OnAttackImpact (3rd hit)
  코드: 동일한 OnAttackHit 이벤트 3번 호출됨

스킬 애니메이션:
  - Frame 0.3: AnimEvent_OnSkillCast (스킬 발동)
  - Frame 0.5: AnimEvent_OnSkillImpact (스킬 타격)
  - 새 이벤트 추가로 확장 가능
```

### 유지보수
```yaml
타이밍 조정:
  AS-IS (V1):
    - 코드에서 attackAnimationDuration 수정
    - 코드 재컴파일 필요
    - 프로그래머만 수정 가능

  TO-BE (V2):
    - Animation Clip에서 Event 위치 드래그
    - 코드 수정 없음
    - 디자이너/아티스트도 수정 가능

애니메이션 교체:
  AS-IS (V1):
    - 새 애니메이션 길이에 맞춰 duration 코드 수정

  TO-BE (V2):
    - Animation Clip만 교체
    - 코드 수정 없음
```

### 정확성
```yaml
타이밍 정확도:
  AS-IS (V1):
    - 코드에서 추정 (예: duration * 0.6f)
    - 실제 타격 프레임과 불일치 가능

  TO-BE (V2):
    - Animation Clip의 정확한 프레임에 Event 설정
    - 완벽한 동기화

디자이너 의도:
  - 아티스트가 만든 애니메이션의 타이밍 그대로 반영
  - 게임플레이 느낌 향상
```

---

## 🚀 Implementation Phases

### Phase 1: Core System (1-2일)

1. **IAnimationController 인터페이스 작성**
   - Event-driven 설계 적용
   - 게임플레이 이벤트 추가

2. **UnitAnimationController 구현**
   - Animation Event 콜백 메서드 작성
   - 코루틴 제거, 이벤트 기반 전환

3. **Unit.cs 통합**
   - AnimationController 참조 추가

4. **기본 테스트**
   - Animation Event 없이 기본 동작 확인

### Phase 2: Component Integration (1-2일)

1. **CombatComponent 수정**
   - OnAttackHit 이벤트 구독
   - 타이밍 로직 제거

2. **MovementComponent 수정**
   - OnMovementFinished 이벤트 구독

3. **UnitService 수정**
   - OnAnimationComplete 대기 로직

### Phase 3: Animation Setup (1일)

1. **Animation Clip에 Event 추가**
   - Move Animation Events
   - Attack Animation Events

2. **타이밍 조정 및 테스트**
   - 공격 히트 타이밍 최적화
   - 느낌 개선

### Phase 4: Polish (1일)

1. **추가 애니메이션 이벤트**
   - Skill Cast, Special Attack 등

2. **디버그 툴**
   - Animation Event 로그
   - Visual feedback

---

## 📝 Implementation Checklist

### Phase 1: Core System (Event-Driven)
- [ ] IAnimationController 인터페이스 작성 (이벤트 중심)
- [ ] UnitAnimationController 구현 (Animation Event 콜백)
- [ ] Unit.cs에 참조 추가
- [ ] 기본 동작 테스트

### Phase 2: Component Integration
- [ ] CombatComponent 수정 (OnAttackHit 구독)
- [ ] MovementComponent 수정 (OnMovementFinished 구독)
- [ ] UnitService 수정 (OnAnimationComplete 대기)

### Phase 3: Animation Event Setup
- [ ] Move Animation에 Event 추가
- [ ] Attack Animation에 Event 추가
- [ ] 타이밍 조정 및 테스트

### Phase 4: Documentation
- [ ] Animation Event 설정 가이드 작성
- [ ] 디자이너용 문서 작성

---

## 🎯 Success Criteria

### 기능 요구사항
✅ Animation Event로 애니메이션 시작/종료 감지
✅ 정확한 프레임에 공격 데미지 적용
✅ 이동 완료 시점 정확히 감지
✅ 코드 수정 없이 타이밍 조정 가능
✅ 다단 히트 공격 지원 가능

### 품질 기준
✅ 디자이너가 Animation Event 설정 가능
✅ 새 애니메이션 추가 시 코드 수정 불필요
✅ 타이밍 조정이 실시간 반영
✅ Graceful degradation (Animation Event 없어도 동작)

### 코드 품질
✅ 이벤트 기반 아키텍처 (느슨한 결합)
✅ 애니메이션 로직과 게임 로직 분리
✅ SOLID 원칙 준수
✅ 확장 가능한 설계

---

## 📚 Comparison: V1 vs V2

| 항목 | V1 (Duration Based) | V2 (Animation Event) |
|------|---------------------|----------------------|
| 타이밍 제어 | 코드에 하드코딩 | Animation Clip에 Event |
| 타이밍 조정 | 코드 수정 + 재컴파일 | Inspector에서 드래그 |
| 공격 타이밍 | 추정치 (duration * 0.6f) | 정확한 프레임 |
| 확장성 | 낮음 (코드 수정 필요) | 높음 (Event만 추가) |
| 유지보수 | 어려움 (프로그래머만) | 쉬움 (디자이너도 가능) |
| 다단 히트 | 복잡 (코루틴 분기) | 간단 (Event 추가) |
| 애니메이션 교체 | 코드 수정 필요 | Clip만 교체 |
| 디자이너 친화적 | ❌ | ✅ |
| 코드 복잡도 | 높음 (타이밍 계산) | 낮음 (이벤트만) |

---

## 📄 Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-02 | Claude Code | Initial design (Duration-based) |
| 2.0 | 2025-10-02 | Claude Code | Redesigned with Animation Event approach |

---

**End of Document**
