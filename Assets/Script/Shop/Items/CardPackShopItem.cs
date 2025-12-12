using System;
using UnityEngine;
using Game.Data;
using Game.Managers;

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
    private readonly CardPackRewardEventChannelSO rewardEventChannel;

    public CardPackShopItem(
        CardPackDefinition definition,
        int price,
        CardRarityTable defaultRarityTable = null,
        CardPackRewardEventChannelSO rewardEventChannel = null)
    {
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.calculatedPrice = price;
        this.defaultRarityTable = defaultRarityTable;
        this.rewardEventChannel = rewardEventChannel;
    }

    /// <summary>
    /// 카드팩의 원본 정의 데이터
    /// UI 등에서 카드팩 메타 정보를 표시할 때 사용됩니다.
    /// </summary>
    public CardPackDefinition Definition => definition;

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

        // ScriptableObject 이벤트 채널을 통한 보상 발생 알림
        if (rewardEventChannel != null)
        {
            var presentationData = new CardPackPresentationData
            {
                result = result,
                packSprite = definition.Icon
            };

            rewardEventChannel.RaiseEvent(presentationData);
        }
    }
}
