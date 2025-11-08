using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.SaveSystem
{
    /// <summary>
    /// 스테이지 상태 열거형
    /// </summary>
    [Serializable]
    public enum StageState
    {
        Locked = 0,      // 잠김
        Unlocked = 1,    // 해금됨
        InProgress = 2,  // 진행중 (시작했지만 클리어하지 못함)
        Cleared = 3,     // 클리어
        Perfect = 4      // 퍼펙트 클리어 (3성)
    }

    /// <summary>
    /// 플레이 세션 정보
    /// </summary>
    [Serializable]
    public class PlaySession
    {
        public DateTime startTime;
        public DateTime endTime;
        public int score;
        public int stars;
        public float playTime;  // 실제 플레이 시간 (초)
        public bool completed;
        public Dictionary<string, int> statistics;  // 킬수, 데미지 등 세션 통계

        public PlaySession()
        {
            startTime = DateTime.Now;
            statistics = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// 개별 스테이지 기록
    /// Dictionary 기반 빠른 접근과 확장 가능한 구조
    /// </summary>
    [Serializable]
    public class StageRecord
    {
        public string stageId;           // 유니크 스테이지 ID (예: "chapter1_stage3")
        public StageState state;         // 현재 상태
        public int bestScore;            // 최고 점수
        public int bestStars;            // 최고 별 개수
        public List<PlaySession> playSessions;  // 모든 플레이 세션 기록
        public DateTime firstUnlockDate; // 첫 해금 시간
        public DateTime firstClearDate;  // 첫 클리어 시간
        public DateTime lastPlayDate;    // 마지막 플레이 시간
        
        // 확장 가능한 메타데이터
        public Dictionary<string, object> metadata;
        
        // 통계 데이터
        public StageStatistics statistics;

        public StageRecord(string id)
        {
            stageId = id;
            state = StageState.Locked;
            playSessions = new List<PlaySession>();
            metadata = new Dictionary<string, object>();
            statistics = new StageStatistics();
        }

        /// <summary>
        /// 총 플레이 횟수
        /// </summary>
        public int TotalPlayCount => playSessions.Count;

        /// <summary>
        /// 클리어 횟수
        /// </summary>
        public int ClearCount => playSessions.Count(s => s.completed);

        /// <summary>
        /// 평균 점수
        /// </summary>
        public float AverageScore
        {
            get
            {
                if (playSessions.Count == 0) return 0;
                return ((float)playSessions.Where(s => s.completed).Average(s => s.score));
            }
        }

        /// <summary>
        /// 총 플레이 시간
        /// </summary>
        public float TotalPlayTime => playSessions.Sum(s => s.playTime);
    }

    /// <summary>
    /// 스테이지 통계 정보
    /// </summary>
    [Serializable]
    public class StageStatistics
    {
        public int totalEnemiesDefeated;
        public int totalDamageDealt;
        public int totalDamageTaken;
        public int perfectClearCount;  // 노데미지 클리어
        public float bestClearTime;
        public Dictionary<string, int> customStats;

        public StageStatistics()
        {
            customStats = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// 전체 진행도 통계
    /// </summary>
    [Serializable]
    public class PlayerProgressStats
    {
        public int totalStagesUnlocked;
        public int totalStagesCleared;
        public int totalStarCount;
        public int totalPlayTime;
        public int totalScore;
        public DateTime firstPlayDate;
        public DateTime lastPlayDate;
        public Dictionary<string, int> achievements;

        public PlayerProgressStats()
        {
            achievements = new Dictionary<string, int>();
            firstPlayDate = DateTime.Now;
        }
    }

    /// <summary>
    /// 스테이지 진행도 데이터
    /// 효율적인 Dictionary 기반 구조
    /// </summary>
    [Serializable]
    public class StageProgressData
    {
        /// <summary>
        /// 스테이지 ID를 키로 하는 레코드 Dictionary
        /// </summary>
        public Dictionary<string, StageRecord> stageRecords;

        /// <summary>
        /// 현재 진행중인 스테이지 ID
        /// </summary>
        public string currentStageId;

        /// <summary>
        /// 마지막으로 해금된 스테이지 ID
        /// </summary>
        public string lastUnlockedStageId;

        /// <summary>
        /// 전체 진행도 통계
        /// </summary>
        public PlayerProgressStats globalStats;

        /// <summary>
        /// 챕터별 진행 상태 (챕터ID -> 진행률)
        /// </summary>
        public Dictionary<string, float> chapterProgress;

        /// <summary>
        /// 데이터 버전 (향후 마이그레이션용)
        /// </summary>
        public int dataVersion;

        /// <summary>
        /// 마지막 수정 시간
        /// </summary>
        public DateTime lastModified;

        public StageProgressData()
        {
            stageRecords = new Dictionary<string, StageRecord>();
            globalStats = new PlayerProgressStats();
            chapterProgress = new Dictionary<string, float>();
            dataVersion = 2;
            lastModified = DateTime.Now;
        }

        /// <summary>
        /// 스테이지 레코드 가져오기 (없으면 생성)
        /// </summary>
        public StageRecord GetOrCreateRecord(string stageId)
        {
            if (!stageRecords.ContainsKey(stageId))
            {
                stageRecords[stageId] = new StageRecord(stageId);
            }
            return stageRecords[stageId];
        }

        /// <summary>
        /// 스테이지 레코드 가져오기
        /// </summary>
        public StageRecord GetRecord(string stageId)
        {
            return stageRecords.TryGetValue(stageId, out var record) ? record : null;
        }

        /// <summary>
        /// 스테이지 상태 확인
        /// </summary>
        public StageState GetStageState(string stageId)
        {
            var record = GetRecord(stageId);
            return record?.state ?? StageState.Locked;
        }

        /// <summary>
        /// 스테이지 해금 여부
        /// </summary>
        public bool IsStageUnlocked(string stageId)
        {
            var state = GetStageState(stageId);
            return state != StageState.Locked;
        }

        /// <summary>
        /// 스테이지 클리어 여부
        /// </summary>
        public bool IsStageCleared(string stageId)
        {
            var state = GetStageState(stageId);
            return state == StageState.Cleared || state == StageState.Perfect;
        }

        /// <summary>
        /// 전체 진행률 계산
        /// </summary>
        public float CalculateOverallProgress(int totalStageCount)
        {
            if (totalStageCount == 0) return 0f;
            
            int clearedCount = stageRecords.Count(r => 
                r.Value.state == StageState.Cleared || 
                r.Value.state == StageState.Perfect);
            
            return (float)clearedCount / totalStageCount;
        }

        /// <summary>
        /// 챕터별 진행률 업데이트
        /// </summary>
        public void UpdateChapterProgress(string chapterId, float progress)
        {
            chapterProgress[chapterId] = Mathf.Clamp01(progress);
            lastModified = DateTime.Now;
        }
    }

    /// <summary>
    /// 저장 데이터 래퍼 (SaveGameManager 호환용)
    /// </summary>
    [Serializable]
    public class StageProgressSaveData
    {
        public string version = "2.0";
        public StageProgressData progressData;
        public DateTime saveTime;

        public StageProgressSaveData(StageProgressData data)
        {
            progressData = data;
            saveTime = DateTime.Now;
        }
    }
}
