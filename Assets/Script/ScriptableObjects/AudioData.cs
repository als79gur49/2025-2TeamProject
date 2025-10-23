using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Audio type classification for routing to appropriate audio service
/// </summary>
public enum AudioType
{
    BGM,    // Background music - routes to BGMAudioService
    Effect  // Sound effects - routes to EffectAudioService
}

/// <summary>
/// ScriptableObject-based audio data container for the audio system.
/// Supports multiple clips for variation, parametric randomization,
/// mixer routing, and anti-spam protection.
/// </summary>
[CreateAssetMenu(fileName = "NewAudioData", menuName = "Audio/AudioData")]
public class AudioData : ScriptableObject
{
    [Header("Audio Type Classification")]
    [Tooltip("Determines which audio service handles this sound (BGM or Effect)")]
    [SerializeField] private AudioType audioType = AudioType.Effect;

    [Header("Audio Clips")]
    [Tooltip("Multiple clips for variation - plays randomly")]
    [SerializeField] private AudioClip[] clips;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMin = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float volumeMax = 1.0f;

    [Header("Pitch Settings")]
    [Range(0.1f, 3f)]
    [SerializeField] private float pitchMin = 1.0f;
    [Range(0.1f, 3f)]
    [SerializeField] private float pitchMax = 1.0f;

    [Header("Audio Routing")]
    [Tooltip("AudioMixer group for this sound (optional)")]
    [SerializeField] private AudioMixerGroup mixerGroup;

    [Header("Playback Settings")]
    [SerializeField] private bool loop = false;
    [SerializeField] private float fadeInTime = 0f;
    [SerializeField] private float fadeOutTime = 0f;
    [Range(0, 256)]
    [SerializeField] private int priority = 128;

    [Header("Anti-Spam Protection")]
    [Tooltip("Minimum time between plays (0 = no limit)")]
    [SerializeField] private float cooldownTime = 0f;

    // Runtime state
    private float lastPlayTime = -Mathf.Infinity;

    // Public Properties
    public AudioType AudioType => audioType;
    public AudioMixerGroup MixerGroup => mixerGroup;
    public bool Loop => loop;
    public float FadeInTime => fadeInTime;
    public float FadeOutTime => fadeOutTime;
    public int Priority => priority;

    /// <summary>
    /// Get a random clip from the array
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    /// <summary>
    /// Get a random volume within the specified range
    /// </summary>
    public float GetRandomVolume()
    {
        return Random.Range(volumeMin, volumeMax);
    }

    /// <summary>
    /// Get a random pitch within the specified range
    /// </summary>
    public float GetRandomPitch()
    {
        return Random.Range(pitchMin, pitchMax);
    }

    /// <summary>
    /// Check if this sound can be played (cooldown check)
    /// </summary>
    public bool CanPlay()
    {
        if (cooldownTime <= 0)
            return true;

        float timeSinceLastPlay = Time.time - lastPlayTime;
        if (timeSinceLastPlay < cooldownTime)
            return false;

        lastPlayTime = Time.time;
        return true;
    }

    /// <summary>
    /// Validation in editor
    /// </summary>
    private void OnValidate()
    {
        // Ensure min <= max
        if (volumeMin > volumeMax)
            volumeMax = volumeMin;
        if (pitchMin > pitchMax)
            pitchMax = pitchMin;
    }
}
