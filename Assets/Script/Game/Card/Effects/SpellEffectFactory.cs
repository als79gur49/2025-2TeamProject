using Game.Data;

/// <summary>
/// Phase 4 최적화: CardData와 통합된 SpellEffectFactory
/// CardSpawnService에서 CardData를 통해 주문 효과를 생성합니다.
/// </summary>
public static class SpellEffectFactory
{
    /// <summary>
    /// CardData에서 주문 효과를 생성합니다 (Phase 4 최적화)
    /// </summary>
    /// <param name="cardData">주문 카드 데이터</param>
    /// <returns>생성된 주문 효과 인터페이스</returns>
    public static ISpellEffect CreateEffect(CardData cardData)
    {
        if (cardData == null || !cardData.IsSpellCard || !cardData.HasValidSpellData)
        {
            return null;
        }

        return CreateEffect(cardData.SpellCategory);
    }

    /// <summary>
    /// 기존 SpellType으로 효과 생성 (하위 호환성 유지)
    /// </summary>
    /// <param name="type">주문 타입</param>
    /// <returns>생성된 주문 효과 인터페이스</returns>
    public static ISpellEffect CreateEffect(CardData.SpellType type)
    {
        return type switch
        {
            CardData.SpellType.Damage => new DamageEffect(),
            CardData.SpellType.Heal => new HealEffect(),
            CardData.SpellType.Buff => new BuffEffect(),
            CardData.SpellType.Debuff => new DebuffEffect(),
            CardData.SpellType.Shield => new ShieldEffect(),
            CardData.SpellType.Teleport => new TeleportEffect(),
            CardData.SpellType.Summon => new SummonEffect(),
            _ => null
        };
    }

    /// <summary>
    /// CardData에서 주문 효과를 실행합니다 (Phase 4 최적화)
    /// </summary>
    /// <param name="cardData">주문 카드 데이터</param>
    /// <param name="position">실행 위치</param>
    /// <returns>실행 성공 여부</returns>
    public static bool ExecuteSpellEffect(CardData cardData, UnityEngine.Vector3 position)
    {
        var effect = CreateEffect(cardData);
        if (effect == null) return false;

        effect.Execute(position, cardData.SpellEffectValue, cardData.SpellRange);
        return true;
    }
}