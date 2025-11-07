using System.Collections.Generic;
using Game.Data;

namespace Game.Managers
{
    /// <summary>
    /// 카드 데이터 레지스트리 인터페이스
    /// Resources.LoadAll의 반복 호출을 방지하기 위한 캐싱 시스템
    /// </summary>
    public interface ICardRegistry
    {
        /// <summary>
        /// 카드 ID로 카드 데이터 조회
        /// </summary>
        CardData GetCardByID(string cardID);

        /// <summary>
        /// 모든 카드 데이터 반환
        /// </summary>
        IEnumerable<CardData> GetAllCards();

        /// <summary>
        /// 카드 존재 여부 확인
        /// </summary>
        bool HasCard(string cardID);

        /// <summary>
        /// 등록된 카드 총 개수
        /// </summary>
        int GetCardCount();
    }
}
