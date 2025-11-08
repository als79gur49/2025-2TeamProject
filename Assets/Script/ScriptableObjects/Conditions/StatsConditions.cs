using UnityEngine;
using Game.SaveSystem;
using System.Linq;

namespace Game.Data
{
    /// <summary>
    /// V2 시스템용 전체 통계 조건
    /// 플레이어의 전체 진행 통계 기반
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalStatsCondition", menuName = "Game/Unlock Conditions/Global Stats")]
    public class GlobalStatsCondition : UnlockCondition
    {
        [Header("Global Requirements")]
        [SerializeField]
        private int minimumTotalStagesUnlocked = 0;
        
        [SerializeField]
        private int minimumTotalStagesCleared = 0;
        
        [SerializeField]
        private int minimumTotalStars = 0;
        
        [SerializeField]
        private int minimumTotalScore = 0;
        
        [SerializeField]
        [Range(0f, 1f)]
        private float minimumOverallProgress = 0f;
        
        [Header("Play Time Requirements")]
        [SerializeField]
        private int minimumTotalPlayTime = 0;  // 초 단위
        
        [SerializeField]
        private int minimumPlaySessions = 0;

        public override bool Evaluate(StageProgressData progressData)
        {
            if (progressData?.globalStats == null)
                return false;

            var stats = progressData.globalStats;

            // 해금 스테이지 수 체크
            if (stats.totalStagesUnlocked < minimumTotalStagesUnlocked)
                return false;

            // 클리어 스테이지 수 체크
            if (stats.totalStagesCleared < minimumTotalStagesCleared)
                return false;

            // 총 별 개수 체크
            if (stats.totalStarCount < minimumTotalStars)
                return false;

            // 총 점수 체크
            if (stats.totalScore < minimumTotalScore)
                return false;

            // 전체 진행도 체크
            if (minimumOverallProgress > 0)
            {
                // 총 스테이지 수를 알아야 함 (외부에서 제공 필요)
                // 임시로 클리어/해금 비율로 계산
                float progress = stats.totalStagesUnlocked > 0 
                    ? (float)stats.totalStagesCleared / stats.totalStagesUnlocked
                    : 0f;
                    
                if (progress < minimumOverallProgress)
                    return false;
            }

            // 플레이 시간 체크
            if (stats.totalPlayTime < minimumTotalPlayTime)
                return false;

            // 플레이 세션 수 체크
            if (minimumPlaySessions > 0)
            {
                int totalSessions = progressData.stageRecords
                    .Sum(kvp => kvp.Value.playSessions.Count);
                    
                if (totalSessions < minimumPlaySessions)
                    return false;
            }

            return true;
        }

        public override float GetProgress(StageProgressData progressData)
        {
            if (progressData?.globalStats == null)
                return 0f;

            var stats = progressData.globalStats;
            float totalProgress = 0f;
            int checkCount = 0;

            // 각 조건별 진행도 계산
            if (minimumTotalStagesUnlocked > 0)
            {
                totalProgress += Mathf.Min(1f, (float)stats.totalStagesUnlocked / minimumTotalStagesUnlocked);
                checkCount++;
            }

            if (minimumTotalStagesCleared > 0)
            {
                totalProgress += Mathf.Min(1f, (float)stats.totalStagesCleared / minimumTotalStagesCleared);
                checkCount++;
            }

            if (minimumTotalStars > 0)
            {
                totalProgress += Mathf.Min(1f, (float)stats.totalStarCount / minimumTotalStars);
                checkCount++;
            }

            if (minimumTotalScore > 0)
            {
                totalProgress += Mathf.Min(1f, (float)stats.totalScore / minimumTotalScore);
                checkCount++;
            }

            if (minimumTotalPlayTime > 0)
            {
                totalProgress += Mathf.Min(1f, (float)stats.totalPlayTime / minimumTotalPlayTime);
                checkCount++;
            }

            if (minimumPlaySessions > 0)
            {
                int totalSessions = progressData.stageRecords
                    .Sum(kvp => kvp.Value.playSessions.Count);
                totalProgress += Mathf.Min(1f, (float)totalSessions / minimumPlaySessions);
                checkCount++;
            }

            return checkCount > 0 ? totalProgress / checkCount : 0f;
        }

        protected override string GetDefaultDescription()
        {
            // 가장 주요한 조건을 설명으로 사용
            if (minimumOverallProgress > 0)
                return $"Reach {minimumOverallProgress:P0} overall progress";
            
            if (minimumTotalStagesCleared > 0)
                return $"Clear {minimumTotalStagesCleared} stages total";
            
            if (minimumTotalStars > 0)
                return $"Earn {minimumTotalStars}★ total";
            
            if (minimumTotalScore > 0)
                return $"Score {minimumTotalScore:N0} points total";
            
            if (minimumTotalPlayTime > 0)
            {
                int hours = minimumTotalPlayTime / 3600;
                int minutes = (minimumTotalPlayTime % 3600) / 60;
                
                if (hours > 0)
                    return $"Play for {hours}h {minutes}m total";
                else
                    return $"Play for {minutes} minutes total";
            }

            return "Meet global requirements";
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (minimumTotalStagesUnlocked < 0) minimumTotalStagesUnlocked = 0;
            if (minimumTotalStagesCleared < 0) minimumTotalStagesCleared = 0;
            if (minimumTotalStars < 0) minimumTotalStars = 0;
            if (minimumTotalScore < 0) minimumTotalScore = 0;
            if (minimumTotalPlayTime < 0) minimumTotalPlayTime = 0;
            if (minimumPlaySessions < 0) minimumPlaySessions = 0;
            
            minimumOverallProgress = Mathf.Clamp01(minimumOverallProgress);
        }
#endif
    }

