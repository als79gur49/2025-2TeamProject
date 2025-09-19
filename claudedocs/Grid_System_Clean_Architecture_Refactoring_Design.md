# Grid System Clean Architecture Refactoring Design

## 🚨 Architecture Violation Analysis

**Issue Identified**: GridRenderer.HandleUnitMoved() contains unit positioning logic (lines 149-151):
```csharp
var worldPos = gridState.GridToWorldPosition(newPos);
unit.transform.position = worldPos + Vector3.up * 0.5f;
```

**Violation**: Presentation Layer (GridRenderer) is modifying business logic (unit positioning) instead of only handling visual representation.

## 🏗️ Clean Architecture 4-Layer Compliance Design

### Current Responsibility Problems
```
❌ GridRenderer (Presentation Layer)
   ├─ ✅ Visual representation
   ├─ ✅ Tile highlighting  
   └─ ❌ Unit positioning ← VIOLATION
```

### Proposed Responsibility Separation

#### 1. **GridController** (Business Logic Layer)
```yaml
responsibilities:
  unit_management:
    - Unit positioning logic
    - Movement validation
    - Position synchronization
    - Business rule enforcement
  
  new_methods:
    - MoveUnit(GameObject unit, Vector2Int from, Vector2Int to)
    - SetUnitPosition(GameObject unit, Vector2Int position)
    - ValidateUnitPlacement(Vector2Int position)
```

#### 2. **GridRenderer** (Presentation Layer)  
```yaml
responsibilities:
  visual_only:
    - Tile visual updates
    - Highlighting effects
    - Animation triggers
    - Visual feedback
  
  removed_logic:
    - Unit transform manipulation
    - Position calculations
    - Direct GameObject positioning
```

#### 3. **GridState** (Data Layer)
```yaml
responsibilities:
  data_storage:
    - Position tracking
    - State persistence
    - Event notifications
    - Coordinate conversions (read-only)
```

#### 4. **GridManager** (Unity Integration Layer)
```yaml
responsibilities:
  coordination:
    - Component orchestration
    - Service registration
    - Unity lifecycle management
```

## 🔧 Implementation Design Specification

### A. GridController Enhancement Design

#### New Interface Methods (IGridController)
```csharp
// Unit positioning responsibility
bool MoveUnit(GameObject unit, Vector2Int fromPosition, Vector2Int toPosition);
void SetUnitWorldPosition(GameObject unit, Vector2Int gridPosition);
bool ValidateAndExecuteUnitMovement(GameObject unit, Vector2Int targetPosition);

// Position calculation delegation
Vector3 CalculateUnitWorldPosition(Vector2Int gridPosition);
Vector3 GetUnitOffset(); // Returns Vector3.up * 0.5f
```

#### Implementation Strategy
```csharp
public bool MoveUnit(GameObject unit, Vector2Int fromPosition, Vector2Int toPosition)
{
    // 1. Business validation
    if (!CanMoveUnit(unit, fromPosition, toPosition))
        return false;
    
    // 2. Update data layer
    if (!gridState.TryMoveUnit(unit, fromPosition, toPosition))
        return false;
    
    // 3. Apply physical positioning (Business Logic responsibility)
    SetUnitWorldPosition(unit, toPosition);
    
    // 4. Trigger events for presentation layer
    OnUnitMoved?.Invoke(unit, fromPosition, toPosition);
    
    return true;
}

public void SetUnitWorldPosition(GameObject unit, Vector2Int gridPosition)
{
    if (unit == null) return;
    
    var worldPos = gridState.GridToWorldPosition(gridPosition);
    unit.transform.position = worldPos + GetUnitOffset();
}
```

### B. GridRenderer Refactoring Design

#### Removed Responsibilities
```csharp
// ❌ Remove from HandleUnitMoved()
private void HandleUnitMoved(GameObject unit, Vector2Int oldPos, Vector2Int newPos)
{
    // Keep: Visual updates only
    UpdateTileVisual(oldPos);
    UpdateTileVisual(newPos);
    
    // ❌ Remove: Unit positioning logic
    // if (unit != null)
    // {
    //     var worldPos = gridState.GridToWorldPosition(newPos);
    //     unit.transform.position = worldPos + Vector3.up * 0.5f;
    // }
}
```

