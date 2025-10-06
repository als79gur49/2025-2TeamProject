# VFX 기반 스펠 카드 효과 시스템 설계서

## 📋 개요

**목적**: 스펠 카드 효과가 VFX에 의해 트리거되도록 설계하여, 시각적 연출과 게임 로직을 동기화

**현재 문제점**:
- 카드 소환 시 효과가 즉시 적용됨
- VFX 재생과 효과 적용이 분리되어 부자연스러움
- `PlayVisualEffect()`가 단순히 프리팹 생성만 수행
- 복수 효과를 가진 카드의 VFX 처리 미정의

**요구사항**:
```
카드 사용 → VFX 재생 → VFX 트리거 포인트 도달 → 실제 효과 적용
```

**Version**: 1.1 (2025-10-06 업데이트)
- 복수 효과 일괄 처리 개선
- 생명주기 관리 강화 (타임아웃, 루프 VFX 처리)
- 에러 핸들링 및 Fallback 메커니즘 강화
- VFXEventTrigger 초기화 구조 개선

---

## 🎯 설계 방향

### 선택한 방식: **하이브리드 이벤트 기반 시스템**

**핵심 컴포넌트**:
1. **VFXEventTrigger**: VFX 프리팹에 부착, 트리거 타이밍 정의
2. **SpellEffectExecutor**: VFX와 효과 실행을 연결하는 중재자 (Mediator)
3. **EffectData 확장**: VFX 트리거 설정 추가

**장점**:
- ✅ 기존 코드 호환성 유지 (VFX 없는 효과도 동작)
- ✅ VFX 타이밍을 디자이너가 Inspector에서 제어 가능
- ✅ VFX 에셋과 로직 분리 (재사용성 향상)
- ✅ Animation Event, Particle System, VFX Graph 모두 지원
- ✅ 확장 가능 (여러 트리거 포인트, 복합 효과 지원)

---

## 🏗️ 시스템 아키텍처

### 실행 흐름

```
1. CardData.ExecuteCard(targetPos, context)
   │
   ├─> foreach effect in effectDataList
   │   │
   │   ├─> CardEffectFactory.CreateEffect(effectData)
   │   │
   │   ├─> effectData.EffectPrefab이 있는가?
   │   │   │
   │   │   ├─ YES → VFX 재생 경로
   │   │   │   │
   │   │   │   ├─> SpellEffectExecutor 생성 (동적 GameObject)
   │   │   │   ├─> VFX Instantiate at targetPosition
   │   │   │   ├─> VFXEventTrigger 찾기/생성
   │   │   │   ├─> VFXEventTrigger.OnEffectTrigger += ExecuteEffect
   │   │   │   └─> VFX 재생 시작
   │   │   │       │
   │   │   │       └─> [VFX 타이밍 도달]
   │   │   │           └─> VFXEventTrigger.OnEffectTrigger 발생
   │   │   │               └─> SpellEffectExecutor.ExecuteEffect()
   │   │   │                   └─> ICardEffect.Execute(targetPos, context)
   │   │   │
   │   │   └─ NO → 즉시 실행 경로
   │   │       └─> ICardEffect.Execute(targetPos, context)
```

---

## 📦 컴포넌트 설계

### 1. VFXEventTrigger Component

**역할**: VFX 프리팹에 부착되어 효과 트리거 타이밍을 정의

**위치**: `/Assets/Script/Game/VFX/VFXEventTrigger.cs`

