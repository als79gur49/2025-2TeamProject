using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;
using Game.Services;
using Game.Core;
using Game.Components;

namespace Game.VFX
{
    /// <summary>
    /// VFX 프리팹에 부착되어 특정 시점에 다중 타겟 TriggerData를 전달
    /// Phase 2 완료: 원자적 다중 타겟 검증 시스템 구현
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

        private Action<List<VFXTriggerData>> onTriggerCallbackList;
        private List<GameObject> predeterminedTargets;
        private IGridController gridController;

        private bool triggered = false;
        private bool destroyed = false;
        private float startTime;
        private float vfxDuration;
        private float currentProgress;

        private ParticleSystem[] particleSystems;

        #endregion

        #region Initialization

        /// <summary>
        /// VFX 트리거 초기화 (원자적 다중 타겟 검증)
        /// Phase 2: 모든 타겟을 VFX 트리거 시점에 동시 검증
        /// </summary>
        /// <param name="normalizedTriggerTime">트리거 발생 정규화 시간 (0.0 ~ 1.0)</param>
        /// <param name="callback">List<VFXTriggerData>를 전달받는 콜백</param>
        /// <param name="targets">사전 결정된 타겟 리스트 (T=0s에 계산됨)</param>
        /// <param name="controller">그리드 좌표 계산용 GridController (null 가능)</param>
        public void Initialize(
            float normalizedTriggerTime,
            Action<List<VFXTriggerData>> callback,
            List<GameObject> targets,
            IGridController controller = null)
        {
            this.triggerType = TriggerType.NormalizedTime;
            this.triggerValue = Mathf.Clamp01(normalizedTriggerTime);
            this.onTriggerCallbackList = callback;
            this.predeterminedTargets = targets ?? new List<GameObject>();

            // GridController 참조 (인수로 전달받음, ServiceLocator 사용 안 함)
            this.gridController = controller;

            // VFX 지속 시간 계산
            vfxDuration = CalculateVFXDuration();
            startTime = Time.time;

            // 파티클 시스템 캐싱
            particleSystems = GetComponentsInChildren<ParticleSystem>();

            if (logTriggerEvents)
            {
                Debug.Log($"[VFXEventTrigger] Initialized: Type={triggerType}, " +
                         $"TriggerValue={triggerValue:F2}, Duration={vfxDuration:F2}s, " +
                         $"PotentialTargets={this.predeterminedTargets.Count}");
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
            if (triggered || destroyed || onTriggerCallbackList == null)
                return;

            float elapsed = Time.time - startTime;
            float normalizedTime = vfxDuration > 0 ? elapsed / vfxDuration : 0f;
            currentProgress = normalizedTime;

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

            var triggerDataList = ValidateAllTargets();

            try
            {
                onTriggerCallbackList.Invoke(triggerDataList);

                if (logTriggerEvents)
                {
                    Debug.Log($"[VFXEventTrigger] Trigger fired: " +
                             $"Progress={normalizedTime:F2}, " +
                             $"ValidatedTargets={triggerDataList.Count}");
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
        /// VFX 트리거 시점에 모든 타겟을 원자적으로 검증
        /// </summary>
        private List<VFXTriggerData> ValidateAllTargets()
        {
            var triggerDataList = new List<VFXTriggerData>();

            // 타겟이 없는 경우
            if (predeterminedTargets == null || predeterminedTargets.Count == 0)
            {
                if (logTriggerEvents)
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

                // 개별 타겟 검증
                ValidateSingleTarget(target, triggerData);

                // 검증 실패한 타겟도 리스트에 포함 (AttackSuccess = false)
                triggerDataList.Add(triggerData);
            }

            return triggerDataList;
        }

        /// <summary>
        /// 단일 타겟 검증 로직 (재사용 가능)
        /// </summary>
        private void ValidateSingleTarget(GameObject target, VFXTriggerData triggerData)
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

            // 3. HealthComponent 존재 및 생존 체크
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

            // 4. 그리드 좌표 계산 및 검증 성공 설정
            Vector2Int gridPos = Vector2Int.zero;
            if (gridController != null)
            {
                gridPos = gridController.WorldToGridPosition(target.transform.position);
            }
            else
            {
                if (logTriggerEvents)
                    Debug.LogWarning($"[VFXEventTrigger] GridController not available for {target.name}");
            }

            triggerData.SetTargetValid(target, gridPos);
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
            if (!triggered && onTriggerCallbackList != null)
            {
                if (logTriggerEvents)
                    Debug.LogWarning($"[VFXEventTrigger] Destroyed before trigger, forcing execution");

                ForceTrigger();
            }
        }

        #endregion
    }
}
