# VFX Dynamic Data System Design

**Version**: 2.0
**Date**: 2025-10-06
**Status**: Design Phase - Clean Architecture (No Legacy)

---

## 개요

VFX 시스템에서 게임 로직으로 동적 정보를 전달하는 시스템입니다.
**핵심 요구사항**: 공격 성공/실패 여부를 VFX에서 게임 로직으로 전달해야 합니다.

**TCG 게임 특성**:
- 모든 타겟은 사전에 결정됨 (predetermined targets)
- 물리 충돌 감지 불필요 (no physics collision detection)
- 타겟 유효성 검증이 중요 (target validation critical)
- 공격 성공/실패가 게임 로직의 핵심 (attack success is critical)

---

## 1. VFXTriggerData 클래스

VFX 트리거 시점에 게임 로직으로 전달되는 동적 데이터입니다.

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// VFX 트리거 시점에 게임 로직으로 전달되는 동적 데이터
    /// TCG 게임 특성: 사전 결정된 타겟 유효성 검증 중심
    /// </summary>
    public class VFXTriggerData
    {
        #region Basic Trigger Info

        /// <summary>VFX 트리거가 발생한 월드 좌표</summary>
        public Vector3 TriggerWorldPosition { get; set; }

        /// <summary>VFX 재생 진행도 (0.0 ~ 1.0)</summary>
        public float NormalizedProgress { get; set; }

        /// <summary>트리거 발생 시간 (게임 시작 기준)</summary>
        public float TriggerTime { get; set; }

        #endregion

        #region TCG-Specific Target Validation

        /// <summary>타겟 공격 성공 여부 (TCG 핵심 정보)</summary>
        public bool AttackSuccess { get; set; }

        /// <summary>공격 대상 (사전 결정된 타겟)</summary>
        public GameObject PredeterminedTarget { get; set; }

        /// <summary>타겟의 그리드 좌표 (TCG 보드 기준)</summary>
        public Vector2Int? TargetGridPosition { get; set; }

        /// <summary>타겟 유효성 실패 이유 (디버깅용)</summary>
        public string ValidationFailureReason { get; set; }

        #endregion

        #region Multi-Target Support (AOE Effects)

        /// <summary>다중 타겟 리스트 (범위 효과용)</summary>
        public List<GameObject> ValidTargets { get; private set; }

        /// <summary>유효하지 않은 타겟 리스트 (디버깅/로그용)</summary>
        public List<GameObject> InvalidTargets { get; private set; }

        #endregion

        #region Custom Data (Future Extension)

        /// <summary>확장 가능한 커스텀 데이터 (파티클 카운트, 애니메이션 상태 등)</summary>
        public Dictionary<string, object> CustomData { get; private set; }

        #endregion

        #region Constructor

        public VFXTriggerData()
        {
            ValidTargets = new List<GameObject>();
            InvalidTargets = new List<GameObject>();
            CustomData = new Dictionary<string, object>();

            TriggerTime = Time.time;
            AttackSuccess = false; // Default: 실패 상태
        }

        #endregion

        #region Helper Methods

        /// <summary>타겟 유효성 검증 성공 설정</summary>
        public void SetTargetValid(GameObject target, Vector2Int gridPos)
        {
            AttackSuccess = true;
            PredeterminedTarget = target;
            TargetGridPosition = gridPos;
            ValidationFailureReason = null;

            if (!ValidTargets.Contains(target))
                ValidTargets.Add(target);
        }

        /// <summary>타겟 유효성 검증 실패 설정</summary>
        public void SetTargetInvalid(GameObject target, string reason)
        {
            AttackSuccess = false;
            PredeterminedTarget = target;
            ValidationFailureReason = reason;

            if (!InvalidTargets.Contains(target))
                InvalidTargets.Add(target);
        }

        /// <summary>커스텀 데이터 추가</summary>
        public void SetCustomData(string key, object value)
        {
            if (CustomData.ContainsKey(key))
                CustomData[key] = value;
            else
                CustomData.Add(key, value);
        }

        /// <summary>커스텀 데이터 조회</summary>
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (CustomData.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        #endregion
    }
}
```

---

## 2. VFXEventTrigger 클래스 (TriggerData 전용)

VFX 프리팹에 부착되어 특정 시점에 TriggerData를 전달하는 컴포넌트입니다.

```csharp
using System;
using UnityEngine;
using Game.Interfaces;
using Game.Services;

