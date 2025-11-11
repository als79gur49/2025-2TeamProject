using UnityEngine;
using Game.Data;

/// <summary>
/// 구매 가능한 아이템의 인터페이스
/// 모든 상점 아이템이 구현해야 하는 기본 계약
/// </summary>
public interface IPurchasableItem
{
    /// <summary>아이템 고유 ID</summary>
    string ItemID { get; }

    /// <summary>표시용 이름</summary>
    string DisplayName { get; }

    /// <summary>표시용 아이콘</summary>
    Sprite DisplayIcon { get; }

    /// <summary>아이템 설명</summary>
    string Description { get; }

    /// <summary>기본 가격 (할인 적용 전)</summary>
    int BasePrice { get; }

    /// <summary>카드 등급 (Strategy 패턴에서 가격 계산에 사용)</summary>
    CardData.CardRarity Rarity { get; }

    /// <summary>
    /// 구매 시 실행되는 로직
    /// </summary>
    /// <param name="context">구매 컨텍스트 (PlayerData, CardCollection 등)</param>
    void OnPurchase(IPurchaseContext context);
}
