# 다중 타겟 검증 아키텍처 설계

## 문서 정보
- **작성일**: 2025-10-07
- **버전**: 1.0
- **목적**: VFXEventTrigger 기반 다중 타겟 원자적 검증 시스템 설계

---

## 1. 문제 정의

### 1.1 현재 구조의 문제점

#### 단일 타겟 검증 흐름 (정상 작동)
```
T=0.0s  SpellEffectExecutor.ExecuteBatch()
        └─ GetTargetObject(targetPos) → 중앙 타겟 획득

T=0.0s  VFXEventTrigger.Initialize(predeterminedTarget)
        └─ VFX 인스턴스 생성

T=2.0s  VFX 트리거 시점 도달
        └─ ValidatePredeterminedTarget(중앙 타겟)
           ├─ null 체크
           ├─ activeInHierarchy 체크
           ├─ HealthComponent.IsAlive 체크
           └─ SetTargetValid() OR SetTargetInvalid()

T=2.0s  ExecuteEffectsWithData(triggerData)
        └─ AttackSuccess 기반 효과 적용
```

#### 다중 타겟 검증 흐름 (문제 발생)
```
T=0.0s  SpellEffectExecutor.ExecuteBatch()
        └─ GetTargetObject(targetPos) → 중앙 타겟만 획득

T=0.0s  VFXEventTrigger.Initialize(중앙 타겟)
        └─ VFX 인스턴스 생성

T=2.0s  VFX 트리거 시점 도달
        └─ ValidatePredeterminedTarget(중앙 타겟)
           └─ 중앙 타겟만 검증됨

T=2.0s  ExecuteEffectsWithData(triggerData)
        └─ if (effectData.AffectedRange > 0):
            └─ ExecuteMultiTargetEffect()
               └─ GetAffectedUnits() ← 이 시점에 범위 계산!

T=2.01s foreach (targetUnit in affectedUnits):
        └─ 🚨 SetTargetValid(targetUnit) 무조건 호출
           └─ AttackSuccess = true 강제 설정
           └─ 개별 검증 없음!
```

### 1.2 핵심 문제

1. **검증 시점 불일치**
   - 중앙 타겟: T=2.0s (VFX 트리거 시점) ✅
   - 범위 내 타겟: T=2.01s (효과 적용 직전) ❌
   - **시간 차이로 인한 상태 불일치 가능**

2. **무조건적 성공 처리**
   ```csharp
   // SpellEffectExecutor.cs:242
   targetTriggerData.SetTargetValid(targetUnit);  // gridPos 인수 부족
   ```
   - 각 유닛에 대한 검증 없이 무조건 `AttackSuccess = true`
   - 사망한 유닛, 비활성화된 유닛도 효과 받음

3. **검증 로직 중복 우려**
   - VFXEventTrigger: `ValidatePredeterminedTarget()`
   - SpellEffectExecutor: 추가 검증 로직 필요 → 중복 발생

---

## 2. 제안된 해결책: 리스트 기반 원자적 검증

### 2.1 핵심 아이디어

> **"VFX 트리거 시점에 모든 영향받는 유닛을 동시에 검증"**

- VFXEventTrigger 초기화 시 모든 잠재적 타겟 전달
- VFX 트리거 시점에 모든 타겟을 원자적으로 검증
- `List<VFXTriggerData>` 형태로 검증 결과 반환

### 2.2 설계 원칙

#### 단일 책임 분리
```
VFXEventTrigger: "VFX 타이밍 제어 + 타겟 검증"
├─ VFX가 영향을 주는 타겟들을 알고 있음
├─ 트리거 시점에 모든 타겟의 유효성 원자적 검증
└─ 검증 결과를 List<VFXTriggerData>로 반환

SpellEffectExecutor: "효과 실행 중재"
├─ 검증된 TriggerData 리스트를 받음
├─ 각 효과에 맞는 TriggerData 필터링
└─ 적절한 타겟에 효과 적용
```

#### 원자적 검증 (Atomic Validation)
```
모든 타겟의 상태를 단일 시점에 스냅샷
→ VFX 트리거 순간의 게임 상태 보장
→ 시간 차이로 인한 불일치 제거
```

---

## 3. 상세 설계

### 3.1 VFXEventTrigger 구조 변경

#### 3.1.1 Initialize 시그니처 변경

**Before**:
```csharp
public void Initialize(
    float triggerNormalizedTime,
    Action<VFXTriggerData> onTrigger,      // 단일 TriggerData
    GameObject predeterminedTarget          // 단일 타겟
)
```

**After**:
```csharp
public void Initialize(
    float triggerNormalizedTime,
    Action<List<VFXTriggerData>> onTrigger,     // 리스트 콜백
    List<GameObject> predeterminedTargets,       // 다중 타겟
    AffectedType affectedType,                   // 효과 대상 타입
    IGridManager gridManager = null              // 그리드 좌표 계산용
)
```

#### 3.1.2 검증 로직 수정

