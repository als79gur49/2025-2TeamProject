using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 유닛이 없는 빈 타일만 선택하는 필터
    /// Summon 효과 등에서 사용됩니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Filters/EmptyTile")]
    public class EmptyTileFilterDefinition : TargetFilterDefinition
    {
        public override bool Matches(Tile tile, Unit unit, GameContext context)
        {
            return tile != null && tile.OccupyingUnit == null;
        }

        public override string GetTargetDescription()
        {
            return "빈 타일";
        }
    }
}

