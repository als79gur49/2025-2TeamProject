using UnityEngine;
using UnityEngine.EventSystems;
using Game.Core;
using Game.Card;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 전략 베이스 클래스 - 공통 로직 구현
    /// </summary>
    public abstract class BaseCardUIStrategy : ICardUIStrategy
    {
        protected CardUIBaseContext context;
        protected bool isDragging;
        protected Coroutine returnCoroutine;

        public virtual void Initialize(CardUIBaseContext context)
        {
            this.context = context;
            UpdateUI();
        }

        public virtual void OnDragStart(PointerEventData eventData)
        {
            if (!CanStartDrag()) return;

            isDragging = true;
            context.DragState.IsDragging = true;

            // 드래그 상태 저장
            CardUIAnimator.SaveDragState(context);

            // 드래그 시각적 효과 적용
            CardUIAnimator.ApplyDragVisuals(context);

            // 전략별 추가 처리
            OnDragStartInternal(eventData);
        }

        public virtual void OnDragging(PointerEventData eventData)
        {
            if (!isDragging) return;

            context.Transform.position = eventData.position;

            // 전략별 추가 처리
            OnDraggingInternal(eventData);
        }

        public virtual bool OnDragEnd(PointerEventData eventData)
        {
            if (!isDragging) return false;

            isDragging = false;
            context.DragState.IsDragging = false;

            // 시각적 피드백 복원
            CardUIAnimator.RestoreDragVisuals(context);

            // 전략별 드롭 처리
            bool dropSuccess = OnDragEndInternal(eventData);

            // 실패 시 원위치 복귀
            if (!dropSuccess && context.Settings.ReturnToOriginalPosition)
            {
                returnCoroutine = context.MonoBehaviour.StartCoroutine(
                    CardUIAnimator.ReturnToOriginalPosition(context, OnReturnComplete)
                );
            }

            return dropSuccess;
        }

        public abstract void OnClick(PointerEventData eventData);
        public abstract void UpdateUI();

        public virtual void UpdateInteractability(bool interactable)
        {
            var viewData = context.ViewData;

            if (viewData.CanvasGroup != null)
            {
                viewData.CanvasGroup.alpha = interactable ? 1f : 0.5f;
                viewData.CanvasGroup.interactable = interactable;
                viewData.CanvasGroup.blocksRaycasts = interactable;
            }
        }

        /// <summary>
        /// 상호작용 가능 여부를 판단하는 템플릿 메서드
        /// 하위 전략에서 오버라이드하여 고유 규칙 구현
        /// </summary>
        protected virtual bool CanInteract()
        {
            return context.CardData != null;
        }

        /// <summary>
        /// CanInteract() 결과를 기반으로 상호작용 상태 갱신
        /// 데이터 변경 후 호출하여 UI 동기화
        /// </summary>
        protected void RefreshInteractability()
        {
            UpdateInteractability(CanInteract());
        }

        public virtual void Cleanup()
        {
            if (returnCoroutine != null && context.MonoBehaviour != null)
            {
                context.MonoBehaviour.StopCoroutine(returnCoroutine);
            }
        }

        // Abstract methods for strategy-specific implementation
        protected abstract bool CanStartDrag();
        protected abstract void OnDragStartInternal(PointerEventData eventData);
        protected abstract void OnDraggingInternal(PointerEventData eventData);
        protected abstract bool OnDragEndInternal(PointerEventData eventData);
        protected virtual void OnReturnComplete() { }

        // Helper methods
        protected void RaiseCardInfoEvent()
        {
            if (context.Events?.CardInfoChannel != null && context.CardData != null)
            {
                context.Events.CardInfoChannel.RaiseEvent(context.CardData);
            }
        }

        protected void RaiseDragStartEvent(CardUIMode mode)
        {
            if (context.Events?.CardDragStartChannel != null && context.CardData != null)
            {
                var dragData = new CardDragData
                {
                    cardData = context.CardData,
                    mode = mode,
                    sourceTransform = context.Transform,
                    sourceIndex = context.DragState.OriginalIndex
                };
                context.Events.CardDragStartChannel.RaiseDragStart(dragData);
            }
        }

        protected void RaiseDragEndEvent()
        {
            Debug.Log($"[BaseCardUIStrategy] RaiseDragEndEvent called");
            Debug.Log($"[BaseCardUIStrategy] Events: {context.Events != null}, Channel: {context.Events?.CardDragEndChannel != null}");
    
            if (context.Events?.CardDragEndChannel != null)
            {
                Debug.Log($"[BaseCardUIStrategy] Calling RaiseEvent on {context.Events.CardDragEndChannel.name}");
                context.Events.CardDragEndChannel.RaiseEvent();
            }
            else
            {
                Debug.LogError("[BaseCardUIStrategy] CardDragEndChannel is NULL!");
            }
            
            //context.Events?.CardDragEndChannel?.RaiseEvent();
        }

        protected void UpdateBasicUI()
        {
            var viewData = context.ViewData;
            var cardData = context.CardData;

            if (cardData == null) return;

            // 비용 텍스트
            if (viewData.CostText != null)
            {
                viewData.CostText.text = cardData.ManaCost.ToString();
                viewData.CostText.color = CardUIColorProvider.GetManaCostColor(cardData.ManaCost);
            }

            // 카드 이미지
            if (viewData.ItemImage != null && cardData.CardArt != null)
            {
                viewData.ItemImage.sprite = cardData.CardArt;
            }
        }
    }
}
