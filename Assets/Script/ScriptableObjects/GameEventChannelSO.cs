using System;
using UnityEngine;

/// <summary>
/// Generic ScriptableObject-based event channel for typed events.
/// Implements the Observer pattern for decoupled component communication.
/// Based on the SoundEventChannelSO pattern.
/// </summary>
/// <typeparam name="T">The type of data passed with the event</typeparam>
public abstract class GameEventChannelSO<T> : ScriptableObject
{
    /// <summary>
    /// Event raised when RaiseEvent is called
    /// </summary>
    private event Action<T> OnEventRaised;

    [TextArea(2, 5)]
    [SerializeField] private string developerDescription = "";

    /// <summary>
    /// Raise the event with the provided data
    /// </summary>
    /// <param name="eventData">Data to pass to all subscribers</param>
    public void RaiseEvent(T eventData)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[{name}] Event Raised: {typeof(T).Name}");
#endif
        OnEventRaised?.Invoke(eventData);
    }

    /// <summary>
    /// Subscribe to the event
    /// </summary>
    /// <param name="listener">Callback to invoke when event is raised</param>
    public void Subscribe(Action<T> listener)
    {
        OnEventRaised -= listener; // Prevent duplicate subscriptions
        OnEventRaised += listener;
    }

    /// <summary>
    /// Unsubscribe from the event
    /// </summary>
    /// <param name="listener">Callback to remove from subscribers</param>
    public void Unsubscribe(Action<T> listener)
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