namespace Game.VFX
{
    /// <summary>
    /// VFX 프리팹에 부착되어 특정 시점에 TriggerData를 전달
    /// TCG 특성: 사전 결정된 타겟 유효성 검증
    /// </summary>
    public class VFXEventTrigger : MonoBehaviour
    {
        #region Trigger Configuration

        public enum TriggerType
        {
            Time,               // 절대 시간 (초 단위)
            NormalizedTime,     // VFX 재생 진행도 (0.0 ~ 1.0)
            ParticleCount,      // 파티클 시스템 카운트 기준
            AnimationEvent,     // Unity Animation Event
            Manual              // 수동 호출
        }

        [Header("Trigger Settings")]
        [SerializeField] private TriggerType triggerType = TriggerType.NormalizedTime;
        [SerializeField] private float triggerValue = 0.7f;

        [Header("Safety Settings")]
        [SerializeField] private float maxLifetime = 10f;

        [Header("Debug")]
        [SerializeField] private bool logTriggerEvents = true;

        #endregion

        #region Runtime State

        private Action<VFXTriggerData> onTriggerCallback;
        private GameObject predeterminedTarget;
        private IGridManager gridManager;

        private bool triggered = false;
        private bool destroyed = false;
        private float startTime;
        private float vfxDuration;

        private ParticleSystem[] particleSystems;

        #endregion

        #region Initialization

        /// <summary>
        /// VFX 트리거 초기화 (TriggerData 콜백 전용)
        /// </summary>
        /// <param name="normalizedTriggerTime">트리거 발생 정규화 시간 (0.0 ~ 1.0)</param>
        /// <param name="callback">TriggerData를 전달받는 콜백</param>
        /// <param name="target">사전 결정된 타겟 (TCG 특성)</param>
        public void Initialize(float normalizedTriggerTime, Action<VFXTriggerData> callback, GameObject target = null)
        {
            this.triggerType = TriggerType.NormalizedTime;
            this.triggerValue = Mathf.Clamp01(normalizedTriggerTime);
            this.onTriggerCallback = callback;
            this.predeterminedTarget = target;

            // GridManager 참조
            gridManager = ServiceLocator.Get<IGridManager>();

            // VFX 지속 시간 계산
            vfxDuration = CalculateVFXDuration();
            startTime = Time.time;

            // 파티클 시스템 캐싱
            particleSystems = GetComponentsInChildren<ParticleSystem>();

            if (logTriggerEvents)
            {
                Debug.Log($"[VFXEventTrigger] Initialized: Type={triggerType}, " +
                         $"TriggerValue={triggerValue:F2}, Duration={vfxDuration:F2}s, " +
                         $"Target={target?.name ?? "None"}");
            }
        }

        #endregion

        #region VFX Duration Calculation

        private float CalculateVFXDuration()
        {
            float duration = 0f;
            bool isLooping = false;

            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                if (ps.main.loop)
                {
                    isLooping = true;
                    continue;
                }

                float psDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                duration = Mathf.Max(duration, psDuration);
            }

            Animator animator = GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
                foreach (var clip in clips)
                {
                    if (clip.isLooping)
                    {
                        isLooping = true;
                        continue;
                    }
                    duration = Mathf.Max(duration, clip.length);
                }
            }

            if (isLooping || duration <= 0f)
            {
                duration = 3f; // 루핑 VFX 기본값
                if (logTriggerEvents)
                    Debug.LogWarning($"[VFXEventTrigger] Looping VFX detected, using default duration: {duration}s");
            }

            return duration;
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (triggered || destroyed || onTriggerCallback == null)
                return;

