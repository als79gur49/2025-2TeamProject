using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Game.Data;

namespace Game.Managers
{
    /// <summary>
    /// 플레이어의 카드 컬렉션을 관리하는 싱글톤 매니저
    /// 소유한 카드 추가/제거, 저장/로드 기능 제공
    /// </summary>
    public class CollectionManager : MonoBehaviour
    {
        public static CollectionManager Instance { get; private set; }

        [Header("Available Cards")]
        [SerializeField] private List<CardData> allAvailableCards = new List<CardData>();

        // 플레이어가 소유한 카드 (카드 → 소유 개수)
        private Dictionary<CardData, int> ownedCards = new Dictionary<CardData, int>();

        // 이벤트
        public event Action OnCollectionChanged;

        #region Lifecycle

        private void Awake()
        {
            // 싱글톤 패턴
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            // 테스트를 위한 일부로 추가한 코드, 
            SaveAvailableCards();

            LoadCollection();
        }

        #endregion

        #region Card Management

        /// <summary>
        /// 컬렉션에 카드 추가
        /// </summary>
        public void AddCardToCollection(CardData card, int count = 1)
        {
            if (card == null)
            {
                Debug.LogWarning("[CollectionManager] Attempted to add null card");
                return;
            }

            if (ownedCards.ContainsKey(card))
            {
                ownedCards[card] += count;
            }
            else
            {
                ownedCards[card] = count;
            }

            OnCollectionChanged?.Invoke();
            SaveCollection();

            Debug.Log($"[CollectionManager] Added {count}x {card.CardName} to collection");
        }

        /// <summary>
        /// 컬렉션에서 카드 제거
        /// </summary>
        public void RemoveCardFromCollection(CardData card, int count = 1)
        {
            if (card == null || !ownedCards.ContainsKey(card))
                return;

            ownedCards[card] -= count;

            if (ownedCards[card] <= 0)
            {
                ownedCards.Remove(card);
            }

            OnCollectionChanged?.Invoke();
            SaveCollection();

            Debug.Log($"[CollectionManager] Removed {count}x {card.CardName} from collection");
        }

        /// <summary>
        /// 특정 카드의 소유 개수 반환
        /// </summary>
        public int GetOwnedCount(CardData card)
        {
            return ownedCards.ContainsKey(card) ? ownedCards[card] : 0;
        }

        /// <summary>
        /// 소유한 모든 카드 목록 반환
        /// </summary>
        public List<CardData> GetAllOwnedCards()
        {
            return ownedCards.Keys.ToList();
        }

        /// <summary>
        /// 특정 카드를 소유하고 있는지 확인
        /// </summary>
        public bool HasCard(CardData card)
        {
            return ownedCards.ContainsKey(card) && ownedCards[card] > 0;
        }

        /// <summary>
        /// 총 카드 개수 반환 (모든 카드의 합)
        /// </summary>
        public int GetTotalCardCount()
        {
            return ownedCards.Values.Sum();
        }

        /// <summary>
        /// 고유 카드 종류 개수 반환
        /// </summary>
        public int GetUniqueCardCount()
        {
            return ownedCards.Count;
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// 컬렉션 저장 (PlayerPrefs JSON)
        /// </summary>
        public void SaveCollection()
        {
            var saveData = new CollectionSaveData
            {
                cardIds = ownedCards.Keys.Select(c => c.name).ToList(),
                counts = ownedCards.Values.ToList()
            };

            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString("PlayerCollection", json);
            PlayerPrefs.Save();

            Debug.Log($"[CollectionManager] Collection saved: {GetUniqueCardCount()} unique cards, {GetTotalCardCount()} total cards");
        }

        /// <summary>
        /// 컬렉션 로드 (PlayerPrefs JSON)
        /// </summary>
        public void LoadCollection()
        {
            string json = PlayerPrefs.GetString("PlayerCollection", "");

            if (string.IsNullOrEmpty(json))
            {
                // 처음 실행 시 기본 카드 지급
                GiveStarterCards();
                return;
            }

            var saveData = JsonUtility.FromJson<CollectionSaveData>(json);

            ownedCards.Clear();

            for (int i = 0; i < saveData.cardIds.Count; i++)
            {
                var card = allAvailableCards.Find(c => c.name == saveData.cardIds[i]);
                if (card != null)
                {
                    ownedCards[card] = saveData.counts[i];
                }
                else
                {
                    Debug.LogWarning($"[CollectionManager] Card not found: {saveData.cardIds[i]}");
                }
            }

            OnCollectionChanged?.Invoke();

            Debug.Log($"[CollectionManager] Collection loaded: {GetUniqueCardCount()} unique cards, {GetTotalCardCount()} total cards");
        }

        /// <summary>
        /// 스타터 카드 지급 (처음 실행 시)
        /// </summary>
        private void GiveStarterCards()
        {
            Debug.Log("[CollectionManager] First run detected - giving starter cards");

            // 사용 가능한 카드 중 처음 10종류를 각 3장씩 지급
            foreach (var card in allAvailableCards.Take(10))
            {
                AddCardToCollection(card, 3);
            }

            Debug.Log("[CollectionManager] Starter cards given");
        }

        #endregion

        #region Test Functions

        /// <summary>
        /// [테스트 전용] allAvailableCards 리스트를 저장
        /// 개발/디버깅 목적으로만 사용
        /// </summary>
        public void SaveAvailableCards()
        {
            var saveData = new CollectionSaveData
            {
                cardIds = allAvailableCards.Select(c => c.name).ToList(),
                counts = allAvailableCards.Select(c => 3).ToList() // 각 카드 3장씩 기본값
            };

            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString("PlayerCollection", json);
            PlayerPrefs.Save();

            Debug.Log($"[CollectionManager] Available cards saved: {allAvailableCards.Count} cards (3 copies each)");
        }

        #endregion
    }

    /// <summary>
    /// 컬렉션 저장 데이터 구조
    /// </summary>
    [System.Serializable]
    public class CollectionSaveData
    {
        public List<string> cardIds;
        public List<int> counts;
    }

    /// <summary>
    /// [테스트 전용] 사용 가능한 카드 목록 저장 데이터 구조
    /// </summary>
    [System.Serializable]
    public class AvailableCardsSaveData
    {
        public List<string> cardIds;
    }
}
