# 🏗️ Service-Based Architecture Design

## 📊 **Current Architecture Problems Identified**

### ⚡ **Role Overlap Issues**
- `UIManager` & `UnitController` both create `TurnManager` instances
- Duplicate unit processing logic across both classes  
- Scattered responsibility for game state management

### 🎯 **Single Responsibility Violations**
- `UIManager`: UI + Input + TurnManager creation + Unit processing
- `UnitController`: Units + TurnManager creation + Debug UI
- `TurnManager`: Only passive state tracking (underutilized)

### 🔄 **Dependency Problems**
- Hard-coded `FindObjectOfType` dependencies
- Creation responsibilities scattered across classes
- Potential initialization order conflicts

---

## 🚀 **Recommended Solution: Service-Based Architecture**

### 🏛️ **Core Service Interfaces**

```csharp
// 🎮 Game Flow Coordination
public interface IGameService
{
    void Initialize();
    void StartGame();
    void Update();
    bool IsGameActive { get; }
}

// ⏰ Turn Management
public interface ITurnService  
{
    bool IsPlayerTurn { get; }
    int TurnCount { get; }
    void StartTurn();
    void EndTurn();
    event System.Action<bool> OnTurnChanged;
    event System.Action<int> OnTurnCountChanged;
}

// 👥 Unit Management
public interface IUnitService
{
    void RegisterUnit(Unit unit);
    void UnregisterUnit(Unit unit);
    void ProcessUnitsForCurrentPlayer(bool isPlayerTurn);
    List<Unit> GetActiveUnits(bool? isPlayerUnit = null);
    int GetUnitCount(bool isPlayerUnit);
    event System.Action<Unit> OnUnitRegistered;
    event System.Action<Unit> OnUnitUnregistered;
}

// 🖥️ UI Management
public interface IUIService
{
    void Initialize();
    void UpdateDisplay();
    void ShowMessage(string message);
    event System.Action OnEndTurnRequested;
}
```

### 🎯 **Service Implementation Architecture**

```csharp
// 🎮 Main Game Coordinator
public class GameService : MonoBehaviour, IGameService
{
    private ITurnService turnService;
    private IUnitService unitService; 
    private IUIService uiService;
    
    public bool IsGameActive { get; private set; }
    
    private void Awake()
    {
        ServiceLocator.Register<IGameService>(this);
        Initialize();
    }
    
    public void Initialize()
    {
        // Dependency injection from ServiceLocator
        turnService = ServiceLocator.Get<ITurnService>();
        unitService = ServiceLocator.Get<IUnitService>();
        uiService = ServiceLocator.Get<IUIService>();
        
        // Subscribe to events
        uiService.OnEndTurnRequested += HandleEndTurnRequest;
        turnService.OnTurnChanged += HandleTurnChanged;
    }
    
    public void StartGame()
    {
        IsGameActive = true;
        turnService.StartTurn();
        uiService.UpdateDisplay();
    }
    
    private void HandleEndTurnRequest()
    {
        unitService.ProcessUnitsForCurrentPlayer(turnService.IsPlayerTurn);
        turnService.EndTurn();
    }
    
    private void HandleTurnChanged(bool isPlayerTurn)
    {
        uiService.UpdateDisplay();
    }
}
```

---

## 📋 **Migration Strategy**

### 🔢 **Implementation Order**
1. **Create Service Interfaces** → Define contracts first
2. **Implement TurnService** → Extract from TurnManager (simplest)
3. **Implement UnitService** → Extract unit logic from UnitController  
4. **Implement UIService** → Extract UI logic from UIManager
5. **Create GameService** → Central coordinator
6. **Refactor Original Classes** → Remove redundant responsibilities
7. **Clean Up** → Delete unused code

### ⚙️ **Unity Integration Pattern**

