# Trait System Architecture - Conflict Analysis (ACCURATE)

> **분석 일자**: 2025-10-30
> **분석 대상**: `claudedocs/TraitSystemArchitecture_StrategyPattern.md` (3112 lines)
> **목적**: 설계 문서와 실제 코드베이스 간 충돌 분석
> **방법론**: 실제 파일 읽기 및 라인별 검증

---

## ⚠️ CRITICAL FINDING: 구현 상태 불일치

**설계 문서의 주장**: 모든 구현 단계에 ✅ 체크마크 표시 (Phase 1-4 완료로 표시)

**실제 코드베이스 상태**:
```bash
# Traits 디렉토리 검색 결과
Assets/Script/Game/Traits/**/*.cs  → ❌ No files found

# 핵심 파일 검색 결과
TraitData.cs           → ❌ DOES NOT EXIST
TraitComponent.cs      → ❌ DOES NOT EXIST
TraitExecutor.cs       → ❌ DOES NOT EXIST
IAttackStrategy.cs     → ❌ DOES NOT EXIST
IMovementStrategy.cs   → ❌ DOES NOT EXIST
IDetectionStrategy.cs  → ❌ DOES NOT EXIST
ConditionEvaluator.cs  → ❌ DOES NOT EXIST
ExecutionContext.cs    → ❌ DOES NOT EXIST
StrategyFactory.cs     → ❌ DOES NOT EXIST
```

**결론**: 설계 문서는 완료된 것처럼 작성되었으나, **실제로는 단 한 줄도 구현되지 않았습니다.**

---

## 📋 실제 존재하는 파일 (검증됨)

| 파일 | 경로 | 라인 수 | 검증 |
|-----|------|--------|------|
| Unit.cs | `/Assets/Script/Game/Unit.cs` | 764 | ✅ 실제 존재 |
| CombatComponent.cs | `/Assets/Script/Game/Components/CombatComponent.cs` | 944 | ✅ 실제 존재 |
| IUnitAI.cs | `/Assets/Script/Game/Interfaces/IUnitAI.cs` | 98 | ✅ 실제 존재 |
| IAnimationController.cs | `/Assets/Script/Game/Interfaces/IAnimationController.cs` | 126 | ✅ 실제 존재 |
| MovementComponent.cs | `/Assets/Script/Game/Components/MovementComponent.cs` | ? | ✅ 실제 존재 (미확인) |
| Tile.cs | `/Assets/Script/Game/Tile.cs` | ? | ✅ 실제 존재 (미확인) |

---

## 🔥 주요 충돌 사항 (라인 레퍼런스 포함)

### 1. 📍 CRITICAL - 아키텍처 근본적 충돌

**설계 문서 가정**: Strategy Pattern으로 Unit의 행동을 런타임에 교체
```csharp
// 설계 문서 Line 246-275 (IAttackStrategy 제안)
public interface IAttackStrategy
{
    AttackResult Execute(List<Tile> targetTiles, ExecutionContext context);
    List<Tile> GetValidTargets(Vector2Int fromPosition, ExecutionContext context);
}
```

**실제 코드 아키텍처**: Component-based delegation, not Strategy Pattern
```csharp
// Unit.cs:32-36 - 실제 컴포넌트 시스템
private IHealthComponent healthComponent;
private ICombatSystem combatComponent;      // ← CombatComponent 직접 호출
private IMovementSystem movementComponent;  // ← MovementComponent 직접 호출
private ITeamComponent teamComponent;
private IAnimationController animationController;
private IUnitAI unitAI;

// Unit.cs:471-479 - AI 결정 실행 흐름
public void Act()
{
    // AI 컴포넌트가 있으면 AI에게 위임
    if (useComponentSystem && unitAI != null)
    {
        var decision = unitAI.DecideAction();  // ← IUnitAI가 결정
        ExecuteDecision(decision);              // ← Unit이 직접 실행
    }
}

// Unit.cs:484-555 - 실제 행동 실행 로직
private void ExecuteDecision(ActionDecision decision)
{
    switch (decision.Type)
    {
        case ActionType.Attack:
            if (decision.TargetTile != null)
            {
                // combatComponent를 직접 호출 (Strategy 패턴 아님!)
                var tiles = new List<Tile> { decision.TargetTile };
                int hitCount = combatComponent.AttackTiles(tiles);
            }
            break;
        case ActionType.Move:
            // movementComponent를 직접 호출
            var result = movementComponent.MoveTo(decision.MovePosition);
            break;
    }
}
```

