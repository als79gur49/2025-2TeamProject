using System;
using System.Collections.Generic;

/// <summary>
/// 상점 데이터 (저장/로드용)
/// 진열된 아이템, 재고, 할인 정보 등을 포함
/// </summary>
[Serializable]
public class ShopData
{
    /// <summary>상점 아이템 목록</summary>
    public List<ShopItemData> items = new List<ShopItemData>();

    /// <summary>마지막 수정 시간</summary>
    public DateTime lastModified = DateTime.Now;
}

/// <summary>
/// 상점 아이템 데이터 (저장/로드용)
/// IPurchasableItem의 직렬화 가능한 버전
/// </summary>
[Serializable]
public class ShopItemData
{
    /// <summary>아이템 타입 (카드 단품, 카드팩 등)</summary>
    public ShopItemType itemType = ShopItemType.CardSingle;

    /// <summary>아이템 고유 ID (CardData의 cardID, CardPackDefinition의 packId 등)</summary>
    public string itemId;

    /// <summary>아이템 ID (CardData의 cardID)</summary>
    public string cardID;

    /// <summary>기본 가격 (Strategy로 계산된 가격)</summary>
    public int basePrice;

    /// <summary>현재 재고 수량</summary>
    public int stockAmount;

    /// <summary>할인율 (0.0 ~ 1.0)</summary>
    public float discountPercentage;
}

/// <summary>
/// 상점 아이템 타입
/// </summary>
public enum ShopItemType
{
    CardSingle = 0,
    CardPack = 1
}
