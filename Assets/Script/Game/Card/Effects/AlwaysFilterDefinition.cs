using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 항상 true를 반환하는 필터 (디버깅/기본값용)
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Filters/Always")]
    public class AlwaysFilterDefinition : TargetFilterDefinition
    {
        public override bool Matches(Tile tile, Unit unit, GameContext context)
        {
            return true;
        }

        public override string GetTargetDescription()
        {
            // 특별한 설명이 필요 없는 디버그/기본 필터
            return string.Empty;
        }
    }
}
