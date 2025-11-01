using UnityEngine;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 드래그 상태 관리
    /// </summary>
    public class CardUIDragState
    {
        public Vector3 OriginalPosition { get; set; }
        public Vector3 OriginalScale { get; set; }
        public Transform OriginalParent { get; set; }
        public int OriginalIndex { get; set; }
        public bool IsDragging { get; set; }
    }
}
