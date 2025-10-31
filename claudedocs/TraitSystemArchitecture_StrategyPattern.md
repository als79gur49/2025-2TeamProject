# 전략 패턴 기반 특성(Trait) 시스템 아키텍처

> **작성일**: 2025-10-30
> **목적**: 유닛의 공격/이동/탐색 행동을 조건부로 변경 가능한 확장 가능한 아키텍처 설계

---

## 📋 목차

1. [아키텍처 개요](#1-아키텍처-개요)
2. [핵심 구성요소](#2-핵심-구성요소)
3. [Strategy Pattern 계층](#3-strategy-pattern-계층)
4. [ScriptableObject 데이터 계층](#4-scriptableobject-데이터-계층)
5. [Executor 실행 계층](#5-executor-실행-계층)
6. [기존 시스템 통합](#6-기존-시스템-통합)
7. [디렉토리 구조](#7-디렉토리-구조)
8. [SOLID 원칙 준수](#8-solid-원칙-준수)
9. [확장성 예시](#9-확장성-예시)
10. [구현 단계](#10-구현-단계)

---

## 1. 아키텍처 개요

### 1.1 설계 목표

1. **전략 패턴 적용**: 유닛의 공격/이동/탐색 행동을 런타임에 교체 가능하게 설계
2. **데이터 주도 설계**: 특성의 조건을 ScriptableObject로 분리하여 디자이너가 코드 없이 수정 가능
3. **Executor 아키텍처**: 조건 평가와 실행 로직을 중앙에서 관리하여 일관성 보장
4. **확장성**: 새로운 전략/조건 추가 시 기존 코드 수정 최소화
5. **유지보수성**: 명확한 책임 분리로 디버깅 및 테스트 용이

### 1.2 핵심 설계 패턴

```
┌─────────────────────────────────────────────────────────────┐
│                   DESIGN PATTERNS USED                       │
├─────────────────────────────────────────────────────────────┤
│ • Strategy Pattern      → 행동 알고리즘 캡슐화              │
│ • Factory Pattern       → 전략/조건 객체 생성               │
│ • Executor Pattern      → 실행 파이프라인 중앙화            │
│ • Component Pattern     → 유닛에 특성 기능 추가             │
│ • Service Locator       → 의존성 주입 (기존 시스템 활용)    │
└─────────────────────────────────────────────────────────────┘
```

### 1.3 아키텍처 다이어그램

#### 전체 시스템 흐름 (AI → Unit → Strategy → AnimationController)

```
┌─────────────────────────────────────────────────────────────────────┐
│                   🧠 AI DECISION LAYER                               │
│  ┌───────────────────────────────────────────────────────────┐      │
│  │ IUnitAI.DecideAction() → ActionDecision                  │      │
│  │  1. TraitComponent에서 활성 Strategy 가져오기            │      │
│  │  2. Strategy.GetValidTargets()로 가능한 타겟 목록 얻기   │      │
│  │  3. AI 로직으로 최적 타겟 선택                           │      │
│  │  • ActionType.Attack → TargetTile 선택 (AI 판단)        │      │
│  │  • ActionType.Move → TargetPosition 선택 (AI 판단)      │      │
│  │  • ActionType.Wait → 대기                                │      │
│  └───────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                   📦 UNIT EXECUTION LAYER                            │
│  ┌───────────────────────────────────────────────────────────┐      │
│  │ Unit.ExecuteDecision(ActionDecision)                      │      │
│  │  1. ExecutionContext 생성 (AI가 선택한 타겟 포함)        │      │
│  │  2. TraitComponent에서 활성 Strategy 가져오기            │      │
│  │  3. Strategy.Execute(AI선택타겟) 호출                    │      │
│  │  4. Result 수신 (애니메이션 메타데이터)                  │      │
│  │  5. AnimationController.PlayAnimation() 신호 전달        │      │
│  │  6. OnAttackHit 콜백 → Context 타겟에 데미지 적용        │      │
│  └───────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                   🎮 STRATEGY LAYER (순수 로직)                      │
│  ┌───────────────────────────────────────────────────────────┐      │
│  │ IAttackStrategy.Execute(AI선택타겟) / IMovementStrategy   │      │
│  │  • AI가 선택한 Primary 타겟 + 추가 타겟 계산            │      │
│  │    (예: AoE는 주변 타겟 추가, Chain은 연쇄 타겟 추가)   │      │
│  │  • 모든 타겟을 Context에 저장                           │      │
│  │  • 데미지 계산 (❌ 적용하지 않음!)                       │      │
│  │  • Result 반환 (애니메이션 메타데이터만)                │      │
│  │  ❌ VFX/SFX 직접 재생 금지                              │      │
│  │  ❌ 데미지 즉시 적용 금지                               │      │
│  └───────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                   🎬 ANIMATION LAYER (타이밍 신호)                   │
│  ┌───────────────────────────────────────────────────────────┐      │
│  │ IAnimationController.PlayAnimation(result)                │      │
│  │  • Result.AnimationType 기반 애니메이션 선택              │      │
│  │  • VFX 프리팹 인스턴스화 및 재생                          │      │
│  │  • 타격 시점에 OnAttackHit?.Invoke() (타겟 없이!)         │      │
│  │  ❌ 타겟 정보 저장하지 않음                              │      │
│  └───────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                   💥 DAMAGE APPLICATION LAYER                        │
│  ┌───────────────────────────────────────────────────────────┐      │
│  │ CombatComponent.OnAnimationAttackHit()                    │      │
│  │  • Context에서 저장된 타겟 가져오기                       │      │
│  │  • 타겟 유효성 재확인 (null 체크, IsAlive 체크)          │      │
│  │  • ApplyDamageToTargets() 실행                           │      │
│  └───────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────┘
```

#### 컴포넌트 구조

```
┌───────────────────────────────────────────────────────────────────┐
│                        UNIT (GameObject)                          │
│  ┌──────────────┐  ┌───────────────┐  ┌─────────────────────┐   │
│  │HealthComp    │  │ CombatComp    │  │ MovementComp        │   │
│  │ (IHealth)    │  │ (ICombat)     │  │ (IMovement)         │   │
│  └──────────────┘  └───────────────┘  └─────────────────────┘   │
│         ↓                  ↓                     ↓                │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              🎯 TraitComponent                           │   │
│  │  ┌────────────────────────────────────────────────┐     │   │
│  │  │ • List<TraitInstance> traits                  │     │   │
│  │  │ • TraitExecutor executor                      │     │   │
│  │  │ • IAttackStrategy currentAttackStrategy       │     │   │
│  │  │ • IMovementStrategy currentMovementStrategy   │     │   │
│  │  │ • IDetectionStrategy currentDetectionStrategy │     │   │
│  │  └────────────────────────────────────────────────┘     │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              🎬 AnimationController                      │   │
│  │  ┌────────────────────────────────────────────────┐     │   │
│  │  │ • PlayAttackAnimation()                       │     │   │
│  │  │ • PlayMovementAnimation()                     │     │   │
│  │  │ • PlayTraitActivationAnimation()              │     │   │
│  │  └────────────────────────────────────────────────┘     │   │
│  └──────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────┘
                           │
                           │ uses
                           ↓
┌───────────────────────────────────────────────────────────────────┐
│                    ⚙️ TraitExecutor                               │
│  ┌────────────────────────────────────────────────────────┐      │
│  │ • CanExecute(trait, context) → bool                    │      │
│  │ • Execute(trait, context) → ExecutionResult            │      │
│  │ • ManageStrategySwitch(trait)                          │      │
│  │ • ApplyCooldown(trait)                                 │      │
│  └────────────────────────────────────────────────────────┘      │
└───────────────────────────────────────────────────────────────────┘
                     │                      │
         ┌───────────┴───────────┐
         ↓                       ↓
┌──────────────────┐  ┌────────────────────┐
│ 🔍 Condition     │  │ 🏭 Strategy        │
│ Evaluator        │  │ Factory            │
│ (static)         │  │                    │
│ • Evaluate()     │  │ • CreateAttack()   │
│ • CheckHealth()  │  │ • CreateMovement() │
│ • CheckRange()   │  │ • CreateDetection()│
└──────────────────┘  └────────────────────┘
         ↓                       ↓
┌──────────────────┐  ┌────────────────────┐
│ 📦 Trait         │  │ 🎮 Strategy        │
│ ConditionData    │  │ Implementations    │
│ (ScriptableObj)  │  │                    │
│                  │  │ • DefaultStrategy  │
│ • Type           │  │ • AoEStrategy      │
│ • Operator       │  │ • ChainStrategy    │
│ • Value          │  │ • TeleportStrategy │
│ • Parameter      │  │ • ...              │
└──────────────────┘  └────────────────────┘
         ↑
         │
         └──────────────────┬───────────────
                            │
                ┌───────────────────────┐
                │ 📋 TraitData          │
                │ (ScriptableObject)    │
                │                       │
                │ • Name/Icon           │
                │ • Conditions[]        │
                │ • StrategyOverrides   │
                │ • Priority/Cooldown   │
                │ • TriggerTypes[]      │
                └───────────────────────┘
                            ↑
                            │ creates
                            │
                ┌───────────────────────┐
                │ 🔄 TraitInstance      │
                │ (Runtime)             │
                │                       │
                │ • TraitData data      │
                │ • bool IsActive       │
                │ • bool IsOnCooldown   │
                │ • float CooldownTimer │
                │ • Applied Strategies  │
                └───────────────────────┘
```

---

## 2. 핵심 구성요소

### 2.1 구성요소 개요

| 컴포넌트 | 타입 | 책임 | 수정 가능성 |
|---------|------|------|-------------|
| **TraitData** | ScriptableObject | 특성 정의 데이터 저장 | 디자이너 (Inspector) |
| **TraitConditionData** | ScriptableObject | 활성화 조건 정의 | 디자이너 (Inspector) |
| **TraitInstance** | Runtime Class | 특성 런타임 상태 관리 | 자동 (코드) |
| **TraitExecutor** | Orchestrator | 실행 파이프라인 조정 | 개발자 (확장) |
| **ConditionEvaluator** | Static Utility | 조건 평가 로직 | 개발자 (조건 추가) |
| **IStrategy 구현체** | Strategy Classes | 행동 알고리즘 구현 | 개발자 (전략 추가) |
| **TraitComponent** | MonoBehaviour | 유닛의 특성 컨테이너 | 자동 (통합) |

### 2.2 데이터 흐름

```
[디자이너가 TraitData 생성]
        ↓
[Unity Inspector에서 조건 설정]
        ↓
[게임 시작 시 TraitInstance 생성]
        ↓
[트리거 발생 (공격/이동/피격 등)]
        ↓
[TraitExecutor.CanExecute() 호출]
        ↓
[ConditionEvaluator로 모든 조건 검사]
        ↓
조건 충족?
├─ YES → [TraitExecutor.Execute() 호출]
│         ├─ 전략 오버라이드 적용
│         └─ 결과 반환
│
└─ NO → [다음 특성 검사]
```

---

## 3. Strategy Pattern 계층

### 3.1 IAttackStrategy (공격 전략 인터페이스)

```csharp
/// <summary>
/// 공격 행동 전략 인터페이스
///
/// ✅ AI가 GetValidTargets()로 가능한 타겟 목록을 얻음
/// ✅ AI가 최적 Primary 타겟을 선택하여 ActionDecision 반환
/// ✅ CombatComponent가 Strategy.Execute()로 최종 타겟 타일 계산
///    - DefaultStrategy: inputTiles 그대로 반환
///    - AoEStrategy: inputTiles[0] 중심 + 주변 타일 추가
///    - ChainStrategy: inputTiles[0] 시작점 + 연쇄 타일 추가
///
/// ⚠️ List<Tile>이 필수인 이유:
///    1. Multi-tile entity (2x2 Base) 중복 제거를 위해 여러 타일 필요
///    2. AoE/Chain 같은 전략은 여러 타일 반환
///    3. Player가 직접 여러 타일 선택 가능 (확장성)
/// </summary>
public interface IAttackStrategy
{
    /// <summary>
    /// 최종 공격 대상 타일 계산
    /// ⚠️ 이 메서드는 타일 계산만 수행, 데미지 적용은 CombatComponent가 담당
    /// </summary>
    /// <param name="inputTiles">
    /// 입력 타일 (AI 선택 또는 Player 선택)
    /// - AI: List { primaryTile } - 단일 타일
    /// - Player: 직접 선택한 여러 타일 가능
    /// </param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>
    /// 최종 공격 대상 타일 리스트
    /// - DefaultStrategy: inputTiles 그대로 반환
    /// - AoEStrategy: inputTiles[0] 중심 + 주변 타일 추가 반환
    /// - ChainStrategy: inputTiles[0] 시작점 + 연쇄 타일 추가 반환
    /// ⚠️ CombatComponent가 이 타일 리스트로 고유 타겟 추출 (중복 제거)
    /// </returns>
    List<Tile> Execute(List<Tile> inputTiles, ExecutionContext context);

    /// <summary>
    /// 유효한 공격 대상 타일 가져오기 (AI 전용)
    /// ⚠️ AI가 DecideAction()에서 호출하여 Primary 타겟 선택에 사용
    /// </summary>
    /// <param name="fromPosition">공격자 위치</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>공격 가능한 타일 리스트 (AI가 선택 가능한 옵션)</returns>
    List<Tile> GetValidTargets(Vector2Int fromPosition, ExecutionContext context);

    /// <summary>예상 데미지 계산 (UI 프리뷰용, AI 판단용)</summary>
    /// <param name="target">대상 타일</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>예상 데미지</returns>
    int CalculateDamage(Tile target, ExecutionContext context);

    /// <summary>전략 식별자</summary>
    string StrategyName { get; }

    /// <summary>전략 타입</summary>
    AttackStrategyType StrategyType { get; }
}
```

#### AttackResult (공격 결과 클래스)
```csharp
/// <summary>
/// 공격 전략 실행 결과 (비즈니스 로직 결과 + 애니메이션 메타데이터)
/// ❌ Strategy는 데미지를 계산하되, 적용하지 않음
/// ❌ 타겟 정보는 ExecutionContext에 저장하고 Result에 포함하지 않음
/// ✅ AnimationController는 Result의 메타데이터로 애니메이션만 재생
/// </summary>
public class AttackResult
{
    // ========== 비즈니스 로직 결과 (참고용) ==========
    /// <summary>계산된 총 데미지 (아직 적용되지 않음)</summary>
    public int CalculatedDamage { get; set; }

    /// <summary>타겟 수</summary>
    public int TargetCount { get; set; }

    /// <summary>실행 성공 여부</summary>
    public bool Success { get; set; }

    /// <summary>에러 메시지 (실패 시)</summary>
    public string ErrorMessage { get; set; }

    // ========== 애니메이션 메타데이터 (AnimationController가 사용) ==========
    /// <summary>애니메이션 타입 (AnimationController가 사용)</summary>
    public AttackAnimationType AnimationType { get; set; }

    /// <summary>VFX 재생 위치들 (타겟이 아닌 위치 정보만)</summary>
    public Vector3[] AnimationTargets { get; set; }

    /// <summary>애니메이션 지속 시간</summary>
    public float AnimationDuration { get; set; }

    /// <summary>애니메이션 파라미터 (범위, 체인 수 등)</summary>
    public Dictionary<string, object> AnimationParameters { get; set; }

    /// <summary>VFX 프리팹 (TraitData에서 가져옴)</summary>
    public GameObject VFXPrefab { get; set; }

    /// <summary>SFX 클립</summary>
    public AudioClip SFXClip { get; set; }
}

/// <summary>
/// 애니메이션 타입 열거형
/// </summary>
public enum AttackAnimationType
{
    Default,        // 기본 단일 공격
    AoE,            // 범위 공격
    Chain,          // 연쇄 공격
    Drain,          // 흡혈 공격
    Pierce,         // 관통 공격
    Projectile,     // 투사체 공격
    Cleave,         // 휩쓸기 공격
    DotDamage       // 지속 피해
}
```

### 3.2 공격 전략 구현체

#### DefaultAttackStrategy (기본 단일 공격)
```csharp
public class DefaultAttackStrategy : IAttackStrategy
{
    public string StrategyName => "기본 공격";
    public AttackStrategyType StrategyType => AttackStrategyType.Default;

    /// <summary>AI가 선택한 단일 타겟 공격</summary>
    public AttackResult Execute(Tile primaryTarget, ExecutionContext context)
    {
        // 1️⃣ 데미지 계산 (적용하지 않음!)
        var damage = CalculateDamage(primaryTarget, context);

        // 2️⃣ 타겟을 ExecutionContext에 저장
        var damageable = primaryTarget.GetDamageableTarget();
        if (damageable != null && damageable.IsAlive)
        {
            context.SetCurrentAttackTargets(new List<GameObject> { damageable.gameObject });
            context.SetParameter("CalculatedDamage", damage);
        }

        // 3️⃣ 애니메이션 메타데이터만 반환
        return new AttackResult
        {
            CalculatedDamage = damage,
            TargetCount = 1,
            Success = true,
            AnimationType = AttackAnimationType.Default,
            AnimationTargets = new[] { primaryTarget.WorldPosition },
            AnimationDuration = 0.5f,
            VFXPrefab = context.GetVFXPrefab("DefaultAttack"),
            SFXClip = context.GetSFXClip("Attack")
        };
    }

    /// <summary>AI가 호출: 공격 가능한 모든 타일 반환</summary>
    public List<Tile> GetValidTargets(Vector2Int fromPosition, ExecutionContext context)
    {
        // 기존 CombatComponent의 공격 범위 로직 재사용
        var combatComponent = context.Executor.GetComponent<ICombatSystem>();
        return combatComponent.GetAttackRangeTiles(fromPosition);
    }

    public int CalculateDamage(Tile target, ExecutionContext context)
    {
        var combatComponent = context.Executor.GetComponent<ICombatSystem>();
        return combatComponent.CurrentAttackPower;
    }
}
```

#### AoEAttackStrategy (범위 공격)
```csharp
public class AoEAttackStrategy : IAttackStrategy
{
    public int AoERadius { get; set; } = 1;
    public float DamageMultiplier { get; set; } = 1.0f;

    public string StrategyName => "범위 공격";
    public AttackStrategyType StrategyType => AttackStrategyType.AoE;

    /// <summary>
    /// AI가 선택한 중심 타일을 기반으로 AoE 범위 공격
    /// ✅ AI는 centerTile만 선택, Strategy가 주변 타겟 자동 계산
    /// </summary>
    public AttackResult Execute(Tile primaryTarget, ExecutionContext context)
    {
        var centerTile = primaryTarget; // AI가 선택한 중심 타일
        var gridManager = context.GridManager;

        // 1️⃣ AoE 범위 내 추가 타겟 계산
        var affectedTiles = gridManager.GetTilesInRadius(
            centerTile.GridPosition,
            AoERadius
        );

        // 유효한 타겟만 필터링 (적군, 살아있음)
        List<GameObject> validTargets = new List<GameObject>();
        int totalCalculatedDamage = 0;
        var teamComponent = context.Executor.GetComponent<ITeamComponent>();

        foreach (var tile in affectedTiles)
        {
            var damageable = tile.GetDamageableTarget();
            if (damageable == null || !damageable.IsAlive) continue;

            // 적군 체크
            var targetTeam = damageable.GetComponent<ITeamComponent>();
            if (targetTeam != null && targetTeam.Team == teamComponent.Team) continue;

            validTargets.Add(damageable.gameObject);
            int damage = CalculateDamage(tile, context);
            totalCalculatedDamage += damage;
        }

        // 2️⃣ 모든 타겟을 ExecutionContext에 저장 (데미지 적용하지 않음!)
        context.SetCurrentAttackTargets(validTargets);
        context.SetParameter("CalculatedDamagePerTarget", totalCalculatedDamage / Mathf.Max(validTargets.Count, 1));

        // 3️⃣ 애니메이션 메타데이터만 반환
        return new AttackResult
        {
            CalculatedDamage = totalCalculatedDamage,
            TargetCount = validTargets.Count,
            Success = validTargets.Count > 0,
            AnimationType = AttackAnimationType.AoE,
            AnimationTargets = new[] { centerTile.WorldPosition },
            AnimationDuration = 1.0f,
            AnimationParameters = new Dictionary<string, object>
            {
                { "Radius", AoERadius },
                { "DamageMultiplier", DamageMultiplier }
            },
            VFXPrefab = context.GetVFXPrefab("AoEExplosion"),
            SFXClip = context.GetSFXClip("Explosion")
        };
    }

    /// <summary>
    /// AI가 호출: AoE 공격 가능한 중심 타일 반환
    /// ⚠️ AoE는 범위 내 타겟을 자동 계산하므로, 가능한 "중심점" 후보만 반환
    /// </summary>
    public List<Tile> GetValidTargets(Vector2Int fromPosition, ExecutionContext context)
    {
        var combatComponent = context.Executor.GetComponent<ICombatSystem>();
        return combatComponent.GetAttackRangeTiles(fromPosition);
    }

    public int CalculateDamage(Tile target, ExecutionContext context)
    {
        var baseDamage = context.Executor.GetComponent<ICombatSystem>().CurrentAttackPower;
        return Mathf.RoundToInt(baseDamage * DamageMultiplier);
    }
}
```

#### ChainAttackStrategy (연쇄 공격)
```csharp
public class ChainAttackStrategy : IAttackStrategy
{
    public int MaxChains { get; set; } = 3;
    public float DamageReductionPerChain { get; set; } = 0.25f; // 25% 감소
    public int ChainRange { get; set; } = 2;

    public string StrategyName => "연쇄 공격";
    public AttackStrategyType StrategyType => AttackStrategyType.Chain;

    /// <summary>
    /// AI가 선택한 첫 번째 타겟을 기반으로 연쇄 공격
    /// ✅ AI는 첫 타겟만 선택, Strategy가 연쇄 타겟 자동 계산
    /// </summary>
    public AttackResult Execute(Tile primaryTarget, ExecutionContext context)
    {
        var firstTarget = primaryTarget; // AI가 선택한 첫 타겟
        var gridManager = context.GridManager;
        var teamComponent = context.Executor.GetComponent<ITeamComponent>();

        // 1️⃣ 연쇄 타겟 계산 (데미지 적용하지 않음!)
        List<Tile> chainedTiles = new List<Tile> { firstTarget };
        List<GameObject> chainedTargets = new List<GameObject>();
        HashSet<Tile> hitTargets = new HashSet<Tile> { firstTarget };

        Tile currentTarget = firstTarget;
        int totalCalculatedDamage = 0;
        List<int> damagesPerTarget = new List<int>();

        for (int chain = 0; chain < MaxChains && currentTarget != null; chain++)
        {
            var damageable = currentTarget.GetDamageableTarget();
            if (damageable != null && damageable.IsAlive)
            {
                // 현재 체인 데미지 계산 (감쇠 적용)
                float damageMultiplier = Mathf.Pow(1 - DamageReductionPerChain, chain);
                int damage = Mathf.RoundToInt(CalculateDamage(currentTarget, context) * damageMultiplier);

                chainedTargets.Add(damageable.gameObject);
                damagesPerTarget.Add(damage);
                totalCalculatedDamage += damage;
            }

            // 다음 체인 타겟 찾기
            currentTarget = FindNextChainTarget(currentTarget, hitTargets, teamComponent, gridManager);

            if (currentTarget != null)
            {
                chainedTiles.Add(currentTarget);
                hitTargets.Add(currentTarget);
            }
        }

        // 2️⃣ 타겟을 ExecutionContext에 저장 (데미지 적용하지 않음!)
        context.SetCurrentAttackTargets(chainedTargets);
        context.SetParameter("DamagesPerTarget", damagesPerTarget); // 체인별 감쇠된 데미지 저장

        // 3️⃣ Result 반환 (애니메이션 정보만, 타겟 제외)
        return new AttackResult
        {
            // 비즈니스 로직 결과 (참고용)
            CalculatedDamage = totalCalculatedDamage,
            TargetCount = chainedTargets.Count,
            Success = chainedTargets.Count > 0,

            // 애니메이션 메타데이터 (AnimationController가 재생)
            AnimationType = AttackAnimationType.Chain,
            AnimationTargets = chainedTiles.Select(t => t.WorldPosition).ToArray(),
            AnimationDuration = 0.3f * chainedTargets.Count,
            AnimationParameters = new Dictionary<string, object>
            {
                { "ChainCount", chainedTargets.Count },
                { "DamageReduction", DamageReductionPerChain }
            },
            VFXPrefab = context.GetVFXPrefab("ChainLightning"),
            SFXClip = context.GetSFXClip("Thunder")
        };
    }

    private Tile FindNextChainTarget(Tile from, HashSet<Tile> alreadyHit,
                                      ITeamComponent attackerTeam, IGridManager gridManager)
    {
        var nearbyTiles = gridManager.GetTilesInRadius(from.GridPosition, ChainRange);

        foreach (var tile in nearbyTiles)
        {
            if (alreadyHit.Contains(tile)) continue;

            var damageable = tile.GetDamageableTarget();
            if (damageable == null) continue;

            var targetTeam = damageable.GetComponent<ITeamComponent>();
            if (targetTeam == null || targetTeam.Team == attackerTeam.Team) continue;

            return tile;
        }

        return null;
    }
}
```

#### DrainAttackStrategy (흡혈 공격)
```csharp
public class DrainAttackStrategy : IAttackStrategy
{
    public float LifeStealPercent { get; set; } = 0.5f; // 50% 흡혈

    public string StrategyName => "흡혈 공격";
    public AttackStrategyType StrategyType => AttackStrategyType.Drain;

    public AttackResult Execute(List<Tile> targetTiles, ExecutionContext context)
    {
        var target = targetTiles[0];
        var damage = CalculateDamage(target, context);

        // 데미지 적용
        var damageable = target.GetDamageableTarget();
        damageable?.TakeDamage(damage);

        // 흡혈 회복
        int healAmount = Mathf.RoundToInt(damage * LifeStealPercent);
        var healthComponent = context.Executor.GetComponent<IHealthComponent>();
        healthComponent?.Heal(healAmount);

        // 흡혈 VFX 재생
        PlayDrainVFX(target.WorldPosition, context.Executor.transform.position);

        return new AttackResult
        {
            DamageDealt = damage,
            TargetsHit = 1,
            HealingDone = healAmount,
            Success = true
        };
    }
}
```

### 3.3 IMovementStrategy (이동 전략 인터페이스)

```csharp
/// <summary>
/// 이동 행동 전략 인터페이스
/// ✅ AI가 GetValidPositions()로 가능한 이동 위치 목록을 얻음
/// ✅ AI가 최적 위치를 선택하여 Execute()에 전달
/// ✅ Strategy는 AI가 선택한 위치로 이동 실행 (방식은 전략에 따라 다름)
/// </summary>
public interface IMovementStrategy
{
    /// <summary>이동 실행 - AI가 선택한 목표 위치로 이동</summary>
    /// <param name="targetPosition">AI가 선택한 목표 위치</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>이동 결과 (애니메이션 메타데이터)</returns>
    MovementResult Execute(Vector2Int targetPosition, ExecutionContext context);

    /// <summary>
    /// 유효한 이동 가능 위치 가져오기 (AI 전용)
    /// ⚠️ AI가 DecideAction()에서 호출하여 이동 위치 선택에 사용
    /// </summary>
    /// <param name="fromPosition">현재 위치</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>이동 가능한 위치 리스트 (AI가 선택 가능한 옵션)</returns>
    List<Vector2Int> GetValidPositions(Vector2Int fromPosition, ExecutionContext context);

    /// <summary>이동 비용 계산 (UI 프리뷰용, AI 판단용)</summary>
    /// <param name="from">출발 위치</param>
    /// <param name="to">도착 위치</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>이동 비용 (이동력 소모량)</returns>
    int CalculateMovementCost(Vector2Int from, Vector2Int to, ExecutionContext context);

    /// <summary>전략 식별자</summary>
    string StrategyName { get; }

    /// <summary>전략 타입</summary>
    MovementStrategyType StrategyType { get; }
}
```

#### MovementResult (이동 결과 클래스)
```csharp
/// <summary>
/// 이동 전략 실행 결과 (비즈니스 로직 + 애니메이션 메타데이터)
/// Strategy는 이 객체에 애니메이션 정보를 포함하되, 직접 재생하지 않음
/// </summary>
public class MovementResult
{
    // ========== 비즈니스 로직 결과 ==========
    /// <summary>실행 성공 여부</summary>
    public bool Success { get; set; }

    /// <summary>최종 도착 위치</summary>
    public Vector2Int FinalPosition { get; set; }

    /// <summary>소모된 이동력</summary>
    public int MovementCostUsed { get; set; }

    /// <summary>결과 메시지</summary>
    public string Message { get; set; }

    /// <summary>에러 메시지 (실패 시)</summary>
    public string ErrorMessage { get; set; }

    // ========== 애니메이션 메타데이터 (AnimationController가 사용) ==========
    /// <summary>애니메이션 타입 (AnimationController가 사용)</summary>
    public MovementAnimationType AnimationType { get; set; }

    /// <summary>이동 경로 (순차적인 위치들)</summary>
    public Vector3[] PathPositions { get; set; }

    /// <summary>이동 속도</summary>
    public float MovementSpeed { get; set; }

    /// <summary>애니메이션 파라미터</summary>
    public Dictionary<string, object> AnimationParameters { get; set; }

    /// <summary>이동 트레일 VFX 프리팹</summary>
    public GameObject TrailVFX { get; set; }

    /// <summary>SFX 클립</summary>
    public AudioClip SFXClip { get; set; }
}

/// <summary>
/// 이동 애니메이션 타입 열거형
/// </summary>
public enum MovementAnimationType
{
    Default,        // 기본 걷기
    Teleport,       // 순간이동 (페이드아웃 → 페이드인)
    Jump,           // 점프 이동 (포물선)
    Dash,           // 돌진 (빠른 직선)
    Fly,            // 비행
    Phase           // 위상 이동 (벽 통과)
}
```

### 3.4 이동 전략 구현체

#### DefaultMovementStrategy (기본 그리드 이동)
```csharp
public class DefaultMovementStrategy : IMovementStrategy
{
    public string StrategyName => "기본 이동";
    public MovementStrategyType StrategyType => MovementStrategyType.Default;

    public MovementResult Execute(Vector2Int targetPosition, ExecutionContext context)
    {
        // 기존 MovementComponent 로직 사용
        var movementComponent = context.Executor.GetComponent<IMovementSystem>();
        return movementComponent.MoveTo(targetPosition);
    }

    public List<Vector2Int> GetValidPositions(Vector2Int fromPosition, ExecutionContext context)
    {
        var movementComponent = context.Executor.GetComponent<IMovementSystem>();
        return movementComponent.GetValidMovePositions();
    }

    public int CalculateMovementCost(Vector2Int from, Vector2Int to, ExecutionContext context)
    {
        // 맨해튼 거리 기반 비용
        return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
    }
}
```

#### TeleportMovementStrategy (순간이동)
```csharp
public class TeleportMovementStrategy : IMovementStrategy
{
    public bool IgnoresObstacles { get; set; } = true;
    public bool IgnoresOccupiedTiles { get; set; } = false;

    public string StrategyName => "순간이동";
    public MovementStrategyType StrategyType => MovementStrategyType.Teleport;

    public MovementResult Execute(Vector2Int targetPosition, ExecutionContext context)
    {
        var unit = context.Executor;
        var gridManager = context.GridManager;

        // 즉시 위치 변경 (경로 탐색 없음)
        var currentPos = gridManager.GetUnitPosition(unit.gameObject);
        var targetTile = gridManager.GetTileAt(targetPosition);

        if (targetTile == null)
        {
            return new MovementResult { Success = false, ErrorMessage = "Invalid position" };
        }

        if (!IgnoresOccupiedTiles && targetTile.IsOccupied)
        {
            return new MovementResult { Success = false, ErrorMessage = "Tile occupied" };
        }

        // ✅ 순수 비즈니스 로직만 수행: 위치 업데이트
        var startPos = gridManager.GridToWorldPosition(currentPos);
        var endPos = gridManager.GridToWorldPosition(targetPosition);

        gridManager.UpdateGridDataLayer(unit.gameObject, targetPosition);
        unit.transform.position = endPos;

        // ✅ 애니메이션 정보를 Result에 포함 (직접 재생하지 않음!)
        return new MovementResult
        {
            // 비즈니스 로직 결과
            Success = true,
            FinalPosition = targetPosition,
            MovementCostUsed = 0, // 순간이동은 이동력 소모 없음
            Message = "Teleported successfully",

            // 애니메이션 메타데이터 (AnimationController가 재생)
            AnimationType = MovementAnimationType.Teleport,
            PathPositions = new[] { startPos, endPos }, // 출발지 → 도착지
            MovementSpeed = 0f, // 즉시 이동
            AnimationParameters = new Dictionary<string, object>
            {
                { "FadeOutDuration", 0.3f },
                { "FadeInDuration", 0.3f },
                { "IgnoresObstacles", IgnoresObstacles }
            }
        };
    }

    public List<Vector2Int> GetValidPositions(Vector2Int fromPosition, ExecutionContext context)
    {
        var movementComponent = context.Executor.GetComponent<IMovementSystem>();
        var range = movementComponent.MovementRange;
        var gridManager = context.GridManager;

        // 범위 내 모든 위치 (장애물 무시)
        var positions = gridManager.GetPositionsInRange(fromPosition, range);

        if (!IgnoresOccupiedTiles)
        {
            // 점유된 타일 제외
            positions.RemoveAll(pos => gridManager.IsPositionOccupied(pos));
        }

        return positions;
    }

    public int CalculateMovementCost(Vector2Int from, Vector2Int to, ExecutionContext context)
    {
        return 0; // 순간이동은 비용 없음
    }
}
```

#### JumpMovementStrategy (점프 이동)
```csharp
public class JumpMovementStrategy : IMovementStrategy
{
    public bool CanJumpOverUnits { get; set; } = true;
    public bool CanJumpOverObstacles { get; set; } = true;

    public string StrategyName => "점프 이동";
    public MovementStrategyType StrategyType => MovementStrategyType.Jump;

    public MovementResult Execute(Vector2Int targetPosition, ExecutionContext context)
    {
        // 점프 애니메이션과 함께 이동
        // 경로 상의 장애물 무시
        // 포물선 이동 경로

        // 구현 내용...
    }
}
```

### 3.5 IDetectionStrategy (탐색 전략 인터페이스)

```csharp
/// <summary>
/// 탐색/감지 전략 인터페이스
/// </summary>
public interface IDetectionStrategy
{
    /// <summary>탐지 가능한 대상 가져오기</summary>
    /// <param name="fromPosition">탐지자 위치</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>탐지된 유닛 리스트</returns>
    List<Unit> GetDetectableTargets(Vector2Int fromPosition, ExecutionContext context);

    /// <summary>특정 대상 탐지 가능 여부</summary>
    /// <param name="target">대상 유닛</param>
    /// <param name="fromPosition">탐지자 위치</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>탐지 가능 여부</returns>
    bool CanDetect(Unit target, Vector2Int fromPosition, ExecutionContext context);

    /// <summary>탐지 범위 가져오기</summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>탐지 범위 (타일 수)</returns>
    int GetDetectionRange(ExecutionContext context);

    /// <summary>전략 식별자</summary>
    string StrategyName { get; }

    /// <summary>전략 타입</summary>
    DetectionStrategyType StrategyType { get; }
}
```

### 3.6 탐색 전략 구현체

#### DefaultDetectionStrategy (기본 시야)
```csharp
public class DefaultDetectionStrategy : IDetectionStrategy
{
    public string StrategyName => "기본 시야";
    public DetectionStrategyType StrategyType => DetectionStrategyType.Default;

    public List<Unit> GetDetectableTargets(Vector2Int fromPosition, ExecutionContext context)
    {
        var range = GetDetectionRange(context);
        var gridManager = context.GridManager;
        var teamComponent = context.Executor.GetComponent<ITeamComponent>();

        var positions = gridManager.GetPositionsInRange(fromPosition, range);
        var detectableUnits = new List<Unit>();

        foreach (var pos in positions)
        {
            var tile = gridManager.GetTileAt(pos);
            if (tile == null || !tile.IsOccupied) continue;

            var unit = tile.OccupyingUnit;
            if (unit == null) continue;

            // 적 팀만 탐지
            var targetTeam = unit.GetComponent<ITeamComponent>();
            if (targetTeam != null && targetTeam.Team != teamComponent.Team)
            {
                detectableUnits.Add(unit);
            }
        }

        return detectableUnits;
    }

    public bool CanDetect(Unit target, Vector2Int fromPosition, ExecutionContext context)
    {
        var range = GetDetectionRange(context);
        var targetPos = context.GridManager.GetUnitPosition(target.gameObject);
        var distance = Vector2Int.Distance(fromPosition, targetPos);

        return distance <= range;
    }

    public int GetDetectionRange(ExecutionContext context)
    {
        // 공격 범위와 동일하게 설정
        var combatComponent = context.Executor.GetComponent<ICombatSystem>();
        return combatComponent?.AttackRange ?? 3;
    }
}
```

#### StealthDetectionStrategy (은신 감지)
```csharp
public class StealthDetectionStrategy : IDetectionStrategy
{
    public bool CanDetectInvisible { get; set; } = true;
    public bool CanDetectStealthed { get; set; } = true;
    public int BonusDetectionRange { get; set; } = 2;

    public string StrategyName => "은신 감지";
    public DetectionStrategyType StrategyType => DetectionStrategyType.StealthDetection;

    public List<Unit> GetDetectableTargets(Vector2Int fromPosition, ExecutionContext context)
    {
        var range = GetDetectionRange(context);
        var gridManager = context.GridManager;
        var teamComponent = context.Executor.GetComponent<ITeamComponent>();

        var positions = gridManager.GetPositionsInRange(fromPosition, range);
        var detectableUnits = new List<Unit>();

        foreach (var pos in positions)
        {
            var tile = gridManager.GetTileAt(pos);
            if (tile == null || !tile.IsOccupied) continue;

            var unit = tile.OccupyingUnit;
            if (unit == null) continue;

            // 적 팀 체크
            var targetTeam = unit.GetComponent<ITeamComponent>();
            if (targetTeam == null || targetTeam.Team == teamComponent.Team) continue;

            // 은신 상태 체크 (이 전략은 은신 유닛도 탐지 가능)
            detectableUnits.Add(unit);
        }

        return detectableUnits;
    }

    public int GetDetectionRange(ExecutionContext context)
    {
        var combatComponent = context.Executor.GetComponent<ICombatSystem>();
        return (combatComponent?.AttackRange ?? 3) + BonusDetectionRange;
    }
}
```

---

## 4. ScriptableObject 데이터 계층

### 4.1 TraitData (특성 정의)

```csharp
using UnityEngine;

/// <summary>
/// 특성 데이터 정의 (디자이너가 편집 가능)
/// </summary>
[CreateAssetMenu(fileName = "New Trait", menuName = "Game/Trait/Trait Data", order = 1)]
public class TraitData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string traitName = "New Trait";
    [SerializeField] [TextArea(3, 5)] private string description = "";
    [SerializeField] private Sprite icon;

    [Header("활성화 조건")]
    [Tooltip("모든 조건이 true여야 특성 활성화")]
    [SerializeField] private TraitConditionData[] activationConditions;

    [Header("전략 오버라이드")]
    [Tooltip("조건 충족 시 공격 방식 변경")]
    [SerializeField] private AttackStrategyType attackStrategyOverride = AttackStrategyType.None;

    [Tooltip("조건 충족 시 이동 방식 변경")]
    [SerializeField] private MovementStrategyType movementStrategyOverride = MovementStrategyType.None;

    [Tooltip("조건 충족 시 탐색 방식 변경")]
    [SerializeField] private DetectionStrategyType detectionStrategyOverride = DetectionStrategyType.None;

    [Header("실행 설정")]
    [Tooltip("우선순위 (낮을수록 먼저 실행)")]
    [SerializeField] [Range(0, 100)] private int executionPriority = 50;

    [Tooltip("어떤 이벤트에서 실행될지")]
    [SerializeField] private TriggerType[] triggerOn;

    [Header("시각/사운드 효과")]
    [SerializeField] private GameObject activationVFX;
    [SerializeField] private AudioClip activationSFX;

    // Properties
    public string TraitName => traitName;
    public string Description => description;
    public Sprite Icon => icon;
    public TraitConditionData[] ActivationConditions => activationConditions;
    public AttackStrategyType AttackStrategyOverride => attackStrategyOverride;
    public MovementStrategyType MovementStrategyOverride => movementStrategyOverride;
    public DetectionStrategyType DetectionStrategyOverride => detectionStrategyOverride;
    public int ExecutionPriority => executionPriority;
    public TriggerType[] TriggerOn => triggerOn;
    public GameObject ActivationVFX => activationVFX;
    public AudioClip ActivationSFX => activationSFX;

    /// <summary>특정 트리거에 반응하는지 확인</summary>
    public bool HasTrigger(TriggerType trigger)
    {
        return triggerOn != null && System.Array.Exists(triggerOn, t => t == trigger);
    }

    /// <summary>데이터 유효성 검증</summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(traitName))
        {
            Debug.LogError($"TraitData: Name is empty", this);
            return false;
        }

        if (activationConditions == null || activationConditions.Length == 0)
        {
            Debug.LogWarning($"TraitData [{traitName}]: No activation conditions defined", this);
        }

        return true;
    }
}
```

### 4.2 TraitConditionData (조건 정의)

```csharp
using UnityEngine;

/// <summary>
/// 특성 활성화 조건 데이터 (단순화된 버전)
/// </summary>
[CreateAssetMenu(fileName = "New Condition", menuName = "Game/Trait/Condition Data", order = 2)]
public class TraitConditionData : ScriptableObject
{
    [Header("조건 타입")]
    [Tooltip("평가할 조건의 종류")]
    [SerializeField] private ConditionType type;

    [Header("추가 옵션")]
    [Tooltip("조건 결과 반전 (NOT 연산)")]
    [SerializeField] private bool invert = false;

    [Tooltip("조건 설명 (디버그용)")]
    [SerializeField] [TextArea(2, 3)] private string conditionDescription;

    // Properties
    public ConditionType Type => type;
    public bool Invert => invert;
    public string ConditionDescription => conditionDescription;

    /// <summary>조건을 사람이 읽을 수 있는 형태로 변환</summary>
    public string ToReadableString()
    {
        string condition = Type switch
        {
            ConditionType.NoEnemyInRow => "가로줄에 적군이 없는 경우",
            ConditionType.UnitDistanceExceedsMovementRange => "앞의 유닛까지 거리가 이동 범위보다 먼 경우",
            ConditionType.OnCardPlaced => "카드 처음 배치 시",
            _ => "알 수 없는 조건"
        };

        if (Invert)
            condition = $"NOT ({condition})";

        return condition;
    }
}

/// <summary>조건 타입 열거형 (단순화)</summary>
public enum ConditionType
{
    NoEnemyInRow,                       // 가로줄에 적군이 없는 경우
    UnitDistanceExceedsMovementRange,   // 앞의 적군/아군까지 거리가 기본 최대 이동거리보다 먼 경우
    OnCardPlaced                        // 세트 시: 카드 처음 배치하는 경우
    // OnTurnEnd는 TriggerType으로 처리됨
}
```

### 4.3 트리거 타입 열거형

```csharp
/// <summary>트리거 타입 열거형</summary>
public enum TriggerType
{
    Passive,                // 패시브 (항상 활성)
    OnBeforeAttack,         // 공격 전
    OnAfterAttack,          // 공격 후
    OnBeforeMove,           // 이동 전
    OnAfterMove,            // 이동 후
    OnDamageTaken,          // 피격 시
    OnDealDamage,           // 데미지 적용 시
    OnKill,                 // 적 처치 시
    OnDeath,                // 사망 시
    OnTurnStart,            // 턴 시작 시
    OnTurnEnd,              // 턴 종료 시
    OnHealthThreshold,      // 체력 임계값 도달 시
    OnTargetInRange,        // 적이 범위 내 진입 시
    Manual                  // 수동 발동
}
```

### 4.4 전략 타입 열거형

```csharp
/// <summary>공격 전략 타입</summary>
public enum AttackStrategyType
{
    None,                   // 오버라이드 없음
    Default,                // 기본 단일 공격
    AoE,                    // 범위 공격
    Chain,                  // 연쇄 공격
    Drain,                  // 흡혈 공격
    Pierce,                 // 관통 공격
    Splash,                 // 스플래시 공격
    Custom                  // 커스텀 전략
}

/// <summary>이동 전략 타입</summary>
public enum MovementStrategyType
{
    None,                   // 오버라이드 없음
    Default,                // 기본 그리드 이동
    Teleport,               // 순간이동
    Jump,                   // 점프 이동
    Phase,                  // 위상 이동 (벽 통과)
    Charge,                 // 돌진 이동
    Custom                  // 커스텀 전략
}

/// <summary>탐색 전략 타입</summary>
public enum DetectionStrategyType
{
    None,                   // 오버라이드 없음
    Default,                // 기본 시야
    StealthDetection,       // 은신 감지
    ExtendedVision,         // 확장 시야
    Blind,                  // 시야 감소/상실
    Omniscient,             // 전체 맵 시야
    Custom                  // 커스텀 전략
}
```

---

## 5. Executor 실행 계층

### 5.1 TraitExecutor (실행 오케스트레이터)

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 특성 실행 파이프라인 오케스트레이터
/// 조건 평가, 실행, 쿨다운 관리 담당
/// </summary>
public class TraitExecutor
{
    private readonly ConditionEvaluator conditionEvaluator;
    private readonly StrategyFactory strategyFactory;

    public TraitExecutor()
    {
        conditionEvaluator = new ConditionEvaluator();
        strategyFactory = new StrategyFactory();
    }

    /// <summary>
    /// 특성 실행 가능 여부 판단
    /// </summary>
    /// <param name="trait">검사할 특성 인스턴스</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>실행 가능 여부</returns>
    public bool CanExecute(TraitInstance trait, ExecutionContext context)
    {
        // 활성화 조건 체크
        var conditions = trait.Data.ActivationConditions;
        if (conditions == null || conditions.Length == 0)
        {
            // 조건이 없으면 항상 실행 가능
            return true;
        }

        // 모든 조건 평가 (AND 연산)
        foreach (var condition in conditions)
        {
            if (!conditionEvaluator.Evaluate(condition, context))
            {
                Debug.Log($"[TraitExecutor] Trait [{trait.Data.TraitName}] condition failed: {condition.ToReadableString()}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 특성 실행
    /// </summary>
    /// <param name="trait">실행할 특성 인스턴스</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>실행 결과</returns>
    public ExecutionResult Execute(TraitInstance trait, ExecutionContext context)
    {
        Debug.Log($"[TraitExecutor] Executing trait: {trait.Data.TraitName}");

        var result = new ExecutionResult
        {
            TraitName = trait.Data.TraitName,
            Success = false
        };

        try
        {
            // 1. 전략 오버라이드 적용
            ApplyStrategyOverrides(trait, context);

            // 2. 활성화 VFX/SFX 재생
            PlayActivationFeedback(trait, context);

            // 3. 특성 상태 업데이트
            trait.OnExecuted();

            result.Success = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TraitExecutor] Error executing trait [{trait.Data.TraitName}]: {ex.Message}");
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>전략 오버라이드 적용</summary>
    private void ApplyStrategyOverrides(TraitInstance trait, ExecutionContext context)
    {
        var data = trait.Data;

        // 공격 전략 오버라이드
        if (data.AttackStrategyOverride != AttackStrategyType.None)
        {
            var strategy = strategyFactory.CreateAttackStrategy(data.AttackStrategyOverride);
            if (strategy != null)
            {
                trait.ApplyAttackStrategy(strategy);
                Debug.Log($"[TraitExecutor] Applied attack strategy: {strategy.StrategyName}");
            }
        }

        // 이동 전략 오버라이드
        if (data.MovementStrategyOverride != MovementStrategyType.None)
        {
            var strategy = strategyFactory.CreateMovementStrategy(data.MovementStrategyOverride);
            if (strategy != null)
            {
                trait.ApplyMovementStrategy(strategy);
                Debug.Log($"[TraitExecutor] Applied movement strategy: {strategy.StrategyName}");
            }
        }

        // 탐색 전략 오버라이드
        if (data.DetectionStrategyOverride != DetectionStrategyType.None)
        {
            var strategy = strategyFactory.CreateDetectionStrategy(data.DetectionStrategyOverride);
            if (strategy != null)
            {
                trait.ApplyDetectionStrategy(strategy);
                Debug.Log($"[TraitExecutor] Applied detection strategy: {strategy.StrategyName}");
            }
        }
    }

    /// <summary>활성화 피드백 재생</summary>
    private void PlayActivationFeedback(TraitInstance trait, ExecutionContext context)
    {
        if (trait.Data.ActivationVFX != null)
        {
            Object.Instantiate(
                trait.Data.ActivationVFX,
                context.Executor.transform.position,
                Quaternion.identity
            );
        }

        if (trait.Data.ActivationSFX != null)
        {
            // AudioSource 재생 로직
            // ServiceLocator.Get<IAudioManager>()?.PlaySFX(trait.Data.ActivationSFX);
        }
    }
}

/// <summary>실행 결과</summary>
public class ExecutionResult
{
    public string TraitName { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}
```

### 5.2 ConditionEvaluator (조건 평가자)

```csharp
using UnityEngine;

/// <summary>
/// 특성 조건 평가 유틸리티 (단순화된 버전)
/// </summary>
public class ConditionEvaluator
{
    /// <summary>
    /// 조건 평가
    /// </summary>
    /// <param name="condition">평가할 조건</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>조건 충족 여부</returns>
    public bool Evaluate(TraitConditionData condition, ExecutionContext context)
    {
        if (condition == null)
        {
            Debug.LogWarning("[ConditionEvaluator] Null condition provided");
            return false;
        }

        bool result = condition.Type switch
        {
            ConditionType.NoEnemyInRow => EvaluateNoEnemyInRow(context),
            ConditionType.UnitDistanceExceedsMovementRange => EvaluateUnitDistanceExceedsMovementRange(context),
            ConditionType.OnCardPlaced => EvaluateOnCardPlaced(context),
            _ => false
        };

        // Invert 플래그 처리
        if (condition.Invert)
        {
            result = !result;
        }

        return result;
    }

    /// <summary>가로줄에 적군이 없는 경우</summary>
    private bool EvaluateNoEnemyInRow(ExecutionContext context)
    {
        var gridManager = context.GridManager;
        var teamComponent = context.Executor.GetComponent<ITeamComponent>();

        if (gridManager == null || teamComponent == null) return false;

        // 현재 유닛의 위치 가져오기
        var currentPos = gridManager.GetUnitPosition(context.Executor.gameObject);

        // 같은 행(row)의 모든 타일 가져오기
        var tilesInRow = gridManager.GetTilesInRow(currentPos.y);

        // 같은 행에 적군이 있는지 확인
        foreach (var tile in tilesInRow)
        {
            if (!tile.IsOccupied) continue;

            var occupyingUnit = tile.OccupyingUnit;
            if (occupyingUnit == null) continue;

            var targetTeam = occupyingUnit.GetComponent<ITeamComponent>();
            if (targetTeam != null && targetTeam.Team != teamComponent.Team)
            {
                // 적군 발견
                return false;
            }
        }

        // 적군이 없음
        return true;
    }

    /// <summary>앞의 유닛까지 거리가 기본 최대 이동거리보다 먼 경우</summary>
    private bool EvaluateUnitDistanceExceedsMovementRange(ExecutionContext context)
    {
        var gridManager = context.GridManager;
        var movementComponent = context.Executor.GetComponent<IMovementSystem>();

        if (gridManager == null || movementComponent == null) return false;

        var currentPos = gridManager.GetUnitPosition(context.Executor.gameObject);
        int maxMovementRange = movementComponent.MovementRange;

        // 앞쪽 방향으로 가장 가까운 유닛 찾기
        var nearestUnitDistance = FindNearestUnitInFront(currentPos, gridManager, context.Executor.gameObject);

        // 유닛이 없으면 true (거리가 무한대로 간주)
        if (nearestUnitDistance < 0) return true;

        // 거리가 이동 범위보다 크면 true
        return nearestUnitDistance > maxMovementRange;
    }

    /// <summary>앞쪽에서 가장 가까운 유닛까지의 거리 찾기</summary>
    private int FindNearestUnitInFront(Vector2Int currentPos, IGridManager gridManager, GameObject self)
    {
        int minDistance = -1;

        // 앞쪽(x축 양의 방향)으로 탐색
        for (int x = currentPos.x + 1; x < gridManager.GridWidth; x++)
        {
            var tile = gridManager.GetTileAt(new Vector2Int(x, currentPos.y));
            if (tile != null && tile.IsOccupied && tile.OccupyingUnit != null && tile.OccupyingUnit.gameObject != self)
            {
                minDistance = x - currentPos.x;
                break;
            }
        }

        return minDistance;
    }

    /// <summary>카드 처음 배치 시 (세트 시)</summary>
    private bool EvaluateOnCardPlaced(ExecutionContext context)
    {
        // ExecutionContext의 파라미터에서 확인
        return context.GetParameter<bool>("IsCardPlaced", false);
    }
}
```

### 5.3 TraitInstance (런타임 특성 상태)

```csharp
using UnityEngine;

/// <summary>
/// 특성 런타임 인스턴스 (상태 관리)
/// </summary>
public class TraitInstance
{
    // 데이터 참조
    private readonly TraitData data;
    private readonly Unit owner;

    // 런타임 상태
    private bool isActive;
    private int timesTriggered;

    // 적용된 전략
    private IAttackStrategy appliedAttackStrategy;
    private IMovementStrategy appliedMovementStrategy;
    private IDetectionStrategy appliedDetectionStrategy;

    // Properties
    public TraitData Data => data;
    public Unit Owner => owner;
    public bool IsActive => isActive;
    public int TimesTriggered => timesTriggered;
    public IAttackStrategy AppliedAttackStrategy => appliedAttackStrategy;
    public IMovementStrategy AppliedMovementStrategy => appliedMovementStrategy;
    public IDetectionStrategy AppliedDetectionStrategy => appliedDetectionStrategy;

    /// <summary>생성자</summary>
    public TraitInstance(TraitData data, Unit owner)
    {
        this.data = data ?? throw new System.ArgumentNullException(nameof(data));
        this.owner = owner ?? throw new System.ArgumentNullException(nameof(owner));

        isActive = false;
        timesTriggered = 0;
    }

    /// <summary>특성 활성화</summary>
    public void Activate()
    {
        if (isActive)
        {
            Debug.LogWarning($"[TraitInstance] Trait [{data.TraitName}] is already active");
            return;
        }

        isActive = true;
        Debug.Log($"[TraitInstance] Trait [{data.TraitName}] activated");
    }

    /// <summary>특성 비활성화</summary>
    public void Deactivate()
    {
        if (!isActive)
        {
            Debug.LogWarning($"[TraitInstance] Trait [{data.TraitName}] is already inactive");
            return;
        }

        isActive = false;

        // 적용된 전략 제거
        appliedAttackStrategy = null;
        appliedMovementStrategy = null;
        appliedDetectionStrategy = null;

        Debug.Log($"[TraitInstance] Trait [{data.TraitName}] deactivated");
    }

    /// <summary>실행 완료 콜백</summary>
    public void OnExecuted()
    {
        timesTriggered++;
        Debug.Log($"[TraitInstance] Trait [{data.TraitName}] executed (Total: {timesTriggered} times)");
    }

    /// <summary>공격 전략 적용</summary>
    public void ApplyAttackStrategy(IAttackStrategy strategy)
    {
        appliedAttackStrategy = strategy;
    }

    /// <summary>이동 전략 적용</summary>
    public void ApplyMovementStrategy(IMovementStrategy strategy)
    {
        appliedMovementStrategy = strategy;
    }

    /// <summary>탐색 전략 적용</summary>
    public void ApplyDetectionStrategy(IDetectionStrategy strategy)
    {
        appliedDetectionStrategy = strategy;
    }

    /// <summary>특성 리셋 (턴 종료 시 등)</summary>
    public void Reset()
    {
        // 필요에 따라 상태 초기화
        // 예: 턴당 1회만 사용 가능한 특성의 경우
    }
}
```

### 5.4 ExecutionContext (실행 컨텍스트)

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특성 실행 컨텍스트 (실행 환경 데이터)
/// ✅ Strategy가 계산한 타겟을 저장하여 CombatComponent가 나중에 사용
/// </summary>
public class ExecutionContext
{
    // 핵심 참조
    public Unit Executor { get; private set; }
    public Tile TargetTile { get; private set; }
    public IGridManager GridManager { get; private set; }
    public GameState GameState { get; private set; }

    // 추가 컨텍스트 파라미터
    public Dictionary<string, object> Parameters { get; private set; }

    // 🆕 공격 타겟 저장 (Strategy → CombatComponent 데이터 전달)
    private List<GameObject> currentAttackTargets;

    /// <summary>생성자</summary>
    public ExecutionContext(
        Unit executor,
        Tile targetTile,
        IGridManager gridManager,
        GameState gameState = null)
    {
        Executor = executor ?? throw new System.ArgumentNullException(nameof(executor));
        TargetTile = targetTile;
        GridManager = gridManager ?? ServiceLocator.Get<IGridManager>();
        GameState = gameState ?? ServiceLocator.Get<GameState>();
        Parameters = new Dictionary<string, object>();
        currentAttackTargets = new List<GameObject>();
    }

    // ========== 공격 타겟 관리 (🆕 추가) ==========

    /// <summary>
    /// Strategy가 계산한 공격 타겟 저장
    /// CombatComponent가 OnAttackHit에서 이 타겟들에게 데미지 적용
    /// </summary>
    public void SetCurrentAttackTargets(List<GameObject> targets)
    {
        currentAttackTargets = targets ?? new List<GameObject>();
    }

    /// <summary>
    /// 저장된 공격 타겟 가져오기
    /// CombatComponent가 OnAttackHit 콜백에서 호출
    /// </summary>
    public List<GameObject> GetCurrentAttackTargets()
    {
        // 유효한 타겟만 반환 (null이거나 파괴된 GameObject 제외)
        return currentAttackTargets.FindAll(t => t != null);
    }

    /// <summary>
    /// 공격 타겟 초기화 (공격 완료 후)
    /// </summary>
    public void ClearAttackTargets()
    {
        currentAttackTargets.Clear();
    }

    // ========== 파라미터 관리 ==========

    /// <summary>파라미터 추가</summary>
    public void SetParameter(string key, object value)
    {
        Parameters[key] = value;
    }

    /// <summary>파라미터 가져오기</summary>
    public T GetParameter<T>(string key, T defaultValue = default)
    {
        if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return defaultValue;
    }

    /// <summary>파라미터 존재 여부</summary>
    public bool HasParameter(string key)
    {
        return Parameters.ContainsKey(key);
    }

    // ========== 리소스 헬퍼 ==========

    /// <summary>
    /// VFX 프리팹 가져오기 헬퍼 메서드
    /// TraitData 또는 중앙 VFX 관리자에서 VFX 프리팹을 가져옴
    /// </summary>
    public GameObject GetVFXPrefab(string vfxName)
    {
        // Option 1: TraitData에서 가져오기
        // Option 2: VFXManager 같은 중앙화된 리소스 관리자 사용
        // Option 3: Resources 폴더에서 로드
        return Resources.Load<GameObject>($"VFX/{vfxName}");
    }

    /// <summary>
    /// SFX 클립 가져오기 헬퍼 메서드
    /// </summary>
    public AudioClip GetSFXClip(string sfxName)
    {
        return Resources.Load<AudioClip>($"SFX/{sfxName}");
    }
}
```

### 5.5 StrategyFactory (전략 팩토리)

```csharp
using UnityEngine;

/// <summary>
/// 전략 객체 생성 팩토리
/// </summary>
public class StrategyFactory
{
    /// <summary>공격 전략 생성</summary>
    public IAttackStrategy CreateAttackStrategy(AttackStrategyType type)
    {
        return type switch
        {
            AttackStrategyType.Default => new DefaultAttackStrategy(),
            AttackStrategyType.AoE => new AoEAttackStrategy(),
            AttackStrategyType.Chain => new ChainAttackStrategy(),
            AttackStrategyType.Drain => new DrainAttackStrategy(),
            AttackStrategyType.Pierce => new PierceAttackStrategy(),
            AttackStrategyType.Splash => new SplashAttackStrategy(),
            _ => null
        };
    }

    /// <summary>이동 전략 생성</summary>
    public IMovementStrategy CreateMovementStrategy(MovementStrategyType type)
    {
        return type switch
        {
            MovementStrategyType.Default => new DefaultMovementStrategy(),
            MovementStrategyType.Teleport => new TeleportMovementStrategy(),
            MovementStrategyType.Jump => new JumpMovementStrategy(),
            MovementStrategyType.Phase => new PhaseMovementStrategy(),
            MovementStrategyType.Charge => new ChargeMovementStrategy(),
            _ => null
        };
    }

    /// <summary>탐색 전략 생성</summary>
    public IDetectionStrategy CreateDetectionStrategy(DetectionStrategyType type)
    {
        return type switch
        {
            DetectionStrategyType.Default => new DefaultDetectionStrategy(),
            DetectionStrategyType.StealthDetection => new StealthDetectionStrategy(),
            DetectionStrategyType.ExtendedVision => new ExtendedVisionStrategy(),
            DetectionStrategyType.Blind => new BlindStrategy(),
            DetectionStrategyType.Omniscient => new OmniscientStrategy(),
            _ => null
        };
    }
}
```

---

## 6. 기존 시스템 통합

### 6.1 TraitComponent (유닛 특성 컨테이너)

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 유닛의 특성 관리 컴포넌트
/// </summary>
public class TraitComponent : MonoBehaviour
{
    [Header("초기 특성")]
    [SerializeField] private TraitData[] initialTraits;

    // 특성 인스턴스 목록
    private List<TraitInstance> traits = new List<TraitInstance>();

    // Executor
    private TraitExecutor executor;

    // 현재 활성 전략
    private IAttackStrategy currentAttackStrategy;
    private IMovementStrategy currentMovementStrategy;
    private IDetectionStrategy currentDetectionStrategy;

    // 기본 전략 (폴백)
    private IAttackStrategy defaultAttackStrategy;
    private IMovementStrategy defaultMovementStrategy;
    private IDetectionStrategy defaultDetectionStrategy;

    // 캐시
    private Unit unit;
    private IGridManager gridManager;

    private void Awake()
    {
        unit = GetComponent<Unit>();
        gridManager = ServiceLocator.Get<IGridManager>();

        // Executor 초기화
        executor = new TraitExecutor();

        // 기본 전략 설정
        defaultAttackStrategy = new DefaultAttackStrategy();
        defaultMovementStrategy = new DefaultMovementStrategy();
        defaultDetectionStrategy = new DefaultDetectionStrategy();

        currentAttackStrategy = defaultAttackStrategy;
        currentMovementStrategy = defaultMovementStrategy;
        currentDetectionStrategy = defaultDetectionStrategy;
    }

    private void Start()
    {
        // 초기 특성 로드
        if (initialTraits != null && initialTraits.Length > 0)
        {
            Initialize(initialTraits);
        }
    }

    /// <summary>특성 초기화</summary>
    public void Initialize(TraitData[] traitDataArray)
    {
        traits.Clear();

        foreach (var traitData in traitDataArray)
        {
            if (traitData == null || !traitData.IsValid())
            {
                Debug.LogWarning($"[TraitComponent] Invalid trait data, skipping");
                continue;
            }

            var traitInstance = new TraitInstance(traitData, unit);
            traits.Add(traitInstance);

            Debug.Log($"[TraitComponent] Trait [{traitData.TraitName}] initialized");
        }
    }

    /// <summary>트리거 기반 특성 평가 및 실행</summary>
    public void EvaluateAndExecuteTraits(TriggerType trigger, ExecutionContext context)
    {
        // 해당 트리거에 반응하는 특성 필터링
        var eligibleTraits = traits
            .Where(t => t.Data.HasTrigger(trigger))
            .Where(t => executor.CanExecute(t, context))
            .OrderBy(t => t.Data.ExecutionPriority); // 우선순위 순 정렬

        foreach (var trait in eligibleTraits)
        {
            var result = executor.Execute(trait, context);

            if (result.Success)
            {
                Debug.Log($"[TraitComponent] Trait [{trait.Data.TraitName}] executed successfully");
            }
            else
            {
                Debug.LogError($"[TraitComponent] Trait [{trait.Data.TraitName}] execution failed: {result.ErrorMessage}");
            }
        }
    }

    /// <summary>활성 공격 전략 가져오기 (조건부 오버라이드 포함)</summary>
    public IAttackStrategy GetActiveAttackStrategy()
    {
        // 활성화된 특성 중 공격 전략 오버라이드가 있는지 확인
        var overrideTrait = traits
            .Where(t => t.IsActive && t.AppliedAttackStrategy != null)
            .OrderBy(t => t.Data.ExecutionPriority)
            .FirstOrDefault();

        if (overrideTrait != null)
        {
            return overrideTrait.AppliedAttackStrategy;
        }

        return currentAttackStrategy;
    }

    /// <summary>활성 이동 전략 가져오기</summary>
    public IMovementStrategy GetActiveMovementStrategy()
    {
        var overrideTrait = traits
            .Where(t => t.IsActive && t.AppliedMovementStrategy != null)
            .OrderBy(t => t.Data.ExecutionPriority)
            .FirstOrDefault();

        if (overrideTrait != null)
        {
            return overrideTrait.AppliedMovementStrategy;
        }

        return currentMovementStrategy;
    }

    /// <summary>활성 탐색 전략 가져오기</summary>
    public IDetectionStrategy GetActiveDetectionStrategy()
    {
        var overrideTrait = traits
            .Where(t => t.IsActive && t.AppliedDetectionStrategy != null)
            .OrderBy(t => t.Data.ExecutionPriority)
            .FirstOrDefault();

        if (overrideTrait != null)
        {
            return overrideTrait.AppliedDetectionStrategy;
        }

        return currentDetectionStrategy;
    }

    /// <summary>특성 추가</summary>
    public void AddTrait(TraitData traitData)
    {
        if (traitData == null || !traitData.IsValid())
        {
            Debug.LogWarning("[TraitComponent] Cannot add invalid trait");
            return;
        }

        // 중복 체크
        if (traits.Any(t => t.Data == traitData))
        {
            Debug.LogWarning($"[TraitComponent] Trait [{traitData.TraitName}] already exists");
            return;
        }

        var traitInstance = new TraitInstance(traitData, unit);
        traits.Add(traitInstance);

        Debug.Log($"[TraitComponent] Trait [{traitData.TraitName}] added");
    }

    /// <summary>특성 제거</summary>
    public void RemoveTrait(TraitData traitData)
    {
        var trait = traits.FirstOrDefault(t => t.Data == traitData);
        if (trait == null)
        {
            Debug.LogWarning($"[TraitComponent] Trait [{traitData.TraitName}] not found");
            return;
        }

        trait.Deactivate();
        traits.Remove(trait);

        Debug.Log($"[TraitComponent] Trait [{traitData.TraitName}] removed");
    }

    /// <summary>모든 특성 가져오기</summary>
    public IReadOnlyList<TraitInstance> GetAllTraits()
    {
        return traits.AsReadOnly();
    }

    /// <summary>특성 존재 여부 확인</summary>
    public bool HasTrait(TraitData traitData)
    {
        return traits.Any(t => t.Data == traitData);
    }
}
```

### 6.2 CombatComponent 수정

```csharp
// CombatComponent.cs (기존 파일 수정)

public class CombatComponent : MonoBehaviour, ICombatSystem
{
    // ... 기존 필드 유지 ...

    // 추가: TraitComponent 참조
    private TraitComponent traitComponent;

    private void Awake()
    {
        // ... 기존 초기화 코드 ...

        // TraitComponent 참조
        traitComponent = GetComponent<TraitComponent>();
    }

    /// <summary>
    /// 타일 공격 (수정됨: 특성 통합)
    /// ⚠️ AI가 이미 타겟 타일을 선택한 상태로 호출
    /// </summary>
    public int AttackTiles(List<Tile> targetTiles, bool isSpecialAttack = false, bool forceCritical = false)
    {
        if (targetTiles == null || targetTiles.Count == 0)
        {
            Debug.LogWarning("[CombatComponent] No target tiles provided");
            return 0;
        }

        // 1. 활성 공격 전략 가져오기
        IAttackStrategy strategy = traitComponent?.GetActiveAttackStrategy()
                                   ?? new DefaultAttackStrategy();

        // 2. 실행 컨텍스트 생성 (AI가 선택한 Primary 타겟)
        var primaryTarget = targetTiles[0]; // AI가 선택한 타겟
        var context = new ExecutionContext(
            executor: GetComponent<Unit>(),
            targetTile: primaryTarget,
            gridManager: ServiceLocator.Get<IGridManager>()
        );

        // 특수 공격 여부 컨텍스트에 추가
        context.SetParameter("IsSpecialAttack", isSpecialAttack);
        context.SetParameter("ForceCritical", forceCritical);

        // 3. OnBeforeAttack 특성 실행
        traitComponent?.EvaluateAndExecuteTraits(TriggerType.OnBeforeAttack, context);

        // 4. 전략으로 공격 실행 (Primary 타겟만 전달, 추가 타겟은 Strategy가 계산)
        var result = strategy.Execute(primaryTarget, context);

        // 5. AnimationController에 애니메이션 재생 요청
        animationController?.PlayAttackAnimation(result);

        // 6. OnAfterAttack 특성 실행
        traitComponent?.EvaluateAndExecuteTraits(TriggerType.OnAfterAttack, context);

        // 7. 공격 완료 처리
        hasAttackedThisTurn = true;
        OnAttackCompleted?.Invoke(result.CalculatedDamage);

        return result.CalculatedDamage;
    }

    /// <summary>공격 가능 타일 가져오기 (수정됨: 전략 고려)</summary>
    public List<Tile> GetAttackRangeTiles(Vector2Int fromPosition)
    {
        // 활성 공격 전략 가져오기
        IAttackStrategy strategy = traitComponent?.GetActiveAttackStrategy()
                                   ?? new DefaultAttackStrategy();

        var context = new ExecutionContext(
            executor: GetComponent<Unit>(),
            targetTile: null,
            gridManager: ServiceLocator.Get<IGridManager>()
        );

        return strategy.GetValidTargets(fromPosition, context);
    }

    // ... 기존 메서드들 유지 ...
}
```

### 6.3 MovementComponent 수정

```csharp
// MovementComponent.cs (기존 파일 수정)

public class MovementComponent : MonoBehaviour, IMovementSystem
{
    // ... 기존 필드 유지 ...

    // 추가: TraitComponent 참조
    private TraitComponent traitComponent;

    private void Awake()
    {
        // ... 기존 초기화 코드 ...

        // TraitComponent 참조
        traitComponent = GetComponent<TraitComponent>();
    }

    /// <summary>이동 실행 (수정됨: 특성 통합)</summary>
    public MovementResult MoveTo(Vector2Int targetPosition)
    {
        // 1. 활성 이동 전략 가져오기
        IMovementStrategy strategy = traitComponent?.GetActiveMovementStrategy()
                                     ?? new DefaultMovementStrategy();

        // 2. 실행 컨텍스트 생성
        var context = new ExecutionContext(
            executor: GetComponent<Unit>(),
            targetTile: gridManager.GetTileAt(targetPosition),
            gridManager: gridManager
        );

        // 3. OnBeforeMove 특성 실행
        traitComponent?.EvaluateAndExecuteTraits(TriggerType.OnBeforeMove, context);

        // 4. 전략으로 이동 실행
        var result = strategy.Execute(targetPosition, context);

        // 5. OnAfterMove 특성 실행
        if (result.Success)
        {
            traitComponent?.EvaluateAndExecuteTraits(TriggerType.OnAfterMove, context);

            // 이동 완료 처리
            hasMovedThisTurn = true;
            currentMovementPoints -= result.MovementCostUsed;
            OnMovementCompleted?.Invoke(targetPosition);
        }

        return result;
    }

    /// <summary>이동 가능 위치 가져오기 (수정됨: 전략 고려)</summary>
    public List<Vector2Int> GetValidMovePositions(int direction = 0)
    {
        // 활성 이동 전략 가져오기
        IMovementStrategy strategy = traitComponent?.GetActiveMovementStrategy()
                                     ?? new DefaultMovementStrategy();

        var context = new ExecutionContext(
            executor: GetComponent<Unit>(),
            targetTile: null,
            gridManager: gridManager
        );

        var currentPos = gridManager.GetUnitPosition(gameObject);
        return strategy.GetValidPositions(currentPos, context);
    }

    // ... 기존 메서드들 유지 ...
}
```

### 6.4 Unit 클래스 수정

```csharp
// Unit.cs (기존 파일 수정)

public class Unit : MonoBehaviour
{
    // ... 기존 필드 유지 ...

    // 추가: TraitComponent 참조
    private TraitComponent traitComponent;

    private void Awake()
    {
        // ... 기존 초기화 코드 ...

        // TraitComponent 참조
        traitComponent = GetComponent<TraitComponent>();
    }

    /// <summary>피격 처리 (수정됨: 특성 트리거 추가)</summary>
    public void OnDamageTaken(int damage, GameObject attacker)
    {
        // 기존 피격 처리
        // ...

        // OnDamageTaken 특성 트리거
        if (traitComponent != null)
        {
            var context = new ExecutionContext(
                executor: this,
                targetTile: null,
                gridManager: ServiceLocator.Get<IGridManager>()
            );
            context.SetParameter("Damage", damage);
            context.SetParameter("Attacker", attacker);

            traitComponent.EvaluateAndExecuteTraits(TriggerType.OnDamageTaken, context);
        }
    }

    /// <summary>턴 시작 (수정됨: 특성 트리거 추가)</summary>
    public void OnTurnStart()
    {
        // 기존 턴 시작 처리
        // ...

        // OnTurnStart 특성 트리거
        if (traitComponent != null)
        {
            var context = new ExecutionContext(
                executor: this,
                targetTile: null,
                gridManager: ServiceLocator.Get<IGridManager>()
            );

            traitComponent.EvaluateAndExecuteTraits(TriggerType.OnTurnStart, context);
        }
    }

    /// <summary>턴 종료 (수정됨: 특성 트리거 추가)</summary>
    public void OnTurnEnd()
    {
        // 기존 턴 종료 처리
        // ...

        // OnTurnEnd 특성 트리거
        if (traitComponent != null)
        {
            var context = new ExecutionContext(
                executor: this,
                targetTile: null,
                gridManager: ServiceLocator.Get<IGridManager>()
            );

            traitComponent.EvaluateAndExecuteTraits(TriggerType.OnTurnEnd, context);
        }
    }

    /// <summary>
    /// 🆕 AI 결정 실행 (Strategy + AnimationController 통합 패턴)
    /// AI가 결정한 행동(ActionDecision)을 실제로 실행
    /// </summary>
    /// <param name="decision">AI가 결정한 행동</param>
    private void ExecuteDecision(ActionDecision decision)
    {
        var gridManager = ServiceLocator.Get<IGridManager>();

        switch (decision.Type)
        {
            case ActionType.Attack:
                if (decision.TargetTile != null)
                {
                    Debug.Log($"[Unit] Executing attack on tile ({decision.TargetTile.X}, {decision.TargetTile.Y})");

                    if (useComponentSystem && combatComponent != null)
                    {
                        // 1. ExecutionContext 생성 (AnimationController 포함)
                        var context = new ExecutionContext(
                            executor: this,
                            targetTile: decision.TargetTile,
                            gridManager: gridManager,
                            animationController: animationController  // 🆕 AnimationController 전달
                        );

                        // 2. TraitComponent에서 활성 Strategy 가져오기 (또는 기본 전략)
                        var strategy = traitComponent?.GetActiveAttackStrategy()
                                       ?? new DefaultAttackStrategy();

                        // 3. Strategy 실행 (순수 로직만 수행)
                        var tiles = new List<Tile> { decision.TargetTile };
                        var result = strategy.Execute(tiles, context);

                        // 4. AnimationController에 애니메이션 위임
                        if (result.Success && animationController != null)
                        {
                            animationController.PlayAttackAnimation(
                                animationType: result.AnimationType,
                                targets: result.AnimationTargets,
                                duration: result.AnimationDuration,
                                vfxPrefab: result.VFXPrefab,
                                onComplete: () =>
                                {
                                    Debug.Log($"[Unit] Attack animation completed");
                                    // 애니메이션 완료 후 추가 로직 (필요 시)
                                }
                            );
                        }

                        // 5. 결과 로깅
                        if (result.Success)
                        {
                            Debug.Log($"[Unit] {gameObject.name} dealt {result.DamageDealt} damage to {result.TargetsHit} target(s)");
                        }
                        else
                        {
                            Debug.LogWarning($"[Unit] Attack failed: {result.ErrorMessage}");
                        }
                    }
                }
                break;

            case ActionType.Move:
                Debug.Log($"[Unit] Executing move to {decision.MovePosition}");

                if (useComponentSystem && movementComponent != null)
                {
                    // 1. ExecutionContext 생성 (AnimationController 포함)
                    var context = new ExecutionContext(
                        executor: this,
                        targetTile: gridManager?.GetTileAt(decision.MovePosition),
                        gridManager: gridManager,
                        animationController: animationController  // 🆕 AnimationController 전달
                    );

                    // 2. TraitComponent에서 활성 Strategy 가져오기 (또는 기본 전략)
                    var strategy = traitComponent?.GetActiveMovementStrategy()
                                   ?? new DefaultMovementStrategy();

                    // 3. Strategy 실행 (순수 로직만 수행)
                    var result = strategy.Execute(decision.MovePosition, context);

                    // 4. AnimationController에 이동 애니메이션 위임
                    if (result.Success && animationController != null)
                    {
                        animationController.PlayMovementAnimation(
                            animationType: result.AnimationType,
                            path: result.PathPositions,
                            speed: result.MovementSpeed,
                            trailVFX: result.TrailVFX,
                            onComplete: () =>
                            {
                                Debug.Log($"[Unit] Movement animation completed");
                                // 애니메이션 완료 후 추가 로직 (필요 시)
                            }
                        );
                    }

                    // 5. 결과 로깅
                    if (result.Success)
                    {
                        Debug.Log($"[Unit] {gameObject.name} moved to {result.FinalPosition}");
                    }
                    else
                    {
                        Debug.LogWarning($"[Unit] Movement failed: {result.ErrorMessage}");
                    }
                }
                break;

            case ActionType.Wait:
                Debug.Log($"[Unit] {gameObject.name} is waiting");
                break;
        }
    }

    // ... 기존 메서드들 유지 ...
}
```

### 6.5 AnimationController 통합 (애니메이션 중앙 관리)

#### 설계 원칙

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        RESPONSIBILITY SEPARATION                                 │
├─────────────────────────────────────────────────────────────────────────────────┤
│ Component           │ Responsibility                                             │
├─────────────────────┼────────────────────────────────────────────────────────────┤
│ IUnitAI             │ 타겟/위치 결정, 전략적 의사결정                            │
│ Unit                │ AI 결정 실행, Strategy-Context-Animation 연결              │
│ ExecutionContext    │ 타겟 데이터 저장소 (Strategy → CombatComponent 전달)      │
│ IAttackStrategy     │ 타겟 계산 + 데미지 계산 (❌ 적용하지 않음!)               │
│ IMovementStrategy   │ 이동 경로 계산 (순수 비즈니스 로직)                        │
│ AttackResult        │ 애니메이션 메타데이터만 (타겟 정보 제외)                   │
│ AnimationController │ 타이밍 신호 전송 (OnAttackHit 이벤트, 타겟 파라미터 없음) │
│ CombatComponent     │ OnAttackHit 수신 → Context에서 타겟 가져옴 → 데미지 적용  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

**핵심 원칙**:
- ✅ **Strategy는 타겟 계산 + Context 저장**: 데미지 계산하되 적용하지 않음
- ✅ **AnimationController는 타이밍 신호만**: 타겟 정보 저장하지 않고 OnAttackHit() 호출만
- ✅ **CombatComponent가 데미지 적용**: Context에서 타겟 가져와 유효성 재확인 후 적용
- ✅ **ExecutionContext가 데이터 중계**: Strategy → Context → CombatComponent

#### IAnimationController 인터페이스

```csharp
using UnityEngine;
using System;

/// <summary>
/// 애니메이션 중앙 관리 인터페이스
/// Strategy는 이 인터페이스를 직접 호출하지 않고, Result에 애니메이션 정보만 포함
/// </summary>
public interface IAnimationController
{
    /// <summary>공격 애니메이션 재생</summary>
    /// <param name="animationType">애니메이션 타입 (AoE, Chain 등)</param>
    /// <param name="targets">VFX 재생 위치들</param>
    /// <param name="duration">애니메이션 지속 시간</param>
    /// <param name="vfxPrefab">VFX 프리팹 (없으면 기본 VFX 사용)</param>
    /// <param name="onComplete">애니메이션 완료 콜백</param>
    void PlayAttackAnimation(
        AttackAnimationType animationType,
        Vector3[] targets,
        float duration,
        GameObject vfxPrefab = null,
        Action onComplete = null
    );

    /// <summary>이동 애니메이션 재생</summary>
    /// <param name="animationType">애니메이션 타입 (Teleport, Jump 등)</param>
    /// <param name="path">이동 경로 (순차적인 위치들)</param>
    /// <param name="speed">이동 속도</param>
    /// <param name="trailVFX">이동 트레일 VFX 프리팹</param>
    /// <param name="onComplete">애니메이션 완료 콜백</param>
    void PlayMovementAnimation(
        MovementAnimationType animationType,
        Vector3[] path,
        float speed,
        GameObject trailVFX = null,
        Action onComplete = null
    );

    /// <summary>특성 활성화 애니메이션 재생</summary>
    /// <param name="vfxPrefab">VFX 프리팹</param>
    /// <param name="sfxClip">SFX 클립</param>
    void PlayTraitActivationAnimation(
        GameObject vfxPrefab,
        AudioClip sfxClip = null
    );

    /// <summary>애니메이션 중단</summary>
    void StopAllAnimations();

    /// <summary>현재 애니메이션 재생 중 여부</summary>
    bool IsPlaying { get; }
}
```

#### AnimationController 구현 예시

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 애니메이션 중앙 관리 구현체
/// </summary>
public class AnimationController : MonoBehaviour, IAnimationController
{
    [Header("VFX References")]
    [SerializeField] private GameObject defaultAttackVFX;
    [SerializeField] private GameObject defaultMoveTrailVFX;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private List<GameObject> activeVFXInstances = new List<GameObject>();
    private Coroutine currentAnimation;

    public bool IsPlaying => currentAnimation != null;

    /// <summary>공격 애니메이션 재생</summary>
    public void PlayAttackAnimation(
        AttackAnimationType animationType,
        Vector3[] targets,
        float duration,
        GameObject vfxPrefab = null,
        Action onComplete = null)
    {
        switch (animationType)
        {
            case AttackAnimationType.Default:
                StartCoroutine(PlaySingleAttackVFX(targets[0], vfxPrefab ?? defaultAttackVFX, duration, onComplete));
                break;

            case AttackAnimationType.AoE:
                StartCoroutine(PlayAoEAttackVFX(targets[0], vfxPrefab, duration, onComplete));
                break;

            case AttackAnimationType.Chain:
                StartCoroutine(PlayChainAttackVFX(targets, vfxPrefab, duration, onComplete));
                break;

            // 다른 애니메이션 타입들...
        }
    }

    /// <summary>이동 애니메이션 재생</summary>
    public void PlayMovementAnimation(
        MovementAnimationType animationType,
        Vector3[] path,
        float speed,
        GameObject trailVFX = null,
        Action onComplete = null)
    {
        switch (animationType)
        {
            case MovementAnimationType.Default:
                StartCoroutine(PlayWalkAnimation(path, speed, trailVFX, onComplete));
                break;

            case MovementAnimationType.Teleport:
                StartCoroutine(PlayTeleportAnimation(path[0], path[1], onComplete));
                break;

            case MovementAnimationType.Jump:
                StartCoroutine(PlayJumpAnimation(path, speed, onComplete));
                break;

            // 다른 애니메이션 타입들...
        }
    }

    /// <summary>특성 활성화 애니메이션</summary>
    public void PlayTraitActivationAnimation(GameObject vfxPrefab, AudioClip sfxClip = null)
    {
        if (vfxPrefab != null)
        {
            var vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity);
            activeVFXInstances.Add(vfx);
            Destroy(vfx, 3f);
        }

        if (sfxClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(sfxClip);
        }
    }

    /// <summary>모든 애니메이션 중단</summary>
    public void StopAllAnimations()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        foreach (var vfx in activeVFXInstances)
        {
            if (vfx != null) Destroy(vfx);
        }
        activeVFXInstances.Clear();
    }

    // ========== Private Animation Coroutines ==========

    private IEnumerator PlaySingleAttackVFX(Vector3 target, GameObject vfxPrefab, float duration, Action onComplete)
    {
        var vfx = Instantiate(vfxPrefab, target, Quaternion.identity);
        activeVFXInstances.Add(vfx);

        yield return new WaitForSeconds(duration);

        Destroy(vfx);
        activeVFXInstances.Remove(vfx);
        onComplete?.Invoke();
    }

    private IEnumerator PlayAoEAttackVFX(Vector3 center, GameObject vfxPrefab, float duration, Action onComplete)
    {
        // AoE VFX 로직
        var vfx = Instantiate(vfxPrefab, center, Quaternion.identity);
        activeVFXInstances.Add(vfx);

        yield return new WaitForSeconds(duration);

        Destroy(vfx);
        activeVFXInstances.Remove(vfx);
        onComplete?.Invoke();
    }

    private IEnumerator PlayChainAttackVFX(Vector3[] targets, GameObject vfxPrefab, float duration, Action onComplete)
    {
        // 연쇄 공격 VFX 로직 (타겟 간 라인 렌더러 등)
        float delayPerChain = duration / targets.Length;

        for (int i = 0; i < targets.Length; i++)
        {
            var vfx = Instantiate(vfxPrefab, targets[i], Quaternion.identity);
            activeVFXInstances.Add(vfx);
            Destroy(vfx, delayPerChain * 2);

            if (i < targets.Length - 1)
            {
                // 다음 타겟으로 연결 라인 생성
                yield return new WaitForSeconds(delayPerChain);
            }
        }

        yield return new WaitForSeconds(delayPerChain);
        onComplete?.Invoke();
    }

    private IEnumerator PlayWalkAnimation(Vector3[] path, float speed, GameObject trailVFX, Action onComplete)
    {
        // 걷기 애니메이션 로직
        GameObject trail = null;
        if (trailVFX != null)
        {
            trail = Instantiate(trailVFX, transform);
            activeVFXInstances.Add(trail);
        }

        foreach (var waypoint in path)
        {
            while (Vector3.Distance(transform.position, waypoint) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, waypoint, speed * Time.deltaTime);
                yield return null;
            }
        }

        if (trail != null)
        {
            Destroy(trail);
            activeVFXInstances.Remove(trail);
        }

        onComplete?.Invoke();
    }

    private IEnumerator PlayTeleportAnimation(Vector3 startPos, Vector3 endPos, Action onComplete)
    {
        // 페이드 아웃
        // (Fade Out VFX 재생)
        yield return new WaitForSeconds(0.3f);

        // 순간 이동
        transform.position = endPos;

        // 페이드 인
        // (Fade In VFX 재생)
        yield return new WaitForSeconds(0.3f);

        onComplete?.Invoke();
    }

    private IEnumerator PlayJumpAnimation(Vector3[] path, float speed, Action onComplete)
    {
        // 점프 애니메이션 (포물선 이동)
        // (Jump VFX 및 포물선 계산)
        yield return new WaitForSeconds(1f);

        onComplete?.Invoke();
    }
}
```

#### 책임 분리 다이어그램 (개선된 흐름)

```
┌─────────────────────────────────────────────────────────────────────┐
│              데이터 흐름 및 책임 분리 (개선 버전)                     │
└─────────────────────────────────────────────────────────────────────┘

1️⃣ AI Decision Layer (IUnitAI)
   ↓ ActionDecision { Type, TargetTile, MovePosition }

2️⃣ Unit Execution Layer (Unit.ExecuteDecision)
   ├─ ExecutionContext 생성 (타겟 저장소 제공)
   ├─ TraitComponent.GetActiveStrategy()
   ↓

3️⃣ Strategy Layer (IAttackStrategy)
   ├─ 타겟 계산 및 Context에 저장
   │  • context.SetCurrentAttackTargets(validTargets)
   ├─ 데미지 계산 (❌ 적용하지 않음!)
   ├─ ❌ VFX/SFX 직접 재생 금지
   ├─ ❌ 타겟에 데미지 적용 금지 (TakeDamage 호출하지 않음)
   ↓ AttackResult (애니메이션 메타데이터만, 타겟 정보 제외)

4️⃣ CombatComponent (이벤트 구독)
   ├─ AnimationController.OnAttackHit 이벤트 구독
   ├─ (대기 상태: 애니메이션 타이밍 신호 대기)
   ↓

5️⃣ Animation Layer (타이밍 신호 전송)
   ├─ Result.AnimationType 기반 애니메이션 선택
   ├─ VFX 인스턴스화 및 재생
   ├─ 타격 시점(60%)에 OnAttackHit?.Invoke() (❌ 타겟 파라미터 없음!)
   ├─ ❌ 타겟 정보 저장하지 않음
   └─ onComplete() 콜백 실행

6️⃣ Damage Application Layer (데미지 적용)
   ├─ CombatComponent.OnAnimationAttackHit() 콜백 실행
   ├─ var targets = context.GetCurrentAttackTargets()
   ├─ 타겟 유효성 재확인 (null, IsAlive 체크)
   └─ ApplyDamageToTargets(targets) → 실제 데미지 적용
```

#### 통합 예시: AoE 공격 전체 흐름

```csharp
// 1. AI가 공격 결정
var decision = unitAI.DecideAction(); // → ActionType.Attack, TargetTile

// 2. Unit이 ExecuteDecision() 호출
Unit.ExecuteDecision(decision);
    ↓
    // 3. ExecutionContext 생성
    var context = new ExecutionContext(
        executor: this,
        targetTile: decision.TargetTile,
        gridManager: gridManager,
        animationController: animationController
    );

    // 4. Strategy 실행 (순수 로직)
    var strategy = new AoEAttackStrategy();
    var result = strategy.Execute(tiles, context);
    // result = {
    //     DamageDealt: 150,
    //     TargetsHit: 3,
    //     Success: true,
    //     AnimationType: AttackAnimationType.AoE,
    //     AnimationTargets: [centerPosition],
    //     AnimationParameters: { "Radius": 2 }
    // }

    // 5. AnimationController에 위임
    animationController.PlayAttackAnimation(
        animationType: result.AnimationType,
        targets: result.AnimationTargets,
        duration: result.AnimationDuration,
        vfxPrefab: result.VFXPrefab,
        onComplete: () => Debug.Log("Animation complete")
    );
```

#### 장점 요약

| 이전 설계 (Strategy가 VFX 재생) | 현재 설계 (AnimationController 분리) |
|----------------------------------|-------------------------------------|
| ❌ Strategy가 비즈니스 로직 + 시각화 모두 담당 | ✅ Strategy는 순수 로직만 담당 |
| ❌ 테스트 시 VFX 의존성 필요 | ✅ Strategy를 VFX 없이 단위 테스트 가능 |
| ❌ Strategy마다 중복 VFX 코드 | ✅ AnimationController에 VFX 로직 중앙화 |
| ❌ 애니메이션 수정 시 Strategy 수정 | ✅ AnimationController만 수정 |
| ❌ Unit.cs에서 애니메이션 제어 불가 | ✅ Unit.cs가 애니메이션 완료 감지 및 제어 가능 |

---

## 7. 디렉토리 구조

```
Assets/Script/Game/
├── Traits/
│   ├── Core/
│   │   ├── TraitExecutor.cs                    # 실행 오케스트레이터
│   │   ├── TraitInstance.cs                    # 런타임 특성 상태
│   │   ├── ExecutionContext.cs                 # 실행 컨텍스트
│   │   ├── ConditionEvaluator.cs               # 조건 평가자
│   │   └── StrategyFactory.cs                  # 전략 팩토리
│   │
│   ├── Strategies/
│   │   ├── Attack/
│   │   │   ├── IAttackStrategy.cs              # 공격 전략 인터페이스
│   │   │   ├── DefaultAttackStrategy.cs        # 기본 공격
│   │   │   ├── AoEAttackStrategy.cs            # 범위 공격
│   │   │   ├── ChainAttackStrategy.cs          # 연쇄 공격
│   │   │   ├── DrainAttackStrategy.cs          # 흡혈 공격
│   │   │   ├── PierceAttackStrategy.cs         # 관통 공격
│   │   │   └── SplashAttackStrategy.cs         # 스플래시 공격
│   │   │
│   │   ├── Movement/
│   │   │   ├── IMovementStrategy.cs            # 이동 전략 인터페이스
│   │   │   ├── DefaultMovementStrategy.cs      # 기본 이동
│   │   │   ├── TeleportMovementStrategy.cs     # 순간이동
│   │   │   ├── JumpMovementStrategy.cs         # 점프 이동
│   │   │   ├── PhaseMovementStrategy.cs        # 위상 이동
│   │   │   └── ChargeMovementStrategy.cs       # 돌진 이동
│   │   │
│   │   └── Detection/
│   │       ├── IDetectionStrategy.cs           # 탐색 전략 인터페이스
│   │       ├── DefaultDetectionStrategy.cs     # 기본 시야
│   │       ├── StealthDetectionStrategy.cs     # 은신 감지
│   │       ├── ExtendedVisionStrategy.cs       # 확장 시야
│   │       ├── BlindStrategy.cs                # 시야 감소
│   │       └── OmniscientStrategy.cs           # 전체 맵 시야
│   │
│   ├── Data/
│   │   ├── TraitData.cs                        # 특성 데이터 ScriptableObject
│   │   ├── TraitConditionData.cs               # 조건 데이터 ScriptableObject
│   │   └── TraitEnums.cs                       # 특성 관련 열거형
│   │
│   └── Components/
│       └── TraitComponent.cs                   # 유닛 특성 컴포넌트
│
├── Components/                                  # 기존 컴포넌트 (수정됨)
│   ├── CombatComponent.cs                      # ← 특성 통합
│   ├── MovementComponent.cs                    # ← 특성 통합
│   ├── HealthComponent.cs                      # ← 특성 트리거 추가
│   └── ...
│
└── Unit.cs                                      # ← 특성 트리거 추가

Assets/Resources/Traits/                         # 특성 ScriptableObject 저장
├── Conditions/
│   ├── HealthBelow30Percent.asset
│   ├── EnemyInRange.asset
│   └── ...
│
└── Traits/
    ├── Berserker.asset                         # 버서커 특성
    ├── Vampire.asset                           # 뱀파이어 특성
    ├── Teleporter.asset                        # 순간이동자 특성
    └── ...
```

---

## 8. SOLID 원칙 준수

### 8.1 Single Responsibility Principle (단일 책임 원칙)

| 클래스/컴포넌트 | 단일 책임 | 검증 |
|----------------|----------|------|
| **TraitData** | 특성 데이터 정의만 저장 | ✅ 로직 없음 |
| **TraitConditionData** | 조건 파라미터만 저장 | ✅ 평가 로직 없음 |
| **TraitInstance** | 특성 런타임 상태 관리만 | ✅ 실행 로직 없음 |
| **TraitExecutor** | 실행 파이프라인 조정만 | ✅ 조건 평가 위임 |
| **ConditionEvaluator** | 조건 평가만 | ✅ 실행 로직 없음 |
| **IAttackStrategy** | 공격 행동 정의만 | ✅ 각 전략 독립적 |
| **TraitComponent** | 유닛의 특성 관리만 | ✅ 실행은 Executor에 위임 |

### 8.2 Open/Closed Principle (개방-폐쇄 원칙)

**확장에는 열려 있고, 수정에는 닫혀 있음**:

- ✅ **새 전략 추가**: `IAttackStrategy` 구현체 추가만으로 가능, 기존 코드 수정 불필요
- ✅ **새 조건 추가**: `ConditionEvaluator`에 case 추가만으로 가능
- ✅ **새 특성 추가**: Unity Inspector에서 ScriptableObject 생성만으로 가능

### 8.3 Liskov Substitution Principle (리스코프 치환 원칙)

**파생 클래스는 기반 클래스로 치환 가능**:

- ✅ 모든 `IAttackStrategy` 구현체는 상호 교환 가능
- ✅ 모든 `IMovementStrategy` 구현체는 상호 교환 가능
- ✅ 모든 `IDetectionStrategy` 구현체는 상호 교환 가능
- ✅ `CombatComponent`는 구체 전략을 알 필요 없음

### 8.4 Interface Segregation Principle (인터페이스 분리 원칙)

**클라이언트는 사용하지 않는 인터페이스에 의존하지 않음**:

- ✅ `IAttackStrategy`, `IMovementStrategy`, `IDetectionStrategy`는 각각 독립적
- ✅ 각 인터페이스는 자신의 책임에 필요한 메서드만 정의
- ✅ 클라이언트는 필요한 전략 인터페이스만 참조

### 8.5 Dependency Inversion Principle (의존성 역전 원칙)

**구체 클래스가 아닌 추상화에 의존**:

- ✅ `TraitComponent`는 `IAttackStrategy` 인터페이스에 의존 (구체 전략 모름)
- ✅ `CombatComponent`는 `TraitComponent` 추상화에 의존
- ✅ `TraitExecutor`는 `ConditionEvaluator` 추상화에 의존
- ✅ `ExecutionContext`를 통한 의존성 주입

---

## 9. 확장성 예시

### 9.1 예시 1: 버서커 특성 (체력 30% 이하 시 공격력 2배)

#### 1단계: 조건 생성
```
Unity Editor → Create → Game/Trait/Condition Data

Name: HealthBelow30Percent
Type: HealthPercentage
Operator: LessThan
Value: 30
Target Parameter: (비워둠)
Invert: false
Description: "체력이 30% 이하일 때"
```

#### 2단계: 특성 생성
```
Unity Editor → Create → Game/Trait/Trait Data

Name: 버서커
Description: "체력이 30% 이하일 때 공격력이 2배가 됩니다."
Icon: [버서커 아이콘]

Activation Conditions:
  - HealthBelow30Percent

Attack Strategy Override: None
Movement Strategy Override: None
Detection Strategy Override: None

Execution Priority: 50
Trigger On: Passive
```

#### 3단계: 유닛에 적용
```
Unit GameObject → Inspector → Trait Component

Initial Traits:
  - 버서커
```

**결과**: 코드 수정 없이 디자이너가 Unity Inspector에서 특성 생성 완료!

---

### 9.2 예시 2: 뱀파이어 특성 (공격 시 데미지의 50% 회복)

#### 1단계: 조건 (항상 활성)
조건 없음 → 항상 실행됨

#### 2단계: 새 전략 클래스 생성 (개발자 작업)

```csharp
// VampireAttackStrategy.cs (새 파일)
public class VampireAttackStrategy : IAttackStrategy
{
    public float LifeStealPercent { get; set; } = 0.5f;

    public string StrategyName => "뱀파이어 공격";
    public AttackStrategyType StrategyType => AttackStrategyType.Drain;

    public AttackResult Execute(List<Tile> targetTiles, ExecutionContext context)
    {
        var target = targetTiles[0];
        var damage = CalculateDamage(target, context);

        // 데미지 적용
        target.GetDamageableTarget()?.TakeDamage(damage);

        // 흡혈 회복
        int healAmount = Mathf.RoundToInt(damage * LifeStealPercent);
        context.Executor.GetComponent<IHealthComponent>()?.Heal(healAmount);

        return new AttackResult
        {
            DamageDealt = damage,
            HealingDone = healAmount,
            Success = true
        };
    }

    // ... 기타 인터페이스 구현 ...
}
```

#### 3단계: StrategyFactory에 등록

```csharp
// StrategyFactory.cs (기존 파일 수정)
public IAttackStrategy CreateAttackStrategy(AttackStrategyType type)
{
    return type switch
    {
        // ... 기존 코드 ...
        AttackStrategyType.Drain => new VampireAttackStrategy(), // ← 추가
        _ => null
    };
}
```

#### 4단계: 열거형에 추가

```csharp
// TraitEnums.cs (기존 파일 수정)
public enum AttackStrategyType
{
    // ... 기존 코드 ...
    Drain, // ← 추가
    // ...
}
```

#### 5단계: 특성 생성 (디자이너 작업)

```
Unity Editor → Create → Game/Trait/Trait Data

Name: 뱀파이어
Description: "공격 시 입힌 데미지의 50%만큼 체력을 회복합니다."
Icon: [뱀파이어 아이콘]

Activation Conditions: (비워둠)

Attack Strategy Override: Drain ← 핵심!
Movement Strategy Override: None
Detection Strategy Override: None

Execution Priority: 50
Trigger On: OnBeforeAttack
```

**결과**: 새 전략 클래스 추가 + StrategyFactory 등록만으로 확장 완료!

---

## 10. 구현 단계

### Phase 1: 핵심 인프라 구축 (1-2일)

**목표**: Strategy 인터페이스, 데이터 계층, Executor 구현

#### 작업 목록:
1. ✅ 디렉토리 구조 생성
   - `Assets/Script/Game/Traits/` 하위 폴더 생성

2. ✅ 데이터 계층 구현
   - `TraitData.cs` ScriptableObject
   - `TraitConditionData.cs` ScriptableObject
   - `TraitEnums.cs` (모든 열거형)

3. ✅ Strategy 인터페이스 정의
   - `IAttackStrategy.cs`
   - `IMovementStrategy.cs`
   - `IDetectionStrategy.cs`

4. ✅ 실행 계층 구현
   - `TraitExecutor.cs`
   - `ConditionEvaluator.cs`
   - `TraitInstance.cs`
   - `ExecutionContext.cs`
   - `StrategyFactory.cs`

**검증**:
- ScriptableObject 생성 가능 여부 확인
- 열거형 Inspector 노출 확인

---

### Phase 2: 기본 구현체 (2-3일)

**목표**: Default 전략 구현 및 기존 시스템 통합

#### 작업 목록:
1. ✅ 기본 전략 구현 (기존 로직 래핑)
   - `DefaultAttackStrategy.cs` (기존 CombatComponent 로직 사용)
   - `DefaultMovementStrategy.cs` (기존 MovementComponent 로직 사용)
   - `DefaultDetectionStrategy.cs` (새로 구현)

2. ✅ TraitComponent 구현
   - `TraitComponent.cs` 완성
   - Unit에 컴포넌트 추가

3. ✅ 기존 컴포넌트 리팩토링
   - `CombatComponent.cs` 수정 (특성 통합)
   - `MovementComponent.cs` 수정 (특성 통합)
   - `Unit.cs` 수정 (트리거 추가)

4. ✅ 조건 평가 로직 구현
   - `ConditionEvaluator.cs`에 기본 조건 타입 구현
   - HealthThreshold, TargetInRange, EnemyCount 등

**검증**:
- 특성 없이도 기존 동작 유지되는지 확인
- TraitComponent 추가 시 정상 동작 확인

---

### Phase 3: 고급 전략 구현 (3-4일)

**목표**: AoE, Chain, Teleport 등 고급 전략 구현

#### 작업 목록:
1. ✅ 고급 공격 전략
   - `AoEAttackStrategy.cs` (범위 공격)
   - `ChainAttackStrategy.cs` (연쇄 공격)
   - `DrainAttackStrategy.cs` (흡혈 공격)
   - `PierceAttackStrategy.cs` (관통 공격)

2. ✅ 고급 이동 전략
   - `TeleportMovementStrategy.cs` (순간이동)
   - `JumpMovementStrategy.cs` (점프 이동)
   - `PhaseMovementStrategy.cs` (위상 이동)

3. ✅ 고급 탐색 전략
   - `StealthDetectionStrategy.cs` (은신 감지)
   - `ExtendedVisionStrategy.cs` (확장 시야)
   - `BlindStrategy.cs` (시야 감소)

4. ✅ StrategyFactory 업데이트
   - 모든 전략 타입 등록

5. ✅ 다양한 조건 타입 추가
   - TileType, HasStatusEffect, TurnCount 등

**검증**:
- 각 전략 개별 테스트
- VFX/SFX 통합 테스트

---

### Phase 4: 테스트 및 최적화 (2-3일)

**목표**: 특성 조합 테스트 및 성능 최적화

#### 작업 목록:
1. ✅ 테스트용 특성 생성
   - 버서커 특성
   - 뱀파이어 특성
   - 텔레포터 특성
   - 은신 감지자 특성
   - 범위 공격자 특성

2. ✅ 특성 조합 테스트
   - 여러 특성 동시 보유 시 우선순위 테스트
   - 조건 충돌 시나리오 테스트
   - 트리거 타입별 실행 테스트

3. ✅ 성능 프로파일링
   - 조건 평가 최적화 (캐싱)
   - 전략 객체 풀링 고려
   - Update() 호출 최적화

4. ✅ 디자이너 워크플로우 검증
   - ScriptableObject 생성 가이드 작성
   - Inspector 사용성 개선
   - 에러 메시지 개선

5. ✅ 문서화
   - 아키텍처 문서 업데이트
   - API 레퍼런스 작성
   - 디자이너 가이드 작성

**검증**:
- 모든 특성 시나리오 통과
- 60 FPS 이상 유지
- 디자이너가 새 특성 생성 가능

---

## 11. BasicUnitAI 통합 예제

### 11.1 AI가 Strategy를 참조하는 구조

```csharp
/// <summary>
/// BasicUnitAI with TraitSystem Integration
/// ✅ AI가 Strategy.GetValidTargets()로 가능한 타겟 얻기
/// ✅ AI가 최적 타겟 선택 → Unit.ExecuteDecision()에 전달
/// ✅ Strategy는 AI가 선택한 타겟 기반으로 실행 (추가 타겟 계산)
/// </summary>
public class BasicUnitAI : MonoBehaviour, IUnitAI
{
    private ICombatSystem combatComponent;
    private IMovementSystem movementComponent;
    private ITeamComponent teamComponent;
    private IGridManager gridManager;
    private TraitComponent traitComponent; // 🆕 추가

    private void Awake()
    {
        combatComponent = GetComponent<ICombatSystem>();
        movementComponent = GetComponent<IMovementSystem>();
        teamComponent = GetComponent<ITeamComponent>();
        traitComponent = GetComponent<TraitComponent>(); // 🆕 초기화
    }

    public ActionDecision DecideAction()
    {
        // 1️⃣ 공격 타겟 찾기 (Strategy 사용)
        var targetTile = FindBestTarget();

        // 2️⃣ 타겟이 있으면 공격
        if (targetTile != null)
        {
            return ActionDecision.Attack(targetTile);
        }

        // 3️⃣ 타겟 없으면 이동
        var movePosition = GetForwardMovePosition();
        if (movePosition.HasValue)
        {
            return ActionDecision.Move(movePosition.Value);
        }

        // 4️⃣ Idle
        return ActionDecision.Idle();
    }

    /// <summary>
    /// 🔄 Strategy를 참조하여 가능한 타겟 얻기
    /// </summary>
    public Tile FindBestTarget()
    {
        if (combatComponent == null || gridManager == null)
            return null;

        // 1️⃣ 활성 공격 전략 가져오기
        IAttackStrategy strategy = traitComponent?.GetActiveAttackStrategy()
                                   ?? new DefaultAttackStrategy();

        // 2️⃣ ExecutionContext 생성
        var myPosition = gridManager.GetUnitPosition(gameObject);
        var context = new ExecutionContext(
            executor: GetComponent<Unit>(),
            targetTile: null,
            gridManager: gridManager
        );

        // 3️⃣ Strategy에게 가능한 타겟 요청
        var validTargetTiles = strategy.GetValidTargets(myPosition, context);

        if (validTargetTiles.Count == 0)
            return null;

        // 4️⃣ AI 로직으로 최적 타겟 선택
        List<Tile> attackableTiles = new List<Tile>();

        foreach (var tile in validTargetTiles)
        {
            // 타일에 유닛 또는 베이스가 있는지 확인
            bool hasUnit = tile.OccupyingUnit != null && tile.OccupyingUnit.IsAlive;
            bool hasBase = tile.OccupyingBase != null && tile.OccupyingBase.IsAlive;

            if (!hasUnit && !hasBase) continue;

            // 적군 체크
            var targetObject = hasUnit ? tile.OccupyingUnit.gameObject : tile.OccupyingBase.gameObject;
            if (IsEnemyTarget(targetObject))
            {
                attackableTiles.Add(tile);
            }
        }

        // 5️⃣ 최적 타겟 선택 (간단한 전략: 첫 번째 타겟)
        // TODO: Enhanced AI - 체력 낮은 적, 고가치 타겟 우선순위
        return attackableTiles.Count > 0 ? attackableTiles[0] : null;
    }

    /// <summary>
    /// 🔄 Strategy를 참조하여 가능한 이동 위치 얻기
    /// </summary>
    private Vector2Int? GetForwardMovePosition()
    {
        if (movementComponent == null || gridManager == null)
            return null;

        // 1️⃣ 활성 이동 전략 가져오기
        IMovementStrategy strategy = traitComponent?.GetActiveMovementStrategy()
                                     ?? new DefaultMovementStrategy();

        // 2️⃣ ExecutionContext 생성
        var myPosition = gridManager.GetUnitPosition(gameObject);
        var context = new ExecutionContext(
            executor: GetComponent<Unit>(),
            targetTile: null,
            gridManager: gridManager
        );

        // 3️⃣ Strategy에게 가능한 이동 위치 요청
        var validPositions = strategy.GetValidPositions(myPosition, context);

        if (validPositions.Count == 0)
            return null;

        // 4️⃣ AI 로직으로 최적 이동 위치 선택 (전진 방향)
        int direction = teamComponent?.Team == TeamType.Player ? 1 : -1;

        Vector2Int? bestPosition = null;
        int maxDistance = 0;

        foreach (var pos in validPositions)
        {
            int distance = (pos.y - myPosition.y) * direction;
            if (distance > maxDistance)
            {
                maxDistance = distance;
                bestPosition = pos;
            }
        }

        return bestPosition;
    }

    private bool IsEnemyTarget(GameObject target)
    {
        if (teamComponent == null) return true;

        var targetTeam = target.GetComponent<ITeamComponent>();
        if (targetTeam == null) return true;

        return teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
    }
}
```

### 11.2 Unit.ExecuteDecision() 예제

```csharp
/// <summary>
/// AI 결정 실행 - ActionDecision을 실제 행동으로 변환
/// ⚠️ AI가 이미 타겟을 선택한 상태
/// </summary>
private void ExecuteDecision(ActionDecision decision)
{
    switch (decision.Type)
    {
        case ActionType.Attack:
            if (decision.TargetTile != null && combatComponent != null)
            {
                // AI가 선택한 타겟 타일을 CombatComponent에 전달
                // CombatComponent가 TraitComponent의 Strategy를 사용하여 실행
                var tiles = new List<Tile> { decision.TargetTile };
                int damage = combatComponent.AttackTiles(tiles);

                Debug.Log($"[Unit] Attack executed: {damage} damage dealt");
            }
            break;

        case ActionType.Move:
            if (decision.MovePosition.HasValue && movementComponent != null)
            {
                // AI가 선택한 이동 위치를 MovementComponent에 전달
                // MovementComponent가 TraitComponent의 Strategy를 사용하여 실행
                var result = movementComponent.MoveTo(decision.MovePosition.Value);

                if (result.Success)
                {
                    Debug.Log($"[Unit] Move executed to {decision.MovePosition.Value}");
                }
            }
            break;

        case ActionType.Idle:
            Debug.Log($"[Unit] Idle - no valid actions");
            break;
    }
}
```

### 11.3 전체 데이터 흐름

```
┌────────────────────────────────────────────────────────────────┐
│ 1️⃣ AI DecideAction()                                          │
│    - traitComponent.GetActiveAttackStrategy()                 │
│    - strategy.GetValidTargets() → 가능한 타겟 목록 얻기      │
│    - AI 로직으로 최적 타겟 선택                               │
│    - ActionDecision.Attack(selectedTile) 반환                 │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│ 2️⃣ Unit.ExecuteDecision(decision)                             │
│    - decision.TargetTile (AI가 선택한 타겟)                   │
│    - combatComponent.AttackTiles([targetTile])                │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│ 3️⃣ CombatComponent.AttackTiles(targetTiles)                   │
│    - traitComponent.GetActiveAttackStrategy()                 │
│    - ExecutionContext 생성 (AI선택타겟 포함)                  │
│    - strategy.Execute(primaryTarget, context)                 │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│ 4️⃣ Strategy.Execute(primaryTarget, context)                   │
│    - Primary 타겟 + 추가 타겟 계산 (AoE, Chain 등)          │
│    - 모든 타겟을 ExecutionContext에 저장                     │
│    - AttackResult 반환 (애니메이션 메타데이터)               │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│ 5️⃣ AnimationController.PlayAttackAnimation(result)            │
│    - VFX 재생                                                 │
│    - 타격 시점에 OnAttackHit 이벤트 발생                      │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│ 6️⃣ CombatComponent.OnAnimationAttackHit()                     │
│    - context.GetCurrentAttackTargets() 가져오기               │
│    - 각 타겟에 데미지 적용                                    │
└────────────────────────────────────────────────────────────────┘
```

---

## 12. 장점 요약

### 12.1 확장성
✅ 새 전략 추가 시 기존 코드 수정 최소화
✅ 새 조건 타입 추가 용이
✅ 디자이너가 코드 없이 특성 생성 가능
✅ 특성 조합으로 무한한 가능성

### 12.2 유지보수성
✅ 명확한 책임 분리로 디버깅 용이
✅ 각 컴포넌트 독립적으로 테스트 가능
✅ 조건 재사용으로 중복 코드 제거
✅ 단일 책임 원칙으로 변경 영향 범위 최소화

### 12.3 성능
✅ 조건 평가 조기 종료 (early exit)
✅ 전략 객체 재사용 (인스턴스 캐싱)
✅ 불필요한 특성 평가 스킵 (트리거 기반)
✅ 조건 검사를 통한 효율적인 특성 활성화

### 12.4 디자이너 친화성
✅ Unity Inspector에서 모든 설정 가능
✅ 코드 지식 불필요
✅ 실시간 수정 가능 (Play Mode 중에도)
✅ 직관적인 조건/효과 조합

### 12.5 테스트 용이성
✅ 각 전략 독립적으로 유닛 테스트 가능
✅ Mock 객체로 조건 평가 테스트 가능
✅ ExecutionContext로 테스트 환경 주입
✅ 특성 조합 시나리오 테스트 자동화 가능

---

## 13. 결론

이 아키텍처는 **전략 패턴**, **ScriptableObject 데이터 주도 설계**, **Executor 실행 파이프라인**을 결합하여:

1. ✅ **확장성**: 새 특성/전략 추가 시 기존 코드 수정 최소화
2. ✅ **유지보수성**: SOLID 원칙 준수로 변경 영향 범위 제한
3. ✅ **객체 책임 보존**: 각 컴포넌트가 명확한 단일 책임 보유
4. ✅ **디자이너 친화성**: 코드 없이 Unity Inspector에서 특성 생성
5. ✅ **성능 최적화**: 조건 평가 최적화 및 전략 재사용

를 모두 달성합니다.

이 설계는 **기존 시스템과의 호환성을 유지**하면서도 **미래 확장을 위한 견고한 기반**을 제공합니다.

---

**문서 버전**: 1.1
**최종 수정**: 2025-10-31
**작성자**: Claude (Anthropic)

**주요 변경사항 (v1.1)**:
- ✅ AI → Strategy 통합 구조 명확화
- ✅ `IAttackStrategy.Execute()` 시그니처 수정: `List<Tile> targetTiles` → `Tile primaryTarget`
- ✅ `IMovementStrategy` 주석 추가로 AI 사용 명확화
- ✅ BasicUnitAI가 Strategy.GetValidTargets()를 참조하는 올바른 패턴 추가 (섹션 11)
- ✅ 전체 시스템 흐름 다이어그램 업데이트 (AI가 Strategy 참조)
- ✅ DefaultAttackStrategy, AoEAttackStrategy, ChainAttackStrategy 예제 수정
- ✅ CombatComponent.AttackTiles() 예제 수정