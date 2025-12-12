using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;
using Game.Managers;

/// <summary>
/// 카드팩 개봉 로직
/// CardPackDefinition, CardPoolSet, CardRarityTable을 사용해 실제 카드 목록을 생성하고 컬렉션에 반영
/// </summary>
public static class CardPackOpener
{
    /// <summary>
    /// 카드팩 개봉 및 컬렉션 반영
    /// </summary>
    /// <param name="definition">카드팩 정의</param>
    /// <param name="collection">카드 컬렉션</param>
    /// <param name="defaultRarityTable">팩에 오버라이드 테이블이 없을 때 사용할 기본 테이블 (선택)</param>
    public static CardPackOpenResult Open(
        CardPackDefinition definition,
        ICardCollection collection,
        CardRarityTable defaultRarityTable = null)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        if (definition.DrawCount <= 0)
        {
            Debug.LogError($"[CardPackOpener] DrawCount must be greater than 0 for pack '{definition.PackId}'.");
            throw new InvalidOperationException("Invalid draw count");
        }

        var pool = definition.PoolSet;
        if (pool == null)
        {
            Debug.LogError($"[CardPackOpener] PoolSet is null for pack '{definition.PackId}'.");
            throw new InvalidOperationException("PoolSet is null");
        }

        int drawCount = definition.DrawCount;

        // 사전 검증: 보장 수량 합계 확인
        int minTotal = 0;
        if (definition.RarityGuarantees != null)
        {
            foreach (var g in definition.RarityGuarantees)
            {
                if (g.minCount > 0)
                {
                    minTotal += g.minCount;
                }
            }
        }

        if (minTotal > drawCount)
        {
            Debug.LogError($"[CardPackOpener] Sum of rarity guarantees ({minTotal}) exceeds drawCount ({drawCount}) for pack '{definition.PackId}'.");
            throw new InvalidOperationException("Invalid rarity guarantees");
        }

        var entries = new List<CardPackOpenResult.Entry>();

        // 1. 보장 처리 (레어리티 내림차순으로 정렬)
        var guarantees = definition.RarityGuarantees != null
            ? definition.RarityGuarantees
                .Where(g => g.minCount > 0)
                .OrderByDescending(g => g.rarity)
                .ToList()
            : new List<CardPackDefinition.RarityGuarantee>();

