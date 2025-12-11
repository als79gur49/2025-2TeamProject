# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Unity 2022.3.62f1** | **3D URP** | **Grid-Based Tactical Card Battle Game**

6x6 grid-based turn-based strategy game where players use cards to summon units (Pawn cards) or cast spells (Spell cards) to destroy the enemy base. The game follows a Enemy Turn → Player Turn → Between Phase turn structure with automatic unit actions during Between Phase.

## Unity Development

### Opening the Project
1. Install Unity 2022.3.62f1 via Unity Hub
2. Add this project directory to Unity Hub
3. Open project - Unity will import and compile scripts automatically

### Testing
- Use Unity's Play Mode (▶ button) to test in-editor
- Main test scene: `Assets/Scenes/TestScenes/Prototypes/Stage01_01.unity`
- Bootstrap scene: `Assets/Scenes/TestScenes/Prototypes/BootStrapScene.unity`
- Use Editor menu items: `Game/Initialize Services`, `Game/Validate Services`
- Unit testing via context menus on Unit components (right-click in Inspector)

### Common Tasks
- **Initialize Services**: Unity Menu → Game → Initialize Services
- **Validate Services**: Unity Menu → Game → Validate Services
- **Test Unit Systems**: Select Unit in Hierarchy → Inspector → Right-click component → Context menu options
- **Create Assets**: Right-click in Project → Create → Game/[Asset Type]
- **Debug Resources**: Unity Menu → Tools → Resource Manager (Play Mode only)

### Resource Manager Editor (Debugging)

**Access**: `Tools → Resource Manager` (Play Mode only)

**Purpose**: Real-time resource manipulation and testing for ResourceManager service

**Features**:
- **Current State**: Live display of player/enemy mana with progress bars
- **Quick Operations**:
  - +1 Mana buttons for quick adjustments
  - Custom amount input for precise control
  - Refill buttons to set resources to maximum
  - Set to Zero buttons for testing resource starvation
- **Advanced Operations**:
  - Reset All: Restore default resource values
  - Increase Turnly Mana: Simulate turn-based mana growth
- **Test Presets**:
  - Early Game (3/5, 3/5): Low resource scenario
  - Mid Game (7/10, 7/10): Standard gameplay state
  - Late Game (10/10, 10/10): Maximum resources
  - Player Advantage (10/10, 3/10): Test player-favored situations
  - Enemy Advantage (3/10, 10/10): Test enemy-favored situations
  - Resource Starvation (1/5, 1/5): Test low-resource pressure
- **Operation History**: Tracks recent resource changes with timestamps

**Usage Pattern**:
1. Enter Play Mode
2. Open `Tools → Resource Manager`
3. Apply presets or manual adjustments to test game balance
4. Observe real-time UI updates and event notifications

**Note**: All changes are immediate and trigger `OnPlayerResourcesChanged`/`OnEnemyResourcesChanged` events

## Architecture

### Service Locator Pattern (Dependency Injection)

The project uses a centralized `ServiceLocator` for all core systems:

```csharp
// Registration (in GameInitializer.cs)
ServiceLocator.Register<IGridManager>(gridManager);
ServiceLocator.Register<IGameServiceManager>(gameServiceManager);

// Retrieval (in any MonoBehaviour)
var gridManager = ServiceLocator.Get<IGridManager>();
var gameService = ServiceLocator.Get<IGameServiceManager>();
```

**Critical Services** (registered in `GameInitializer.cs`):
- `IGlobalStateManager` - Global game state tracking (highest priority)
- `IGridManager` - 6x6 grid system management
- `IGameServiceManager` - Game flow and turn management
- `ICardServiceManager` - Card spawning and effects
- `IModifierFactory` - Creates ability modifiers from data
- `IResourceManager` - Resource/mana management
- `IBaseManager` - Player/enemy base management
- `IGameOutcomeManager` - Victory/defeat conditions
- `IDeathAnimationManager` - Death animation sequencing

### Initialization Flow

```
ServiceBootstrap (Bootstrap scene - global services)
  ↓
GameInitializer.InitializeGame() (Stage scenes)
  ↓
1. ServiceLocator initialization (preserves bootstrap services)
2. Stage Context loading (StageProgressManager)
3. Core services registration
4. Component services registration
5. Service validation
6. Game session start
```

