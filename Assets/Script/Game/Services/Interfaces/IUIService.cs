using System;

namespace Game.Services
{
    /// <summary>
    /// Service responsible for managing user interface elements and user interactions
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// Injects dependencies required by the UIService
        /// </summary>
        /// <param name="turnService">Turn management service</param>
        /// <param name="unitService">Unit management service</param>
        void InjectDependencies(ITurnService turnService, IUnitService unitService);
        
        /// <summary>
        /// Initializes the UI service
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Updates the display with current game state
        /// </summary>
        void UpdateDisplay();
        
        /// <summary>
        /// Shows a message to the user
        /// </summary>
        /// <param name="message">Message to display</param>
        void ShowMessage(string message);
        
        /// <summary>
        /// Enables or disables the end turn button
        /// </summary>
        /// <param name="enabled">Whether the button should be enabled</param>
        void SetEndTurnButtonEnabled(bool enabled);
        
        /// <summary>
        /// Event fired when the user requests to end the current turn
        /// </summary>
        event Action OnEndTurnRequested;
        
        /// <summary>
        /// Event fired when the user requests to restart the game
        /// </summary>
        event Action OnRestartRequested;
    }
}