using Game.Data;
using UnityEngine;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 카드 UI 색상 제공자
    /// </summary>
    public static class CardUIColorProvider
    {
        public static Color GetManaCostColor(int manaCost)
        {
            if (manaCost <= 2) return Color.green;
            if (manaCost <= 4) return Color.yellow;
            if (manaCost <= 6) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }

        public static Color GetRarityColor(CardData cardData)
        {
            switch (cardData.Rarity)
            {
                case CardData.CardRarity.Common: //일반
                    return Color.green;
                case CardData.CardRarity.Uncommon: // 희귀
                    return Color.blue;
                case CardData.CardRarity.Rare: // 전설
                    return Color.yellow;
                case CardData.CardRarity.Epic: // 영웅
                    return Color.magenta;
                case CardData.CardRarity.Legendary: // 신화
                    return new Color(1f, 0.5f, 0f); // 오렌지색
                default:
                    return Color.gray;
            }
        }

        public static void UpdateDropFeedback(CardUIBaseContext context, bool isValid)
        {
            var glowEffect = context.ViewData.GlowEffect;
            if (glowEffect != null)
            {
                glowEffect.gameObject.SetActive(true);
                glowEffect.color = isValid
                    ? context.Settings.ValidDropColor
                    : context.Settings.InvalidDropColor;
            }
        }

        public static void ClearDropFeedback(CardUIBaseContext context)
        {
            var glowEffect = context.ViewData.GlowEffect;
            if (glowEffect != null)
            {
                glowEffect.gameObject.SetActive(false);
            }
        }
    }
}