#### Enhanced Visual-Only Responsibilities
```csharp
// ✅ Enhanced visual feedback
private void HandleUnitMoved(GameObject unit, Vector2Int oldPos, Vector2Int newPos)
{
    // Visual tile updates
    UpdateTileVisual(oldPos);
    UpdateTileVisual(newPos);
    
    // Optional: Visual effects for movement
    PlayMovementEffect(oldPos, newPos);
    
    // Optional: Animation triggers
    TriggerTileChangeAnimation(oldPos);
    TriggerTileChangeAnimation(newPos);
}
```

### C. Event Flow Redesign

#### Current Flow (Violates Clean Architecture)
```
GridState.MoveUnit() → GridRenderer.HandleUnitMoved() → unit.transform.position = ...
                  ↳ Presentation Layer handling Business Logic ❌
```

#### Proposed Flow (Clean Architecture Compliant)
```
External Request → GridController.MoveUnit() → GridState.MoveUnit() 
                                          ↳ unit.transform.position = ... ✅
                                          ↳ OnUnitMoved event
                                            ↳ GridRenderer.HandleUnitMoved() (visual only) ✅
```

## 📋 Complete Refactoring Specification

### File-Specific Changes Design

#### 1. **GridController.cs** Enhancements
```csharp
// Add to IGridController interface
public interface IGridController
{
    // Existing methods...
    
    // New unit positioning methods
    bool MoveUnit(GameObject unit, Vector2Int fromPosition, Vector2Int toPosition);
    void SetUnitWorldPosition(GameObject unit, Vector2Int gridPosition);
    Vector3 CalculateUnitWorldPosition(Vector2Int gridPosition);
    Vector3 GetUnitOffset();
    bool ValidateAndExecuteUnitMovement(GameObject unit, Vector2Int targetPosition);
}

// Implementation additions
public class GridController : IGridController
{
    // New configuration
    [SerializeField] private Vector3 unitOffset = Vector3.up * 0.5f;
    
    // Positioning logic moved from GridRenderer
    public bool MoveUnit(GameObject unit, Vector2Int fromPosition, Vector2Int toPosition)
    {
        if (unit == null || !IsValidPosition(toPosition))
            return false;
            
        // Business validation
        if (!CanMoveUnit(unit, fromPosition, toPosition))
            return false;
            
        // Update state
        if (!gridState.TryMoveUnit(unit, fromPosition, toPosition))
            return false;
            
        // Handle positioning (Business Logic responsibility)
        SetUnitWorldPosition(unit, toPosition);
        
        // Notify presentation layer
        OnUnitMoved?.Invoke(unit, fromPosition, toPosition);
        
        return true;
    }
    
    public void SetUnitWorldPosition(GameObject unit, Vector2Int gridPosition)
    {
        if (unit == null || !IsValidPosition(gridPosition))
            return;
            
        var worldPos = CalculateUnitWorldPosition(gridPosition);
        unit.transform.position = worldPos;
    }
    
    public Vector3 CalculateUnitWorldPosition(Vector2Int gridPosition)
    {
        return gridState.GridToWorldPosition(gridPosition) + GetUnitOffset();
    }
    
    public Vector3 GetUnitOffset() => unitOffset;
}
```

#### 2. **GridRenderer.cs** Refactoring
```csharp
// Modified HandleUnitMoved - Visual responsibilities only
private void HandleUnitMoved(GameObject unit, Vector2Int oldPos, Vector2Int newPos)
{
    // ✅ Keep: Visual tile updates
    UpdateTileVisual(oldPos);
    UpdateTileVisual(newPos);
    
    // ❌ Remove: Unit positioning logic
    // Business Logic Layer (GridController) now handles unit.transform.position
    
    // ✅ Optional: Enhanced visual feedback
    if (enableMovementEffects)
    {
        PlayMovementEffect(oldPos, newPos);
        TriggerTileChangeAnimation(oldPos);
        TriggerTileChangeAnimation(newPos);
    }
}

// Modified HandleUnitPlaced - Visual responsibilities only  
private void HandleUnitPlaced(Vector2Int position, GameObject unit)
{
    UpdateTileVisual(position);
    
    // ❌ Remove: Unit positioning logic
    // GridController now handles unit.transform.position during placement
    
    // ✅ Optional: Placement visual effects
    PlayPlacementEffect(position);
}

// Optional: Enhanced visual methods
private void PlayMovementEffect(Vector2Int from, Vector2Int to)
{
    // Visual effect implementation
    Debug.Log($"[GridRenderer] Playing movement effect from {from} to {to}");
}

private void TriggerTileChangeAnimation(Vector2Int position)
{
    // Tile animation implementation  
    if (TryGetTileGameObject(position, out var tile))
    {
        // Trigger animation on tile
    }
}
```

