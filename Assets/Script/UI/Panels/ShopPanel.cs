using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Managers;
using Game.SaveSystem;
using Game.Core;

/// <summary>
/// 상점 UI 패널
/// UIPanel 상속, IOpenablePanel 구현으로 TitleTestScene 버튼 바인딩 지원
/// </summary>
public class ShopPanel : UIPanel, IOpenablePanel
{
    [Header("UI References")]
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private Transform shopItemContainer;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Cart UI")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button buyCancelButton;
    [SerializeField] private TextMeshProUGUI cartCountText;
    [SerializeField] private TextMeshProUGUI cartTotalPriceText;
    [SerializeField] private RectTransform cartTargetPosition;

    // IOpenablePanel 구현
    public Button OpenButton => openButton;
    public Button CloseButton => closeButton;

    // Dependencies
    private IShopManager shopManager;
    private IPlayerDataManager playerDataManager;
    private ICardCollection collectionManager;
    private ISaveDataAdapter saveDataAdapter;

    // UI 상태
    private List<GameObject> currentItemUIs = new List<GameObject>();

    // 장바구니 상태
    private Dictionary<string, Game.Shop.CartItem> shoppingCart = new Dictionary<string, Game.Shop.CartItem>();
    private Dictionary<string, ShopItemUI> itemUICache = new Dictionary<string, ShopItemUI>();

    #region Initialization

