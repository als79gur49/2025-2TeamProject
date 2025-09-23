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
        
        // Phase 1: Sequential Processing System - New Interface Methods
        
        /// <summary>
        /// 비동기적으로 페이즈에 맞는 유닛들을 순차 처리를 시작합니다.
        /// </summary>
        /// <param name="phase">처리할 턴 페이즈</param>
        /// <returns>처리 시작 성공 여부</returns>
        bool ProcessUnitsForPhaseAsync(TurnPhase phase);
        
        /// <summary>
        /// 현재 진행 중인 페이즈를 취소합니다.
        /// </summary>
        /// <param name="completeAllActions">true일 경우 남은 유닛들의 로직을 즉시 실행하고 종료</param>
        /// <returns>취소 성공 여부</returns>
        bool CancelCurrentPhase(bool completeAllActions = false);
        
        /// <summary>
        /// 현재 페이즈의 진행률을 가져옵니다 (0.0 ~ 1.0).
        /// </summary>
        /// <returns>진행률 (0.0 ~ 1.0)</returns>
        float GetPhaseProgress();
        
        /// <summary>
        /// 현재 페이즈가 실행 중인지 여부를 나타냅니다.
        /// </summary>
        bool IsPhaseExecuting { get; }
        
        /// <summary>
        /// 현재 실행 중인 페이즈를 나타냅니다. 실행 중이 아니면 null입니다.
        /// </summary>
        TurnPhase? CurrentPhase { get; }
        
        /// <summary>
        /// 유닛 액션 간의 시간 간격을 설정합니다.
        /// </summary>
        float UnitActionInterval { get; set; }
        
        // 강화된 이벤트 시스템
        
        /// <summary>
        /// 페이즈 시작 시 발생하는 이벤트
        /// </summary>
        event Action<TurnPhase> OnPhaseStarted;
        
        /// <summary>
        /// 페이즈 완료 시 발생하는 이벤트
        /// </summary>
        event Action<TurnPhase> OnPhaseCompleted;
        
        /// <summary>
        /// 페이즈 취소 시 발생하는 이벤트
        /// </summary>
        event Action<TurnPhase> OnPhaseCancelled;
        
        /// <summary>
        /// 개별 유닛 처리 완료 시 발생하는 이벤트
        /// </summary>
        /// <param name="unit">처리된 유닛</param>
        /// <param name="currentIndex">현재 인덱스 (1부터 시작)</param>
        /// <param name="totalCount">총 유닛 개수</param>
        event Action<Unit, int, int> OnUnitProcessed;
    }
}