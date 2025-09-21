# Turn System Design Specification

## 📋 Overview

A comprehensive design for transforming the current binary turn system into a 4-phase turn system that supports: **Enemy Summon → Ally Summon → Enemy Action → Ally Action** with grid-ordered unit processing.

## 🎯 Core Requirements Analysis

### Current System State
- **Turn Management**: Binary player/enemy system with simple toggle
- **Unit Processing**: Basic team-based processing without ordering
- **Service Architecture**: Well-established GameServiceManager with event aggregation
- **Grid System**: Clean architecture with positional unit tracking

### Required Changes
1. **4-Phase Turn System**: Replace binary with structured phase progression
2. **Summon Phases**: Unit summoning capability (placeholder implementation)
3. **Action Phases**: Grid-ordered unit processing (top-right to bottom-left)
4. **Backward Compatibility**: Maintain existing event system and service integration

## 🏗️ Architecture Design

### 1. Turn Phase Enumeration

```csharp
namespace Game.Services
{
    /// <summary>
    /// Defines the four phases of each complete turn cycle
    /// </summary>
    public enum TurnPhase
    {
        EnemySummon = 0,    // 적군 소환 턴
        AllySummon = 1,     // 아군 소환 턴  
        EnemyAction = 2,    // 적군 행동 턴
        AllyAction = 3      // 아군 행동 턴
    }
}
```

**Design Rationale:**
- Sequential numeric values enable easy phase advancement
- Clear naming convention matching Korean requirements
- Fixed order ensures predictable game flow

### 2. Enhanced ITurnService Interface

```csharp
namespace Game.Services
{
    public interface ITurnService
    {
        // Phase Management Properties
        TurnPhase CurrentPhase { get; }
        int TurnCount { get; }          // Complete turn cycles (every 4 phases)
        int PhaseCount { get; }         // Total phase count 
        
        // Legacy Compatibility Properties
        bool IsPlayerTurn { get; }      // True during AllySummon & AllyAction phases
        
        // Phase Control Methods
        void StartGame();               // Initialize to EnemySummon phase
        void StartCurrentPhase();       // Begin current phase processing
        void EndCurrentPhase();         // End current phase, advance to next
        
        // Phase Query Methods
        bool IsSummonPhase { get; }     // True during summon phases
        bool IsActionPhase { get; }     // True during action phases
        bool IsEnemyPhase { get; }      // True during enemy phases
        bool IsAllyPhase { get; }       // True during ally phases
        
        // Events (maintaining backward compatibility)
        event Action<bool> OnTurnChanged;           // Legacy compatibility
        event Action<int> OnTurnCountChanged;       // Turn cycle changes
        event Action<TurnPhase> OnPhaseChanged;     // New: Phase changes
        event Action<int> OnPhaseCountChanged;      // New: Phase count changes
    }
}
```

**Key Design Features:**
- **Backward Compatibility**: Existing `IsPlayerTurn` and events preserved
- **Phase Granularity**: New events for phase-specific logic
- **Query Methods**: Convenient phase type checking
- **Clear Semantics**: Distinction between turn cycles and individual phases

### 3. TurnService Implementation

