using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 카드 레어리티별 등장 가중치 테이블
    /// 카드팩 드로우 시 등급 선택에 사용
    /// </summary>
    [CreateAssetMenu(fileName = "CardRarityTable_", menuName = "Shop/Card Rarity Table")]
    public class CardRarityTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public CardData.CardRarity rarity;
            public float weight;
        }

        [Header("Identification")]
        [SerializeField] private string tableId = "default";

        [Header("Weights")]
        [SerializeField] private List<Entry> entries = new List<Entry>();

        // 정규화된 가중치 캐시
        private Dictionary<CardData.CardRarity, float> _normalizedWeights;

        public string TableId => string.IsNullOrEmpty(tableId) ? name : tableId;

        /// <summary>
        /// 레어리티별 정규화된 가중치 딕셔너리 반환
        /// (필요 시 런타임에서 한 번 계산 후 캐시)
        /// </summary>
        public IReadOnlyDictionary<CardData.CardRarity, float> GetNormalizedWeights()
        {
            if (_normalizedWeights != null)
                return _normalizedWeights;

            _normalizedWeights = new Dictionary<CardData.CardRarity, float>();

            if (entries == null || entries.Count == 0)
                return _normalizedWeights;

            // rarity별로 weight 집계 (중복 rarity 허용, 합산)
            var aggregated = new Dictionary<CardData.CardRarity, float>();
            float total = 0f;

            foreach (var entry in entries)
            {
                if (entry.weight <= 0f)
                    continue;

                if (aggregated.TryGetValue(entry.rarity, out var existing))
                {
                    aggregated[entry.rarity] = existing + entry.weight;
                }
                else
                {
                    aggregated[entry.rarity] = entry.weight;
                }
            }

            foreach (var kvp in aggregated)
            {
                total += kvp.Value;
            }

            if (total <= 0f)
                return _normalizedWeights;

            foreach (var kvp in aggregated)
            {
                _normalizedWeights[kvp.Key] = kvp.Value / total;
            }

            return _normalizedWeights;
        }

        private void OnValidate()
        {
            // 에디터에서 값 변경 시 캐시 초기화
            _normalizedWeights = null;
        }
    }
}