            float elapsed = Time.time - startTime;
            float normalizedTime = vfxDuration > 0 ? elapsed / vfxDuration : 0f;

            // Timeout 체크
            if (elapsed >= maxLifetime)
            {
                if (logTriggerEvents)
                    Debug.LogWarning($"[VFXEventTrigger] Timeout reached ({maxLifetime}s), forcing trigger");

                ForceTrigger();
                return;
            }

            // 트리거 조건 체크
            bool shouldTrigger = triggerType switch
            {
                TriggerType.Time => elapsed >= triggerValue,
                TriggerType.NormalizedTime => normalizedTime >= triggerValue,
                TriggerType.ParticleCount => CheckParticleCount(),
                TriggerType.Manual => false,
                _ => false
            };

            if (shouldTrigger)
            {
                FireTrigger(normalizedTime);
            }
        }

        #endregion

        #region Trigger Execution

        /// <summary>트리거 발생 및 TriggerData 생성</summary>
        private void FireTrigger(float normalizedTime)
        {
            if (triggered) return;
            triggered = true;

            // TriggerData 생성 및 기본 정보 설정
            VFXTriggerData triggerData = new VFXTriggerData
            {
                TriggerWorldPosition = transform.position,
                NormalizedProgress = normalizedTime
            };

            // TCG 타겟 유효성 검증
            ValidatePredeterminedTarget(triggerData);

            // 콜백 실행
            try
            {
                onTriggerCallback?.Invoke(triggerData);

                if (logTriggerEvents)
                {
                    Debug.Log($"[VFXEventTrigger] Trigger fired: " +
                             $"Progress={normalizedTime:F2}, " +
                             $"AttackSuccess={triggerData.AttackSuccess}, " +
                             $"Target={predeterminedTarget?.name ?? "None"}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VFXEventTrigger] Callback error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>강제 트리거 (타임아웃)</summary>
        private void ForceTrigger()
        {
            FireTrigger(1.0f);
        }

        /// <summary>수동 트리거 (Manual 모드용)</summary>
        public void ManualTrigger()
        {
            if (triggerType != TriggerType.Manual)
            {
                Debug.LogWarning($"[VFXEventTrigger] ManualTrigger called but TriggerType is {triggerType}");
                return;
            }

            float elapsed = Time.time - startTime;
            float normalizedTime = vfxDuration > 0 ? elapsed / vfxDuration : 0f;
            FireTrigger(normalizedTime);
        }

        #endregion

        #region TCG Target Validation

        /// <summary>
        /// TCG 타겟 유효성 검증
        /// 물리 충돌 없이 사전 결정된 타겟의 유효성만 검증
        /// </summary>
        private void ValidatePredeterminedTarget(VFXTriggerData triggerData)
        {
            // 타겟이 없는 경우
            if (predeterminedTarget == null)
            {
                triggerData.SetTargetInvalid(null, "No predetermined target");
                return;
            }

            // 타겟이 파괴된 경우
            if (predeterminedTarget == null || !predeterminedTarget.activeInHierarchy)
            {
                triggerData.SetTargetInvalid(predeterminedTarget, "Target destroyed or inactive");
                return;
            }

            // ITargetable 인터페이스 체크 (타겟 가능 여부)
            var targetable = predeterminedTarget.GetComponent<ITargetable>();
            if (targetable == null)
            {
                triggerData.SetTargetInvalid(predeterminedTarget, "Target does not implement ITargetable");
                return;
            }

            if (!targetable.IsValidTarget())
            {
                triggerData.SetTargetInvalid(predeterminedTarget, "Target is not valid (dead, invincible, etc.)");
                return;
            }

            // GridManager를 통한 그리드 좌표 검증
            if (gridManager != null)
            {
                Vector2Int gridPos = gridManager.WorldToGridPosition(predeterminedTarget.transform.position);
                triggerData.SetTargetValid(predeterminedTarget, gridPos);
            }
            else
            {
                // GridManager 없이도 성공 처리 (그리드 좌표는 null)
                triggerData.SetTargetValid(predeterminedTarget, Vector2Int.zero);
                Debug.LogWarning($"[VFXEventTrigger] GridManager not found, grid position unavailable");
            }
        }

        #endregion

        #region Particle Count Check

        private bool CheckParticleCount()
        {
            if (particleSystems == null || particleSystems.Length == 0)
                return false;

            int totalParticles = 0;
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                    totalParticles += ps.particleCount;
            }

            return totalParticles >= (int)triggerValue;
        }

        #endregion

        #region Lifecycle

        private void OnDestroy()
        {
            destroyed = true;

            // 트리거 미발생 시 강제 실행
            if (!triggered && onTriggerCallback != null)
            {
                if (logTriggerEvents)
                    Debug.LogWarning($"[VFXEventTrigger] Destroyed before trigger, forcing execution");

                ForceTrigger();
            }
        }

        #endregion
    }
}
```

---

## 3. SpellEffectExecutor 수정 (TriggerData 통합 + ServiceLocator 패턴)

### Interface 정의

```csharp
namespace Game.VFX
{
    using Game.Data;
    using Game.Interfaces;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// VFX 기반 효과 실행 인터페이스
    /// ServiceLocator 패턴으로 의존성 주입 지원
    /// </summary>
    public interface ISpellEffectExecutor
    {
        /// <summary>
        /// 여러 효과를 단일 VFX로 실행 (TriggerData 기반)
        /// </summary>
        void ExecuteBatch(List<EffectData> effectDataList, Vector2Int targetPos, GameContext context);
    }
}
```

### 구현체

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Card.Effects;
using Game.Data;
using Game.Interfaces;
using Game.Core;

namespace Game.VFX
{
    /// <summary>
    /// VFX 기반 효과 실행 중재자 (Mediator Pattern + ServiceLocator)
    /// TriggerData를 통해 공격 성공/실패 정보 전달
    /// GameInitializer에서 Inspector 직렬화를 통해 등록됨
    /// </summary>
    public class SpellEffectExecutor : MonoBehaviour, ISpellEffectExecutor
    {
        #region ServiceLocator Integration

        // ✅ Singleton 패턴 제거 - ServiceLocator 패턴으로 전환
        // GameInitializer의 Inspector에서 직렬화 필드로 참조하여 등록
        // 사용법: ServiceLocator.Get<ISpellEffectExecutor>().ExecuteBatch(...)

        #endregion

        #region Public API

        /// <summary>
        /// 여러 효과를 단일 VFX로 실행 (TriggerData 기반)
        /// </summary>
        public void ExecuteBatch(List<EffectData> effectDataList, Vector2Int targetPos, GameContext context)
        {
            if (effectDataList == null || effectDataList.Count == 0)
            {
                Debug.LogWarning("[SpellEffectExecutor] ExecuteBatch: Empty effect list");
                return;
            }

            // 우선순위 정렬
            var sortedEffects = effectDataList.OrderBy(e => e.Priority).ToList();

            // VFX 데이터 가져오기 (첫 번째 효과의 VFX 사용)
            VFXData vfxData = sortedEffects[0].VFXData;

            if (vfxData == null || vfxData.VFXPrefab == null)
            {
                Debug.LogWarning("[SpellEffectExecutor] No VFX data, executing effects immediately");
                ExecuteEffectsImmediate(sortedEffects, targetPos, context, null);
                return;
            }

            // 타겟 GameObject 가져오기
            GameObject targetObject = GetTargetObject(targetPos, context);

            // Coroutine 실행 (더 이상 새 인스턴스 생성 안 함)
            StartCoroutine(ExecuteWithVFX(sortedEffects, vfxData, targetPos, targetObject, context));
        }

        #endregion

        #region VFX Execution Coroutine

        private IEnumerator ExecuteWithVFX(List<EffectData> effects, VFXData vfxData, Vector2Int targetPos, GameObject targetObject, GameContext context)
        {
            GameObject vfxInstance = null;

            try
            {
                // VFX 생성
                Vector3 worldPos = context.GridManager.GridToWorldPosition(targetPos);
                vfxInstance = Instantiate(vfxData.VFXPrefab, worldPos, Quaternion.identity);

                // VFXEventTrigger 초기화 (TriggerData 콜백)
                VFXEventTrigger trigger = vfxInstance.GetComponent<VFXEventTrigger>();
                if (trigger == null)
                    trigger = vfxInstance.AddComponent<VFXEventTrigger>();

                bool effectsExecuted = false;

                trigger.Initialize(
                    vfxData.TriggerNormalizedTime,
                    (triggerData) => {
                        // TriggerData를 통해 효과 실행
                        ExecuteEffectsWithData(effects, targetPos, context, triggerData);
                        effectsExecuted = true;
                    },
                    targetObject
                );

                // VFX 재생 대기 (최대 시간 제한)
                float maxWait = vfxData.IsLooping ? 10f : vfxData.Duration + 1f;
                float elapsed = 0f;

                while (!effectsExecuted && elapsed < maxWait)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                }

                // Timeout 처리
                if (!effectsExecuted)
                {
                    Debug.LogWarning($"[SpellEffectExecutor] VFX timeout, forcing execution");
                    VFXTriggerData timeoutData = new VFXTriggerData
                    {
                        TriggerWorldPosition = worldPos,
                        NormalizedProgress = 1.0f
                    };
                    timeoutData.SetTargetInvalid(targetObject, "VFX timeout");
                    ExecuteEffectsWithData(effects, targetPos, context, timeoutData);
                }

                // VFX 정리
                if (!vfxData.IsLooping)
                {
                    yield return new WaitForSeconds(vfxData.Duration);
                    if (vfxInstance != null)
                        Destroy(vfxInstance);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpellEffectExecutor] Error: {ex.Message}\n{ex.StackTrace}");

                // Fallback: 즉시 실행
                ExecuteEffectsImmediate(effects, targetPos, context, null);

                if (vfxInstance != null)
                    Destroy(vfxInstance);
            }
            finally
            {
                // ✅ ServiceLocator 패턴으로 변경되어 더 이상 gameObject를 파괴하지 않음
                // GameObject는 GameInitializer에서 관리됨
            }
        }

        #endregion

        #region Effect Execution with TriggerData

        /// <summary>
        /// TriggerData를 통해 효과 실행 (공격 성공/실패 반영)
        /// </summary>
        private void ExecuteEffectsWithData(List<EffectData> effects, Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
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

                    // IVFXAwareEffect 체크 (고급 효과)
                    if (effect is IVFXAwareEffect vfxAwareEffect)
                    {
                        vfxAwareEffect.ExecuteWithVFXData(targetPos, context, triggerData);
                    }
                    else
                    {
                        // 기본 효과 (AttackSuccess 여부만 확인)
                        if (triggerData.AttackSuccess)
                        {
                            effect.Execute(targetPos, context);
                        }
                        else
                        {
                            Debug.Log($"[SpellEffectExecutor] Effect skipped due to attack failure: {effectData.Type}");
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
        /// 즉시 효과 실행 (VFX 없이)
        /// </summary>
        private void ExecuteEffectsImmediate(List<EffectData> effects, Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            foreach (var effectData in effects)
            {
                try
                {
                    ICardEffect effect = CreateEffectInstance(effectData);
                    if (effect == null) continue;

                    if (effect is IVFXAwareEffect vfxAwareEffect && triggerData != null)
                    {
                        vfxAwareEffect.ExecuteWithVFXData(targetPos, context, triggerData);
                    }
                    else
                    {
                        effect.Execute(targetPos, context);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SpellEffectExecutor] Immediate execution error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Effect Factory

        private ICardEffect CreateEffectInstance(EffectData effectData)
        {
            return effectData.Type switch
            {
                EffectType.Damage => new DamageEffect(effectData),
                EffectType.Heal => new HealEffect(effectData),
                EffectType.Summon => new SummonEffect(effectData),
                _ => null
            };
        }

        #endregion

        #region Helper Methods

        private GameObject GetTargetObject(Vector2Int gridPos, GameContext context)
        {
            // GridManager를 통해 해당 위치의 GameObject 검색
            // 실제 구현은 프로젝트의 Grid 시스템에 따라 다름

            // 예시: context.GridManager.GetObjectAtPosition(gridPos);
            return null; // Placeholder
        }

        #endregion
    }
}
```

---

## 4. GameInitializer 통합 (ServiceLocator 등록)

### GameInitializer 수정

```csharp
public class GameInitializer : MonoBehaviour
{
    [Header("서비스 참조")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameServiceManager gameServiceManager;
    [SerializeField] private CardServiceManager cardServiceManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private SpellEffectExecutor spellEffectExecutor; // ✅ VFX 서비스 추가

    // ... 기존 코드 ...

    private void RegisterCoreServices()
    {
        Log("Registering core services...");

        // Grid Services
        if (gridManager != null)
        {
            gridManager.InitializeForServiceLocator();
            RegisterGridServices();
        }

        // Game Services
        RegisterGameServices();

        // Resource Services
        RegisterResourceServices();

        // Card Services
        RegisterCardServices();

        // VFX Services ✅ 추가
        RegisterVFXServices();
    }

    /// <summary>
    /// VFX 서비스 등록 - SpellEffectExecutor를 통한 VFX 효과 시스템
    /// </summary>
    private void RegisterVFXServices()
    {
        Log("Registering VFX services...");

        if (spellEffectExecutor != null)
        {
            ServiceLocator.Register<ISpellEffectExecutor>(spellEffectExecutor);
            Log("✅ ISpellEffectExecutor registered");
        }
        else
        {
            LogError("❌ SpellEffectExecutor not assigned in inspector");
        }

        Log("VFX services registration completed");
    }

    private void ValidateServices()
    {
        Log("Validating services...");

        // ... 기존 검증 코드 ...

        // VFX 서비스 확인 ✅ 추가
        if (!ServiceLocator.IsRegistered<ISpellEffectExecutor>())
        {
            LogError("❌ Critical service missing: ISpellEffectExecutor");
        }

        ServiceLocator.ValidateServices();
        Log("Service validation completed");
    }
}
```

### Unity Inspector 설정

1. **씬에 SpellEffectExecutor GameObject 생성**
   - Hierarchy: `Create Empty` → 이름: `SpellEffectExecutor`
   - Component 추가: `SpellEffectExecutor` 스크립트

2. **GameInitializer 설정**
   - `GameInitializer` GameObject 선택
   - Inspector의 `Spell Effect Executor` 필드에 SpellEffectExecutor GameObject 드래그 앤 드롭

### 사용 예시

```csharp
// CardData 또는 효과 실행 시스템에서
public class SomeCardSystem
{
    public void ExecuteSpellCard(CardData cardData, Vector2Int targetPos, GameContext context)
    {
        // ServiceLocator를 통해 SpellEffectExecutor 가져오기
        var executor = ServiceLocator.Get<ISpellEffectExecutor>();

        if (executor != null)
        {
            executor.ExecuteBatch(cardData.EffectDataList, targetPos, context);
        }
        else
        {
            Debug.LogError("[SomeCardSystem] ISpellEffectExecutor not registered!");
        }
    }
}
```

### 의존성 주입 방식 (권장)

```csharp
using Game.Core;

public class CardExecutionService : MonoBehaviour
{
    [Inject] private ISpellEffectExecutor spellEffectExecutor;

    private void Awake()
    {
        // 자동 의존성 주입
        this.InjectDependencies();
    }

    public void ExecuteCard(CardData cardData, Vector2Int targetPos, GameContext context)
    {
        // Null 체크
        if (spellEffectExecutor == null)
        {
            Debug.LogError("SpellEffectExecutor not injected!");
            return;
        }

        // VFX 효과 실행
        spellEffectExecutor.ExecuteBatch(cardData.EffectDataList, targetPos, context);
    }
}
```

---

## 5. IVFXAwareEffect 인터페이스

VFX 동적 데이터를 활용하는 고급 효과를 위한 인터페이스입니다.

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// VFX TriggerData를 활용하는 고급 효과 인터페이스
    /// 공격 성공/실패, 타겟 유효성 등의 동적 정보에 반응
    /// </summary>
    public interface IVFXAwareEffect : ICardEffect
    {
        /// <summary>
        /// VFX TriggerData를 포함한 효과 실행
        /// </summary>
        /// <param name="targetPos">타겟 그리드 좌표</param>
        /// <param name="context">게임 컨텍스트</param>
        /// <param name="triggerData">VFX 동적 데이터 (공격 성공/실패, 타겟 정보 등)</param>
        void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData);
    }
}
```

### IVFXAwareEffect 구현 예시

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// VFX-Aware 데미지 효과 (공격 성공/실패에 반응)
    /// </summary>
    public class AdvancedDamageEffect : IVFXAwareEffect
    {
        private EffectData effectData;

        public AdvancedDamageEffect(EffectData data)
        {
            effectData = data;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            // 기본 Execute는 성공 가정
            ApplyDamage(effectData.Value, null, context);
        }

        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            // 공격 실패 시 스킵
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[AdvancedDamageEffect] Attack failed: {triggerData.ValidationFailureReason}");
                PlayMissEffect(targetPos, context);
                return;
            }

            // 공격 성공 시 데미지 적용
            GameObject target = triggerData.PredeterminedTarget;
            ApplyDamage(effectData.Value, target, context);

            // VFX 위치 기반 파티클 효과
            PlayHitEffect(triggerData.TriggerWorldPosition, context);
        }

        private void ApplyDamage(int damage, GameObject target, GameContext context)
        {
            if (target == null)
            {
                Debug.LogWarning("[AdvancedDamageEffect] No target to apply damage");
                return;
            }

            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                Debug.Log($"[AdvancedDamageEffect] Applied {damage} damage to {target.name}");
            }
        }

        private void PlayMissEffect(Vector2Int gridPos, GameContext context)
        {
            Debug.Log($"[AdvancedDamageEffect] Playing miss effect at {gridPos}");
            // Miss VFX 재생 로직
        }

        private void PlayHitEffect(Vector3 worldPos, GameContext context)
        {
            Debug.Log($"[AdvancedDamageEffect] Playing hit effect at {worldPos}");
            // Hit VFX 재생 로직
        }
    }
}
```

