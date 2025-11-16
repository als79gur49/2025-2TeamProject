using Game;
using Game.Components;
using Game.Components.Abilities;
using Game.Core;
using Game.Core.Modifiers;
using Game.Core.Effects;
using Game.Data;
using Game.Interfaces;
using Game.Services;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;
public class Unit : MonoBehaviour
{
    [Header("Legacy Configuration (for Inspector compatibility)")]
    [SerializeField] private int health = 100;
    [SerializeField] private int attackPower = 10;
    [SerializeField] private int movementRange = 1;
    [SerializeField] private bool isPlayerUnit = true;
    
    [Header("Component System")]
    [SerializeField] private bool useComponentSystem = true;
    [SerializeField] private bool autoAddMissingComponents = true;

    [Header("Event Channels")]
    [SerializeField] private EnemyKilledEventChannelSO enemyKilledChannel;

    [Header("Runtime Status (Read Only)")]
    [SerializeField, Tooltip("Shows if all components are properly initialized")]
    private bool componentSystemReady = false;
    
    [SerializeField, Tooltip("Shows if Unit has been initialized with ServiceLocator")]
    private bool isInitialized = false;
    
    [SerializeField, Tooltip("Shows if death notification has been sent to prevent duplicates")]
    private bool deathNotificationSent = false;
    [SerializeField, Tooltip("사망애니메이션 길이")]
    private float deathAnimationDuration = 1f; // 해당 시간 이후 파괴

    // Component references
    private IHealthComponent healthComponent;
    private ICombatSystem combatComponent;
    private IMovementSystem movementComponent;
    private ITeamComponent teamComponent;
    private IAnimationController animationController;
    private IUnitAI unitAI;

    // 새로운 Action System 필드
    private ActionEvaluator actionEvaluator;
    private ActionExecutorRegistry executorRegistry;
    private bool isExecutingAction = false;
    private ActionResult currentActionResult;
    private ActionContext currentActionContext;

    // Effect System
    private EffectManager effectManager;

    // Legacy system support
    private int legacyMaxHealth;
    
    // Phase 3: Clean Architecture - ServiceLocator pattern
    private IGridManager gridManager;
    private IGlobalStateManager globalStateManager;
    private IDeathAnimationManager deathAnimationManager;

    // Death animation state tracking
    private bool isPlayingDeathAnimation = false;
    private bool isWaitingForDeathAnimation = false;

    [SerializeField]
    private Tile currentTile;
    
    // Public properties with component delegation
    public int Health => useComponentSystem && healthComponent != null ? healthComponent.CurrentHealth : health;
    public int MaxHealth => useComponentSystem && healthComponent != null ? healthComponent.MaxHealth : legacyMaxHealth;
    public int AttackPower => useComponentSystem && combatComponent != null ? combatComponent.CurrentAttackPower : attackPower;
    public int MovementRange => useComponentSystem && movementComponent != null ? movementComponent.MovementRange : movementRange;
    public bool IsAlive => useComponentSystem && healthComponent != null ? healthComponent.IsAlive : health > 0;
    public bool IsPlayerUnit => useComponentSystem && teamComponent != null ? teamComponent.Team == TeamType.Player : isPlayerUnit;
    public Tile CurrentTile => currentTile;
    
    // Legacy grid position properties
    public int X => CurrentTile?.X ?? (gridManager != null ? gridManager.GetUnitPosition(gameObject).x : -1);
    public int Y => CurrentTile?.Y ?? (gridManager != null ? gridManager.GetUnitPosition(gameObject).y : -1);
    
    // Initialization status properties
    public bool IsInitialized => isInitialized;
    public bool IsReadyForGame => isInitialized && gridManager != null;

    // Component access methods
    public IAnimationController GetAnimationController() => animationController;
    
    private void Awake()
    {
        // Initialize component system
        InitializeComponents();

        // Initialize new Action System components
        actionEvaluator = new ActionEvaluator();

        executorRegistry = new ActionExecutorRegistry();

        // Register action executors for each supported action type
        executorRegistry.RegisterExecutor(new Game.Core.Executors.AttackActionExecutor());
        executorRegistry.RegisterExecutor(new Game.Core.Executors.MovementActionExecutor());

        // Initialize Effect system
        effectManager = new EffectManager(this);

        // Legacy system fallback
        legacyMaxHealth = health;

        // Note: Modifiers should be added externally via ModifierFactory
        // or through UnitData/CardData configuration
    }
    
