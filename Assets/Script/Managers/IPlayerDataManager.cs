using Game.SaveSystem;

namespace Game.Managers
{
    /// <summary>
    /// 플레이어 데이터 관리 인터페이스
    ///
    /// 책임:
    /// - 플레이어 정보 관리 (ID, 이름, 골드 등)
    /// - 런타임 플레이어 데이터 메모리 관리
    /// - SaveDataAdapter와의 데이터 교환
    /// </summary>
    public interface IPlayerDataManager
    {
        #region Initialization
        /// <summary>
        /// 초기화 - 기본 플레이어 데이터 생성
        /// </summary>
        void Initialize();
        #endregion

        #region Data Access
        /// <summary>
        /// 현재 플레이어 데이터 전체 반환 (SaveDataAdapter에서 사용)
        /// </summary>
        /// <returns>현재 플레이어 데이터</returns>
        PlayerData GetCurrentPlayerData();

        /// <summary>
        /// 플레이어 데이터 전체 설정 (SaveDataAdapter에서 로드 시 사용)
        /// </summary>
        /// <param name="data">설정할 플레이어 데이터</param>
        void SetPlayerData(PlayerData data);
        #endregion

        #region Individual Property Accessors
        /// <summary>
        /// 플레이어 ID 조회
        /// </summary>
        /// <returns>플레이어 고유 ID</returns>
        string GetPlayerID();

        /// <summary>
        /// 플레이어 이름 조회
        /// </summary>
        /// <returns>플레이어 이름</returns>
        string GetPlayerName();

        /// <summary>
        /// 플레이어 이름 설정
        /// </summary>
        /// <param name="name">새로운 플레이어 이름</param>
        void SetPlayerName(string name);

        /// <summary>
        /// 골드 조회
        /// </summary>
        /// <returns>현재 골드량</returns>
        int GetGold();

        /// <summary>
        /// 골드 추가
        /// </summary>
        /// <param name="amount">추가할 골드량</param>
        void AddGold(int amount);

        /// <summary>
        /// 골드 설정 (직접 설정)
        /// </summary>
        /// <param name="amount">설정할 골드량</param>
        void SetGold(int amount);

        /// <summary>
        /// 총 플레이 시간 조회
        /// </summary>
        /// <returns>총 플레이 시간 (초)</returns>
        float GetTotalPlayTime();

        /// <summary>
        /// 플레이 시간 추가
        /// </summary>
        /// <param name="time">추가할 플레이 시간 (초)</param>
        void AddPlayTime(float time);
        #endregion
    }
}
