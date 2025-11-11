using System;
using System.Collections.Generic;
using Game.Managers;
using Game.SaveSystem;

/// <summary>
/// 상점 시스템의 핵심 매니저 인터페이스
/// Strategy 패턴을 통해 가격 책정, 할인, 카드 선택 로직을 유연하게 관리
/// </summary>
public interface IShopManager
{
    /// <summary>아이템 구매 성공 이벤트</summary>
    event Action<ShopItem> OnItemPurchased;

    /// <summary>구매 실패 이벤트</summary>
    event Action<string> OnPurchaseFailed;

    /// <summary>상점 새로고침 이벤트</summary>
    event Action OnShopRefreshed;

    /// <summary>현재 상점 아이템 목록 (읽기 전용)</summary>
    IReadOnlyList<ShopItem> CurrentShopItems { get; }

    /// <summary>
    /// ShopManager 초기화
    /// </summary>
    /// <param name="playerData">플레이어 데이터 매니저</param>
    /// <param name="collection">카드 컬렉션 매니저</param>
    /// <param name="saveAdapter">저장 데이터 어댑터</param>
    /// <param name="pricing">가격 책정 전략 (선택적)</param>
    /// <param name="discount">할인 전략 (선택적)</param>
    /// <param name="selection">카드 선택 전략 (선택적)</param>
    void Initialize(
        IPlayerDataManager playerData,
        ICardCollection collection,
        ISaveDataAdapter saveAdapter,
        IPricingStrategy pricing = null,
        IDiscountStrategy discount = null,
        ICardSelectionStrategy selection = null);

    /// <summary>
    /// 상점 새로고침 (새 아이템 진열)
    /// </summary>
    void RefreshShop();

    /// <summary>
    /// 카드 구매
    /// </summary>
    /// <param name="item">구매할 ShopItem</param>
    /// <returns>구매 성공 여부</returns>
    bool PurchaseCard(ShopItem item);

    /// <summary>
    /// 상점 데이터 로드
    /// </summary>
    /// <param name="shopData">로드할 상점 데이터</param>
    void LoadShopData(ShopData shopData);

    /// <summary>
    /// UI를 위한 ViewModel 목록 생성
    /// </summary>
    /// <param name="currentGold">현재 플레이어 골드</param>
    /// <returns>ShopItemViewModel 목록</returns>
    List<ShopItemViewModel> GetAllViewModels(int currentGold);
}
