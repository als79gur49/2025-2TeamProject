using System;

namespace Game.Shop
{
    /// <summary>
    /// 장바구니에 담긴 아이템 정보
    /// </summary>
    [Serializable]
    public class CartItem
    {
        /// <summary>
        /// 상점 아이템 참조
        /// </summary>
        public ShopItem shopItem;

        /// <summary>
        /// 장바구니에 담긴 수량
        /// </summary>
        public int quantity;

        /// <summary>
        /// 최대 구매 가능 수량 (상점 재고)
        /// </summary>
        public int maxQuantity;

        /// <summary>
        /// 생성자
        /// </summary>
        public CartItem(ShopItem shopItem, int quantity = 0)
        {
            this.shopItem = shopItem;
            this.quantity = quantity;
            this.maxQuantity = shopItem.stockAmount;
        }

        /// <summary>
        /// 이 아이템의 총 가격 (단가 * 수량)
        /// </summary>
        public int TotalPrice => shopItem.FinalPrice * quantity;

        /// <summary>
        /// 더 추가할 수 있는지 여부
        /// </summary>
        public bool CanAddMore => quantity < maxQuantity && quantity < shopItem.stockAmount;

        /// <summary>
        /// 제거할 수 있는지 여부
        /// </summary>
        public bool CanRemove => quantity > 0;
    }
}