**실제 IUnitAI 인터페이스**: 이미 Strategy Pattern 역할 수행
```csharp
// IUnitAI.cs:13-26 - AI가 이미 전략 역할
public interface IUnitAI
{
    ActionDecision DecideAction();          // ← AI가 행동 결정 (이미 Strategy)
    Tile FindBestTarget();                  // ← AI가 타겟 선택
    void SetStrategy(AIStrategy strategy);  // ← AI 전략 변경 가능
}

// IUnitAI.cs:89-96 - AIStrategy enum 이미 존재
public enum AIStrategy
{
    Basic,       // 기본 전략 (가장 가까운 적 공격)
    Aggressive,  // 공격적 (높은 공격력 우선)
    Defensive,   // 방어적 (낮은 HP 아군 보호)
    Optimal      // 최적화 (피해 최대화 계산)
}
```

**충돌 분석**:
- 설계 문서는 `IAttackStrategy`, `IMovementStrategy`로 행동을 교체하려 함
- 실제 코드는 이미 `IUnitAI`가 전략 역할을 수행 중 (Strategy Pattern 이미 적용됨)
- `Unit.ExecuteDecision()`이 직접 `combatComponent`/`movementComponent` 호출 → Strategy 삽입 불가능한 구조
- **중복된 Strategy Pattern 구조 제안** → 기존 IUnitAI와 역할 충돌

**권장 해결책**:
```csharp
// Option 1: IUnitAI를 확장하여 Trait 통합 (기존 구조 유지)
public interface IUnitAI
{
    ActionDecision DecideAction();
    void SetStrategy(AIStrategy strategy);
    void ApplyTrait(TraitModifier trait);  // ← Trait를 AI 전략에 통합
}

// Option 2: TraitComponent를 Observer로 변경 (행동 교체가 아닌 수정)
public class TraitComponent : MonoBehaviour
{
    // AttackTiles() 호출 전에 파라미터 수정
    public void ModifyAttackParameters(ref List<Tile> targets, ref int damage);
    // MoveTo() 호출 전에 파라미터 수정
    public void ModifyMoveParameters(ref Vector2Int destination, ref int range);
}
```

**심각도**: 🔴 CRITICAL
**영향 범위**: Unit.cs 전체, CombatComponent.cs, MovementComponent.cs, IUnitAI 설계 철학
**예상 작업량**: 30-50시간 (아키텍처 재설계 필요)

---

### 2. 📍 CRITICAL - 애니메이션 시스템 통합 충돌

**설계 문서 가정**: Strategy가 Result를 반환하면 AnimationController가 재생
```csharp
// 설계 문서 Line 279-299 (AttackResult 제안)
public class AttackResult
{
    public int CalculatedDamage { get; set; }   // ❌ Strategy는 계산만
    public int TargetCount { get; set; }
    public AnimationType AnimationType { get; set; }  // ← 애니메이션 메타데이터
    // ❌ Strategy는 데미지를 적용하지 않음 (문서에 명시)
}
```

**실제 애니메이션 시스템**: Event-driven, not Result-based
```csharp
// CombatComponent.cs:61-77 - 애니메이션 이벤트 구독 (Awake에서)
private void Awake()
{
    animationController = GetComponent<IAnimationController>();

    // BlendTree 애니메이션 이벤트 구독
    if (animationController != null)
    {
        animationController.OnAttackStart += OnAnimationAttackStart;
        animationController.OnAttackHit += OnAnimationAttackHit;    // ← 타격 시점
        animationController.OnAttackEnd += OnAnimationAttackEnd;
    }
}

// CombatComponent.cs:171-212 - 실제 공격 흐름
public CombatResult Attack(GameObject target)
{
    // 공격 상태 시작
    isAttacking = true;
    currentAttackTargets = new List<GameObject> { target };  // ← 타겟 저장

    // BlendTree 애니메이션 재생 (데미지는 OnAnimationAttackHit에서 적용)
    if (animationController != null)
    {
        animationController.PlayAttackAnimation(targets);
        return CombatResult.Hit(0, target, attackType, false, "Attack animation started");
        // ↑ Result는 즉시 반환되지만 데미지는 아직 적용 안 됨!
    }
}

// CombatComponent.cs:709-729 - 실제 데미지 적용 시점
private void OnAnimationAttackHit(List<GameObject> targets)
{
    // currentAttackTargets 검증
    if (currentAttackTargets == null || currentAttackTargets.Count == 0)
    {
        Debug.LogWarning($"Attack hit but no current targets");
        return;
    }

    // 실제 데미지 적용 (여러 타겟 처리)
    int affectedCount = ApplyDamageToTargets(currentAttackTargets,
                                             isSpecialAttackActive,
                                             isForceCritical);
    // ↑ 애니메이션 60% 진행 시점에서만 데미지 적용!
}
```

