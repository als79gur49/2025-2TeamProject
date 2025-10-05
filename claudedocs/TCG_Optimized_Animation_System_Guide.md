# TCG 최적화 애니메이션 시스템 가이드

## 문서 개요
본 문서는 TCG(Trading Card Game) 장르의 특성에 최적화된 유닛 애니메이션 시스템 설계 가이드입니다. 복잡한 동기화 메커니즘 대신, 게임의 핵심 규칙을 활용한 실용적이고 간결한 아키텍처를 제시합니다.

**프로젝트 아키텍처**: Service Locator 패턴 기반, 이벤트 중심 설계

---

## 1. 게임 규칙 및 제약사항

### 핵심 규칙
본 TCG 게임은 다음과 같은 명확한 규칙을 가지고 있습니다:

1. **단일 액션 원칙**: 특정 유닛이 행동할 때는 **해당 유닛만** 행동이 가능합니다.
2. **외부 개입 불가**: 행동 중인 유닛에 대한 **외부 개입이 절대 불가능**합니다.

### 규칙이 제거하는 복잡성

이 규칙들은 다음과 같은 복잡한 문제들을 **원천적으로 차단**합니다:

- ❌ **동시 다발적 이벤트**: 여러 유닛이 동시에 움직이는 상황
- ❌ **경합 조건(Race Condition)**: 여러 시스템이 같은 리소스에 접근하는 상황
- ❌ **행동 중단(Interruption)**: 이동 중 공격 명령, 공격 중 스킬 사용 등
- ❌ **상태 충돌**: 애니메이션 상태와 게임 로직 상태의 불일치

**결론**: 복잡한 이중 플래그, normalizedTime 추적, OnAnimationInterrupted 이벤트 등의 정교한 동기화 장치가 **필요 없습니다**.

---

## 2. 유지해야 할 핵심 설계 원칙

복잡한 안전장치는 제거하되, 다음 원칙들은 반드시 유지해야 합니다:

### 2.1 데이터와 시각화의 분리 (Decoupling Logic and Presentation)
```
게임 로직(GridState) → 즉시 업데이트
시각 표현(Animation) → 비동기로 처리
```

**이유**: 게임 로직이 애니메이션 속도에 종속되지 않고, UI나 다른 시스템이 항상 정확한 최신 상태를 참조할 수 있습니다.

### 2.2 비동기적 시각 처리
- 유닛 이동/공격 중에도 게임 전체가 멈추지 않습니다(freeze 방지)
- 코루틴을 사용하여 시각적 연출과 게임 흐름을 독립적으로 관리

### 2.3 애니메이션 이벤트 기반 타이밍
- `yield return new WaitForSeconds(0.5f)` 같은 하드코딩 대신
- Animation Event(`AnimEvent_OnAttackImpact` 등)를 사용하여 타이밍 결정
- **장점**: 애니메이션 길이/속도 변경 시 코드 수정 불필요

### 2.4 서비스 로케이터 패턴
- 싱글톤 대신 `ServiceLocator.Get<T>()`를 통한 의존성 주입
- GridManager, UnitService 등 핵심 서비스 접근
- 느슨한 결합(Loose Coupling)으로 테스트 용이성 향상

---

## 3. 프로젝트 아키텍처 개요

### 핵심 컴포넌트

```
ServiceLocator (중앙 의존성 관리)
    ├── GridManager (IGridManager)
    │   ├── GridState (데이터 레이어)
    │   ├── GridController (비즈니스 로직)
    │   └── GridRenderer (프레젠테이션)
    │
    └── UnitService (유닛 관리 및 페이즈 제어)
        ├── Phase Event System
        │   ├── OnPhaseStarted
        │   ├── OnPhaseCompleted
        │   └── OnUnitProcessed
        └── Sequential Unit Processing
```

### 이벤트 기반 통신

**UnitService가 중앙 이벤트 허브 역할**:
- 페이즈 시작/종료 관리
- 유닛 순차 처리 (ProcessUnitsForPhaseAsync)
- 애니메이션 완료 대기 (`IAnimationController.IsAnimationPlaying` 체크)

