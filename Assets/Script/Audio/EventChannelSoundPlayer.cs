using UnityEngine;

/// <summary>
/// Example proof-of-concept: Sound player using Event Channel architecture.
/// This component demonstrates complete decoupling from AudioServiceContainer.
/// Can be used for any gameplay system that needs to play sounds.
/// </summary>
public class EventChannelSoundPlayer : MonoBehaviour
{
    [Header("Event Channel")]
    [Tooltip("The sound event channel to broadcast through")]
    [SerializeField] private SoundEventChannelSO soundEventChannel;

    [Header("Sound Data")]
    [Tooltip("AudioData assets to play")]
    [SerializeField] private AudioData soundToPlay;

    [Header("Test Controls (Editor Only)")]
    [SerializeField] private bool play2DSound = false;
    [SerializeField] private bool play3DSound = false;
    [SerializeField] private Vector3 testPosition = Vector3.zero;

    /// <summary>
    /// Play a 2D sound (no spatial audio)
    /// </summary>
    public void PlaySound()
    {
        if (soundEventChannel == null)
        {
            Debug.LogError("EventChannelSoundPlayer: No SoundEventChannel assigned!");
            return;
        }

        if (soundToPlay == null)
        {
            Debug.LogError("EventChannelSoundPlayer: No AudioData assigned!");
            return;
        }

        // Broadcast event - no direct dependency on AudioServiceContainer!
        soundEventChannel.RaiseSoundEvent(soundToPlay);
    }

    /// <summary>
    /// Play a sound with optional position parameter (currently unused, for future extensibility)
    /// </summary>
    public void PlaySoundAtPosition(Vector3 position)
    {
        if (soundEventChannel == null)
        {
            Debug.LogError("EventChannelSoundPlayer: No SoundEventChannel assigned!");
            return;
        }

        if (soundToPlay == null)
        {
            Debug.LogError("EventChannelSoundPlayer: No AudioData assigned!");
            return;
        }

        // Broadcast event with position parameter (currently unused)
        soundEventChannel.RaiseSoundEvent(soundToPlay, position);
    }

    /// <summary>
    /// Play a sound at this GameObject's position
    /// </summary>
    public void PlaySoundHere()
    {
        PlaySoundAtPosition(transform.position);
    }

    /// <summary>
    /// Play a different sound (useful for events with multiple sounds)
    /// </summary>
    public void PlayCustomSound(AudioData customSound)
    {
        if (soundEventChannel == null || customSound == null)
            return;

        soundEventChannel.RaiseSoundEvent(customSound);
    }

    /// <summary>
    /// Play a custom sound with optional position parameter (currently unused, for future extensibility)
    /// </summary>
    public void PlayCustomSoundAt(AudioData customSound, Vector3 position)
    {
        if (soundEventChannel == null || customSound == null)
            return;

        soundEventChannel.RaiseSoundEvent(customSound, position);
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Editor testing in Inspector
    /// </summary>
    private void OnValidate()
    {
        if (play2DSound)
        {
            play2DSound = false;
            if (Application.isPlaying)
            {
                PlaySound();
            }
        }

        if (play3DSound)
        {
            play3DSound = false;
            if (Application.isPlaying)
            {
                PlaySoundAtPosition(testPosition);
            }
        }
    }
    #endif
}
