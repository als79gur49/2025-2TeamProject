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
        Summon,

        /// <summary>유닛의 체력/공격력/이동력을 변경하는 버프/디버프입니다</summary>
        Buff,

        /// <summary>특정 팀이 카드를 드로우합니다</summary>
        DrawCards,

        /// <summary>유닛을 기절(스턴) 상태로 만듭니다</summary>
        Stun,

        /// <summary>베이스(넥서스)의 체력을 회복합니다</summary>
        HealBase,

        /// <summary>베이스(넥서스)에 피해를 줍니다</summary>
        DamageBase,

        /// <summary>대상 유닛을 손패로 되돌립니다</summary>
        ReturnToHand
    }
}
