using UnityEngine;

public class TurnManagerTest : MonoBehaviour
{
    private TurnManager turnManager;
    
    void Start()
    {
        turnManager = FindObjectOfType<TurnManager>();
        if (turnManager == null)
        {
            GameObject turnManagerObj = new GameObject("TurnManager");
            turnManager = turnManagerObj.AddComponent<TurnManager>();
        }
        
        TestTurnManager();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            turnManager.EndTurn();
            Debug.Log($"Current Turn: {(turnManager.IsPlayerTurn ? "Player" : "Enemy")}, Turn Count: {turnManager.TurnCount}");
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Restarting Game...");
            turnManager.StartGame();
        }
    }
    
    private void TestTurnManager()
    {
        Debug.Log("=== TurnManager Test Started ===");
        
        turnManager.StartGame();
        Debug.Log($"Initial State - Player Turn: {turnManager.IsPlayerTurn}, Turn Count: {turnManager.TurnCount}");
        
        turnManager.EndTurn();
        Debug.Log($"After EndTurn - Player Turn: {turnManager.IsPlayerTurn}, Turn Count: {turnManager.TurnCount}");
        
        turnManager.EndTurn();
        Debug.Log($"After 2nd EndTurn - Player Turn: {turnManager.IsPlayerTurn}, Turn Count: {turnManager.TurnCount}");
        
        Debug.Log("Press SPACE to end turn, R to restart game");
        Debug.Log("=== TurnManager Test Completed ===");
    }
}