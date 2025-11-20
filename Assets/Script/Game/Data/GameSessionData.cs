using System;
using System.Collections.Generic;

namespace Game.Data
{
    /// <summary>
    /// 게임 세션 데이터 전송 객체 (DTO)
    /// GameSessionManager에서 수집한 실시간 게임 데이터를 전달하는 용도
    /// </summary>
    [Serializable]
    public class GameSessionData
    {
        /// <summary>
        /// 플레이한 스테이지 ID
        /// </summary>
        public string stageId;

        /// <summary>
        /// 최종 점수
        /// </summary>
        public int score;

        /// <summary>
        /// 게임 통계 (적 처치 수, 데미지, 유닛 소환 등)
        /// </summary>
        public Dictionary<string, int> statistics;

        /// <summary>
        /// 플레이 시간 (초)
        /// </summary>
        public float playTime;

        /// <summary>
        /// 세션 시작 시간
        /// </summary>
        public DateTime startTime;

        /// <summary>
        /// 세션 종료 시간
        /// </summary>
        public DateTime endTime;

        /// <summary>
        /// 세션 결과 (승리/패배/중단 등)
        /// </summary>
        public SessionOutcome outcome = SessionOutcome.Unknown;

        /// <summary>
        /// 생성자
        /// </summary>
        public GameSessionData()
        {
            statistics = new Dictionary<string, int>();
        }

        /// <summary>
        /// 세션 데이터 복사본 생성
        /// </summary>
        public GameSessionData Clone()
        {
            return new GameSessionData
            {
                stageId = this.stageId,
                score = this.score,
                statistics = new Dictionary<string, int>(this.statistics),
                playTime = this.playTime,
                startTime = this.startTime,
                endTime = this.endTime
            };
        }

        public override string ToString()
        {
            return $"GameSessionData [Stage: {stageId}, Score: {score}, PlayTime: {playTime:F2}s]";
        }
    }
}
