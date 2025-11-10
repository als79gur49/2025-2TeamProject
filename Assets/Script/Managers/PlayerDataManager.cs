using UnityEngine;
using Game.SaveSystem;

namespace Game.Managers
{
    /// <summary>
    /// PlayerData의 런타임 메모리 관리자
    /// SaveDataAdapter는 이 매니저에서 데이터를 수집/적용
    /// </summary>
    public class PlayerDataManager : MonoBehaviour, IPlayerDataManager
    {
        //반드시 네임스페이스 지정할 것. PlayerData.PlayerData와 중복. PlayerData.PlayerData가 우선순위가 더 높은 것 같다.
        #region Current State
        private Game.SaveSystem.PlayerData currentPlayerData;
        #endregion

        #region Initialization

        /// <summary>
        /// 초기화 - 기본 플레이어 데이터 생성
        /// </summary>
        public void Initialize()
        {
            currentPlayerData = new Game.SaveSystem.PlayerData
            {
                playerID = GeneratePlayerID(),
                playerName = "Player",
                gold = 0,
                totalPlayTime = 0f
            };

            Debug.Log($"[PlayerDataManager] Initialized with PlayerID: {currentPlayerData.playerID}");
        }

        #endregion

        #region Data Access

        /// <summary>
        /// 현재 플레이어 데이터 전체 반환 (SaveDataAdapter에서 사용)
        /// </summary>
        public Game.SaveSystem.PlayerData GetCurrentPlayerData()
        {
            return currentPlayerData;
        }

        /// <summary>
        /// 플레이어 데이터 전체 설정 (SaveDataAdapter에서 로드 시 사용)
        /// </summary>
        public void SetPlayerData(Game.SaveSystem.PlayerData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerDataManager] SetPlayerData called with null data");
                return;
            }

            currentPlayerData = data;
            Debug.Log($"[PlayerDataManager] Player data set - ID: {data.playerID}, Gold: {data.gold}");
        }

        #endregion

        #region Individual Property Accessors

        /// <summary>
        /// 플레이어 ID 조회
        /// </summary>
        public string GetPlayerID()
        {
            return currentPlayerData?.playerID ?? string.Empty;
        }

        /// <summary>
        /// 플레이어 이름 조회
        /// </summary>
        public string GetPlayerName()
        {
            return currentPlayerData?.playerName ?? "Player";
        }

        /// <summary>
        /// 플레이어 이름 설정
        /// </summary>
        public void SetPlayerName(string name)
        {
            if (currentPlayerData == null)
            {
                Debug.LogWarning("[PlayerDataManager] Cannot set player name - data not initialized");
                return;
            }

            currentPlayerData.playerName = name;
            Debug.Log($"[PlayerDataManager] Player name changed to: {name}");
        }

        /// <summary>
        /// 골드 조회
        /// </summary>
        public int GetGold()
        {
            return currentPlayerData?.gold ?? 0;
        }

        /// <summary>
        /// 골드 추가
        /// </summary>
        public void AddGold(int amount)
        {
            if (currentPlayerData == null)
            {
                Debug.LogWarning("[PlayerDataManager] Cannot add gold - data not initialized");
                return;
            }

            currentPlayerData.gold += amount;
            Debug.Log($"[PlayerDataManager] Gold changed: +{amount} → Total: {currentPlayerData.gold}");
        }

        /// <summary>
        /// 골드 설정 (직접 설정)
        /// </summary>
        public void SetGold(int amount)
        {
            if (currentPlayerData == null)
            {
                Debug.LogWarning("[PlayerDataManager] Cannot set gold - data not initialized");
                return;
            }

            currentPlayerData.gold = amount;
            Debug.Log($"[PlayerDataManager] Gold set to: {amount}");
        }

        /// <summary>
        /// 총 플레이 시간 조회
        /// </summary>
        public float GetTotalPlayTime()
        {
            return currentPlayerData?.totalPlayTime ?? 0f;
        }

        /// <summary>
        /// 플레이 시간 추가
        /// </summary>
        public void AddPlayTime(float time)
        {
            if (currentPlayerData == null)
            {
                Debug.LogWarning("[PlayerDataManager] Cannot add play time - data not initialized");
                return;
            }

            currentPlayerData.totalPlayTime += time;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 새로운 플레이어 ID 생성
        /// </summary>
        private string GeneratePlayerID()
        {
            return "PLR" + UnityEngine.Random.Range(100000, 999999).ToString();
        }

        #endregion
    }
}
