namespace Game.Core
{
    /// <summary>
    /// Marker interface for services that must persist across scene transitions.
    /// Implementing services MUST call DontDestroyOnLoad in Awake.
    /// </summary>
    public interface IGlobalService
    {
        /// <summary>
        /// Validate that the service is in a valid state.
        /// Used for runtime debugging and health checks.
        /// </summary>
        bool IsValid();
    }
}