```csharp
using UnityEngine;
using System;

namespace Game.VFX
{
    /// <summary>
    /// VFX 이펙트에서 게임 효과를 트리거하는 컴포넌트
    /// VFX 프리팹에 부착하여 특정 타이밍에 효과 실행을 알림
    /// </summary>
    public class VFXEventTrigger : MonoBehaviour
    {
        public enum TriggerType
        {
            Time,              // 절대 시간 (초)
            NormalizedTime,    // 정규화된 시간 (0-1)
            ParticleCount,     // 파티클 개수 기준
            AnimationEvent,    // Animation Event 기반
            Manual             // 수동 호출
        }

        #region Serialized Fields

        [Header("트리거 설정")]
        [SerializeField] private TriggerType triggerType = TriggerType.NormalizedTime;

        [Tooltip("트리거 시점 (Time: 초, NormalizedTime: 0-1, ParticleCount: 개수)")]
        [SerializeField] private float triggerValue = 0.5f;

        [SerializeField] private bool autoTrigger = true;

        [Header("VFX 정보")]
        [Tooltip("VFX 전체 지속 시간 (초). 0이면 자동 계산")]
        [SerializeField] private float vfxDuration = 0f;

        [Header("디버그")]
        [SerializeField] private bool logTriggerEvents = true;

        #endregion

        #region Events

        /// <summary>
        /// 효과 트리거 이벤트 (게임 로직 실행 신호)
        /// </summary>
        public event Action OnEffectTrigger;

        #endregion

        #region Runtime State

        private float elapsedTime;
        private ParticleSystem particleSystem;
        private Animator animator;
        private bool hasTriggered = false;
        private float calculatedDuration;

        #endregion

        #region Public Methods

        /// <summary>
        /// 효과를 수동으로 트리거합니다
        /// </summary>
        public void TriggerEffect()
        {
            if (hasTriggered)
            {
                if (logTriggerEvents)
                    Debug.LogWarning($"[VFXEventTrigger] {gameObject.name}: Already triggered");
                return;
            }

            hasTriggered = true;
            OnEffectTrigger?.Invoke();

            if (logTriggerEvents)
                Debug.Log($"[VFXEventTrigger] {gameObject.name}: Effect triggered at {elapsedTime:F2}s");
        }

        /// <summary>
        /// VFX의 전체 지속 시간을 반환합니다
        /// </summary>
        public float GetVFXDuration()
        {
            if (vfxDuration > 0)
                return vfxDuration;

            if (calculatedDuration > 0)
                return calculatedDuration;

            // 자동 계산
            calculatedDuration = CalculateVFXDuration();
            return calculatedDuration;
        }

        /// <summary>
        /// 트리거 여부를 리셋합니다 (재사용 시)
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
            elapsedTime = 0f;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            particleSystem = GetComponent<ParticleSystem>();
            animator = GetComponent<Animator>();

            if (vfxDuration <= 0)
            {
                calculatedDuration = CalculateVFXDuration();
            }
        }

        private void Update()
        {
            if (!autoTrigger || hasTriggered)
                return;

            elapsedTime += Time.deltaTime;

            switch (triggerType)
            {
                case TriggerType.Time:
                    if (elapsedTime >= triggerValue)
                        TriggerEffect();
                    break;

                case TriggerType.NormalizedTime:
                    float duration = GetVFXDuration();
                    if (duration > 0 && elapsedTime / duration >= triggerValue)
                        TriggerEffect();
                    break;

                case TriggerType.ParticleCount:
                    if (particleSystem != null && particleSystem.particleCount >= triggerValue)
                        TriggerEffect();
                    break;
            }
        }

        #endregion

        #region Animation Event Handler

        /// <summary>
        /// Animation Event에서 호출되는 메서드
        /// Animator에서 "TriggerSpellEffect" 이벤트를 설정하면 호출됨
        /// </summary>
        public void OnAnimationTrigger()
        {
            if (triggerType == TriggerType.AnimationEvent)
            {
                TriggerEffect();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// VFX 지속 시간을 자동으로 계산합니다
        /// </summary>
        private float CalculateVFXDuration()
        {
            float duration = 2f; // 기본값

            // ParticleSystem 기반 계산
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                duration = main.duration + main.startLifetime.constantMax;
            }
            // Animator 기반 계산
            else if (animator != null && animator.runtimeAnimatorController != null)
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                if (clips.Length > 0)
                {
                    duration = clips[0].length;
                }
            }

            return duration;
        }

        #endregion

        #region Editor Utilities

#if UNITY_EDITOR
        private void OnValidate()
        {
            triggerValue = Mathf.Max(0f, triggerValue);
            vfxDuration = Mathf.Max(0f, vfxDuration);

            if (triggerType == TriggerType.NormalizedTime)
            {
                triggerValue = Mathf.Clamp01(triggerValue);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            // 트리거 진행도 시각화
            float progress = GetVFXDuration() > 0 ? elapsedTime / GetVFXDuration() : 0f;
            Gizmos.color = hasTriggered ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f * progress);
        }
#endif

        #endregion
    }
}
```

---

### 2. SpellEffectExecutor Component

**역할**: VFX와 효과 실행을 중재하는 임시 오브젝트

**위치**: `/Assets/Script/Game/Card/Effects/SpellEffectExecutor.cs`

