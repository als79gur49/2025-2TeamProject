using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 타일/유닛이 이 효과의 타겟이 될 수 있는지 여부를 판단하는 필터 베이스
    /// EffectDefinition.TargetFilter에 연결되어 사용됩니다.
    /// </summary>
    public abstract class TargetFilterDefinition : ScriptableObject
    {
        /// <summary>
        /// 주어진 타일/유닛이 조건을 만족하는지 여부를 반환합니다.
        /// </summary>
        public abstract bool Matches(Tile tile, Unit unit, GameContext context);

        /// <summary>
        /// UI/설명용 대상 텍스트를 반환합니다.
        /// 예: "적군 유닛", "아군 유닛", "빈 타일" 등
        /// 기본 구현은 빈 문자열입니다.
        /// </summary>
        public virtual string GetTargetDescription()
        {
            return string.Empty;
        }
    }
}