---

## 6. 통합 워크플로우

### 카드 사용 → VFX → 효과 실행 흐름

```
1. 사용자가 스펠 카드 사용
   ↓
2. CardData.ExecuteEffects() 호출
   ↓
3. ServiceLocator.Get<ISpellEffectExecutor>().ExecuteBatch(effectDataList, targetPos, context)
   ↓
4. VFX 생성 및 VFXEventTrigger.Initialize(normalizedTime, callback, target)
   ↓
5. VFX 재생 중... (Update Loop)
   ↓
6. 트리거 조건 도달 (normalizedTime >= 0.7)
   ↓
7. VFXEventTrigger.ValidatePredeterminedTarget(triggerData)
   - AttackSuccess 설정
   - TargetGridPosition 설정
   - ValidationFailureReason 설정 (실패 시)
   ↓
8. callback(triggerData) 실행
   ↓
9. SpellEffectExecutor.ExecuteEffectsWithData(effects, targetPos, context, triggerData)
   ↓
10. 각 효과 실행:
    - IVFXAwareEffect → ExecuteWithVFXData() (TriggerData 활용)
    - 기본 ICardEffect → Execute() (AttackSuccess 체크 후 실행)
   ↓
11. VFX 정리 및 SpellEffectExecutor 파괴
```

