# 유닛 이동 애니메이션 동기화 개선 계획

**작성일**: 2025-10-03
**목적**: 물리적 이동(Transform)과 애니메이션의 완벽한 동기화 구현
**참조**: MovementComponent.cs, UnitAnimationController.cs

---

## 📋 목차

1. [현재 문제점 분석](#현재-문제점-분석)
2. [블렌딩/전환 시간 문제 해결책](#블렌딩전환-시간-문제-해결책)
3. [중단/취소 시 동기화 문제 해결책](#중단취소-시-동기화-문제-해결책)
4. [구현 체크리스트](#구현-체크리스트)
5. [테스트 시나리오](#테스트-시나리오)

---

## 현재 문제점 분석

### ❌ 문제 1: 물리와 애니메이션이 동기화되지 않음

**현재 동작:**
```
MovementComponent.cs:254
→ gridManager.MoveUnit() → Grid 위치 즉시 업데이트
→ GridController:1112 → transform.position 즉시 텔레포트
→ MovementComponent:259 → animationController.PlayMoveAnimation() 호출

결과: Transform이 즉시 이동하고, 애니메이션은 장식용으로만 재생
```

**근본 원인:**
- Grid 업데이트: 즉시 완료
- Transform 업데이트: 즉시 완료 (텔레포트)
- Animation: 이후 재생 (시각 효과만)

### ❌ 문제 2: 블렌딩/전환 시간 미고려

**2D Blend Tree 구조 (Trigger 없음):**
```
타이밍 구조: frontTransition - move - backTransition

- frontTransition: 0.2초 (가속 0.0 → 1.0)
- move: 0.5초 (정속 1.0 유지)
- backTransition: 0.3초 (감속 1.0 → 0.0)
→ 총 재생 시간: 1.0초

Blend Tree는 항상 활성 상태, Float 파라미터(MoveSpeed, AttackTrigger)만 변경
```

**모든 방식의 공통 문제:**
```csharp
// ❌ AnimationClip 참조
float duration = moveClip.length / animationSpeedMultiplier;
// → Transition 시간 누락! (0.5초만 계산)

// ❌ 설정 기반
float duration = moveDuration; // 0.5초
// → Transition 시간 수동 포함 필요

// ❌ GetCurrentAnimatorStateInfo
var info = animator.GetCurrentAnimatorStateInfo(0);
float duration = info.length / info.speed;
// → Transition 시간 누락!
```

### ❌ 문제 3: 중단/취소 시 동기화 깨짐

**시나리오별 문제점:**

1. **이동 중 공격 명령**
   - Grid: targetPos (이미 완료)
   - Transform: 중간 지점 (보간 중)
   - `StopCurrentAnimation()` 호출 시 Transform이 중간에 멈춤

2. **연속 이동 명령**
   ```csharp
   // MovementComponent.cs:283
   finally {
       isMoving = false;  // ← 즉시 false!
   }
   ```
   - `MoveTo(B)` → isMoving 즉시 false → Transform 보간 시작
   - `MoveTo(C)` 가능 → 새 Transform 보간 시작
   - **두 개의 코루틴이 동시에 Transform 조작!**

3. **유닛 사망 중 이동**
   - 사망 후에도 Transform 보간 코루틴이 계속 실행

4. **빠른 연속 클릭**
   - A→B, A→C, A→D 코루틴이 동시에 실행되어 떨림 발생

---

## 블렌딩/전환 시간 문제 해결책

### 🎯 최종 권장: Animation Event 기반 실시간 동기화

**핵심 아이디어:**
- Transition duration을 직접 계산하지 않음
- Animation Event가 실제 시작/종료 시점을 알려줌
- `normalizedTime`으로 진행도 추적하여 Transform 보간

**장점:**
- ✅ Transition duration 자동 고려
- ✅ Clip length 자동 고려
- ✅ 속도 변경 실시간 대응
- ✅ 모든 애니메이션 자동 대응

---

### 📝 Todo 1: UnitAnimationController 개선

#### 1.1 새 이벤트 추가

```csharp
// UnitAnimationController.cs

// 🔧 추가: Transform 이동 제어 이벤트
public event Action<Vector2Int, Vector2Int> OnTransformMoveStart;
public event Action<Vector2Int> OnTransformMoveEnd;
```

**목적:**
- `OnTransformMoveStart`: Animation Event 시작 시점에 Transform 보간 시작
- `OnTransformMoveEnd`: Animation Event 종료 시점에 Transform 보간 종료

**구현 위치:**
- `AnimEvent_OnAnimationStart()` 내부
- `AnimEvent_OnAnimationEnd()` 내부

---

#### 1.2 AnimEvent_OnAnimationStart 수정

```csharp
/// <summary>
/// Animation Event: 애니메이션 시작 시 호출
/// Transition 완료 후 실제 애니메이션이 시작되는 시점
/// </summary>
public void AnimEvent_OnAnimationStart()
{
    isAnimationPlaying = true;
    currentAnimationProgress = 0f;

    if (logAnimationEvents)
        Debug.Log($"[AnimationController] {gameObject.name}: Animation Started - {currentAnimationType}");

    // 🔧 추가: Transform 보간 시작 신호
    if (currentAnimationType == "Move")
    {
        OnTransformMoveStart?.Invoke(moveStartPosition, moveTargetPosition);
    }

    OnAnimationStarted?.Invoke(currentAnimationType);
}
```

**설명:**
- Transition이 완료된 **실제 애니메이션 시작 시점**에 호출됨
- 이 시점부터 Transform 보간을 시작해야 함

---

#### 1.3 AnimEvent_OnAnimationEnd 수정

```csharp
/// <summary>
/// Animation Event: 애니메이션 종료 시 호출
/// </summary>
public void AnimEvent_OnAnimationEnd()
{
    isAnimationPlaying = false;
    currentAnimationProgress = 1f;

    if (logAnimationEvents)
        Debug.Log($"[AnimationController] {gameObject.name}: Animation Ended - {currentAnimationType}");

    // 🔧 추가: Transform 보간 종료 신호
    if (currentAnimationType == "Move")
    {
        OnTransformMoveEnd?.Invoke(moveTargetPosition);
        OnMovementFinished?.Invoke(moveTargetPosition);
    }

    OnAnimationComplete?.Invoke();

    // 정리
    currentAnimationType = null;
    currentTarget = null;
}
```

**설명:**
- 애니메이션이 완전히 종료된 시점에 호출됨
- Transform을 최종 위치로 스냅해야 함

---

#### 1.4 Update에서 진행도 추적

```csharp
/// <summary>
/// Update에서 애니메이션 진행도를 실시간으로 추적
/// </summary>
private void Update()
{
    if (isAnimationPlaying && useAnimator && animator != null)
    {
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 🔧 추가: Transition 중이 아닐 때만 진행도 업데이트
        if (!animator.IsInTransition(0))
        {
            currentAnimationProgress = Mathf.Clamp01(stateInfo.normalizedTime);
        }
    }
}
```

**설명:**
- `normalizedTime`: 0.0 (시작) ~ 1.0 (종료)
- Transition 중에는 진행도를 업데이트하지 않음 (부정확하므로)
- 이 값을 사용하여 Transform을 Lerp

---

### 📝 Todo 2: MovementComponent 개선 (블렌딩 문제 해결)

#### 2.1 새 필드 추가

```csharp
// MovementComponent.cs

// 🔧 추가: Transform 보간 관리
private Coroutine currentTransformMoveCoroutine;
private bool isTransformMoving = false; // Transform 보간 진행 상태
```

**목적:**
- `currentTransformMoveCoroutine`: 실행 중인 코루틴 참조 저장
- `isTransformMoving`: Transform 보간이 진행 중인지 추적

---

#### 2.2 Awake/OnDestroy에서 이벤트 구독

```csharp
private void Awake()
{
    // 기존 코드...
    healthComponent = GetComponent<IHealthComponent>();
    combatSystem = GetComponent<ICombatSystem>();
    teamComponent = GetComponent<ITeamComponent>();
    animationController = GetComponent<IAnimationController>();

    // 초기 이동력 설정
    maxMovementPoints = movementRange;
    currentMovementPoints = maxMovementPoints;

    // 기존: 애니메이션 이벤트 구독
    if (animationController != null)
    {
        animationController.OnMovementFinished += OnAnimationMovementFinished;

        // 🔧 추가: Transform 이동 이벤트 구독
        animationController.OnTransformMoveStart += OnTransformMoveStart;
        animationController.OnTransformMoveEnd += OnTransformMoveEnd;
        animationController.OnAnimationInterrupted += OnAnimationInterrupted;
    }
}

private void OnDestroy()
{
    // 기존: 애니메이션 이벤트 구독 해제
    if (animationController != null)
    {
        animationController.OnMovementFinished -= OnAnimationMovementFinished;

        // 🔧 추가: Transform 이동 이벤트 구독 해제
        animationController.OnTransformMoveStart -= OnTransformMoveStart;
        animationController.OnTransformMoveEnd -= OnTransformMoveEnd;
        animationController.OnAnimationInterrupted -= OnAnimationInterrupted;
    }
}
```

---

#### 2.3 CanMove 수정

```csharp
// 🔧 수정: Transform 보간 완료까지 다음 이동 차단
public bool CanMove => healthComponent?.IsAlive == true
                       && currentMovementPoints > 0
                       && !isMoving
                       && !isTransformMoving; // ← 추가
```

**설명:**
- Transform 보간이 완료될 때까지 새 이동 명령을 차단
- 중복 코루틴 실행 방지

---

#### 2.4 Transform 이동 시작 핸들러

```csharp
/// <summary>
/// Animation Event: 애니메이션 시작 시 Transform 보간 시작
/// </summary>
private void OnTransformMoveStart(Vector2Int from, Vector2Int to)
{
    // 이전 코루틴이 있다면 중단 (안전장치)
    StopTransformMove();

    isTransformMoving = true;
    currentTransformMoveCoroutine = StartCoroutine(SyncTransformWithAnimation(from, to));

    Debug.Log($"[MovementComponent] {gameObject.name}: Transform move started {from} → {to}");
}
```

**설명:**
- Animation Event가 실제 시작 시점을 알려주면 Transform 보간 시작
- Transition duration이 자동으로 고려됨

---

#### 2.5 Transform 이동 종료 핸들러

```csharp
/// <summary>
/// Animation Event: 애니메이션 종료 시 Transform 보간 종료
/// </summary>
private void OnTransformMoveEnd(Vector2Int targetPos)
{
    isTransformMoving = false;

    // 최종 위치 보장 (Grid 위치와 동기화)
    if (gridManager != null)
    {
        transform.position = gridManager.GridToWorldPosition(targetPos);
    }

    if (currentTransformMoveCoroutine != null)
    {
        StopCoroutine(currentTransformMoveCoroutine);
        currentTransformMoveCoroutine = null;
    }

    Debug.Log($"[MovementComponent] {gameObject.name}: Transform move ended at {targetPos}");
}
```

**설명:**
- 애니메이션 종료 시 Transform을 정확히 최종 위치로 스냅
- 부동소수점 오차 방지

---

#### 2.6 Transform 보간 코루틴

```csharp
/// <summary>
/// 애니메이션 진행도에 맞춰 Transform을 실시간으로 보간
/// </summary>
private IEnumerator SyncTransformWithAnimation(Vector2Int from, Vector2Int to)
{
    if (gridManager == null || animationController == null)
    {
        Debug.LogError($"[MovementComponent] Cannot sync transform: missing dependencies");
        yield break;
    }

    Vector3 startPos = gridManager.GridToWorldPosition(from);
    Vector3 endPos = gridManager.GridToWorldPosition(to);

    // 애니메이션 진행도에 맞춰 Transform 보간
    while (animationController.IsAnimationPlaying && isTransformMoving)
    {
        float progress = animationController.CurrentAnimationProgress;
        transform.position = Vector3.Lerp(startPos, endPos, progress);
        yield return null;
    }

    // 최종 위치 보장
    transform.position = endPos;
    isTransformMoving = false;
    currentTransformMoveCoroutine = null;

    Debug.Log($"[MovementComponent] {gameObject.name}: Transform sync completed");
}
```

**핵심 로직:**
- `normalizedTime`을 사용하여 애니메이션과 정확히 동기화
- Transition + Clip duration이 모두 자동으로 고려됨
- 속도 변경도 실시간 반영됨

---

## 중단/취소 시 동기화 문제 해결책

### 🎯 핵심 전략

1. **코루틴 명시적 관리**: 이전 코루틴 중단 후 새 코루틴 시작
2. **이중 플래그 시스템**: `isMoving` (로직) + `isTransformMoving` (시각)
3. **중단 시 동기화**: Transform을 Grid 위치로 강제 스냅
4. **중단 이벤트 활용**: `OnAnimationInterrupted` 구독

---

### 📝 Todo 3: 중단/취소 처리 구현

#### 3.1 애니메이션 중단 핸들러

```csharp
/// <summary>
/// 애니메이션 중단 시 처리 (공격, 스킬 등으로 전환 시)
/// </summary>
private void OnAnimationInterrupted()
{
    Debug.Log($"[MovementComponent] {gameObject.name}: Animation interrupted, snapping to grid position");

    // 현재 Grid 위치로 Transform 스냅 (동기화)
    if (gridManager != null)
    {
        var currentGridPos = gridManager.GetUnitPosition(gameObject);
        transform.position = gridManager.GridToWorldPosition(currentGridPos);
    }

    // Transform 이동 중단
    StopTransformMove();
}
```

**설명:**
- 애니메이션이 중단되면 Transform을 Grid 위치로 강제 이동
- 중간 지점에 멈추는 문제 해결

---

#### 3.2 Transform 이동 강제 중단 메서드

```csharp
/// <summary>
/// Transform 이동 강제 중단 (안전장치)
/// </summary>
private void StopTransformMove()
{
    if (currentTransformMoveCoroutine != null)
    {
        StopCoroutine(currentTransformMoveCoroutine);
        currentTransformMoveCoroutine = null;
        Debug.Log($"[MovementComponent] {gameObject.name}: Transform move coroutine stopped");
    }

    isTransformMoving = false;
}
```

**설명:**
- 코루틴을 명시적으로 중단
- 플래그 초기화
- 연속 이동 명령 시 이전 코루틴 충돌 방지

---

#### 3.3 MoveToPosition 수정

```csharp
public MovementResult MoveToPosition(Vector2Int targetPosition, bool useMovementPoints = true)
{
    var startPosition = gridManager?.GetUnitPosition(gameObject) ?? Vector2Int.zero;

    if (!CanMoveTo(targetPosition))
    {
        return MovementResult.Failed(startPosition, "Cannot move to target position");
    }

    // 🔧 추가: 이전 Transform 이동 중단 (안전장치)
    StopTransformMove();

    OnMovementStarted?.Invoke(startPosition, targetPosition);
    isMoving = true;

    try
    {
        // 경로 계산
        int movementCost;

        if (CanFly || CanPhaseThrough)
        {
            movementCost = startPosition.GetManhattanDistance(targetPosition);
        }
        else
        {
            var path = gridManager.FindPath(startPosition, targetPosition, gameObject);
            if (path == null || path.Count == 0)
            {
                return MovementResult.Failed(startPosition, "No valid path found");
            }
            movementCost = CalculatePathCost(path);
        }

        // 그리드 위치 업데이트 (즉시) - 로직은 즉시 처리
        if (gridManager.MoveUnit(gameObject, startPosition, targetPosition))
        {
            // 🔧 수정: Transform 즉시 이동 제거
            // 이제 Transform 이동은 AnimationEvent에서 처리됨

            // 이동 애니메이션 재생 (Transform 보간은 AnimationEvent에서 시작)
            if (animationController != null)
            {
                animationController.PlayMoveAnimation(startPosition, targetPosition);
            }
            else
            {
                // 애니메이션 컨트롤러 없으면 Transform 즉시 이동
                transform.position = gridManager.GridToWorldPosition(targetPosition);
            }

            if (useMovementPoints)
            {
                ConsumeMovementPoints(movementCost);
            }

            hasMovedThisTurn = true;

            var result = MovementResult.Succeeded(startPosition, targetPosition, null,
                                                movementCost, 0f, "Movement successful");

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
        // 🔧 수정: isMoving만 초기화 (isTransformMoving은 코루틴 완료 시)
        isMoving = false;
        Debug.Log($"[MovementComponent] {gameObject.name} Movement logic completed - isMoving reset to false");
    }
}
```

**주요 변경사항:**
1. `StopTransformMove()` 추가: 이전 코루틴 중단
2. GridController의 `SetUnitWorldPosition()` 호출 제거
3. Transform 이동은 AnimationEvent에서만 처리
4. 애니메이션 컨트롤러 없을 때만 즉시 이동

---

#### 3.4 ResetMovement 수정

```csharp
public void ResetMovement()
{
    // 🔧 완전한 이동 상태 초기화
    hasMovedThisTurn = false;
    isMoving = false;

    // 🔧 추가: Transform 이동도 중단
    StopTransformMove();

    // 현재 Grid 위치로 동기화 (안전장치)
    if (gridManager != null)
    {
        var currentGridPos = gridManager.GetUnitPosition(gameObject);
        transform.position = gridManager.GridToWorldPosition(currentGridPos);
    }

    RefreshMovementPoints();

    Debug.Log($"[MovementComponent] {gameObject.name} Movement fully reset - " +
             $"CanMove: {CanMove}, MovementPoints: {currentMovementPoints}");
}
```

**설명:**
- 턴 종료 시 Transform 위치를 Grid와 동기화
- 코루틴 정리

---

#### 3.5 GridController 수정 필요

```csharp
// GridController.cs:1097 수정 필요

public bool MoveUnit(GameObject unit, Vector2Int fromPosition, Vector2Int toPosition)
{
    if (unit == null || !IsValidPosition(toPosition))
        return false;

    // Business validation
    if (!CanMoveUnit(unit, toPosition))
        return false;

    // Update state through data layer
    if (!gridState.SetUnitPosition(unit, toPosition))
        return false;

    // 🔧 제거: Transform 즉시 이동 제거
    // SetUnitWorldPosition(unit, toPosition);
    // → 이제 MovementComponent의 AnimationEvent에서 처리

    // Notify presentation layer through events (already handled by gridState.SetUnitPosition)
    return true;
}
```

**설명:**
- GridController에서 Transform을 즉시 이동시키는 코드 제거
- Transform 이동은 MovementComponent의 AnimationEvent 기반 보간으로만 처리

---

## 구현 체크리스트

### ✅ Phase 1: UnitAnimationController 수정

- [ ] `OnTransformMoveStart` 이벤트 추가
- [ ] `OnTransformMoveEnd` 이벤트 추가
- [ ] `AnimEvent_OnAnimationStart()` 수정
  - [ ] Move 타입일 때 `OnTransformMoveStart` 발생
- [ ] `AnimEvent_OnAnimationEnd()` 수정
  - [ ] Move 타입일 때 `OnTransformMoveEnd` 발생
- [ ] `Update()` 메서드 추가
  - [ ] `normalizedTime` 기반 진행도 추적
  - [ ] Transition 중에는 업데이트 제외

### ✅ Phase 2: MovementComponent 수정

- [ ] 새 필드 추가
  - [ ] `currentTransformMoveCoroutine`
  - [ ] `isTransformMoving`
- [ ] `Awake()` 수정
  - [ ] `OnTransformMoveStart` 구독
  - [ ] `OnTransformMoveEnd` 구독
  - [ ] `OnAnimationInterrupted` 구독
- [ ] `OnDestroy()` 수정
  - [ ] 이벤트 구독 해제
- [ ] `CanMove` 프로퍼티 수정
  - [ ] `isTransformMoving` 조건 추가
- [ ] 새 메서드 추가
  - [ ] `OnTransformMoveStart()` 핸들러
  - [ ] `OnTransformMoveEnd()` 핸들러
  - [ ] `OnAnimationInterrupted()` 핸들러
  - [ ] `SyncTransformWithAnimation()` 코루틴
  - [ ] `StopTransformMove()` 유틸리티
- [ ] `MoveToPosition()` 수정
  - [ ] `StopTransformMove()` 호출 추가
  - [ ] Transform 즉시 이동 코드 제거
- [ ] `ResetMovement()` 수정
  - [ ] `StopTransformMove()` 호출 추가
  - [ ] Grid 위치로 동기화 추가

### ✅ Phase 3: GridController 수정

- [ ] `MoveUnit(from, to)` 메서드 수정
  - [ ] `SetUnitWorldPosition()` 호출 제거
  - [ ] Transform 이동 로직 제거

### ✅ Phase 4: 테스트 및 검증

- [ ] 단위 테스트
  - [ ] 정상 이동 시나리오
  - [ ] 이동 중 공격 시나리오
  - [ ] 연속 이동 명령 시나리오
  - [ ] 유닛 사망 중 이동 시나리오
  - [ ] 빠른 연속 클릭 시나리오
- [ ] 통합 테스트
  - [ ] Transition duration 다양한 값 테스트
  - [ ] Animation speed 변경 테스트
  - [ ] 여러 이동 애니메이션 클립 테스트
- [ ] 시각적 검증
  - [ ] Transform이 부드럽게 이동하는지 확인
  - [ ] 중단 시 Grid 위치로 스냅되는지 확인
  - [ ] 떨림/튐 현상 없는지 확인

---

## 테스트 시나리오

### 🧪 시나리오 1: 정상 이동

**테스트 순서:**
1. 유닛 선택
2. 타일 클릭하여 이동 명령
3. 애니메이션 재생 중 Transform 위치 확인
4. 애니메이션 종료 후 최종 위치 확인

**예상 결과:**
- ✅ Transform이 부드럽게 보간되어 이동
- ✅ 애니메이션 종료 시 정확히 타겟 위치
- ✅ Grid 위치와 Transform 위치 일치

---

### 🧪 시나리오 2: 이동 중 공격 명령

**테스트 순서:**
1. 유닛 선택
2. 먼 거리 타일로 이동 명령 (긴 애니메이션)
3. 이동 중 적 유닛 클릭하여 공격 명령
4. Transform 위치 확인

**예상 결과:**
- ✅ 이동 애니메이션 중단
- ✅ Transform이 즉시 Grid 위치로 스냅
- ✅ 공격 애니메이션 정상 재생
- ✅ Grid-Transform 불일치 없음

---

### 🧪 시나리오 3: 연속 이동 명령

**테스트 순서:**
1. 유닛 선택
2. A → B 이동 명령
3. 즉시 B → C 이동 명령 (B 도착 전)
4. Transform 위치 및 코루틴 상태 확인

**예상 결과:**
- ✅ 첫 번째 이동 차단 (`CanMove = false`)
- ✅ 또는 첫 번째 이동 중단, 두 번째 이동 시작
- ✅ Transform이 떨리지 않음
- ✅ 최종적으로 C에 정확히 도착

---

### 🧪 시나리오 4: 빠른 연속 클릭

**테스트 순서:**
1. 유닛 선택
2. A → B → C → D → E 빠르게 클릭
3. Transform 동작 및 최종 위치 확인

**예상 결과:**
- ✅ 마지막 명령(E)만 실행 또는
- ✅ 각 명령이 순차적으로 실행
- ✅ Transform이 발작하듯 떨리지 않음
- ✅ 최종 위치가 명령과 일치

---

### 🧪 시나리오 5: 유닛 사망 중 이동

**테스트 순서:**
1. 유닛 선택
2. 이동 명령
3. 이동 중 HP를 0으로 설정 (스크립트)
4. Transform 동작 확인

**예상 결과:**
- ✅ 이동 코루틴 즉시 중단
- ✅ 사망 애니메이션 재생
- ✅ Transform이 계속 이동하지 않음

---

### 🧪 시나리오 6: Transition Duration 변경

**테스트 순서:**
1. Animator Controller에서 Transition Duration을 0.3초로 변경
2. 이동 명령
3. Transform 보간 시간 측정

**예상 결과:**
- ✅ Transform 보간이 전체 시간(Transition + Clip)과 일치
- ✅ 블렌딩 시간이 자동으로 고려됨

---

### 🧪 시나리오 7: Animation Speed 변경

**테스트 순서:**
1. GameSettings.GlobalAnimationSpeed를 2.0으로 변경
2. 이동 명령
3. Transform 보간 속도 확인

**예상 결과:**
- ✅ Transform 보간이 애니메이션 속도와 동기화
- ✅ 2배속으로 부드럽게 이동
- ✅ normalizedTime 기반이므로 자동 대응

---

## 주의사항

### ⚠️ 성능 고려사항

1. **Update 메서드 오버헤드**
   - `CurrentAnimationProgress` 추적을 위해 Update 사용
   - 이동 중인 유닛이 많을 경우 부하 증가 가능
   - 필요시 FixedUpdate로 변경 고려

2. **코루틴 관리**
   - 유닛마다 최대 1개의 Transform 보간 코루틴만 실행
   - `StopTransformMove()` 호출로 명시적 정리

3. **디버그 로그**
   - 구현 완료 후 디버그 로그 제거 또는 조건부 활성화
   - 성능 영향 최소화

### ⚠️ 엣지 케이스

1. **AnimationController가 없는 유닛**
   - Fallback으로 즉시 이동 처리
   - `MoveToPosition()` 코드에서 이미 처리됨

2. **GridManager가 없는 경우**
   - 코루틴 내에서 null 체크
   - 조기 종료 처리

3. **Animation Event 누락**
   - Animation Clip에 Event가 설정되지 않은 경우
   - 코루틴이 무한 대기할 수 있음
   - Timeout 로직 고려 가능

### ⚠️ 호환성

1. **기존 시스템과의 통합**
   - `OnMovementFinished` 이벤트는 유지됨
   - 기존 구독자들에게 영향 없음

2. **Grid 위치 우선**
   - Grid 위치가 항상 진실의 원천 (Source of Truth)
   - Transform은 시각적 표현일 뿐

3. **턴 시스템 통합**
   - `StartTurn()`, `EndTurn()`에서 상태 초기화 확인
   - `ResetMovement()`에서 완전한 정리

---

## 참조

- **관련 파일**:
  - `Assets/Script/Game/Components/MovementComponent.cs`
  - `Assets/Script/Game/Components/UnitAnimationController.cs`
  - `Assets/Script/Game/Components/GridController.cs`
  - `Assets/Script/Game/Interfaces/IAnimationController.cs`

- **관련 문서**:
  - `Unit_Animation_System_Design.md`
  - `Unit_Animation_System_Design_V2_AnimationEvent.md`
  - `Phase3_Animation_Event_Setup_Guide.md`

- **테스트**:
  - `Assets/Script/Game/Test/TileOccupancyIntegrationTest.cs`

---

## 버전 히스토리

- **v1.0.0** (2025-10-03): 초안 작성
  - 블렌딩/전환 시간 문제 분석 및 해결책
  - 중단/취소 동기화 문제 분석 및 해결책
  - 구현 체크리스트 작성
  - 테스트 시나리오 정의