**Important**: Services are registered in dependency order. GlobalStateManager must be first, GridManager before ModifierFactory, etc.

### Component-Based Unit System

Units use a component-based architecture:

```csharp
// Core Components (auto-added if missing)
- IHealthComponent (HealthComponent)
- ICombatSystem (CombatComponent)
- IMovementSystem (MovementComponent)
- ITeamComponent (TeamComponent)
- IAnimationController (UnitAnimationController)
- IUnitAI (BasicUnitAI)
```

**Unit Initialization Pattern**:
```csharp
// External initialization (preferred)
unit.Init(unitData, gridPosition, isPlayerUnit);

// Components get data from UnitData ScriptableObject
// Modifiers applied via ModifierFactory
```

### Modifier System (Current Refactoring)

**Branch**: `feature/carddata-refactoring`

The modifier system provides unit abilities through `IActionModifier`:

```
UnitData (ScriptableObject)
  ├─ ModifierData references (e.g., MeleeModifierData, RangedModifierData)
  └─ Applied via ModifierFactory.CreateFromData()

ModifierFactory (Service)
  ├─ Creates IActionModifier instances
  ├─ Injects dependencies (IGridManager, ITargetSelector, ICombatCalculator)
  └─ Registers modifiers to Unit.actionEvaluator

Action Execution Chain:
  Unit.ExecuteAITurn()
  → ActionEvaluator.EvaluateNextAction()
  → ActionExecutorRegistry.TryExecute()
  → Modifier.Execute()
  → Unit.OnActionCompleted()
```

**Available Modifiers** (in [Assets/Script/Game/Data/Modifiers/](Assets/Script/Game/Data/Modifiers/)):
- `MeleeModifierData` - Melee attack ability
- `RangedModifierData` - Ranged attack ability
- `SniperModifierData` - Long-range attack ability
- `NormalMoveModifierData` - Standard movement
- `BoosterModifierData` - Enhanced movement

### Grid System Architecture

```
GridManager (IGridManager - Central Hub)
  ├─ GridController - Placement validation, spawn logic
  ├─ GridState - 6x6 array tracking, occupancy
  └─ GridRenderer - Visual tile rendering

Tile (GameObject Component)
  ├─ Grid coordinates (X, Y)
  ├─ OccupyingUnit reference
  └─ OccupyingBase reference
```

**Grid Operations**:
```csharp
// Always get from ServiceLocator
var gridManager = ServiceLocator.Get<IGridManager>();
var gridController = gridManager.GetGridController();
var gridState = gridManager.GetGridState();

// Common operations
Vector3 worldPos = gridManager.GridToWorldPosition(gridPos);
bool canSpawn = gridController.CanSpawnUnit(gridPos, teamType);
gridController.SpawnUnit(unitPrefab, gridPos, teamType);
```

### Event System

**ScriptableObject Event Channels** (in [Assets/Script/ScriptableObjects/](Assets/Script/ScriptableObjects/)):
```csharp
// Event channel types
VoidEventChannelSO - Simple notifications
GameEventChannelSO - Game state events
CardInfoEventChannelSO - Card UI events
SoundEventChannelSO - Audio events
DamageDisplayEventChannelSO - Damage popup events

// Usage pattern
[SerializeField] private VoidEventChannelSO onTurnEndChannel;
onTurnEndChannel.RaiseEvent(); // Raise
onTurnEndChannel.OnEventRaised += OnTurnEnded; // Subscribe
```

**Internal Events** (Service-level):
```csharp
gridManager.OnUnitMoved += OnUnitMovedInGrid;
globalStateManager.OnBusyStateChanged += OnGlobalBusyStateChanged;
healthComponent.OnDeath += OnHealthComponentDeath;
```

### Audio System

DI-based audio with separation of concerns:

