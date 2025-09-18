using System.Linq;
using UnityEngine;
using Game.Interfaces;
using Game.Core;

/// <summary>
/// Phase 3 마이그레이션 도우미 클래스
/// 기존 코드를 Phase 3 Clean Architecture 패턴으로 마이그레이션하는 도구 및 가이드 제공
/// </summary>
public static class GridMigrationHelper
{
    /// <summary>
    /// 레거시 GridManager 참조를 새로운 인터페이스 기반 접근으로 마이그레이션
    /// </summary>
    [System.Obsolete("Direct GridManager access is deprecated. Use GetGridManager() instead.")]
    public static IGridManager MigrateGridManagerAccess()
    {
        // 기존: FindObjectOfType<GridManager>()
        // 새로운: ServiceLocator.Get<IGridManager>()
        
        var gridManager = ServiceLocator.Get<IGridManager>();
        if (gridManager == null)
        {
            Debug.LogError("[GridMigrationHelper] IGridManager not found in ServiceLocator. Ensure GridManager is initialized.");
        }
        
        return gridManager;
    }

    /// <summary>
    /// Phase 3 권장 방식: 그리드 매니저 접근
    /// </summary>
    public static IGridManager GetGridManager()
    {
        return ServiceLocator.Get<IGridManager>();
    }
    
    /// <summary>
    /// Phase 3 권장 방식: 그리드 서비스 접근
    /// </summary>
    public static IGridServices GetGridServices()
    {
        return ServiceLocator.Get<IGridServices>();
    }
    
    /// <summary>
    /// Phase 3 권장 방식: 그리드 상태 읽기 전용 접근
    /// </summary>
    public static IReadOnlyGridState GetGridState()
    {
        return ServiceLocator.Get<IReadOnlyGridState>();
    }
    
    /// <summary>
    /// Phase 3 권장 방식: 그리드 렌더러 접근
    /// </summary>
    public static IGridRenderer GetGridRenderer()
    {
        return ServiceLocator.Get<IGridRenderer>();
    }

    /// <summary>
    /// 컴포넌트의 그리드 의존성을 Phase 3 방식으로 초기화
    /// </summary>
    public static void InitializeGridDependencies(MonoBehaviour component)
    {
        if (component is IGridDependent gridDependent)
        {
            var gridServices = GetGridServices();
            if (gridServices != null)
            {
                gridDependent.Initialize(gridServices);
                Debug.Log($"[GridMigrationHelper] Initialized grid dependencies for {component.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"[GridMigrationHelper] Failed to initialize grid dependencies for {component.GetType().Name} - GridServices not available");
            }
        }
        else
        {
            Debug.LogWarning($"[GridMigrationHelper] Component {component.GetType().Name} does not implement IGridDependent");
        }
    }

    /// <summary>
    /// 씬 내 모든 그리드 의존성 컴포넌트를 Phase 3 방식으로 초기화
    /// </summary>
    [ContextMenu("Migrate All Grid Dependencies")]
    public static void MigrateAllGridDependencies()
    {
        var gridDependents = Object.FindObjectsOfType<MonoBehaviour>()
            .Where(mb => mb is IGridDependent);
        
        var gridServices = GetGridServices();
        if (gridServices == null)
        {
            Debug.LogError("[GridMigrationHelper] GridServices not available. Ensure GridManager is initialized.");
            return;
        }

        int migratedCount = 0;
        foreach (var dependent in gridDependents.Cast<IGridDependent>())
        {
            try
            {
                dependent.Initialize(gridServices);
                migratedCount++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GridMigrationHelper] Failed to migrate {dependent.GetType().Name}: {ex.Message}");
            }
        }
        
        Debug.Log($"[GridMigrationHelper] Successfully migrated {migratedCount} grid-dependent components to Phase 3");
    }