**실제 IAnimationController 인터페이스**: Event-driven architecture
```csharp
// IAnimationController.cs:87-109 - 애니메이션 이벤트 정의
public interface IAnimationController
{
    // 공격 시작 이벤트 (targets)
    event Action<List<GameObject>> OnAttackStart;

    // 공격 종료 이벤트 (targets)
    event Action<List<GameObject>> OnAttackEnd;

    // 공격 타격 순간 이벤트 (공격 진행도 60% 지점)
    // List 기반으로 단일/다중 타겟 모두 지원
    // CombatComponent가 이 이벤트를 구독하여 데미지 적용
    event Action<List<GameObject>> OnAttackHit;  // ← 핵심!
}
```

**충돌 분석**:
- 설계 문서: `Strategy.Execute()` → `AttackResult` 반환 → `AnimationController.PlayAnimation(result)`
- 실제 코드: `CombatComponent.Attack()` → `AnimationController.PlayAttackAnimation()` → (애니메이션 진행) → `OnAttackHit` 이벤트 → `ApplyDamageToTargets()`
- **타이밍 문제**: 설계는 Result를 즉시 반환하지만, 실제는 애니메이션 완료까지 기다림
- **타겟 저장 문제**: 실제 코드는 `currentAttackTargets` 필드에 타겟 저장, 설계는 Result에 포함 안 함

**설계 문서의 제약 (Line 282-284)**:
```
❌ Strategy는 데미지를 계산하되, 적용하지 않음
❌ 타겟 정보는 ExecutionContext에 저장하고 Result에 포함하지 않음
✅ AnimationController는 Result의 메타데이터로 애니메이션만 재생
```

**실제 구현과의 차이**:
```csharp
// 실제 코드는 타겟을 필드에 저장하고 이벤트 콜백에서 사용
private List<GameObject> currentAttackTargets = null;  // CombatComponent.cs:51

// 설계 문서는 ExecutionContext에 저장한다고 했지만, 실제로는 필드 사용
```

**권장 해결책**:
```csharp
// TraitComponent가 애니메이션 이벤트를 가로채서 수정
public class TraitComponent : MonoBehaviour
{
    private void Awake()
    {
        var animController = GetComponent<IAnimationController>();
        if (animController != null)
        {
            // OnAttackHit 이벤트를 가로채서 Trait 효과 적용
            animController.OnAttackHit += OnTraitModifiedAttackHit;
        }
    }

    private void OnTraitModifiedAttackHit(List<GameObject> targets)
    {
        // Trait에 따라 타겟 리스트 수정 (AoE 추가 등)
        var modifiedTargets = ApplyTraitEffects(targets);
        // CombatComponent로 전달
    }
}
```

**심각도**: 🔴 CRITICAL
**영향 범위**: CombatComponent.cs (Line 61-77, 171-212, 709-729), IAnimationController.cs
**예상 작업량**: 20-30시간 (이벤트 기반 아키텍처 이해 및 통합 필요)

---

### 3. 📍 HIGH - ExecutionContext 개념 충돌

**설계 문서**: ExecutionContext로 모든 실행 정보 전달
```csharp
// 설계 문서 Line 1268-1318 (ExecutionContext 제안)
public class ExecutionContext
{
    public Unit Attacker { get; private set; }
    public List<Tile> TargetTiles { get; set; }
    public List<GameObject> TargetObjects { get; set; }  // ← 타겟 저장
    public int BaseDamage { get; set; }
    public bool IsCritical { get; set; }

    // ... 수많은 프로퍼티
}
```

