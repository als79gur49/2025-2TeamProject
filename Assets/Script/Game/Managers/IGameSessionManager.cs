using System;
using System.Collections.Generic;
using Game.Data;

namespace Game.Managers
{
    /// <summary>
    /// 게임 세션 관리 인터페이스
    ///
    /// 책임:
    /// - 게임 플레이 중 실시간 데이터 추적
    /// - 세션 시작/종료 관리
    /// - 점수 및 통계 관리
    /// </summary>
    public interface IGameSessionManager
    {
        #region Properties
        /// <summary>
        /// 현재 스테이지 ID
        /// </summary>
        string CurrentStageId { get; }

        /// <summary>
        /// 현재 점수
        /// </summary>
        int CurrentScore { get; }

        /// <summary>
        /// 현재 통계 (읽기 전용 복사본)
        /// </summary>
        Dictionary<string, int> CurrentStatistics { get; }

        /// <summary>
        /// 플레이 시간 (초)
        /// </summary>
        float PlayTime { get; }

        /// <summary>
        /// 세션 활성 상태
        /// </summary>
        bool IsSessionActive { get; }
        #endregion

        #region Events
        /// <summary>
        /// 세션 시작 이벤트
        /// </summary>
        event Action<string> OnSessionStarted;

        /// <summary>
        /// 세션 종료 이벤트
        /// </summary>
        event Action<GameSessionData> OnSessionEnded;
        #endregion

        #region Session Management
        /// <summary>
        /// 게임 세션 시작
        /// </summary>
        /// <param name="stageId">스테이지 ID</param>
        /// <param name="stageData">스테이지 데이터 (점수 계산 규칙 포함)</param>
        void StartSession(string stageId, StageDataSO stageData);

        /// <summary>
        /// 게임 세션 종료 및 최종 데이터 반환
        /// </summary>
        /// <returns>게임 세션 데이터</returns>
        GameSessionData EndSessionWithVictory();
        
        /// <summary>
        /// 패배로 인한 게임 세션 종료 및 최종 데이터 반환
        /// </summary>
        /// <returns>게임 세션 데이터</returns>
        GameSessionData EndSessionWithDefeat();
        
        /// <summary>
        /// 중단(강제 종료 등)으로 인한 게임 세션 종료 및 최종 데이터 반환
        /// </summary>
        /// <returns>게임 세션 데이터</returns>
        GameSessionData EndSessionAborted();
        #endregion

        #region Data Update Methods
        /// <summary>
        /// 점수 추가
        /// </summary>
        /// <param name="points">추가할 점수</param>
        void AddScore(int points);

        /// <summary>
        /// 통계 증가
        /// </summary>
        /// <param name="key">통계 키</param>
        /// <param name="amount">증가량</param>
        void IncrementStatistic(string key, int amount = 1);

        /// <summary>
        /// 통계 설정
        /// </summary>
        /// <param name="key">통계 키</param>
        /// <param name="value">설정할 값</param>
        void SetStatistic(string key, int value);
        #endregion

        #region Convenience Methods (High-Level API)
        /// <summary>
        /// 적 처치 기록 (점수 계산 + 추가 + 통계 증가)
        /// </summary>
        /// <param name="enemyLevel">적 레벨</param>
        void RecordEnemyDefeat(int enemyLevel);

        /// <summary>
        /// 콤보 달성 기록 (콤보 보너스 점수 추가)
        /// </summary>
        /// <param name="comboCount">콤보 횟수</param>
        void RecordComboAchieved(int comboCount);

        /// <summary>
        /// 데미지 기록 (통계 증가 및 점수 반영)
        /// </summary>
        /// <param name="amount">데미지 양</param>
        /// <param name="isTaken">받은 데미지인지 (true: 받음, false: 입힘)</param>
        void RecordDamage(int amount, bool isTaken);

        /// <summary>
        /// 승리 보너스 계산 및 적용 (EndSession 호출 전 사용)
        /// HP 보너스와 시간 보너스를 자동으로 계산하여 점수에 추가
        /// </summary>
        /// <param name="playerHealthPercent">플레이어 베이스 HP 퍼센트 (0-100)</param>
        void ApplyVictoryBonus(float playerHealthPercent);
        #endregion
    }
}
