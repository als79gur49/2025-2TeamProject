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
    /// Event raised when a sound is requested
    /// AudioData.Loop property determines playback behavior (one-shot vs loop)
    /// </summary>
    public UnityAction<AudioData, object> OnSoundRequested;

    /// <summary>
    /// [루프 사운드 중지 이벤트] Event raised when loop sound stop is requested by owner
    /// Parameters: (object owner, AudioData audioData)
    /// - audioData가 null이면 해당 owner의 모든 루프 중지
    /// - audioData가 있으면 해당 owner의 특정 AudioData만 중지
    /// </summary>
    public UnityAction<object, AudioData> OnStopLoopRequested;

    /// <summary>
    /// Raise a sound event
    /// AudioData.Loop property automatically determines one-shot vs loop playback
    /// </summary>
    /// <param name="soundData">The audio data to play</param>
    /// <param name="owner">Owner for loop sounds (required if soundData.Loop is true)</param>
    public void RaiseSoundEvent(AudioData soundData, object owner = null)
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
        OnSoundRequested?.Invoke(soundData, owner);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        string playbackType = soundData.Loop ? "loop" : "one-shot";
        Debug.Log($"SoundEventChannelSO: Sound event raised - '{soundData.name}' ({playbackType})");
        #endif
    }

    /// <summary>
    /// [통합 메서드] 루프 사운드 중지 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="owner">사운드 소유자</param>
    /// <param name="audioData">중지할 AudioData (null일 경우 해당 owner의 모든 루프 사운드)</param>
    public void RaiseStopLoopEvent(object owner, AudioData audioData = null)
    {
        if (owner == null)
        {
            Debug.LogWarning("SoundEventChannelSO: null owner로 루프 중지 이벤트 발생 시도");
            return;
        }

        OnStopLoopRequested?.Invoke(owner, audioData);

        #if UNITY_EDITOR
        string target = audioData == null ? "모든 루프" : $"'{audioData.name}' 루프";
        Debug.Log($"SoundEventChannelSO: Loop stop event raised - Owner: {owner}, Target: {target}");
        #endif
    }

    /// <summary>
    /// [오버로드] 소유자의 모든 루프 사운드 중지 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="owner">사운드 소유자</param>
    public void RaiseStopAllLoopsEvent(object owner)
    {
        RaiseStopLoopEvent(owner, null);
    }

    /// <summary>
    /// [오버로드] 소유자의 특정 AudioData 루프 중지 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="owner">사운드 소유자</param>
    /// <param name="audioData">중지할 AudioData</param>
    public void RaiseStopSpecificLoopEvent(object owner, AudioData audioData)
    {
        RaiseStopLoopEvent(owner, audioData);
    }

    /// <summary>
    /// Get current subscriber count for debugging
    /// </summary>
    public int GetListenerCount()
    {
        return OnSoundRequested?.GetInvocationList().Length ?? 0;
    }
}
