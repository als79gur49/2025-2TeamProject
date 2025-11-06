using System;

namespace Game.SaveSystem
{
    /// <summary>
    /// 강화 가능한 카드 데이터
    /// 향후 카드 레벨링/강화 시스템을 위한 확장 데이터 구조
    /// </summary>
    [Serializable]
    public class EnhancedCardData
    {
        /// <summary>
        /// 카드 고유 ID (CardData의 CardID와 매칭)
        /// </summary>
        public string cardID;

        /// <summary>
        /// 카드 이름 (표시용, 캐싱)
        /// </summary>
        public string cardName;

        /// <summary>
        /// 보유 수량
        /// </summary>
        public int quantity;

        /// <summary>
        /// 카드 레벨 (1부터 시작)
        /// </summary>
        public int level;

        /// <summary>
        /// 강화 단계 (0부터 시작)
        /// </summary>
        public int enhancementLevel;

        /// <summary>
        /// 누적 경험치
        /// </summary>
        public int experience;

        /// <summary>
        /// 잠금 상태 (덱 편집 방지 등)
        /// </summary>
        public bool isLocked;

        /// <summary>
        /// 획득 일시
        /// </summary>
        public DateTime obtainedDate;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public EnhancedCardData()
        {
            cardID = "";
            cardName = "";
            quantity = 1;
            level = 1;
            enhancementLevel = 0;
            experience = 0;
            isLocked = false;
            obtainedDate = DateTime.Now;
        }

        /// <summary>
        /// 카드 ID로 생성하는 생성자
        /// </summary>
        public EnhancedCardData(string cardID, string cardName, int quantity = 1)
        {
            this.cardID = cardID;
            this.cardName = cardName;
            this.quantity = quantity;
            this.level = 1;
            this.enhancementLevel = 0;
            this.experience = 0;
            this.isLocked = false;
            this.obtainedDate = DateTime.Now;
        }
    }
}