    protected override void OnInitializeSelf()
    {
        // 버튼 이벤트 바인딩 (의존성 없음)
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => OnHide());

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);

        if (buyCancelButton != null)
            buyCancelButton.onClick.AddListener(OnBuyCancelButtonClicked);
    }

    protected override void OnInitializeWithDependencies()
    {
        // ServiceLocator를 통한 의존성 주입
        shopManager = ServiceLocator.Get<IShopManager>();
        playerDataManager = ServiceLocator.Get<IPlayerDataManager>();
        collectionManager = ServiceLocator.Get<ICardCollection>();
        saveDataAdapter = ServiceLocator.Get<ISaveDataAdapter>();

        // 검증
        if (shopManager == null)
            Debug.LogError("[ShopPanel] ShopManager not found in ServiceLocator");
        if (playerDataManager == null)
            Debug.LogError("[ShopPanel] PlayerDataManager not found in ServiceLocator");
        if (collectionManager == null)
            Debug.LogError("[ShopPanel] CardCollection not found in ServiceLocator");
        if (saveDataAdapter == null)
            Debug.LogError("[ShopPanel] SaveDataAdapter not found in ServiceLocator");

        // 이벤트 구독
        if (shopManager != null)
        {
            shopManager.OnItemPurchased += OnPurchaseSuccess;
            shopManager.OnPurchaseFailed += OnPurchaseFailed;
            shopManager.OnShopRefreshed += OnShopRefreshed;
        }

        if (playerDataManager != null)
        {
            playerDataManager.OnGoldChanged += OnGoldChanged;
        }
    }

    #endregion

    #region UIPanel Lifecycle

    protected override void OnShowPanel()
    {
        // 상점 데이터 로드
        if (saveDataAdapter != null && shopManager != null)
        {
            ShopData shopData = saveDataAdapter.LoadShopData();
            shopManager.LoadShopData(shopData);
        }

        // UI 갱신
        UpdateGoldDisplay();
        RebuildShopItemsUI();
        UpdateCartDisplay();

        Debug.Log("[ShopPanel] Shop panel shown");
    }

    protected override void OnHidePanel()
    {
        // 장바구니 비우기 (패널 닫을 때)
        ClearCart();

        // 모든 데이터 저장 (PlayerData, CardCollection, ShopData)
        SaveAllData();

        Debug.Log("[ShopPanel] Shop panel hidden");
    }

    protected override void OnCleanup()
    {
        // 이벤트 구독 해제
        if (shopManager != null)
        {
            shopManager.OnItemPurchased -= OnPurchaseSuccess;
            shopManager.OnPurchaseFailed -= OnPurchaseFailed;
            shopManager.OnShopRefreshed -= OnShopRefreshed;
        }

        if (playerDataManager != null)
        {
            playerDataManager.OnGoldChanged -= OnGoldChanged;
        }

        // UI 정리
        ClearItemUIs();

        // 장바구니 정리
        ClearCart();
    }

    #endregion

    #region UI Update Methods

    /// <summary>
    /// 골드 표시 갱신
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (goldText != null && playerDataManager != null)
        {
            goldText.text = $"Gold: {playerDataManager.CurrentGold}";
        }
    }

    /// <summary>
    /// 상점 아이템 UI 재구성
    /// </summary>
    private void RebuildShopItemsUI()
    {
        if (shopManager == null) return;

        // 기존 UI 삭제
        ClearItemUIs();
        itemUICache.Clear();

        // ViewModel 생성
        int currentGold = playerDataManager != null ? playerDataManager.CurrentGold : 0;
        List<ShopItemViewModel> viewModels = shopManager.GetAllViewModels(currentGold);

        // UI 생성
        foreach (var viewModel in viewModels)
        {
            if (shopItemPrefab == null || shopItemContainer == null)
            {
                Debug.LogError("[ShopPanel] shopItemPrefab or shopItemContainer is null");
                break;
            }

            // 장바구니 수량 반영
            if (shoppingCart.ContainsKey(viewModel.ItemID))
            {
                viewModel.QuantityInCart = shoppingCart[viewModel.ItemID].quantity;
            }

            GameObject itemUI = Instantiate(shopItemPrefab, shopItemContainer);
            ShopItemUI component = itemUI.GetComponent<ShopItemUI>();

            if (component != null)
            {
                // 좌클릭: 장바구니 추가, 우클릭: 장바구니 제거
                component.Initialize(viewModel, OnLeftClickItem, OnRightClickItem);
                itemUICache[viewModel.ItemID] = component;
            }
            else
            {
                Debug.LogError("[ShopPanel] ShopItemUI component not found on prefab");
            }

            currentItemUIs.Add(itemUI);
        }

        Debug.Log($"[ShopPanel] Rebuilt UI with {currentItemUIs.Count} items");
    }

    /// <summary>
    /// 기존 아이템 UI 삭제
    /// </summary>
    private void ClearItemUIs()
    {
        foreach (GameObject ui in currentItemUIs)
        {
            if (ui != null)
                Destroy(ui);
        }
        currentItemUIs.Clear();
    }

    /// <summary>
    /// 장바구니 UI 표시 업데이트
    /// </summary>
    private void UpdateCartDisplay()
    {
        int totalItems = 0;
        int totalPrice = 0;

        foreach (var cartItem in shoppingCart.Values)
        {
            totalItems += cartItem.quantity;
            totalPrice += cartItem.TotalPrice;
        }

        // 장바구니 개수 텍스트
        if (cartCountText != null)
        {
            cartCountText.text = totalItems > 0 ? $"{totalItems} items" : "Empty";
        }

        // 장바구니 총 가격 텍스트
        if (cartTotalPriceText != null)
        {
            cartTotalPriceText.text = $"Total: {totalPrice} Gold";
        }

        // 버튼 활성화 상태 업데이트
        bool hasItems = totalItems > 0;
        bool canAfford = playerDataManager != null && playerDataManager.CurrentGold >= totalPrice;

        if (buyButton != null)
        {
            buyButton.interactable = hasItems && canAfford;
        }

        if (buyCancelButton != null)
        {
            buyCancelButton.interactable = hasItems;
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// 새로고침 버튼 클릭
    /// </summary>
    private void OnRefreshButtonClicked()
    {
        if (shopManager != null)
        {
            shopManager.RefreshShop();
            Debug.Log("[ShopPanel] Refresh button clicked");
        }
    }

    /// <summary>
    /// 아이템 좌클릭 (장바구니에 추가)
    /// </summary>
    private void OnLeftClickItem(string itemID)
    {
        AddToCart(itemID);
    }

    /// <summary>
    /// 아이템 우클릭 (장바구니에서 제거)
    /// </summary>
    private void OnRightClickItem(string itemID)
    {
        RemoveFromCart(itemID);
    }

    /// <summary>
    /// 일괄 구매 버튼 클릭
    /// </summary>
    private void OnBuyButtonClicked()
    {
        PurchaseCartItems();
    }

    /// <summary>
    /// 장바구니 취소 버튼 클릭
    /// </summary>
    private void OnBuyCancelButtonClicked()
    {
        ClearCart();
        UpdateCartDisplay();
        RebuildShopItemsUI();
        Debug.Log("[ShopPanel] Cart cleared");
    }

    #endregion

    #region Cart Management

    /// <summary>
    /// 장바구니에 아이템 추가
    /// </summary>
    private void AddToCart(string itemID)
    {
        if (shopManager == null) return;

        // ShopItem 찾기
        ShopItem shopItem = shopManager.CurrentShopItems
            .FirstOrDefault(i => i.purchasableItem.ItemID == itemID);

        if (shopItem == null)
        {
            Debug.LogError($"[ShopPanel] Item not found: {itemID}");
            return;
        }

        // 장바구니에 이미 있는지 확인
        if (!shoppingCart.ContainsKey(itemID))
        {
            shoppingCart[itemID] = new Game.Shop.CartItem(shopItem, 0);
        }

        Game.Shop.CartItem cartItem = shoppingCart[itemID];

        // 재고 확인
        if (!cartItem.CanAddMore)
        {
            Debug.LogWarning($"[ShopPanel] Cannot add more: {itemID} (stock limit reached)");

            // 실패 애니메이션 재생
            if (itemUICache.ContainsKey(itemID))
            {
                itemUICache[itemID].PlayFailAnimation();
            }
            return;
        }

        // 수량 증가
        cartItem.quantity++;
        Debug.Log($"[ShopPanel] Added to cart: {itemID} (quantity: {cartItem.quantity})");

        // UI 업데이트
        UpdateCartDisplay();
        RebuildShopItemsUI();

        // 애니메이션 재생
        if (itemUICache.ContainsKey(itemID) && cartTargetPosition != null)
        {
            itemUICache[itemID].PlayAddToCartAnimation(cartTargetPosition);
        }
    }

    /// <summary>
    /// 장바구니에서 아이템 제거
    /// </summary>
    private void RemoveFromCart(string itemID)
    {
        if (!shoppingCart.ContainsKey(itemID))
        {
            Debug.LogWarning($"[ShopPanel] Item not in cart: {itemID}");

            // 실패 애니메이션 재생
            if (itemUICache.ContainsKey(itemID))
            {
                itemUICache[itemID].PlayFailAnimation();
            }
            return;
        }

        Game.Shop.CartItem cartItem = shoppingCart[itemID];

        if (!cartItem.CanRemove)
        {
            Debug.LogWarning($"[ShopPanel] Cannot remove: {itemID} (quantity is 0)");

            // 실패 애니메이션 재생
            if (itemUICache.ContainsKey(itemID))
            {
                itemUICache[itemID].PlayFailAnimation();
            }
            return;
        }

        // 수량 감소
        cartItem.quantity--;
        Debug.Log($"[ShopPanel] Removed from cart: {itemID} (quantity: {cartItem.quantity})");

        // 수량이 0이면 장바구니에서 제거
        if (cartItem.quantity == 0)
        {
            shoppingCart.Remove(itemID);
        }

        // UI 업데이트
        UpdateCartDisplay();
        RebuildShopItemsUI();

        // 애니메이션 재생
        if (itemUICache.ContainsKey(itemID))
        {
            itemUICache[itemID].PlayRemoveFromCartAnimation();
        }
    }

    /// <summary>
    /// 장바구니의 모든 아이템 일괄 구매
    /// </summary>
    private void PurchaseCartItems()
    {
        if (shopManager == null || playerDataManager == null) return;

        if (shoppingCart.Count == 0)
        {
            Debug.LogWarning("[ShopPanel] Cart is empty");
            return;
        }

        // 총 가격 계산
        int totalPrice = 0;
        foreach (var cartItem in shoppingCart.Values)
        {
            totalPrice += cartItem.TotalPrice;
        }

        // 골드 검증
        if (playerDataManager.CurrentGold < totalPrice)
        {
            Debug.LogError($"[ShopPanel] Not enough gold. Need: {totalPrice}, Have: {playerDataManager.CurrentGold}");
            OnPurchaseFailed($"골드가 부족합니다. 필요: {totalPrice} Gold");
            return;
        }

        // 일괄 구매 시도
        int successCount = 0;
        List<string> itemsToRemove = new List<string>();

        foreach (var kvp in shoppingCart)
        {
            string itemID = kvp.Key;
            Game.Shop.CartItem cartItem = kvp.Value;

            // 각 아이템을 수량만큼 구매
            for (int i = 0; i < cartItem.quantity; i++)
            {
                bool success = shopManager.PurchaseCard(cartItem.shopItem);
                if (success)
                {
                    successCount++;
                }
                else
                {
                    Debug.LogError($"[ShopPanel] Failed to purchase: {itemID}");
                    break;
                }
            }

            itemsToRemove.Add(itemID);
        }

        // 장바구니 비우기
        ClearCart();

        // UI 업데이트
        UpdateGoldDisplay();
        UpdateCartDisplay();
        RebuildShopItemsUI();

        // 데이터 저장 (한 번만)
        SaveAllData();

        Debug.Log($"[ShopPanel] Batch purchase completed: {successCount} items purchased");
    }

    /// <summary>
    /// 장바구니 비우기
    /// </summary>
    private void ClearCart()
    {
        shoppingCart.Clear();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 구매 성공 이벤트 처리
    /// </summary>
    private void OnPurchaseSuccess(ShopItem item)
    {
        Debug.Log($"[ShopPanel] Purchase successful: {item.purchasableItem.DisplayName}");

        // 일괄 구매가 아닌 경우에만 UI 갱신 (일괄 구매는 내부에서 처리)
        // UI 갱신은 PurchaseCartItems에서 처리됨

        // TODO: 성공 피드백 (토스트 메시지, 애니메이션 등)
    }

    /// <summary>
    /// 구매 실패 이벤트 처리
    /// </summary>
    private void OnPurchaseFailed(string reason)
    {
        Debug.LogWarning($"[ShopPanel] Purchase failed: {reason}");

        // TODO: 실패 피드백 (토스트 메시지, 경고음 등)
    }

    /// <summary>
    /// 상점 새로고침 이벤트 처리
    /// </summary>
    private void OnShopRefreshed()
    {
        Debug.Log("[ShopPanel] Shop refreshed event received");

        // 상점이 새로고침되면 장바구니 비우기 (아이템이 더 이상 존재하지 않음)
        ClearCart();

        UpdateCartDisplay();
        RebuildShopItemsUI();
    }

    /// <summary>
    /// 골드 변경 이벤트 처리
    /// </summary>
    private void OnGoldChanged(int newGold)
    {
        UpdateGoldDisplay();

        // 장바구니 구매 가능 여부 업데이트
        UpdateCartDisplay();

        // 구매 가능 여부가 변경되었을 수 있으므로 UI 재구성
        RebuildShopItemsUI();
    }

    #endregion

    #region Save/Load

    /// <summary>
    /// 모든 데이터 저장 (PlayerData, CardCollection, ShopData)
    /// </summary>
    private void SaveAllData()
    {
        if (saveDataAdapter == null) return;

        // SaveDataAdapter가 내부적으로 매니저에서 데이터를 수집하여 저장
        saveDataAdapter.SaveSpecific(Game.SaveSystem.SaveFileType.PlayerData);
        saveDataAdapter.SaveSpecific(Game.SaveSystem.SaveFileType.CardCollection);

        // ShopData는 ShopManager에서 이미 저장됨 (PurchaseCard/RefreshShop 시)
        Debug.Log("[ShopPanel] All data saved");
    }

    #endregion
}
