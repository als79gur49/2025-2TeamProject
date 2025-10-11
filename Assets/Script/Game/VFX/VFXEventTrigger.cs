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

        [Header("Audio Settings")]
        [SerializeField] private SoundEventChannelSO soundEventChannel;
        [SerializeField] private AudioData startSound;     // 루프 사운드 (생성 시 시작)
        [SerializeField] private AudioData triggerSound;   // 트리거 사운드 (FireTrigger 시 재생)

        [Header("Debug")]
        [SerializeField] private bool logTriggerEvents = true;

        #endregion

        #region Runtime State

        private Action<List<VFXTriggerData>> onTriggerCallbackList;
        private List<Vector3> predeterminedTilePositions; // 타일 기반: 월드 좌표 리스트
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
        /// VFX 트리거 초기화 (타일 기반)
        /// Phase 3: 타일 위치 리스트를 받아 VFX 트리거 시점에 검증
        /// </summary>
        /// <param name="normalizedTriggerTime">트리거 발생 정규화 시간 (0.0 ~ 1.0)</param>
        /// <param name="callback">List<VFXTriggerData>를 전달받는 콜백</param>
        /// <param name="tilePositions">사전 결정된 타일 월드 좌표 리스트</param>
        /// <param name="controller">그리드 좌표 계산용 GridController (null 가능)</param>
        /// <param name="playbackSpeed">VFX 재생 속도 배율 (0.1 ~ 3.0)</param>
        /// <param name="manualDuration">수동 지속시간 (0 이하면 자동 계산)</param>
        public void Initialize(
            float normalizedTriggerTime,
            Action<List<VFXTriggerData>> callback,
            List<Vector3> tilePositions,
            IGridController controller = null,
            float playbackSpeed = 1.0f,
            float manualDuration = -1f)
        {
            this.triggerType = TriggerType.NormalizedTime;
            this.triggerValue = Mathf.Clamp01(normalizedTriggerTime);
            this.onTriggerCallbackList = callback;
            this.predeterminedTilePositions = tilePositions ?? new List<Vector3>();

            // GridController 참조 (인수로 전달받음, ServiceLocator 사용 안 함)
            this.gridController = controller;

            // 재생 속도 적용
            ApplyPlaybackSpeed(playbackSpeed);

            // VFX 지속 시간 계산 (수동 > 자동)
            if (manualDuration > 0f)
            {
                vfxDuration = manualDuration;
                if (logTriggerEvents)
                    Debug.Log($"[VFXEventTrigger] Using manual duration: {vfxDuration:F2}s");
            }
            else
            {
                vfxDuration = CalculateVFXDuration();
                if (logTriggerEvents)
                    Debug.Log($"[VFXEventTrigger] Calculated auto duration: {vfxDuration:F2}s");
            }
            startTime = Time.time;

            // 파티클 시스템 캐싱
            particleSystems = GetComponentsInChildren<ParticleSystem>();

            if (logTriggerEvents)
            {
                string durationMode = manualDuration > 0f ? "Manual" : "Auto";
                Debug.Log($"[VFXEventTrigger] Initialized: Type={triggerType}, " +
                         $"TriggerValue={triggerValue:F2}, Duration={vfxDuration:F2}s ({durationMode}), " +
                         $"PlaybackSpeed={playbackSpeed:F2}, " +
                         $"TilePositions={this.predeterminedTilePositions.Count}");
            }

            // 루프 사운드 시작 (선택적) - AudioData.Loop 속성이 자동으로 재생 방식 결정
            if (soundEventChannel != null && startSound != null)
            {
                soundEventChannel.RaiseSoundEvent(startSound, this);
                if (logTriggerEvents)
                    Debug.Log($"[VFXEventTrigger] Started sound: {startSound.name} (Loop={startSound.Loop})");
            }
        }


        /// <summary>
        /// VFX 재생 속도 적용 (ParticleSystem과 Animator에 적용)
        /// </summary>
        private void ApplyPlaybackSpeed(float speed)
        {
            speed = Mathf.Clamp(speed, 0.1f, 3.0f);

            // ParticleSystem 속도 적용
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.simulationSpeed = speed;
            }

            // Animator 속도 적용
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = speed;
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
                             $"VFXDuration={vfxDuration:F1}, " +
                             $"ValidatedTargets={triggerDataList.Count}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VFXEventTrigger] Callback error: {ex.Message}\n{ex.StackTrace}");
            }

            // 트리거 사운드 재생 (선택적)
            if (soundEventChannel != null && triggerSound != null)
            {
                soundEventChannel.RaiseSoundEvent(triggerSound, this);
                if (logTriggerEvents)
                    Debug.Log($"[VFXEventTrigger] Played trigger sound: {triggerSound.name}");
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

        #region Tile-Based Validation (Phase 3)

        /// <summary>
        /// VFX 트리거 시점에 모든 타일 위치를 검증
        /// 타일 기반: 월드 좌표 → 그리드 좌표 변환 → 타일 유효성 검증
        /// </summary>
        private List<VFXTriggerData> ValidateAllTargets()
        {
            var triggerDataList = new List<VFXTriggerData>();

            // 타일 위치가 없는 경우
            if (predeterminedTilePositions == null || predeterminedTilePositions.Count == 0)
            {
                if (logTriggerEvents)
                    Debug.LogWarning("[VFXEventTrigger] No predetermined tile positions");
                return triggerDataList;
            }

            // GridController 필수 체크
            if (gridController == null)
            {
                Debug.LogError("[VFXEventTrigger] GridController is null - cannot validate tiles");
                return triggerDataList;
            }

            // 각 타일 위치를 개별적으로 검증
            foreach (var worldPos in predeterminedTilePositions)
            {
                var triggerData = new VFXTriggerData
                {
                    TileWorldPosition = worldPos,
                    NormalizedProgress = currentProgress
                };

                // 타일 위치 검증
                ValidateTilePosition(worldPos, triggerData);

                // 검증 실패한 타일도 리스트에 포함 (AttackSuccess = false)
                triggerDataList.Add(triggerData);
            }

            return triggerDataList;
        }

        /// <summary>
        /// Phase 3: 타일 위치 기반 검증
        /// 월드 좌표 → 그리드 좌표 → 타일 존재 확인 → 유닛 존재 확인 (선택적)
        /// </summary>
        private void ValidateTilePosition(Vector3 worldPos, VFXTriggerData triggerData)
        {
            // 1. 월드 좌표를 그리드 좌표로 변환
            Vector2Int gridPos2D = gridController.WorldToGridPosition(worldPos);
            Vector3Int gridPos3D = new Vector3Int(gridPos2D.x, gridPos2D.y, 0);

            triggerData.TileGridPosition = gridPos3D;

            // 2. 타일 존재 확인
            var tile = gridController.GetTileAtPosition(gridPos2D);
            if (tile == null)
            {
                triggerData.SetTileTargetInvalid(gridPos3D, "Tile does not exist at position");
                return;
            }

            // 3. 타일이 활성화되어 있는지 확인
            if (!tile.gameObject.activeInHierarchy)
            {
                triggerData.SetTileTargetInvalid(gridPos3D, "Tile is inactive");
                return;
            }

            // 4. 타일 기반 검증 성공
            // 유닛 존재 여부는 Effect에서 판단 (tile.OccupyingUnit)
            triggerData.SetTileTargetValid(gridPos3D, worldPos);

            if (logTriggerEvents)
            {
                bool hasUnit = tile.OccupyingUnit != null;
                Debug.Log($"[VFXEventTrigger] Tile validated at {gridPos3D}, HasUnit={hasUnit}");
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
            // 루프 사운드 정지 (선택적)
            if (soundEventChannel != null && startSound != null)
            {
                soundEventChannel.RaiseStopLoopEvent(this, startSound);
                if (logTriggerEvents)
                    Debug.Log($"[VFXEventTrigger] Stopped loop sound: {startSound.name}");
            }

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
