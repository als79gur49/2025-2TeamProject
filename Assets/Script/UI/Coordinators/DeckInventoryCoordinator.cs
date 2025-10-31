using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Game.Data;
using Game.UI.Panels;
using Game.Card;
using Game.UI.Events;
using Game.UI.Feedback;

namespace Game.UI.Coordinators
{
    /// <summary>
    /// 덱 빌더와 인벤토리 패널 간의 통신을 조율하는 Coordinator
    /// 드래그 앤 드롭 이벤트를 중계하고, 패널 간 데이터 동기화를 담당
    /// </summary>
    public class DeckInventoryCoordinator : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private InventoryPanel inventoryPanel;
        [SerializeField] private DeckBuilderPanel deckPanel;

        [Header("Event Channels")]
        [SerializeField] private CardDragStartEventChannelSO dragStartChannel;

        [Header("Visual Feedback")]
        [SerializeField] private CardTransferFeedback transferFeedback;

        #region Lifecycle

        private void Start()
        {
            // InventoryPanel에 DeckBuilderPanel 참조 설정
            if (inventoryPanel != null && deckPanel != null)
            {
                inventoryPanel.SetDeckBuilderPanel(deckPanel);
                Debug.Log("[DeckInventoryCoordinator] DeckBuilderPanel reference set to InventoryPanel");
            }
        }

        private void OnEnable()
        {
            // 드래그 시작 이벤트 구독
            if (dragStartChannel != null)
            {
                dragStartChannel.Subscribe(OnCardDragStart);
            }

            // 덱 변경 이벤트 구독
            if (deckPanel != null)
            {
                deckPanel.OnCardAddedToDeck += OnCardAddedToDeck;
                deckPanel.OnCardRemovedFromDeck += OnCardRemovedFromDeck;
            }
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (dragStartChannel != null)
            {
                dragStartChannel.Unsubscribe(OnCardDragStart);
            }

            if (deckPanel != null)
            {
                deckPanel.OnCardAddedToDeck -= OnCardAddedToDeck;
                deckPanel.OnCardRemovedFromDeck -= OnCardRemovedFromDeck;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 카드 드래그 시작 시 호출
        /// </summary>
        private void OnCardDragStart(CardDragData dragData)
        {
            if (dragData.mode == CardUIMode.InInventory)
            {
                // 인벤토리 → 덱: 덱 패널에 드래그 중인 카드 정보 전달
                if (deckPanel != null)
                {
                    deckPanel.SetDraggedCard(dragData.cardData);
                }
                Debug.Log($"[DeckInventoryCoordinator] Card drag started (Inventory → Deck): {dragData.cardData.CardName}");
            }
            else if (dragData.mode == CardUIMode.InDeck)
            {
                // 덱 → 인벤토리: 인벤토리 패널에 드래그 중인 카드 정보 전달
                if (inventoryPanel != null)
                {
                    inventoryPanel.SetDraggedCard(dragData.cardData);
                }
                Debug.Log($"[DeckInventoryCoordinator] Card drag started (Deck → Inventory): {dragData.cardData.CardName}");
            }
        }

        /// <summary>
        /// 덱에 카드 추가 시 호출
        /// </summary>
        private void OnCardAddedToDeck(CardData card)
        {
            // 사용 가능한 개수 계산 및 인벤토리 UI 업데이트
            if (inventoryPanel != null && deckPanel != null)
            {
                int ownedCount = Game.Managers.CollectionManager.Instance.GetOwnedCount(card);
                int inDeckCount = deckPanel.GetDeckCardCount(card);
                int availableCount = ownedCount - inDeckCount;

                inventoryPanel.UpdateCardAvailability(card, availableCount);
            }

            // 시각/청각 피드백 재생
            if (transferFeedback != null && deckPanel != null)
            {
                transferFeedback.PlayAddToDeckFeedback(deckPanel.transform.position);
            }

            Debug.Log($"[DeckInventoryCoordinator] Card added to deck: {card.CardName}");
        }

        /// <summary>
        /// 덱에서 카드 제거 시 호출
        /// </summary>
        private void OnCardRemovedFromDeck(CardData card)
        {
            // 사용 가능한 개수 계산 및 인벤토리 UI 업데이트
            if (inventoryPanel != null && deckPanel != null)
            {
                int ownedCount = Game.Managers.CollectionManager.Instance.GetOwnedCount(card);
                int inDeckCount = deckPanel.GetDeckCardCount(card);
                int availableCount = ownedCount - inDeckCount;

                inventoryPanel.UpdateCardAvailability(card, availableCount);
            }

            // 시각/청각 피드백 재생
            if (transferFeedback != null && deckPanel != null)
            {
                transferFeedback.PlayRemoveFromDeckFeedback(deckPanel.transform.position);
            }

            Debug.Log($"[DeckInventoryCoordinator] Card removed from deck: {card.CardName}");
        }

        #endregion

        #region Utility

        /// <summary>
        /// 드래그가 특정 패널 위에서 끝났는지 확인
        /// </summary>
        private bool IsDraggedOverPanel(PointerEventData eventData, UIPanel panel)
        {
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (var result in raycastResults)
            {
                if (result.gameObject.GetComponentInParent<UIPanel>() == panel)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 덱에서 인벤토리로 카드 드롭 처리 (public으로 외부에서 호출 가능)
        /// </summary>
        public void OnCardDroppedToInventory(CardData card)
        {
            if (deckPanel != null && card != null)
            {
                // 덱에서 카드 제거
                deckPanel.RemoveCardFromDeck(card);
                Debug.Log($"[DeckInventoryCoordinator] Card dropped to inventory: {card.CardName}");
            }
        }

        #endregion
    }
}