```csharp
public class TurnService : MonoBehaviour, ITurnService
{
    [Header("Turn State")]
    [SerializeField] private TurnPhase currentPhase = TurnPhase.EnemySummon;
    [SerializeField] private int turnCount = 0;
    [SerializeField] private int phaseCount = 0;
    
    // Properties Implementation
    public TurnPhase CurrentPhase => currentPhase;
    public int TurnCount => turnCount;
    public int PhaseCount => phaseCount;
    
    // Legacy Compatibility
    public bool IsPlayerTurn => IsAllyPhase;
    
    // Phase Query Properties
    public bool IsSummonPhase => currentPhase == TurnPhase.EnemySummon || 
                                currentPhase == TurnPhase.AllySummon;
    public bool IsActionPhase => currentPhase == TurnPhase.EnemyAction || 
                                currentPhase == TurnPhase.AllyAction;
    public bool IsEnemyPhase => currentPhase == TurnPhase.EnemySummon || 
                               currentPhase == TurnPhase.EnemyAction;
    public bool IsAllyPhase => currentPhase == TurnPhase.AllySummon || 
                              currentPhase == TurnPhase.AllyAction;
    
    // Events
    public event Action<bool> OnTurnChanged;
    public event Action<int> OnTurnCountChanged;
    public event Action<TurnPhase> OnPhaseChanged;
    public event Action<int> OnPhaseCountChanged;
    
    public void StartGame()
    {
        currentPhase = TurnPhase.EnemySummon;
        turnCount = 0;
        phaseCount = 0;
        
        // Fire initial events
        OnPhaseChanged?.Invoke(currentPhase);
        OnPhaseCountChanged?.Invoke(phaseCount);
        OnTurnChanged?.Invoke(IsPlayerTurn);
        OnTurnCountChanged?.Invoke(turnCount);
        
        Debug.Log($"[TurnService] Game started - Phase: {currentPhase}");
    }
    
    public void StartCurrentPhase()
    {
        Debug.Log($"[TurnService] Phase {currentPhase} started");
        // Phase-specific initialization logic can be added here
    }
    
    public void EndCurrentPhase()
    {
        var previousPhase = currentPhase;
        var wasPlayerTurn = IsPlayerTurn;
        
        // Advance to next phase
        currentPhase = GetNextPhase(currentPhase);
        phaseCount++;
        
        // Increment turn count every 4 phases
        if (phaseCount % 4 == 0)
        {
            turnCount++;
            OnTurnCountChanged?.Invoke(turnCount);
        }
        
        // Fire events
        OnPhaseChanged?.Invoke(currentPhase);
        OnPhaseCountChanged?.Invoke(phaseCount);
        
        // Fire legacy turn changed event if player status changed
        if (wasPlayerTurn != IsPlayerTurn)
        {
            OnTurnChanged?.Invoke(IsPlayerTurn);
        }
        
        Debug.Log($"[TurnService] Phase changed: {previousPhase} → {currentPhase}");
    }
    
    private TurnPhase GetNextPhase(TurnPhase current)
    {
        return current switch
        {
            TurnPhase.EnemySummon => TurnPhase.AllySummon,
            TurnPhase.AllySummon => TurnPhase.EnemyAction,
            TurnPhase.EnemyAction => TurnPhase.AllyAction,
            TurnPhase.AllyAction => TurnPhase.EnemySummon,
            _ => TurnPhase.EnemySummon
        };
    }
}
```

### 4. Grid-Ordered Unit Processing System

```csharp
// Enhanced UnitService methods for phase-specific processing
public class UnitService : MonoBehaviour, IUnitService
{
    // Existing code...
    
    /// <summary>
    /// Processes units for the current turn phase with appropriate ordering
    /// </summary>
    public void ProcessUnitsForPhase(TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.EnemySummon:
                ProcessSummonPhase(false); // Enemy summon
                break;
                
            case TurnPhase.AllySummon:
                ProcessSummonPhase(true); // Ally summon
                break;
                
            case TurnPhase.EnemyAction:
                ProcessActionPhase(false); // Enemy action
                break;
                
            case TurnPhase.AllyAction:
                ProcessActionPhase(true); // Ally action
                break;
        }
    }
    
    /// <summary>
    /// Processes summon phase (placeholder implementation)
    /// </summary>
    private void ProcessSummonPhase(bool isPlayerUnits)
    {
        Debug.Log($"[UnitService] Processing summon phase for {(isPlayerUnits ? "Player" : "Enemy")} units");
        // TODO: Implement unit summoning logic when required
        OnUnitsProcessed?.Invoke();
    }
    
    /// <summary>
    /// Processes action phase with grid-ordered unit processing
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
    /// Gets units ordered by grid position (top-right to bottom-left)
    /// </summary>
    private List<Unit> GetUnitsInGridOrder(bool isPlayerUnits)
    {
        var units = GetActiveUnits(isPlayerUnits);
        
        // Grid order: Top to bottom (Y descending), then right to left (X descending)
        return units.OrderBy(unit => -unit.Y)           // Top to bottom
                   .ThenByDescending(unit => unit.X)    // Right to left
                   .ToList();
    }
    
    /// <summary>
    /// Processes individual unit action
    /// </summary>
    private void ProcessUnitAction(Unit unit)
    {
        Debug.Log($"[UnitService] Processing action for unit {unit.name} at ({unit.X}, {unit.Y})");
        // TODO: Implement unit action logic
    }
}
```

