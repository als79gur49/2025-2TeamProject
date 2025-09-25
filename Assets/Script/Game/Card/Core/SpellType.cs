namespace Game.Card.Core
{
    /// <summary>
    /// 주문 카드 타입 열거형 - Phase 4에서 확장 구현
    /// CardSpawnService에서 주문 효과 적용에 사용됩니다.
    /// </summary>
    public enum SpellType
    {
        /// <summary>데미지 주문 - 대상에게 피해를 줍니다</summary>
        Damage,
        
        /// <summary>힐 주문 - 대상을 회복시킵니다</summary>
        Heal,
        
        /// <summary>버프 주문 - 대상을 강화합니다</summary>
        Buff,
        
        /// <summary>디버프 주문 - 대상을 약화시킵니다</summary>
        Debuff,
        
        /// <summary>실드 주문 - 대상에게 보호막을 제공합니다</summary>
        Shield,
        
        /// <summary>텔레포트 주문 - 대상을 이동시킵니다</summary>
        Teleport,
        
        /// <summary>소환 주문 - 새로운 유닛을 소환합니다</summary>
        Summon
    }
}