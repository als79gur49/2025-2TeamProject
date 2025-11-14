namespace Game.AI.CardSelection
{
    /// <summary>
    /// 카드 선택 방식을 정의하는 열거형
    /// </summary>
    public enum SelectionType
    {
        /// <summary>
        /// 완전 랜덤 선택 - 모든 카드가 동일한 확률로 선택됨
        /// </summary>
        Random,

        /// <summary>
        /// 등급 기반 가중치 선택 - 카드 등급(Rarity)에 따라 출현 확률이 다름
        /// Common > Uncommon > Rare > Epic > Legendary 순으로 확률 감소
        /// </summary>
        RarityWeighted
    }
}
