using Game.Data;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// UI 패널 가시성 헬퍼
    /// </summary>
    public static class CardUIPanelHelper
    {
        public static void HideUnitStatPanels(CardUIViewData viewData)
        {
            if (viewData.AttackParent != null)
                viewData.AttackParent.SetActive(false);
            if (viewData.HpParent != null)
                viewData.HpParent.SetActive(false);
            if (viewData.MovementParent != null)
                viewData.MovementParent.SetActive(false);
        }

        public static void UpdateUnitStatPanels(CardUIViewData viewData, CardData cardData, bool visible)
        {
            bool hasUnitStats = visible && cardData != null &&
                                cardData.HasEffectType(Game.Card.Effects.EffectType.Summon);

            if (viewData.AttackParent != null)
                viewData.AttackParent.SetActive(hasUnitStats);
            if (viewData.HpParent != null)
                viewData.HpParent.SetActive(hasUnitStats);
            if (viewData.MovementParent != null)
                viewData.MovementParent.SetActive(hasUnitStats);
        }
    }
}