**기존 단일 타겟 검증**:
```csharp
private void ValidatePredeterminedTarget(VFXTriggerData triggerData)
{
    if (predeterminedTarget == null)
    {
        triggerData.SetTargetInvalid(null, "No predetermined target");
        return;
    }

    if (!predeterminedTarget.activeInHierarchy)
    {
        triggerData.SetTargetInvalid(predeterminedTarget, "Target destroyed or inactive");
        return;
    }

    var healthComponent = predeterminedTarget.GetComponent<HealthComponent>();
    if (healthComponent == null || !healthComponent.IsAlive)
    {
        triggerData.SetTargetInvalid(predeterminedTarget, "Target is not alive");
        return;
    }

    Vector2Int gridPos = gridManager.WorldToGridPosition(predeterminedTarget.transform.position);
    triggerData.SetTargetValid(predeterminedTarget, gridPos);
}
```

**새로운 다중 타겟 검증**:
```csharp
/// <summary>
/// VFX 트리거 시점에 모든 타겟을 원자적으로 검증
/// </summary>
private List<VFXTriggerData> ValidateAllTargets(AffectedType affectedType)
{
    var triggerDataList = new List<VFXTriggerData>();

    // 타겟이 없는 경우
    if (predeterminedTargets == null || predeterminedTargets.Count == 0)
    {
        Debug.LogWarning("[VFXEventTrigger] No predetermined targets");
        return triggerDataList;
    }

    // 각 타겟을 개별적으로 검증
    foreach (var target in predeterminedTargets)
    {
        var triggerData = new VFXTriggerData
        {
            TriggerWorldPosition = target != null ? target.transform.position : Vector3.zero,
            NormalizedProgress = currentProgress
        };

        // 개별 타겟 검증 (AffectedType 전달)
        ValidateSingleTarget(target, triggerData, affectedType);

        // 검증 실패한 타겟도 리스트에 포함 (AttackSuccess = false)
        triggerDataList.Add(triggerData);
    }

    return triggerDataList;
}

/// <summary>
/// 단일 타겟 검증 로직 (재사용 가능)
/// </summary>
private void ValidateSingleTarget(GameObject target, VFXTriggerData triggerData, AffectedType affectedType)
{
    // 1. Null 체크
    if (target == null)
    {
        triggerData.SetTargetInvalid(null, "Target is null");
        return;
    }

    // 2. 활성화 상태 체크
    if (!target.activeInHierarchy)
    {
        triggerData.SetTargetInvalid(target, "Target destroyed or inactive");
        return;
    }

    // 3. AffectedType.NotAny는 타일 타겟이므로 HealthComponent 검사 건너뛰기
    if (affectedType == AffectedType.NotAny)
    {
        // 타일 GameObject는 항상 유효 (빈 타일 여부는 Effect에서 최종 확인)
        Vector2Int gridPos = Vector2Int.zero;
        if (gridManager != null)
        {
            gridPos = gridManager.WorldToGridPosition(target.transform.position);
        }
        triggerData.SetTargetValid(target, gridPos);
        return;
    }

    // 4. HealthComponent 존재 및 생존 체크 (유닛 타겟만)
    var healthComponent = target.GetComponent<HealthComponent>();
    if (healthComponent == null)
    {
        triggerData.SetTargetInvalid(target, "Target does not have HealthComponent");
        return;
    }

    if (!healthComponent.IsAlive)
    {
        triggerData.SetTargetInvalid(target, "Target is not alive");
        return;
    }

    // 5. 그리드 좌표 계산 및 검증 성공 설정
    Vector2Int gridPos = Vector2Int.zero;
    if (gridManager != null)
    {
        gridPos = gridManager.WorldToGridPosition(target.transform.position);
    }
    else
    {
        Debug.LogWarning($"[VFXEventTrigger] GridManager not available for {target.name}");
    }

    triggerData.SetTargetValid(target, gridPos);
}
```

#### 3.1.3 트리거 타이밍 처리

```csharp
private void OnParticleSystemStopped()
{
    if (onTriggerCallback == null) return;

    // 모든 타겟을 원자적으로 검증 (AffectedType 전달)
    var triggerDataList = ValidateAllTargets(affectedType);

    // 리스트 콜백 호출
    onTriggerCallback.Invoke(triggerDataList);

    Debug.Log($"[VFXEventTrigger] Triggered with {triggerDataList.Count} validated targets");
}
```

### 3.2 SpellEffectExecutor 구조 변경

#### 3.2.1 ExecuteBatch 수정

```csharp
public void ExecuteBatch(IReadOnlyList<EffectData> effectDataList, Vector2Int targetPos, GameContext context)
{
    if (effectDataList == null || effectDataList.Count == 0)
    {
        Debug.LogWarning("[SpellEffectExecutor] ExecuteBatch: Empty effect list");
        return;
    }

    // 우선순위 정렬
    var sortedEffects = effectDataList.OrderBy(e => e.Priority).ToList();

    // VFX 데이터 가져오기
    VFXData vfxData = sortedEffects[0].VFXData;

    if (vfxData == null || vfxData.VFXPrefab == null)
    {
        Debug.LogWarning("[SpellEffectExecutor] No VFX data, executing effects immediately");
        ExecuteEffectsImmediate(sortedEffects, targetPos, context, null);
        return;
    }

    // 🆕 모든 잠재적 타겟 사전 계산
    var allPotentialTargets = CalculateAllPotentialTargets(sortedEffects, targetPos, context);

    // Coroutine 실행
    StartCoroutine(ExecuteWithVFX(sortedEffects, vfxData, targetPos, allPotentialTargets, context));
}
```

#### 3.2.2 잠재적 타겟 계산

