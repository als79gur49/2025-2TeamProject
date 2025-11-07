using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Services;
using Game.Core;

namespace Game.Services
{
    /// <summary>
    /// 유닛 죽음 애니메이션을 관리하고 게임 흐름을 제어
    /// ServiceLocator 패턴 사용
    /// </summary>
    public class DeathAnimationManager : MonoBehaviour, IDeathAnimationManager
    {
        [Header("Configuration")]
        [SerializeField] private float defaultDeathDuration = 1.5f;
        [SerializeField] private bool allowSimultaneousDeaths = true;

        [Header("Timing Settings")]
        [Tooltip("GlobalStateManager SetBusy timeout에 추가할 시간 (안전 마진)")]
        [SerializeField] private float timeoutBuffer = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private const string DEATH_ANIMATION_TRIGGER = "Die";

        // Dependencies
        private IGlobalStateManager globalStateManager;

        // Runtime state
        private Queue<Unit> pendingDeathUnits = new Queue<Unit>();
        private bool isProcessingDeath = false;

        // Public properties for interface
        public bool IsProcessingDeath => isProcessingDeath;
        public int PendingDeathCount => pendingDeathUnits.Count;

        /// <summary>
        /// ServiceLocator를 통한 초기화 (GameInitializer에서 호출)
        /// 주의: ServiceLocator 등록은 GameInitializer에서 수행
        /// </summary>
        public void Initialize()
        {
            // Get GlobalStateManager from ServiceLocator
            globalStateManager = ServiceLocator.Get<IGlobalStateManager>();

            if (globalStateManager == null)
            {
                LogError("Failed to get IGlobalStateManager from ServiceLocator!");
            }
            else
            {
                Log("Successfully initialized with GlobalStateManager");
            }

            Log("DeathAnimationManager initialized");
        }

        private void OnDestroy()
        {
            // ServiceLocator에서 자동으로 정리됨 (ValidateServices에 의해)
            Log("DeathAnimationManager destroyed");
        }

        /// <summary>
        /// 유닛 죽음 처리 요청
        /// </summary>
        public void ProcessUnitDeath(Unit unit, float customDuration = -1f)
        {
            if (unit == null)
            {
                LogWarning("ProcessUnitDeath called with null unit");
                return;
            }

            float duration = customDuration > 0 ? customDuration : defaultDeathDuration;
            Log($"Processing death for {unit.name} with duration {duration}s");

            if (allowSimultaneousDeaths)
            {
                StartCoroutine(ProcessDeathAnimation(unit, duration));
            }
            else
            {
                pendingDeathUnits.Enqueue(unit);
                if (!isProcessingDeath)
                {
                    StartCoroutine(ProcessDeathQueue());
                }
            }
        }

        private IEnumerator ProcessDeathQueue()
        {
            isProcessingDeath = true;
            Log($"Starting death queue processing. {pendingDeathUnits.Count} units pending");

            while (pendingDeathUnits.Count > 0)
            {
                Unit unit = pendingDeathUnits.Dequeue();
                if (unit != null)
                {
                    yield return ProcessDeathAnimation(unit, defaultDeathDuration);
                }
            }

            isProcessingDeath = false;
            Log("Death queue processing completed");
        }

        private IEnumerator ProcessDeathAnimation(Unit unit, float duration)
        {
            Log($"Starting death animation for {unit.name} (duration: {duration}s)");

            // GlobalStateManager에 DeathAnimation 상태 설정
            // timeout = duration + timeoutBuffer로 안전 마진 확보
            float timeout = duration + timeoutBuffer;

            if (globalStateManager != null)
            {
                globalStateManager.SetBusy(this, BusyType.DeathAnimation, timeout);
                Log($"Set GlobalStateManager DeathAnimation busy state (timeout: {timeout}s)");
            }
            else
            {
                LogWarning("GlobalStateManager is null, death animation state not set");
            }

            // 유닛의 죽음 애니메이션 트리거
            var animator = unit.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(DEATH_ANIMATION_TRIGGER);
                Log($"Triggered death animation for {unit.name}");
            }
            else
            {
                LogWarning($"No Animator found on {unit.name}");
            }

            // 유닛의 상호작용 불가능 상태로 변경
            unit.SetDeathAnimationState(true);

            // 애니메이션 재생 대기
            yield return new WaitForSeconds(duration);

            // 유닛이 중간에 파괴되었는지 확인
            if (unit != null)
            {
                // 타일 정리 및 유닛 제거
                unit.CompleteDeathSequence();
                Log($"Completed death sequence for {unit.name}");
            }
            else
            {
                LogWarning("Unit was destroyed during death animation");
            }

            // GlobalStateManager 상태 해제
            if (globalStateManager != null)
            {
                globalStateManager.SetIdle(this, BusyType.DeathAnimation);
                Log("Released GlobalStateManager DeathAnimation busy state");
            }

            Log($"Death animation completed for {unit?.name ?? "destroyed unit"}");
        }

        #region Debug Logging

        private void Log(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[DeathAnimationManager] {message}");
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[DeathAnimationManager] {message}");
        }

        private void LogError(string message)
        {
            // 에러는 항상 출력
            Debug.LogError($"[DeathAnimationManager] {message}");
        }

        #endregion
    }
}
