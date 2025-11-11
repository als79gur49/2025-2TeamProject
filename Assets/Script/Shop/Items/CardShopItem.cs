using System;
using UnityEngine;
using Game.Data;


/// <summary>
/// 카드 상점 아이템 (Pure C# 클래스, ScriptableObject 아님)
/// 런타임에 생성되며 CardData를 래핑하여 IPurchasableItem 인터페이스 제공
/// </summary>
[Serializable]
public class CardShopItem : IPurchasableItem
{
    private readonly CardData cardData;
    private readonly int calculatedPrice;

    /// <summary>
    /// CardShopItem 생성자
    /// </summary>
    /// <param name="card">카드 데이터</param>
    /// <param name="price">Strategy에 의해 계산된 가격</param>
    public CardShopItem(CardData card, int price)
    {
        this.cardData = card ?? throw new ArgumentNullException(nameof(card));
        this.calculatedPrice = price;
    }

    // IPurchasableItem 구현
    public string ItemID => cardData.CardID;
    public string DisplayName => cardData.CardName;
    public Sprite DisplayIcon => cardData.CardArt;
    public string Description => cardData.Description;
    public int BasePrice => calculatedPrice;
    public CardData.CardRarity Rarity => cardData.Rarity;

    /// <summary>
    /// 구매 시 실행: 카드를 플레이어 컬렉션에 추가
    /// </summary>
    public void OnPurchase(IPurchaseContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        // CardCollection에 카드 추가 (수량 증가)
        context.CardCollection.AddCard(cardData, 1);
    }

    /// <summary>
    /// 원본 CardData 접근 (필요 시)
    /// </summary>
    public CardData GetCardData() => cardData;
}
