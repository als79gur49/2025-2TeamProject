using UnityEngine;
using Game.Core;
using Game.Services;

/// <summary>
/// ⚠️ DEPRECATED: TurnManager is replaced by TurnService in the service-based architecture
/// 
/// Migration Path:
/// - Use ITurnService interface instead of TurnManager directly
/// - Access via ServiceLocator.Get<ITurnService>()
/// - TurnService provides the same functionality with better architecture
/// 
/// This class remains for compatibility but delegates to TurnService
/// </summary>
[System.Obsolete("Use ITurnService and TurnService instead. Access via ServiceLocator.Get<ITurnService>()")]
public class TurnManagerDeprecated : MonoBehaviour
{
    private ITurnService turnService;
    
    [SerializeField] private bool isPlayerTurn = true;
    private int turnCount = 0;
    
    public bool IsPlayerTurn => turnService?.IsPlayerTurn ?? isPlayerTurn;
    public int TurnCount => turnService?.TurnCount ?? turnCount;
    
    private void Start()
    {
        // Try to get TurnService from ServiceLocator
        turnService = ServiceLocator.Get<ITurnService>();
        
        if (turnService == null)
        {
            Debug.LogWarning("[TurnManagerDeprecated] ITurnService not found. Using legacy functionality.");
            Debug.LogWarning("[TurnManagerDeprecated] Recommendation: Use GameServiceManager to initialize service architecture.");
        }
        else
        {
            Debug.LogWarning("[TurnManagerDeprecated] Delegating to TurnService. Consider migrating to direct ITurnService usage.");
        }
    }
    
    public void StartGame()
    {
        if (turnService != null)
        {
            turnService.StartGame();
        }
        else
        {
            // Legacy fallback
            isPlayerTurn = true;
            turnCount = 0;
            Debug.Log("[TurnManagerDeprecated] Game Started - Player Turn (Legacy Mode)");
        }
    }
    
    public void EndTurn()
    {
        if (turnService != null)
        {
            turnService.EndTurn();
        }
        else
        {
            // Legacy fallback
            isPlayerTurn = !isPlayerTurn;
            turnCount++;
            
            if (isPlayerTurn)
            {
                Debug.Log($"[TurnManagerDeprecated] Turn {turnCount}: Player Turn (Legacy Mode)");
            }
            else
            {
                Debug.Log($"[TurnManagerDeprecated] Turn {turnCount}: Enemy Turn (Legacy Mode)");
            }
        }
    }
}