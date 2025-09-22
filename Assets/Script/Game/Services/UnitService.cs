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
        
        /// <summary>
        /// 현재 턴 페이즈에 맞춰 적절한 순서로 유닛을 처리합니다.
        /// </summary>
        public void ProcessUnitsForPhase(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.EnemySummon:
                    ProcessSummonPhase(false); // 적 소환
                    break;
                    
                case TurnPhase.AllySummon:
                    ProcessSummonPhase(true); // 아군 소환
                    break;
                    
                case TurnPhase.EnemyAction:
                    ProcessActionPhase(false); // 적 행동
                    break;
                    
                case TurnPhase.AllyAction:
                    ProcessActionPhase(true); // 아군 행동
                    break;
            }
        }
        
        /// <summary>
        /// 소환 페이즈를 처리합니다 (플레이스홀더 구현).
        /// </summary>
        private void ProcessSummonPhase(bool isPlayerUnits)
        {
            Debug.Log($"[UnitService] Processing summon phase for {(isPlayerUnits ? "Player" : "Enemy")} units");
            // TODO: 유닛 소환 로직 구현 필요
            OnUnitsProcessed?.Invoke();
        }
        
        /// <summary>
        /// 그리드 순서에 따라 행동 페이즈를 처리합니다.
        /// </summary>
        private void ProcessActionPhase(bool isPlayerUnits)
        {
            var units = GetUnitsInGridOrder(isPlayerUnits);
            
            Debug.Log($"[UnitService] Processing {units.Count} {(isPlayerUnits ? "player" : "enemy")} units in grid order");
            
            foreach (var unit in units)
            {
                ProcessUnitAction(unit);
            }
            
            OnUnitsProcessed?.Invoke();
        }
        
        /// <summary>
        /// 그리드 위치(우상단에서 좌하단)에 따라 정렬된 유닛 리스트를 가져옵니다.
        /// </summary>
        public List<Unit> GetUnitsInGridOrder(bool isPlayerUnits)
        {
            var units = GetActiveUnits(isPlayerUnits);
            
            // 그리드 순서: Y 내림차순 (상단 -> 하단), X 내림차순 (우측 -> 좌측)
            return units.OrderByDescending(unit => unit.Y)
                       .ThenByDescending(unit => unit.X)
                       .ToList();
        }
        
        /// <summary>
        /// 개별 유닛의 행동을 처리합니다.
        /// </summary>
        private void ProcessUnitAction(Unit unit)
        {
            if (unit != null && unit.IsAlive)
            {
                Debug.Log($"[UnitService] Processing action for unit {unit.name} at ({unit.X}, {unit.Y})");
                unit.OnTurnStart();
            }
        }
        
        private void CleanupDeadUnits()
        {
            allUnits.RemoveAll(u => u == null || !u.IsAlive);
        }
    }
}