```csharp
using UnityEngine;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// 스펠 카드 효과 실행을 관리하는 중재자 컴포넌트
    /// VFX와 게임 로직 실행을 연결하고 생명주기를 관리합니다
    /// </summary>
    public class SpellEffectExecutor : MonoBehaviour
    {
        #region Static Factory Method

        /// <summary>
        /// EffectData를 기반으로 효과를 실행합니다 (VFX 포함)
        /// </summary>
        /// <param name="effectData">효과 데이터</param>
        /// <param name="targetPos">목표 위치</param>
        /// <param name="context">게임 컨텍스트</param>
        public static void Execute(EffectData effectData, Vector2Int targetPos, GameContext context)
        {
            if (effectData == null || context == null || !context.IsValid())
            {
                Debug.LogError("[SpellEffectExecutor] Invalid effectData or context");
                return;
            }

            // CardEffectFactory로 ICardEffect 생성
            var effect = CardEffectFactory.CreateEffect(effectData);
            if (effect == null)
            {
                Debug.LogError($"[SpellEffectExecutor] Failed to create effect from {effectData}");
                return;
            }

            // VFX가 있는 경우 VFX 경로, 없으면 즉시 실행
            if (effectData.EffectPrefab != null)
            {
                ExecuteWithVFX(effect, effectData, targetPos, context);
            }
            else
            {
                ExecuteImmediately(effect, targetPos, context);
            }
        }

        #endregion

        #region Private Fields

        private ICardEffect cardEffect;
        private Vector2Int targetPosition;
        private GameContext gameContext;
        private GameObject vfxInstance;
        private VFXEventTrigger vfxTrigger;
        private bool effectExecuted = false;

        #endregion

        #region VFX Execution Path

        /// <summary>
        /// VFX를 재생하고 트리거를 대기하는 경로
        /// </summary>
        private static void ExecuteWithVFX(ICardEffect effect, EffectData effectData, Vector2Int targetPos, GameContext context)
        {
            // SpellEffectExecutor GameObject 생성
            var executorObj = new GameObject($"SpellEffectExecutor_{effect.EffectType}_{targetPos}");
            var executor = executorObj.AddComponent<SpellEffectExecutor>();

            executor.cardEffect = effect;
            executor.targetPosition = targetPos;
            executor.gameContext = context;

            // VFX 생성
            if (context.GridController == null)
            {
                Debug.LogError("[SpellEffectExecutor] GridController is null!");
                Destroy(executorObj);
                ExecuteImmediately(effect, targetPos, context);
                return;
            }

            Vector3 worldPos = context.GridController.GridToWorldPosition(targetPos);
            executor.vfxInstance = Instantiate(effectData.EffectPrefab, worldPos, Quaternion.identity);

            if (executor.vfxInstance == null)
            {
                Debug.LogError("[SpellEffectExecutor] Failed to instantiate VFX prefab!");
                Destroy(executorObj);
                ExecuteImmediately(effect, targetPos, context);
                return;
            }

            // VFXEventTrigger 연결
            executor.vfxTrigger = executor.vfxInstance.GetComponent<VFXEventTrigger>();
            if (executor.vfxTrigger == null)
            {
                // VFXEventTrigger가 없으면 자동 생성 (기본 설정)
                executor.vfxTrigger = executor.vfxInstance.AddComponent<VFXEventTrigger>();
                Debug.LogWarning($"[SpellEffectExecutor] VFXEventTrigger not found on {effectData.EffectPrefab.name}, added default trigger");
            }

            // 트리거 이벤트 구독
            executor.vfxTrigger.OnEffectTrigger += executor.OnVFXTrigger;

            // VFX 자동 파괴 타이머
            float vfxDuration = executor.vfxTrigger.GetVFXDuration();
            Destroy(executor.vfxInstance, vfxDuration + 1f);
            Destroy(executorObj, vfxDuration + 2f);

            Debug.Log($"[SpellEffectExecutor] VFX started for {effect.EffectType} at {targetPos}, duration: {vfxDuration:F2}s");
        }

        /// <summary>
        /// VFX 트리거 이벤트 핸들러
        /// VFXEventTrigger에서 호출됨
        /// </summary>
        private void OnVFXTrigger()
        {
            if (effectExecuted)
            {
                Debug.LogWarning($"[SpellEffectExecutor] Effect already executed for {cardEffect?.EffectType}");
                return;
            }

            effectExecuted = true;

            Debug.Log($"[SpellEffectExecutor] VFX triggered, executing effect: {cardEffect?.EffectType}");

            // 실제 효과 실행
            ExecuteImmediately(cardEffect, targetPosition, gameContext);
        }

        #endregion

        #region Immediate Execution Path

        /// <summary>
        /// 효과를 즉시 실행합니다 (VFX 없음)
        /// </summary>
        private static void ExecuteImmediately(ICardEffect effect, Vector2Int targetPos, GameContext context)
        {
            if (effect == null)
            {
                Debug.LogError("[SpellEffectExecutor] Effect is null");
                return;
            }

            if (!effect.CanExecute(targetPos, context))
            {
                Debug.LogWarning($"[SpellEffectExecutor] Cannot execute {effect.EffectType} at {targetPos}");
                return;
            }

            effect.Execute(targetPos, context);
            Debug.Log($"[SpellEffectExecutor] Executed {effect.EffectType} immediately at {targetPos}");
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (vfxTrigger != null)
            {
                vfxTrigger.OnEffectTrigger -= OnVFXTrigger;
            }

            // 효과가 실행되지 않았다면 강제 실행 (안전 장치)
            if (!effectExecuted && cardEffect != null)
            {
                Debug.LogWarning($"[SpellEffectExecutor] Force executing {cardEffect.EffectType} on destroy");
                ExecuteImmediately(cardEffect, targetPosition, gameContext);
            }
        }

        #endregion
    }
}
```

---

### 3. EffectData 확장 (선택적)