        foreach (var guarantee in guarantees)
        {
            var candidates = pool.GetCandidates(guarantee.rarity);
            if (candidates == null || candidates.Count == 0)
            {
                Debug.LogError($"[CardPackOpener] No candidates for guaranteed rarity {guarantee.rarity} in pool '{pool.PoolId}'.");
                throw new InvalidOperationException("Guarantee rarity has no candidates");
            }

            for (int i = 0; i < guarantee.minCount; i++)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                var card = candidates[index];

                var entry = new CardPackOpenResult.Entry
                {
                    Card = card,
                    Rarity = guarantee.rarity
                };

                entries.Add(entry);
            }
        }

        int remaining = drawCount - entries.Count;

        if (remaining > 0)
        {
            // 2. 잔여 슬롯 처리 - 래어리티 테이블 기반
            var effectiveWeights = BuildEffectiveRarityWeights(definition, pool, defaultRarityTable);

            // 폴백: 유효 가중치가 없으면, 풀에 존재하는 레어리티를 균등 분배
            if (effectiveWeights == null || effectiveWeights.Count == 0)
            {
                effectiveWeights = BuildUniformWeightsFromPool(pool);
            }

            if (effectiveWeights == null || effectiveWeights.Count == 0)
            {
                Debug.LogError($"[CardPackOpener] No valid rarity weights or candidates for pack '{definition.PackId}'.");
                throw new InvalidOperationException("No candidates in pool");
            }

            // 누적 가중치 리스트 (레어리티 오름차순 정렬)
            var cumulative = BuildCumulativeWeights(effectiveWeights);

            for (int i = 0; i < remaining; i++)
            {
                var rarity = DrawRarity(cumulative);
                var candidates = pool.GetCandidates(rarity);

                if (candidates == null || candidates.Count == 0)
                {
                    // 이 경우는 이론상 없어야 하지만, 안전을 위해 풀 전체에서 폴백
                    candidates = GetAllCandidatesFromPool(pool);
                }

                if (candidates == null || candidates.Count == 0)
                {
                    Debug.LogError($"[CardPackOpener] Pool '{pool.PoolId}' has no candidates during remaining draw.");
                    throw new InvalidOperationException("Pool has no candidates");
                }

                int index = UnityEngine.Random.Range(0, candidates.Count);
                var card = candidates[index];

                var entry = new CardPackOpenResult.Entry
                {
                    Card = card,
                    Rarity = card.Rarity
                };

                entries.Add(entry);
            }
        }

        // 3. 컬렉션에 반영 (배치 처리)
        var cardsToAdd = entries.Select(e => e.Card).ToList();
        collection.AddCards(cardsToAdd);

        // 4. 결과 생성
        return new CardPackOpenResult(entries);
    }

    private static Dictionary<CardData.CardRarity, float> BuildEffectiveRarityWeights(
        CardPackDefinition definition,
        CardPoolSet pool,
        CardRarityTable defaultRarityTable)
    {
        IReadOnlyDictionary<CardData.CardRarity, float> baseWeights = null;

        if (definition.RarityTableOverride != null)
        {
            baseWeights = definition.RarityTableOverride.GetNormalizedWeights();
        }
        else if (defaultRarityTable != null)
        {
            baseWeights = defaultRarityTable.GetNormalizedWeights();
        }

        // 테이블이 없거나 유효하지 않으면 null 반환
        if (baseWeights == null || baseWeights.Count == 0)
            return null;

        var effective = new Dictionary<CardData.CardRarity, float>();

        // 후보가 존재하는 레어리티만 필터링
        foreach (var kvp in baseWeights)
        {
            var candidates = pool.GetCandidates(kvp.Key);
            if (candidates != null && candidates.Count > 0 && kvp.Value > 0f)
            {
                effective[kvp.Key] = kvp.Value;
            }
        }

        if (effective.Count == 0)
            return null;

        // 다시 정규화
        float total = effective.Values.Sum();
        if (total <= 0f)
            return null;

        var normalized = new Dictionary<CardData.CardRarity, float>();
        foreach (var kvp in effective)
        {
            normalized[kvp.Key] = kvp.Value / total;
        }

        return normalized;
    }

    private static Dictionary<CardData.CardRarity, float> BuildUniformWeightsFromPool(CardPoolSet pool)
    {
        // 풀에 존재하는 레어리티만 수집
        var raritiesWithCandidates = new List<CardData.CardRarity>();

        foreach (CardData.CardRarity rarity in Enum.GetValues(typeof(CardData.CardRarity)))
        {
            var candidates = pool.GetCandidates(rarity);
            if (candidates != null && candidates.Count > 0)
            {
                raritiesWithCandidates.Add(rarity);
            }
        }

        if (raritiesWithCandidates.Count == 0)
            return null;

        float uniformWeight = 1f / raritiesWithCandidates.Count;
        var result = new Dictionary<CardData.CardRarity, float>();

        foreach (var rarity in raritiesWithCandidates)
        {
            result[rarity] = uniformWeight;
        }

        return result;
    }

    private static List<(CardData.CardRarity rarity, float cumulative)> BuildCumulativeWeights(
        Dictionary<CardData.CardRarity, float> weights)
    {
        var list = new List<(CardData.CardRarity rarity, float cumulative)>();

        float cumulative = 0f;
        foreach (var kvp in weights.OrderBy(k => k.Key))
        {
            cumulative += kvp.Value;
            list.Add((kvp.Key, cumulative));
        }

        // 누적 값이 1보다 약간 작을 수 있으므로, 마지막 항목은 1로 맞춤
        if (list.Count > 0)
        {
            var last = list[list.Count - 1];
            list[list.Count - 1] = (last.rarity, 1f);
        }

        return list;
    }

    private static CardData.CardRarity DrawRarity(List<(CardData.CardRarity rarity, float cumulative)> cumulative)
    {
        if (cumulative == null || cumulative.Count == 0)
            throw new ArgumentException("Cumulative weights list is empty");

        float value = UnityEngine.Random.value;

        foreach (var item in cumulative)
        {
            if (value <= item.cumulative)
                return item.rarity;
        }

        // 폴백: 마지막 레어리티
        return cumulative[cumulative.Count - 1].rarity;
    }

    private static List<CardData> GetAllCandidatesFromPool(CardPoolSet pool)
    {
        if (pool == null)
            return new List<CardData>();

        var result = new List<CardData>();

        foreach (CardData.CardRarity rarity in Enum.GetValues(typeof(CardData.CardRarity)))
        {
            var candidates = pool.GetCandidates(rarity);
            if (candidates != null && candidates.Count > 0)
            {
                result.AddRange(candidates);
            }
        }

        return result;
    }
}