**MovementComponent/CombatComponent는 순수 기능만 담당**:
- GridManager를 통한 로직 처리
- 애니메이션 트리거만 담당
- UnitService나 다른 매니저 직접 호출 금지

---

## 4. 3단계 구현 가이드

### Level 1: 필수 기능 (The Essentials)
> 이 정도만 구현해도 TCG 게임을 만드는 데 전혀 문제가 없습니다.

#### 4.1.1 UnitService 페이즈 관리 (이벤트 기반)

**UnitService의 페이즈 실행 시스템:**

```csharp
public class UnitService : MonoBehaviour, IUnitService
{
    // 페이즈 상태
    private PhaseExecutionContext currentContext;

    public bool IsPhaseExecuting => currentContext?.State == PhaseExecutionState.Executing;
    public TurnPhase? CurrentPhase => currentContext?.Phase;

    // 페이즈 이벤트
    public event System.Action<TurnPhase> OnPhaseStarted;
    public event System.Action<TurnPhase> OnPhaseCompleted;
    public event System.Action<Unit, int, int> OnUnitProcessed;

    // 페이즈 비동기 실행
    public bool ProcessUnitsForPhaseAsync(TurnPhase phase)
    {
        if (IsPhaseExecuting) return false;

        currentContext = new PhaseExecutionContext
        {
            Phase = phase,
            State = PhaseExecutionState.Executing,
            UnitsToProcess = GetUnitsForPhase(phase)
        };

        OnPhaseStarted?.Invoke(phase);
        StartCoroutine(ExecutePhaseSequentially());
        return true;
    }

    private IEnumerator ExecutePhaseSequentially()
    {
        var units = currentContext.UnitsToProcess;

        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit != null && unit.IsAlive)
            {
                // 유닛 행동 실행
                yield return StartCoroutine(ProcessUnitActionAsync(unit, currentContext.Phase));
                OnUnitProcessed?.Invoke(unit, i + 1, units.Count);
            }
        }

        CompleteCurrentPhase();
    }

    private IEnumerator ProcessUnitActionAsync(Unit unit, TurnPhase phase)
    {
        var animController = unit?.GetAnimationController();

        // 유닛 행동 실행 (이동 or 공격)
        unit.Act();

        // 애니메이션 완료 대기
        if (animController != null && animController.IsAnimationPlaying)
        {
            yield return StartCoroutine(WaitForAnimationComplete(animController, unit));
        }
    }

    private IEnumerator WaitForAnimationComplete(IAnimationController animController, Unit unit)
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (animController != null && animController.IsAnimationPlaying && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
    }
}
```

#### 4.1.2 MovementComponent (Service Locator 기반)

**핵심: GridManager 직접 호출, UnitService는 이벤트로만 통신**

```csharp
public class MovementComponent : MonoBehaviour, IAdvancedMovementSystem
{
    private IGridManager gridManager;
    private IAnimationController animationController;

    private void Start()
    {
        // Service Locator를 통한 의존성 주입
        gridManager = ServiceLocator.Get<IGridManager>();
        animationController = GetComponent<IAnimationController>();
    }

    public MovementResult MoveToPosition(Vector2Int targetPosition, bool useMovementPoints = true)
    {
        var startPosition = gridManager?.GetUnitPosition(gameObject) ?? Vector2Int.zero;

        if (!CanMoveTo(targetPosition))
        {
            return MovementResult.Failed(startPosition, "Cannot move to target position");
        }

        // 1. GridState 즉시 업데이트 (데이터)
        if (gridManager.MoveUnit(gameObject, startPosition, targetPosition))
        {
            // 2. 애니메이션 트리거 (시각만, 비동기)
            if (animationController != null)
            {
                animationController.PlayMoveAnimation(startPosition, targetPosition);
            }

            // 3. 이동력 소모
            if (useMovementPoints)
            {
                ConsumeMovementPoints(movementCost);
            }

            // ✅ UnitService는 IsAnimationPlaying을 체크하여 자동으로 대기
            // ✅ MovementComponent는 별도의 입력 차단 처리 불필요

            return MovementResult.Succeeded(startPosition, targetPosition, null, movementCost, 0f, "Movement successful");
        }

        return MovementResult.Failed(startPosition, "Failed to move unit on grid");
    }
}
```

