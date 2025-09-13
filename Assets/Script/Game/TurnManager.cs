using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private bool isPlayerTurn = true;
    
    private int turnCount = 0;
    
    public bool IsPlayerTurn => isPlayerTurn;
    public int TurnCount => turnCount;
    
    public void StartGame()
    {
        isPlayerTurn = true;
        turnCount = 0;
        Debug.Log("Game Started - Player Turn");
    }
    
    public void EndTurn()
    {
        isPlayerTurn = !isPlayerTurn;
        turnCount++;
        
        if (isPlayerTurn)
        {
            Debug.Log($"Turn {turnCount}: Player Turn");
        }
        else
        {
            Debug.Log($"Turn {turnCount}: Enemy Turn");
        }
    }
}