**위치**: `/Assets/Script/Game/Card/Effects/EffectData.cs` (기존 파일 수정)

```csharp
[Header("VFX 트리거 설정")]
[Tooltip("VFX 재생 후 효과를 실행할 타이밍 (0-1 정규화)")]
[SerializeField] private float vfxTriggerNormalizedTime = 0.5f;

[Tooltip("VFX 트리거를 기다릴지 여부 (false면 즉시 실행)")]
[SerializeField] private bool waitForVFX = true;

/// <summary>VFX 트리거 타이밍 (0-1)</summary>
public float VFXTriggerNormalizedTime => vfxTriggerNormalizedTime;

/// <summary>VFX 트리거 대기 여부</summary>
public bool WaitForVFX => waitForVFX;
```

**OnValidate 추가 검증**:
```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    // 기존 검증 코드...

    // VFX 트리거 타이밍 검증
    vfxTriggerNormalizedTime = Mathf.Clamp01(vfxTriggerNormalizedTime);
}
#endif
```

---

### 4. CardData.ExecuteEffects() 수정

**위치**: `/Assets/Script/Game/Data/CardData.cs` (기존 메서드 수정)

```csharp
/// <summary>
/// Phase 2.10 & VFX 시스템: 지정된 위치에서 카드의 모든 효과를 실행합니다
/// VFX가 있는 경우 VFX 트리거를 대기하고, 없으면 즉시 실행합니다
/// </summary>
/// <param name="targetPosition">목표 위치</param>
/// <param name="context">게임 컨텍스트 (서비스 참조)</param>
/// <returns>실행 요청된 효과 개수 (VFX 대기 중인 효과 포함)</returns>
public int ExecuteEffects(Vector2Int targetPosition, GameContext context)
{
    if (context == null || !context.IsValid())
    {
        Debug.LogError($"CardData[{cardName}]: 유효하지 않은 GameContext입니다.");
        return 0;
    }

    if (effectDataList == null || effectDataList.Count == 0)
    {
        Debug.LogWarning($"CardData[{cardName}]: 실행할 효과가 없습니다.");
        return 0;
    }

    int executedCount = 0;
    Debug.Log($"CardData[{cardName}]: {targetPosition}에서 {effectDataList.Count}개 효과 실행을 시작합니다.");

    foreach (var effectData in effectDataList)
    {
        if (effectData == null || !effectData.IsValid())
        {
            Debug.LogWarning($"CardData[{cardName}]: 유효하지 않은 EffectData를 건너뜁니다.");
            continue;
        }

        try
        {
            // SpellEffectExecutor로 위임 (VFX 처리 포함)
            SpellEffectExecutor.Execute(effectData, targetPosition, context);
            executedCount++;

            Debug.Log($"CardData[{cardName}]: {effectData.Type} 효과 실행 요청 완료 " +
                     $"(VFX: {(effectData.EffectPrefab != null ? "대기" : "즉시")})");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CardData[{cardName}]: {effectData.Type} 효과 실행 중 오류 발생: {ex.Message}");
        }
    }

    Debug.Log($"CardData[{cardName}]: 총 {executedCount}/{effectDataList.Count}개 효과 실행 요청이 완료되었습니다.");
    return executedCount;
}
```

---

### 5. ICardEffect 구현체 수정 (DamageEffect 예시)

**위치**: `/Assets/Script/Game/Card/Effects/DamageEffect.cs`

**변경 사항**: `PlayVisualEffect()` 메서드 제거

```csharp
public void Execute(Vector2Int targetPos, GameContext context)
{
    if (!CanExecute(targetPos, context))
    {
        Debug.LogWarning("DamageEffect: 실행 조건을 만족하지 않습니다.");
        return;
    }

    var affectedUnits = GetAffectedUnits(targetPos, context);
    var damageAmount = _effectData.Value;

    Debug.Log($"DamageEffect: {affectedUnits.Count}개 유닛에게 {damageAmount} 피해를 적용합니다.");

    foreach (var unit in affectedUnits)
    {
        ApplyDamageToUnit(unit, damageAmount);
    }

    // ❌ 제거: PlayVisualEffect(targetPos, context);
    // ✅ VFX 재생은 SpellEffectExecutor가 담당
}
```

**동일하게 적용**: `HealEffect.cs`, `SummonEffect.cs` 등 모든 ICardEffect 구현체

---

## 🎮 사용 예시

### 1. VFX 프리팹 설정

**Fireball VFX 프리팹 구성**:
```
Fireball_VFX (GameObject)
├─ ParticleSystem (발사 파티클)
├─ Light (빛 효과)
└─ VFXEventTrigger (컴포넌트)
   ├─ Trigger Type: NormalizedTime
   ├─ Trigger Value: 0.7 (70% 지점에서 폭발)
   ├─ Auto Trigger: ✓
   └─ VFX Duration: 1.5초
```

