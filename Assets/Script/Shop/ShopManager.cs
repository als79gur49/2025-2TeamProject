using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Managers;
using Game.SaveSystem;
using Game.Data;
using Game.Core;

/// <summary>
/// 상점 시스템의 핵심 매니저
/// Strategy 패턴을 통해 가격 책정, 할인, 카드 선택 로직을 유연하게 관리
/// </summary>
public class ShopManager : MonoBehaviour, IShopManager
{
    [Header("Configuration")]
    [SerializeField] private ShopConfiguration config;

    // Strategy 패턴
    private IPricingStrategy pricingStrategy;
    private IDiscountStrategy discountStrategy;
    private ICardSelectionStrategy selectionStrategy;

    // 매니저들
    private IPlayerDataManager playerDataManager;
    private ICardCollection collectionManager;
    private ISaveDataAdapter saveDataAdapter;

    // Context 객체
    private IPurchaseContext purchaseContext;

    // 상점 데이터
    private List<ShopItem> currentShopItems = new List<ShopItem>();
    private bool isInitialized = false;

    // 이벤트
    public event Action<ShopItem> OnItemPurchased;
    public event Action<string> OnPurchaseFailed;
    public event Action OnShopRefreshed;

    /// <summary>현재 상점 아이템 목록 (읽기 전용)</summary>
    public IReadOnlyList<ShopItem> CurrentShopItems => currentShopItems.AsReadOnly();

    /// <summary>
    /// ShopManager 초기화
    /// </summary>
    public void Initialize(
        IPlayerDataManager playerData,
        ICardCollection collection,
        ISaveDataAdapter saveAdapter,
        IPricingStrategy pricing = null,
        IDiscountStrategy discount = null,
        ICardSelectionStrategy selection = null)
    {
        // 필수 의존성 검증
        playerDataManager = playerData ?? throw new ArgumentNullException(nameof(playerData));
        collectionManager = collection ?? throw new ArgumentNullException(nameof(collection));
        saveDataAdapter = saveAdapter ?? throw new ArgumentNullException(nameof(saveAdapter));

        // Strategy 기본값 설정
        pricingStrategy = pricing ?? new RarityBasedPricing();
        discountStrategy = discount ?? new RandomDiscountStrategy();
        selectionStrategy = selection ?? new RandomSelectionStrategy();

        // PurchaseContext 생성
        purchaseContext = new PurchaseContext(playerDataManager, collectionManager);

        isInitialized = true;
        Debug.Log("ShopManager initialized successfully");
    }

    /// <summary>
    /// 상점 새로고침 (새 아이템 진열)
    /// </summary>
    public void RefreshShop()
    {
        if (!isInitialized)
        {
            Debug.LogError("ShopManager not initialized");
            return;
        }

        currentShopItems.Clear();

        // 1. CardDatabase에서 모든 카드 가져오기 (ServiceLocator를 통해 접근)
        var cardRegistry = ServiceLocator.Get<ICardRegistry>();
        if (cardRegistry == null)
        {
            Debug.LogError("CardRegistry not available from ServiceLocator");
            return;
        }

        List<CardData> availableCards = cardRegistry.GetAllCards().ToList();

        // 2. SelectionStrategy로 카드 선택
        List<CardData> selectedCards = selectionStrategy.SelectCards(
            availableCards,
            config.ShopItemCount
        );

        // 3. 선택된 카드들을 ShopItem으로 변환
        foreach (CardData card in selectedCards)
        {
            // PricingContext 생성
            var pricingContext = new PricingContext(card.Rarity, config)
            {
                CardCollection = collectionManager,
                PlayerData = playerDataManager,
                CardData = card
            };

            // PricingStrategy로 가격 계산
            int price = pricingStrategy.CalculatePrice(pricingContext);

            // CardShopItem 생성 (Pure C# class)
            var item = new CardShopItem(card, price);

            // DiscountStrategy로 할인 적용
            float discount = discountStrategy.ShouldApplyDiscount(config)
                ? discountStrategy.GetDiscountAmount(config)
                : 0f;

            // ShopItem 생성 및 추가
            ShopItem shopItem = new ShopItem(item, config.DefaultStock, discount);
            currentShopItems.Add(shopItem);
        }

        // 4. 저장 및 이벤트 발생
        SaveShopData();
        OnShopRefreshed?.Invoke();

        Debug.Log($"Shop refreshed: {currentShopItems.Count} items loaded");
    }