```csharp
/// <summary>
/// 모든 효과가 영향을 줄 수 있는 잠재적 타겟 계산
/// </summary>
private List<GameObject> CalculateAllPotentialTargets(
    IReadOnlyList<EffectData> effects,
    Vector2Int targetPos,
    GameContext context)
{
    // 1. 최대 범위 계산
    int maxRange = effects.Max(e => e.AffectedRange);

    // 2. AffectedType 통합 (가장 포괄적인 타입 선택)
    AffectedType unifiedType = DetermineUnifiedAffectedType(effects);

    // 3. 범위 내 모든 유닛 획득
    if (maxRange == 0)
    {
        // 단일 타겟: 중앙 타겟만
        var centerTarget = context.GridController.GetUnitAtPosition(targetPos);
        return centerTarget != null ? new List<GameObject> { centerTarget } : new List<GameObject>();
    }
    else
    {
        // 다중 타겟: 최대 범위 내 모든 유닛
        return context.GridController.GetAffectedUnits(
            targetPos,
            unifiedType,
            maxRange,
            context.PlayerId
        );
    }
}

/// <summary>
/// 여러 효과의 AffectedType을 통합
/// Any > Enemy > Ally 우선순위
/// </summary>
private AffectedType DetermineUnifiedAffectedType(IReadOnlyList<EffectData> effects)
{
    bool hasAny = effects.Any(e => e.AffectedType == AffectedType.Any);
    if (hasAny) return AffectedType.Any;

    bool hasEnemy = effects.Any(e => e.AffectedType == AffectedType.Enemy);
    bool hasAlly = effects.Any(e => e.AffectedType == AffectedType.Ally);

    // Enemy와 Ally 모두 있으면 Any
    if (hasEnemy && hasAlly) return AffectedType.Any;

    // 하나만 있으면 해당 타입
    if (hasEnemy) return AffectedType.Enemy;
    if (hasAlly) return AffectedType.Ally;

    return AffectedType.None;
}
```

#### 3.2.3 VFX 실행 Coroutine 수정

```csharp
private IEnumerator ExecuteWithVFX(
    IReadOnlyList<EffectData> effects,
    VFXData vfxData,
    Vector2Int targetPos,
    List<GameObject> potentialTargets,  // 🆕 다중 타겟
    GameContext context)
{
    GameObject vfxInstance = null;
    Vector3 worldPos = Vector3.zero;
    bool hasError = false;
    bool effectsExecuted = false;
    float maxWait = 0f;
    float elapsed = 0f;

    try
    {
        worldPos = context.GridController.GridToWorldPosition(targetPos);
        vfxInstance = Instantiate(vfxData.VFXPrefab, worldPos, Quaternion.identity);

        VFXEventTrigger trigger = vfxInstance.GetComponent<VFXEventTrigger>();
        if (trigger == null)
            trigger = vfxInstance.AddComponent<VFXEventTrigger>();

        // 🆕 리스트 콜백으로 변경
        trigger.Initialize(
            vfxData.TriggerNormalizedTime,
            (triggerDataList) => {
                // 리스트 기반 효과 실행
                ExecuteEffectsWithDataList(effects, triggerDataList, targetPos, context);
                effectsExecuted = true;
            },
            potentialTargets,           // 🆕 다중 타겟 전달
            context.GridController      // 🆕 GridManager 전달
        );

        maxWait = vfxData.IsLooping ? 10f : vfxData.Duration + 1f;
    }
    catch (Exception ex)
    {
        Debug.LogError($"[SpellEffectExecutor] VFX initialization error: {ex.Message}\n{ex.StackTrace}");
        hasError = true;
    }

    if (hasError)
    {
        ExecuteEffectsImmediate(effects, targetPos, context, null);
        if (vfxInstance != null)
            Destroy(vfxInstance);
        yield break;
    }

    // VFX 재생 대기
    while (!effectsExecuted && elapsed < maxWait)
    {
        yield return null;
        elapsed += Time.deltaTime;
    }

    // Timeout 처리
    if (!effectsExecuted)
    {
        Debug.LogWarning($"[SpellEffectExecutor] VFX timeout, forcing execution");
        // 🆕 빈 리스트로 강제 실행
        ExecuteEffectsWithDataList(effects, new List<VFXTriggerData>(), targetPos, context);
    }

    // VFX 정리
    if (!vfxData.IsLooping)
    {
        yield return new WaitForSeconds(vfxData.Duration);
        if (vfxInstance != null)
            Destroy(vfxInstance);
    }
}
```

#### 3.2.4 리스트 기반 효과 실행

