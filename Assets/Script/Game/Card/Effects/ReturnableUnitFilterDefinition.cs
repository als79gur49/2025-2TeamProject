using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 카드 기반으로 소환된 유닛만을 타겟으로 허용하는 필터
    /// UnitCardLink 컴포넌트에 SourceCard가 설정된 유닛만 통과합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Filters/ReturnableUnit")]
    public class ReturnableUnitFilterDefinition : TargetFilterDefinition
    {
        public override bool Matches(Tile tile, Unit unit, GameContext context)
        {
            if (tile == null || unit == null)
            {
                return false;
            }

            if (!unit.IsAlive)
            {
                return false;
            }

            var link = unit.GetComponent<UnitCardLink>();
            if (link == null || link.SourceCard == null)
            {
                return false;
            }

            return true;
        }

        public override string GetTargetDescription()
        {
            return "카드로 소환된 유닛";
        }
    }
}

