using UnityEngine;
using Game.Data;

namespace Game.Interfaces
{
    /// <summary>
    /// 카드 서비스 매니저 인터페이스
    /// 카드 시스템의 전체적인 상태 관리와 초기화를 담당
    /// </summary>
    public interface ICardServiceManager
    {
        /// <summary>카드 서비스 매니저가 초기화되었는지 여부</summary>
        bool IsInitialized { get; }

        /// <summary>모든 카드 서비스가 정상 상태인지 여부</summary>
        bool AreServicesHealthy { get; }

        /// <summary>
        /// GameInitializer에 의해 호출되는 초기화 메서드
        /// </summary>
        /// <param name="stageData">스테이지 데이터 (스테이지 설정 구성에 사용)</param>
        void InitializeAndRegisterServices(StageDataSO stageData = null);

        /// <summary>카드 서비스 상태 정보 반환 (디버깅용)</summary>
        string GetServiceStatus();

        /// <summary>
        /// 특정 팀의 카드 풀에서 카드를 드로우합니다.
        /// TeamType(Player/Enemy) 값은 유닛 이펙트 등에서
        /// 상대 관계(Ally/Enemy 등)를 해석한 결과로 전달됩니다.
        /// </summary>
        /// <param name="team">카드를 드로우할 팀 (Player/Enemy)</param>
        /// <param name="amount">드로우할 카드 수</param>
        void DrawCardsForTeam(TeamType team, int amount);

        /// <summary>카드 핸드 매니저 반환</summary>
        ICardHandManager GetCardHandManager();

        /// <summary>카드 스폰 서비스 반환</summary>
        ICardSpawnService GetCardSpawnService();

        /// <summary>스폰 검증자 반환</summary>
        ISpawnValidator GetSpawnValidator();

        /// <summary>적군 카드 핸드 뷰 반환</summary>
        IEnemyCardHandView GetEnemyCardHandView();
    }
}
