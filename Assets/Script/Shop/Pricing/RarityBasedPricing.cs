using UnityEngine;
using Game.Data;

/// <summary>
/// 등급 기반 고정 가격 책정 전략
/// ShopConfiguration의 데이터를 사용하여 카드 등급에 따라 가격 결정
/// (Config는 데이터만 제공, 실제 계산 로직은 이 Strategy가 수행)
/// </summary>
public class RarityBasedPricing : IPricingStrategy
{
    public int CalculatePrice(PricingContext context)
    {
        if (context == null || context.Config == null)
        {
            Debug.LogError("PricingContext or Config is null");
            return 0;
        }

        var config = context.Config;

        // 등급별 가격 매핑 (Config에서 데이터만 가져옴, 로직은 여기서 수행)
        return context.Rarity switch
        {
            CardData.CardRarity.Common => config.CommonPrice,
            CardData.CardRarity.Uncommon => config.UncommonPrice,
            CardData.CardRarity.Rare => config.RarePrice,
            CardData.CardRarity.Epic => config.EpicPrice,
            CardData.CardRarity.Legendary => config.LegendaryPrice,
            _ => config.CommonPrice // 기본값
        };
    }
}
