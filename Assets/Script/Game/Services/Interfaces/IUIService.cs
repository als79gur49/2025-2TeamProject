using System;

namespace Game.Services
{
    public interface IUIService
    {
        void Initialize();
        void UpdateDisplay();
        void ShowMessage(string message);
        void SetEndTurnButtonEnabled(bool enabled);
        
        event Action OnEndTurnRequested;
        event Action OnRestartRequested;
    }
}