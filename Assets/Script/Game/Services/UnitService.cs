using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    public class UnitService : MonoBehaviour, IUnitService
    {
        private List<Unit> allUnits = new List<Unit>();
        
        public int ActiveUnitCount => allUnits.Count(u => u != null && u.IsAlive);
        
        public event System.Action<Unit> OnUnitRegistered;
        public event System.Action<Unit> OnUnitUnregistered;
        public event System.Action OnUnitsProcessed;
        
        private void Awake()
        {
            Debug.Log("[UnitService] Awake() called - Registration handled by GameInitializer");
        }
        
        private void Start()
        {
            // Periodic cleanup of dead units
            InvokeRepeating(nameof(CleanupDeadUnits), 1f, 2f);
        }
        
        public void RegisterUnit(Unit unit)
        {
            if (unit == null || allUnits.Contains(unit)) return;
            
            allUnits.Add(unit);
            Debug.Log($"[UnitService] Unit registered: {unit.name}");
            OnUnitRegistered?.Invoke(unit);
        }
        
        public void UnregisterUnit(Unit unit)
        {
            if (allUnits.Remove(unit))
            {
                Debug.Log($"[UnitService] Unit unregistered: {unit?.name}");
                OnUnitUnregistered?.Invoke(unit);
            }
        }
        
        public void ProcessUnitsForCurrentPlayer(bool isPlayerTurn)
        {
            string playerType = isPlayerTurn ? "Player" : "Enemy";
            Debug.Log($"[UnitService] Processing {playerType} units...");
            
            var unitsToProcess = GetActiveUnits(isPlayerTurn);
            
            foreach (Unit unit in unitsToProcess)
            {
                if (unit != null && unit.IsAlive)
                {
                    unit.OnTurnStart();
                }
            }
            
            Debug.Log($"[UnitService] {playerType} units processed: {unitsToProcess.Count}");
            OnUnitsProcessed?.Invoke();
        }
        
        public void ProcessAllUnits()
        {
            Debug.Log("[UnitService] Processing all active units...");
            
            foreach (Unit unit in allUnits)
            {
                if (unit != null && unit.IsAlive)
                {
                    unit.OnTurnStart();
                }
            }
            
            Debug.Log($"[UnitService] All units processed: {ActiveUnitCount}");
            OnUnitsProcessed?.Invoke();
        }
        
        public List<Unit> GetActiveUnits(bool? isPlayerUnit = null)
        {
            return allUnits.Where(u => u != null && u.IsAlive && 
                                      (isPlayerUnit == null || u.IsPlayerUnit == isPlayerUnit))
                          .ToList();
        }
        
        public int GetUnitCount(bool isPlayerUnit)
        {
            return allUnits.Count(u => u != null && u.IsAlive && u.IsPlayerUnit == isPlayerUnit);
        }
        
        private void CleanupDeadUnits()
        {
            allUnits.RemoveAll(u => u == null || !u.IsAlive);
        }
    }
}