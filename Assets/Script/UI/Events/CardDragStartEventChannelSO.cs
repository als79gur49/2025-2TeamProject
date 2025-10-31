using UnityEngine;
using Game.Card;

namespace Game.UI.Events
{
    /// <summary>
    /// 카드 드래그 시작 이벤트 채널
    /// CardUI가 드래그를 시작할 때 CardDragData를 브로드캐스트
    /// 구독자(Manager/Service)가 mode 필드를 보고 자신의 도메인 이벤트만 처리
    /// </summary>
    [CreateAssetMenu(fileName = "CardDragStartEventChannel", menuName = "Events/Card/Card Drag Start Event Channel")]
    public class CardDragStartEventChannelSO : GameEventChannelSO<CardDragData>
    {
        /// <summary>
        /// 카드 드래그 시작 이벤트 발생
        /// </summary>
        /// <param name="dragData">드래그 데이터 (CardData, mode, sourceTransform, sourceIndex)</param>
        public void RaiseDragStart(CardDragData dragData)
        {
            if (dragData.cardData == null)
            {
                Debug.LogWarning("[CardDragStartEventChannel] Attempted to raise event with null CardData");
                return;
            }

            RaiseEvent(dragData);
        }
    }
}