---

## 7. TCG 특화 기능

### 공격 성공/실패 판정 로직

```csharp
private void ValidatePredeterminedTarget(VFXTriggerData triggerData)
{
    // 1. Null 체크
    if (predeterminedTarget == null)
    {
        triggerData.SetTargetInvalid(null, "No predetermined target");
        return;
    }

    // 2. GameObject 활성화 체크
    if (!predeterminedTarget.activeInHierarchy)
    {
        triggerData.SetTargetInvalid(predeterminedTarget, "Target destroyed or inactive");
        return;
    }

    // 3. ITargetable 인터페이스 체크
    var targetable = predeterminedTarget.GetComponent<ITargetable>();
    if (targetable == null)
    {
        triggerData.SetTargetInvalid(predeterminedTarget, "Target does not implement ITargetable");
        return;
    }

    // 4. 타겟 유효성 체크 (죽음, 무적 등)
    if (!targetable.IsValidTarget())
    {
        triggerData.SetTargetInvalid(predeterminedTarget, "Target is not valid (dead, invincible, etc.)");
        return;
    }

    // 5. 성공: 그리드 좌표 설정
    Vector2Int gridPos = gridManager.WorldToGridPosition(predeterminedTarget.transform.position);
    triggerData.SetTargetValid(predeterminedTarget, gridPos);
}
```

