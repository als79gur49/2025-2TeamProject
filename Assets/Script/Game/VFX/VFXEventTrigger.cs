using System;
using UnityEngine;
using Game.Interfaces;
using Game.Services;
using Game.Core;
using Game.Components;

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
            if (!predeterminedTarget.activeInHierarchy)
            {
                triggerData.SetTargetInvalid(predeterminedTarget, "Target destroyed or inactive");
                return;
            }

            // HealthComponent 체크 (공격 가능 여부)
            var healthComponent = predeterminedTarget.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                triggerData.SetTargetInvalid(predeterminedTarget, "Target does not have HealthComponent");
                return;
            }

            if (!healthComponent.IsAlive)
            {
                triggerData.SetTargetInvalid(predeterminedTarget, "Target is not alive");
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
