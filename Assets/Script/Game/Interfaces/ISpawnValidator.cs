using UnityEngine;
using Game.Data;
using Game.Services;

namespace Game.Interfaces
{
    /// <summary>
    /// 소환 검증자 인터페이스
    /// 유닛 소환 및 주문 사용의 유효성을 검증
    /// </summary>
    public interface ISpawnValidator
    {
        /// <summary>검증자가 초기화되었는지 여부</summary>
        bool IsInitialized { get; }

        /// <summary>CardServiceManager에 의해 호출되는 초기화 메서드</summary>
        void Init(IGridController iGridController, ITurnService iTurnService, IResourceManager iResourceManager);

        /// <summary>유닛 소환이 가능한지 검증</summary>
        /// <param name="cardData">소환할 카드 데이터</param>
        /// <param name="gridPosition">소환 위치</param>
        /// <param name="isPlayerUnit">플레이어 유닛인지 여부</param>
        /// <returns>소환 가능 여부</returns>
        bool CanSpawnUnit(CardData cardData, Vector2Int gridPosition, bool isPlayerUnit = true);

        /// <summary>주문 사용이 가능한지 검증</summary>
        /// <param name="cardData">사용할 주문 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <returns>사용 가능 여부</returns>
        bool CanUseSpell(CardData cardData, Vector2Int targetPosition);

        /// <summary>검증자 상태 정보 반환</summary>
        string GetStatus();
    }
}