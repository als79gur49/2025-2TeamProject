using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Data;

namespace Game.Validators
{
    /// <summary>
    /// 덱 유효성 검증 클래스
    /// 덱 크기, 카드 복사본 제한, 희귀도 제한 등을 검증
    /// </summary>
    public static class DeckValidator
    {
        // 덱 크기 제약
        public const int MIN_DECK_SIZE = 30;
        public const int MAX_DECK_SIZE = 60;

        // 카드 복사본 제약
        public const int MAX_COPIES_PER_CARD = 3;
        public const int MAX_LEGENDARY_COPIES = 1; // 전설 카드는 1장 제한

        /// <summary>
        /// 덱 전체 유효성 검증 (빌드 완료 시)
        /// </summary>
        public static DeckValidationResult ValidateDeck(Dictionary<CardData, int> deckCards)
        {
            var result = new DeckValidationResult();

            if (deckCards == null || deckCards.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("덱이 비어있습니다.");
                return result;
            }

            int totalCards = deckCards.Sum(kvp => kvp.Value);

            // 1. 덱 크기 검증
            if (totalCards < MIN_DECK_SIZE)
            {
                result.IsValid = false;
                result.Errors.Add($"덱 크기가 부족합니다. (현재: {totalCards}장, 최소: {MIN_DECK_SIZE}장)");
            }

            if (totalCards > MAX_DECK_SIZE)
            {
                result.IsValid = false;
                result.Errors.Add($"덱 크기가 초과되었습니다. (현재: {totalCards}장, 최대: {MAX_DECK_SIZE}장)");
            }

            // 2. 카드별 복사본 수 검증
            foreach (var entry in deckCards)
            {
                var card = entry.Key;
                int count = entry.Value;

                // 전설 카드 제한
                if (card.Rarity == CardData.CardRarity.Legendary && count > MAX_LEGENDARY_COPIES)
                {
                    result.IsValid = false;
                    result.Errors.Add($"{card.CardName}: 전설 카드는 {MAX_LEGENDARY_COPIES}장까지만 가능합니다.");
                }
                // 일반 카드 제한
                else if (count > MAX_COPIES_PER_CARD)
                {
                    result.IsValid = false;
                    result.Errors.Add($"{card.CardName}: 최대 {MAX_COPIES_PER_CARD}장까지만 가능합니다.");
                }
            }

            // 3. 경고 메시지 (유효하지만 최적이 아닌 경우)
            if (result.IsValid)
            {
                // 마나 커브 균형 체크 (선택적 경고)
                CheckManaCurveBalance(deckCards, result);
            }

            return result;
        }

        /// <summary>
        /// 단일 카드 추가 가능 여부 검증
        /// </summary>
        public static bool CanAddCard(Dictionary<CardData, int> deckCards, CardData cardToAdd)
        {
            if (cardToAdd == null)
                return false;

            // 1. 덱 크기 체크
            int totalCards = deckCards.Sum(kvp => kvp.Value);
            if (totalCards >= MAX_DECK_SIZE)
                return false;

            // 2. 카드 복사본 수 체크
            int currentCount = deckCards.ContainsKey(cardToAdd) ? deckCards[cardToAdd] : 0;

            if (cardToAdd.Rarity == CardData.CardRarity.Legendary)
            {
                return currentCount < MAX_LEGENDARY_COPIES;
            }
            else
            {
                return currentCount < MAX_COPIES_PER_CARD;
            }
        }

        /// <summary>
        /// 마나 커브 균형 체크 (선택적 경고)
        /// </summary>
        private static void CheckManaCurveBalance(Dictionary<CardData, int> deckCards, DeckValidationResult result)
        {
            var manaCurve = new Dictionary<int, int>();

            foreach (var entry in deckCards)
            {
                int manaCost = entry.Key.ManaCost;
                int count = entry.Value;

                if (!manaCurve.ContainsKey(manaCost))
                    manaCurve[manaCost] = 0;

                manaCurve[manaCost] += count;
            }

            int totalCards = deckCards.Sum(kvp => kvp.Value);

            // 고비용 카드(6+ 마나)가 30% 이상이면 경고
            int highCostCards = manaCurve.Where(kvp => kvp.Key >= 6).Sum(kvp => kvp.Value);
            float highCostRatio = (float)highCostCards / totalCards;

            if (highCostRatio > 0.3f)
            {
                result.Warnings.Add($"고비용 카드({highCostCards}장)가 많습니다. 마나 커브 균형을 고려하세요.");
            }

            // 저비용 카드(0-2 마나)가 20% 미만이면 경고
            int lowCostCards = manaCurve.Where(kvp => kvp.Key <= 2).Sum(kvp => kvp.Value);
            float lowCostRatio = (float)lowCostCards / totalCards;

            if (lowCostRatio < 0.2f)
            {
                result.Warnings.Add($"저비용 카드({lowCostCards}장)가 부족합니다. 초반 플레이를 고려하세요.");
            }
        }

        /// <summary>
        /// 카드 제거 가능 여부 검증
        /// </summary>
        public static bool CanRemoveCard(Dictionary<CardData, int> deckCards, CardData cardToRemove)
        {
            if (cardToRemove == null)
                return false;

            // 덱에 해당 카드가 있는지 확인
            return deckCards.ContainsKey(cardToRemove) && deckCards[cardToRemove] > 0;
        }
    }
}