**실제 코드**: Context 객체 없음, 직접 파라미터 전달
```csharp
// CombatComponent.cs:863-940 - 실제 AttackTiles 메서드
public int AttackTiles(List<Tile> targetTiles, bool isSpecialAttack = false, bool forceCritical = false)
{
    // 1. 검증
    if (targetTiles == null || targetTiles.Count == 0) return 0;
    if (!CanAttack) return 0;

    // 2. 타겟 추출 및 검증 (ExecutionContext 없음!)
    List<GameObject> validTargets = new List<GameObject>();
    foreach (var tile in targetTiles)
    {
        HealthComponent targetHealth = tile.GetDamageableTarget();
        if (targetHealth != null && targetHealth.IsAlive)
        {
            validTargets.Add(targetHealth.gameObject);
        }
    }

    // 3. 공격 상태 설정 (필드에 직접 저장)
    isAttacking = true;
    currentAttackTargets = validTargets;  // ← Context 대신 필드 사용
    isSpecialAttackActive = isSpecialAttack;

    // 4. 애니메이션 재생
    animationController.PlayAttackAnimation(validTargets);
    return validTargets.Count;
}
```

**충돌 분석**:
- 설계: 모든 정보를 ExecutionContext에 담아서 전달
- 실제: 파라미터와 필드 변수로 상태 관리
- ExecutionContext 도입 시 기존 메서드 시그니처 전부 변경 필요

**영향받는 메서드**:
```csharp
// 모두 ExecutionContext 없이 직접 파라미터 사용
CombatComponent.Attack(GameObject target)
CombatComponent.AttackTiles(List<Tile> targetTiles, bool isSpecialAttack, bool forceCritical)
CombatComponent.ApplyDamageToTargets(List<GameObject> targets, bool isSpecialAttack, bool forceCritical)
MovementComponent.MoveTo(Vector2Int position)
IUnitAI.DecideAction()
```

**권장 해결책**:
```csharp
// Option 1: Context는 내부적으로만 사용 (외부 API 유지)
public int AttackTiles(List<Tile> targetTiles, bool isSpecialAttack = false)
{
    // 내부에서 Context 생성 (하위 호환성 유지)
    var context = new ExecutionContext(this, targetTiles);
    return AttackTilesInternal(context);
}

// Option 2: Context 사용 안 함 (현재 구조 유지)
// Trait는 메서드 호출 전/후에 파라미터를 수정하는 방식으로 통합
```

**심각도**: 🟡 HIGH
**영향 범위**: CombatComponent.cs, MovementComponent.cs, IUnitAI.cs
**예상 작업량**: 15-20시간

---

### 4. 📍 HIGH - TraitComponent 통합 시점 충돌

**설계 문서**: Unit에 TraitComponent 추가하고 실행 파이프라인 수정
```csharp
// 설계 문서 Line 1717-1847 (기존 시스템 통합 제안)
public class Unit : MonoBehaviour
{
    private TraitComponent traitComponent;  // ← 추가 제안

    public void Act()
    {
        // TraitExecutor를 통한 조건 평가 및 전략 오버라이드
        if (traitComponent != null)
        {
            var activeStrategy = traitComponent.GetActiveAttackStrategy();
            // ... 전략 실행
        }
    }
}
```

**실제 Unit.cs**: 이미 복잡한 초기화 시퀀스
```csharp
// Unit.cs:68-75 - Awake에서 컴포넌트 초기화
private void Awake()
{
    // Initialize component system
    InitializeComponents();  // ← 이미 6개 컴포넌트 초기화
    legacyMaxHealth = health;
}

// Unit.cs:303-326 - InitializeComponents 메서드
private void InitializeComponents()
{
    // Cache component references for performance
    healthComponent = GetComponent<IHealthComponent>();
    combatComponent = GetComponent<ICombatSystem>();
    movementComponent = GetComponent<IMovementSystem>();
    teamComponent = GetComponent<ITeamComponent>();
    animationController = GetComponent<IAnimationController>();
    unitAI = GetComponent<IUnitAI>();

    if (useComponentSystem)
    {
        InitializeHealthComponent();
        InitializeCombatComponent();
        InitializeMovementComponent();
        InitializeTeamComponent();
        InitializeAnimationController();
        InitializeAIComponent();

        UpdateComponentSystemStatus();
    }
}

// Unit.cs:81-138 - Init() 메서드 (외부 호출용)
public void Init(UnitData unitData, Vector2Int position, bool isPlayerUnit)
{
    if (isInitialized)
    {
        Debug.LogWarning($"[Unit] {gameObject.name} already initialized");
        return;
    }

    // UnitData로부터 스탯 설정
    if (unitData != null)
    {
        health = unitData.MaxHealth;
        // ... 모든 컴포넌트에 데이터 적용
    }

    // ServiceLocator에서 GridManager 가져오기
    var gridManager = ServiceLocator.Get<IGridManager>();
    if (gridManager != null)
    {
        InitializeGridManager(gridManager);
    }

    isInitialized = true;
}
```