```csharp
// 🎯 Service Manager for initialization order
public class GameServiceManager : MonoBehaviour
{
    [Header("Service Prefabs")]
    [SerializeField] private GameObject turnServicePrefab;
    [SerializeField] private GameObject unitServicePrefab;
    [SerializeField] private GameObject uiServicePrefab;
    [SerializeField] private GameObject gameServicePrefab;
    
    private void Awake()
    {
        InitializeServices();
    }
    
    private void InitializeServices()
    {
        // 1. Core services first
        Instantiate(turnServicePrefab);
        Instantiate(unitServicePrefab);
        
        // 2. UI service depends on core services
        Instantiate(uiServicePrefab);
        
        // 3. Game service coordinates all others
        Instantiate(gameServicePrefab);
    }
}
```

---

## ✅ **Benefits of This Architecture**

### 🎯 **Solves Current Problems**
- ✅ **Eliminates Role Overlap**: Each service has single responsibility
- ✅ **Removes Duplication**: Only one service manages each concept  
- ✅ **Clear Dependencies**: Interface-based injection pattern
- ✅ **Proper Initialization**: Controlled service startup order

### 🚀 **Additional Benefits**
- 🧪 **Testable**: Interface-based design enables unit testing
- 🔄 **Event-Driven**: Loose coupling through observer pattern
- 📈 **Scalable**: Easy to add new services without affecting existing ones
- 🎮 **Unity Compatible**: Leverages existing ServiceLocator pattern
- 🛠️ **Maintainable**: Clear separation of concerns

### 🎨 **Design Patterns Used**
- **Service Locator**: For dependency injection (existing pattern)
- **Observer Pattern**: Event-driven communication between services
- **Singleton Pattern**: Single instance per service type
- **Facade Pattern**: GameService as simplified interface to complex subsystem

---

## 🔧 **Concrete Implementation Specifications**

### 📄 **Service Interface Definitions** 

```csharp
// Assets/Script/Game/Services/Interfaces/IGameService.cs
using System;

namespace Game.Services
{
    public interface IGameService
    {
        bool IsGameActive { get; }
        void Initialize();
        void StartGame();
        void RestartGame();
        void Update();
        
        event Action OnGameStarted;
        event Action OnGameEnded;
    }
}
```

```csharp
// Assets/Script/Game/Services/Interfaces/ITurnService.cs
using System;

namespace Game.Services
{
    public interface ITurnService
    {
        bool IsPlayerTurn { get; }
        int TurnCount { get; }
        
        void StartTurn();
        void EndTurn();
        void StartGame();
        
        event Action<bool> OnTurnChanged;
        event Action<int> OnTurnCountChanged;
    }
}
```

```csharp
// Assets/Script/Game/Services/Interfaces/IUnitService.cs
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
```

```csharp
// Assets/Script/Game/Services/Interfaces/IUIService.cs
using System;

namespace Game.Services
{
    public interface IUIService
    {
        void Initialize();
        void UpdateDisplay();
        void ShowMessage(string message);
        void SetEndTurnButtonEnabled(bool enabled);
        
        event Action OnEndTurnRequested;
        event Action OnRestartRequested;
    }
}
```

### 🏗️ **Service Implementations**

```csharp
// Assets/Script/Game/Services/TurnService.cs
using UnityEngine;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    public class TurnService : MonoBehaviour, ITurnService
    {
        [SerializeField] private bool isPlayerTurn = true;
        private int turnCount = 0;
        
        public bool IsPlayerTurn => isPlayerTurn;
        public int TurnCount => turnCount;
        
        public event System.Action<bool> OnTurnChanged;
        public event System.Action<int> OnTurnCountChanged;
        
        private void Awake()
        {
            ServiceLocator.Register<ITurnService>(this);
            Debug.Log("[TurnService] Registered in ServiceLocator");
        }
        
        public void StartGame()
        {
            isPlayerTurn = true;
            turnCount = 0;
            Debug.Log("[TurnService] Game Started - Player Turn");
            OnTurnChanged?.Invoke(isPlayerTurn);
            OnTurnCountChanged?.Invoke(turnCount);
        }
        
        public void StartTurn()
        {
            Debug.Log($"[TurnService] Turn {turnCount + 1} Started - {(isPlayerTurn ? "Player" : "Enemy")}");
        }
        
        public void EndTurn()
        {
            isPlayerTurn = !isPlayerTurn;
            turnCount++;
            
            Debug.Log($"[TurnService] Turn {turnCount} Complete - Next: {(isPlayerTurn ? "Player" : "Enemy")}");
            
            OnTurnChanged?.Invoke(isPlayerTurn);
            OnTurnCountChanged?.Invoke(turnCount);
        }
    }
}
```