### 2. CardData 설정

**Inspector 설정**:
```
FireballCard (CardData)
├─ Card Name: "화염구"
├─ Effect Data List:
│  └─ [0] DamageEffect
│     ├─ Type: Damage
│     ├─ Value: 5
│     ├─ Affected Type: Enemy
│     ├─ Affected Range: 1
│     ├─ Effect Prefab: Fireball_VFX
│     ├─ VFX Trigger Normalized Time: 0.7
│     └─ Wait For VFX: ✓
```

### 3. 실행 흐름

```csharp
// 1. 카드 사용
CardData fireballCard = /* ... */;
Vector2Int targetPos = new Vector2Int(3, 5);
GameContext context = /* ... */;

// 2. 효과 실행 요청
fireballCard.ExecuteCard(targetPos, context);

// ↓ 내부 실행 과정

// 3. SpellEffectExecutor 생성
SpellEffectExecutor.Execute(effectData, targetPos, context);

// 4. VFX 재생 시작
GameObject vfx = Instantiate(Fireball_VFX, worldPos, Quaternion.identity);
VFXEventTrigger trigger = vfx.GetComponent<VFXEventTrigger>();

// 5. VFX 진행 (0.0s ~ 1.05s)
// - 0.0s: 화염구 발사
// - 0.7s: 70% 지점 도달
// - 1.05s (0.7 * 1.5s): VFXEventTrigger.OnEffectTrigger 발생

// 6. 효과 실행
SpellEffectExecutor.OnVFXTrigger()
→ DamageEffect.Execute(targetPos, context)
   → 범위 내 적에게 5 피해 적용
```

---

## 🔄 대안 설계 비교

### 옵션 1: Animation Event 방식
```
장점: Unity 기본 기능, 타임라인 기반 정확도
단점: Animator 필수, Particle System 부적합, VFX 에셋 코드 의존성
```

### 옵션 2: Callback 방식
```
장점: 간단한 구조, 모든 VFX 타입 지원
단점: 타이밍 정확도 낮음, 디자이너가 코드 수정 필요
```

### 옵션 3: 이벤트 기반 (채택안)
```
장점: VFX/로직 분리, 모든 VFX 타입 지원, Inspector 제어, 확장성
단점: 컴포넌트 추가 필요 (자동 생성으로 해결)
```

---

## 🔧 Version 1.1 개선 사항

### 1. 복수 효과 일괄 처리 (Multi-Effect Support)

**문제점**: 기존 설계는 각 EffectData마다 별도의 SpellEffectExecutor와 VFX를 생성하여 비효율적

**개선안**:
```csharp
// SpellEffectExecutor에 배치 실행 메서드 추가
public class SpellEffectExecutor : MonoBehaviour
{
    private List<ICardEffect> cardEffects = new List<ICardEffect>();

    public static void ExecuteBatch(
        List<EffectData> effectDataList,
        Vector2Int targetPos,
        GameContext context)
    {
        if (effectDataList == null || effectDataList.Count == 0) return;

        // VFX가 있는 첫 번째 효과를 대표로 선택
        var primaryEffect = effectDataList.FirstOrDefault(e => e.EffectPrefab != null);

        if (primaryEffect != null)
        {
            // 하나의 VFX로 모든 효과 트리거
            ExecuteWithVFXBatch(effectDataList, primaryEffect, targetPos, context);
        }
        else
        {
            // VFX 없이 모든 효과 즉시 실행
            ExecuteImmediateBatch(effectDataList, targetPos, context);
        }
    }

    private static void ExecuteWithVFXBatch(
        List<EffectData> effectDataList,
        EffectData primaryEffect,
        Vector2Int targetPos,
        GameContext context)
    {
        var executorObj = new GameObject($"SpellEffectExecutor_Batch_{targetPos}");
        var executor = executorObj.AddComponent<SpellEffectExecutor>();

        // 모든 효과의 ICardEffect 인스턴스 생성
        foreach (var effectData in effectDataList)
        {
            var effect = CardEffectFactory.CreateEffect(effectData);
            if (effect != null)
            {
                executor.cardEffects.Add(effect);
            }
        }

        executor.targetPosition = targetPos;
        executor.gameContext = context;

        // VFX 생성 및 트리거 설정
        Vector3 worldPos = context.GridController.GridToWorldPosition(targetPos);
        executor.vfxInstance = Instantiate(primaryEffect.EffectPrefab, worldPos, Quaternion.identity);

        executor.vfxTrigger = executor.vfxInstance.GetComponent<VFXEventTrigger>();
        if (executor.vfxTrigger == null)
        {
            executor.vfxTrigger = executor.vfxInstance.AddComponent<VFXEventTrigger>();
        }

        // 콜백 방식으로 연결 (이벤트 구독 대신)
        executor.vfxTrigger.Initialize(
            primaryEffect.VFXTriggerNormalizedTime,
            executor.ExecuteBatchEffects
        );

        // 생명주기 관리
        float vfxDuration = executor.vfxTrigger.GetVFXDuration();
        Destroy(executor.vfxInstance, vfxDuration + 1f);
        Destroy(executorObj, vfxDuration + 2f);
    }

    private void ExecuteBatchEffects()
    {
        if (effectExecuted) return;
        effectExecuted = true;

        // 모든 효과를 우선순위 순으로 실행
        var sortedEffects = cardEffects.OrderBy(e => e.Priority).ToList();

        foreach (var effect in sortedEffects)
        {
            if (effect.CanExecute(targetPosition, gameContext))
            {
                effect.Execute(targetPosition, gameContext);
            }
        }
    }
}
```

