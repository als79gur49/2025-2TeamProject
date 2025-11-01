using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 기본 UI 데이터만 포함하는 뷰 데이터
    /// </summary>
    public class CardUIViewData
    {
        // 기본 UI 요소
        public Image CardImage { get; set; }
        public Image ItemImage { get; set; }
        public TextMeshProUGUI CostText { get; set; }
        public TextMeshProUGUI OwnedCountText { get; set; }
        public CanvasGroup CanvasGroup { get; set; }
        public Button RemoveButton { get; set; }

        // Visual Feedback
        public Image GlowEffect { get; set; }

        // Unit Stats UI
        public GameObject AttackParent { get; set; }
        public TextMeshProUGUI AttackText { get; set; }
        public GameObject HpParent { get; set; }
        public TextMeshProUGUI HpText { get; set; }
        public GameObject MovementParent { get; set; }
        public TextMeshProUGUI MovementText { get; set; }
    }
}
