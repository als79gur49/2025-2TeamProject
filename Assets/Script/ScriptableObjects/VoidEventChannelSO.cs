using System;
using UnityEngine;

/// <summary>
/// ScriptableObject-based event channel for parameterless events.
/// Implements the Observer pattern for decoupled component communication.
/// Use this when no data needs to be passed with the event.
/// </summary>
public abstract class VoidEventChannelSO : ScriptableObject
{
    /// <summary>
    /// Event raised when RaiseEvent is called
    /// </summary>
    private event Action OnEventRaised;

    [TextArea(2, 5)]
    [SerializeField] private string developerDescription = "";

    /// <summary>
    /// Raise the event
    /// </summary>
    public void RaiseEvent()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[{name}] Void Event Raised");
#endif
        OnEventRaised?.Invoke();
    }

    /// <summary>
    /// Subscribe to the event
    /// </summary>
    /// <param name="listener">Callback to invoke when event is raised</param>
    public void Subscribe(Action listener)
    {
        OnEventRaised -= listener; // Prevent duplicate subscriptions
        OnEventRaised += listener;
    }

    /// <summary>
    /// Unsubscribe from the event
    /// </summary>
    /// <param name="listener">Callback to remove from subscribers</param>
    public void Unsubscribe(Action listener)
    {
        OnEventRaised -= listener;
    }

    /// <summary>
    /// Get the current number of subscribers (for debugging)
    /// </summary>
    public int GetListenerCount()
    {
        return OnEventRaised?.GetInvocationList().Length ?? 0;
    }
}
