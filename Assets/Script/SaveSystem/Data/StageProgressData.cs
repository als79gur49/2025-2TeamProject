using System;
using System.Collections.Generic;

namespace Game.SaveSystem
{
    /// <summary>
    /// 개별 스테이지 기록
    /// </summary>
    [Serializable]
    public class StageRecord
    {
        /// <summary>
        /// 챕터 번호
        /// </summary>
        public int chapter;

        /// <summary>
        /// 스테이지 번호
        /// </summary>
        public int stage;

        /// <summary>
        /// 잠금 해제 여부
        /// </summary>
        public bool isUnlocked;

        /// <summary>
        /// 클리어 여부
        /// </summary>
        public bool isCleared;

        /// <summary>
        /// 최고 점수
        /// </summary>
        public int bestScore;

        /// <summary>
        /// 획득한 최고 별 개수
        /// </summary>
        public int bestStars;

        /// <summary>
        /// 클리어 횟수
        /// </summary>
        public int clearCount;

        /// <summary>
        /// 첫 클리어 시간
        /// </summary>
        public DateTime firstClearDate;

        /// <summary>
        /// 마지막 플레이 시간
        /// </summary>
        public DateTime lastPlayDate;

        public StageRecord()
        {
            chapter = 1;
            stage = 1;
            isUnlocked = false;
            isCleared = false;
            bestScore = 0;
            bestStars = 0;
            clearCount = 0;
            firstClearDate = DateTime.MinValue;
            lastPlayDate = DateTime.MinValue;
        }
    }

    /// <summary>
    /// 스테이지 진행도 데이터
    /// 현재 진행 상황 및 모든 스테이지 기록
    /// </summary>
    [Serializable]
    public class StageProgressData
    {
        /// <summary>
        /// 현재 진행 중인 챕터
        /// </summary>
        public int currentChapter;

        /// <summary>
        /// 현재 진행 중인 스테이지
        /// </summary>
        public int currentStage;

        /// <summary>
        /// 전체 스테이지 기록 목록
        /// </summary>
        public List<StageRecord> stageRecords;

        /// <summary>
        /// 마지막 수정 시간
        /// </summary>
        public DateTime lastModified;

        /// <summary>
        /// 기본 생성자 - 초기값 설정
        /// </summary>
        public StageProgressData()
        {
            currentChapter = 1;
            currentStage = 1;
            stageRecords = new List<StageRecord>();
            lastModified = DateTime.Now;
        }
    }
}
