using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.Core;
using Game.Services;

public class UnitController : MonoBehaviour
{
    // Phase 3: Interface-based dependencies (Clean Architecture)
    [Inject(Required = false)]
    private IGridManager gridManager;
    
    [Inject(Required = false)] 
    private IGridServices gridServices;
     
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        // Phase 3: ServiceLocator-based dependency injection
        InitializeDependencies();      
    }
    
    /// <summary>
    /// Phase 3: ServiceLocator 기반 의존성 주입 (Clean Architecture)
    /// </summary>
    private void InitializeDependencies()
    {
        // ServiceLocator를 통한 의존성 주입
        this.InjectDependencies();
        
        // 현재 Phase 3 방식: ServiceLocator에서 직접 조회
        if (gridManager == null)
        {
            gridManager = ServiceLocator.Get<IGridManager>();
            if (gridManager == null)
            {
                Debug.LogError($"[UnitController] IGridManager not found in ServiceLocator. Please ensure GridManager is initialized first.");
                Debug.LogError($"[UnitController] GridManager should register itself through ServiceLocator.Register<IGridManager>() in Awake().");
            }
        }
        
        // Grid services 조회
        if (gridServices == null)
        {
            gridServices = ServiceLocator.Get<IGridServices>();
            if (gridServices == null)
            {
                Debug.LogError($"[UnitController] IGridServices not found in ServiceLocator. Please ensure GridManager is initialized first.");
            }
        }
        
        Debug.Log($"[UnitController] Dependencies initialized (Phase 3) - GridManager: {gridManager != null}, GridServices: {gridServices != null}");
    }
}