**주요 포인트**:
- `gridManager = ServiceLocator.Get<IGridManager>()` (싱글톤 ❌)
- GridManager를 통해 즉시 위치 업데이트
- 애니메이션은 시각적 효과만, 로직과 분리
- UnitService가 `animationController.IsAnimationPlaying`을 체크하여 대기

#### 4.1.3 CombatComponent (AnimEvent 기반)

```csharp
public class CombatComponent : MonoBehaviour, ICombatSystem
{
    private IAnimationController animController;
    private GameObject currentTarget;

    private void Awake()
    {
        animController = GetComponent<IAnimationController>();

        // Animation Event 구독
        if (animController != null)
        {
            animController.OnAttackHit += HandleAttackImpact;
        }
    }

    public void AttackTarget(GameObject target)
    {
        currentTarget = target;

        // 1. 공격 애니메이션 트리거
        animController.PlayAttackAnimation(target);

        // 2. AnimEvent_OnAttackImpact에서 실제 데미지 처리
        // (HandleAttackImpact 메서드에서 자동 처리)

        // ✅ UnitService가 IsAnimationPlaying을 체크하여 자동으로 대기
    }

    private void HandleAttackImpact(GameObject target)
    {
        if (target != null)
        {
            var healthComponent = target.GetComponent<IHealthComponent>();
            if (healthComponent != null)
            {
                int damage = CalculateDamage();
                healthComponent.TakeDamage(damage);
                Debug.Log($"[Combat] Dealt {damage} damage to {target.name}");
            }
        }
    }

    private void OnDestroy()
    {
        if (animController != null)
        {
            animController.OnAttackHit -= HandleAttackImpact;
        }
    }
}
```

#### 4.1.4 IAnimationController 인터페이스 (실제 구현)

```csharp
public interface IAnimationController
{
    // 상태 프로퍼티
    bool IsAnimationPlaying { get; }
    float CurrentAnimationProgress { get; }
    GameObject CurrentTarget { get; }

    // 애니메이션 재생
    void PlayMoveAnimation(Vector2Int from, Vector2Int to);
    void PlayAttackAnimation(GameObject target);
    void StopCurrentAnimation();
    void SetAnimationSpeed(float speed);

    // 라이프사이클 이벤트
    event Action<string> OnAnimationStarted;
    event Action OnAnimationComplete;
    event Action OnAnimationInterrupted;

    // 게임플레이 이벤트
    event Action<GameObject> OnAttackHit;
    event Action<Vector2Int> OnMovementFinished;
    event Action OnSkillCast;
}
```

#### 4.1.5 UnitAnimationController 구현 (2D Blend Tree 기반)