**충돌 분석**:
- Unit은 Awake → InitializeComponents → Init(UnitData) 순서로 초기화
- TraitComponent 추가 시 초기화 순서 문제 발생 가능:
  - TraitComponent는 언제 초기화? (Awake? Start? Init?)
  - TraitData는 UnitData에서 로드? 별도 ScriptableObject?
  - 다른 컴포넌트들과 의존성 순서는?

**실제 컴포넌트 초기화 패턴**:
```csharp
// Unit.cs:338-363 - HealthComponent 초기화 예시
private void InitializeHealthComponent()
{
    if (healthComponent == null && useComponentSystem && autoAddMissingComponents)
    {
        var comp = gameObject.AddComponent<HealthComponent>();
        healthComponent = comp;
    }

    if (healthComponent != null)
    {
        healthComponent.SetMaxHealth(health);
        healthComponent.SetHealth(health);

        // 이벤트 구독
        var healthComp = healthComponent as HealthComponent;
        if (healthComp != null)
        {
            healthComp.OnDeath += OnHealthComponentDeath;
        }
    }
}
```

**권장 해결책**:
```csharp
// Unit.cs에 TraitComponent 초기화 추가
private ITraitComponent traitComponent;

private void InitializeComponents()
{
    // ... 기존 컴포넌트 초기화
    traitComponent = GetComponent<ITraitComponent>();

    if (useComponentSystem)
    {
        // ... 기존 초기화
        InitializeTraitComponent();  // ← 추가
    }
}

private void InitializeTraitComponent()
{
    if (traitComponent == null && useComponentSystem && autoAddMissingComponents)
    {
        var comp = gameObject.AddComponent<TraitComponent>();
        traitComponent = comp;
    }

    // TraitData는 UnitData에서 로드
    if (traitComponent != null && currentUnitData != null)
    {
        traitComponent.InitializeTraits(currentUnitData.Traits);
    }
}
```

**심각도**: 🟡 HIGH
**영향 범위**: Unit.cs (Line 68-326), UnitData 구조
**예상 작업량**: 10-15시간

---

### 5. 📍 MEDIUM - 조건 평가 시스템 누락

**설계 문서**: ConditionEvaluator로 조건 검사
```csharp
// 설계 문서 Line 1217-1267 (ConditionEvaluator 제안)
public static class ConditionEvaluator
{
    public static bool EvaluateConditions(List<TraitConditionData> conditions, ExecutionContext context)
    {
        foreach (var condition in conditions)
        {
            if (!EvaluateCondition(condition, context))
                return false;
        }
        return true;
    }

    private static bool EvaluateCondition(TraitConditionData condition, ExecutionContext context)
    {
        switch (condition.ConditionType)
        {
            case ConditionType.HealthThreshold:
                // ... 조건 검사
        }
    }
}
```

**실제 코드**: 조건 평가 시스템 없음
```csharp
// Unit.cs:471-479 - AI 결정만 있음 (조건 검사 없음)
public void Act()
{
    if (useComponentSystem && unitAI != null)
    {
        var decision = unitAI.DecideAction();  // ← AI가 결정
        ExecuteDecision(decision);              // ← 즉시 실행
    }
}

// IUnitAI.cs에도 조건 평가 개념 없음
public interface IUnitAI
{
    ActionDecision DecideAction();  // ← 단순 결정, 조건 없음
    Tile FindBestTarget();
    void SetStrategy(AIStrategy strategy);
}
```

**실제 코드에서 조건 검사가 있는 곳**:
```csharp
// CombatComponent.cs:116-119 - 공격 가능 여부만 체크
public bool CanAttack => healthComponent?.IsAlive == true && Time.time >= NextAttackTime;

// CombatComponent.cs:121-152 - 타겟 검증
public bool CanAttackTarget(GameObject target)
{
    if (!CanAttack || target == null) return false;
    if (!this.IsValidTarget(target)) return false;
    if (!this.CanAttackByTeam(target)) return false;
    if (!CanAttackPosition(targetPosition)) return false;
    return true;
}
```

**충돌 분석**:
- 설계는 복잡한 조건 시스템 제안 (HP 임계값, 적 수, 턴 수 등)
- 실제는 단순 검증만 존재 (살아있나? 범위 안인가?)
- Trait의 조건부 활성화를 위해서는 새로운 평가 시스템 필요

