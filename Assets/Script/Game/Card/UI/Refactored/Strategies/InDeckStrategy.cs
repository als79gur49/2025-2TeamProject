using UnityEngine;
using UnityEngine.EventSystems;
using Game.Card;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 덱 빌더 카드 전략
    /// </summary>
    public class InDeckStrategy : BaseCardUIStrategy
    {
        private CardUIBuilderContext builderContext;
        private int cardCount;

        public override void Initialize(CardUIBaseContext context)
        {
            base.Initialize(context);

            builderContext = context as CardUIBuilderContext;
            if (builderContext == null)
            {
                Debug.LogError("[InDeckStrategy] Invalid context type - expected CardUIBuilderContext");
            }
        }

        public void SetCardCount(int count)
        {
            cardCount = count;
            UpdateCardCountDisplay();
            RefreshInteractability(); // ← 비즈니스 규칙을 CanInteract()에 위임
        }

        /// <summary>
        /// 상호작용 규칙: 덱에 1개 이상 포함되어 있을 때만 상호작용 가능
        /// </summary>
        protected override bool CanInteract()
        {
            return base.CanInteract() && cardCount > 0;
        }

        protected override bool CanStartDrag()
        {
            return CanInteract(); // ← 중복 제거, CanInteract() 재사용
        }

        protected override void OnDragStartInternal(PointerEventData eventData)
        {
            RaiseDragStartEvent(CardUIMode.InDeck);

            Debug.Log($"[InDeckStrategy] Started dragging for reordering: {context.CardData.CardName} (x{cardCount})");
        }

        protected override void OnDraggingInternal(PointerEventData eventData)
        {
            // 향후 재정렬 프리뷰 구현
        }

        protected override bool OnDragEndInternal(PointerEventData eventData)
        {
            RaiseDragEndEvent();

            // 현재는 재정렬 미지원
            Debug.Log($"[InDeckStrategy] Reordering not implemented yet");
            return false;
        }

        public override void OnClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                RaiseCardInfoEvent();
            }
            else if (eventData.button == PointerEventData.InputButton.Right && CanInteract())
            {
                RemoveFromDeck();
            }
        }

        public override void UpdateUI()
        {
            UpdateBasicUI();
            UpdateCardCountDisplay();

            // 덱 모드 특수 UI
            if (context.ViewData.RemoveButton != null)
            {
                context.ViewData.RemoveButton.gameObject.SetActive(true);
                context.ViewData.RemoveButton.onClick.RemoveAllListeners();
                context.ViewData.RemoveButton.onClick.AddListener(RemoveFromDeck);
            }

            // 카드 후광 UI
            if (context.ViewData.GlowEffect != null)
                context.ViewData.GlowEffect.color = CardUIColorProvider.GetRarityColor(context.CardData);

            CardUIPanelHelper.HideUnitStatPanels(context.ViewData);
        }

        public override void UpdateInteractability(bool interactable)
        {
            base.UpdateInteractability(interactable);

            // 제거 버튼도 함께 제어
            if (context.ViewData.RemoveButton != null)
            {
                context.ViewData.RemoveButton.interactable = interactable;
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();

            if (context.ViewData.RemoveButton != null)
            {
                context.ViewData.RemoveButton.onClick.RemoveAllListeners();
            }
        }

        private void UpdateCardCountDisplay()
        {
            if (context.ViewData.OwnedCountText != null)
            {
                context.ViewData.OwnedCountText.gameObject.SetActive(true);
                context.ViewData.OwnedCountText.text = $"x{cardCount}";
            }
        }

        private void RemoveFromDeck()
        {
            if (context?.Coordinator != null && context.CardData != null)
            {
                // Coordinator를 통해 덱에서 제거 요청 (중재자 패턴)
                context.Coordinator.RequestRemoveCardFromDeck(context.CardData);
                Debug.Log($"[InDeckStrategy] Removed {context.CardData.CardName} from deck via coordinator");
            }
            else
            {
                Debug.LogError("[InDeckStrategy] Coordinator reference missing or CardData is null");
            }
        }
    }
}
