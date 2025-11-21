using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 여러 TargetFilterDefinition을 OR(하나 이상 만족)로 결합하는 필터
    /// requiredFilterCount를 통해 "N개 이상 만족" 조건도 표현할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "OrTargetFilter", menuName = "Game/Effects/Filters/Logic/OR")]
    public class OrTargetFilterDefinition : TargetFilterDefinition
    {
        [Header("OR Logic")]
        [SerializeField]
        private List<TargetFilterDefinition> filters = new List<TargetFilterDefinition>();

        [SerializeField]
        [Range(1, 10)]
        private int requiredFilterCount = 1;

        /// <summary>
        /// 등록된 하위 필터 목록을 반환합니다. null/자기 자신은 포함되지 않습니다.
        /// </summary>
        public List<TargetFilterDefinition> GetFilters()
        {
            return filters?.Where(f => f != null && f != this).ToList() ?? new List<TargetFilterDefinition>();
        }

        public override bool Matches(Tile tile, Unit unit, GameContext context)
        {
            if (filters == null || filters.Count == 0)
            {
                // OR는 최소 하나 이상의 조건이 있어야 의미가 있으므로, 기본값은 false
                return false;
            }

            var validFilters = filters.Where(f => f != null && f != this).ToList();

            if (validFilters.Count == 0)
            {
                return false;
            }

            int satisfiedCount = validFilters.Count(f => f.Matches(tile, unit, context));
            int required = Mathf.Clamp(requiredFilterCount, 1, validFilters.Count);
            return satisfiedCount >= required;
        }

        public override string GetTargetDescription()
        {
            if (filters == null || filters.Count == 0)
            {
                return string.Empty;
            }

            var validDescriptions = filters
                .Where(f => f != null && f != this)
                .Select(f => f.GetTargetDescription())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (validDescriptions.Count == 0)
            {
                return string.Empty;
            }

            if (validDescriptions.Count == 1)
            {
                return validDescriptions[0];
            }

            string joined = string.Join(" OR ", validDescriptions);

            if (requiredFilterCount <= 1)
            {
                // 예: "(아군 유닛 OR 적군 유닛)"
                return $"({joined})";
            }

            // 예: "다음 중 2개 이상: (조건1 OR 조건2 OR 조건3)"
            return $"다음 중 {requiredFilterCount}개 이상: ({joined})";
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 필터 목록 정리 및 requiredFilterCount 보정
        /// </summary>
        private void OnValidate()
        {
            // ScriptableObject에는 OnValidate가 virtual이 아니므로 override 사용 금지
            filters?.RemoveAll(f => f == null || f == this);

            if (filters != null && filters.Count > 0)
            {
                requiredFilterCount = Mathf.Clamp(requiredFilterCount, 1, filters.Count);
            }
        }
#endif
    }
}

