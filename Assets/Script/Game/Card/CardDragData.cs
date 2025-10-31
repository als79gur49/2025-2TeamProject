using UnityEngine;
using Game.Data;

namespace Game.Card
{
    /// <summary>
    /// CardUI가 사용되는 컨텍스트를 구분하는 enum
    /// 이벤트 구독자(Manager/Service)가 자신의 도메인 이벤트만 필터링하는 데 사용
    /// </summary>
    public enum CardUIMode
    {
        InHand,      // 전투 중: Hand → Field 타일 드래그
        InInventory, // 인벤토리: 인벤토리 → 덱빌더 드래그
        InDeck       // 덱빌더: 덱 내 카드 재정렬
    }

    /// <summary>
    /// 카드 드래그 이벤트에 전달되는 데이터
    /// View(CardUI)와 비즈니스 로직(Managers/Services) 간의 데이터 전달에 사용
    /// </summary>
    public struct CardDragData
    {
        /// <summary>드래그 중인 카드 데이터</summary>
        public CardData cardData;

        /// <summary>드래그 컨텍스트 (InHand, InInventory, InDeck)</summary>
        public CardUIMode mode;

        /// <summary>드래그 시작 위치 Transform</summary>
        public Transform sourceTransform;

        /// <summary>원래 인덱스 (Hand 또는 Deck 내)</summary>
        public int sourceIndex;

        /// <summary>생성자</summary>
        public CardDragData(CardData cardData, CardUIMode mode, Transform sourceTransform, int sourceIndex)
        {
            this.cardData = cardData;
            this.mode = mode;
            this.sourceTransform = sourceTransform;
            this.sourceIndex = sourceIndex;
        }
    }
}
