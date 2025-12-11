using System.Collections.Generic;
using Game.Data;

/// <summary>
/// 카드팩 개봉 결과 데이터
/// 뽑힌 카드 목록 및 레어리티별 개수 요약 포함
/// </summary>
public class CardPackOpenResult
{
    public struct Entry
    {
        public CardData Card;
        public CardData.CardRarity Rarity;
    }

    private readonly List<Entry> _entries;
    private readonly Dictionary<CardData.CardRarity, int> _rarityCounts;

    public IReadOnlyList<Entry> Entries => _entries;
    public IReadOnlyDictionary<CardData.CardRarity, int> RarityCounts => _rarityCounts;
    public int TotalCount => _entries.Count;

    public CardPackOpenResult(List<Entry> entries)
    {
        _entries = entries ?? new List<Entry>();
        _rarityCounts = new Dictionary<CardData.CardRarity, int>();

        foreach (var entry in _entries)
        {
            if (_rarityCounts.TryGetValue(entry.Rarity, out var count))
            {
                _rarityCounts[entry.Rarity] = count + 1;
            }
            else
            {
                _rarityCounts[entry.Rarity] = 1;
            }
        }
    }
}
