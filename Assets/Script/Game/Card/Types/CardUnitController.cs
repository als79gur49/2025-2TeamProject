using UnityEngine;
using System;

public class CardUnitController : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] private string unitName;
    [SerializeField] private UnitRarity rarity;
    
    private UnitStats baseStats;
    private int currentHP;
    private bool isAlive = true;
    private Vector3 currentPosition;
    
    public string UnitName => unitName;
    public UnitRarity Rarity => rarity;
    public int CurrentHP => currentHP;
    public int MaxHP => baseStats.MaxHP;
    public int Attack => baseStats.Attack;
    public int MovementRange => baseStats.MovementRange;
    public int AttackRange => baseStats.AttackRange;
    public bool IsAlive => isAlive;
    public Vector3 CurrentPosition => currentPosition;
    
    public bool IsHealthy => currentHP == baseStats.MaxHP;
    public bool IsCriticalHealth => currentHP <= baseStats.MaxHP * 0.25f;
    public float HealthPercentage => (float)currentHP / baseStats.MaxHP;
    
    public event Action<CardUnitController> OnUnitDied;
    public event Action<CardUnitController, int> OnHealthChanged;
    public event Action<CardUnitController, Vector3> OnUnitMoved;
    public event Action<CardUnitController, GameObject> OnUnitAttacked;
    
    public void Initialize(UnitStats stats, string name, UnitRarity unitRarity)
    {
        baseStats = stats;
        unitName = name;
        rarity = unitRarity;
        currentHP = stats.MaxHP;
        currentPosition = transform.position;
        isAlive = true;
        
        Debug.Log($"유닛 초기화: {unitName} (HP: {currentHP}, Attack: {Attack})");
    }
    
    public bool TakeDamage(int damage)
    {
        if (!isAlive || damage < 0) return false;
        
        int previousHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - damage);
        
        OnHealthChanged?.Invoke(this, currentHP - previousHP);
        
        if (currentHP <= 0)
        {
            Die();
        }
        
        return true;
    }
    
    public bool Heal(int healAmount)
    {
        if (!isAlive || healAmount <= 0) return false;
        
        int previousHP = currentHP;
        currentHP = Mathf.Min(baseStats.MaxHP, currentHP + healAmount);
        
        OnHealthChanged?.Invoke(this, currentHP - previousHP);
        return true;
    }
    
    public bool MoveTo(Vector3 targetPosition)
    {
        if (!isAlive) return false;
        
        float distance = Vector3.Distance(currentPosition, targetPosition);
        if (distance > baseStats.MovementRange) return false;
        
        currentPosition = targetPosition;
        transform.position = targetPosition;
        
        OnUnitMoved?.Invoke(this, targetPosition);
        return true;
    }
    
    public bool AttackTarget(GameObject target)
    {
        if (!isAlive || target == null) return false;
        
        float distance = Vector3.Distance(currentPosition, target.transform.position);
        if (distance > baseStats.AttackRange) return false;
        
        var targetUnit = target.GetComponent<CardUnitController>();
        if (targetUnit != null)
        {
            targetUnit.TakeDamage(baseStats.Attack);
        }
        
        OnUnitAttacked?.Invoke(this, target);
        return true;
    }
    
    private void Die()
    {
        if (!isAlive) return;
        
        isAlive = false;
        OnUnitDied?.Invoke(this);
        
        Debug.Log($"{unitName} 이(가) 사망했습니다.");
        
        Destroy(gameObject, 1f);
    }
    
    public void PrintStatus()
    {
        Debug.Log($"[{unitName}] HP: {currentHP}/{baseStats.MaxHP}, " +
                 $"Attack: {baseStats.Attack}, Position: {currentPosition}");
    }
}