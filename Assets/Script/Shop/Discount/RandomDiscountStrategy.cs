using UnityEngine;

/// <summary>
/// 랜덤 할인 전략
/// Config의 DiscountChance 확률로 할인 적용
/// 할인율은 MinDiscount ~ MaxDiscount 범위에서 랜덤 결정
/// </summary>
public class RandomDiscountStrategy : IDiscountStrategy
{
    public bool ShouldApplyDiscount(ShopConfiguration config)
    {
        if (config == null)
        {
            Debug.LogError("ShopConfiguration is null");
            return false;
        }

        // Config의 DiscountChance 확률로 할인 여부 결정
        // 예: 0.3 = 30% 확률
        return Random.value < config.DiscountChance;
    }

    public float GetDiscountAmount(ShopConfiguration config)
    {
        if (config == null)
        {
            Debug.LogError("ShopConfiguration is null");
            return 0f;
        }

        // Config의 MinDiscount ~ MaxDiscount 범위에서 랜덤 할인율 결정
        // 예: 0.1 ~ 0.3 (10% ~ 30% 할인)
        return Random.Range(config.MinDiscount, config.MaxDiscount);
    }
}
