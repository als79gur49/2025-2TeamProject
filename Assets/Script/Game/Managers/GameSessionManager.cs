using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Services;

namespace Game.Managers
{
    /// <summary>
    /// 게임 세션 실시간 데이터 추적 매니저
    ///
    /// 책임:
    /// - 게임 플레이 중 실시간 점수 계산
    /// - 게임 플레이 중 실시간 통계 수집
    /// - 세션 시간 측정
    /// - 세션 종료 시 데이터 반환
    ///
    /// 씬 종속성: 게임 씬에만 존재
    /// </summary>
    public class GameSessionManager : MonoBehaviour, IGameSessionManager
    {
        #region Runtime Data
        private string currentStageId;
        private int currentScore;
        private Dictionary<string, int> currentStatistics;
        private DateTime sessionStartTime;
        private float sessionStartGameTime;
        private bool isSessionActive;
        private IScoringService scoringService;
        #endregion

        #region Public API
        /// <summary>
        /// 현재 스테이지 ID
        /// </summary>
        public string CurrentStageId => currentStageId;

        /// <summary>
        /// 현재 점수
        /// </summary>
        public int CurrentScore => currentScore;

        /// <summary>
        /// 현재 통계 (읽기 전용 복사본)
        /// </summary>
        public Dictionary<string, int> CurrentStatistics => new Dictionary<string, int>(currentStatistics);

        /// <summary>
        /// 플레이 시간 (초)
        /// </summary>
        public float PlayTime => isSessionActive ? Time.time - sessionStartGameTime : 0f;

        /// <summary>
        /// 세션 활성 상태
        /// </summary>
        public bool IsSessionActive => isSessionActive;
        #endregion

        #region Events
        /// <summary>
        /// 세션 시작 이벤트
        /// </summary>
        public event Action<string> OnSessionStarted;

        /// <summary>
        /// 세션 종료 이벤트
        /// </summary>
        public event Action<GameSessionData> OnSessionEnded;
        #endregion

        #region Session Management

        /// <summary>
        /// 게임 세션 시작
        /// </summary>
        /// <param name="stageId">스테이지 ID</param>
        /// <param name="stageData">스테이지 데이터 (점수 계산 규칙 포함)</param>
        public void StartSession(string stageId, StageDataSO stageData)
        {
            if (isSessionActive)
            {
                Debug.LogWarning($"[GameSessionManager] Session already active for stage: {currentStageId}. Ending previous session.");
                EndSession();
            }

            currentStageId = stageId;
            currentScore = 0;
            sessionStartTime = DateTime.Now;
            sessionStartGameTime = Time.time;
            isSessionActive = true;

            // ScoringService 초기화
            scoringService = new ScoringService();
            scoringService.Initialize(stageData);

            // 통계 초기화
            currentStatistics = new Dictionary<string, int>
            {
                { "enemies_defeated", 0 },
                { "damage_dealt", 0 },
                { "damage_taken", 0 },
                { "units_spawned", 0 },
                { "play_time", 0 }
            };

            OnSessionStarted?.Invoke(stageId);
            Debug.Log($"[GameSessionManager] Session started: {stageId}");
        }

        /// <summary>
        /// 게임 세션 종료 및 최종 데이터 반환
        /// </summary>
        /// <returns>게임 세션 데이터</returns>
        public GameSessionData EndSession()
        {
            if (!isSessionActive)
            {
                Debug.LogWarning("[GameSessionManager] No active session to end.");
                return null;
            }

            // 최종 플레이 시간 계산
            float finalPlayTime = Time.time - sessionStartGameTime;
            currentStatistics["play_time"] = Mathf.RoundToInt(finalPlayTime);

            // 세션 데이터 생성
            var sessionData = new GameSessionData
            {
                stageId = currentStageId,
                score = currentScore,
                statistics = new Dictionary<string, int>(currentStatistics),
                playTime = finalPlayTime,
                startTime = sessionStartTime,
                endTime = DateTime.Now
            };

            // 세션 상태 리셋
            isSessionActive = false;

            OnSessionEnded?.Invoke(sessionData);
            Debug.Log($"[GameSessionManager] Session ended: {sessionData}");

            return sessionData;
        }