### 2. VFXEventTrigger 초기화 구조 개선

**문제점**: 이벤트 구독 방식은 구독 해제 관리가 복잡하고, EffectData 정보 전달 어려움

**개선안**:
```csharp
public class VFXEventTrigger : MonoBehaviour
{
    // 콜백 방식 추가 (이벤트와 병행)
    private System.Action onTriggerCallback;

    /// <summary>
    /// VFX 트리거 초기화 (콜백 방식)
    /// SpellEffectExecutor가 호출하여 설정
    /// </summary>
    public void Initialize(float normalizedTriggerTime, System.Action callback)
    {
        this.triggerType = TriggerType.NormalizedTime;
        this.triggerValue = normalizedTriggerTime;
        this.onTriggerCallback = callback;
        this.autoTrigger = true;

        if (logTriggerEvents)
            Debug.Log($"[VFXEventTrigger] Initialized with callback, trigger at {normalizedTriggerTime:P0}");
    }

    public void TriggerEffect()
    {
        if (hasTriggered)
        {
            if (logTriggerEvents)
                Debug.LogWarning($"[VFXEventTrigger] {gameObject.name}: Already triggered");
            return;
        }

        hasTriggered = true;

        // 콜백 우선 실행
        onTriggerCallback?.Invoke();

        // 기존 이벤트도 발생 (다른 시스템용)
        OnEffectTrigger?.Invoke();

        if (logTriggerEvents)
            Debug.Log($"[VFXEventTrigger] {gameObject.name}: Effect triggered at {elapsedTime:F2}s");
    }
}
```

### 3. 생명주기 관리 강화

**문제점**:
- VFX가 루프 설정일 경우 무한 대기 가능
- Executor가 영원히 남아있을 수 있음

**개선안**:
```csharp
public class SpellEffectExecutor : MonoBehaviour
{
    [Header("안전 장치")]
    [SerializeField] private float maxLifetime = 10f; // 최대 생존 시간
    [SerializeField] private bool debugMode = false;

    private float creationTime;

    private void Awake()
    {
        creationTime = Time.time;
    }

    private void Update()
    {
        // 타임아웃 체크
        if (Time.time - creationTime > maxLifetime)
        {
            if (debugMode)
                Debug.LogWarning($"[SpellEffectExecutor] Timeout after {maxLifetime}s, force executing");

            ForceExecuteAndDestroy();
        }
    }

    private void ForceExecuteAndDestroy()
    {
        // 아직 실행되지 않았다면 강제 실행
        if (!effectExecuted)
        {
            if (cardEffects != null && cardEffects.Count > 0)
            {
                ExecuteBatchEffects();
            }
        }

        // VFX 정리
        if (vfxInstance != null)
        {
            Destroy(vfxInstance);
        }

        // 자신 정리
        Destroy(gameObject);
    }
}
```

**VFX Duration 계산 개선**:
```csharp
private float CalculateVFXDuration()
{
    float duration = 2f; // 기본값

    // ParticleSystem 기반 계산
    if (particleSystem != null)
    {
        var main = particleSystem.main;

        // 루프 파티클 감지
        if (main.loop)
        {
            Debug.LogWarning($"[VFXEventTrigger] {gameObject.name} is looping, using default duration");
            return 3f; // 루프 VFX는 짧게 설정
        }

        duration = main.duration + main.startLifetime.constantMax;
    }
    // Animator 기반 계산
    else if (animator != null && animator.runtimeAnimatorController != null)
    {
        var clips = animator.runtimeAnimatorController.animationClips;
        if (clips.Length > 0)
        {
            duration = clips[0].length;
        }
    }

    return duration;
}
```

### 4. 에러 핸들링 및 Fallback 강화

