using System;
using System.Collections.Generic;

namespace Game.Services
{
    public interface IUnitService
    {
        int ActiveUnitCount { get; }
        
        void RegisterUnit(Unit unit);
        void UnregisterUnit(Unit unit);
        void ProcessUnitsForCurrentPlayer(bool isPlayerTurn);
        void ProcessAllUnits();
        
        List<Unit> GetActiveUnits(bool? isPlayerUnit = null);
        int GetUnitCount(bool isPlayerUnit);
        
        event Action<Unit> OnUnitRegistered;
        event Action<Unit> OnUnitUnregistered;
        event Action OnUnitsProcessed;
    }
}