```csharp
/// <summary>
/// 검증된 TriggerData 리스트를 기반으로 효과 실행
/// </summary>
private void ExecuteEffectsWithDataList(
    IReadOnlyList<EffectData> effects,
    List<VFXTriggerData> triggerDataList,
    Vector2Int targetPos,
    GameContext context)
{
    Debug.Log($"[SpellEffectExecutor] Executing {effects.Count} effects on {triggerDataList.Count} validated targets");

    foreach (var effectData in effects)
    {
        try
        {
            ICardEffect effect = CreateEffectInstance(effectData);
            if (effect == null)
            {
                Debug.LogWarning($"[SpellEffectExecutor] Failed to create effect: {effectData.Type}");
                continue;
            }

            // 해당 효과에 적용 가능한 TriggerData 필터링
            var relevantTriggers = FilterTriggersForEffect(triggerDataList, effectData, targetPos);

            Debug.Log($"[SpellEffectExecutor] Effect {effectData.Type}: {relevantTriggers.Count} relevant targets");

            // 각 타겟에 효과 적용
            foreach (var triggerData in relevantTriggers)
            {
                // AttackSuccess 체크는 각 효과 내부에서 처리됨
                if (effect is IVFXAwareEffect vfxAwareEffect)
                {
                    vfxAwareEffect.ExecuteWithVFXData(triggerData.TargetGridPosition, context, triggerData);
                }
                else
                {
                    effect.Execute(triggerData.TargetGridPosition, context);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SpellEffectExecutor] Effect execution error: {ex.Message}");
        }
    }
}

/// <summary>
/// 특정 효과에 적용 가능한 TriggerData 필터링
/// </summary>
private List<VFXTriggerData> FilterTriggersForEffect(
    List<VFXTriggerData> triggerDataList,
    EffectData effectData,
    Vector2Int centerPos)
{
    var filtered = new List<VFXTriggerData>();

    foreach (var triggerData in triggerDataList)
    {
        // 1. 공격 성공 여부 체크
        if (!triggerData.AttackSuccess)
        {
            Debug.Log($"[SpellEffectExecutor] Target validation failed: {triggerData.ValidationFailureReason}");
            continue;
        }

        // 2. 범위 체크
        if (effectData.AffectedRange == 0)
        {
            // 단일 타겟: 중앙 위치만
            if (triggerData.TargetGridPosition == centerPos)
            {
                filtered.Add(triggerData);
            }
        }
        else
        {
            // 다중 타겟: 맨하탄 거리 기반
            int distance = Mathf.Abs(triggerData.TargetGridPosition.x - centerPos.x) +
                          Mathf.Abs(triggerData.TargetGridPosition.y - centerPos.y);

            if (distance <= effectData.AffectedRange)
            {
                filtered.Add(triggerData);
            }
        }

        // 3. 추가 필터: AffectedType (이미 GetAffectedUnits에서 필터링됨)
        // 필요시 추가 검증 로직
    }

    return filtered;
}
```

---

## 4. 실행 흐름 비교

### 4.1 Before: 기존 방식

```
[T=0.0s] ExecuteBatch
├─ sortedEffects = 우선순위 정렬
├─ vfxData = sortedEffects[0].VFXData
├─ targetObject = GetTargetObject(targetPos)  // 중앙 타겟만
└─ StartCoroutine(ExecuteWithVFX)

[T=0.0s] ExecuteWithVFX
├─ VFX 인스턴스 생성
└─ trigger.Initialize(triggerTime, callback, targetObject)

[T=2.0s] VFX 트리거 시점
└─ ValidatePredeterminedTarget(targetObject)
   ├─ null, activeInHierarchy, IsAlive 체크
   └─ triggerData.SetTargetValid(targetObject, gridPos)

[T=2.0s] callback 호출
└─ ExecuteEffectsWithData(effects, targetPos, context, triggerData)
   └─ foreach effectData:
      └─ if (AffectedRange > 0):
         └─ ExecuteMultiTargetEffect()

[T=2.01s] ExecuteMultiTargetEffect
├─ affectedUnits = GetAffectedUnits()  // 🚨 이 시점에 계산!
└─ foreach targetUnit:
   ├─ targetTriggerData = new VFXTriggerData()
   ├─ 🚨 SetTargetValid(targetUnit) 무조건 호출
   └─ effect.ExecuteWithVFXData(..., targetTriggerData)
```

**문제점**:
- ❌ 다중 타겟 검증 시점: T=2.01s (VFX 트리거 이후)
- ❌ 무조건 `SetTargetValid` 호출
- ❌ 시간 차이로 인한 상태 불일치 가능

### 4.2 After: 제안된 방식

```
[T=0.0s] ExecuteBatch
├─ sortedEffects = 우선순위 정렬
├─ vfxData = sortedEffects[0].VFXData
├─ 🆕 allPotentialTargets = CalculateAllPotentialTargets()
│  ├─ maxRange = effects.Max(e => e.AffectedRange)
│  ├─ unifiedType = DetermineUnifiedAffectedType(effects)
│  └─ GetAffectedUnits(targetPos, unifiedType, maxRange)
└─ StartCoroutine(ExecuteWithVFX(..., allPotentialTargets))

[T=0.0s] ExecuteWithVFX
├─ VFX 인스턴스 생성
└─ 🆕 trigger.Initialize(triggerTime, listCallback, allPotentialTargets)

[T=2.0s] VFX 트리거 시점 (원자적 검증!)
└─ ValidateAllTargets()
   ├─ triggerDataList = new List<VFXTriggerData>()
   └─ foreach target in predeterminedTargets:
      ├─ triggerData = new VFXTriggerData()
      ├─ ValidateSingleTarget(target, triggerData)
      │  ├─ null, activeInHierarchy, IsAlive 체크
      │  └─ SetTargetValid() OR SetTargetInvalid()
      └─ triggerDataList.Add(triggerData)

[T=2.0s] listCallback 호출
└─ 🆕 ExecuteEffectsWithDataList(effects, triggerDataList)
   └─ foreach effectData:
      ├─ relevantTriggers = FilterTriggersForEffect()
      │  ├─ AttackSuccess 체크
      │  └─ AffectedRange 거리 필터링
      └─ foreach triggerData in relevantTriggers:
         └─ effect.ExecuteWithVFXData(..., triggerData)
```

