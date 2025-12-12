using UnityEngine;
using Game.Data;

/// <summary>
/// 상점 아이템 UI용 ViewModel
/// Domain 로직과 Presentation 계층 분리를 위한 데이터 전송 객체 (DTO)
/// </summary>
public class ShopItemViewModel
{
    /// <summary>아이템 고유 ID</summary>
    public string ItemID { get; set; }

    /// <summary>표시용 이름</summary>
    public string DisplayName { get; set; }

    /// <summary>아이콘 이미지</summary>
    public Sprite Icon { get; set; }

    /// <summary>원래 가격 (할인 전)</summary>
    public int OriginalPrice { get; set; }

    /// <summary>최종 가격 (할인 후)</summary>
    public int FinalPrice { get; set; }

    /// <summary>재고 수량</summary>
    public int Stock { get; set; }

    /// <summary>장바구니에 담긴 수량</summary>
    public int QuantityInCart { get; set; }

    /// <summary>할인이 적용되었는지 여부</summary>
    public bool HasDiscount { get; set; }

    /// <summary>구매 가능 여부 (재고 > 0)</summary>
    public bool IsAvailable { get; set; }

    /// <summary>구매할 수 있는 골드를 가지고 있는지 여부</summary>
    public bool CanAfford { get; set; }

    /// <summary>
    /// 원본 카드 데이터 (카드 아이템인 경우)
    /// ViewModel이 Model을 참조하는 것은 MVVM에서 허용됨
    /// 향후 다른 아이템 타입 추가 시 해당 타입 필드도 추가 가능 (예: WeaponData)
    /// </summary>
    public CardData CardData { get; set; }

    /// <summary>
    /// 카드팩 아이템인지 여부 (true면 CardPack, false면 단일 카드)
    /// </summary>
    public bool IsCardPack { get; set; }

    /// <summary>
    /// 카드팩 아이템인 경우 원본 카드팩 정의
    /// (단일 카드 아이템인 경우 null)
    /// </summary>
    public CardPackDefinition PackDefinition { get; set; }

    /// <summary>
    /// 남은 재고 (전체 재고 - 장바구니 수량)
    /// </summary>
    public int RemainingStock => Stock - QuantityInCart;

    /// <summary>
    /// 가격 표시 텍스트 (할인 표시 포함)
    /// 예: "500G" 또는 "<s>500</s> 350G"
    /// </summary>
    public string PriceText => HasDiscount
        ? $"<s>{OriginalPrice}</s> {FinalPrice}G"
        : $"{FinalPrice}G";

    /// <summary>
    /// 재고 표시 텍스트 (남은 재고만 표시)
    /// 예: "Stock: 3"
    /// </summary>
    public string StockText => $"Stock: {RemainingStock}";

    /// <summary>
    /// 버튼 텍스트 (상태에 따라 변경)
    /// 예: "Buy", "Sold Out", "Not Enough Gold"
    /// </summary>
    public string ButtonText
    {
        get
        {
            if (!IsAvailable) return "Sold Out";
            if (!CanAfford) return "Not Enough Gold";
            return "Buy";
        }
    }
}