    /// <summary>
    /// V2 시스템용 특정 통계 조건
    /// 특정 스테이지의 상세 통계 기반
    /// </summary>
    [CreateAssetMenu(fileName = "StageStatsCondition", menuName = "Game/Unlock Conditions/Stage Stats")]
    public class StageStatsCondition : UnlockCondition
    {
        [Header("Target Stage")]
        [SerializeField]
        private string targetStageId = "";
        
        [Header("Performance Requirements")]
        [SerializeField]
        private int minimumScore = 0;
        
        [SerializeField]
        private float maximumClearTime = 0f;  // 초 단위
        
        [SerializeField]
        private int maximumDamageTaken = -1;  // -1 = 무제한
        
        [SerializeField]
        private int minimumEnemiesDefeated = 0;
        
        [SerializeField]
        private int minimumPerfectClears = 0;
        
        [Header("Custom Stats")]
        [SerializeField]
        private string customStatKey = "";
        
        [SerializeField]
        private int customStatMinValue = 0;

        public override bool Evaluate(StageProgressData progressData)
        {
            if (progressData == null || string.IsNullOrEmpty(targetStageId))
                return false;

            var record = progressData.GetRecord(targetStageId);
            if (record == null || record.statistics == null)
                return false;

            var stats = record.statistics;

            // 점수 체크
            if (record.bestScore < minimumScore)
                return false;

            // 클리어 시간 체크
            if (maximumClearTime > 0 && stats.bestClearTime > maximumClearTime)
                return false;

            // 데미지 체크
            if (maximumDamageTaken >= 0 && stats.totalDamageTaken > maximumDamageTaken)
                return false;

            // 적 처치 수 체크
            if (stats.totalEnemiesDefeated < minimumEnemiesDefeated)
                return false;

            // 퍼펙트 클리어 체크
            if (stats.perfectClearCount < minimumPerfectClears)
                return false;

            // 커스텀 통계 체크
            if (!string.IsNullOrEmpty(customStatKey))
            {
                if (!stats.customStats.ContainsKey(customStatKey) ||
                    stats.customStats[customStatKey] < customStatMinValue)
                {
                    return false;
                }
            }

            return true;
        }

        public override float GetProgress(StageProgressData progressData)
        {
            if (progressData == null || string.IsNullOrEmpty(targetStageId))
                return 0f;

            var record = progressData.GetRecord(targetStageId);
            if (record == null || record.statistics == null)
                return 0f;

            var stats = record.statistics;
            float totalProgress = 0f;
            int checkCount = 0;

            // 점수 진행도
            if (minimumScore > 0)
            {
                totalProgress += Mathf.Min(1f, (float)record.bestScore / minimumScore);
                checkCount++;
            }

            // 적 처치 진행도
            if (minimumEnemiesDefeated > 0)
            {
                totalProgress += Mathf.Min(1f, (float)stats.totalEnemiesDefeated / minimumEnemiesDefeated);
                checkCount++;
            }

            // 퍼펙트 클리어 진행도
            if (minimumPerfectClears > 0)
            {
                totalProgress += Mathf.Min(1f, (float)stats.perfectClearCount / minimumPerfectClears);
                checkCount++;
            }

            // 커스텀 통계 진행도
            if (!string.IsNullOrEmpty(customStatKey) && customStatMinValue > 0)
            {
                int currentValue = stats.customStats.ContainsKey(customStatKey) 
                    ? stats.customStats[customStatKey] : 0;
                totalProgress += Mathf.Min(1f, (float)currentValue / customStatMinValue);
                checkCount++;
            }

            return checkCount > 0 ? totalProgress / checkCount : 0f;
        }

        protected override string GetDefaultDescription()
        {
            string desc = $"In {targetStageId}:";

            if (minimumScore > 0)
                desc = $"Score {minimumScore:N0} in {targetStageId}";
            else if (maximumClearTime > 0)
                desc = $"Clear {targetStageId} in {maximumClearTime:F1} seconds";
            else if (maximumDamageTaken == 0)
                desc = $"No-damage clear {targetStageId}";
            else if (minimumPerfectClears > 0)
                desc = $"Perfect clear {targetStageId} {minimumPerfectClears} times";
            else if (minimumEnemiesDefeated > 0)
                desc = $"Defeat {minimumEnemiesDefeated} enemies in {targetStageId}";
            else if (!string.IsNullOrEmpty(customStatKey))
                desc = $"Achieve {customStatKey}:{customStatMinValue} in {targetStageId}";

            return desc;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (minimumScore < 0) minimumScore = 0;
            if (maximumClearTime < 0) maximumClearTime = 0;
            if (minimumEnemiesDefeated < 0) minimumEnemiesDefeated = 0;
            if (minimumPerfectClears < 0) minimumPerfectClears = 0;
            if (customStatMinValue < 0) customStatMinValue = 0;
        }
#endif
    }
}
