using UnityEngine;

/// <summary>
/// Event channel for broadcasting card drag end events.
/// Raised when a card drag operation completes (successful drop or cancelled).
/// Used to hide card information panels.
/// </summary>
[CreateAssetMenu(fileName = "CardDragEndEventChannel", menuName = "Events/UI/Card Drag End Event Channel")]
public class CardDragEndEventChannelSO : VoidEventChannelSO
{
    // No additional methods needed - inherits RaiseEvent() from VoidEventChannelSO
}
