using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;

/// <summary>
/// 랜덤 카드 선택 전략
/// 사용 가능한 카드 목록에서 무작위로 지정된 수만큼 선택
/// </summary>
public class RandomSelectionStrategy : ICardSelectionStrategy
{
    public List<CardData> SelectCards(List<CardData> availableCards, int count)
    {
        // Null 체크
        if (availableCards == null || availableCards.Count == 0)
        {
            Debug.LogWarning("Available cards list is null or empty");
            return new List<CardData>();
        }

        // 요청 수량이 사용 가능한 카드 수보다 많으면 전체 반환
        int actualCount = Mathf.Min(count, availableCards.Count);

        // LINQ를 사용한 무작위 선택
        // OrderBy(x => Random.value)로 랜덤 정렬 후 Take로 필요한 수만큼 선택
        return availableCards
            .OrderBy(x => Random.value)
            .Take(actualCount)
            .ToList();
    }
}
