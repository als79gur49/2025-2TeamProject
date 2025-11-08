using UnityEngine;
using Game.SaveSystem;
using System.Linq;

namespace Game.Data
{
    /// <summary>
    /// V2 시스템용 스테이지 클리어 조건
    /// StageId 기반으로 동작
    /// </summary>
    [CreateAssetMenu(fileName = "StageClearCondition", menuName = "Game/Unlock Conditions/Stage Clear")]
    public class StageClearCondition : UnlockCondition
    {
        [Header("Stage Clear Requirements")]
        [SerializeField]
        private string requiredStageId = "";
        
        [SerializeField]
        [Range(0, 3)]
        private int minimumStars = 0;
        
        [SerializeField]
        private StageState minimumState = StageState.Cleared;
        
        [Header("Optional Requirements")]
        [SerializeField]
        private bool requirePerfectClear = false;
        
        [SerializeField]
        private int minimumClearCount = 1;

        public override bool Evaluate(StageProgressData progressData)
        {
            if (progressData == null || string.IsNullOrEmpty(requiredStageId))
                return false;

            var record = progressData.GetRecord(requiredStageId);
            if (record == null)
                return false;

            // 상태 체크
            if (record.state < minimumState)
                return false;

            // 퍼펙트 클리어 체크
            if (requirePerfectClear && record.state != StageState.Perfect)
                return false;

            // 별 개수 체크
            if (record.bestStars < minimumStars)
                return false;

            // 클리어 횟수 체크
            if (record.ClearCount < minimumClearCount)
                return false;

            return true;
        }

        public override float GetProgress(StageProgressData progressData)
        {
            if (progressData == null || string.IsNullOrEmpty(requiredStageId))
                return 0f;

            var record = progressData.GetRecord(requiredStageId);
            if (record == null)
                return 0f;

            float progress = 0f;
            int totalChecks = 0;

            // 상태 진행도
            totalChecks++;
            if (record.state >= minimumState)
                progress += 1f;
            else if (record.state != StageState.Locked)
                progress += 0.5f;

            // 별 진행도
            if (minimumStars > 0)
            {
                totalChecks++;
                progress += (float)record.bestStars / minimumStars;
            }

            // 클리어 횟수 진행도
            if (minimumClearCount > 1)
            {
                totalChecks++;
                progress += Mathf.Min(1f, (float)record.ClearCount / minimumClearCount);
            }

            return totalChecks > 0 ? progress / totalChecks : 0f;
        }

        protected override string GetDefaultDescription()
        {
            string desc = $"Clear {requiredStageId}";

            if (requirePerfectClear)
                desc = $"Perfect clear {requiredStageId}";
            else if (minimumStars > 0)
                desc = $"Get {minimumStars}★ in {requiredStageId}";
            
            if (minimumClearCount > 1)
                desc += $" ({minimumClearCount} times)";

            return desc;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (minimumStars < 0) minimumStars = 0;
            if (minimumStars > 3) minimumStars = 3;
            if (minimumClearCount < 1) minimumClearCount = 1;
        }
#endif
    }
}