        #endregion

        #region Data Update Methods

        /// <summary>
        /// 점수 추가
        /// </summary>
        /// <param name="points">추가할 점수</param>
        public void AddScore(int points)
        {
            if (!isSessionActive)
            {
                Debug.LogWarning("[GameSessionManager] Cannot add score: No active session.");
                return;
            }

            currentScore += points;
            currentScore = Mathf.Max(0, currentScore); // 음수 방지
        }

        /// <summary>
        /// 통계 증가
        /// </summary>
        /// <param name="key">통계 키</param>
        /// <param name="amount">증가량</param>
        public void IncrementStatistic(string key, int amount = 1)
        {
            if (!isSessionActive)
            {
                Debug.LogWarning("[GameSessionManager] Cannot increment statistic: No active session.");
                return;
            }

            if (currentStatistics.ContainsKey(key))
            {
                currentStatistics[key] += amount;
            }
            else
            {
                currentStatistics[key] = amount;
            }
        }

        /// <summary>
        /// 통계 설정
        /// </summary>
        /// <param name="key">통계 키</param>
        /// <param name="value">설정할 값</param>
        public void SetStatistic(string key, int value)
        {
            if (!isSessionActive)
            {
                Debug.LogWarning("[GameSessionManager] Cannot set statistic: No active session.");
                return;
            }

            currentStatistics[key] = value;
        }

        #endregion

        #region Convenience Methods (High-Level API)

        /// <summary>
        /// 적 처치 기록 (점수 계산 + 추가 + 통계 증가)
        /// </summary>
        /// <param name="enemyLevel">적 레벨</param>
        public void RecordEnemyDefeat(int enemyLevel)
        {
            if (!isSessionActive)
            {
                Debug.LogWarning("[GameSessionManager] Cannot record enemy defeat: No active session.");
                return;
            }

            if (scoringService == null)
            {
                Debug.LogWarning("[GameSessionManager] ScoringService not initialized.");
                return;
            }

            // 1. 점수 계산
            int points = scoringService.CalculateEnemyScore(enemyLevel);

            // 2. 점수 추가
            AddScore(points);

            // 3. 통계 증가
            IncrementStatistic("enemies_defeated");
        }

        /// <summary>
        /// 콤보 달성 기록 (콤보 보너스 점수 추가)
        /// </summary>
        /// <param name="comboCount">콤보 횟수</param>
        public void RecordComboAchieved(int comboCount)
        {
            if (!isSessionActive)
            {
                Debug.LogWarning("[GameSessionManager] Cannot record combo: No active session.");
                return;
            }

            if (scoringService == null)
            {
                Debug.LogWarning("[GameSessionManager] ScoringService not initialized.");
                return;
            }

            // 콤보 보너스 계산 및 추가
            int bonus = scoringService.CalculateComboBonus(comboCount);
            AddScore(bonus);
        }

        /// <summary>
        /// 데미지 기록 (통계 증가 및 점수 반영)
        /// </summary>
        /// <param name="amount">데미지 양</param>
        /// <param name="isTaken">받은 데미지인지 (true: 받음, false: 입힘)</param>
        public void RecordDamage(int amount, bool isTaken)
        {
            if (!isSessionActive)
            {
                Debug.LogWarning("[GameSessionManager] Cannot record damage: No active session.");
                return;
            }

            string statKey = isTaken ? "damage_taken" : "damage_dealt";
            IncrementStatistic(statKey, amount);
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (isSessionActive)
            {
                Debug.LogWarning("[GameSessionManager] Session still active on destroy. Ending session.");
                EndSession();
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// 현재 세션 상태 로그
        /// </summary>
        [ContextMenu("Log Current Session")]
        public void LogCurrentSession()
        {
            if (!isSessionActive)
            {
                Debug.Log("[GameSessionManager] No active session.");
                return;
            }

            Debug.Log($"[GameSessionManager] Current Session:\n" +
                      $"  Stage: {currentStageId}\n" +
                      $"  Score: {currentScore}\n" +
                      $"  Play Time: {PlayTime:F2}s\n" +
                      $"  Statistics: {string.Join(", ", currentStatistics)}");
        }

        #endregion
    }
}
