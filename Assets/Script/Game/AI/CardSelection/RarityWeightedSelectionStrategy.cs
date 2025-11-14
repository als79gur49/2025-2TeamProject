using System;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using UnityEngine;

namespace Game.AI.CardSelection
{
    /// <summary>
    /// 등급 기반 가중치 선택 설정
    /// Unity Inspector에서 직렬화 가능
    /// </summary>
    [Serializable]
    public class RarityWeightSettings
    {
        [Header("Rarity Weight Configuration")]
        [Tooltip("Common 등급 카드의 가중치 (기본: 40)")]
        [Range(1, 100)]
        public int commonWeight = 40;

        [Tooltip("Uncommon 등급 카드의 가중치 (기본: 30)")]
        [Range(1, 100)]
        public int uncommonWeight = 30;

        [Tooltip("Rare 등급 카드의 가중치 (기본: 20)")]
        [Range(1, 100)]
        public int rareWeight = 20;

        [Tooltip("Epic 등급 카드의 가중치 (기본: 8)")]
        [Range(1, 100)]
        public int epicWeight = 8;

        [Tooltip("Legendary 등급 카드의 가중치 (기본: 2)")]
        [Range(1, 100)]
        public int legendaryWeight = 2;

        /// <summary>
        /// 특정 등급의 가중치를 반환합니다
        /// </summary>
        public int GetWeightForRarity(CardData.CardRarity rarity)
        {
            return rarity switch
            {
                CardData.CardRarity.Common => commonWeight,
                CardData.CardRarity.Uncommon => uncommonWeight,
                CardData.CardRarity.Rare => rareWeight,
                CardData.CardRarity.Epic => epicWeight,
                CardData.CardRarity.Legendary => legendaryWeight,
                _ => 1 // 알 수 없는 등급은 최소 가중치
            };
        }

        /// <summary>
        /// 기본 설정 반환
        /// </summary>
        public static RarityWeightSettings Default => new RarityWeightSettings();
    }

    /// <summary>
    /// 등급 기반 가중치 카드 선택 전략
    /// 카드 등급(Rarity)에 따라 출현 확률이 다릅니다
    /// </summary>
    public class RarityWeightedSelectionStrategy : ICardSelectionStrategy
    {
        private readonly RarityWeightSettings weightSettings;

        public string StrategyName => "Rarity Weighted Selection";

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="settings">가중치 설정 (null이면 기본값 사용)</param>
        public RarityWeightedSelectionStrategy(RarityWeightSettings settings = null)
        {
            weightSettings = settings ?? RarityWeightSettings.Default;
        }

        /// <summary>
        /// 등급 기반 가중치를 적용하여 카드를 선택합니다
        /// </summary>
        /// <param name="pool">선택 가능한 카드 목록</param>
        /// <returns>가중치 기반으로 선택된 카드</returns>
        public CardData DrawCard(IReadOnlyList<CardData> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("[RarityWeightedSelectionStrategy] Card pool is empty or null");
                return null;
            }

            // 단일 카드인 경우 즉시 반환
            if (pool.Count == 1)
            {
                Debug.Log($"[RarityWeightedSelectionStrategy] Single card in pool: {pool[0].CardName}");
                return pool[0];
            }

            // 1. 각 카드의 가중치 계산
            var cardWeights = new List<(CardData card, int weight)>();
            int totalWeight = 0;

            foreach (var card in pool)
            {
                if (card == null)
                {
                    Debug.LogWarning("[RarityWeightedSelectionStrategy] Null card found in pool, skipping");
                    continue;
                }

                int weight = weightSettings.GetWeightForRarity(card.Rarity);
                cardWeights.Add((card, weight));
                totalWeight += weight;
            }

            if (totalWeight == 0)
            {
                Debug.LogError("[RarityWeightedSelectionStrategy] Total weight is 0, falling back to first card");
                return pool[0];
            }

            // 2. 가중치 기반 랜덤 선택
            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            foreach (var (card, weight) in cardWeights)
            {
                cumulativeWeight += weight;
                if (randomValue < cumulativeWeight)
                {
                    Debug.Log($"[RarityWeightedSelectionStrategy] Drew card: {card.CardName} " +
                             $"(Rarity: {card.Rarity}, Weight: {weight}/{totalWeight}, Roll: {randomValue})");
                    return card;
                }
            }

            // Fallback (발생하면 안 되지만 안전장치)
            Debug.LogWarning("[RarityWeightedSelectionStrategy] Fallback to last card in pool");
            return cardWeights.Last().card;
        }

        /// <summary>
        /// 현재 가중치 설정을 로그로 출력합니다 (디버깅용)
        /// </summary>
        public void LogWeightSettings()
        {
            Debug.Log($"[RarityWeightedSelectionStrategy] Weight Settings:\n" +
                     $"  Common: {weightSettings.commonWeight}\n" +
                     $"  Uncommon: {weightSettings.uncommonWeight}\n" +
                     $"  Rare: {weightSettings.rareWeight}\n" +
                     $"  Epic: {weightSettings.epicWeight}\n" +
                     $"  Legendary: {weightSettings.legendaryWeight}");
        }
    }
}