```csharp
public class UnitAnimationController : MonoBehaviour, IAnimationController
{
    [SerializeField] private Animator animator;
    [SerializeField] private float frontTransitionDuration = 0.2f;
    [SerializeField] private float backTransitionDuration = 0.3f;

    private bool isAnimationPlaying;
    private GameObject currentTarget;
    private Coroutine currentAnimationCoroutine;

    public bool IsAnimationPlaying => isAnimationPlaying;
    public GameObject CurrentTarget => currentTarget;

    // 이벤트
    public event Action<string> OnAnimationStarted;
    public event Action OnAnimationComplete;
    public event Action<GameObject> OnAttackHit;
    public event Action<Vector2Int> OnMovementFinished;

    // 2D Blend Tree Float 파라미터 제어 (Trigger 없음)
    public void PlayMoveAnimation(Vector2Int from, Vector2Int to)
    {
        if (animator != null)
        {
            // Trigger 대신 Float 파라미터 사용
            // frontTransition - move - backTransition 구조
            if (currentAnimationCoroutine != null)
                StopCoroutine(currentAnimationCoroutine);

            currentAnimationCoroutine = StartCoroutine(BlendMoveAnimation());
        }
    }

    public void PlayAttackAnimation(GameObject target)
    {
        currentTarget = target;
        if (animator != null)
        {
            // Y축(AttackTrigger) Float 파라미터 사용
            animator.SetFloat("AttackTrigger", 1.0f);
            StartCoroutine(ResetAttackTrigger());
        }
    }

    private IEnumerator BlendMoveAnimation()
    {
        isAnimationPlaying = true;
        OnAnimationStarted?.Invoke("Move");

        // frontTransition: 0.0 → 1.0 가속
        float elapsed = 0f;
        while (elapsed < frontTransitionDuration)
        {
            float t = elapsed / frontTransitionDuration;
            animator.SetFloat("MoveSpeed", Mathf.Lerp(0f, 1f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // move: 1.0 유지 (정속)
        animator.SetFloat("MoveSpeed", 1.0f);
        yield return new WaitForSeconds(0.5f); // move 구간 시간

        // backTransition: 1.0 → 0.0 감속
        elapsed = 0f;
        while (elapsed < backTransitionDuration)
        {
            float t = elapsed / backTransitionDuration;
            animator.SetFloat("MoveSpeed", Mathf.Lerp(1f, 0f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        animator.SetFloat("MoveSpeed", 0f);
        isAnimationPlaying = false;
        OnAnimationComplete?.Invoke();
    }

    private IEnumerator ResetAttackTrigger()
    {
        yield return new WaitForSeconds(0.5f);
        animator.SetFloat("AttackTrigger", 0f);
    }

    // Animation Event에서 호출
    public void AnimEvent_OnAttackImpact()
    {
        if (currentTarget != null)
        {
            OnAttackHit?.Invoke(currentTarget);
        }
    }
}
```

---

### Level 2: 권장 기능 (Nice-to-Haves)
> 게임을 좀 더 다듬고 싶을 때 추가하면 좋은 기능입니다.

#### 4.2.1 GameSettings 연동

**전역 애니메이션 속도 제어:**

```csharp
public class UnitAnimationController : MonoBehaviour, IAnimationController
{
    [SerializeField] private float animationSpeedMultiplier = 1f;
    [SerializeField] private bool skipAnimations = false;

    private void Awake()
    {
        ApplyGameSettings();
        GameSettings.OnAnimationSettingsChanged += ApplyGameSettings;
    }

    private void ApplyGameSettings()
    {
        skipAnimations = !GameSettings.EnableUnitAnimations;
        animationSpeedMultiplier = GameSettings.GlobalAnimationSpeed;

        if (animator != null)
        {
            animator.SetFloat("AnimSpeed", animationSpeedMultiplier);
        }
    }

    public void PlayMoveAnimation(Vector2Int from, Vector2Int to)
    {
        if (skipAnimations)
        {
            // 애니메이션 스킵 시 즉시 완료
            OnMovementFinished?.Invoke(to);
            return;
        }

        // 정상 애니메이션 재생
        animator?.SetTrigger("Move");
    }
}
```

#### 4.2.2 VFX/SFX 이벤트 시스템

```csharp
public class UnitAnimationController : MonoBehaviour, IAnimationController
{
    // VFX/SFX 요청 이벤트
    public event Action<string, Vector3, Vector3> OnVFXRequested;
    public event Action<string, Vector3> OnSFXRequested;

    public void PlayAttackAnimation(GameObject target)
    {
        currentTarget = target;
        animator?.SetTrigger("Attack");

        // VFX 요청
        if (GameSettings.EnableVFX)
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            OnVFXRequested?.Invoke("Attack_Swing", transform.position, direction);
        }

        // SFX 요청
        if (GameSettings.EnableSFX)
        {
            OnSFXRequested?.Invoke("Attack_Swing", transform.position);
        }
    }
}
```

---

