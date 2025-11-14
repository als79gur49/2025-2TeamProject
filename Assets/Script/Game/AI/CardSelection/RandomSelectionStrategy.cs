using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.AI.CardSelection
{
    /// <summary>
    /// 완전 랜덤 카드 선택 전략
    /// 카드 풀의 모든 카드가 동일한 확률로 선택됩니다
    /// </summary>
    public class RandomSelectionStrategy : ICardSelectionStrategy
    {
        public string StrategyName => "Random Selection";

        /// <summary>
        /// 카드 풀에서 완전 랜덤하게 카드를 선택합니다
        /// </summary>
        /// <param name="pool">선택 가능한 카드 목록</param>
        /// <returns>랜덤으로 선택된 카드</returns>
        public CardData DrawCard(IReadOnlyList<CardData> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("[RandomSelectionStrategy] Card pool is empty or null");
                return null;
            }

            // Unity의 Random을 사용하여 균등 분포 랜덤 선택
            int randomIndex = Random.Range(0, pool.Count);
            CardData selectedCard = pool[randomIndex];

            Debug.Log($"[RandomSelectionStrategy] Drew card: {selectedCard?.CardName ?? "null"} (Index: {randomIndex}/{pool.Count})");
            return selectedCard;
        }
    }
}