### 5. Enhanced IUnitService Interface

```csharp
namespace Game.Services
{
    public interface IUnitService
    {
        // Existing interface members...
        int ActiveUnitCount { get; }
        void RegisterUnit(Unit unit);
        void UnregisterUnit(Unit unit);
        void ProcessUnitsForCurrentPlayer(bool isPlayerTurn);
        void ProcessAllUnits();
        List<Unit> GetActiveUnits(bool? isPlayerUnit = null);
        int GetUnitCount(bool isPlayerUnit);
        
        // New phase-specific processing methods
        void ProcessUnitsForPhase(TurnPhase phase);
        List<Unit> GetUnitsInGridOrder(bool isPlayerUnits);
        
        // Existing events...
        event Action<Unit> OnUnitRegistered;
        event Action<Unit> OnUnitUnregistered;
        event Action OnUnitsProcessed;
    }
}
```

### 6. GameServiceManager Integration

```csharp
public class GameServiceManager : MonoBehaviour
{
    // Existing events...
    public event Action<bool> OnTurnChanged;
    public event Action<int> OnTurnCountChanged;
    public event Action<Unit> OnUnitRegistered;
    public event Action<Unit> OnUnitUnregistered;
    public event Action OnUnitsProcessed;
    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action OnEndTurnRequested;
    public event Action OnRestartRequested;
    public event Action OnServicesInitialized;
    public event Action<string> OnServiceError;
    
    // Add new events for phase management
    public event Action<TurnPhase> OnPhaseChanged;
    public event Action<int> OnPhaseCountChanged;
    
    // Enhanced event connection
    private void ConnectServiceEvents()
    {
        // Existing TurnService events
        turnService.OnTurnChanged += HandleTurnChanged;
        turnService.OnTurnCountChanged += HandleTurnCountChanged;
        
        // New phase events
        turnService.OnPhaseChanged += HandlePhaseChanged;
        turnService.OnPhaseCountChanged += HandlePhaseCountChanged;
        
        // Existing UnitService events
        unitService.OnUnitRegistered += HandleUnitRegistered;
        unitService.OnUnitUnregistered += HandleUnitUnregistered;
        unitService.OnUnitsProcessed += HandleUnitsProcessed;
        
        // Existing GameService events
        gameService.OnGameStarted += HandleGameStarted;
        gameService.OnGameEnded += HandleGameEnded;
        
        // Existing UIService events
        uiService.OnEndTurnRequested += HandleEndTurnRequested;
        uiService.OnRestartRequested += HandleRestartRequested;
        
        areEventsConnected = true;
        LogEvent("🔗 All service events connected (including new phase events)");
    }
    
    // New event handlers
    private void HandlePhaseChanged(TurnPhase phase)
    {
        LogEvent($"🔄 Phase changed: {phase}");
        OnPhaseChanged?.Invoke(phase);
        
        // Trigger phase-specific unit processing
        if (unitService != null)
        {
            unitService.ProcessUnitsForPhase(phase);
        }
    }
    
    private void HandlePhaseCountChanged(int phaseCount)
    {
        LogEvent($"📊 Phase count changed: {phaseCount}");
        OnPhaseCountChanged?.Invoke(phaseCount);
    }
    
    // Enhanced event cleanup
    private void DisconnectServiceEvents()
    {
        if (areEventsConnected)
        {
            // Existing TurnService events
            if (turnService != null)
            {
                turnService.OnTurnChanged -= HandleTurnChanged;
                turnService.OnTurnCountChanged -= HandleTurnCountChanged;
                
                // New phase events cleanup
                turnService.OnPhaseChanged -= HandlePhaseChanged;
                turnService.OnPhaseCountChanged -= HandlePhaseCountChanged;
            }
            
            // Rest of existing cleanup code...
            
            areEventsConnected = false;
            LogEvent("🔗 All service events disconnected (including phase events)");
        }
    }
}
```