### Level 3: 불필요한 기능 (Explicitly Excluded)
> 귀하의 TCG 게임 규칙 하에서는 구현할 필요가 없는 기능들입니다.

#### ❌ 제외 목록

1. **TurnManager 싱글톤 패턴**
   - 이유: Service Locator 패턴 사용, 느슨한 결합 유지

2. **MovementComponent에서 입력 차단 직접 처리**
   - 이유: UnitService의 페이즈 시스템이 자동으로 순차 처리

3. **OnAnimationInterrupted 이벤트 (복잡한 중단 처리)**
   - 이유: 행동 중단이 발생하지 않으므로 불필요

4. **이중 상태 플래그 (isMoving + isTransformMoving)**
   - 이유: `IsAnimationPlaying` 하나로 충분

5. **normalizedTime을 사용한 정교한 transform 동기화**
   - 이유: 이동이 중단되지 않으므로 시작/끝 위치만 보간하면 충분

6. **복잡한 큐(Queue) 시스템**
   - 이유: 동시 다발적 이벤트가 없으므로 순차 처리만으로 충분

---

## 5. 기존 시스템과의 비교

| 항목 | 잘못된 패턴 (문서 v1.0) | 올바른 패턴 (현재 프로젝트) |
|------|----------------------|-------------------------|
| **의존성 주입** | TurnManager.Instance (싱글톤) | ServiceLocator.Get<IGridManager>() |
| **입력 차단** | TurnManager.BeginAction/EndAction | UnitService.IsPhaseExecuting (자동) |
| **이동 처리** | MovementComponent가 입력 제어 | GridManager만 호출, UnitService가 제어 |
| **애니메이션 대기** | 각 컴포넌트별 처리 | UnitService가 IsAnimationPlaying 체크 |
| **GridManager 접근** | GridManager.Instance | ServiceLocator.Get<IGridManager>() |
| **책임 분리** | MovementComponent가 다중 책임 | MovementComponent는 이동만, UnitService가 조율 |

---

## 6. 마이그레이션 가이드

### Step 1: Service Locator 패턴 적용

```csharp
// ❌ 잘못된 방법 (싱글톤)
GridManager.Instance.UpdateUnitPosition(...);

// ✅ 올바른 방법 (Service Locator)
private IGridManager gridManager;

void Start()
{
    gridManager = ServiceLocator.Get<IGridManager>();
}

void MoveUnit()
{
    gridManager.MoveUnit(...);
}
```

### Step 2: MovementComponent 책임 분리

```csharp
// ❌ 제거: TurnManager 직접 호출
TurnManager.Instance.BeginAction();
TurnManager.Instance.EndAction();

// ✅ 추가: 순수 이동 로직만
public MovementResult MoveToPosition(Vector2Int target)
{
    // 1. GridManager 통해 위치 업데이트
    gridManager.MoveUnit(gameObject, startPos, target);

    // 2. 애니메이션 트리거 (시각만)
    animationController.PlayMoveAnimation(startPos, target);

    // UnitService가 IsAnimationPlaying 체크하여 자동 대기
    return MovementResult.Succeeded(...);
}
```

### Step 3: UnitService 이벤트 시스템 활용

```csharp
// UnitService가 페이즈 관리
unitService.ProcessUnitsForPhaseAsync(TurnPhase.EnemyAction);

// UnitService가 자동으로:
// 1. 각 유닛의 Act() 호출
// 2. IsAnimationPlaying 체크하여 대기
// 3. 다음 유닛으로 진행
```

### Step 4: 입력 처리 수정

```csharp
// ❌ 잘못된 방법
void Update()
{
    if (!TurnManager.Instance.CanProcessInput()) return;
    // 입력 처리...
}

// ✅ 올바른 방법
private IUnitService unitService;

void Start()
{
    unitService = ServiceLocator.Get<IUnitService>();
}

void Update()
{
    if (unitService.IsPhaseExecuting) return;
    // 입력 처리...
}
```

---

## 7. 테스트 체크리스트