```csharp
// Access via ServiceContainer
var bgm = AudioServiceContainer.Instance.GetService<IBGMAudioService>();
var effects = AudioServiceContainer.Instance.GetService<IEffectAudioService>();
var volume = AudioServiceContainer.Instance.GetService<IVolumeController>();

// Operations
bgm.PlayBGM("MainTheme", loop: true);
effects.PlayEffect("ButtonClick");
volume.SetMasterVolumeNormalized(0.8f);
```

UI buttons automatically play sounds via `ButtonSoundPlayer` component.

### UI System

Panel-based UI with coordinator pattern:

```csharp
// Panel access
var victoryPanel = UIPanelFacade.GetPanel<VictoryPanel>();
victoryPanel.Open();

// Coordinators mediate between game systems and UI
GameUICoordinator - Game events → UI panels
SettingsCoordinator - Settings panel ↔ Audio services
GameResultCoordinator - Game end → Result processing
```

## Data-Driven Design

### ScriptableObject Assets

**Core Data Types** (in [Assets/Script/ScriptableObjects/](Assets/Script/ScriptableObjects/)):

```csharp
// UnitData - Unit templates
[CreateAssetMenu(menuName = "Game/Unit Data")]
public class UnitData : ScriptableObject
{
    MaxHealth, AttackPower, MovementRange
    List<ModifierData> Modifiers
}

// CardData - Card definitions (BEING REFACTORED)
[CreateAssetMenu(menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    // Moving from CardType enum to EffectData list
    // See CardData_Refactoring_Plan_v2.md
}

// ModifierData - Ability definitions
[CreateAssetMenu(menuName = "Game/Modifiers/[Type]")]
public class ModifierData : ScriptableObject
{
    ModifierType, Priority
    Type-specific configuration
}

// StageDataSO - Level definitions
[CreateAssetMenu(menuName = "Game/Stage Data")]
public class StageDataSO : ScriptableObject
{
    StageId, DisplayName
    SceneData, DeckData
    StartingResources
}
```

### Creating New Content

**New Unit Type**:
1. Right-click in Project → Create → Game → Unit Data
2. Configure: Name, Stats, Prefab reference
3. Add Modifiers from existing ModifierData assets
4. Reference in CardData or stage configuration

**New Modifier**:
1. Create ModifierData asset (Create → Game → Modifiers → [Type])
2. Implement IActionModifier class if new type
3. Register in ModifierFactory.CreateFromData()
4. Add to UnitData.Modifiers list

**New Card Effect**:
1. Create CardData asset (Create → Game → Card Data)
2. Configure EffectType (Damage/Heal/Summon)
3. Set TargetType/TargetRange (placement validation)
4. Set AffectedType/AffectedRange (effect targeting)

## Current Refactoring (CardData System)

**Active Branch**: `feature/carddata-refactoring`
**Plan**: [CardData_Refactoring_Plan_v2.md](CardData_Refactoring_Plan_v2.md)

### Key Changes

**Before (Legacy)**:
```csharp
enum CardType { Unit, Spell }
enum SpellType { Damage, Heal, Buff, Debuff, ... }
```

**After (Target Architecture)**:
```csharp
class EffectData {
    EffectType { Damage, Heal, Summon }
    Value (damage/heal amount)
    UnitToSummon (for Summon type)
}

// Separation of concerns
TargetType + TargetRange → Placement validation
AffectedType + AffectedRange → Effect targeting
```

**Factory Pattern for Effects**:
```csharp
ICardEffect effect = CardEffectFactory.Create(effectData);
effect.Execute(targetPos, gameContext);
```

### Migration Strategy

When working with card system:
1. Check current refactoring phase in plan document
2. New code should use EffectData system
3. Legacy CardType references will be migrated incrementally
4. Both systems may coexist during transition

## Common Development Patterns

### Adding a New Service

1. Define interface in [Assets/Script/Game/Services/Interfaces/](Assets/Script/Game/Services/Interfaces/)
2. Implement in [Assets/Script/Game/Services/](Assets/Script/Game/Services/)
3. Register in [GameInitializer.cs](Assets/Script/Game/Core/GameInitializer.cs) `RegisterCoreServices()`
4. Add validation check in `ValidateServices()`

### Creating Event-Based Communication

