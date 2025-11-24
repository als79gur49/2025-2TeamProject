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

        // Broadcast event via AudioPlayRequest - AudioData.Loop property still determines behavior
        // For loop sounds, pass 'this' as owner to enable proper cleanup
        object owner = soundToPlay.Loop ? this : null;
        var request = AudioPlayRequest.Create(soundToPlay, owner);
        soundEventChannel.RaiseSoundEvent(request);
    }

    /// <summary>
    /// Play a different sound (useful for events with multiple sounds)
    /// </summary>
    public void PlayCustomSound(AudioData customSound)
    {
        if (soundEventChannel == null || customSound == null)
            return;

        object owner = customSound.Loop ? this : null;
        var request = AudioPlayRequest.Create(customSound, owner);
        soundEventChannel.RaiseSoundEvent(request);
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
