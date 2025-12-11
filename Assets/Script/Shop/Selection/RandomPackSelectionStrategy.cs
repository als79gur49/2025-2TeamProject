using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;

/// <summary>
/// 랜덤 카드팩 선택 전략
/// 사용 가능한 팩 목록에서 무작위로 지정된 수만큼 선택
/// </summary>
public class RandomPackSelectionStrategy : IPackSelectionStrategy
{
    public List<CardPackDefinition> SelectPacks(List<CardPackDefinition> availablePacks, int count)
    {
        if (availablePacks == null || availablePacks.Count == 0)
        {
            Debug.LogWarning("[RandomPackSelectionStrategy] Available packs list is null or empty");
            return new List<CardPackDefinition>();
        }

        int actualCount = Mathf.Min(count, availablePacks.Count);

        return availablePacks
            .Where(p => p != null)
            .OrderBy(_ => Random.value)
            .Take(actualCount)
            .ToList();
    }
}

