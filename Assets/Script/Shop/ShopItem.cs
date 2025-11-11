using System;
using UnityEngine;

/// <summary>
/// 상점에 진열되는 아이템 (재고 및 할인 정보 포함)
/// IPurchasableItem을 래핑하여 상점 전용 메타데이터 추가
/// </summary>
[Serializable]
public class ShopItem
{
    /// <summary>구매 가능한 아이템 (CardShopItem 등)</summary>
    public IPurchasableItem purchasableItem;

    /// <summary>현재 재고 수량</summary>
    public int stockAmount;

    /// <summary>할인율 (0.0 ~ 1.0, 0.2 = 20% 할인)</summary>
    public float discountPercentage;

    /// <summary>
    /// ShopItem 생성자
    /// </summary>
    /// <param name="item">구매 가능한 아이템</param>
    /// <param name="stock">초기 재고</param>
    /// <param name="discount">할인율 (0.0 ~ 1.0)</param>
    public ShopItem(IPurchasableItem item, int stock, float discount)
    {
        purchasableItem = item ?? throw new ArgumentNullException(nameof(item));
        stockAmount = stock;
        discountPercentage = Mathf.Clamp01(discount);
    }

    /// <summary>
    /// 할인 적용된 최종 가격
    /// basePrice 중복 없이 purchasableItem.BasePrice 직접 사용
    /// </summary>
    public int FinalPrice => Mathf.RoundToInt(
        purchasableItem.BasePrice * (1 - discountPercentage)
    );

    /// <summary>구매 가능 여부 (재고 확인)</summary>
    public bool IsAvailable => stockAmount > 0;

    /// <summary>할인 적용 여부</summary>
    public bool HasDiscount => discountPercentage > 0;
}
