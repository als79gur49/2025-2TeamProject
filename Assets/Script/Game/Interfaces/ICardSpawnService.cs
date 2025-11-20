using UnityEngine;
using Game.Data;
using Game.Services;
using static Game.Interfaces.ITeamComponent;

namespace Game.Interfaces
{
    /// <summary>
    /// 리팩토링된 카드 소환 서비스 인터페이스
    /// EffectDefinition 기반 통합 카드 처리 시스템
    /// </summary>
    public interface ICardSpawnService
    {
        /// <summary>소환 서비스가 초기화되었는지 여부</summary>
        bool IsInitialized { get; }

        /// <summary>CardServiceManager에 의해 호출되는 초기화 메서드</summary>
        void Init(IUnitService iUnitService, IGridController iGridController,
                        IGridState iGridState, ISpawnValidator iSpawnValidator,
                        IResourceManager iResourceManager);

        /// <summary>카드를 사용하여 모든 효과를 실행 (기본: 플레이어)</summary>
        /// <param name="cardData">사용할 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <returns>실행 성공 여부</returns>
        bool TryExecuteCard(CardData cardData, Vector2Int targetPosition);

        /// <summary>카드를 사용하여 모든 효과를 실행 (팀 지정)</summary>
        /// <param name="cardData">사용할 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="casterTeam">카드를 사용한 팀</param>
        /// <returns>실행 성공 여부</returns>
        bool TryExecuteCard(CardData cardData, Vector2Int targetPosition, TeamType casterTeam);

        /// <summary>소환 서비스 상태 정보 반환</summary>
        string GetStatus();
    }
}
