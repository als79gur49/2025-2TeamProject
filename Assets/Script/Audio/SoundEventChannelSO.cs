using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ScriptableObject-based event channel for audio system decoupling.
/// Implements the Observer pattern for broadcasting sound events across the game.
/// This allows complete decoupling between sound requesters and the audio system.
/// </summary>
[CreateAssetMenu(fileName = "SoundEventChannel", menuName = "Audio/Event Channel")]
public class SoundEventChannelSO : ScriptableObject
{
    /// <summary>
    /// Event raised when a sound is requested with optional position parameter
    /// </summary>
    public UnityAction<AudioData, Vector3> OnSoundRequested;

    /// <summary>
    /// Raise a sound event with optional position parameter (currently unused, for future extensibility)
    /// </summary>
    /// <param name="soundData">The audio data to play</param>
    /// <param name="position">Optional world position (currently not used for spatial audio)</param>
    public void RaiseSoundEvent(AudioData soundData, Vector3 position)
    {
        if (soundData == null)
        {
            Debug.LogWarning("SoundEventChannelSO: Attempted to raise event with null AudioData");
            return;
        }

        // Check cooldown before broadcasting
        if (!soundData.CanPlay())
        {
            Debug.Log($"SoundEventChannelSO: '{soundData.name}' is on cooldown, skipping");
            return;
        }

        // Broadcast to all listeners
        OnSoundRequested?.Invoke(soundData, position);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"SoundEventChannelSO: Sound event raised - '{soundData.name}' at {position}");
        #endif
    }

    /// <summary>
    /// Raise a sound event without position parameter
    /// </summary>
    /// <param name="soundData">The audio data to play</param>
    public void RaiseSoundEvent(AudioData soundData)
    {
        // Use Vector3.zero as default position (not used for spatial audio)
        RaiseSoundEvent(soundData, Vector3.zero);
    }

    /// <summary>
    /// Get current subscriber count for debugging
    /// </summary>
    public int GetListenerCount()
    {
        return OnSoundRequested?.GetInvocationList().Length ?? 0;
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Editor-only validation and debugging
    /// </summary>
    private void OnValidate()
    {
        // Can add editor-only validation logic here
    }

    /// <summary>
    /// Editor-only: Clear all listeners (helps prevent cross-scene issues in editor)
    /// </summary>
    [ContextMenu("Clear All Listeners")]
    private void ClearAllListeners()
    {
        if (OnSoundRequested != null)
        {
            foreach (var d in OnSoundRequested.GetInvocationList())
            {
                OnSoundRequested -= (UnityAction<AudioData, Vector3>)d;
            }
            Debug.Log("SoundEventChannelSO: All listeners cleared");
        }
    }
    #endif
}
