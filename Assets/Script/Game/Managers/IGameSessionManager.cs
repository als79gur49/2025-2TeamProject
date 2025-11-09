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
        void StartSession(string stageId);

        /// <summary>
        /// 게임 세션 종료 및 최종 데이터 반환
        /// </summary>
        /// <returns>게임 세션 데이터</returns>
        GameSessionData EndSession();
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
    }
}
