using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Data;

namespace Game.Data
{
    /// <summary>
    /// 덱 데이터를 저장하는 ScriptableObject
    /// 덱 이름, 카드 목록, 카드 개수를 관리
    /// </summary>
    [CreateAssetMenu(fileName = "New Deck", menuName = "Game/Deck Data")]
    public class DeckData : ScriptableObject
    {
        [SerializeField] private string deckName = "새로운 덱";
        [SerializeField] private List<DeckCardEntry> cards = new List<DeckCardEntry>();

        /// <summary>덱 이름</summary>
        public string DeckName => deckName;

        /// <summary>덱에 포함된 카드 목록 (읽기 전용)</summary>
        public IReadOnlyList<DeckCardEntry> Cards => cards.AsReadOnly();

        /// <summary>덱 이름 설정</summary>
        public void SetDeckName(string name)
        {
            deckName = name;
        }

        /// <summary>
        /// 덱에 카드 추가
        /// </summary>
        /// <param name="card">추가할 카드</param>
        /// <param name="count">추가할 개수 (기본 1장)</param>
        public void AddCard(CardData card, int count = 1)
        {
            if (card == null)
            {
                Debug.LogWarning("[DeckData] Attempted to add null card");
                return;
            }

            var existing = cards.Find(e => e.Card == card);
            if (existing != null)
            {
                existing.Count += count;
            }
            else
            {
                cards.Add(new DeckCardEntry(card, count));
            }
        }

        /// <summary>
        /// 덱에서 카드 제거
        /// </summary>
        /// <param name="card">제거할 카드</param>
        /// <param name="count">제거할 개수 (기본 1장)</param>
        public void RemoveCard(CardData card, int count = 1)
        {
            if (card == null) return;

            var existing = cards.Find(e => e.Card == card);
            if (existing != null)
            {
                existing.Count -= count;
                if (existing.Count <= 0)
                {
                    cards.Remove(existing);
                }
            }
        }

        /// <summary>덱 초기화 (모든 카드 제거)</summary>
        public void Clear()
        {
            cards.Clear();
        }

        /// <summary>덱의 총 카드 개수 반환</summary>
        public int GetTotalCardCount()
        {
            return cards.Sum(e => e.Count);
        }

        /// <summary>특정 카드의 개수 반환</summary>
        public int GetCardCount(CardData card)
        {
            var entry = cards.Find(e => e.Card == card);
            return entry?.Count ?? 0;
        }

        /// <summary>
        /// 덱 데이터를 Dictionary로 변환
        /// </summary>
        public Dictionary<CardData, int> ToDictionary()
        {
            return cards.ToDictionary(e => e.Card, e => e.Count);
        }

        /// <summary>
        /// Dictionary로부터 덱 데이터 설정
        /// </summary>
        public void FromDictionary(Dictionary<CardData, int> deckCards)
        {
            cards.Clear();
            foreach (var kvp in deckCards)
            {
                cards.Add(new DeckCardEntry(kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// 덱 내 카드 엔트리 (카드 + 개수)
    /// </summary>
    [System.Serializable]
    public class DeckCardEntry
    {
        [SerializeField] private CardData card;
        [SerializeField] private int count;

        /// <summary>카드 데이터</summary>
        public CardData Card => card;

        /// <summary>카드 개수</summary>
        public int Count
        {
            get => count;
            set => count = value;
        }

        /// <summary>생성자</summary>
        public DeckCardEntry(CardData card, int count)
        {
            this.card = card;
            this.count = count;
        }
    }
}
