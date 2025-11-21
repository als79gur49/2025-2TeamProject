using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 여러 TargetFilterDefinition을 AND(모든 조건 만족)로 결합하는 필터
    /// EffectDefinition.TargetFilter에 할당하여 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "AndTargetFilter", menuName = "Game/Effects/Filters/Logic/AND")]
    public class AndTargetFilterDefinition : TargetFilterDefinition
    {
        [Header("AND Logic")]
        [SerializeField]
        private List<TargetFilterDefinition> filters = new List<TargetFilterDefinition>();

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
                // 별도의 조건이 없으면 모든 타겟 허용
                return true;
            }

            var validFilters = filters.Where(f => f != null && f != this);

            if (!validFilters.Any())
            {
                // 유효한 필터가 없으면 제한 없음으로 간주
                return true;
            }

            // 모든 하위 필터를 만족해야 함
            return validFilters.All(f => f.Matches(tile, unit, context));
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

            // 예: "(아군 유닛 AND 체력 50% 이하)"
            return $"({string.Join(" AND ", validDescriptions)})";
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 필터 목록을 정리 (null/자기 자신 제거)
        /// </summary>
        private void OnValidate()
        {
            // ScriptableObject에는 OnValidate가 virtual이 아니므로 override 사용 금지
            filters?.RemoveAll(f => f == null || f == this);
        }
#endif
    }
}

