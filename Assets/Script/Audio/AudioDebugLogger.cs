using UnityEngine;

/// <summary>
/// Debug logger for audio events in development builds.
/// Attaches to AudioServiceContainer to monitor all sound events in real-time.
/// Toggle-able via Inspector for performance optimization.
/// </summary>
public class AudioDebugLogger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private bool logToUI = false;

    [Header("Event Channel")]
    [SerializeField] private SoundEventChannelSO soundEventChannel;

    [Header("Filtering")]
    [SerializeField] private bool filterByName = false;
    [SerializeField] private string nameFilter = "";

    [Header("Statistics")]
    [SerializeField] private bool trackStatistics = true;
    private int totalEventsLogged = 0;
    private int eventsThisFrame = 0;

    private void OnEnable()
    {
        if (soundEventChannel != null)
        {
            soundEventChannel.OnSoundRequested += HandleSoundEvent;
            LogMessage("AudioDebugLogger: Subscribed to sound event channel");
        }
        else
        {
            Debug.LogWarning("AudioDebugLogger: No SoundEventChannel assigned!");
        }
    }

    private void OnDisable()
    {
        if (soundEventChannel != null)
        {
            soundEventChannel.OnSoundRequested -= HandleSoundEvent;
            LogMessage("AudioDebugLogger: Unsubscribed from sound event channel");
        }
    }

    private void LateUpdate()
    {
        // Reset per-frame counter
        if (eventsThisFrame > 0)
        {
            eventsThisFrame = 0;
        }
    }

    /// <summary>
    /// Handle sound event from the event channel
    /// </summary>
    private void HandleSoundEvent(AudioData soundData, Vector3 position)
    {
        if (!enableLogging)
            return;

        // Apply name filter if enabled
        if (filterByName && !string.IsNullOrEmpty(nameFilter))
        {
            if (!soundData.name.Contains(nameFilter))
                return;
        }

        // Update statistics
        if (trackStatistics)
        {
            totalEventsLogged++;
            eventsThisFrame++;
        }

        // Format log message
        string positionStr = position == Vector3.zero ? "Standard" : $"with position {position}";
        string message = $"[AudioEvent #{totalEventsLogged}] '{soundData.name}' - {positionStr}";

        LogMessage(message);

        // Check for potential spam
        if (eventsThisFrame > 10)
        {
            Debug.LogWarning($"AudioDebugLogger: High event count this frame ({eventsThisFrame})! Possible audio spam.");
        }
    }

    /// <summary>
    /// Log message to configured outputs
    /// </summary>
    private void LogMessage(string message)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (logToConsole)
        {
            Debug.Log(message);
        }

        // UI logging can be implemented later with a TextMeshPro component
        if (logToUI)
        {
            // TODO: Implement on-screen logging
        }
        #endif
    }

    /// <summary>
    /// Get current statistics summary
    /// </summary>
    public string GetStatisticsSummary()
    {
        return $"Total Events: {totalEventsLogged} | This Frame: {eventsThisFrame} | Listeners: {soundEventChannel?.GetListenerCount() ?? 0}";
    }

    /// <summary>
    /// Reset statistics
    /// </summary>
    [ContextMenu("Reset Statistics")]
    public void ResetStatistics()
    {
        totalEventsLogged = 0;
        eventsThisFrame = 0;
        Debug.Log("AudioDebugLogger: Statistics reset");
    }

    /// <summary>
    /// Toggle logging at runtime
    /// </summary>
    public void SetLoggingEnabled(bool enabled)
    {
        enableLogging = enabled;
        Debug.Log($"AudioDebugLogger: Logging {(enabled ? "enabled" : "disabled")}");
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure soundEventChannel is assigned in editor
        if (soundEventChannel == null)
        {
            Debug.LogWarning("AudioDebugLogger: Please assign a SoundEventChannel!");
        }
    }
    #endif
}
