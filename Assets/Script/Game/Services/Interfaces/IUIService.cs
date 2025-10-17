using System;

namespace Game.Services
{
    /// <summary>
    /// Service responsible for managing user interface elements and user interactions
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// Initializes the UIService with dependency injection
        /// Injects dependencies, retrieves ServiceLocator dependencies, and initializes UI elements
        /// </summary>
        /// <param name="turnService">Turn management service</param>
        /// <param name="unitService">Unit management service</param>
        void Init(ITurnService turnService, IUnitService unitService);

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