    /// <summary>
    /// 레거시 사용 패턴 검증 및 경고
    /// </summary>
    [ContextMenu("Validate Phase 3 Migration")]
    public static void ValidatePhase3Migration()
    {
        Debug.Log("=== Phase 3 Migration Validation ===");
        
        // ServiceLocator 상태 확인
        bool servicesAvailable = ServiceLocator.IsInitialized;
        Debug.Log($"🌐 ServiceLocator: {(servicesAvailable ? "✓ Initialized" : "✗ Not initialized")}");
        
        if (!servicesAvailable)
        {
            Debug.LogError("ServiceLocator not initialized. GridManager may not be running.");
            return;
        }
        
        // 핵심 서비스 확인
        var gridServices = ServiceLocator.Get<IGridServices>();
        var gridManager = ServiceLocator.Get<IGridManager>();
        var gridState = ServiceLocator.Get<IReadOnlyGridState>();
        var gridRenderer = ServiceLocator.Get<IGridRenderer>();
        
        Debug.Log($"📊 IGridServices: {(gridServices != null ? "✓" : "✗")}");
        Debug.Log($"🧠 IGridManager: {(gridManager != null ? "✓" : "✗")}");
        Debug.Log($"💾 IReadOnlyGridState: {(gridState != null ? "✓" : "✗")}");
        Debug.Log($"🎨 IGridRenderer: {(gridRenderer != null ? "✓" : "✗")}");
        
        // 레거시 컴포넌트 확인
        var legacyGridManagers = Object.FindObjectsOfType<MonoBehaviour>()
            .Where(mb => mb.GetType().Name.Contains("GridManager") && mb.GetType().Name != "GridManager");
        
        if (legacyGridManagers.Any())
        {
            Debug.LogWarning($"🚨 Found {legacyGridManagers.Count()} legacy GridManager components:");
            foreach (var legacy in legacyGridManagers)
            {
                Debug.LogWarning($"   - {legacy.GetType().Name} on {legacy.name}");
            }
        }
        
        // 그리드 의존성 컴포넌트 확인
        var gridDependents = Object.FindObjectsOfType<MonoBehaviour>()
            .OfType<IGridDependent>();
        
        Debug.Log($"🔗 Grid Dependent Components: {gridDependents.Count()}");
        
        // 성능 검증
        if (gridManager != null && gridState != null)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var testPosition = new Vector2Int(0, 0);
            
            // 간단한 성능 테스트
            for (int i = 0; i < 100; i++)
            {
                gridManager.IsValidPosition(testPosition);
            }
            
            timer.Stop();
            Debug.Log($"⚡ Performance: 100 IsValidPosition calls in {timer.ElapsedMilliseconds}ms");
        }
        
        Debug.Log("=== Migration Validation Complete ===");
    }

    /// <summary>
    /// Phase 3 사용 예제 출력
    /// </summary>
    [ContextMenu("Show Phase 3 Usage Examples")]
    public static void ShowUsageExamples()
    {
        Debug.Log(@"=== Phase 3 Grid System Usage Examples ===

// ✅ 권장: ServiceLocator를 통한 인터페이스 접근
var gridManager = ServiceLocator.Get<IGridManager>();
var gridServices = ServiceLocator.Get<IGridServices>();
var gridState = ServiceLocator.Get<IReadOnlyGridState>();

// ✅ 유닛 이동
if (gridManager.CanMoveUnit(unit, targetPosition))
{
    gridManager.MoveUnit(unit, targetPosition);
}

// ✅ 경로 탐색
var path = gridManager.FindPath(start, end, movingUnit);

// ✅ 범위 검색
var positions = gridManager.GetPositionsInRange(center, range);
var units = gridManager.GetUnitsInRange(center, range);

// ✅ 이벤트 구독
gridState.OnUnitMoved += (unit, oldPos, newPos) => {
    Debug.Log($'Unit moved from {oldPos} to {newPos}');
};

// ✅ 컴포넌트 초기화 (IGridDependent 구현)
public class MyGridUser : MonoBehaviour, IGridDependent
{
    private IGridManager gridManager;
    
    public void Initialize(IGridServices gridServices)
    {
        gridManager = gridServices.GridController;
        // 초기화 로직
    }
}

// ❌ 비권장: 직접 컴포넌트 접근
// var gridManager = FindObjectOfType<GridManager>(); // 사용 금지

=== Migration Helper Methods ===

// 자동 마이그레이션
GridMigrationHelper.MigrateAllGridDependencies();

// 마이그레이션 검증
GridMigrationHelper.ValidatePhase3Migration();

// 개별 컴포넌트 초기화
GridMigrationHelper.InitializeGridDependencies(this);
");
    }
}

/// <summary>
/// Phase 3 그리드 사용자를 위한 베이스 클래스 예제
/// </summary>
public abstract class GridUserBase : MonoBehaviour, IGridDependent
{
    protected IGridManager gridManager;
    protected IReadOnlyGridState gridState;
    protected IGridRenderer gridRenderer;
    
    public virtual void Initialize(IGridServices gridServices)
    {
        if (gridServices == null)
            throw new System.ArgumentNullException(nameof(gridServices));
            
        gridManager = gridServices.GridController;
        gridState = gridServices.GridState;
        gridRenderer = gridServices.GridRenderer;
        
        OnGridServicesInitialized();
    }
    
    protected virtual void OnGridServicesInitialized()
    {
        // 서브클래스에서 오버라이드하여 추가 초기화 수행
    }
    
    protected virtual void OnDestroy()
    {
        // 이벤트 구독 해제 등 정리 작업
    }
}