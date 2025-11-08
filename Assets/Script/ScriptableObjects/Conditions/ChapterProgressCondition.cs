using UnityEngine;
using Game.SaveSystem;
using System.Linq;

namespace Game.Data
{
    /// <summary>
    /// V2 시스템용 챕터 진행도 조건
    /// 챕터 전체 또는 특정 진행률 달성 조건
    /// </summary>
    [CreateAssetMenu(fileName = "ChapterProgressCondition", menuName = "Game/Unlock Conditions/Chapter Progress")]
    public class ChapterProgressCondition : UnlockCondition
    {
        [Header("Chapter Requirements")]
        [SerializeField]
        private string targetChapterId = "chapter1";
        
        [SerializeField]
        [Range(0f, 1f)]
        private float requiredProgress = 0.5f;
        
        [Header("Specific Requirements")]
        [SerializeField]
        private bool requireAllPerfect = false;
        
        [SerializeField]
        private int minimumClearedStages = 0;
        
        [SerializeField]
        private int minimumTotalStars = 0;
        
        [SerializeField]
        private int minimumTotalScore = 0;

        public override bool Evaluate(StageProgressData progressData)
        {
            if (progressData == null)
                return false;

            // 챕터 진행도 체크
            float chapterProgress = progressData.chapterProgress.ContainsKey(targetChapterId) 
                ? progressData.chapterProgress[targetChapterId] 
                : 0f;

            if (chapterProgress < requiredProgress)
                return false;

            // 챕터의 스테이지들 필터링
            var chapterStages = progressData.stageRecords
                .Where(kvp => kvp.Key.StartsWith(targetChapterId))
                .Select(kvp => kvp.Value)
                .ToList();

            // 퍼펙트 클리어 체크
            if (requireAllPerfect)
            {
                if (chapterStages.Any(s => s.state != StageState.Perfect && s.state != StageState.Locked))
                    return false;
            }

            // 최소 클리어 스테이지 수 체크
            if (minimumClearedStages > 0)
            {
                int clearedCount = chapterStages.Count(s => 
                    s.state == StageState.Cleared || s.state == StageState.Perfect);
                
                if (clearedCount < minimumClearedStages)
                    return false;
            }

            // 최소 총 별 개수 체크
            if (minimumTotalStars > 0)
            {
                int totalStars = chapterStages.Sum(s => s.bestStars);
                if (totalStars < minimumTotalStars)
                    return false;
            }

            // 최소 총 점수 체크
            if (minimumTotalScore > 0)
            {
                int totalScore = chapterStages.Sum(s => s.bestScore);
                if (totalScore < minimumTotalScore)
                    return false;
            }

            return true;
        }

        public override float GetProgress(StageProgressData progressData)
        {
            if (progressData == null)
                return 0f;

            float totalProgress = 0f;
            int checkCount = 0;

            // 기본 챕터 진행도
            float chapterProgress = progressData.chapterProgress.ContainsKey(targetChapterId) 
                ? progressData.chapterProgress[targetChapterId] 
                : 0f;
            
            totalProgress += Mathf.Min(1f, chapterProgress / requiredProgress);
            checkCount++;

            // 챕터 스테이지 데이터
            var chapterStages = progressData.stageRecords
                .Where(kvp => kvp.Key.StartsWith(targetChapterId))
                .Select(kvp => kvp.Value)
                .ToList();

            // 클리어 스테이지 진행도
            if (minimumClearedStages > 0)
            {
                int clearedCount = chapterStages.Count(s => 
                    s.state == StageState.Cleared || s.state == StageState.Perfect);
                totalProgress += Mathf.Min(1f, (float)clearedCount / minimumClearedStages);
                checkCount++;
            }

            // 별 개수 진행도
            if (minimumTotalStars > 0)
            {
                int totalStars = chapterStages.Sum(s => s.bestStars);
                totalProgress += Mathf.Min(1f, (float)totalStars / minimumTotalStars);
                checkCount++;
            }

            // 점수 진행도
            if (minimumTotalScore > 0)
            {
                int totalScore = chapterStages.Sum(s => s.bestScore);
                totalProgress += Mathf.Min(1f, (float)totalScore / minimumTotalScore);
                checkCount++;
            }

            return checkCount > 0 ? totalProgress / checkCount : 0f;
        }

        protected override string GetDefaultDescription()
        {
            string desc = "";

            if (requireAllPerfect)
            {
                desc = $"Perfect clear all stages in {targetChapterId}";
            }
            else if (requiredProgress >= 1f)
            {
                desc = $"Complete {targetChapterId}";
            }
            else
            {
                desc = $"Progress {targetChapterId} to {requiredProgress:P0}";
            }

            if (minimumTotalStars > 0)
            {
                desc += $" with {minimumTotalStars}★ total";
            }

            if (minimumTotalScore > 0)
            {
                desc += $" ({minimumTotalScore:N0} points)";
            }

            return desc;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            requiredProgress = Mathf.Clamp01(requiredProgress);
            if (minimumClearedStages < 0) minimumClearedStages = 0;
            if (minimumTotalStars < 0) minimumTotalStars = 0;
            if (minimumTotalScore < 0) minimumTotalScore = 0;
        }
#endif
    }
}