### 필수 테스트
- [ ] ServiceLocator를 통해 GridManager, UnitService 접근 가능한가?
- [ ] UnitService.ProcessUnitsForPhaseAsync()가 유닛을 순차적으로 처리하는가?
- [ ] 유닛 애니메이션 중 IsAnimationPlaying이 true로 설정되는가?
- [ ] UnitService가 IsAnimationPlaying을 체크하여 애니메이션 완료까지 대기하는가?
- [ ] MovementComponent가 GridManager만 호출하고, UnitService/TurnManager 직접 호출하지 않는가?
- [ ] AnimEvent_OnAttackImpact가 정확한 타이밍에 호출되는가?
- [ ] GridState와 Transform 위치가 동기화되는가?

### 권장 테스트 (Level 2 구현 시)
- [ ] GameSettings.EnableUnitAnimations = false 시 애니메이션 스킵되는가?
- [ ] GameSettings.GlobalAnimationSpeed 변경 시 애니메이션 속도가 변경되는가?
- [ ] VFX/SFX 이벤트가 올바른 타이밍에 발생하는가?

---

## 8. 결론

### 핵심 원칙
1. **규칙을 활용하라**: 게임의 제약사항이 복잡성을 제거해줍니다.
2. **단순함을 유지하라**: 필요 없는 안전장치는 과감히 제거합니다.
3. **분리를 지켜라**: 데이터(게임 로직)와 시각(애니메이션)은 독립적으로 관리합니다.
4. **Service Locator 활용**: 싱글톤 대신 느슨한 결합으로 의존성 관리
5. **이벤트 기반 통신**: MovementComponent는 이동만, UnitService가 전체 조율

### 구현 우선순위
1. **Level 1 (필수)**: Service Locator + UnitService 페이즈 시스템 + AnimEvent 기반 공격
2. **Level 2 (권장)**: GameSettings 연동 + VFX/SFX 이벤트
3. **Level 3 (제외)**: 싱글톤 패턴, 직접 입력 차단, 복잡한 동기화 장치

### 아키텍처 요약

```
Service Locator (중앙 허브)
    │
    ├─ GridManager (IGridManager)
    │   └─ 즉시 위치 업데이트 (데이터)
    │
    └─ UnitService (IUnitService)
        ├─ ProcessUnitsForPhaseAsync()
        ├─ IsAnimationPlaying 체크
        └─ 순차 처리 보장

MovementComponent
    ├─ gridManager.MoveUnit() 호출 (로직)
    ├─ animationController.PlayMoveAnimation() (시각)
    └─ ❌ UnitService/TurnManager 직접 호출 금지

CombatComponent
    ├─ animationController.PlayAttackAnimation()
    ├─ OnAttackHit 이벤트 구독
    └─ ❌ UnitService/TurnManager 직접 호출 금지
```

---

## 부록: 전체 시스템 다이어그램

```
[UnitService.ProcessUnitsForPhaseAsync(TurnPhase.EnemyAction)]
    ↓
[각 유닛의 Act() 순차 호출]
    ↓
[MovementComponent.MoveToPosition() or CombatComponent.AttackTarget()]
    ├─ GridManager.MoveUnit() → GridState 즉시 업데이트 (데이터)
    └─ AnimationController.PlayMoveAnimation() → 애니메이션 트리거 (시각)
         ↓
    [Animation Event 발생]
    ├─ AnimEvent_OnAnimationStart → IsAnimationPlaying = true
    ├─ AnimEvent_OnAttackImpact → OnAttackHit 이벤트 → 데미지 처리
    └─ AnimEvent_OnAnimationEnd → IsAnimationPlaying = false
         ↓
[UnitService가 IsAnimationPlaying == false 감지]
    ↓
[다음 유닛으로 진행]
```

---

**문서 버전**: 2.0
**최종 수정일**: 2025-10-03
**작성자**: Claude Code Analysis
**주요 변경사항**: 싱글톤 → Service Locator 패턴, UnitService 이벤트 시스템 기반 재작성