**개선점**:
- ✅ 모든 타겟 검증 시점: T=2.0s (VFX 트리거와 동시)
- ✅ 각 타겟 개별 검증 (ValidateSingleTarget)
- ✅ 원자적 스냅샷 보장
- ✅ 검증 실패 시 `AttackSuccess = false` 정확히 설정

---

## 5. 장단점 분석

### 5.1 장점

#### 1. 원자적 검증 (Atomic Validation) ⭐
```
모든 타겟의 상태를 단일 시점에 검증
→ VFX 트리거 순간의 게임 상태 스냅샷
→ 시간 차이로 인한 불일치 제거
```

**시나리오**:
```
2초 VFX, 범위 내 3명의 적

기존 방식:
T=0s    VFX 시작
T=1.5s  적2가 다른 공격에 사망
T=2.0s  VFX 트리거 → 중앙 타겟만 검증
T=2.01s GetAffectedUnits() → 적1, 적2(사망), 적3
T=2.02s 적2 검증 없이 SetTargetValid → 사망한 적에게 효과 적용! ❌

제안 방식:
T=0s    VFX 시작, [적1, 적2, 적3] 리스트 결정
T=1.5s  적2가 다른 공격에 사망
T=2.0s  VFX 트리거 → [적1, 적2, 적3] 동시 검증
        └─ 적1: IsAlive=true → SetTargetValid ✅
        └─ 적2: IsAlive=false → SetTargetInvalid ✅
        └─ 적3: IsAlive=true → SetTargetValid ✅
T=2.01s 적1, 적3만 효과 적용 ✅
```

#### 2. 단일 검증 지점 (Single Source of Truth)
```
VFXEventTrigger만 검증 담당
→ 중복 코드 완전 제거
→ 유지보수성 향상
```

#### 3. 책임 명확화 (Clear Responsibility)
```
VFXEventTrigger:
├─ VFX 타이밍 제어
└─ 타겟 검증 (VFX가 영향을 주는 대상 판단)

SpellEffectExecutor:
├─ 효과 실행 중재
└─ 검증된 데이터 기반 효과 적용
```

#### 4. 일관성 보장 (Consistency)
```
단일 타겟 = VFX 트리거 시점 검증
다중 타겟 = VFX 트리거 시점 검증
→ 동일한 검증 시점, 동일한 로직
```

#### 5. 확장성 (Extensibility)
```
추가 검증 로직 (회피, 무적, 실드 등)
→ ValidateSingleTarget 메서드 한 곳만 수정
→ 모든 타겟에 자동 적용
```

### 5.2 단점

#### 1. 구조 대폭 변경
```
변경 범위:
├─ VFXEventTrigger.Initialize 시그니처
├─ 콜백 타입: Action<VFXTriggerData> → Action<List<VFXTriggerData>>
├─ SpellEffectExecutor.ExecuteWithVFX
├─ ExecuteEffectsWithData → ExecuteEffectsWithDataList
└─ 기존 호출 코드 모두 수정
```

**영향**:
- 기존 코드 호환성 깨짐
- 테스트 케이스 재작성 필요
- 리팩토링 비용 발생

#### 2. 사전 계산 복잡도
```csharp
// 효과마다 다른 AffectedType 처리
Effect1: AffectedType.Enemy, Range 2
Effect2: AffectedType.Ally, Range 1
Effect3: AffectedType.Any, Range 0

// 해결: 최대 범위 + 통합 타입
maxRange = 2
unifiedType = Any  // 가장 포괄적
```

**문제**:
- AffectedType 통합 로직 필요
- 최대 범위 계산 오버헤드

#### 3. 불필요한 검증
```
Effect1: Range 2 → 5명 영향
Effect2: Range 0 → 1명 영향

현재: 모든 5명을 검증
→ Effect2는 1명만 필요한데 5명 검증
→ 성능 오버헤드 (소규모)
```

#### 4. 필터링 로직 필요
```csharp
// SpellEffectExecutor에서 각 효과마다 필터링
relevantTriggers = triggerDataList.Where(td =>
    td.AttackSuccess &&
    IsWithinEffectRange(td.TargetGridPosition, effectData)
);
```

**추가 작업**:
- 각 효과별 범위 필터링 로직 필요
- 필터링 성능 고려 필요

**중요**: 턴제 그리드 전투 시스템의 특성상 유닛 이동 고려 불필요
- VFX 재생 중(T=0s ~ T=2.0s) 유닛은 그리드 셀에 고정됨
- 따라서 T=0s에 계산된 범위 내 타겟 리스트는 T=2.0s에도 유효
- FilterTriggersForEffect의 거리 재계산은 각 효과의 범위에 맞게 필터링하기 위함 (유닛 이동 때문이 아님)

### 5.3 성능 영향 분석

