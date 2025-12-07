using System.Collections;
using UnityEngine;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 카드 UI 애니메이션 유틸리티
    /// </summary>
    public static class CardUIAnimator
    {
        public static IEnumerator ReturnToOriginalPosition(
            CardUIBaseContext context,
            System.Action onComplete = null)
        {
            var dragState = context.DragState;
            var transform = context.Transform;
            var settings = context.Settings;

            // 원래 부모로 복귀
            if (dragState.OriginalParent != null)
            {
                transform.SetParent(dragState.OriginalParent, true);
                transform.SetSiblingIndex(dragState.OriginalIndex);
            }

            // 부드러운 이동 애니메이션
            while (Vector3.Distance(transform.position, dragState.OriginalPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    dragState.OriginalPosition,
                    settings.ReturnSpeed * Time.deltaTime
                );
                transform.localScale = Vector3.Lerp(
                    transform.localScale,
                    dragState.OriginalScale,
                    settings.ReturnSpeed * Time.deltaTime
                );
                yield return null;
            }

            transform.position = dragState.OriginalPosition;
            transform.localScale = dragState.OriginalScale;

            onComplete?.Invoke();
        }

        public static void SaveDragState(CardUIBaseContext context)
        {
            var dragState = context.DragState;
            var transform = context.Transform;

            // 복귀 애니메이션 중에는 홈 상태를 덮어쓰지 않는다
            if (dragState != null && dragState.IsReturning)
            {
                return;
            }

            dragState.OriginalPosition = transform.position;
            dragState.OriginalScale = transform.localScale;
            dragState.OriginalParent = transform.parent;
            dragState.OriginalIndex = transform.GetSiblingIndex();
        }

        public static void ApplyDragVisuals(CardUIBaseContext context)
        {
            var viewData = context.ViewData;
            var settings = context.Settings;
            var transform = context.Transform;

            if (viewData.CanvasGroup != null)
            {
                viewData.CanvasGroup.alpha = settings.DragAlpha;
                viewData.CanvasGroup.blocksRaycasts = false;
            }

            transform.localScale = context.DragState.OriginalScale * settings.DragScale;

            // 최상위로 이동
            if (context.ParentCanvas != null)
            {
                transform.SetParent(context.ParentCanvas.transform, true);
            }
        }

        public static void RestoreDragVisuals(CardUIBaseContext context)
        {
            var viewData = context.ViewData;

            if (viewData.CanvasGroup != null)
            {

                // ✅ 현재 interactable 상태에 따라 복원
                 bool isInteractable = viewData.CanvasGroup.interactable;
                viewData.CanvasGroup.alpha = isInteractable ? 1f : 0.5f;
                viewData.CanvasGroup.blocksRaycasts = isInteractable;
                
                //viewData.CanvasGroup.alpha = 1f;
                //viewData.CanvasGroup.blocksRaycasts = true;
            }
        }
    }
}
