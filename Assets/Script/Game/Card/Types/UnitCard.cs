using UnityEngine;
using System;

public class UnitCard : BaseCard
{
    [Header("Unit Stats")]
    [SerializeField] private int hp;
    [SerializeField] private int attack;
    [SerializeField] private int movementRange;
    [SerializeField] private int attackRange;
    [SerializeField] private UnitRarity rarity;
    
    [Header("Unit Prefab")]
    [SerializeField] private GameObject unitPrefab;
    
    public int HP => hp;
    public int Attack => attack;
    public int MovementRange => movementRange;
    public int AttackRange => attackRange;
    public UnitRarity Rarity => rarity;
    
    public static event Action<UnitCard, GameObject> OnUnitSummoned;
    
    public UnitCard(string name, string desc, int cost, int hp, int attack, int moveRange, int atkRange, UnitRarity rarity = UnitRarity.Common) 
        : base(name, desc, cost, CardType.Unit)
    {
        this.hp = hp;
        this.attack = attack;
        this.movementRange = moveRange;
        this.attackRange = atkRange;
        this.rarity = rarity;
    }
    
    public override void Use()
    {
        if (!CanUse()) return;
        
        OnCardUsed();
        SummonUnit();
    }
    
    public override bool CanUse()
    {
        return base.CanUse() && unitPrefab != null && HasValidSpawnPosition();
    }
    
    private void SummonUnit()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        GameObject spawnedUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
        
        var unitController = spawnedUnit.GetComponent<CardUnitController>();
        if (unitController != null)
        {
            var stats = new UnitStats(hp, attack, movementRange, attackRange);
            unitController.Initialize(stats, CardName, rarity);
        }
        
        OnUnitSummoned?.Invoke(this, spawnedUnit);
        Debug.Log($"유닛 소환 완료: {CardName}");
    }
    
    private bool HasValidSpawnPosition()
    {
        return true;
    }
    
    private Vector3 GetSpawnPosition()
    {
        return Vector3.zero;
    }
}