using UnityEngine;
using Game.SaveSystem;
using System.Collections.Generic;
using System.Linq;

namespace Game.Data
{
    /// <summary>
    /// V2 시스템용 AND 복합 조건
    /// 모든 하위 조건을 만족해야 함
    /// </summary>
    [CreateAssetMenu(fileName = "AndCondition", menuName = "Game/Unlock Conditions/Logic/AND")]
    public class AndCondition : UnlockCondition
    {
        [Header("AND Logic")]
        [SerializeField]
        private List<UnlockCondition> conditions = new List<UnlockCondition>();
        
        [SerializeField]
        private bool requireAllConditions = true;

        public override bool Evaluate(StageProgressData progressData)
        {
            if (conditions == null || conditions.Count == 0)
                return true;  // 조건이 없으면 통과

            var validConditions = conditions.Where(c => c != null);
            
            if (!validConditions.Any())
                return true;

            // 모든 조건이 true여야 함
            return validConditions.All(c => c.Evaluate(progressData));
        }

        public override float GetProgress(StageProgressData progressData)
        {
            if (conditions == null || conditions.Count == 0)
                return 1f;

            var validConditions = conditions.Where(c => c != null).ToList();
            
            if (validConditions.Count == 0)
                return 1f;

            // 각 조건의 진행도 평균
            float totalProgress = validConditions.Sum(c => c.GetProgress(progressData));
            return totalProgress / validConditions.Count;
        }

        protected override string GetDefaultDescription()
        {
            if (conditions == null || conditions.Count == 0)
                return "No conditions";

            var validConditions = conditions.Where(c => c != null).ToList();
            if (validConditions.Count == 0)
                return "No valid conditions";

            if (validConditions.Count == 1)
                return validConditions[0].GetDescription();

            var descriptions = validConditions
                .Select(c => $"• {c.GetDescription()}")
                .ToList();

            return $"Complete ALL:\n{string.Join("\n", descriptions)}";
        }

        public List<UnlockCondition> GetConditions()
        {
            return conditions.Where(c => c != null).ToList();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // 자기 자신 제거 (순환 참조 방지)
            conditions?.RemoveAll(c => c == this);
        }
#endif
    }

    /// <summary>
    /// V2 시스템용 OR 복합 조건
    /// 하나 이상의 조건을 만족하면 됨
    /// </summary>
    [CreateAssetMenu(fileName = "OrCondition", menuName = "Game/Unlock Conditions/Logic/OR")]
    public class OrCondition : UnlockCondition
    {
        [Header("OR Logic")]
        [SerializeField]
        private List<UnlockCondition> conditions = new List<UnlockCondition>();
        
        [SerializeField]
        [Range(1, 10)]
        private int requiredConditionCount = 1;

        public override bool Evaluate(StageProgressData progressData)
        {
            if (conditions == null || conditions.Count == 0)
                return false;  // OR는 최소 하나는 있어야 함

            var validConditions = conditions.Where(c => c != null);
            
            if (!validConditions.Any())
                return false;

            // 만족하는 조건 개수 확인
            int satisfiedCount = validConditions.Count(c => c.Evaluate(progressData));
            return satisfiedCount >= requiredConditionCount;
        }

        public override float GetProgress(StageProgressData progressData)
        {
            if (conditions == null || conditions.Count == 0)
                return 0f;

            var validConditions = conditions.Where(c => c != null).ToList();
            
            if (validConditions.Count == 0)
                return 0f;

            // 가장 높은 진행도 반환 (OR 특성)
            if (requiredConditionCount == 1)
            {
                return validConditions.Max(c => c.GetProgress(progressData));
            }

            // 여러 개 필요한 경우 상위 N개의 평균
            var progresses = validConditions
                .Select(c => c.GetProgress(progressData))
                .OrderByDescending(p => p)
                .Take(requiredConditionCount)
                .ToList();

            return progresses.Count > 0 ? progresses.Average() : 0f;
        }

        protected override string GetDefaultDescription()
        {
            if (conditions == null || conditions.Count == 0)
                return "No conditions";

            var validConditions = conditions.Where(c => c != null).ToList();
            if (validConditions.Count == 0)
                return "No valid conditions";

            if (validConditions.Count == 1)
                return validConditions[0].GetDescription();

            var descriptions = validConditions
                .Select(c => $"• {c.GetDescription()}")
                .ToList();

            string requirement = requiredConditionCount > 1 
                ? $"Complete {requiredConditionCount} of" 
                : "Complete ANY";

            return $"{requirement}:\n{string.Join("\n", descriptions)}";
        }

        public List<UnlockCondition> GetConditions()
        {
            return conditions.Where(c => c != null).ToList();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // 자기 자신 제거
            conditions?.RemoveAll(c => c == this);
            
            // 필요 조건 수 제한
            if (conditions != null && requiredConditionCount > conditions.Count)
                requiredConditionCount = Mathf.Max(1, conditions.Count);
        }
#endif
    }
}
