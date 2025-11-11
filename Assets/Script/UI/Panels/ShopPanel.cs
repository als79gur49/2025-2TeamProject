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

    #region Initialization

    protected override void OnInitializeSelf()
    {
        // 버튼 이벤트 바인딩 (의존성 없음)
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => OnHide());
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

        Debug.Log("[ShopPanel] Shop panel shown");
    }

    protected override void OnHidePanel()
    {
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

            GameObject itemUI = Instantiate(shopItemPrefab, shopItemContainer);
            ShopItemUI component = itemUI.GetComponent<ShopItemUI>();

            if (component != null)
            {
                component.Initialize(viewModel, OnItemBuyClicked);
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
    /// 아이템 구매 버튼 클릭
    /// </summary>
    /// <param name="itemID">구매할 아이템 ID</param>
    private void OnItemBuyClicked(string itemID)
    {
        if (shopManager == null) return;

        // itemID로 ShopItem 검색
        ShopItem item = shopManager.CurrentShopItems
            .FirstOrDefault(i => i.purchasableItem.ItemID == itemID);

        if (item != null)
        {
            shopManager.PurchaseCard(item);
        }
        else
        {
            Debug.LogError($"[ShopPanel] Item not found: {itemID}");
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 구매 성공 이벤트 처리
    /// </summary>
    private void OnPurchaseSuccess(ShopItem item)
    {
        Debug.Log($"[ShopPanel] Purchase successful: {item.purchasableItem.DisplayName}");

        // UI 갱신
        UpdateGoldDisplay();
        RebuildShopItemsUI();

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
        RebuildShopItemsUI();
    }

    /// <summary>
    /// 골드 변경 이벤트 처리
    /// </summary>
    private void OnGoldChanged(int newGold)
    {
        UpdateGoldDisplay();

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