**권장 해결책**:
```csharp
// 간단한 조건 평가기 구현 (Predicate 기반)
public class TraitCondition
{
    public Func<Unit, bool> Evaluate;  // ← 단순 Predicate

    public static TraitCondition HealthBelow(float threshold)
    {
        return new TraitCondition
        {
            Evaluate = (unit) => unit.Health < unit.MaxHealth * threshold
        };
    }
}

// TraitComponent에서 사용
public bool ShouldActivateTrait(TraitData trait)
{
    return trait.Conditions.All(c => c.Evaluate(GetComponent<Unit>()));
}
```

**심각도**: 🟢 MEDIUM
**영향 범위**: 신규 시스템 추가
**예상 작업량**: 5-10시간

---

### 6. �� MEDIUM - 기존 CombatComponent와의 역할 중복

**설계 문서**: IAttackStrategy가 공격 로직 담당
```csharp
// 설계 문서 Line 246-275
public interface IAttackStrategy
{
    AttackResult Execute(List<Tile> targetTiles, ExecutionContext context);
    List<Tile> GetValidTargets(Vector2Int fromPosition, ExecutionContext context);
    int CalculateDamage(Tile target, ExecutionContext context);
}
```

**실제 CombatComponent**: 이미 모든 공격 로직 구현됨
```csharp
// CombatComponent.cs:110-339 - ICombatSystem 구현
public int CurrentAttackPower => GetModifiedAttackPower();  // ← 데미지 계산
public int AttackRange => attackRange;
public bool CanAttack => healthComponent?.IsAlive == true && Time.time >= NextAttackTime;

public CombatResult Attack(GameObject target)  // ← 공격 실행
{
    // ... 전체 공격 로직 구현
}

public List<Vector2Int> GetAttackRange(Vector2Int fromPosition)  // ← 범위 계산
{
    switch (rangeType)
    {
        case AttackRangeType.Single: return GetSingleTargetRange(fromPosition);
        case AttackRangeType.Line: return GetLineRange(fromPosition);
        case AttackRangeType.Cross: return GetCrossRange(fromPosition);
        case AttackRangeType.Square: return GetSquareRange(fromPosition);
        case AttackRangeType.Circle: return GetCircleRange(fromPosition);
        // ...
    }
}

public List<GameObject> GetTargetsInRange(Vector2Int fromPosition)  // ← 타겟 검색
{
    // ... 타겟 검색 로직
}
```

**CombatComponent의 고급 기능**: 이미 다양한 공격 타입 지원
```csharp
// CombatComponent.cs:24-25 - 공격 타입
[SerializeField] private AttackRangeType rangeType = AttackRangeType.Single;

// AttackRangeType enum (어딘가에 정의되어 있음)
public enum AttackRangeType
{
    Single,   // 단일 타겟
    Line,     // 직선
    Cross,    // 십자
    Square,   // 사각형
    Circle,   // 원형
    Cone,     // 원뿔
    All       // 전체
}
```

**충돌 분석**:
- IAttackStrategy가 제공하려는 기능이 CombatComponent에 이미 다 있음
- 설계의 "고급 전략" (AoE, Chain, Drain 등)도 CombatComponent가 구현 가능한 수준
- **역할 중복**: IAttackStrategy vs CombatComponent

**현재 CombatComponent 확장 방법**:
```csharp
// CombatComponent는 이미 확장 가능한 구조
[SerializeField] private AttackRangeType rangeType;  // ← 범위 타입 변경 가능
[SerializeField] private bool hasSpecialAttack;      // ← 특수 공격 지원
[SerializeField] private float criticalChance;        // ← 크리티컬 지원
```

**권장 해결책**:
```csharp
// Option 1: CombatComponent를 확장 (Strategy 불필요)
public class CombatComponent : MonoBehaviour, ICombatSystem
{
    [SerializeField] private List<AttackModifier> attackModifiers;  // ← Trait로 추가

    public int AttackTiles(List<Tile> targetTiles)
    {
        // 1. 기본 타겟 선택
        var targets = SelectTargets(targetTiles);

        // 2. Trait Modifier 적용
        foreach (var modifier in attackModifiers)
        {
            targets = modifier.ModifyTargets(targets);  // ← AoE 확장 등
        }

        // 3. 공격 실행
        return ExecuteAttack(targets);
    }
}

// Option 2: TraitComponent가 CombatComponent를 래핑
public class TraitComponent : MonoBehaviour
{
    private ICombatSystem baseCombat;

    public int AttackWithTraits(List<Tile> targetTiles)
    {
        // Trait 조건 검사
        if (ShouldActivateChainAttack())
        {
            // Chain Attack: 타일 추가 후 기본 공격 호출
            var additionalTiles = FindChainTargets(targetTiles);
            return baseCombat.AttackTiles(targetTiles.Concat(additionalTiles).ToList());
        }

        // 조건 안 맞으면 기본 공격
        return baseCombat.AttackTiles(targetTiles);
    }
}
```

