# Scene Management Global Singleton Architecture

**Document Version**: 1.0
**Last Updated**: 2025-10-21
**Status**: Implemented

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture Diagram](#architecture-diagram)
3. [Service Lifecycle](#service-lifecycle)
4. [Dependency Graph](#dependency-graph)
5. [Implementation Details](#implementation-details)
6. [Unity Setup Guide](#unity-setup-guide)
7. [Testing Procedures](#testing-procedures)
8. [Troubleshooting Guide](#troubleshooting-guide)
9. [Best Practices](#best-practices)

---

## Overview

### Problem Statement

Before this refactoring, the scene management system suffered from a critical lifecycle mismatch:

```
❌ BEFORE: Broken Architecture
├─ SceneTransitionController: Global Singleton (DontDestroyOnLoad) ✓
├─ SceneLoaderService: Scene-Local ❌
└─ Result: MissingReferenceException on scene transitions
```

**Root Cause**: A global singleton (`SceneTransitionController`) depended on a scene-local service (`SceneLoaderService`). When scenes transitioned, the service was destroyed while the controller maintained a reference to it, causing `MissingReferenceException`.

### Solution Architecture

```
✅ AFTER: Correct Architecture
├─ ServiceBootstrap: Global Singleton (creates all services)
│   ├─ SceneLoaderService: Global Singleton (DontDestroyOnLoad) ✓
│   └─ SceneTransitionController: Global Singleton (DontDestroyOnLoad) ✓
└─ Result: All services persist across scene transitions
```

**Key Insight**: Both services now use `RegisterSingleton()` with automatic `ServiceCleanup` management. Global singletons are protected by `DontDestroyOnLoad`, so `ServiceCleanup.OnDestroy()` only executes on game exit, ensuring proper cleanup without premature service destruction.

---

## Architecture Diagram

### System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    BOOTSTRAP SCENE (Index 0)                │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ServiceBootstrap (DontDestroyOnLoad)                 │   │
│  │  Script Execution Order: -100 (runs first)           │   │
│  │                                                       │   │
│  │  Creates & Registers:                                │   │
│  │   1. SceneLoaderService (Global Singleton)           │   │
│  │      - DontDestroyOnLoad in Awake()                  │   │
│  │      - RegisterSingleton + ServiceCleanup            │   │
│  │                                                       │   │
│  │   2. SceneTransitionController (Global Singleton)    │   │
│  │      - DontDestroyOnLoad in Awake()                  │   │
│  │      - RegisterSingleton + ServiceCleanup            │   │
│  │      - Depends on ISceneLoaderService                │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           ↓ Auto-loads MainMenu
┌─────────────────────────────────────────────────────────────┐
│                    MAIN MENU SCENE                          │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ MainMenuPanel (Scene-Local UI Component)             │   │
│  │  - Start() → ServiceLocator.Get<ISceneTransition>()  │   │
│  │  - OnGameStart() → Calls TransitionToScene()         │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           ↓ Transition to Stage
┌─────────────────────────────────────────────────────────────┐
│ GLOBAL SINGLETONS (Persist Across All Scene Changes)       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ SceneTransitionController                            │   │
│  │  ↓ depends on                                        │   │
│  │ SceneLoaderService                                   │   │
│  │  ↓ uses                                              │   │
│  │ Unity SceneManager                                   │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Lifecycle Comparison

```
┌──────────────────────────────────────────────────────────────┐
│ Global Singleton Lifecycle (DontDestroyOnLoad)              │
├──────────────────────────────────────────────────────────────┤
│ 1. Game Start → Awake() → DontDestroyOnLoad()               │
│ 2. Scene Transition 1 → GameObject NOT destroyed            │
│ 3. Scene Transition 2 → GameObject NOT destroyed            │
│ 4. Scene Transition N → GameObject NOT destroyed            │
│ 5. Game Exit → OnDestroy() → ServiceCleanup.Unregister()    │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Scene-Local Component Lifecycle                             │
├──────────────────────────────────────────────────────────────┤
│ 1. Scene Load → Awake() → Start()                           │
│ 2. Scene Transition → OnDestroy() → Component destroyed     │
│ 3. New Scene Load → New instance created (if present)       │
└──────────────────────────────────────────────────────────────┘
```

---

## Service Lifecycle

### Service Registration Flow

```csharp
// ServiceBootstrap.Awake() - Execution Order: -100

1. InitializeSceneLoaderService()
   ├─ Create GameObject "SceneLoaderService"
   ├─ AddComponent<SceneLoaderService>()
   │   └─ SceneLoaderService.Awake()
   │       ├─ Singleton check (destroy duplicates)
   │       └─ DontDestroyOnLoad(gameObject)
   │
   └─ ServiceLocator.RegisterSingleton<ISceneLoaderService, SceneLoaderService>()
       ├─ Register in ServiceLocator dictionary
       └─ Attach ServiceCleanup component
           └─ RegisterType(typeof(ISceneLoaderService))

2. InitializeSceneTransitionController()
   ├─ Validate dependency: ServiceLocator.IsRegistered<ISceneLoaderService>()
   ├─ Instantiate prefab → SceneTransitionController GameObject
   ├─ SceneTransitionController.Awake()
   │   ├─ Singleton check (destroy duplicates)
   │   ├─ DontDestroyOnLoad(gameObject)
   │   └─ Self-registers via RegisterSingleton (in this implementation)
   │
   └─ SceneTransitionController.Start()
       └─ Resolve ISceneLoaderService from ServiceLocator
```

### ServiceCleanup Behavior

```csharp
// ServiceCleanup.cs (attached by RegisterSingleton)

public class ServiceCleanup : MonoBehaviour
{
    private List<Type> registeredTypes; // e.g., [typeof(ISceneLoaderService)]

    // ✅ For Global Singletons with DontDestroyOnLoad:
    // This OnDestroy() ONLY executes on GAME EXIT
    private void OnDestroy()
    {
        foreach (var type in registeredTypes)
        {
            ServiceLocator.Unregister(type); // Safe cleanup on game exit
        }
    }
}
```

**Why This Works**:
- Global singletons have `DontDestroyOnLoad`, so their GameObjects are **never destroyed during scene transitions**
- `ServiceCleanup.OnDestroy()` is **only called when the game exits**
- At game exit, services are properly unregistered from ServiceLocator (clean shutdown)
- Scene-local singletons (future additions) will automatically unregister on scene transition

---

## Dependency Graph

### Service Dependencies

```
ServiceBootstrap (Script Execution Order: -100)
│
├─ Phase 1: Foundational Services (No Dependencies)
│   └─ SceneLoaderService
│       ├─ Interface: ISceneLoaderService
│       ├─ Dependencies: None
│       └─ Lifecycle: DontDestroyOnLoad
│
└─ Phase 2: Controllers (Depend on Phase 1)
    └─ SceneTransitionController
        ├─ Interface: ISceneTransitionController
        ├─ Dependencies: ISceneLoaderService ⚠️ CRITICAL
        └─ Lifecycle: DontDestroyOnLoad
```

**Dependency Rules**:
1. Services with **no dependencies** initialize first (Phase 1)
2. Services with **dependencies** initialize after their dependencies are registered (Phase 2+)
3. ServiceBootstrap validates dependencies before initialization (`IsRegistered<T>()`)
4. Fail-fast: Missing dependencies log errors and abort initialization

---

## Implementation Details

### Core Components

#### 1. IGlobalService Interface

**Location**: `Assets/Script/Game/Core/IGlobalService.cs`

```csharp
namespace Game.Core
{
    public interface IGlobalService
    {
        bool IsValid(); // Health check for debugging
    }
}
```

**Purpose**: Marker interface for services that must persist across scene transitions. Enables runtime validation via `IsValid()` method.

---

#### 2. SceneLoaderService (Global Singleton)

**Location**: `Assets/Script/Game/Services/SceneLoaderService.cs`

**Key Changes**:
```csharp
public class SceneLoaderService : MonoBehaviour, ISceneLoaderService, IGlobalService
{
    private static SceneLoaderService instance;

    private void Awake()
    {
        // Singleton enforcement
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // ✅ CRITICAL: Persist across scenes
    }

    public bool IsValid()
    {
        return instance == this && gameObject != null;
    }
}
```

**Lifecycle**: Created in BootstrapScene, persists entire game session, destroyed only on game exit.

---

#### 3. SceneTransitionController (Global Singleton)

**Location**: `Assets/Script/Game/Controllers/SceneTransitionController.cs`

**Key Changes**:
```csharp
public class SceneTransitionController : MonoBehaviour, ISceneTransitionController, IGlobalService
{
    private static SceneTransitionController instance;
    private ISceneLoaderService sceneLoaderService;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // ✅ CRITICAL: Persist across scenes
        RegisterToServiceLocator(); // Self-registration
    }

    private void Start()
    {
        // ⚠️ CRITICAL: Resolve dependency after ServiceBootstrap completes
        sceneLoaderService = ServiceLocator.Get<ISceneLoaderService>();
    }

    public bool IsValid()
    {
        return instance == this
            && gameObject != null
            && loadingScreenPrefab != null
            && sceneLoaderService != null;
    }
}
```

**Dependencies**: Requires `ISceneLoaderService` to be registered first.

---

#### 4. ServiceBootstrap (Initialization Manager)

**Location**: `Assets/Script/Game/Core/ServiceBootstrap.cs`

**Responsibilities**:
1. Create service GameObjects (from prefabs or dynamically)
2. Register services with `ServiceLocator.RegisterSingleton()`
3. Enforce initialization order (dependencies first)
4. Validate all services are in valid state
5. Provide context menu validation for debugging

**Key Methods**:
```csharp
private void Awake()
{
    // Script Execution Order: -100 ensures this runs before any scene code
    if (isInitialized) { Destroy(gameObject); return; }

    isInitialized = true;
    DontDestroyOnLoad(gameObject);

    InitializeServices(); // Phase 1 → Phase 2 → ...
}

[ContextMenu("Validate Services")]
public void ValidateServices()
{
    // Right-click in Inspector → "Validate Services"
    // Checks: IsRegistered → Get → IGlobalService.IsValid()
}
```

---

### ServiceLocator Integration

**Location**: `Assets/Script/Game/Core/ServiceLocator.cs`

**RegisterSingleton Behavior**:
```csharp
public static void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
    where TImplementation : MonoBehaviour, TInterface
    where TInterface : class
{
    // 1. Register in dictionary
    services[typeof(TInterface)] = instance;

    // 2. Attach ServiceCleanup component
    var cleanup = instance.gameObject.AddComponent<ServiceCleanup>();
    cleanup.RegisterType(typeof(TInterface));

    // 3. For global singletons: ServiceCleanup.OnDestroy() only runs on game exit
}
```

**Why RegisterSingleton for Global Singletons?**
- **Consistent Pattern**: All singletons use the same registration method
- **Automatic Cleanup**: ServiceCleanup handles unregistration (game exit only for global singletons)
- **No Manual Management**: Developers don't need to remember to call `Unregister()`
- **Future-Proof**: If scene-local singletons are added later, they use the same pattern but without `DontDestroyOnLoad`

---

## Unity Setup Guide

### Step 1: Create Bootstrap Scene

1. **Create New Scene**:
   - File → New Scene
   - Save as: `Assets/Scenes/BootstrapScene.unity`

2. **Add ServiceBootstrap GameObject**:
   - Hierarchy → Right-click → Create Empty
   - Name: "ServiceBootstrap"
   - Add Component → `ServiceBootstrap` script

3. **Configure ServiceBootstrap Inspector**:
   ```
   Service Bootstrap (Script)
   ├─ Service Prefab References
   │   ├─ Scene Loader Service Prefab: [None] (will auto-create)
   │   └─ Scene Transition Controller Prefab: [Drag prefab here] ⚠️ REQUIRED
   └─ Debug Options
       ├─ Enable Debug Logs: ✓ (recommended during setup)
       └─ Validate On Start: ✓ (recommended)
   ```

---

### Step 2: Create SceneTransitionController Prefab

1. **Create Prefab Structure**:
   ```
   SceneTransitionController (GameObject)
   ├─ SceneTransitionController (Component)
   │   └─ Loading Screen Prefab: [Assign LoadingScreenPanel prefab]
   └─ [Optional: Add LoadingScreenPanel as child for reference]
   ```

2. **Save as Prefab**:
   - Drag GameObject to `Assets/Prefab/Core/SceneTransitionController.prefab`
   - Delete from Hierarchy (will be instantiated by ServiceBootstrap)

3. **Verify Prefab**:
   - Select prefab in Project window
   - Ensure `SceneTransitionController` component is present
   - Ensure `Loading Screen Prefab` field is assigned

---

### Step 3: Configure Script Execution Order

1. **Open Settings**:
   - Edit → Project Settings → Script Execution Order

2. **Add ServiceBootstrap**:
   - Click "+" button
   - Select `ServiceBootstrap` script
   - Set Execution Time: **-100** ⚠️ CRITICAL

3. **Verify**:
   ```
   Script Execution Order:
   ├─ ServiceBootstrap: -100 ⚠️ (must run first)
   └─ Default Time: 0 (all other scripts)
   ```

---

### Step 4: Update Build Settings

1. **Open Build Settings**:
   - File → Build Settings

2. **Configure Scene Order**:
   ```
   Scenes In Build:
   [0] BootstrapScene ⚠️ MUST BE INDEX 0
   [1] MainMenuScene
   [2] StageScene
   [...] Other scenes
   ```

3. **Verify All Scenes Enabled**:
   - Check that all scenes have checkmarks (enabled)

---

### Step 5: Create SceneData Assets

#### MainMenuSceneData

1. **Create Asset**:
   - Right-click in `Assets/SO/SceneData/`
   - Create → Game → Scene Management → Scene Data
   - Name: "MainMenuSceneData"

2. **Configure**:
   - Scene Name: "MainMenuScene" (exact match with Build Settings)
   - Category: MainMenu
   - Description: "Main menu scene with UI panels for game start, settings, and exit"

#### StageSceneData

1. **Create Asset**:
   - Right-click in `Assets/SO/SceneData/`
   - Create → Game → Scene Management → Scene Data
   - Name: "StageSceneData"

2. **Configure**:
   - Scene Name: "StageScene" (exact match with Build Settings)
   - Category: Gameplay
   - Description: "Main gameplay stage scene"
   - Loading Tips: [Add gameplay tips]

---

### Step 6: Assign SceneData to MainMenuPanel

1. **Open MainMenuScene**
2. **Select MainMenuPanel GameObject in Hierarchy**
3. **Inspector → MainMenuPanel Component**:
   ```
   Main Menu Panel (Script)
   ├─ UI References
   │   ├─ Game Start Button: [Assigned]
   │   ├─ Settings Button: [Assigned]
   │   └─ Exit Button: [Assigned]
   └─ Scene References
       └─ Stage Scene Data: [Drag StageSceneData.asset here] ⚠️ REQUIRED
   ```

---

## Testing Procedures

### Test 1: Bootstrap Initialization

**Objective**: Verify services initialize correctly on game start

**Steps**:
1. Open BootstrapScene in Unity Editor
2. Press Play
3. Check Console for initialization log sequence

**Expected Output**:
```
[ServiceBootstrap] === ServiceBootstrap: Starting Service Initialization ===
[ServiceBootstrap] [1/2] Initializing SceneLoaderService...
[SceneLoaderService] Initialized as global singleton
[ServiceLocator] Registered ISceneLoaderService
[ServiceBootstrap]   ✓ SceneLoaderService created and registered
[ServiceBootstrap] [2/2] Initializing SceneTransitionController...
[SceneTransitionController] Initialized as Global Singleton Service with DontDestroyOnLoad
[ServiceLocator] Registered ISceneTransitionController
[ServiceBootstrap]   ✓ SceneTransitionController created and registered
[ServiceBootstrap] === ServiceBootstrap: Service Initialization Complete ===
[ServiceBootstrap] === Service Validation ===
[ServiceBootstrap]   ✓ SceneLoaderService: Valid
[ServiceBootstrap]   ✓ SceneTransitionController: Valid
[ServiceBootstrap] ✓ All services validated successfully
```

**Failure Indicators**:
- ❌ "Duplicate instance detected" → ServiceBootstrap exists in multiple scenes
- ❌ "Dependency check failed" → Service initialization order is wrong
- ❌ "Prefab reference is missing" → SceneTransitionController prefab not assigned

---

### Test 2: Scene Transition Flow

**Objective**: Verify scene transitions work without errors

**Steps**:
1. Start game from BootstrapScene
2. Wait for MainMenu to load
3. Click "Game Start" button
4. Observe loading screen transition
5. Verify Stage scene loads
6. Check Console for any errors

**Expected Behavior**:
- Loading screen fades in smoothly
- Progress bar updates (if implemented)
- Stage scene loads successfully
- Loading screen fades out
- **NO `MissingReferenceException` errors**

**Console Output**:
```
[MainMenuPanel] Game Start button clicked
[SceneTransitionController] Starting scene transition to: StageScene
[SceneLoaderService] Starting async load: StageScene
[SceneLoaderService] Scene ready and wait time met. Triggering FadeOut...
[SceneLoaderService] Scene loaded successfully: StageScene
[SceneTransitionController] Scene load completed: StageScene
```

---

### Test 3: Service Persistence Validation

**Objective**: Verify services persist across multiple scene transitions

**Steps**:
1. Start game from BootstrapScene
2. Transition: MainMenu → Stage → MainMenu (requires "Return to Menu" button)
3. Check Hierarchy during each scene
4. Verify services remain present

**Expected Hierarchy** (During Stage Scene):
```
DontDestroyOnLoad
├─ ServiceBootstrap
├─ SceneLoaderService
└─ SceneTransitionController
```

**Validation**:
- Services appear in `DontDestroyOnLoad` section in Hierarchy
- Services do NOT appear in current scene's Hierarchy
- Instance references remain valid across transitions

---

### Test 4: Service Validation (Context Menu)

**Objective**: Use built-in validation to check service health

**Steps**:
1. While game is running (Play Mode)
2. Find `ServiceBootstrap` GameObject in Hierarchy (under DontDestroyOnLoad)
3. Select `ServiceBootstrap` component in Inspector
4. Right-click on component header → "Validate Services"
5. Check Console output

**Expected Output**:
```
[ServiceBootstrap] === Service Validation ===
[ServiceBootstrap]   ✓ SceneLoaderService: Valid
[ServiceBootstrap]   ✓ SceneTransitionController: Valid
[ServiceBootstrap] ✓ All services validated successfully
```

**Failure Indicators**:
- ❌ "Not registered in ServiceLocator" → Service initialization failed
- ❌ "Registered but returns null" → Service was destroyed or reference broken
- ❌ "IsValid() returned false" → Service internal state is corrupted

---

## Troubleshooting Guide

### Issue 1: "Duplicate instance detected"

**Symptoms**:
```
[SceneLoaderService] Duplicate instance detected on SceneLoaderService. Destroying...
```

**Causes**:
1. SceneLoaderService exists in both BootstrapScene AND another scene
2. ServiceBootstrap is creating the service, but it already exists in the scene

**Solutions**:
- Remove any SceneLoaderService GameObjects from non-Bootstrap scenes
- Verify only ServiceBootstrap creates the service
- Check for manual instantiation in other scripts

---

### Issue 2: `MissingReferenceException` on Scene Transition

**Symptoms**:
```
MissingReferenceException: The object of type 'SceneLoaderService' has been destroyed
```

**Causes**:
1. SceneLoaderService is missing `DontDestroyOnLoad(gameObject)` in Awake
2. Service is being destroyed manually somewhere
3. Service is not using the global singleton pattern

**Solutions**:
- Verify `DontDestroyOnLoad` is called in `SceneLoaderService.Awake()`
- Search for `Destroy(sceneLoaderService)` calls in codebase
- Ensure SceneLoaderService implements singleton pattern correctly

---

### Issue 3: "Dependency check failed: ISceneLoaderService not registered"

**Symptoms**:
```
[ServiceBootstrap] ✗ Dependency check failed: ISceneLoaderService not registered!
[ServiceBootstrap] → Cannot initialize SceneTransitionController without SceneLoaderService
```

**Causes**:
1. Initialization order is incorrect (Controller before Service)
2. SceneLoaderService initialization failed silently
3. ServiceLocator registration failed

**Solutions**:
- Verify `InitializeSceneLoaderService()` is called before `InitializeSceneTransitionController()`
- Check Console for SceneLoaderService initialization errors
- Confirm ServiceLocator.RegisterSingleton is working correctly

---

### Issue 4: "Script Execution Order not set"

**Symptoms**:
- Services initialize AFTER scene UI components
- MainMenuPanel gets null when calling `ServiceLocator.Get<>()`

**Causes**:
- ServiceBootstrap Script Execution Order is not set to -100

**Solutions**:
1. Edit → Project Settings → Script Execution Order
2. Add ServiceBootstrap script
3. Set to -100
4. Click "Apply"

---

### Issue 5: "Scene not in Build Settings"

**Symptoms**:
```
[SceneLoaderService] Cannot load scene: Scene 'StageScene' is not in Build Settings or is disabled
```

**Causes**:
1. Scene is not added to Build Settings
2. Scene is added but disabled (unchecked)
3. Scene name in SceneData doesn't match Build Settings

**Solutions**:
- File → Build Settings → Add scene to list
- Verify scene checkbox is enabled
- Check SceneData.sceneName matches exactly (case-sensitive)

---

## Best Practices

### 1. Service Registration Order

**Always register dependencies first**:
```csharp
// ✅ CORRECT ORDER
InitializeSceneLoaderService();      // No dependencies
InitializeSceneTransitionController(); // Depends on SceneLoaderService

// ❌ WRONG ORDER
InitializeSceneTransitionController(); // Will fail: dependency not found
InitializeSceneLoaderService();
```

### 2. Singleton Pattern Enforcement

**Always check for duplicates in Awake**:
```csharp
private void Awake()
{
    // ✅ CRITICAL: Prevent duplicate singletons
    if (instance != null && instance != this)
    {
        Destroy(gameObject);
        return; // Stop initialization
    }

    instance = this;
    DontDestroyOnLoad(gameObject);
}
```

### 3. Service Validation

**Use IGlobalService.IsValid() for health checks**:
```csharp
public bool IsValid()
{
    // Check all critical references
    bool isValid = instance == this
        && gameObject != null
        && dependency1 != null
        && dependency2 != null;

    if (!isValid)
    {
        // Log specific failure reason
        Debug.LogError($"Validation failed: ...");
    }

    return isValid;
}
```

### 4. ServiceBootstrap Configuration

**Best practices for ServiceBootstrap setup**:
- Leave `sceneLoaderServicePrefab` null (auto-create is fine)
- Always assign `sceneTransitionControllerPrefab` (requires LoadingScreenPanel reference)
- Enable `enableDebugLogs` during development
- Enable `validateOnStart` to catch issues early

### 5. Scene Data Assets

**SceneData asset guidelines**:
- Scene Name MUST match Build Settings exactly (case-sensitive)
- Always add scenes to Build Settings BEFORE creating SceneData
- Use descriptive asset names: `[SceneName]SceneData.asset`
- Add loading tips for better user experience

### 6. Error Handling

**Fail-fast principle for critical services**:
```csharp
if (sceneLoaderService == null)
{
    Debug.LogError("[Critical] SceneLoaderService not found!");
    return; // Don't continue with broken state
}
```

### 7. Testing Strategy

**Test scene transitions in all directions**:
- Bootstrap → MainMenu → Stage
- Stage → MainMenu (if supported)
- MainMenu → Settings → MainMenu (if applicable)
- Verify services persist in Hierarchy (DontDestroyOnLoad section)

---

## Summary

### Key Achievements

✅ **Global Singleton Lifecycle Alignment**
- SceneLoaderService and SceneTransitionController both use `DontDestroyOnLoad`
- Services persist across all scene transitions
- No more `MissingReferenceException` errors

✅ **Consistent Service Registration**
- All singletons use `ServiceLocator.RegisterSingleton()`
- `ServiceCleanup` provides automatic lifecycle management
- Clear separation: global singletons vs scene-local components

✅ **Centralized Service Initialization**
- `ServiceBootstrap` manages all service creation
- Enforced dependency order prevents initialization failures
- Script Execution Order (-100) ensures services ready before scene code

✅ **Robust Validation**
- `IGlobalService.IsValid()` for runtime health checks
- Context Menu validation for debugging
- Comprehensive logging for troubleshooting

### Architecture Benefits

1. **Reliability**: Services guaranteed to persist across scene transitions
2. **Maintainability**: Clear service lifecycle and dependency graph
3. **Scalability**: Easy to add new global services following the same pattern
4. **Debuggability**: Comprehensive logging and validation tools
5. **Consistency**: Unified approach for all singleton services

---

**End of Documentation**
