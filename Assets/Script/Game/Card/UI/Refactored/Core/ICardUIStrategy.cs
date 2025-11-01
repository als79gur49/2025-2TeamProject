using UnityEngine.EventSystems;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 카드 UI 전략 인터페이스
    /// </summary>
    public interface ICardUIStrategy
    {
        void Initialize(CardUIBaseContext context);
        void OnDragStart(PointerEventData eventData);
        void OnDragging(PointerEventData eventData);
        bool OnDragEnd(PointerEventData eventData);
        void OnClick(PointerEventData eventData);
        void UpdateUI();
        void UpdateInteractability(bool interactable);
        void Cleanup();
    }
}
