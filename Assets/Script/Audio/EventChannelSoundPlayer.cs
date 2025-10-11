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
    [SerializeField] private bool playSound = false;

    /// <summary>
    /// Play a sound - AudioData.Loop property automatically determines playback behavior
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

        // Broadcast event - AudioData.Loop property determines behavior
        // For loop sounds, pass 'this' as owner to enable proper cleanup
        object owner = soundToPlay.Loop ? this : null;
        soundEventChannel.RaiseSoundEvent(soundToPlay, owner);
    }

    /// <summary>
    /// Play a different sound (useful for events with multiple sounds)
    /// </summary>
    public void PlayCustomSound(AudioData customSound)
    {
        if (soundEventChannel == null || customSound == null)
            return;

        object owner = customSound.Loop ? this : null;
        soundEventChannel.RaiseSoundEvent(customSound, owner);
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Editor testing in Inspector
    /// </summary>
    private void OnValidate()
    {
        if (playSound)
        {
            playSound = false;
            if (Application.isPlaying)
            {
                PlaySound();
            }
        }
    }
    #endif
}
