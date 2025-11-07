using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;

namespace Game.Managers
{
    /// <summary>
    /// 카드 데이터베이스
    /// Resources 폴더의 모든 카드를 초기화 시 한 번만 로드하여 캐싱
    /// O(n) 조회를 O(1)로 개선
    /// </summary>
    public class CardDatabase : MonoBehaviour, ICardRegistry
    {
        private Dictionary<string, CardData> cardCache;
        private bool isInitialized = false;

        #region Initialization

        /// <summary>
        /// 카드 데이터베이스 초기화
        /// Resources 폴더에서 모든 카드를 로드하여 Dictionary에 캐싱
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[CardDatabase] Already initialized");
                return;
            }

            var cards = Resources.LoadAll<CardData>("Cards");

            if (cards == null || cards.Length == 0)
            {
                Debug.LogError("[CardDatabase] No cards found in Resources/Cards folder!");
                cardCache = new Dictionary<string, CardData>();
                return;
            }

            cardCache = new Dictionary<string, CardData>();

            foreach (var card in cards)
            {
                if (card == null)
                {
                    Debug.LogWarning("[CardDatabase] Null card found in Resources");
                    continue;
                }

                if (string.IsNullOrEmpty(card.CardID))
                {
                    Debug.LogWarning($"[CardDatabase] Card with empty ID found: {card.CardName}");
                    continue;
                }

                if (cardCache.ContainsKey(card.CardID))
                {
                    Debug.LogWarning($"[CardDatabase] Duplicate card ID found: {card.CardID}");
                    continue;
                }

                cardCache[card.CardID] = card;
            }

            isInitialized = true;
            Debug.Log($"[CardDatabase] Initialized with {cardCache.Count} cards");
        }

        #endregion

        #region ICardRegistry Implementation

        /// <summary>
        /// 카드 ID로 카드 데이터 조회 (O(1))
        /// </summary>
        public CardData GetCardByID(string cardID)
        {
            if (!isInitialized)
            {
                Debug.LogError("[CardDatabase] Not initialized! Call Initialize() first.");
                return null;
            }

            if (string.IsNullOrEmpty(cardID))
            {
                Debug.LogWarning("[CardDatabase] GetCardByID called with null or empty ID");
                return null;
            }

            if (cardCache.TryGetValue(cardID, out var card))
            {
                return card;
            }

            Debug.LogWarning($"[CardDatabase] Card not found: {cardID}");
            return null;
        }

        /// <summary>
        /// 모든 카드 데이터 반환
        /// </summary>
        public IEnumerable<CardData> GetAllCards()
        {
            if (!isInitialized)
            {
                Debug.LogError("[CardDatabase] Not initialized! Call Initialize() first.");
                return System.Linq.Enumerable.Empty<CardData>();
            }

            return cardCache.Values;
        }

        /// <summary>
        /// 카드 존재 여부 확인
        /// </summary>
        public bool HasCard(string cardID)
        {
            if (!isInitialized)
            {
                Debug.LogError("[CardDatabase] Not initialized! Call Initialize() first.");
                return false;
            }

            return !string.IsNullOrEmpty(cardID) && cardCache.ContainsKey(cardID);
        }

        /// <summary>
        /// 등록된 카드 총 개수
        /// </summary>
        public int GetCardCount()
        {
            if (!isInitialized)
            {
                Debug.LogError("[CardDatabase] Not initialized! Call Initialize() first.");
                return 0;
            }

            return cardCache.Count;
        }

        #endregion
    }
}