**CardData.ExecuteEffects() 개선**:
```csharp
public int ExecuteEffects(Vector2Int targetPosition, GameContext context)
{
    if (context == null || !context.IsValid())
    {
        Debug.LogError($"CardData[{cardName}]: 유효하지 않은 GameContext입니다.");
        return 0;
    }

    if (effectDataList == null || effectDataList.Count == 0)
    {
        Debug.LogWarning($"CardData[{cardName}]: 실행할 효과가 없습니다.");
        return 0;
    }

    try
    {
        // 배치 실행으로 변경 (효율적)
        SpellEffectExecutor.ExecuteBatch(effectDataList, targetPosition, context);

        Debug.Log($"CardData[{cardName}]: {effectDataList.Count}개 효과 실행 요청 완료");
        return effectDataList.Count;
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"CardData[{cardName}]: 효과 실행 중 오류 발생: {ex.Message}\n{ex.StackTrace}");

        // Fallback: 각 효과를 개별적으로 즉시 실행
        Debug.LogWarning($"CardData[{cardName}]: Fallback 모드로 효과 실행 시도");
        return ExecuteEffectsFallback(targetPosition, context);
    }
}

/// <summary>
/// Fallback: VFX 없이 모든 효과를 즉시 실행
/// </summary>
private int ExecuteEffectsFallback(Vector2Int targetPosition, GameContext context)
{
    int executedCount = 0;

    foreach (var effectData in effectDataList)
    {
        try
        {
            var effect = CardEffectFactory.CreateEffect(effectData);
            if (effect != null && effect.CanExecute(targetPosition, context))
            {
                effect.Execute(targetPosition, context);
                executedCount++;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CardData[{cardName}]: Fallback 실행 중 오류: {ex.Message}");
        }
    }

    return executedCount;
}
```

---

## 📊 확장 가능성

### 1. 복합 트리거 지원
```csharp
// 여러 시점에 효과 발동 (추후 확장)
VFXEventTrigger[] triggers = vfx.GetComponents<VFXEventTrigger>();
triggers[0].triggerValue = 0.3f; // 30% 지점: 버프 적용
triggers[1].triggerValue = 0.7f; // 70% 지점: 데미지 적용
```

### 2. 체인 효과
```csharp
// SpellEffectExecutor 확장
public void OnVFXTrigger() {
    ExecuteEffect();

    // 다음 VFX 트리거
    if (nextEffectData != null) {
        SpellEffectExecutor.Execute(nextEffectData, nextTargetPos, context);
    }
}
```

---

## ✅ 구현 체크리스트

### Phase 1: 기본 시스템 (Version 1.1)
- [ ] `VFXEventTrigger.cs` 구현
  - [ ] Initialize(float, Action) 콜백 메서드 추가
  - [ ] 루프 VFX 감지 로직 추가
  - [ ] Duration 자동 계산 개선
- [ ] `SpellEffectExecutor.cs` 구현
  - [ ] ExecuteBatch() 정적 메서드 추가
  - [ ] 생명주기 타임아웃 안전 장치 추가
  - [ ] ForceExecuteAndDestroy() 메서드 구현
- [ ] `EffectData.cs` VFX 필드 추가 (선택적)
  - [ ] vfxTriggerNormalizedTime 필드
  - [ ] waitForVFX 필드
- [ ] `CardData.ExecuteEffects()` 수정
  - [ ] ExecuteBatch 호출로 변경
  - [ ] try-catch 에러 핸들링 추가
  - [ ] ExecuteEffectsFallback() 메서드 추가

### Phase 2: 효과 시스템 통합
- [ ] `DamageEffect.cs` VFX 제거
  - [ ] PlayVisualEffect() 메서드 삭제
- [ ] `HealEffect.cs` VFX 제거
  - [ ] PlayVisualEffect() 메서드 삭제
- [ ] `SummonEffect.cs` VFX 제거
  - [ ] PlayVisualEffect() 메서드 삭제

### Phase 3: VFX 에셋 설정
- [ ] Fireball VFX 프리팹 생성
  - [ ] VFXEventTrigger 컴포넌트 부착
  - [ ] Trigger Type: NormalizedTime (0.7)
  - [ ] VFX Duration 수동 설정
- [ ] Ice Spike VFX 프리팹 생성
  - [ ] VFXEventTrigger 컴포넌트 부착
- [ ] Heal Light VFX 프리팹 생성
  - [ ] VFXEventTrigger 컴포넌트 부착

### Phase 4: 테스트
- [ ] VFX 타이밍 테스트
  - [ ] 50%, 70%, 100% 트리거 지점 테스트
  - [ ] 루프 VFX 타임아웃 테스트
- [ ] 즉시 실행 경로 테스트 (VFX 없는 카드)
- [ ] 복합 효과 테스트
  - [ ] 데미지 + 힐 조합
  - [ ] 3개 이상 효과 배치 실행
- [ ] 에러 핸들링 테스트
  - [ ] VFX Prefab null 처리
  - [ ] GridController null 처리
  - [ ] Fallback 모드 동작 확인
- [ ] 생명주기 테스트
  - [ ] maxLifetime 타임아웃 확인
  - [ ] VFX 자동 파괴 확인

---

## 🐛 주의사항 및 Best Practices

