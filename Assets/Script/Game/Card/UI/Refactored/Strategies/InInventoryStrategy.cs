using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using Game.Card;
using Game.UI.Coordinators;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 인벤토리 카드 전략
    /// </summary>
    public class InInventoryStrategy : BaseCardUIStrategy
    {
        private CardUIInventoryContext inventoryContext;
        private int ownedCount;

        public override void Initialize(CardUIBaseContext context)
        {
            base.Initialize(context);

            inventoryContext = context as CardUIInventoryContext;
            if (inventoryContext == null)
            {
                Debug.LogError("[InInventoryStrategy] Invalid context type - expected CardUIInventoryContext");
            }
        }

        public void SetOwnedCount(int count)
        {
            ownedCount = count;
            UpdateOwnedCountDisplay();
            RefreshInteractability(); // ← 비즈니스 규칙을 CanInteract()에 위임
        }

        /// <summary>
        /// 상호작용 규칙: 보유 개수가 1개 이상일 때만 상호작용 가능
        /// </summary>
        protected override bool CanInteract()
        {
            return base.CanInteract() && ownedCount > 0;
        }

        protected override bool CanStartDrag()
        {
            return CanInteract(); // ← 중복 제거, CanInteract() 재사용
        }

        protected override void OnDragStartInternal(PointerEventData eventData)
        {
            RaiseDragStartEvent(CardUIMode.InInventory);
            RaiseCardInfoEvent();

            Debug.Log($"[InInventoryStrategy] Started dragging: {context.CardData.CardName} (x{ownedCount})");
        }

        protected override void OnDraggingInternal(PointerEventData eventData)
        {
            // Inventory <-> Deck GlowEffect 시각적 효과 필요 x
            //ValidateDropOverDeckBuilder(eventData);
        }

        protected override bool OnDragEndInternal(PointerEventData eventData)
        {
            RaiseDragEndEvent();

            Debug.Log($"[InInventoryStrategy] Drag ended - DeckInventoryCoordinator handles actual transfer");
            return false; // 항상 false - DeckInventoryCoordinator가 처리
        }

        public override void OnClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                RaiseCardInfoEvent();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                TryQuickAddToDeck();
            }
        }

        public override void UpdateUI()
        {
            UpdateBasicUI();
            UpdateOwnedCountDisplay();

            // 인벤토리 모드 특수 UI
            if (context.ViewData.RemoveButton != null)
                context.ViewData.RemoveButton.gameObject.SetActive(false);

            // 카드 후광 UI
            if (context.ViewData.GlowEffect != null)
                context.ViewData.GlowEffect.color = CardUIColorProvider.GetRarityColor(context.CardData);

            CardUIPanelHelper.HideUnitStatPanels(context.ViewData);
        }

        private void UpdateOwnedCountDisplay()
        {
            if (context.ViewData.OwnedCountText != null)
            {
                context.ViewData.OwnedCountText.gameObject.SetActive(true);
                context.ViewData.OwnedCountText.text = $"x{ownedCount}";
            }
        }

        private void ValidateDropOverDeckBuilder(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool overDeckBuilder = false;
            foreach (var result in results)
            {
                if (result.gameObject.GetComponent<Game.UI.Panels.DeckBuilderPanel>() != null)
                {
                    overDeckBuilder = true;
                    break;
                }
            }

            CardUIColorProvider.UpdateDropFeedback(context, overDeckBuilder);
        }

        private void TryQuickAddToDeck()
        {
            if (context?.Coordinator != null && context.CardData != null)
            {
                // Coordinator를 통해 덱에 추가 요청 (중재자 패턴)
                context.Coordinator.RequestAddCardToDeck(context.CardData);
                Debug.Log($"[InInventoryStrategy] Quick-added {context.CardData.CardName} to deck via coordinator");
            }
            else
            {
                Debug.LogError("[InInventoryStrategy] Coordinator reference missing or CardData is null");
            }
        }
    }
}