1. Create EventChannelSO asset in [Assets/Script/ScriptableObjects/](Assets/Script/ScriptableObjects/)
2. Reference in MonoBehaviour SerializeField
3. Raise: `eventChannel.RaiseEvent(data)`
4. Subscribe: `eventChannel.OnEventRaised += Handler`
5. Cleanup: Unsubscribe in `OnDestroy()`

### Working with GridManager

```csharp
// Always get from ServiceLocator
var gridManager = ServiceLocator.Get<IGridManager>();

// Check existence before placement
if (gridManager.GetGridState().IsValidPosition(pos) &&
    !gridManager.GetGridState().IsOccupied(pos)) {
    gridManager.GetGridController().SpawnUnit(prefab, pos, team);
}

// Subscribe to events
gridManager.OnUnitMoved += (unit, oldPos, newPos) => {
    // Handle unit movement
};
```

### Death Animation Pattern

```csharp
// Units handle death through DeathAnimationManager
private IDeathAnimationManager deathAnimationManager;

// On death event
void OnHealthComponentDeath() {
    NotifyUnitServiceOfDeath(); // Immediate list cleanup
    deathAnimationManager.ProcessUnitDeath(this, duration);
    // GameObject destroyed after animation completes
}
```

## File Organization

```
Assets/Script/
├── Audio/                    # Audio service implementation
├── Editor/                   # Editor tools
├── Game/
│   ├── AI/                   # Enemy AI systems
│   ├── Card/                 # Card system (being refactored)
│   ├── Components/           # Unit components (Health, Combat, Movement)
│   ├── Controllers/          # Scene transition, flow control
│   ├── Coordinators/         # Mediator pattern implementations
│   ├── Core/                 # Core systems (ServiceLocator, GameInitializer)
│   │   └── Modifiers/        # Modifier factory and implementations
│   ├── Data/                 # Data structures
│   │   └── Modifiers/        # ModifierData ScriptableObjects
│   ├── Interfaces/           # Core interfaces
│   ├── Managers/             # High-level managers (Grid, Game, Session)
│   ├── Services/             # Service implementations
│   │   ├── Card/             # Card service
│   │   ├── Interfaces/       # Service interfaces
│   │   └── Modifiers/        # Modifier service components
│   ├── Test/                 # Integration tests
│   ├── UI/                   # Game UI elements
│   ├── Utilities/            # Helper classes
│   └── VFX/                  # Visual effects
├── ScriptableObjects/        # ScriptableObject definitions
└── UI/                       # UI system (panels, coordinators)
```

## Important Notes

### Dependency Injection Rules
- **Never** instantiate services with `new` - always use `ServiceLocator.Get<T>()`
- **Never** call `ServiceLocator.Clear()` in game code - only in `GameInitializer`
- **Always** check `ServiceLocator.IsRegistered<T>()` before critical operations
- Services registered in `GameInitializer` survive scene transitions via Bootstrap

### Grid System Rules
- **Never** modify `GridState` directly - use `GridController` methods
- **Always** update via `GridManager` to ensure event notifications
- Tile.OccupyingUnit is managed by GridController, don't set manually
- Unit.currentTile is synced via GridManager.OnUnitMoved event

### Component System Rules
- Components auto-added to Units when `autoAddMissingComponents = true`
- Components initialized before `Unit.Init()` call
- Component references cached in `Awake()`, initialized in `Init()`
- **Never** access components before `Unit.IsInitialized == true`

### Event Cleanup
- **Always** unsubscribe from events in `OnDestroy()`
- ScriptableObject events don't auto-cleanup - must manually unsubscribe
- Service events (via ServiceLocator) must be unsubscribed if service outlives subscriber

### Stage Context
- Current stage loaded via `IStageProgressManager.GetCurrentStageId()`
- StageDataSO provides: DeckData, StartingResources, SceneData
- Fallback to "chapter1_stage1" for editor testing
- GameSessionManager tracks runtime session data

## Known Issues & Quirks

- `DestroyImmediate` used in `CardHandManager` - should be refactored to `Destroy`
- Unit death notifications sent before animation completes (intentional for UnitService cleanup)
- Virtual tiles created when physical Tile GameObjects missing
- ServiceLocator preserves Bootstrap services (Scene transition controllers, Audio)
