using UnityEngine;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 카드 UI 설정값
    /// </summary>
    public class CardUISettings
    {
        public float DragAlpha { get; set; } = 0.6f;
        public float DragScale { get; set; } = 0.7f;
        public float ReturnSpeed { get; set; } = 10f;
        public bool ReturnToOriginalPosition { get; set; } = true;
        public Color ValidDropColor { get; set; } = Color.green;
        public Color InvalidDropColor { get; set; } = Color.red;
    }
}
