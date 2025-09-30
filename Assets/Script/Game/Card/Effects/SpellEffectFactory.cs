using Game.Data;
using UnityEngine;
using Game.Card.Effects;

/// <summary>
/// CardData 기반 주문 효과 팩토리
/// EffectData 시스템과 통합되어 작동합니다.
/// </summary>
public static class SpellEffectFactory
{
    /// <summary>
    /// CardData의 EffectData로부터 주문 효과를 생성합니다
    /// </summary>
    /// <param name="cardData">주문 카드 데이터</param>
    /// <returns>생성된 주문 효과 인터페이스</returns>
    public static ISpellEffect CreateEffect(CardData cardData)
    {
        if (cardData == null || !cardData.IsEffectBasedCard)
        {
            return null;
        }

        // EffectData 기반으로 효과 생성
        var effectDataList = cardData.EffectDataList;
        if (effectDataList == null || effectDataList.Count == 0)
        {
            return null;
        }

        // 첫 번째 EffectData를 사용하여 효과 생성
        var firstEffect = effectDataList[0];
        return CreateEffectFromEffectData(firstEffect);
    }

    /// <summary>
    /// EffectData로부터 주문 효과를 생성합니다
    /// </summary>
    /// <param name="effectData">효과 데이터</param>
    /// <returns>생성된 주문 효과 인터페이스</returns>
    private static ISpellEffect CreateEffectFromEffectData(EffectData effectData)
    {
        if (effectData == null || !effectData.IsValid())
        {
            return null;
        }

        return effectData.Type switch
        {
            EffectType.Damage => new DamageEffect(),
            EffectType.Heal => new HealEffect(),
            EffectType.Summon => new SummonEffect(),
            _ => null
        };
    }

    /// <summary>
    /// CardData에서 주문 효과를 실행합니다
    /// </summary>
    /// <param name="cardData">주문 카드 데이터</param>
    /// <param name="position">실행 위치</param>
    /// <returns>실행 성공 여부</returns>
    public static bool ExecuteSpellEffect(CardData cardData, Vector3 position)
    {
        var effect = CreateEffect(cardData);
        if (effect == null) return false;

        // EffectData의 값을 사용하여 실행
        var effectData = cardData.EffectDataList[0];
        effect.Execute(position, effectData.Value, effectData.AffectedRange);
        return true;
    }
}