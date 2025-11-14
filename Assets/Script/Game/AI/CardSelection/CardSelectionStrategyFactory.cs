using UnityEngine;

namespace Game.AI.CardSelection
{
    /// <summary>
    /// 카드 선택 전략 팩토리
    /// SelectionType에 따라 적절한 ICardSelectionStrategy 인스턴스를 생성합니다
    /// </summary>
    public static class CardSelectionStrategyFactory
    {
        /// <summary>
        /// SelectionType에 맞는 전략 인스턴스를 생성합니다
        /// </summary>
        /// <param name="type">생성할 전략 타입</param>
        /// <param name="weightSettings">RarityWeighted 전략용 가중치 설정 (선택적)</param>
        /// <returns>생성된 전략 인스턴스</returns>
        public static ICardSelectionStrategy CreateStrategy(SelectionType type, RarityWeightSettings weightSettings = null)
        {
            switch (type)
            {
                case SelectionType.Random:
                    Debug.Log("[CardSelectionStrategyFactory] Creating RandomSelectionStrategy");
                    return new RandomSelectionStrategy();

                case SelectionType.RarityWeighted:
                    Debug.Log("[CardSelectionStrategyFactory] Creating RarityWeightedSelectionStrategy");
                    return new RarityWeightedSelectionStrategy(weightSettings);

                default:
                    Debug.LogWarning($"[CardSelectionStrategyFactory] Unknown SelectionType: {type}, falling back to Random");
                    return new RandomSelectionStrategy();
            }
        }

        /// <summary>
        /// 기본 설정으로 전략을 생성합니다
        /// </summary>
        /// <param name="type">생성할 전략 타입</param>
        /// <returns>생성된 전략 인스턴스</returns>
        public static ICardSelectionStrategy CreateDefaultStrategy(SelectionType type)
        {
            return CreateStrategy(type, null);
        }
    }
}