```csharp
// Assets/Script/Game/Services/UnitService.cs
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
            ServiceLocator.Register<IUnitService>(this);
            Debug.Log("[UnitService] Registered in ServiceLocator");
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
```

```csharp
// Assets/Script/Game/Services/UIService.cs
using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    public class UIService : MonoBehaviour, IUIService
    {
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Text turnStatusText;
        [SerializeField] private Canvas gameCanvas;
        
        private ITurnService turnService;
        private IUnitService unitService;
        
        public event System.Action OnEndTurnRequested;
        public event System.Action OnRestartRequested;
        
        private void Awake()
        {
            ServiceLocator.Register<IUIService>(this);
            Debug.Log("[UIService] Registered in ServiceLocator");
        }
        
        private void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            // Get dependencies from ServiceLocator
            turnService = ServiceLocator.Get<ITurnService>();
            unitService = ServiceLocator.Get<IUnitService>();
            
            if (turnService == null)
                Debug.LogError("[UIService] ITurnService not found in ServiceLocator");
            if (unitService == null)
                Debug.LogError("[UIService] IUnitService not found in ServiceLocator");
            
            // Subscribe to events
            if (turnService != null)
            {
                turnService.OnTurnChanged += HandleTurnChanged;
                turnService.OnTurnCountChanged += HandleTurnCountChanged;
            }
            
            CreateUIElements();
            UpdateDisplay();
        }
        
        private void CreateUIElements()
        {
            if (gameCanvas == null) CreateCanvas();
            if (endTurnButton == null) CreateEndTurnButton();
            if (turnStatusText == null) CreateTurnStatusText();
        }
        
        private void Update()
        {
            HandleKeyboardInput();
        }
        
        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnEndTurnRequested?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnRestartRequested?.Invoke();
            }
        }
        
        public void UpdateDisplay()
        {
            UpdateTurnStatusText();
            UpdateEndTurnButton();
        }
        
        public void ShowMessage(string message)
        {
            Debug.Log($"[UIService] Message: {message}");
            // Could extend to show actual UI message
        }
        
        public void SetEndTurnButtonEnabled(bool enabled)
        {
            if (endTurnButton != null)
                endTurnButton.interactable = enabled;
        }
        
        private void HandleTurnChanged(bool isPlayerTurn)
        {
            UpdateDisplay();
        }
        
        private void HandleTurnCountChanged(int turnCount)
        {
            UpdateDisplay();
        }
        
        private void OnEndTurnButtonClicked()
        {
            OnEndTurnRequested?.Invoke();
        }
        
        // UI Creation methods (same as original UIManager)
        private void CreateCanvas() { /* Same implementation */ }
        private void CreateEndTurnButton() { /* Same implementation */ }
        private void CreateTurnStatusText() { /* Same implementation */ }
        private void UpdateTurnStatusText() { /* Same implementation */ }
        private void UpdateEndTurnButton() { /* Same implementation */ }
    }
}
```

