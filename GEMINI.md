# GEMINI.md

## Project Overview

This is a Unity project for a 6x6 grid-based strategic card battle game. The goal is for the player to use cards to summon units and destroy the enemy's base. The project is built with Unity 2022.3.62f1 and uses the 3D Universal Render Pipeline (URP).

The project follows a clean architecture with a service-oriented approach. Key systems are managed by services, which are accessed through a `ServiceLocator`. This promotes loose coupling and better testability.

### Core Technologies

*   **Engine:** Unity 2022.3.62f1 (3D URP)
*   **Language:** C#
*   **Architecture:** Service-Oriented Clean Architecture
*   **Key Libraries:**
    *   **DOTween:** For animations.
    *   **TextMesh Pro:** For advanced text rendering.

### Key Systems

*   **Grid System:** Managed by `GridManager`, which coordinates `GridState` (data), `GridController` (business logic), and `GridRenderer` (presentation).
*   **Turn System:** Managed by `TurnService`, which controls the game flow through a series of phases (`TurnPhase` enum).
*   **Card System:** `CardData` (ScriptableObjects) define card properties. `CardHandManager` and `CardSpawnService` handle card operations.
*   **Unit System:** Units are represented by `Unit` GameObjects, with components like `HealthComponent`, `MovementComponent`, and `CombatComponent`. `UnitData` (ScriptableObjects) define unit stats.
*   **Service Locator:** A central `ServiceLocator` provides access to all major services (`ITurnService`, `IUnitService`, `IGridManager`, etc.).

## Building and Running

This is a Unity project, so it must be opened in the Unity Editor to be built and run.

1.  **Open the project:** Open the project folder (`2025-2TeamProject`) in the Unity Hub, using Unity version **2022.3.62f1**.
2.  **Open the main scene:** The main scene is likely `Assets/Scenes/SampleScene.unity` or `Assets/Scenes/GridTestScene.unity`.
3.  **Run the game:** Press the "Play" button in the Unity Editor.

### Testing

The project includes a suite of tests that can be run from the Unity Test Runner window.

*   **PlayMode tests:** These tests run in the editor's play mode and are located in `Assets/Script/Tests`. They cover the integration of different game systems.
*   **EditMode tests:** (TODO: Add information on EditMode tests if any exist).

## Development Conventions

*   **Clean Architecture:** The project follows a 4-layer clean architecture:
    1.  **Unity Integration Layer:** (e.g., `GridManager`) MonoBehaviours that interact with the Unity engine.
    2.  **Presentation Layer:** (e.g., `GridRenderer`) Handles rendering and UI.
    3.  **Business Logic Layer:** (e.g., `GridController`) Contains the core game logic.
    4.  **Data Layer:** (e.g., `GridState`) Manages the game's data.
*   **Service Locator:** Use the `ServiceLocator` to access game services. Do not create direct dependencies between managers.
*   **Interfaces:** Services and components are defined by interfaces (e.g., `ITurnService`, `IHealthComponent`) to promote dependency inversion.
*   **Data-Driven Design:** `ScriptableObject`s are used to define data for units (`UnitData`) and cards (`CardData`).
*   **Events:** Services use C# events to communicate with each other and with the UI.
*   **Coding Style:** Follow the existing coding style, which includes:
    *   Using namespaces (e.g., `Game.Services`, `Game.Components`).
    *   Using private fields with `[SerializeField]` for Unity Inspector exposure.
    *   Using summary comments for public methods and classes.
