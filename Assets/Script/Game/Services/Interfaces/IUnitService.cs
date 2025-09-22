using System;
using System.Collections.Generic;

namespace Game.Services
{
    /// <summary>
    /// Service responsible for managing unit lifecycle and processing
    /// </summary>
    public interface IUnitService
    {
        /// <summary>
        /// Gets the total number of active units
        /// </summary>
        int ActiveUnitCount { get; }
        
        /// <summary>
        /// Registers a unit with the service
        /// </summary>
        /// <param name="unit">Unit to register</param>
        void RegisterUnit(Unit unit);
        
        /// <summary>
        /// Unregisters a unit from the service
        /// </summary>
        /// <param name="unit">Unit to unregister</param>
        void UnregisterUnit(Unit unit);
        
        /// <summary>
        /// Processes units for the current player turn
        /// </summary>
        /// <param name="isPlayerTurn">Whether it's the player's turn</param>
        void ProcessUnitsForCurrentPlayer(bool isPlayerTurn);
        
        /// <summary>
        /// Processes all units regardless of owner
        /// </summary>
        void ProcessAllUnits();
        
        /// <summary>
        /// 현재 턴 페이즈에 맞춰 적절한 순서로 유닛을 처리합니다.
        /// </summary>
        /// <param name="phase">처리할 턴 페이즈</param>
        void ProcessUnitsForPhase(TurnPhase phase);
        
        /// <summary>
        /// 그리드 위치(우상단에서 좌하단)에 따라 정렬된 유닛 리스트를 가져옵니다.
        /// </summary>
        /// <param name="isPlayerUnits">플레이어 유닛 여부</param>
        /// <returns>그리드 순서로 정렬된 유닛 리스트</returns>
        List<Unit> GetUnitsInGridOrder(bool isPlayerUnits);
        
        /// <summary>
        /// Gets a list of active units, optionally filtered by owner
        /// </summary>
        /// <param name="isPlayerUnit">Filter by player ownership (null for all units)</param>
        /// <returns>List of matching units</returns>
        List<Unit> GetActiveUnits(bool? isPlayerUnit = null);
        
        /// <summary>
        /// Gets the count of units for a specific owner
        /// </summary>
        /// <param name="isPlayerUnit">Whether to count player or AI units</param>
        /// <returns>Number of units owned by the specified player type</returns>
        int GetUnitCount(bool isPlayerUnit);
        
        /// <summary>
        /// Event fired when a unit is registered
        /// </summary>
        event Action<Unit> OnUnitRegistered;
        
        /// <summary>
        /// Event fired when a unit is unregistered
        /// </summary>
        event Action<Unit> OnUnitUnregistered;
        
        /// <summary>
        /// Event fired when unit processing is completed
        /// </summary>
        event Action OnUnitsProcessed;
    }
}