```csharp
// Assets/Script/Game/Services/GameService.cs
using UnityEngine;
using Game.Core;
using Game.Services;

namespace Game.Services
{
    public class GameService : MonoBehaviour, IGameService
    {
        private ITurnService turnService;
        private IUnitService unitService;
        private IUIService uiService;
        
        public bool IsGameActive { get; private set; }
        
        public event System.Action OnGameStarted;
        public event System.Action OnGameEnded;
        
        private void Awake()
        {
            ServiceLocator.Register<IGameService>(this);
            Debug.Log("[GameService] Registered in ServiceLocator");
        }
        
        private void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            // Get dependencies from ServiceLocator
            turnService = ServiceLocator.Get<ITurnService>();
            unitService = ServiceLocator.Get<IUnitService>();
            uiService = ServiceLocator.Get<IUIService>();
            
            ValidateDependencies();
            SubscribeToEvents();
            
            // Auto-start game
            StartGame();
        }
        
        private void ValidateDependencies()
        {
            if (turnService == null)
                Debug.LogError("[GameService] ITurnService not found");
            if (unitService == null)
                Debug.LogError("[GameService] IUnitService not found");
            if (uiService == null)
                Debug.LogError("[GameService] IUIService not found");
        }
        
        private void SubscribeToEvents()
        {
            if (uiService != null)
            {
                uiService.OnEndTurnRequested += HandleEndTurnRequest;
                uiService.OnRestartRequested += RestartGame;
            }
        }
        
        public void StartGame()
        {
            Debug.Log("[GameService] Starting game...");
            
            IsGameActive = true;
            turnService?.StartGame();
            uiService?.UpdateDisplay();
            
            OnGameStarted?.Invoke();
        }
        
        public void RestartGame()
        {
            Debug.Log("[GameService] Restarting game...");
            StartGame();
        }
        
        public void Update()
        {
            // Handle any global game state updates
            HandleTestInput();
        }
        
        private void HandleEndTurnRequest()
        {
            if (!IsGameActive || turnService == null || unitService == null) return;
            
            Debug.Log("[GameService] Processing end turn request...");
            
            // Process units for current turn
            unitService.ProcessUnitsForCurrentPlayer(turnService.IsPlayerTurn);
            
            // End the turn
            turnService.EndTurn();
            
            // Update UI
            uiService?.UpdateDisplay();
        }
        
        private void HandleTestInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                HandleEndTurnRequest();
            }
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                unitService?.ProcessAllUnits();
            }
        }
    }
}
```

---

## ✅ **Unity Best Practices Validation**

### 🎮 **Unity-Specific Compliance**

| **Practice** | **Status** | **Implementation** |
|--------------|------------|-------------------|
| **MonoBehaviour Lifecycle** | ✅ | Services use Awake/Start properly for initialization order |
| **ServiceLocator Pattern** | ✅ | Leverages existing ServiceLocator from codebase |
| **Script Execution Order** | ✅ | Clear dependency hierarchy through initialization timing |
| **Event System** | ✅ | UnityEvent-compatible C# events for loose coupling |
| **Singleton Management** | ✅ | Single service instances registered in ServiceLocator |
| **Performance** | ✅ | Minimal Update() usage, efficient event-driven architecture |
| **Debugging** | ✅ | Comprehensive logging for service interactions |
| **Testability** | ✅ | Interface-based design enables unit testing |

### 🔧 **Architecture Quality Validation**

| **Principle** | **Validation** | **Evidence** |
|---------------|----------------|--------------|
| **Single Responsibility** | ✅ **Achieved** | Each service has one clear purpose |
| **Dependency Inversion** | ✅ **Achieved** | Services depend on interfaces, not concrete classes |
| **Open/Closed Principle** | ✅ **Achieved** | Easy to extend services without modifying existing code |
| **Loose Coupling** | ✅ **Achieved** | Event-driven communication between services |
| **High Cohesion** | ✅ **Achieved** | Related functionality grouped within services |

### 📋 **Migration & Deployment Strategy**