    /// <summary>
    /// Unit을 ServiceLocator와 연결하고 초기화 수행
    /// 외부에서 호출하여 초기화 타이밍 제어 가능
    /// </summary>
    public void Init(UnitData unitData, Vector2Int position, bool isPlayerUnit)
    {
        if (isInitialized)
        {
            Debug.LogWarning($"[Unit] {gameObject.name} already initialized");
            return;
        }

        // UnitData로부터 스탯 설정
        if (unitData != null)
        {
            health = unitData.MaxHealth;
            legacyMaxHealth = unitData.MaxHealth;
            attackPower = unitData.AttackPower;
            movementRange = unitData.MovementRange;
            this.isPlayerUnit = isPlayerUnit;

            // 컴포넌트 시스템 사용 시 컴포넌트에도 적용
            if (useComponentSystem)
            {
                if (healthComponent != null)
                {
                    healthComponent.SetMaxHealth(unitData.MaxHealth);
                    healthComponent.SetHealth(unitData.MaxHealth);
                }
                if (combatComponent != null)
                {
                    combatComponent.SetBaseAttackPower(unitData.AttackPower);
                }
                if (movementComponent != null)
                {
                    movementComponent.SetMovementRange(unitData.MovementRange);
                }
                if (teamComponent != null)
                {
                    teamComponent.Team = isPlayerUnit ? TeamType.Player : TeamType.Enemy;
                }
            }

            Debug.Log($"[Unit] {gameObject.name} initialized with UnitData: HP={unitData.MaxHealth}, ATK={unitData.AttackPower}, MOV={unitData.MovementRange}, Team={isPlayerUnit}");
        }

        // Modifiers 적용 (UnitData로부터)
        if (unitData != null && unitData.Modifiers != null && unitData.Modifiers.Count > 0)
        {
            var modifierFactory = ServiceLocator.Get<IModifierFactory>();
            if (modifierFactory != null)
            {
                int appliedCount = 0;
                foreach (var modifierData in unitData.Modifiers)
                {
                    if (modifierData != null)
                    {
                        try
                        {
                            var modifier = modifierFactory.CreateFromData(modifierData, this);
                            if (modifier != null)
                            {
                                AddActionModifier(modifier);
                                appliedCount++;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[Unit] Failed to create modifier from {modifierData.name}: {ex.Message}");
                        }
                    }
                }
                Debug.Log($"[Unit] {appliedCount} modifier(s) applied to {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[Unit] IModifierFactory not found in ServiceLocator - Cannot apply modifiers to {gameObject.name}");
            }
        }

        // Effects 적용 (UnitData로부터)
        if (unitData != null && unitData.Effects != null && unitData.Effects.Count > 0)
        {
            int appliedEffects = 0;
            foreach (var effectData in unitData.Effects)
            {
                if (effectData == null) continue;

                try
                {
                    var effect = effectData.CreateEffect(this);
                    if (effect != null)
                    {
                        AddEffect(effect);
                        appliedEffects++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Unit] Failed to create effect from {effectData.name}: {ex.Message}");
                }
            }

            if (appliedEffects > 0)
            {
                Debug.Log($"[Unit] {appliedEffects} effect(s) applied to {gameObject.name} from UnitData");
            }
        }

        // ServiceLocator에서 GridManager 가져오기 (등록은 외부에서 처리)
        var gridManager = ServiceLocator.Get<IGridManager>();

        if (gridManager != null)
        {
            InitializeGridManager(gridManager);
        }
        else
        {
            Debug.LogWarning($"[Unit] {gameObject.name} could not get GridManager from ServiceLocator");
        }

        // GlobalStateManager 초기화
        globalStateManager = ServiceLocator.Get<IGlobalStateManager>();
        if (globalStateManager == null)
        {
            Debug.LogWarning($"[Unit] {gameObject.name} could not get GlobalStateManager from ServiceLocator");
        }
        else
        {
            // GlobalStateManager 이벤트 구독
            globalStateManager.OnBusyStateChanged += OnGlobalBusyStateChanged;
        }

        // DeathAnimationManager 초기화
        deathAnimationManager = ServiceLocator.Get<IDeathAnimationManager>();
        if (deathAnimationManager == null)
        {
            Debug.LogWarning($"[Unit] {gameObject.name} could not get IDeathAnimationManager from ServiceLocator");
        }

        isInitialized = true;

        Debug.Log($"[Unit] {gameObject.name} initialization completed at position ({position.x}, {position.y}) - 프레임: {Time.frameCount}");
    }

    /// <summary>
    /// ServiceLocator를 통한 직접 GridManager 초기화
    /// </summary>
    private void InitializeGridManager(IGridManager igridManager)
    {
        gridManager = igridManager;
        if (gridManager != null)
        {
            Debug.Log($"[Unit] GridManager initialized directly for {gameObject.name}");

            SetupEventSubscriptions();
            InitializeCurrentTile();
        }
        else
        {
            Debug.LogWarning($"[Unit] Failed to initialize GridManager for {gameObject.name}");
        }
    }
    
    /// <summary>
    /// 초기화 시 currentTile 설정 - 이미 그리드에 배치된 유닛을 위한 처리
    /// </summary>
    private void InitializeCurrentTile()
    {
        if (gridManager == null)
        {
            Debug.LogWarning($"[Unit] {gameObject.name} InitializeCurrentTile called but gridManager is null");
            return;
        }
        
        // currentTile이 이미 설정되어 있으면 스킵
        if (currentTile != null) return;
        
        // GridManager에서 현재 유닛의 위치 확인
        if (gridManager.TryGetUnitPosition(gameObject, out Vector2Int currentPosition))
        {
            Debug.Log($"[Unit] Found {gameObject.name} at position ({currentPosition.x}, {currentPosition.y}) during initialization");
            UpdateCurrentTile(currentPosition);
        }
        else
        {
            Debug.Log($"[Unit] {gameObject.name} not found in GridManager during initialization - currentTile will be set when placed");
        }
    }
    
    /// <summary>
    /// Phase 3: 이벤트 기반 통신 설정 - GridManager 이벤트 구독
    /// </summary>
    private void SetupEventSubscriptions()
    {
        // GridManager 이벤트 구독
        if (gridManager != null)
        {
            gridManager.OnUnitMoved += OnUnitMovedInGrid;
        }
        else
        {
            Debug.LogWarning($"[Unit] GridManager not available for {gameObject.name} - event subscriptions skipped");
        }
    }
    
    /// <summary>
    /// 그리드에서 유닛이 이동했을 때 호출되는 이벤트 핸들러
    /// </summary>
    private void OnUnitMovedInGrid(GameObject movedUnit, Vector2Int oldPos, Vector2Int newPos)
    {
        // 자신의 이동이면 위치 업데이트
        if (movedUnit == gameObject)
        {
            Debug.Log($"[Unit] {gameObject.name} moved from {oldPos} to {newPos} via event");
            UpdateCurrentTile(newPos);
        }
    }
    
    
    /// <summary>
    /// 지정된 그리드 위치의 Tile 컴포넌트를 찾아서 currentTile을 업데이트
    /// </summary>
    private void UpdateCurrentTile(Vector2Int gridPosition)
    {
        if (gridManager == null)
        {
            Debug.LogWarning($"[Unit] Cannot update currentTile for {gameObject.name} - gridManager is null");
            return;
        }

        // 월드 위치로 변환
        Vector3 worldPosition = gridManager.GridToWorldPosition(gridPosition);
        
        // 해당 위치 근처에서 Tile 컴포넌트를 찾기
        Tile foundTile = FindTileAtPosition(worldPosition, gridPosition);
        
        if (foundTile != null)
        {
            currentTile = foundTile;
            Debug.Log($"[Unit] {gameObject.name} currentTile updated to {foundTile.name} at ({gridPosition.x}, {gridPosition.y})");
        }
        else
        {
            Debug.LogWarning($"[Unit] Could not find Tile component at grid position ({gridPosition.x}, {gridPosition.y}) for {gameObject.name}");
            
            // Fallback: Create a virtual tile data if no physical tile is found
            CreateVirtualTile(gridPosition);
        }
    }
    
    /// <summary>
    /// 지정된 위치에서 Tile 컴포넌트를 찾기
    /// </summary>
    private Tile FindTileAtPosition(Vector3 worldPosition, Vector2Int gridPosition)
    {
        // Method 1: 반경 내에서 Tile 검색
        Collider[] colliders = Physics.OverlapSphere(worldPosition, gridManager.TileSize * 0.6f);
        foreach (var collider in colliders)
        {
            Tile tile = collider.GetComponent<Tile>();
            if (tile != null && tile.X == gridPosition.x && tile.Y == gridPosition.y)
            {
                return tile;
            }
        }
        
        // Method 2: 이름으로 검색 (GridRenderer가 생성한 타일들)
        GameObject tileObject = GameObject.Find($"Tile_{gridPosition.x}_{gridPosition.y}");
        if (tileObject != null)
        {
            Tile tile = tileObject.GetComponent<Tile>();
            if (tile != null)
            {
                return tile;
            }
        }
        
        // Method 3: 모든 Tile에서 위치 매칭 검색
        Tile[] allTiles = FindObjectsOfType<Tile>();
        foreach (var tile in allTiles)
        {
            if (tile.X == gridPosition.x && tile.Y == gridPosition.y)
            {
                return tile;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 물리적 타일이 없을 때 가상 타일 생성
    /// </summary>
    private void CreateVirtualTile(Vector2Int gridPosition)
    {
        // 임시 GameObject 생성하여 Tile 컴포넌트 추가
        GameObject virtualTileObject = new GameObject($"VirtualTile_{gridPosition.x}_{gridPosition.y}");
        virtualTileObject.transform.position = gridManager.GridToWorldPosition(gridPosition);
        
        Tile virtualTile = virtualTileObject.AddComponent<Tile>();
        virtualTile.Initialize(gridPosition.x, gridPosition.y);
        
        currentTile = virtualTile;
        
        Debug.Log($"[Unit] Created virtual tile for {gameObject.name} at ({gridPosition.x}, {gridPosition.y})");
    }
    
    private void InitializeComponents()
    {
        // Cache component references for performance
        healthComponent = GetComponent<IHealthComponent>();
        combatComponent = GetComponent<ICombatSystem>();
        movementComponent = GetComponent<IMovementSystem>();
        teamComponent = GetComponent<ITeamComponent>();
        animationController = GetComponent<IAnimationController>();
        unitAI = GetComponent<IUnitAI>();

        // Initialize component system if available
        if (useComponentSystem)
        {
            InitializeHealthComponent();
            InitializeCombatComponent();
            InitializeMovementComponent();
            InitializeTeamComponent();
            InitializeAnimationController();
            InitializeAIComponent();

            // Update runtime status
            UpdateComponentSystemStatus();
        }
    }
    
    private void UpdateComponentSystemStatus()
    {
        componentSystemReady = useComponentSystem &&
                              healthComponent != null &&
                              combatComponent != null &&
                              movementComponent != null &&
                              teamComponent != null &&
                              unitAI != null;
    }
    
    private void InitializeHealthComponent()
    {
        if (healthComponent == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add HealthComponent if not present
            var comp = gameObject.AddComponent<HealthComponent>();
            healthComponent = comp;
            Debug.Log($"[Unit] Auto-added HealthComponent to {gameObject.name}");
        }

        // Apply legacy values to component
        if (healthComponent != null)
        {
            healthComponent.SetMaxHealth(health);
            healthComponent.SetHealth(health);

            // Subscribe to OnDeath event for event-based death handling
            // HealthComponent will invoke this when ProcessDeath() is called
            var healthComp = healthComponent as HealthComponent;
            if (healthComp != null)
            {
                healthComp.OnDeath += OnHealthComponentDeath;
                Debug.Log($"[Unit] Subscribed to HealthComponent.OnDeath event for {gameObject.name}");
            }
        }
    }
    
    private void InitializeCombatComponent()
    {
        if (combatComponent == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add CombatComponent if not present
            var comp = gameObject.AddComponent<CombatComponent>();
            combatComponent = comp;
            Debug.Log($"[Unit] Auto-added CombatComponent to {gameObject.name}");
        }
        
        // Apply legacy values to component
        if (combatComponent != null)
        {
            combatComponent.SetBaseAttackPower(attackPower);
        }
    }
    
    private void InitializeMovementComponent()
    {
        if (movementComponent == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add MovementComponent if not present
            var comp = gameObject.AddComponent<MovementComponent>();
            movementComponent = comp;
            Debug.Log($"[Unit] Auto-added MovementComponent to {gameObject.name}");
        }
        
        // Apply legacy values to component
        if (movementComponent != null)
        {
            movementComponent.SetMovementRange(movementRange);
        }
    }
    
    private void InitializeTeamComponent()
    {
        if (teamComponent == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add TeamComponent if not present
            var comp = gameObject.AddComponent<TeamComponent>();
            teamComponent = comp;
            Debug.Log($"[Unit] Auto-added TeamComponent to {gameObject.name}");
        }

        // Apply legacy values to component
        if (teamComponent != null)
        {
            // Set team based on legacy isPlayerUnit flag
            teamComponent.Team = isPlayerUnit ? TeamType.Player : TeamType.Enemy;
        }
    }

    private void InitializeAnimationController()
    {
        if (animationController == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add UnitAnimationController if not present
            var comp = gameObject.AddComponent<UnitAnimationController>();
            animationController = comp;
            Debug.Log($"[Unit] Auto-added UnitAnimationController to {gameObject.name}");
        }
    }

    private void InitializeAIComponent()
    {
        if (unitAI == null && useComponentSystem && autoAddMissingComponents)
        {
            // Auto-add BasicUnitAI if not present
            var comp = gameObject.AddComponent<BasicUnitAI>();
            unitAI = comp;
            Debug.Log($"[Unit] Auto-added BasicUnitAI to {gameObject.name}");
        }
    }
    
    public void SetCurrentTile(Tile tile)
    {
        currentTile = tile;
    }

    public void OnPlaced(Tile tile)
    {
        SetCurrentTile(tile);
        TriggerEffects(EffectTrigger.OnDeploy, new EffectContext());
    }
    
    public void OnTurnStart()
    {
        if (!IsAlive) return;
        
        // Ensure currentTile is set before initializing turn
        if (currentTile == null)
        {
            Debug.LogWarning($"[Unit] {gameObject.name} OnTurnStart called but currentTile is null - attempting to initialize");
            InitializeCurrentTile();
            
            if (currentTile == null)
            {
                Debug.LogError($"[Unit] {gameObject.name} cannot initialize turn - currentTile is still null after initialization attempt");
                return;
            }
        }
        
        // Initialize turn for components - 턴 시작 시 필요한 초기화만 수행
        if (useComponentSystem && movementComponent != null)
        {
            movementComponent.StartTurn();
        }

        // Effect: OnTurnStart 트리거
        TriggerEffects(EffectTrigger.OnTurnStart, new EffectContext());
        
        Debug.Log($"[Unit] {gameObject.name} OnTurnStart - currentTile: {currentTile?.name} at ({currentTile?.X}, {currentTile?.Y}) - Turn initialized");
        // Act() 호출 제거 - 실제 행동은 Action Phase에서 별도로 처리
    }

    public void OnTurnEnd()
    {
        if (!IsAlive) return;

        // Effect: OnTurnEnd 트리거
        TriggerEffects(EffectTrigger.OnTurnEnd, new EffectContext());

        // 지속 턴 감소 및 정리
        effectManager?.TickDurationsAndCleanup();

        Debug.Log($"[Unit] {gameObject.name} OnTurnEnd");
    }
    
    public void Act()
    {
        // AI 컴포넌트가 있으면 AI에게 위임
        if(true)
        {
            ExecuteAITurn();
        }
        else if (useComponentSystem && unitAI != null)
        {
            var decision = unitAI.DecideAction();
            ExecuteDecision(decision);
        }
    }

    /// <summary>
    /// AI 결정 실행 - IUnitAI의 ActionDecision을 실제 행동으로 변환
    /// </summary>
    private void ExecuteDecision(ActionDecision decision)
    {
        switch (decision.Type)
        {
            case UnitActionType.Attack:
                // Prioritize Tile-based attack (new system)
                if (decision.TargetTile != null)
                {
                    var targetName = decision.TargetTile.OccupyingUnit != null
                        ? decision.TargetTile.OccupyingUnit.gameObject.name
                        : decision.TargetTile.OccupyingBase?.gameObject.name ?? "Unknown";
                    Debug.Log($"[Unit] AI Decision: Attack tile at ({decision.TargetTile.X}, {decision.TargetTile.Y}) with target: {targetName}");

                    if (useComponentSystem && combatComponent != null)
                    {
                        // Use tile-based attack (handles multi-tile entities correctly)
                        var tiles = new List<Tile> { decision.TargetTile };
                        int hitCount = combatComponent.AttackTiles(tiles);

                        if (hitCount > 0)
                        {
                            Debug.Log($"{gameObject.name} attacked tile ({decision.TargetTile.X}, {decision.TargetTile.Y}), hit {hitCount} target(s)");
                        }
                        else
                        {
                            Debug.Log($"{gameObject.name} attack on tile ({decision.TargetTile.X}, {decision.TargetTile.Y}) failed (no valid targets)");
                        }
                    }
                }
                // Fallback: GameObject-based attack (backward compatibility)
                else if (decision.TargetObject != null)
                {
                    Debug.Log($"[Unit] AI Decision: Attack GameObject {decision.TargetObject.name} (Legacy mode)");

                    if (useComponentSystem && combatComponent != null)
                    {
                        var result = combatComponent.Attack(decision.TargetObject);
                        if (result.Success && result.IsHit)
                        {
                            Debug.Log($"{gameObject.name} attacks {decision.TargetObject.name} for {result.DamageDealt} damage!");
                        }
                        else
                        {
                            Debug.Log($"{gameObject.name} attack on {decision.TargetObject.name} failed! " +
                                $"cause: {result.Message}");
                        }
                    }
                }
                break;

            case UnitActionType.Move:
                Debug.Log($"[Unit] AI Decision: Move to {decision.MovePosition}");

                if (useComponentSystem && movementComponent != null)
                {
                    var result = movementComponent.MoveTo(decision.MovePosition);
                    if (result.Success)
                    {
                        Debug.Log($"{gameObject.name} moved to ({decision.MovePosition.x}, {decision.MovePosition.y})");
                    }
                    else
                    {
                        Debug.Log($"{gameObject.name} movement failed - {result.Message}");
                    }
                }
                break;

            case UnitActionType.Idle:
                Debug.Log($"[Unit] AI Decision: Idle (no valid actions)");
                break;
        }
    }
       
    private void Die()
    {
        Debug.Log($"[Unit] {gameObject.name} has been destroyed!");
        
        // UnitService에 사망을 알림 (아직 하지 않았다면)
        if (!deathNotificationSent)
        {
            NotifyUnitServiceOfDeath();
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// HealthComponent가 사망을 감지했을 때 호출되는 메서드
    /// </summary>
    public void OnHealthComponentDeath()
    {
        Debug.Log($"[Unit] {gameObject.name} received death notification from HealthComponent");

        // 적 유닛 처치 이벤트 발행 (점수 시스템 등에 알림)
        if (!IsPlayerUnit && enemyKilledChannel != null)
        {
            enemyKilledChannel.RaiseEvent();
            Debug.Log($"[Unit] Enemy killed event raised for {gameObject.name}");
        }

        // Effect: OnDeath 트리거
        TriggerEffects(EffectTrigger.OnDeath, new EffectContext());

        // DeathAnimationManager를 통한 죽음 처리
        if (deathAnimationManager != null)
        {
            Debug.Log($"[Unit] Requesting death animation for {gameObject.name}");

            // UnitService 사망 통지 (즉시 리스트에서 제거)
            NotifyUnitServiceOfDeath();

            // 사망 애니메이션 재생 (애니메이션 완료 후 자동 파괴)
            deathAnimationManager.ProcessUnitDeath(this, deathAnimationDuration);
        }
        else
        {
            Debug.LogWarning($"[Unit] DeathAnimationManager not found - using direct destruction for {gameObject.name}");
            NotifyUnitServiceOfDeath();
            Die();
        }
    }
    
    /// <summary>
    /// UnitService에 유닛 사망을 알려서 즉시 정리하도록 요청
    /// </summary>
    private void NotifyUnitServiceOfDeath()
    {
        if (deathNotificationSent)
        {
            Debug.Log($"[Unit] Death notification already sent for {gameObject.name}, skipping");
            return;
        }

        // Unit 사망 시 Effect 정리
        effectManager?.ClearAllEffects();

        // Unit 사망 시 currentTile 정리
        CleanupCurrentTile();

        // GameServiceManager를 통해 UnitService에 접근
        //var gameServiceManager = FindObjectOfType<GameServiceManager>();
        var gameServiceManager = ServiceLocator.Get<IGameServiceManager>();
        if (gameServiceManager != null)
        {
            Debug.Log($"[Unit] Notifying UnitService of {gameObject.name} death for immediate cleanup");
            gameServiceManager.UnregisterUnit(this);
            deathNotificationSent = true;
        }
        else
        {
            Debug.LogWarning($"[Unit] Could not find GameServiceManager to notify UnitService of {gameObject.name} death");
        }
    }

    /// <summary>
    /// 죽음 애니메이션 상태 설정 (DeathAnimationManager에서 호출)
    /// </summary>
    public void SetDeathAnimationState(bool isPlaying)
    {
        isPlayingDeathAnimation = isPlaying;

        if (isPlaying)
        {
            // 애니메이션 중 상호작용 불가능하도록 콜라이더 비활성화
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Debug.Log($"[Unit] Disabled collider for {gameObject.name} during death animation");
            }
        }
    }

    /// <summary>
    /// 죽음 시퀀스 완료 (DeathAnimationManager에서 호출)
    /// </summary>
    public void CompleteDeathSequence()
    {
        Debug.Log($"[Unit] Completing death sequence for {gameObject.name}");

        // 타일 정리 (아직 하지 않았다면)
        if (!deathNotificationSent)
        {
            CleanupCurrentTile();
        }

        // GameObject 파괴
        Destroy(gameObject);
    }

    /// <summary>
    /// GlobalStateManager의 Busy 상태 변경 이벤트 핸들러
    /// </summary>
    private void OnGlobalBusyStateChanged(BusyType type, bool isBusy)
    {
        // DeathAnimation이 끝났을 때만 처리
        if (type == BusyType.DeathAnimation && !isBusy)
        {
            // 대기 중인 행동 체인이 있으면 재개
            if (isWaitingForDeathAnimation)
            {
                Debug.Log($"[Unit] {gameObject.name} resuming action chain after death animation");
                isWaitingForDeathAnimation = false;
                ResumeActionChain();
            }
        }
    }

    /// <summary>
    /// 일시정지된 행동 체인 재개
    /// </summary>
    private void ResumeActionChain()
    {
        Debug.Log($"[Unit] {gameObject.name} resuming action chain after death animation");

        // OnActionCompleted의 정상 흐름 재개
        var result = currentActionResult;
        currentActionResult = null;

        if (result == null)
        {
            OnAllActionsCompleted();
            return;
        }

        if (result.ShouldContinueChain())
        {
            actionEvaluator.MoveToNextModifier();
            EvaluateAndExecuteNextAction();
        }
        else
        {
            OnAllActionsCompleted();
        }
    }

    /// <summary>
    /// Unit 사망 시 Grid 관련 데이터를 정리합니다.
    /// Clean Architecture: GridManager에게 모든 Grid 정리 작업을 위임
    /// - GridManager.RemoveUnit()이 GridState와 Tile 모두 정리
    /// - Unit은 자신의 참조만 정리
    /// </summary>
    private void CleanupCurrentTile()
    {
        if (gridManager != null)
        {
            Debug.Log($"[Unit] Requesting GridManager to cleanup {gameObject.name}");

            // GridManager에게 모든 Grid 관련 정리 위임
            // GridManager → GridState → Tile 정리 흐름
            bool removed = gridManager.RemoveUnit(gameObject);

            if (removed)
            {
                Debug.Log($"[Unit] Successfully removed {gameObject.name} from Grid (GridState + Tile cleaned)");
            }
            else
            {
                Debug.LogWarning($"[Unit] Failed to remove {gameObject.name} from Grid - unit may not have been tracked");
            }
        }
        else
        {
            Debug.LogWarning($"[Unit] Cannot cleanup grid for {gameObject.name} - gridManager is null");
        }

        // 자신의 참조만 정리
        currentTile = null;
    }
    
    // Helper methods for component system management
    public void EnableComponentSystem(bool enable)
    {
        useComponentSystem = enable;
        if (enable)
        {
            InitializeComponents();
        }
    }
    
    [ContextMenu("Initialize Components")]
    private void ForceInitializeComponents()
    {
        InitializeComponents();
        Debug.Log($"[Unit] Components initialized. System ready: {componentSystemReady}");
    }
    
    [ContextMenu("Toggle Component System")]
    private void ToggleComponentSystem()
    {
        EnableComponentSystem(!useComponentSystem);
        Debug.Log($"[Unit] Component system {(useComponentSystem ? "enabled" : "disabled")}");
    }
    
    [ContextMenu("Log Component Status")]
    private void LogComponentStatus()
    {
        Debug.Log($"=== Unit Component Status for {gameObject.name} ===");
        Debug.Log($"Use Component System: {useComponentSystem}");
        Debug.Log($"Component System Ready: {componentSystemReady}");
        Debug.Log($"Health Component: {healthComponent != null} ({healthComponent?.GetType().Name})");
        Debug.Log($"Combat Component: {combatComponent != null} ({combatComponent?.GetType().Name})");
        Debug.Log($"Movement Component: {movementComponent != null} ({movementComponent?.GetType().Name})");
        Debug.Log($"Team Component: {teamComponent != null} ({teamComponent?.GetType().Name})");
        Debug.Log($"AI Component: {unitAI != null} ({unitAI?.GetType().Name})");
        Debug.Log($"Current Stats - Health: {Health}/{MaxHealth}, Attack: {AttackPower}, Movement: {MovementRange}");
        Debug.Log($"Team: {(teamComponent?.Team.ToString() ?? "Legacy")} | Player Unit: {IsPlayerUnit}");
        Debug.Log($"Current Tile: {(currentTile != null ? $"{currentTile.name} ({currentTile.X}, {currentTile.Y})" : "NULL")}");
        Debug.Log($"Grid Position: ({X}, {Y})");
        Debug.Log($"Initialization Status - Initialized: {isInitialized}, Ready for Game: {IsReadyForGame}");
    }
    
    [ContextMenu("Force Update Current Tile")]
    private void ForceUpdateCurrentTile()
    {
        if (gridManager != null && gridManager.TryGetUnitPosition(gameObject, out Vector2Int position))
        {
            Debug.Log($"[Unit] Forcing currentTile update for {gameObject.name} at position ({position.x}, {position.y})");
            UpdateCurrentTile(position);
        }
        else
        {
            Debug.LogWarning($"[Unit] Cannot force update currentTile for {gameObject.name} - not found in GridManager");
        }
    }
    
    [ContextMenu("Test OnTurnStart")]
    private void TestOnTurnStart()
    {
        Debug.Log($"[Unit] Testing OnTurnStart for {gameObject.name}");
        OnTurnStart();
    }
    
    [ContextMenu("Test Death System")]
    private void TestDeathSystem()
    {
        Debug.Log($"[Unit] Testing death system for {gameObject.name}");
        if (useComponentSystem && healthComponent != null)
        {
            Debug.Log($"[Unit] Using component system - setting health to 0");
            healthComponent.TakeDamage(healthComponent.CurrentHealth + 100);
        }
        else
        {
            Debug.Log($"[Unit] Using legacy system - calling Die()");
            Die();
        }
    }
    
    /// <summary>
    /// Phase 2: 이벤트 구독 해제 - 메모리 누수 방지
    /// </summary>
    private void OnDestroy()
    {
        // Force release GameFlowLock if Unit destroyed mid-action
        globalStateManager?.SetIdle(this, BusyType.GameFlowLock);

        CleanupEventSubscriptions();
    }
    
    /// <summary>
    /// Phase 3: 이벤트 구독 정리 - Clean Architecture
    /// </summary>
    private void CleanupEventSubscriptions()
    {
        // GridManager 이벤트 구독 해제
        if (gridManager != null)
        {
            gridManager.OnUnitMoved -= OnUnitMovedInGrid;
        }

        // GlobalStateManager 이벤트 구독 해제
        if (globalStateManager != null)
        {
            globalStateManager.OnBusyStateChanged -= OnGlobalBusyStateChanged;
        }

        // HealthComponent 이벤트 구독 해제
        if (healthComponent != null)
        {
            var healthComp = healthComponent as HealthComponent;
            if (healthComp != null)
            {
                healthComp.OnDeath -= OnHealthComponentDeath;
            }
        }
    }
    
    // Validation for Editor
    private void OnValidate()
    {
        if (Application.isPlaying && useComponentSystem)
        {
            UpdateComponentSystemStatus();
        }
    }

    #region New Action System Integration

    /// <summary>
    /// 행동 수정자 추가 (새로운 Action System용)
    /// </summary>
    public void AddActionModifier(IActionModifier modifier)
    {
        if (modifier == null) return;
        actionEvaluator.AddModifier(modifier);
    }

    /// <summary>
    /// AI 턴 실행 (새로운 Action System 버전)
    /// </summary>
    public void ExecuteAITurn()
    {
        if (IsStunned())
        {
            Debug.Log($"[Unit] {gameObject.name} is stunned and skips its turn");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogError($"[Unit] {gameObject.name} cannot execute AI turn - gridManager is null");
            return;
        }

        // Set GameFlowLock before action chain starts
        globalStateManager?.SetBusy(this, BusyType.GameFlowLock, timeout: 15f);
        Debug.Log($"[Unit] {gameObject.name} set GameFlowLock for action chain execution");

        var myPosition = gridManager.GetUnitPosition(gameObject);
        currentActionContext = new ActionContext(myPosition);

        isExecutingAction = true;
        actionEvaluator.Reset();

        Debug.Log($"[Unit] {gameObject.name} ExecuteAITurn");

        EvaluateAndExecuteNextAction();
    }

    /// <summary>
    /// 다음 행동 평가 및 실행
    /// </summary>
    private void EvaluateAndExecuteNextAction()
    {
        ActionResult result = actionEvaluator.EvaluateNextAction(currentActionContext);

        if (result.IsSuccess)
        {
            currentActionResult = result;
            currentActionContext.ExecutedActions.Add(result.SelectedModifier);

            bool executed = executorRegistry.TryExecute(result, currentActionContext, this);

            if (!executed)
            {
                result.SelectedModifier.Execute(currentActionContext);
                OnActionCompleted();
            }
        }
        else
        {
            OnAllActionsCompleted();
        }
    }

    /// <summary>
    /// 행동 완료 콜백 (CombatComponent, MovementComponent에서 호출)
    /// </summary>
    public void OnActionCompleted()
    {
        // 죽음 애니메이션 중이면 행동 체인 일시정지
        if (globalStateManager != null && globalStateManager.IsBusy(BusyType.DeathAnimation))
        {
            Debug.Log($"[Unit] {gameObject.name} pausing action chain - death animation in progress");
            isWaitingForDeathAnimation = true;
            // currentActionResult 보존 (ResumeActionChain에서 사용)
            return;
        }

        // 로컬 복사본 생성 (다음 행동 시작 전에 현재 결과 보존)
        var result = currentActionResult;

        // 즉시 클리어 (다음 행동이 새 currentActionResult를 설정할 수 있도록)
        currentActionResult = null;

        if (result == null)
        {
            OnAllActionsCompleted();
            return;
        }

        if (result.ShouldContinueChain())
        {
            actionEvaluator.MoveToNextModifier();
            EvaluateAndExecuteNextAction();  // 새 currentActionResult 설정 가능
        }
        else
        {
            OnAllActionsCompleted();
        }
    }

    /// <summary>
    /// 모든 행동 완료 처리
    /// </summary>
    private void OnAllActionsCompleted()
    {
        isExecutingAction = false;
        currentActionResult = null;
        currentActionContext = null;
        actionEvaluator.Reset();

        // Release GameFlowLock after action chain completes
        globalStateManager?.SetIdle(this, BusyType.GameFlowLock);

        Debug.Log($"[Unit] {gameObject.name} completed all actions and released GameFlowLock");
    }

    #endregion

    #region Effect System Integration

    public void AddEffect(IEffect effect)
    {
        if (effect == null) return;
        effectManager?.AddEffect(effect);
    }

    public void RemoveEffect(IEffect effect)
    {
        if (effect == null) return;
        effectManager?.RemoveEffect(effect);
    }

    public void TriggerEffects(EffectTrigger trigger, EffectContext context)
    {
        if (effectManager == null) return;
        effectManager.TriggerEffects(trigger, context ?? new EffectContext());
    }

    public bool HasEffect(string effectName)
    {
        return effectManager != null && effectManager.HasEffect(effectName);
    }

    public bool HasEffect<T>() where T : class, Game.Core.Effects.IEffect
    {
        return effectManager != null && effectManager.HasEffect<T>();
    }

    public void AddStun(int turns)
    {
        if (turns <= 0) return;
        var stunEffect = new Game.Components.Abilities.StunStatusEffect(
            this,
            turns,
            EffectTrigger.OnTurnStart,
            priority: 999,
            durationTurns: turns
        );
        AddEffect(stunEffect);
        Debug.Log($"[Unit] {gameObject.name} stunned for {turns} turns");
    }

    public bool IsStunned()
    {
        // 스턴 상태 여부는 이름이 아닌 타입 기반으로 판정
        return HasEffect<Game.Components.Abilities.StunStatusEffect>();
    }

    #endregion
}