#### 계산 복잡도
```
기존:
├─ GetTargetObject: O(1)
├─ ValidatePredeterminedTarget: O(1)
└─ ExecuteMultiTargetEffect:
   ├─ GetAffectedUnits: O(N*M) [N=범위, M=유닛수]
   └─ foreach SetTargetValid: O(K) [K=영향받는 유닛수]
총합: O(N*M + K)

제안:
├─ CalculateAllPotentialTargets:
│  └─ GetAffectedUnits: O(N*M)
├─ ValidateAllTargets: O(K)
└─ FilterTriggersForEffect: O(K*E) [E=효과수]
총합: O(N*M + K*E)
```

**분석**:
- GetAffectedUnits 호출 시점만 변경 (T=0.0s vs T=2.01s)
- 검증 복잡도는 동일 (O(K))
- 필터링 추가: O(K*E) (일반적으로 E는 작음, 1-3개)

**결론**: 성능 영향 미미, 정확성 향상 가치가 더 큼

#### 유닛 이동과 범위 계산
```
턴제 그리드 전투 시스템의 전제 조건:
├─ 유닛은 그리드 셀에 고정됨
├─ VFX 재생 중에는 이동하지 않음
└─ 공격 선언 → VFX 재생 → 효과 적용 (유닛 정지)

따라서:
T=0s:   GetAffectedUnits(range) → [적1, 적2, 적3]
T=2.0s: ValidateAllTargets()
        → [적1, 적2, 적3]의 생존 여부만 검증
        → 위치 변경 없음, 범위 재계산 불필요

FilterTriggersForEffect의 거리 재계산 목적:
→ 유닛 이동 대응 ❌
→ 각 효과의 AffectedRange에 맞게 필터링 ✅
   (예: Effect1은 Range 2, Effect2는 Range 1)
```

---

## 6. 구현 계획

### 6.1 Phase 1: VFXEventTrigger 리팩토링

#### Step 1.1: 새로운 Initialize 메서드 추가
```csharp
// 기존 메서드 유지 (하위 호환성)
[Obsolete("Use Initialize(float, Action<List<VFXTriggerData>>, List<GameObject>, IGridManager) instead")]
public void Initialize(
    float triggerNormalizedTime,
    Action<VFXTriggerData> onTrigger,
    GameObject predeterminedTarget)
{
    // 단일 타겟을 리스트로 변환하여 새 메서드 호출
    var targetList = predeterminedTarget != null
        ? new List<GameObject> { predeterminedTarget }
        : new List<GameObject>();

    Initialize(triggerNormalizedTime, (list) => {
        if (list.Count > 0)
            onTrigger?.Invoke(list[0]);
    }, targetList, null);
}

// 새로운 메서드
public void Initialize(
    float triggerNormalizedTime,
    Action<List<VFXTriggerData>> onTrigger,
    List<GameObject> predeterminedTargets,
    IGridManager gridManager = null)
{
    // 구현
}
```

#### Step 1.2: ValidateAllTargets 구현
```csharp
private List<VFXTriggerData> ValidateAllTargets() { /* ... */ }
private void ValidateSingleTarget(GameObject target, VFXTriggerData triggerData) { /* ... */ }
```

#### Step 1.3: 트리거 로직 수정
```csharp
private void OnParticleSystemStopped()
{
    if (onTriggerCallbackList == null) return;

    var triggerDataList = ValidateAllTargets();
    onTriggerCallbackList.Invoke(triggerDataList);
}
```

### 6.2 Phase 2: SpellEffectExecutor 리팩토링

#### Step 2.1: CalculateAllPotentialTargets 구현
```csharp
private List<GameObject> CalculateAllPotentialTargets(...) { /* ... */ }
private AffectedType DetermineUnifiedAffectedType(...) { /* ... */ }
```

#### Step 2.2: ExecuteWithVFX 수정
```csharp
// 새로운 시그니처
private IEnumerator ExecuteWithVFX(
    IReadOnlyList<EffectData> effects,
    VFXData vfxData,
    Vector2Int targetPos,
    List<GameObject> potentialTargets,
    GameContext context)
```

#### Step 2.3: ExecuteEffectsWithDataList 구현
```csharp
private void ExecuteEffectsWithDataList(...) { /* ... */ }
private List<VFXTriggerData> FilterTriggersForEffect(...) { /* ... */ }
```

### 6.3 Phase 3: 테스트 및 검증

#### Test Case 1: 단일 타겟
```csharp
[Test]
public void SingleTarget_Validation_Success()
{
    // Given: 단일 유효한 타겟
    var targetUnit = CreateAliveUnit(new Vector2Int(1, 1));

    // When: VFX 트리거
    var triggerDataList = ExecuteVFXWithTargets(new List<GameObject> { targetUnit });

    // Then: 1개의 유효한 TriggerData
    Assert.AreEqual(1, triggerDataList.Count);
    Assert.IsTrue(triggerDataList[0].AttackSuccess);
}
```

#### Test Case 2: 다중 타겟 - 일부 사망
```csharp
[Test]
public void MultiTarget_PartialDead_FilteredCorrectly()
{
    // Given: 3개 타겟 (1명 사망)
    var unit1 = CreateAliveUnit(new Vector2Int(1, 1));
    var unit2 = CreateDeadUnit(new Vector2Int(1, 2));
    var unit3 = CreateAliveUnit(new Vector2Int(1, 3));

    // When: VFX 트리거
    var triggerDataList = ExecuteVFXWithTargets(new List<GameObject> { unit1, unit2, unit3 });

    // Then: 3개 TriggerData, 2개만 AttackSuccess=true
    Assert.AreEqual(3, triggerDataList.Count);
    Assert.IsTrue(triggerDataList[0].AttackSuccess);
    Assert.IsFalse(triggerDataList[1].AttackSuccess);  // 사망
    Assert.IsTrue(triggerDataList[2].AttackSuccess);
}
```

