using UnityEngine;
using Game.Data;

/// <summary>
/// Event channel for broadcasting card information.
/// Raised when a card is dragged to display its details in the UI.
/// </summary>
[CreateAssetMenu(fileName = "CardInfoEventChannel", menuName = "Events/UI/Card Info Event Channel")]
public class CardInfoEventChannelSO : GameEventChannelSO<CardData>
{
    /// <summary>
    /// Convenience method to show card information.
    /// Validates CardData before raising the event.
    /// </summary>
    /// <param name="cardData">The card data to display</param>
    public void ShowCardInfo(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogWarning("CardInfoEventChannelSO: Attempted to show null CardData");
            return;
        }

        RaiseEvent(cardData);
    }
}
