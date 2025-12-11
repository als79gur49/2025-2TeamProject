using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 카드팩에서 사용할 후보 카드 풀 정의
    /// 여러 카드팩이 공유할 수 있는 카드 집합
    /// </summary>
    [CreateAssetMenu(fileName = "CardPoolSet_", menuName = "Shop/Card Pool Set")]
    public class CardPoolSet : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private string poolId = "";

        [Header("Candidates")]
        [SerializeField] private List<CardData> candidates = new List<CardData>();

        // 런타임 캐시: 등급별 후보 카드 목록
        private Dictionary<CardData.CardRarity, List<CardData>> _candidatesByRarity;

        public string PoolId => string.IsNullOrEmpty(poolId) ? name : poolId;

        public IReadOnlyList<CardData> Candidates => candidates;

        /// <summary>
        /// 지정된 레어리티의 후보 카드 목록 반환
        /// </summary>
        public IReadOnlyList<CardData> GetCandidates(CardData.CardRarity rarity)
        {
            EnsureCache();

            if (_candidatesByRarity != null &&
                _candidatesByRarity.TryGetValue(rarity, out var list) &&
                list != null)
            {
                return list;
            }

            return Array.Empty<CardData>();
        }

        private void EnsureCache()
        {
            if (_candidatesByRarity != null)
                return;

            _candidatesByRarity = new Dictionary<CardData.CardRarity, List<CardData>>();

            if (candidates == null)
                return;

            foreach (var card in candidates)
            {
                if (card == null)
                    continue;

                var rarity = card.Rarity;

                if (!_candidatesByRarity.TryGetValue(rarity, out var list))
                {
                    list = new List<CardData>();
                    _candidatesByRarity[rarity] = list;
                }

                list.Add(card);
            }
        }

        private void OnValidate()
        {
            // 데이터 변경 시 캐시 초기화
            _candidatesByRarity = null;
        }
    }
}

