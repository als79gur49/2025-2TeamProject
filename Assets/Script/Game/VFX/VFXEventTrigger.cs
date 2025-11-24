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
        [SerializeField] [Range(0f, 1f)] private float maxWaitNormalizedTime = 1f;

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
        private float playbackSpeed = 1.0f; // VFX 재생 속도 배율
        private float currentProgress;
        private float effectiveMaxLifetime;

        private ParticleSystem[] particleSystems;

        public float MaxLifetime => effectiveMaxLifetime > 0f ? effectiveMaxLifetime : maxLifetime;
        public float MaxWaitTime => MaxLifetime * maxWaitNormalizedTime;
        public float Elapsed => Time.time - startTime;
        public float NormalizedProgress => MaxLifetime > 0f
            ? Mathf.Clamp01(Elapsed / MaxLifetime)
            : 0f;

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
        public void Initialize(
            float normalizedTriggerTime,
            Action<List<VFXTriggerData>> callback,
            List<Vector3> tilePositions,
            IGridController controller = null,
            float playbackSpeed = 1.0f)
        {
            this.triggerType = TriggerType.NormalizedTime;
            this.triggerValue = Mathf.Clamp01(normalizedTriggerTime);
            this.onTriggerCallbackList = callback;
            this.predeterminedTilePositions = tilePositions ?? new List<Vector3>();

            // GridController 참조 (인수로 전달받음, ServiceLocator 사용 안 함)
            this.gridController = controller;

            // 재생 속도 저장 및 적용
            playbackSpeed = Mathf.Clamp(playbackSpeed, 0.1f, 3.0f);
            this.playbackSpeed = playbackSpeed;
            ApplyPlaybackSpeed(playbackSpeed);

            // 재생 속도에 비례하여 실제 생존 시간(effectiveMaxLifetime)을 조정합니다.
            // 기본 maxLifetime은 playbackSpeed = 1 기준 수명으로 간주하고,
            // 재생 속도가 빨라질수록 MaxLifetime이 짧아지도록 합니다.
            effectiveMaxLifetime = maxLifetime / playbackSpeed;

            startTime = Time.time;

            // 파티클 시스템 캐싱
            particleSystems = GetComponentsInChildren<ParticleSystem>();

            if (logTriggerEvents)
            {
                Debug.Log($"[VFXEventTrigger] Initialized: Type={triggerType}, " +
                         $"TriggerValue={triggerValue:F2}, MaxLifetime={MaxLifetime:F2}s, " +
                         $"PlaybackSpeed={playbackSpeed:F2}, " +
                         $"TilePositions={this.predeterminedTilePositions.Count}");
            }

            // 루프 사운드 시작 (선택적) - AudioPlayRequest 기반으로 재생 속도(pitch) 정보 전달
            if (soundEventChannel != null && startSound != null)
            {
                var request = AudioPlayRequest
                    .Create(startSound, this)
                    .WithPitch(this.playbackSpeed);

                soundEventChannel.RaiseSoundEvent(request);

                if (logTriggerEvents)
                    Debug.Log($"[VFXEventTrigger] Started sound: {startSound.name} (Loop={startSound.Loop}, PitchMult={this.playbackSpeed:F2})");
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
                main.simulationSpeed *= speed;
            }

            // Animator 속도 적용
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed *= speed;
            }
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (triggered || destroyed || onTriggerCallbackList == null)
                return;

            float elapsed = Elapsed;
            float normalizedTime = NormalizedProgress;
            currentProgress = normalizedTime;

            if (elapsed >= MaxLifetime)
            {
                if (logTriggerEvents)
                    Debug.LogWarning($"[VFXEventTrigger] Lifetime reached ({MaxLifetime:F2}s), forcing trigger and destroy");

                if (!triggered)
                {
                    ForceTrigger();
                }

                Destroy(gameObject);
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
                    Debug.Log($"[VFXEventTrigger] Trigger fired: Progress={normalizedTime:F2}, " +
                              $"MaxLifetime={MaxLifetime:F1}, ValidatedTargets={triggerDataList.Count}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VFXEventTrigger] Callback error: {ex.Message}\n{ex.StackTrace}");
            }

            // 트리거 사운드 재생 (선택적)
            if (soundEventChannel != null && triggerSound != null)
            {
                var request = AudioPlayRequest
                    .Create(triggerSound, this)
                    .WithPitch(playbackSpeed);

                soundEventChannel.RaiseSoundEvent(request);

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

            float normalizedTime = NormalizedProgress;
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
