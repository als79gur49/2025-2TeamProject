using UnityEngine;
using Game.Data;
using Game.UI.Coordinators;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 공통 컨텍스트 - 모든 전략이 공유하는 최소한의 데이터
    /// </summary>
    public class CardUIBaseContext
    {
        public CardData CardData { get; set; }
        public Transform Transform { get; set; }
        public GameObject GameObject { get; set; }
        public MonoBehaviour MonoBehaviour { get; set; }
        public Canvas ParentCanvas { get; set; }

        public CardUIViewData ViewData { get; set; }
        public CardUIDragState DragState { get; set; }
        public CardUISettings Settings { get; set; }
        public CardUIEventChannels Events { get; set; }

        /// <summary>
        /// 중재자 패턴 - Panel 간 통신을 위한 Coordinator 참조
        /// </summary>
        public DeckInventoryCoordinator Coordinator { get; set; }
    }
}
