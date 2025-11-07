using System;

namespace Game.SaveSystem
{
    /// <summary>
    /// 플레이어 기본 데이터
    /// 플레이어 식별 정보, 재화, 플레이 통계 등
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        /// <summary>
        /// 플레이어 고유 ID
        /// </summary>
        public string playerID;

        /// <summary>
        /// 플레이어 이름
        /// </summary>
        public string playerName;

        /// <summary>
        /// 보유 골드
        /// </summary>
        public int gold;

        /// <summary>
        /// 총 플레이 시간 (초 단위)
        /// </summary>
        public float totalPlayTime;

        /// <summary>
        /// 마지막으로 사용한 덱 이름 (자동 로드용)
        /// </summary>
        public string lastUsedDeckName;

        /// <summary>
        /// 마지막 수정 시간
        /// </summary>
        public DateTime lastModified;

        /// <summary>
        /// 기본 생성자 - 초기값 설정
        /// </summary>
        public PlayerData()
        {
            playerID = "";
            playerName = "Player";
            gold = 0;
            totalPlayTime = 0f;
            lastUsedDeckName = "";
            lastModified = DateTime.Now;
        }
    }
}
