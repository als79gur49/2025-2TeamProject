using UnityEngine;

/// <summary>
/// 상점 설정 데이터 (순수 데이터 저장소, 로직 없음)
/// ScriptableObject로 에디터에서 설정 가능
/// </summary>
[CreateAssetMenu(fileName = "ShopConfig", menuName = "Shop/Configuration")]
public class ShopConfiguration : ScriptableObject
{
    [Header("Shop Settings")]
    [SerializeField] private int shopItemCount = 6;
    [SerializeField] private int defaultStock = 3;

    [Header("Pricing by Rarity")]
    [SerializeField] private int commonPrice = 100;
    [SerializeField] private int uncommonPrice = 250;
    [SerializeField] private int rarePrice = 500;
    [SerializeField] private int epicPrice = 1000;
    [SerializeField] private int legendaryPrice = 2000;

    [Header("Discount Settings")]
    [SerializeField, Range(0f, 1f)] private float discountChance = 0.3f;
    [SerializeField, Range(0f, 0.5f)] private float minDiscount = 0.1f;
    [SerializeField, Range(0f, 0.5f)] private float maxDiscount = 0.3f;

    // Read-only properties (Immutability)
    public int ShopItemCount => shopItemCount;
    public int DefaultStock => defaultStock;
    public int CommonPrice => commonPrice;
    public int UncommonPrice => uncommonPrice;
    public int RarePrice => rarePrice;
    public int EpicPrice => epicPrice;
    public int LegendaryPrice => legendaryPrice;
    public float DiscountChance => discountChance;
    public float MinDiscount => minDiscount;
    public float MaxDiscount => maxDiscount;
}
