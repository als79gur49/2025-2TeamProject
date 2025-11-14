using System.Collections.Generic;
using Game.Data;

namespace Game.AI.CardSelection
{
    /// <summary>
    /// 카드 선택 전략 인터페이스
    /// Strategy Pattern을 적용하여 다양한 카드 선택 로직을 교체 가능하게 함
    /// </summary>
    public interface ICardSelectionStrategy
    {
        /// <summary>
        /// 카드 풀에서 카드를 하나 선택하여 반환합니다
        /// </summary>
        /// <param name="pool">선택 가능한 카드 목록</param>
        /// <returns>선택된 카드 (풀이 비어있으면 null)</returns>
        CardData DrawCard(IReadOnlyList<CardData> pool);

        /// <summary>
        /// 전략 이름 반환 (디버깅 및 로깅용)
        /// </summary>
        string StrategyName { get; }
    }
}
