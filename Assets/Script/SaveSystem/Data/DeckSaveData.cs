using System;
using System.Collections.Generic;

namespace Game.SaveSystem
{
    /// <summary>
    /// 덱 저장 데이터
    /// 플레이어가 구성한 덱 정보
    /// </summary>
    [Serializable]
    public class DeckSaveData
    {
        /// <summary>
        /// 덱 이름
        /// </summary>
        public string deckName;

        /// <summary>
        /// 덱에 포함된 카드 목록
        /// </summary>
        public List<EnhancedCardData> cards;

        /// <summary>
        /// 마지막 수정 시간
        /// </summary>
        public DateTime lastModified;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public DeckSaveData()
        {
            deckName = "New Deck";
            cards = new List<EnhancedCardData>();
            lastModified = DateTime.Now;
        }

        /// <summary>
        /// 덱 이름으로 생성하는 생성자
        /// </summary>
        public DeckSaveData(string deckName)
        {
            this.deckName = deckName;
            this.cards = new List<EnhancedCardData>();
            this.lastModified = DateTime.Now;
        }

        /// <summary>
        /// 덱의 총 카드 수 반환
        /// </summary>
        public int GetTotalCardCount()
        {
            int total = 0;
            foreach (var card in cards)
            {
                total += card.quantity;
            }
            return total;
        }
    }
}