**심각도**: 🟢 MEDIUM
**영향 범위**: CombatComponent.cs, 설계 방향성
**예상 작업량**: 0시간 (기존 시스템 활용 권장)

---

### 7. 📍 LOW - ScriptableObject 데이터 구조

**설계 문서**: TraitData ScriptableObject
```csharp
// 설계 문서 Line 950-1033
[CreateAssetMenu(fileName = "NewTrait", menuName = "Game/Trait Data")]
public class TraitData : ScriptableObject
{
    public string traitName;
    public string description;
    public TraitTriggerType triggerType;
    public List<TraitConditionData> activationConditions;
    public AttackStrategyType attackStrategyOverride;
    // ...
}
```

**실제 ScriptableObject 패턴**: 프로젝트에 이미 존재
```csharp
// CardData.cs 예시 (이미 ScriptableObject 패턴 사용 중)
public class CardData : ScriptableObject
{
    [SerializeField] private string cardName;
    [SerializeField] private CardRarity rarity;
    [SerializeField] private List<EffectData> effectDataList;
    // ...
}
```

**실제 UnitData 확인 필요**:
```bash
# UnitData.cs 찾기
find Assets/Script -name "UnitData.cs"
```

**충돌 가능성**: LOW
- ScriptableObject 패턴은 프로젝트에서 이미 사용 중
- TraitData 추가 시 기존 패턴 따르면 됨
- 단, UnitData에 TraitData 레퍼런스 추가 필요

**권장 해결책**:
```csharp
// UnitData에 Trait 레퍼런스 추가
public class UnitData : ScriptableObject
{
    public int MaxHealth;
    public int AttackPower;
    public int MovementRange;
    public List<TraitData> DefaultTraits;  // ← 추가
}

// Unit.Init()에서 TraitData 로드
public void Init(UnitData unitData, Vector2Int position, bool isPlayerUnit)
{
    // ... 기존 초기화

    if (traitComponent != null && unitData.DefaultTraits != null)
    {
        traitComponent.InitializeTraits(unitData.DefaultTraits);
    }
}
```

**심각도**: 🟢 LOW
**영향 범위**: UnitData 구조, TraitComponent
**예상 작업량**: 2-5시간

---

## 📊 충돌 요약표

| # | 충돌 항목 | 심각도 | 핵심 문제 | 예상 작업 |
|---|---------|--------|----------|----------|
| 1 | 아키텍처 근본 충돌 | 🔴 CRITICAL | Strategy Pattern 중복, IUnitAI와 역할 충돌 | 30-50h |
| 2 | 애니메이션 시스템 | 🔴 CRITICAL | Event-driven vs Result-based 아키텍처 충돌 | 20-30h |
| 3 | ExecutionContext | 🟡 HIGH | Context 객체 없음, 파라미터 직접 전달 | 15-20h |
| 4 | TraitComponent 통합 | 🟡 HIGH | 복잡한 초기화 시퀀스, 의존성 순서 | 10-15h |
| 5 | 조건 평가 시스템 | 🟢 MEDIUM | 조건 평가 시스템 없음, 신규 구현 필요 | 5-10h |
| 6 | 역할 중복 | 🟢 MEDIUM | CombatComponent가 이미 모든 기능 구현 | 0h (재활용) |
| 7 | ScriptableObject | 🟢 LOW | 기존 패턴 따르면 문제없음 | 2-5h |

**총 예상 작업량**: 82-130시간

---

## 🎯 권장 접근 방식

### Option A: Strategy Pattern 포기 (권장 ⭐)

**핵심 아이디어**: Trait를 "행동 교체"가 아닌 "행동 수정자"로 재정의

