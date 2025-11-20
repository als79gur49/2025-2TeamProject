using UnityEngine;
using Game.Data;
using Game.Managers;
using Game.Services;
using Game.Core;

namespace Game.Coordinators
{
    /// <summary>
    /// 게임 결과 조율자
    ///
    /// 책임:
    /// - GameOutcomeManager 이벤트 수신 (OnVictory/OnDefeat)
    /// - 승리 시: GameSessionManager → StageProgressManager 데이터 전달
    /// - 패배 시: 세션 데이터 폐기
    ///
    /// 씬 종속성: 게임플레이 씬에만 존재
    /// </summary>
    public class GameResultCoordinator : MonoBehaviour
    {
        #region Dependencies
        private IGameOutcomeManager outcomeManager;
        private IGameSessionManager sessionManager;
        private IStageProgressManager progressManager;
        private IBaseManager baseManager;
        #endregion

        #region Configuration
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        #endregion

        #region Initialization

        /// <summary>
        /// Coordinator 초기화
        /// ServiceLocator를 통해 의존성을 주입받음
        /// </summary>
        public void Initialize()
        {
            // 의존성 가져오기
            outcomeManager = ServiceLocator.Get<IGameOutcomeManager>();
            sessionManager = ServiceLocator.Get<IGameSessionManager>();
            progressManager = ServiceLocator.Get<IStageProgressManager>();
            baseManager = ServiceLocator.Get<IBaseManager>();

            // 의존성 검증
            if (outcomeManager == null)
            {
                Debug.LogError("[GameResultCoordinator] IGameOutcomeManager not found in ServiceLocator!");
                return;
            }

            if (sessionManager == null)
            {
                Debug.LogError("[GameResultCoordinator] GameSessionManager not found in ServiceLocator!");
                return;
            }

            if (progressManager == null)
            {
                Debug.LogError("[GameResultCoordinator] StageProgressManager not found in ServiceLocator!");
                return;
            }

            if (baseManager == null)
            {
                Debug.LogError("[GameResultCoordinator] IBaseManager not found in ServiceLocator!");
                return;
            }

            // 이벤트 구독
            SubscribeToEvents();

            if (enableDebugLogs)
            {
                Debug.Log("[GameResultCoordinator] Initialized successfully");
            }
        }

        #endregion

        #region Event Subscription

        /// <summary>
        /// GameOutcomeManager 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            if (outcomeManager != null)
            {
                outcomeManager.OnVictory += HandleVictory;
                outcomeManager.OnDefeat += HandleDefeat;

                if (enableDebugLogs)
                {
                    Debug.Log("[GameResultCoordinator] Subscribed to GameOutcomeManager events");
                }
            }
        }

        /// <summary>
        /// GameOutcomeManager 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (outcomeManager != null)
            {
                outcomeManager.OnVictory -= HandleVictory;
                outcomeManager.OnDefeat -= HandleDefeat;

                if (enableDebugLogs)
                {
                    Debug.Log("[GameResultCoordinator] Unsubscribed from GameOutcomeManager events");
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 승리 처리: 보너스 적용 → 세션 종료 → 진행도 저장
        /// </summary>
        private void HandleVictory()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[GameResultCoordinator] Victory detected! Processing game result...");
            }

            // 1. 세션 유효성 검증
            if (sessionManager == null || !sessionManager.IsSessionActive)
            {
                Debug.LogError("[GameResultCoordinator] Cannot process victory: No active session!");
                return;
            }

            // 2. 플레이어 베이스 HP 조회 및 승리 보너스 적용
            if (baseManager != null && baseManager.PlayerBase != null)
            {
                float playerHealthPercent = baseManager.PlayerBase.HealthComponent.HealthPercentage * 100f;
                sessionManager.ApplyVictoryBonus(playerHealthPercent);

                if (enableDebugLogs)
                {
                    Debug.Log($"[GameResultCoordinator] Victory bonus applied with {playerHealthPercent:F1}% HP remaining");
                }
            }
            else
            {
                Debug.LogWarning("[GameResultCoordinator] BaseManager or PlayerBase is null! Victory bonus skipped.");
            }

            // 3. 세션 데이터 수집
            GameSessionData sessionData = sessionManager.EndSessionWithVictory();
            if (sessionData == null)
            {
                Debug.LogError("[GameResultCoordinator] Failed to retrieve session data!");
                return;
            }

            // 4. 진행도 매니저에 기록
            if (progressManager != null)
            {
                try
                {
                    progressManager.RecordStageCompletion(
                        sessionData.stageId,
                        sessionData.score,
                        sessionData.statistics
                    );

                    if (enableDebugLogs)
                    {
                        int healthBonus = sessionData.statistics.TryGetValue("health_bonus", out var hb) ? hb : 0;
                        int timeBonus = sessionData.statistics.TryGetValue("time_bonus", out var tb) ? tb : 0;
                        Debug.Log($"[GameResultCoordinator] Victory processed successfully:\n" +
                                  $"  Stage: {sessionData.stageId}\n" +
                                  $"  Score: {sessionData.score}\n" +
                                  $"  Play Time: {sessionData.playTime:F2}s\n" +
                                  $"  Health Bonus: {healthBonus}\n" +
                                  $"  Time Bonus: {timeBonus}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameResultCoordinator] Error recording stage completion: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                Debug.LogError("[GameResultCoordinator] StageProgressManager is null! Cannot save progress.");
            }
        }

        /// <summary>
        /// 패배 처리: 세션 데이터 폐기 (저장 안 함)
        /// </summary>
        private void HandleDefeat()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[GameResultCoordinator] Defeat detected! Discarding session data...");
            }

            // 세션 종료 (데이터 수집하지만 저장하지 않음)
            if (sessionManager != null && sessionManager.IsSessionActive)
            {
                GameSessionData sessionData = sessionManager.EndSessionWithDefeat();

                if (enableDebugLogs && sessionData != null)
                {
                    Debug.Log($"[GameResultCoordinator] Session discarded:\n" +
                              $"  Stage: {sessionData.stageId}\n" +
                              $"  Score: {sessionData.score}\n" +
                              $"  Play Time: {sessionData.playTime:F2}s\n" +
                              $"  (Not saved due to defeat)");
                }
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (enableDebugLogs)
            {
                Debug.Log("[GameResultCoordinator] Destroyed");
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// 현재 상태 로그 (디버깅용)
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log($"[GameResultCoordinator] Current State:\n" +
                      $"  OutcomeManager: {(outcomeManager != null ? "Connected" : "NULL")}\n" +
                      $"  SessionManager: {(sessionManager != null ? "Connected" : "NULL")}\n" +
                      $"  ProgressManager: {(progressManager != null ? "Connected" : "NULL")}\n" +
                      $"  BaseManager: {(baseManager != null ? "Connected" : "NULL")}\n" +
                      $"  Active Session: {(sessionManager != null && sessionManager.IsSessionActive ? sessionManager.CurrentStageId : "None")}");
        }

        #endregion
    }
}