## 📝 Implementation Plan

### Phase 1: Core Infrastructure
1. **Create TurnPhase Enum** - Define phase enumeration in Game.Services namespace
2. **Update ITurnService Interface** - Add phase management properties and events
3. **Implement Enhanced TurnService** - Core 4-phase logic with backward compatibility

### Phase 2: Unit Processing Enhancement
1. **Extend IUnitService Interface** - Add phase-specific processing methods
2. **Implement Grid-Ordered Processing** - Top-right to bottom-left unit ordering
3. **Add Phase-Specific Logic** - Separate summon and action phase handling

### Phase 3: Service Integration
1. **Update GameServiceManager** - Add new phase events and handlers
2. **Enhance Event System** - Phase-specific event aggregation and logging
3. **Maintain Backward Compatibility** - Ensure existing services continue working

### Phase 4: Testing and Validation
1. **Unit Tests** - Phase transition logic and grid ordering
2. **Integration Tests** - Service coordination and event flow
3. **Compatibility Tests** - Verify existing functionality unchanged

## 🔄 Turn Flow Diagram

```
Game Start
    ↓
EnemySummon Phase
    ↓ (EndCurrentPhase)
AllySummon Phase  
    ↓ (EndCurrentPhase)
EnemyAction Phase → Process Enemy Units (Grid Order: Top-Right to Bottom-Left)
    ↓ (EndCurrentPhase)
AllyAction Phase → Process Ally Units (Grid Order: Top-Right to Bottom-Left)
    ↓ (EndCurrentPhase, TurnCount++)
EnemySummon Phase (Next Cycle)
    ↓
...
```

## 🎯 Key Benefits

### ✅ Architectural Improvements
- **Structured Turn Flow**: Clear 4-phase progression replaces binary system
- **Grid-Based Ordering**: Predictable unit action sequence (우상단부터 좌하단까지)
- **Enhanced Events**: Granular phase-specific notifications
- **Backward Compatibility**: No breaking changes to existing code

### 🔍 Implementation Features
- **Type Safety**: Enum-based phase management prevents invalid states
- **Performance**: Efficient grid ordering algorithm O(n log n)
- **Maintainability**: Clear separation of summon vs action logic
- **Extensibility**: Easy addition of new phases or processing rules

### 🚀 User Experience
- **Predictable Gameplay**: Consistent turn order and unit processing
- **Strategic Depth**: Phase-specific mechanics enable tactical planning
- **Visual Clarity**: Clear indication of current phase and team turn

## 📊 Technical Specifications

### Performance Characteristics
- **Phase Transition**: O(1) - Simple enum increment
- **Grid Ordering**: O(n log n) - Efficient sorting algorithm
- **Event Overhead**: Minimal - Same pattern as existing system

### Memory Impact
- **Additional State**: ~16 bytes per TurnService (enum + counters)
- **Event System**: No significant overhead beyond existing events
- **Unit Processing**: Temporary collections during processing only

### Compatibility Matrix
- **Unity Version**: Compatible with existing project requirements
- **Existing Services**: 100% backward compatible
- **Game Logic**: No changes required to external game systems
- **UI Systems**: Enhanced with new phase information

## 🧪 Testing Strategy

