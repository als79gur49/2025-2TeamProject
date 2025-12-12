using UnityEngine;
using Game.Data;

/// <summary>
/// Event channel for broadcasting card pack information.
/// Used to request opening CardPackInfoPanel with a given CardPackDefinition.
/// </summary>
[CreateAssetMenu(fileName = "CardPackInfoEventChannel", menuName = "Events/UI/Card Pack Info Event Channel")]
public class CardPackInfoEventChannelSO : GameEventChannelSO<CardPackDefinition>
{
    /// <summary>
    /// Convenience method to show card pack information.
    /// Validates CardPackDefinition before raising the event.
    /// </summary>
    /// <param name="packDefinition">The card pack definition to display.</param>
    public void ShowPackInfo(CardPackDefinition packDefinition)
    {
        if (packDefinition == null)
        {
            Debug.LogWarning("CardPackInfoEventChannelSO: Attempted to show null CardPackDefinition");
            return;
        }

        RaiseEvent(packDefinition);
    }
}

