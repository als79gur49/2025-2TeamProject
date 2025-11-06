using System;
using System.Collections.Generic;

namespace Game.SaveSystem
{
    /// <summary>
    /// 카드 컬렉션 데이터
    /// 플레이어가 보유한 모든 카드 정보
    /// </summary>
    [Serializable]
    public class CardCollectionData
    {
        /// <summary>
        /// 보유 카드 목록 (강화 정보 포함)
        /// </summary>
        public List<EnhancedCardData> ownedCards;

        /// <summary>
        /// 총 수집한 카드 종류 수
        /// </summary>
        public int totalCardsCollected;

        /// <summary>
        /// 마지막 수정 시간
        /// </summary>
        public DateTime lastModified;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public CardCollectionData()
        {
            ownedCards = new List<EnhancedCardData>();
            totalCardsCollected = 0;
            lastModified = DateTime.Now;
        }

        /// <summary>
        /// 특정 카드가 컬렉션에 있는지 확인
        /// </summary>
        public bool HasCard(string cardID)
        {
            return ownedCards.Exists(c => c.cardID == cardID);
        }

        /// <summary>
        /// 특정 카드의 보유 수량 반환
        /// </summary>
        public int GetCardCount(string cardID)
        {
            var card = ownedCards.Find(c => c.cardID == cardID);
            return card != null ? card.quantity : 0;
        }
    }
}