    /// <summary>
    /// 카드 구매
    /// </summary>
    /// <param name="item">구매할 ShopItem</param>
    /// <returns>구매 성공 여부</returns>
    public bool PurchaseCard(ShopItem item)
    {
        // 검증 1: 초기화 확인
        if (!isInitialized)
        {
            Debug.LogError("ShopManager not initialized");
            return false;
        }

        // 검증 2: 아이템 유효성
        if (item == null || !currentShopItems.Contains(item))
        {
            OnPurchaseFailed?.Invoke("Invalid item");
            return false;
        }

        // 검증 3: 재고 확인
        if (!item.IsAvailable)
        {
            OnPurchaseFailed?.Invoke("Out of stock");
            return false;
        }

        // 검증 4: 골드 확인
        int finalPrice = item.FinalPrice;
        if (playerDataManager.CurrentGold < finalPrice)
        {
            OnPurchaseFailed?.Invoke($"Not enough gold. Need {finalPrice}");
            return false;
        }

        // 트랜잭션 실행
        try
        {
            // 1. 골드 차감
            if (!playerDataManager.SpendGold(finalPrice))
            {
                OnPurchaseFailed?.Invoke("Failed to spend gold");
                return false;
            }

            // 2. 아이템 구매 (카드 컬렉션에 추가)
            item.purchasableItem.OnPurchase(purchaseContext);

            // 3. 재고 감소
            item.stockAmount--;

            // 4. 저장
            SaveAfterPurchase();

            // 5. 이벤트 발생
            OnItemPurchased?.Invoke(item);

            Debug.Log($"Purchase successful: {item.purchasableItem.DisplayName} for {finalPrice}G");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Purchase failed with exception: {e.Message}");
            OnPurchaseFailed?.Invoke("Purchase failed");
            return false;
        }
    }

    /// <summary>
    /// 구매 후 저장 (PlayerData, CardCollection, ShopData 모두 저장)
    /// </summary>
    private void SaveAfterPurchase()
    {
        // SaveDataAdapter가 내부적으로 매니저에서 데이터를 수집하여 저장
        saveDataAdapter.SaveSpecific(Game.SaveSystem.SaveFileType.PlayerData);
        saveDataAdapter.SaveSpecific(Game.SaveSystem.SaveFileType.CardCollection);
        SaveShopData();
    }

    /// <summary>
    /// 상점 데이터 저장
    /// </summary>
    private void SaveShopData()
    {
        ShopData shopData = new ShopData
        {
            items = currentShopItems.Select(item => new ShopItemData
            {
                cardID = item.purchasableItem.ItemID,
                basePrice = item.purchasableItem.BasePrice,
                stockAmount = item.stockAmount,
                discountPercentage = item.discountPercentage
            }).ToList()
        };

        saveDataAdapter.SaveShopData(shopData);
    }

    /// <summary>
    /// 상점 데이터 로드
    /// </summary>
    /// <param name="shopData">로드할 상점 데이터</param>
    public void LoadShopData(ShopData shopData)
    {
        if (!isInitialized)
        {
            Debug.LogError("ShopManager not initialized");
            return;
        }

        // 데이터가 없거나 비어있으면 새로고침
        if (shopData == null || shopData.items == null || shopData.items.Count == 0)
        {
            Debug.Log("No shop data to load, refreshing shop");
            RefreshShop();
            return;
        }

        currentShopItems.Clear();

        // CardRegistry 가져오기 (ServiceLocator를 통해 접근)
        var cardRegistry = ServiceLocator.Get<ICardRegistry>();
        if (cardRegistry == null)
        {
            Debug.LogError("CardRegistry not available from ServiceLocator");
            return;
        }

        // ShopItemData를 ShopItem으로 복원
        foreach (var itemData in shopData.items)
        {
            CardData card = cardRegistry.GetCardByID(itemData.cardID);
            if (card == null)
            {
                Debug.LogWarning($"Card not found: {itemData.cardID}");
                continue;
            }

            // CardShopItem 생성 (저장된 basePrice 사용)
            var item = new CardShopItem(card, itemData.basePrice);

            // ShopItem 생성 (저장된 재고 및 할인율 복원)
            ShopItem shopItem = new ShopItem(item, itemData.stockAmount, itemData.discountPercentage);
            currentShopItems.Add(shopItem);
        }

        Debug.Log($"Shop loaded: {currentShopItems.Count} items restored");
    }

    /// <summary>
    /// UI를 위한 ViewModel 목록 생성
    /// </summary>
    /// <param name="currentGold">현재 플레이어 골드</param>
    /// <returns>ShopItemViewModel 목록</returns>
    public List<ShopItemViewModel> GetAllViewModels(int currentGold)
    {
        return currentShopItems.Select(item => new ShopItemViewModel
        {
            ItemID = item.purchasableItem.ItemID,
            DisplayName = item.purchasableItem.DisplayName,
            Icon = item.purchasableItem.DisplayIcon,
            OriginalPrice = item.purchasableItem.BasePrice,
            FinalPrice = item.FinalPrice,
            Stock = item.stockAmount,
            HasDiscount = item.HasDiscount,
            IsAvailable = item.IsAvailable,
            CanAfford = currentGold >= item.FinalPrice
        }).ToList();
    }
}