### 1. VFX 지속 시간 설정 (중요!)
```csharp
// ⚠️ 루프 VFX는 반드시 Inspector에서 Duration 수동 설정
// - 자동 계산은 루프 감지 시 기본값(3초) 사용
// - 복잡한 SubEmitter가 있는 경우 수동 설정 권장

// ✅ 권장 방법:
// 1. VFX 프리팹에 VFXEventTrigger 부착
// 2. VFX Duration 필드에 실제 재생 시간 입력
// 3. Trigger Value를 0.5~0.7 사이로 설정 (효과 타이밍)
```

### 2. 효과 중복 실행 방지
```csharp
// ✅ Version 1.1 개선:
// - effectExecuted 플래그로 중복 방지
// - OnDestroy()에서 미실행 효과 강제 실행
// - maxLifetime 타임아웃 안전 장치

// ⚠️ 주의: 타임아웃 시간은 가장 긴 VFX보다 길게 설정
public float maxLifetime = 10f; // 기본 10초
```

### 3. 메모리 관리
```csharp
// ✅ 자동 파괴 메커니즘:
// - SpellEffectExecutor: VFX Duration + 2초 후 파괴
// - VFX Instance: VFX Duration + 1초 후 파괴
// - maxLifetime 도달 시 강제 파괴

// ⚠️ 디버그 시 Scene에 Executor 오브젝트 남아있으면:
// → VFX가 루프이거나 Duration 계산 오류
// → Inspector에서 직접 Duration 설정 필요
```

### 4. 복수 효과 카드 설정 (Version 1.1)
```yaml
# Inspector 설정 예시
FireStorm Card:
  Effect Data List:
    - [0] Damage Effect
      Type: Damage
      Value: 3
      Effect Prefab: FireStorm_VFX  # 대표 VFX
      VFX Trigger Normalized Time: 0.7
    - [1] Damage Effect (DoT)
      Type: Damage
      Value: 1
      Effect Prefab: null  # VFX 없음, [0]의 VFX 공유
    - [2] Debuff Effect
      Type: Debuff
      Value: 2
      Effect Prefab: null

# 결과: FireStorm_VFX 하나만 재생되고, 70% 지점에서 모든 효과 실행
```

### 5. 에러 디버깅 가이드

**증상**: 효과가 실행되지 않음
```
1. Console 확인:
   - "[SpellEffectExecutor] VFX started..." 로그 있는가?
   → 없으면 EffectPrefab null 또는 ExecuteBatch 호출 실패

2. VFXEventTrigger 확인:
   - "Effect triggered at..." 로그 있는가?
   → 없으면 TriggerValue 도달 전 VFX 파괴 (Duration 부족)

3. Hierarchy 확인:
   - SpellEffectExecutor_Batch 오브젝트가 남아있는가?
   → 있으면 타임아웃 미발생, maxLifetime 증가 필요
```

**증상**: VFX는 재생되는데 효과 적용 타이밍이 이상함
```
1. VFXTriggerNormalizedTime 값 확인:
   - 0.5 = VFX 50% 지점
   - 0.7 = VFX 70% 지점 (권장)
   - 1.0 = VFX 끝나는 시점

2. VFX Duration 확인:
   - 실제 VFX 재생 시간과 일치하는가?
   - 루프 VFX는 수동 설정 필수
```

**증상**: Fallback 모드가 계속 실행됨
```
1. GridController null 체크:
   - GameContext.GridController가 제대로 초기화되었는가?

2. VFX Prefab 경로 확인:
   - EffectPrefab에 올바른 프리팹이 할당되었는가?
   - 프리팹이 null이면 즉시 실행 경로로 분기 (정상)
```

---

## 📚 참고자료

- Unity Animation Events: https://docs.unity3d.com/Manual/script-AnimationWindowEvent.html
- Unity VFX Graph: https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest
- Unity Particle System: https://docs.unity3d.com/Manual/ParticleSystems.html
- 기존 UnitAnimationController 구조: [Assets/Script/Game/Components/UnitAnimationController.cs:266](Assets/Script/Game/Components/UnitAnimationController.cs#L266)

---

**작성일**: 2025-10-06
**최종 업데이트**: 2025-10-06
**버전**: 1.1
**상태**: 설계 완료 + Version 1.1 개선사항 추가 → 구현 대기

---

## 📝 변경 이력

### Version 1.1 (2025-10-06)
- ✅ 복수 효과 일괄 처리 (`ExecuteBatch`) 추가
- ✅ VFXEventTrigger 초기화 구조 개선 (콜백 방식)
- ✅ 생명주기 관리 강화 (타임아웃, 루프 VFX 처리)
- ✅ 에러 핸들링 및 Fallback 메커니즘 추가
- ✅ 디버깅 가이드 및 Best Practices 추가

### Version 1.0 (2025-10-06)
- 초기 설계 완료
- VFXEventTrigger, SpellEffectExecutor 컴포넌트 설계
- 이벤트 기반 시스템 아키텍처 정의
