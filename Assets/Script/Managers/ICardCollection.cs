using System.Collections.Generic;
using Game.Data;
using System;

namespace Game.Managers
{
    /// <summary>
    /// 카드 컬렉션 인터페이스
    /// CollectionManager의 핵심 기능을 추상화하여 의존성 역전
    /// </summary>
    public interface ICardCollection
    {
        /// <summary>
        /// 소유한 모든 카드 반환
        /// </summary>
        List<CardData> GetAllOwnedCards();

        /// <summary>
        /// 특정 카드의 소유 개수 반환
        /// </summary>
        int GetOwnedCount(CardData card);

        /// <summary>
        /// 카드 추가 (상점 구매, 보상 획득 등)
        /// </summary>
        /// <param name="card">추가할 카드</param>
        /// <param name="quantity">추가할 수량</param>
        void AddCard(CardData card, int quantity = 1);

        /// <summary>
        /// 컬렉션 로드 (저장 데이터 반영)
        /// </summary>
        void LoadCollection();

        /// <summary>
        /// 총 카드 개수 반환 (중복 포함)
        /// </summary>
        int GetTotalCardCount();

        /// <summary>
        /// 고유 카드 종류 개수 반환
        /// </summary>
        int GetUniqueCardCount();

        event Action OnCollectionChanged;

        void SetCollectionFromLoadedData(List<CardData> cards, List<int> counts);
    }
}
