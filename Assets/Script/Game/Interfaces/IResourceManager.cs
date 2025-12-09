using System;
using Game.Services;

namespace Game.Interfaces
{
    /// <summary>
    /// 자원 관리 서비스 인터페이스
    /// 플레이어와 적군의 Mana, ActionPoint 관리를 담당
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>자원 매니저가 초기화되었는지 여부</summary>
        bool IsInitialized { get; }

        /// <summary>GameInitializer에 의해 호출되는 초기화 메서드</summary>
        void Initialize();

        #region 플레이어 자원 조회

        /// <summary>플레이어 현재 마나</summary>
        int PlayerMana { get; }

        /// <summary>플레이어 최대 마나</summary>
        int PlayerMaxMana { get; }

        #endregion

        #region 적군 자원 조회

        /// <summary>적군 현재 마나</summary>
        int EnemyMana { get; }

        /// <summary>적군 최대 마나</summary>
        int EnemyMaxMana { get; }

        #endregion

        #region 자원 검증

        /// <summary>플레이어가 지정된 비용을 지불할 수 있는지 확인</summary>
        /// <param name="manaCost">필요한 마나</param>
        /// <param name="actionCost">필요한 행동력</param>
        /// <returns>지불 가능 여부</returns>
        bool CanPlayerAfford(int manaCost);

        /// <summary>적군이 지정된 비용을 지불할 수 있는지 확인</summary>
        /// <param name="manaCost">필요한 마나</param>
        /// <param name="actionCost">필요한 행동력</param>
        /// <returns>지불 가능 여부</returns>
        bool CanEnemyAfford(int manaCost);

        /// <summary>팀에 따라 자원 지불 가능 여부 확인</summary>
        /// <param name="isPlayerTeam">플레이어 팀인지 여부</param>
        /// <param name="manaCost">필요한 마나</param>
        /// <param name="actionCost">필요한 행동력</param>
        /// <returns>지불 가능 여부</returns>
        bool CanAfford(bool isPlayerTeam, int manaCost);

        #endregion

        #region 자원 소모

        /// <summary>플레이어 자원 소모</summary>
        /// <param name="manaCost">소모할 마나</param>
        /// <param name="actionCost">소모할 행동력</param>
        /// <returns>소모 성공 여부</returns>
        bool SpendPlayerResources(int manaCost);

        /// <summary>적군 자원 소모</summary>
        /// <param name="manaCost">소모할 마나</param>
        /// <param name="actionCost">소모할 행동력</param>
        /// <returns>소모 성공 여부</returns>
        bool SpendEnemyResources(int manaCost);

        /// <summary>팀에 따라 자원 소모</summary>
        /// <param name="isPlayerTeam">플레이어 팀인지 여부</param>
        /// <param name="manaCost">소모할 마나</param>
        /// <param name="actionCost">소모할 행동력</param>
        /// <returns>소모 성공 여부</returns>
        bool SpendResources(bool isPlayerTeam, int manaCost);

        #endregion

        #region 자원 회복

        /// <summary>플레이어 자원 회복</summary>
        /// <param name="manaAmount">회복할 마나</param>
        /// <param name="actionAmount">회복할 행동력</param>
        void RestorePlayerResources(int manaAmount);

        /// <summary>적군 자원 회복</summary>
        /// <param name="manaAmount">회복할 마나</param>
        /// <param name="actionAmount">회복할 행동력</param>
        void RestoreEnemyResources(int manaAmount);

        /// <summary>플레이어 자원을 최대치로 설정</summary>
        void RefillPlayerResources();

        /// <summary>적군 자원을 최대치로 설정</summary>
        void RefillEnemyResources();

        /// <summary>자원 상태 초기화 (테스트용)</summary>
        void ResetResources();

        #endregion

        #region 이벤트

        /// <summary>플레이어 자원 변경 이벤트 (마나, 행동력)</summary>
        event System.Action<ManaData> OnPlayerResourcesChanged;

        /// <summary>적군 자원 변경 이벤트 (마나, 행동력)</summary>
        event System.Action<ManaData> OnEnemyResourcesChanged;

        /// <summary>자원 부족 이벤트 (플레이어 여부, 필요한 마나, 필요한 행동력)</summary>
        event System.Action<bool, int> OnInsufficientResources;

        #endregion

        #region 턴 시스템 연동
        
        /// <summary>매 턴 사용가능 마나를 1증가시킴</summary>
        void IncreaseTurnlyMana();

        #endregion

        #region 유틸리티

        /// <summary>자원 매니저 상태 정보 반환 (디버깅용)</summary>
        string GetStatus();

        #endregion
    }
}