#### 3. **GridState.cs** Interface Compliance
```csharp
// Ensure GridState only handles data, not positioning
public class GridState : IGridState
{
    // ✅ Keep: Data operations only
    public bool TryMoveUnit(GameObject unit, Vector2Int from, Vector2Int to)
    {
        // Data layer validation and state update
        if (!IsValidPosition(from) || !IsValidPosition(to))
            return false;
            
        if (!IsPositionOccupied(from) || IsPositionOccupied(to))
            return false;
            
        // Update internal state
        occupiedPositions.Remove(from);
        occupiedPositions[to] = unit;
        
        // ❌ NO unit.transform positioning here
        // That's Business Logic Layer responsibility
        
        // ✅ Notify via events only
        OnUnitMoved?.Invoke(unit, from, to);
        
        return true;
    }
    
    // ✅ Keep: Read-only coordinate conversion
    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        // Pure coordinate conversion, no business logic
        return gridOrigin + new Vector3(
            gridPosition.x * tileSize,
            0,
            gridPosition.y * tileSize
        );
    }
}
```

#### 4. **GridManager.cs** Coordination Updates
```csharp
// Enhanced service registration with new interface methods
private void RegisterServices()
{
    var services = new GridServices(gridState, gridController, gridRenderer);
    
    // Register all interfaces including enhanced IGridController
    ServiceLocator.Register<IGridServices>(services);
    ServiceLocator.Register<IGridManager>(gridController);
    ServiceLocator.Register<IGridController>(gridController); // Now includes positioning methods
    ServiceLocator.Register<IReadOnlyGridState>(gridState);
    ServiceLocator.Register<IGridState>(gridState);
    ServiceLocator.Register<IGridRenderer>(gridRenderer);
    
    ServiceLocator.MarkAsInitialized();
    
    Debug.Log("[GridManager] Phase 3 Clean Architecture services registered");
}
```

## 🎯 Implementation Strategy & Migration Plan

### Phase 1: Interface Extension
1. **Extend IGridController** with unit positioning methods
2. **Add configuration** for unit offset in GridController
3. **Implement positioning logic** in GridController

### Phase 2: GridRenderer Refactoring  
1. **Remove positioning logic** from HandleUnitMoved()
2. **Remove positioning logic** from HandleUnitPlaced()
3. **Enhance visual-only capabilities** (optional effects)

### Phase 3: External API Updates
1. **Update existing calls** to use GridController.MoveUnit()
2. **Replace direct GridState.MoveUnit()** calls with GridController.MoveUnit()
3. **Validate Clean Architecture compliance**

### Dependency Flow Validation
```
External Request
    ↓
GridController.MoveUnit() ← Business Logic Layer (positioning logic)
    ↓
GridState.TryMoveUnit() ← Data Layer (state management)
    ↓
Event: OnUnitMoved
    ↓
GridRenderer.HandleUnitMoved() ← Presentation Layer (visual only)
```

### Benefits Achieved
- ✅ **Single Responsibility**: Each layer has clear, distinct responsibilities
- ✅ **Dependency Inversion**: Presentation layer no longer handles business logic
- ✅ **Open/Closed**: Easy to extend positioning logic without touching presentation
- ✅ **Interface Segregation**: Clear separation between data, business, and presentation concerns
- ✅ **Liskov Substitution**: All implementations respect their interface contracts

This design ensures that each component adheres to its architectural layer responsibilities while maintaining the clean event-driven communication pattern established in Phase 3.