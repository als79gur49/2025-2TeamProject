using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private int health = 100;
    [SerializeField] private int attackPower = 10;
    [SerializeField] private int movementRange = 1;
    [SerializeField] private bool isPlayerUnit = true;
    
    private int maxHealth;
    private GridManager gridManager;
    private Tile currentTile;
    
    public int Health => health;
    public int MaxHealth => maxHealth;
    public int AttackPower => attackPower;
    public int MovementRange => movementRange;
    public bool IsAlive => health > 0;
    public bool IsPlayerUnit => isPlayerUnit;
    public Tile CurrentTile => currentTile;
    
    private void Awake()
    {
        maxHealth = health;
        gridManager = FindObjectOfType<GridManager>();
    }
    
    public void SetCurrentTile(Tile tile)
    {
        currentTile = tile;
    }
    
    public void OnTurnStart()
    {
        if (!IsAlive) return;
        
        Act();
    }
    
    private void Act()
    {
        Unit enemy = SearchForNearbyEnemies();
        
        if (enemy != null)
        {
            AttackEnemy(enemy);
        }
        else
        {
            MoveForward();
        }
    }
    
    private Unit SearchForNearbyEnemies()
    {
        if (currentTile == null || gridManager == null) return null;
        
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        
        for (int i = 0; i < dx.Length; i++)
        {
            int newX = currentTile.X + dx[i];
            int newY = currentTile.Y + dy[i];
            
            Tile adjacentTile = gridManager.GetTile(newX, newY);
            if (adjacentTile != null && adjacentTile.IsOccupied)
            {
                Unit adjacentUnit = adjacentTile.OccupyingUnit;
                if (adjacentUnit != null && adjacentUnit.IsPlayerUnit != this.IsPlayerUnit)
                {
                    return adjacentUnit;
                }
            }
        }
        
        return null;
    }
    
    private void AttackEnemy(Unit enemy)
    {
        if (enemy == null || !enemy.IsAlive) return;
        
        Debug.Log($"{gameObject.name} attacks {enemy.gameObject.name} for {attackPower} damage!");
        enemy.TakeDamage(attackPower);
    }
    
    private void MoveForward()
    {
        if (currentTile == null || gridManager == null) return;
        
        int targetX = currentTile.X;
        int targetY = currentTile.Y + (isPlayerUnit ? movementRange : -movementRange);
        
        if (gridManager.CanPlaceUnitAt(targetX, targetY))
        {
            Tile targetTile = gridManager.GetTile(targetX, targetY);
            if (targetTile != null)
            {
                currentTile.RemoveUnit();
                
                targetTile.PlaceUnit(this);
                SetCurrentTile(targetTile);
                
                Debug.Log($"{gameObject.name} moved to ({targetX}, {targetY})");
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} cannot move forward - path blocked or out of bounds");
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;
        
        health -= damage;
        health = Mathf.Max(0, health);
        
        Debug.Log($"{gameObject.name} took {damage} damage. Current health: {health}/{maxHealth}");
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");
        Destroy(gameObject);
    }
    
    public void Heal(int healAmount)
    {
        if (!IsAlive) return;
        
        health += healAmount;
        health = Mathf.Min(maxHealth, health);
        
        Debug.Log($"{gameObject.name} healed {healAmount}. Current health: {health}/{maxHealth}");
    }
}