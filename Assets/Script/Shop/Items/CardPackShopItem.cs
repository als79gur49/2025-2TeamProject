using System;
using UnityEngine;
using Game.Data;
using Game.Managers;
using Game.Core;

/// <summary>
/// 카드팩 상점 아이템
/// CardPackDefinition을 래핑하여 IPurchasableItem 인터페이스를 구현
/// </summary>
[Serializable]
public class CardPackShopItem : IPurchasableItem
{
    private readonly CardPackDefinition definition;
    private readonly int calculatedPrice;
    private readonly CardRarityTable defaultRarityTable;

    public CardPackShopItem(CardPackDefinition definition, int price, CardRarityTable defaultRarityTable = null)
    {
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.calculatedPrice = price;
        this.defaultRarityTable = defaultRarityTable;
    }

    public string ItemID => definition.PackId;
    public string DisplayName => definition.DisplayName;
    public Sprite DisplayIcon => definition.Icon;
    public string Description => definition.Description;

    /// <summary>
    /// 카드팩의 기본 가격 (팩 정의의 basePrice 또는 외부 전략에 의해 계산된 값)
    /// </summary>
    public int BasePrice => calculatedPrice;

    /// <summary>
    /// 대표 레어리티: 보장된 레어리티 중 가장 높은 등급, 없으면 Common
    /// </summary>
    public CardData.CardRarity Rarity => definition.GetRepresentativeRarity();

    /// <summary>
    /// 카드팩 구매 시 실행: 카드팩 개봉, 컬렉션 반영, 연출 큐에 결과 전달
    /// </summary>
    public void OnPurchase(IPurchaseContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var collection = context.CardCollection;
        if (collection == null)
        {
            Debug.LogError("[CardPackShopItem] CardCollection is null in PurchaseContext.");
            return;
        }

        // 카드팩 개봉 및 컬렉션 반영
        var result = CardPackOpener.Open(definition, collection, defaultRarityTable);

        // 연출/보상 큐에 결과 전달 (등록된 경우에만)
        if (ServiceLocator.TryGet<IRewardPresentationQueue>(out var queue) && queue != null)
        {
            queue.EnqueueCardPackReward(result);
        }
        else
        {
            Debug.Log("[CardPackShopItem] RewardPresentationQueue not registered. Skipping visual reward enqueue.");
        }
    }
}