#### Test Case 3: 시간 차이 검증
```csharp
[Test]
public void AtomicValidation_StateConsistency()
{
    // Given: VFX 시작 시점에 생존한 타겟
    var targetUnit = CreateAliveUnit(new Vector2Int(1, 1));
    var targets = new List<GameObject> { targetUnit };

    // When: VFX 재생 중 타겟 사망 시뮬레이션
    StartVFXWithDelay(targets, onDelay: () => {
        targetUnit.GetComponent<HealthComponent>().TakeDamage(9999);
    });

    // Then: VFX 트리거 시점에 검증 → 사망 상태 반영
    var triggerDataList = WaitForVFXTrigger();
    Assert.IsFalse(triggerDataList[0].AttackSuccess);
    Assert.AreEqual("Target is not alive", triggerDataList[0].ValidationFailureReason);
}
```

#### Test Case 4: 범위 필터링
```csharp
[Test]
public void FilterTriggersForEffect_RangeFiltering()
{
    // Given: 범위 1 효과, 거리 0/1/2에 타겟
    var center = new Vector2Int(2, 2);
    var unit1 = CreateUnit(new Vector2Int(2, 2));  // 거리 0
    var unit2 = CreateUnit(new Vector2Int(2, 3));  // 거리 1
    var unit3 = CreateUnit(new Vector2Int(2, 4));  // 거리 2

    var effectData = new EffectData { AffectedRange = 1 };
    var triggerDataList = CreateValidatedList(unit1, unit2, unit3);

    // When: 범위 필터링
    var filtered = FilterTriggersForEffect(triggerDataList, effectData, center);

    // Then: 거리 0, 1만 포함
    Assert.AreEqual(2, filtered.Count);
}
```

---

## 7. 마이그레이션 전략

### 7.1 하위 호환성 유지

#### Obsolete 패턴
```csharp
[Obsolete("Use Initialize with List<GameObject> for multi-target support", false)]
public void Initialize(
    float triggerNormalizedTime,
    Action<VFXTriggerData> onTrigger,
    GameObject predeterminedTarget)
{
    // 기존 호출을 새 메서드로 위임
    var targetList = predeterminedTarget != null
        ? new List<GameObject> { predeterminedTarget }
        : new List<GameObject>();

    Initialize(triggerNormalizedTime, (list) => {
        if (list.Count > 0)
            onTrigger?.Invoke(list[0]);
        else
            onTrigger?.Invoke(new VFXTriggerData());
    }, targetList, null);
}
```

### 7.2 점진적 마이그레이션

#### Stage 1: 새 API 추가 (기존 코드 유지)
- 새로운 Initialize 메서드 추가
- 기존 메서드는 Obsolete로 표시

#### Stage 2: SpellEffectExecutor 전환
- ExecuteBatch에서 새 API 사용
- 기존 단일 타겟 코드 제거

#### Stage 3: 테스트 및 검증
- 모든 테스트 케이스 통과 확인
- 성능 벤치마크 확인

#### Stage 4: 기존 API 제거
- Obsolete 메서드 제거
- 코드 정리

---

## 8. 추가 고려사항

### 8.1 회피 시스템 통합

현재 설계는 회피 시스템을 쉽게 추가할 수 있습니다:

```csharp
private void ValidateSingleTarget(GameObject target, VFXTriggerData triggerData)
{
    // 기존 검증 (Null, activeInHierarchy, IsAlive)...

    // 🆕 회피 시스템 추가
    var evasionComponent = target.GetComponent<EvasionComponent>();
    if (evasionComponent != null && evasionComponent.ShouldEvade())
    {
        triggerData.SetTargetInvalid(target, "Target evaded");
        return;
    }

    // 🆕 무적 상태 체크
    var statusComponent = target.GetComponent<StatusEffectComponent>();
    if (statusComponent != null && statusComponent.HasStatus(StatusType.Invincible))
    {
        triggerData.SetTargetInvalid(target, "Target is invincible");
        return;
    }

    // 🆕 위치 변경 체크는 불필요
    // → 턴제 그리드 시스템에서 VFX 재생 중 유닛은 이동하지 않음
    // → T=0s에 계산된 범위 내 타겟은 T=2.0s에도 동일한 위치

    triggerData.SetTargetValid(target, gridPos);
}
```

**중요**: 유닛 위치 검증 불필요
- 턴제 전투 시스템에서 유닛은 그리드 셀에 고정
- VFX 재생 중(T=0s ~ T=2.0s) 위치 변경 없음
- ValidateSingleTarget은 **생존 여부 및 상태 효과**만 검증

### 8.2 디버깅 지원

