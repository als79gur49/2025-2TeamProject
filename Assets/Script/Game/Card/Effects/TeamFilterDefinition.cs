using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// 팀 관계(아군/적군/무관)를 기준으로 타겟을 필터링하는 기본 필터
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Filters/TeamFilter")]
    public class TeamFilterDefinition : TargetFilterDefinition
    {
        [SerializeField] private TeamRelation relation = TeamRelation.Any;

        public TeamRelation Relation => relation;

        public override bool Matches(Tile tile, Unit unit, GameContext context)
        {
            if (context == null) return false;

            // 유닛이 없는 타일은 어떤 경우에도 대상이 될 수 없음
            if (unit == null) return false;

            // Any: 관계에 상관없이 유닛이 존재하기만 하면 통과
            if (relation == TeamRelation.Any)
            {
                return true;
            }

            var unitTeam = unit.IsPlayerUnit ? TeamType.Player : TeamType.Enemy;
            var casterTeam = context.CasterTeam;

            return relation switch
            {
                TeamRelation.Ally    => unitTeam == casterTeam,
                TeamRelation.Enemy   => unitTeam != casterTeam,
                TeamRelation.Self    => unitTeam == casterTeam,          // Self는 Ally와 동일하게 해석 (유닛 단위에서는 구분 없음)
                TeamRelation.Neutral => unitTeam == TeamType.Neutral,
                _                    => false
            };
        }

        public override string GetTargetDescription()
        {
            return relation switch
            {
                TeamRelation.Ally    => "아군 유닛",
                TeamRelation.Enemy   => "적군 유닛",
                TeamRelation.Self    => "자신",
                TeamRelation.Neutral => "중립 유닛",
                TeamRelation.Any     => "모든 유닛",
                _                    => string.Empty
            };
        }
    }
}