### Unit Tests
```csharp
[Test]
public void TurnService_EndCurrentPhase_ShouldAdvancePhaseCorrectly()
{
    // Given
    var turnService = CreateTurnService();
    turnService.StartGame(); // Starts at EnemySummon
    
    // When
    turnService.EndCurrentPhase();
    
    // Then
    Assert.AreEqual(TurnPhase.AllySummon, turnService.CurrentPhase);
    Assert.AreEqual(1, turnService.PhaseCount);
}

[Test]
public void TurnService_CompleteFourPhases_ShouldIncrementTurnCount()
{
    // Given
    var turnService = CreateTurnService();
    turnService.StartGame();
    
    // When - Complete one full cycle
    turnService.EndCurrentPhase(); // AllySummon
    turnService.EndCurrentPhase(); // EnemyAction
    turnService.EndCurrentPhase(); // AllyAction
    turnService.EndCurrentPhase(); // Back to EnemySummon
    
    // Then
    Assert.AreEqual(TurnPhase.EnemySummon, turnService.CurrentPhase);
    Assert.AreEqual(1, turnService.TurnCount);
    Assert.AreEqual(4, turnService.PhaseCount);
}

[Test]
public void UnitService_GetUnitsInGridOrder_ShouldReturnTopRightToBottomLeft()
{
    // Given
    var unitService = CreateUnitService();
    var units = CreateTestUnits(); // Units at various grid positions
    
    // When
    var orderedUnits = unitService.GetUnitsInGridOrder(true);
    
    // Then
    // Verify order: Top-right to bottom-left
    for (int i = 0; i < orderedUnits.Count - 1; i++)
    {
        var current = orderedUnits[i];
        var next = orderedUnits[i + 1];
        
        // Should be ordered by Y descending, then X descending
        Assert.IsTrue(current.Y >= next.Y);
        if (current.Y == next.Y)
        {
            Assert.IsTrue(current.X >= next.X);
        }
    }
}
```

### Integration Tests
```csharp
[Test]
public void GameServiceManager_PhaseTransition_ShouldTriggerUnitProcessing()
{
    // Given
    var manager = CreateGameServiceManager();
    var phaseChangedFired = false;
    var unitsProcessedFired = false;
    
    manager.OnPhaseChanged += (phase) => phaseChangedFired = true;
    manager.OnUnitsProcessed += () => unitsProcessedFired = true;
    
    // When
    manager.GetComponent<TurnService>().EndCurrentPhase();
    
    // Then
    Assert.IsTrue(phaseChangedFired);
    Assert.IsTrue(unitsProcessedFired);
}
```

## 📋 Migration Checklist

### Pre-Implementation
- [ ] Review existing TurnService usage across codebase
- [ ] Identify any hardcoded references to `IsPlayerTurn`
- [ ] Document current turn-based game logic dependencies

### Implementation Steps
- [ ] Create `TurnPhase` enum in Game.Services namespace
- [ ] Update `ITurnService` interface with new methods and properties
- [ ] Implement enhanced `TurnService` with 4-phase logic
- [ ] Add phase-specific methods to `IUnitService` interface
- [ ] Implement grid-ordered unit processing in `UnitService`
- [ ] Update `GameServiceManager` with new phase events
- [ ] Add comprehensive unit tests for all new functionality
- [ ] Run integration tests to verify service coordination
- [ ] Test backward compatibility with existing game logic

### Post-Implementation
- [ ] Update documentation and code comments
- [ ] Performance testing with various unit counts
- [ ] User acceptance testing with 4-phase gameplay
- [ ] Monitor for any regression issues

## 📚 Additional Considerations

### Future Enhancements
- **Dynamic Phase Duration**: Variable time limits per phase
- **Phase-Specific UI**: Different interfaces for summon vs action phases
- **Advanced Grid Processing**: Priority-based unit ordering within grid positions
- **Phase Interruption**: Ability to pause/resume phases for special events

### Optimization Opportunities
- **Unit Caching**: Cache grid-ordered unit lists when positions don't change
- **Event Batching**: Batch multiple unit actions for performance
- **Conditional Processing**: Skip empty phases (no units to summon/act)

### Error Handling
- **Invalid Phase Transitions**: Safeguards against corruption
- **Missing Unit Data**: Graceful handling of units without grid positions
- **Service Dependency**: Robust handling when services are unavailable

This design specification provides a comprehensive blueprint for implementing the 4-phase turn system while maintaining the robust architecture already established in the GameServiceManager and related services.