### 효과 실행 시 AttackSuccess 활용

```csharp
private static void ExecuteEffectsWithData(List<EffectData> effects, Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
{
    foreach (var effectData in effects)
    {
        ICardEffect effect = CreateEffectInstance(effectData);

        if (effect is IVFXAwareEffect vfxAwareEffect)
        {
            // 고급 효과: TriggerData 전체 활용
            vfxAwareEffect.ExecuteWithVFXData(targetPos, context, triggerData);
        }
        else
        {
            // 기본 효과: AttackSuccess 체크 후 실행
            if (triggerData.AttackSuccess)
            {
                effect.Execute(targetPos, context);
            }
            else
            {
                Debug.Log($"Effect skipped: {effectData.Type} (Attack failed: {triggerData.ValidationFailureReason})");
            }
        }
    }
}
```

---

## 8. 장점 및 특징

### TCG 게임 특성 반영
- ✅ **사전 결정된 타겟**: 물리 충돌 없이 타겟 유효성만 검증
- ✅ **공격 성공/실패 명확화**: `AttackSuccess` 플래그로 게임 로직 분기
- ✅ **그리드 기반 좌표**: `TargetGridPosition`으로 보드 게임 로직 통합

### 설계 원칙
- ✅ **레거시 제거**: 단일 콜백 방식 (`Action<VFXTriggerData>`)
- ✅ **ServiceLocator 패턴**: Interface 기반 의존성 주입, 테스트 용이성 확보
- ✅ **Inspector 직렬화**: GameInitializer에서 직접 관리, 명시적 생명주기
- ✅ **확장 가능성**: `IVFXAwareEffect`로 고급 효과 지원
- ✅ **안전성**: Timeout, Null 체크, Try-Catch
- ✅ **디버깅 용이**: 상세한 로그 및 ValidationFailureReason