```csharp
public class VFXTriggerData
{
    // 기존 필드...

    // 🆕 디버깅 정보
    public float ValidationTimestamp { get; private set; }
    public string DebugInfo => $"Target:{PredeterminedTarget?.name}, Success:{AttackSuccess}, Reason:{ValidationFailureReason}";

    public void SetTargetValid(GameObject target, Vector2Int gridPos)
    {
        ValidationTimestamp = Time.time;
        // ...
    }
}

// SpellEffectExecutor에서 사용
private void ExecuteEffectsWithDataList(...)
{
    Debug.Log($"[VFX Trigger] Validated {triggerDataList.Count} targets at T={triggerDataList[0].ValidationTimestamp:F3}s");
    foreach (var data in triggerDataList)
    {
        Debug.Log($"  - {data.DebugInfo}");
    }
}
```

### 8.3 성능 최적화

#### 타겟 리스트 재사용
```csharp
// ObjectPool 패턴
private static readonly ObjectPool<List<GameObject>> TargetListPool = new ObjectPool<List<GameObject>>(
    createFunc: () => new List<GameObject>(),
    actionOnGet: list => list.Clear(),
    actionOnRelease: list => list.Clear(),
    actionOnDestroy: list => list.Clear()
);

private List<GameObject> CalculateAllPotentialTargets(...)
{
    var targetList = TargetListPool.Get();

    // 계산 로직...

    return targetList;
}

// 사용 후 반환
private void OnVFXComplete()
{
    TargetListPool.Release(cachedTargetList);
}
```

---

## 9. 결론

### 9.1 핵심 가치

이 설계의 핵심은 **"시간적 일관성 보장"**입니다:

```
VFX 트리거 순간 = 모든 타겟 검증 순간
→ 게임 상태의 원자적 스냅샷
→ 효과 적용의 정확성 보장
```

### 9.1.1 턴제 그리드 전투 시스템 전제

이 설계는 다음 조건을 전제로 합니다:

```
턴제 그리드 전투 시스템:
├─ 유닛은 그리드 셀에 고정됨
├─ VFX 재생 중 유닛 이동 없음
└─ 공격 선언 → VFX 재생 → 효과 적용 (유닛 정지 상태)

타겟 범위 결정:
T=0s:   CalculateAllPotentialTargets()
        → GetAffectedUnits(centerPos, maxRange)
        → 범위 내 모든 타겟 리스트 고정

타겟 검증 (T=2.0s):
→ 생존 여부 (IsAlive) ✅
→ 활성화 상태 (activeInHierarchy) ✅
→ 상태 효과 (회피, 무적) ✅
→ 위치 변경 체크 ❌ (이동 없으므로 불필요)

따라서:
- T=0s의 범위 내 타겟 = T=2.0s의 범위 내 타겟
- 범위를 벗어난 유닛 발생 가능성 없음
- 새로 범위에 진입한 유닛 발생 가능성 없음
```

### 9.2 설계 품질

| 품질 속성 | 평가 | 설명 |
|---------|------|------|
| 정확성 | ⭐⭐⭐⭐⭐ | 원자적 검증으로 상태 불일치 제거 |
| 일관성 | ⭐⭐⭐⭐⭐ | 단일/다중 타겟 동일한 검증 시점 |
| 유지보수성 | ⭐⭐⭐⭐☆ | 단일 검증 지점, 확장 용이 |
| 성능 | ⭐⭐⭐⭐☆ | 약간의 오버헤드, 실용적 수준 |
| 구현 복잡도 | ⭐⭐⭐☆☆ | 구조 변경 필요, 하위 호환 가능 |

### 9.3 추천 의견

**강력 추천**: 이 설계는 단순한 버그 수정을 넘어, 시스템의 근본적인 정확성을 향상시킵니다.

**구현 우선순위**:
1. 🔴 **즉시**: 회피 시스템 등 개별 검증이 필수적인 경우
2. 🟡 **중기**: 정확성이 중요하지만 즉시 필요하지 않은 경우
3. 🟢 **장기**: 리팩토링 계획의 일부로 포함

**대안과의 비교**:
- 방법 1 (SpellExecutor 검증): 빠르지만 시점 불일치
- 방법 2 (별도 Validator): SRP 준수하지만 시점 불일치
- **제안 방식**: 구조 변경 필요하지만 근본적 해결

---

## 10. 다음 단계

### 10.1 즉시 조치
1. ✅ 이 문서 검토 및 승인
2. ⏳ VFXEventTrigger 리팩토링 시작
3. ⏳ 테스트 케이스 작성

### 10.2 향후 계획
1. ⏳ 회피 시스템 통합
2. ⏳ 성능 프로파일링
3. ⏳ 추가 검증 로직 (무적, 실드 등)

---

**문서 버전**: 1.1
**최종 수정**: 2025-10-07 (턴제 시스템 전제 조건 명시)
**작성자**: Claude Code Agent
**검토 상태**: 대기 중

---

## 변경 이력

### v1.1 (2025-10-07)
- **추가**: 턴제 그리드 전투 시스템 전제 조건 명시 (섹션 9.1.1)
- **명확화**: FilterTriggersForEffect의 거리 재계산 목적 설명 (유닛 이동 대응 아님)
- **명확화**: ValidateSingleTarget은 생존 여부 및 상태 효과만 검증, 위치 검증 불필요
- **추가**: 유닛 이동과 범위 계산 관계 설명 (섹션 5.3)

### v1.0 (2025-10-07)
- 초기 문서 작성
- VFXEventTrigger 기반 다중 타겟 원자적 검증 시스템 설계
