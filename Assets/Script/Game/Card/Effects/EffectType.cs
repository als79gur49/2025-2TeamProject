namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 새로운 효과 타입 열거형
    /// 기존 CardType을 대체하여 효과 기반 아키텍처를 구현합니다.
    /// </summary>
    public enum EffectType
    {
        /// <summary>대상에게 데미지를 줍니다</summary>
        Damage,

        /// <summary>대상을 회복시킵니다</summary>
        Heal,

        /// <summary>대상 위치에 유닛을 소환합니다</summary>
        Summon
    }
}