```csharp
// Assets/Script/Game/Services/GameServiceManager.cs
using UnityEngine;
using Game.Services;

namespace Game.Services
{
    /// <summary>
    /// Manages service initialization order and coordination
    /// </summary>
    public class GameServiceManager : MonoBehaviour
    {
        [Header("Service Components")]
        [SerializeField] private TurnService turnService;
        [SerializeField] private UnitService unitService;
        [SerializeField] private UIService uiService;
        [SerializeField] private GameService gameService;
        
        [Header("Auto-Initialize")]
        [SerializeField] private bool autoInitialize = true;
        
        private void Awake()
        {
            if (autoInitialize)
            {
                InitializeServices();
            }
        }
        
        public void InitializeServices()
        {
            Debug.Log("[GameServiceManager] Initializing game services...");
            
            // Ensure services are present
            EnsureServiceComponents();
            
            // Services will auto-register through their Awake() methods
            // and auto-initialize through their Start() methods
            
            Debug.Log("[GameServiceManager] All services initialized successfully");
        }
        
        private void EnsureServiceComponents()
        {
            if (turnService == null)
                turnService = gameObject.AddComponent<TurnService>();
            
            if (unitService == null)
                unitService = gameObject.AddComponent<UnitService>();
            
            if (uiService == null)
                uiService = gameObject.AddComponent<UIService>();
            
            if (gameService == null)
                gameService = gameObject.AddComponent<GameService>();
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            GUILayout.Label("=== Service Architecture Status ===");
            
            var gameServ = ServiceLocator.Get<IGameService>();
            var turnServ = ServiceLocator.Get<ITurnService>();
            var unitServ = ServiceLocator.Get<IUnitService>();
            var uiServ = ServiceLocator.Get<IUIService>();
            
            GUILayout.Label($"Game Service: {(gameServ != null ? "✅" : "❌")}");
            GUILayout.Label($"Turn Service: {(turnServ != null ? "✅" : "❌")}");
            GUILayout.Label($"Unit Service: {(unitServ != null ? "✅" : "❌")}");
            GUILayout.Label($"UI Service: {(uiServ != null ? "✅" : "❌")}");
            
            if (gameServ != null)
                GUILayout.Label($"Game Active: {gameServ.IsGameActive}");
            if (turnServ != null)
                GUILayout.Label($"Turn: {turnServ.TurnCount} ({(turnServ.IsPlayerTurn ? "Player" : "Enemy")})");
            if (unitServ != null)
                GUILayout.Label($"Active Units: {unitServ.ActiveUnitCount}");
            
            GUILayout.EndArea();
        }
    }
}
```

---

## 🎯 **Final Recommendation Summary**

### 🏆 **Optimal Solution: Service-Based Architecture**

The **Service-Based Architecture with Event-Driven Communication** is the optimal solution for resolving the identified role overlap and architectural issues.

### ⚡ **Key Advantages Over Alternatives**

| **Alternative** | **Why Service Architecture is Better** |
|-----------------|----------------------------------------|
| **Inheritance-based solution** | ❌ Creates tight coupling and complex hierarchies |
| **Static utility classes** | ❌ Hard to test, violates dependency inversion |
| **Heavyweight patterns (ECS)** | ❌ Over-engineering for current scope |
| **Command pattern** | ❌ Adds complexity without clear benefits |

### 🎯 **Service Architecture Benefits**

✅ **Solves All Identified Problems**
- Eliminates role overlap between managers
- Provides clear single responsibilities
- Removes duplicate object creation
- Establishes proper dependency management

✅ **Maintains Unity Best Practices**
- Uses existing ServiceLocator pattern
- MonoBehaviour-based services
- Proper initialization order
- Unity-compatible event system

✅ **Enables Future Growth**
- Easy to add new services
- Interface-based testing
- Event-driven scalability
- Clean separation of concerns

### 🚀 **Implementation Recommendation**

**Use the provided Service-Based Architecture** as it:
1. **Directly addresses all role overlap issues** identified in the analysis
2. **Leverages existing infrastructure** (ServiceLocator pattern)
3. **Follows Unity and C# best practices** comprehensively
4. **Provides immediate and long-term benefits** for code maintainability

This architecture transforms the current tightly-coupled manager system into a clean, maintainable, and scalable service-oriented design that eliminates the identified problems while preserving all existing functionality.