using UnityEngine;
using Game.Data;
using Game.Services;

namespace Game.Interfaces
{
    /// <summary>
    /// Phase 3.14: 리팩토링된 카드 소환 서비스 인터페이스
    /// EffectData 기반 통합 카드 처리 시스템
    /// </summary>
    public interface ICardSpawnService
    {
        /// <summary>소환 서비스가 초기화되었는지 여부</summary>
        bool IsInitialized { get; }

        /// <summary>CardServiceManager에 의해 호출되는 초기화 메서드</summary>
        void Init(IUnitService iUnitService, IGridController iGridController,
                        IGridState iGridState, ISpawnValidator iSpawnValidator,
                        IResourceManager iResourceManager);

        /// <summary>Phase 3.14: 카드를 사용하여 모든 효과를 실행 (기본: 플레이어)</summary>
        /// <param name="cardData">사용할 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <returns>실행 성공 여부</returns>
        bool TryExecuteCard(CardData cardData, Vector2Int targetPosition);

        /// <summary>Phase 3.14: 카드를 사용하여 모든 효과를 실행 (플레이어/적군 구분)</summary>
        /// <param name="cardData">사용할 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="isPlayerCard">플레이어 카드인지 여부</param>
        /// <returns>실행 성공 여부</returns>
        bool TryExecuteCard(CardData cardData, Vector2Int targetPosition, bool isPlayerCard);

        /// <summary>[Obsolete] 카드로부터 유닛을 소환하고 UnitService에 등록 - TryExecuteCard 사용 권장</summary>
        /// <param name="cardData">소환할 카드 데이터</param>
        /// <param name="gridPosition">소환할 그리드 위치</param>
        /// <returns>소환 성공 여부</returns>
        [System.Obsolete("Use TryExecuteCard instead. This method will be removed in future versions.")]
        bool TrySpawnUnitFromCard(CardData cardData, Vector2Int gridPosition);

        /// <summary>[Obsolete] 카드로부터 유닛을 소환하고 UnitService에 등록 (플레이어/적군 구분) - TryExecuteCard 사용 권장</summary>
        /// <param name="cardData">소환할 카드 데이터</param>
        /// <param name="gridPosition">소환할 그리드 위치</param>
        /// <param name="isPlayerUnit">플레이어 유닛인지 여부</param>
        /// <returns>소환 성공 여부</returns>
        [System.Obsolete("Use TryExecuteCard instead. This method will be removed in future versions.")]
        bool TrySpawnUnitFromCard(CardData cardData, Vector2Int gridPosition, bool isPlayerUnit);

        /// <summary>[Obsolete] 주문 카드 발동 - TryExecuteCard 사용 권장</summary>
        /// <param name="cardData">발동할 주문 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <returns>발동 성공 여부</returns>
        [System.Obsolete("Use TryExecuteCard instead. This method will be removed in future versions.")]
        bool TryActivateSpellFromCard(CardData cardData, Vector2Int targetPosition);

        /// <summary>소환 서비스 상태 정보 반환</summary>
        string GetStatus();
    }
}