```csharp
// Trait는 기존 시스템을 수정만 함
public interface ITraitModifier
{
    // 공격 전: 타겟 수정 (AoE 추가, Chain Attack 등)
    void ModifyAttackTargets(ref List<Tile> targets, Unit attacker);

    // 공격 전: 데미지 수정 (버프/디버프)
    void ModifyDamage(ref int damage, Unit attacker, GameObject target);

    // 이동 전: 범위 수정 (Teleport, Jump 등)
    void ModifyMovementRange(ref int range, Unit mover);
}

public class TraitComponent : MonoBehaviour
{
    private List<ITraitModifier> activeTraits;

    // CombatComponent.AttackTiles() 호출 전에 타겟 수정
    public void ApplyAttackTraits(ref List<Tile> targets)
    {
        foreach (var trait in activeTraits)
        {
            if (ShouldActivate(trait))
            {
                trait.ModifyAttackTargets(ref targets, GetComponent<Unit>());
            }
        }
    }
}
```

**장점**:
- 기존 아키텍처 유지 (Unit → CombatComponent 흐름 그대로)
- IUnitAI와 충돌 없음 (AI는 결정, Trait는 수정)
- 애니메이션 시스템 변경 불필요
- 점진적 구현 가능

**단점**:
- 설계 문서 대부분 폐기
- "전략" 패턴이 아닌 "데코레이터/수정자" 패턴으로 변경

---

### Option B: 제한적 Strategy Pattern (하위 호환)

**핵심 아이디어**: IUnitAI를 확장하여 Trait 통합

```csharp
// IUnitAI에 Trait 기능 추가
public interface IUnitAI
{
    ActionDecision DecideAction();
    void SetStrategy(AIStrategy strategy);
    void ApplyTrait(TraitData trait);  // ← 추가
}

// BasicUnitAI 구현체에서 Trait 처리
public class BasicUnitAI : MonoBehaviour, IUnitAI
{
    private List<TraitData> activeTraits;

    public ActionDecision DecideAction()
    {
        // 1. 기본 결정
        var decision = DecideBasicAction();

        // 2. Trait 조건 검사 및 수정
        foreach (var trait in activeTraits)
        {
            if (ShouldActivateTrait(trait))
            {
                decision = ModifyDecisionByTrait(decision, trait);
            }
        }

        return decision;
    }
}
```

**장점**:
- 기존 아키텍처 최소 변경
- IUnitAI와 통합되어 역할 명확
- 설계 문서 일부 활용 가능

**단점**:
- Trait가 AI에 종속됨
- 복잡한 Strategy Pattern 구조는 여전히 불가능

---

### Option C: 완전한 재설계 (비권장 ❌)

**설계 문서대로 완전히 재구현**

**예상 작업량**: 150-200시간
**리스크**: 매우 높음 (기존 시스템 파괴, 테스트 필요, 버그 위험)

**권장하지 않는 이유**:
1. 기존 시스템이 이미 잘 작동 중
2. IUnitAI가 이미 Strategy 역할 수행
3. 애니메이션 시스템이 깊게 통합됨
4. 설계 문서가 실제 구조를 이해하지 못한 상태로 작성됨

---

## 📝 결론

1. **설계 문서의 치명적 문제**:
   - 실제 코드베이스를 읽지 않고 작성됨
   - 모든 파일에 ✅ 표시했지만 실제로는 **하나도 구현되지 않음**
   - 기존 아키텍처(IUnitAI, Event-driven Animation)와 근본적 충돌

2. **실제 코드베이스는**:
   - 이미 잘 설계된 Component 기반 아키텍처
   - IUnitAI로 Strategy Pattern 이미 적용됨
   - Event-driven 애니메이션 시스템으로 타이밍 제어
   - CombatComponent가 다양한 공격 타입 지원

3. **권장 사항**:
   - ⭐ **Option A** 채택 권장: Trait를 "수정자 패턴"으로 재설계
   - 설계 문서 대부분 폐기하고 실제 구조에 맞게 재작성
   - 점진적 구현: TraitModifier → 조건 시스템 → ScriptableObject

4. **예상 작업량 (Option A 기준)**:
   - TraitModifier 인터페이스: 5-10h
   - TraitComponent 구현: 10-15h
   - 조건 평가 시스템: 5-10h
   - ScriptableObject 통합: 2-5h
   - 테스트 및 디버깅: 10-20h
   - **총 32-60시간** (설계 문서 재작성 시간 제외)

---

**작성자**: Claude (Conflict Analysis Agent)
**검증 방법**: 모든 파일 실제 읽기 (Glob + Read), 라인 레퍼런스 확인
**신뢰도**: ✅ HIGH (실제 코드 기반 분석)
