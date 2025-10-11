using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 0/1 Knapsack 알고리즘을 사용하여 최적의 카드 조합을 선택하는 유틸리티 클래스 (v2.0)
    /// 동적 계획법(Dynamic Programming)을 통해 주어진 마나 내에서 최대 가치의 카드 조합을 찾습니다.
    /// v2.0: 외부에서 계산된 상황 가치(CardValueInfo)를 사용하여 필드 상황을 반영합니다.
    /// </summary>
    public static class KnapsackCardSelector
    {
        /// <summary>
        /// 주어진 마나 내에서 최대 가치를 가지는 카드 조합을 선택합니다.
        /// v2.0: CardValueInfo를 받아 외부에서 계산된 상황 가치와 최적 위치를 사용합니다.
        /// </summary>
        /// <param name="availableCards">가치가 계산된 카드 정보 리스트</param>
        /// <param name="maxMana">최대 사용 가능 마나 (가방의 용량)</param>
        /// <returns>선택된 최적의 카드 정보 리스트 (위치 포함)</returns>
        public static List<CardValueInfo> SelectOptimalCards(List<CardValueInfo> availableCards, int maxMana)
        {
            if (availableCards == null || availableCards.Count == 0)
            {
                Debug.LogWarning("[KnapsackCardSelector v2.0] No cards available for selection");
                return new List<CardValueInfo>();
            }

            if (maxMana <= 0)
            {
                Debug.LogWarning($"[KnapsackCardSelector v2.0] Invalid maxMana: {maxMana}");
                return new List<CardValueInfo>();
            }

            int cardCount = availableCards.Count;

            // dp[i, w] = i번째 카드까지 고려하고, 현재 마나가 w일 때의 최대 가치
            int[,] dp = new int[cardCount + 1, maxMana + 1];

            // 동적 계획법: 각 카드에 대해 각 마나 용량에서의 최대 가치 계산
            for (int i = 1; i <= cardCount; i++)
            {
                var cardInfo = availableCards[i - 1];
                int cost = cardInfo.Cost;        // 카드의 마나 비용 = 무게
                int value = cardInfo.Value;      // 외부에서 계산된 상황 가치

                for (int w = 1; w <= maxMana; w++)
                {
                    // 현재 카드의 마나 비용이 w보다 크면, 이 카드는 담을 수 없음
                    if (cost > w)
                    {
                        dp[i, w] = dp[i - 1, w];
                    }
                    else
                    {
                        // 카드를 담지 않는 경우(dp[i-1, w])와
                        // 카드를 담는 경우(value + dp[i-1, w-cost]) 중 더 큰 가치를 선택
                        dp[i, w] = Mathf.Max(dp[i - 1, w], value + dp[i - 1, w - cost]);
                    }
                }
            }

            // DP 테이블 역추적하여 선택된 카드 찾기
            var selectedCards = new List<CardValueInfo>();
            int currentMana = maxMana;

            for (int i = cardCount; i > 0 && currentMana > 0; i--)
            {
                // dp[i, currentMana]와 dp[i-1, currentMana]가 다르다는 것은
                // i번째 카드가 선택되었다는 의미
                if (dp[i, currentMana] != dp[i - 1, currentMana])
                {
                    var selectedCard = availableCards[i - 1];
                    selectedCards.Add(selectedCard);
                    currentMana -= selectedCard.Cost;

                    Debug.Log($"[KnapsackCardSelector v2.0] Selected: {selectedCard.Card.CardName} " +
                             $"(Cost: {selectedCard.Cost}, Value: {selectedCard.Value}, Position: {selectedCard.Position})");
                }
            }

            // 선택 결과 요약
            int totalCost = selectedCards.Sum(c => c.Cost);
            int totalValue = selectedCards.Sum(c => c.Value);

            Debug.Log($"[KnapsackCardSelector v2.0] Optimization complete: " +
                     $"Selected {selectedCards.Count} cards, " +
                     $"Total Cost: {totalCost}/{maxMana}, " +
                     $"Total Value: {totalValue}");

            // 선택 순서를 원래 순서로 복원 (역추적 순서가 역순이므로)
            selectedCards.Reverse();

            return selectedCards;
        }

        /// <summary>
        /// 디버깅용: 카드 정보 리스트의 총 가치를 계산
        /// </summary>
        public static int GetTotalValue(List<CardValueInfo> cards)
        {
            return cards?.Sum(c => c.Value) ?? 0;
        }

        /// <summary>
        /// 디버깅용: 카드 정보 리스트의 총 비용을 계산
        /// </summary>
        public static int GetTotalCost(List<CardValueInfo> cards)
        {
            return cards?.Sum(c => c.Cost) ?? 0;
        }
    }
}