### 성능 최적화
- 단일 VFX로 다중 효과 처리 (ExecuteBatch)
- 불필요한 물리 연산 제거 (TCG 특성)
- Pooling 지원 가능 (VFXData.usePooling)

---

## 9. 다음 단계

1. **GameInitializer에 SpellEffectExecutor 등록**
   - Inspector에서 SpellEffectExecutor GameObject 참조
   - `RegisterVFXServices()` 메서드 추가
   - ServiceLocator 검증 로직 추가

2. **ISpellEffectExecutor 인터페이스 파일 생성**
   - `Assets/Script/Game/VFX/ISpellEffectExecutor.cs` 생성
   - Interface 정의

3. **SpellEffectExecutor 구현**
   - Singleton 패턴 제거
   - `ISpellEffectExecutor` 인터페이스 구현
   - `static` 메서드를 인스턴스 메서드로 변경

4. **ITargetable 인터페이스 구현** - 타겟 유효성 검증 로직

5. **EffectData 마이그레이션** - VFXData 필드 추가

6. **기존 Effect 클래스 수정** - IVFXAwareEffect 구현

7. **통합 테스트** - 카드 사용 → VFX → 효과 실행 전체 플로우

8. **디버깅 도구** - VFX 타이밍, 타겟 유효성 시각화

