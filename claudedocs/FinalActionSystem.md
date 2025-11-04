# 리펙토링 완료 - 통합 Action 시스템 전체 코드

## 1. 인터페이스

### Game/Interfaces/IActionModifier.cs

```csharp
using UnityEngine;
using Game.Core;
using System.Collections.Generic;

namespace Game.Interfaces
{
    public enum ActionType
    {
        Attack,
        Movement,
        Idle,
        Special
    }
    
    public enum ChainBehavior
    {
        FallbackOnFailure,
        AlwaysContinue,
        AlwaysTerminate,
        ContinueOnSuccess
    }
    
    public interface IActionModifier
    {
        string ModifierName { get; }
        ActionType ActionType { get; }
        int Priority { get; }
        ChainBehavior ChainBehavior { get; }
        Unit Owner { get; }
        
        void SetNext(IActionModifier next);
        ActionResult Evaluate(ActionContext context);
        void Execute(ActionContext context);
    }
    
    public class ActionContext
    {
        public Vector2Int ActorPosition { get; set; }
        public ActionType CurrentActionType { get; set; }
        public List<Tile> AttackTiles { get; set; }
        public int AttackRange { get; set; }
        public bool HasClearRow { get; set; }
        public Vector2Int? TargetMovePosition { get; set; }
        public List<Vector2Int> MovePath { get; set; }
        public int MoveRange { get; set; }
        public List<IActionModifier> ExecutedActions { get; set; }
        
        public ActionContext(Vector2Int pos)
        {
            ActorPosition = pos;
            AttackTiles = new List<Tile>();
            MovePath = new List<Vector2Int>();
            ExecutedActions = new List<IActionModifier>();
        }
    }
    
    public class ActionResult
    {
        public bool IsSuccess { get; set; }
        public ActionType ActionType { get; set; }
        public ChainBehavior ChainBehavior { get; set; }
        public IActionModifier SelectedModifier { get; set; }
        public List<Tile> ValidTiles { get; set; }
        public Vector2Int? MoveDestination { get; set; }
        public List<Vector2Int> MovePath { get; set; }
        
        public ActionResult()
        {
            ValidTiles = new List<Tile>();
            MovePath = new List<Vector2Int>();
        }
        
        public bool ShouldContinueChain()
        {
            switch (ChainBehavior)
            {
                case ChainBehavior.FallbackOnFailure:
                    return !IsSuccess;
                case ChainBehavior.AlwaysContinue:
                    return true;
                case ChainBehavior.AlwaysTerminate:
                    return false;
                case ChainBehavior.ContinueOnSuccess:
                    return IsSuccess;
                default:
                    return false;
            }
        }
    }
}
```

### Game/Interfaces/IActionExecutor.cs

```csharp
using UnityEngine;

namespace Game.Interfaces
{
    public interface IActionExecutor
    {
        ActionType SupportedActionType { get; }
        void Execute(ActionResult result, ActionContext context, Unit owner);
    }
}
```

### Game/Interfaces/IAttackModifier.cs

```csharp
namespace Game.Interfaces
{
    public interface IAttackModifier : IActionModifier
    {
        int CalculateDamage(ActionContext context);
    }
}
```

### Game/Interfaces/IMovementModifier.cs

```csharp
using UnityEngine;

namespace Game.Interfaces
{
    public interface IMovementModifier : IActionModifier
    {
        int MaxMoveRange { get; }
        Vector2Int CalculateFinalDestination(Vector2Int intended, ActionContext context);
    }
}
```

### Game/Interfaces/IAttackEffect.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
using Game.Core;

namespace Game.Interfaces
{
    public interface IAttackEffect
    {
        string EffectName { get; }
        int Priority { get; }
        Unit Owner { get; }
        
        void ApplyEffectToTiles(List<Tile> primaryTiles, ActionContext context);
    }
}
```

## 2. 핵심 시스템

### Game/Core/ActionEvaluator.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using System.Collections.Generic;

namespace Game.Core
{
    public class ActionEvaluator
    {
        private List<IActionModifier> actionModifiers = new List<IActionModifier>();
        private IActionModifier actionModifierChain;
        private IActionModifier currentModifier;
        
        public void AddModifier(IActionModifier modifier)
        {
            if (modifier == null) return;
            actionModifiers.Add(modifier);
            RebuildChain();
        }
        
        private void RebuildChain()
        {
            if (actionModifiers.Count == 0) return;
            
            actionModifiers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            for (int i = 0; i < actionModifiers.Count - 1; i++)
                actionModifiers[i].SetNext(actionModifiers[i + 1]);
            
            actionModifierChain = actionModifiers[0];
        }
        
        public ActionResult EvaluateNextAction(ActionContext context)
        {
            if (currentModifier == null)
                currentModifier = actionModifierChain;
            
            if (currentModifier == null)
                return new ActionResult { IsSuccess = false };
            
            ActionResult result = currentModifier.Evaluate(context);
            
            if (!result.IsSuccess && result.ShouldContinueChain())
            {
                currentModifier = GetNextModifier(currentModifier);
                if (currentModifier != null)
                    return EvaluateNextAction(context);
            }
            
            return result;
        }
        
        public void MoveToNextModifier()
        {
            currentModifier = GetNextModifier(currentModifier);
        }
        
        public void Reset()
        {
            currentModifier = null;
        }
        
        private IActionModifier GetNextModifier(IActionModifier current)
        {
            int currentIndex = actionModifiers.IndexOf(current);
            return (currentIndex >= 0 && currentIndex < actionModifiers.Count - 1) 
                ? actionModifiers[currentIndex + 1] 
                : null;
        }
    }
}
```

### Game/Core/ActionExecutorRegistry.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using System.Collections.Generic;

namespace Game.Core
{
    public class ActionExecutorRegistry
    {
        private Dictionary<ActionType, IActionExecutor> executors = new Dictionary<ActionType, IActionExecutor>();
        
        public void RegisterExecutor(IActionExecutor executor)
        {
            if (executor == null) return;
            executors[executor.SupportedActionType] = executor;
            Debug.Log($"[Registry] {executor.SupportedActionType} Executor 등록");
        }
        
        public bool TryExecute(ActionResult result, ActionContext context, Unit owner)
        {
            if (executors.TryGetValue(result.ActionType, out var executor))
            {
                executor.Execute(result, context, owner);
                return true;
            }
            
            Debug.LogWarning($"[Registry] {result.ActionType} Executor 없음");
            return false;
        }
    }
}
```

### Game/Core/Executors/AttackActionExecutor.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Components;

namespace Game.Core.Executors
{
    public class AttackActionExecutor : IActionExecutor
    {
        public ActionType SupportedActionType => ActionType.Attack;
        
        public void Execute(ActionResult result, ActionContext context, Unit owner)
        {
            Debug.Log($"[AttackExecutor] 공격 실행");
            
            var combatComponent = owner.GetComponent<CombatComponent>();
            if (combatComponent != null)
                combatComponent.ExecuteAttackWithResult(result, context);
            else
                Debug.LogWarning($"[AttackExecutor] CombatComponent 없음");
        }
    }
}
```

### Game/Core/Executors/MovementActionExecutor.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Components;

namespace Game.Core.Executors
{
    public class MovementActionExecutor : IActionExecutor
    {
        public ActionType SupportedActionType => ActionType.Movement;
        
        public void Execute(ActionResult result, ActionContext context, Unit owner)
        {
            Debug.Log($"[MovementExecutor] 이동 실행");
            
            if (!result.MoveDestination.HasValue)
            {
                Debug.LogWarning($"[MovementExecutor] 이동 목적지 없음");
                return;
            }
            
            var movementComponent = owner.GetComponent<MovementComponent>();
            if (movementComponent != null)
                movementComponent.ExecuteMoveWithResult(result, context);
            else
                Debug.LogWarning($"[MovementExecutor] MovementComponent 없음");
        }
    }
}
```

## 3. 공격 수정자

### Game/Components/Abilities/AttackModifiers/SniperModifier.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class SniperModifier : IAttackModifier
    {
        public string ModifierName => "스나이퍼";
        public ActionType ActionType => ActionType.Attack;
        public int Priority { get; private set; }
        public ChainBehavior ChainBehavior => ChainBehavior.FallbackOnFailure;
        public Unit Owner { get; private set; }
        
        private IActionModifier nextModifier;
        private IGridManager gridManager;
        private ITeamComponent teamComponent;
        private ICombatSystem combatSystem;
        
        public SniperModifier(Unit owner, int priority = 100)
        {
            Owner = owner;
            Priority = priority;
            gridManager = ServiceLocator.Get<IGridManager>();
            teamComponent = owner.GetComponent<ITeamComponent>();
            combatSystem = owner.GetComponent<ICombatSystem>();
        }
        
        public void SetNext(IActionModifier next) => nextModifier = next;
        
        public ActionResult Evaluate(ActionContext context)
        {
            ActionResult result = new ActionResult
            {
                ActionType = ActionType.Attack,
                ChainBehavior = this.ChainBehavior
            };
            
            bool hasEnemyInRow = CheckEnemyInSameRow(context.ActorPosition);
            context.HasClearRow = !hasEnemyInRow;
            
            if (!hasEnemyInRow)
            {
                Tile enemyNexusTile = GetEnemyNexusTile();
                if (enemyNexusTile != null)
                {
                    result.ValidTiles.Add(enemyNexusTile);
                    result.IsSuccess = true;
                    result.SelectedModifier = this;
                }
            }
            else
            {
                result.IsSuccess = false;
            }
            
            return result;
        }
        
        public void Execute(ActionContext context) { }
        
        public int CalculateDamage(ActionContext context)
            => combatSystem?.CurrentAttackPower ?? 0;
        
        private bool CheckEnemyInSameRow(Vector2Int pos)
        {
            var gridController = gridManager.GetGridController();
            if (gridController == null) return false;
            var gridSize = gridManager.GridSize;
            
            for (int x = 0; x < gridSize.x; x++)
            {
                if (x == pos.x) continue;
                var tile = gridController.GetTileAtPosition(new Vector2Int(x, pos.y));
                if (tile == null) continue;
                
                bool hasUnit = tile.OccupyingUnit != null && tile.OccupyingUnit.IsAlive;
                bool hasBase = tile.OccupyingBase != null && tile.OccupyingBase.IsAlive;
                
                if ((hasUnit || hasBase) && IsEnemy(hasUnit ? tile.OccupyingUnit.gameObject : tile.OccupyingBase.gameObject))
                    return true;
            }
            return false;
        }
        
        private Tile GetEnemyNexusTile()
        {
            var gridController = gridManager.GetGridController();
            if (gridController == null) return null;
            var gridSize = gridManager.GridSize;
            
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var tile = gridController.GetTileAtPosition(new Vector2Int(x, y));
                    if (tile?.OccupyingBase != null && tile.OccupyingBase.IsAlive && IsEnemy(tile.OccupyingBase.gameObject))
                        return tile;
                }
            }
            return null;
        }
        
        private bool IsEnemy(GameObject target)
        {
            if (teamComponent == null) return true;
            var targetTeam = target.GetComponent<ITeamComponent>();
            return targetTeam == null ? true : teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
        }
    }
}
```

### Game/Components/Abilities/AttackModifiers/RangedModifier.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class RangedModifier : IAttackModifier
    {
        public string ModifierName { get; private set; }
        public ActionType ActionType => ActionType.Attack;
        public int Priority { get; private set; }
        public virtual ChainBehavior ChainBehavior => ChainBehavior.AlwaysTerminate;
        public Unit Owner { get; private set; }
        
        protected int range;
        private IActionModifier nextModifier;
        private IGridManager gridManager;
        private ITeamComponent teamComponent;
        private ICombatSystem combatSystem;
        
        public RangedModifier(Unit owner, int range, int priority = 90)
        {
            Owner = owner;
            this.range = range;
            Priority = priority;
            ModifierName = $"원거리({range})";
            gridManager = ServiceLocator.Get<IGridManager>();
            teamComponent = owner.GetComponent<ITeamComponent>();
            combatSystem = owner.GetComponent<ICombatSystem>();
        }
        
        public void SetNext(IActionModifier next) => nextModifier = next;
        
        public ActionResult Evaluate(ActionContext context)
        {
            ActionResult result = new ActionResult
            {
                ActionType = ActionType.Attack,
                ChainBehavior = this.ChainBehavior
            };
            
            context.AttackRange = range;
            List<Tile> tilesInRange = FindTilesInRange(context.ActorPosition);
            
            if (tilesInRange.Count > 0)
            {
                result.ValidTiles = tilesInRange;
                result.IsSuccess = true;
                result.SelectedModifier = this;
            }
            else
            {
                result.IsSuccess = false;
            }
            
            return result;
        }
        
        public void Execute(ActionContext context) { }
        
        public int CalculateDamage(ActionContext context)
            => combatSystem?.CurrentAttackPower ?? 0;
        
        private List<Tile> FindTilesInRange(Vector2Int attackerPos)
        {
            List<Tile> tiles = new List<Tile>();
            var gridController = gridManager.GetGridController();
            if (gridController == null) return tiles;
            
            int direction = teamComponent?.Team == TeamType.Player ? 1 : -1;
            
            for (int i = 1; i <= range; i++)
            {
                Vector2Int checkPos = new Vector2Int(attackerPos.x, attackerPos.y + (direction * i));
                var tile = gridController.GetTileAtPosition(checkPos);
                if (tile == null) continue;
                
                bool hasUnit = tile.OccupyingUnit != null && tile.OccupyingUnit.IsAlive;
                bool hasBase = tile.OccupyingBase != null && tile.OccupyingBase.IsAlive;
                
                if (hasUnit || hasBase)
                {
                    var targetObj = hasUnit ? tile.OccupyingUnit.gameObject : tile.OccupyingBase.gameObject;
                    if (IsEnemy(targetObj))
                    {
                        tiles.Add(tile);
                        break;
                    }
                }
            }
            return tiles;
        }
        
        private bool IsEnemy(GameObject target)
        {
            if (teamComponent == null) return true;
            var targetTeam = target.GetComponent<ITeamComponent>();
            return targetTeam == null ? true : teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
        }
    }
    
    public class RangedContinueModifier : RangedModifier
    {
        public override ChainBehavior ChainBehavior => ChainBehavior.AlwaysContinue;
        public RangedContinueModifier(Unit owner, int range, int priority = 90) 
            : base(owner, range, priority) { }
    }
}
```

### Game/Components/Abilities/AttackModifiers/MeleeModifier.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class MeleeModifier : IAttackModifier
    {
        public string ModifierName => "근접";
        public ActionType ActionType => ActionType.Attack;
        public int Priority { get; private set; }
        public ChainBehavior ChainBehavior => ChainBehavior.AlwaysTerminate;
        public Unit Owner { get; private set; }
        
        private IActionModifier nextModifier;
        private IGridManager gridManager;
        private ITeamComponent teamComponent;
        private ICombatSystem combatSystem;
        
        public MeleeModifier(Unit owner, int priority = 80)
        {
            Owner = owner;
            Priority = priority;
            gridManager = ServiceLocator.Get<IGridManager>();
            teamComponent = owner.GetComponent<ITeamComponent>();
            combatSystem = owner.GetComponent<ICombatSystem>();
        }
        
        public void SetNext(IActionModifier next) => nextModifier = next;
        
        public ActionResult Evaluate(ActionContext context)
        {
            ActionResult result = new ActionResult
            {
                ActionType = ActionType.Attack,
                ChainBehavior = this.ChainBehavior
            };
            
            List<Tile> adjacentTiles = FindAdjacentEnemyTiles(context.ActorPosition);
            
            if (adjacentTiles.Count > 0)
            {
                result.ValidTiles = adjacentTiles;
                result.IsSuccess = true;
                result.SelectedModifier = this;
            }
            else
            {
                result.IsSuccess = false;
            }
            
            return result;
        }
        
        public void Execute(ActionContext context) { }
        
        public int CalculateDamage(ActionContext context)
            => combatSystem?.CurrentAttackPower ?? 0;
        
        private List<Tile> FindAdjacentEnemyTiles(Vector2Int pos)
        {
            List<Tile> tiles = new List<Tile>();
            var gridController = gridManager.GetGridController();
            if (gridController == null) return tiles;
            
            Vector2Int[] adjacentPositions = new Vector2Int[]
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1),
            };
            
            foreach (Vector2Int adjPos in adjacentPositions)
            {
                var tile = gridController.GetTileAtPosition(adjPos);
                if (tile == null) continue;
                
                bool hasUnit = tile.OccupyingUnit != null && tile.OccupyingUnit.IsAlive;
                bool hasBase = tile.OccupyingBase != null && tile.OccupyingBase.IsAlive;
                
                if (hasUnit || hasBase)
                {
                    var targetObj = hasUnit ? tile.OccupyingUnit.gameObject : tile.OccupyingBase.gameObject;
                    if (IsEnemy(targetObj))
                        tiles.Add(tile);
                }
            }
            
            return tiles;
        }
        
        private bool IsEnemy(GameObject target)
        {
            if (teamComponent == null) return true;
            var targetTeam = target.GetComponent<ITeamComponent>();
            return targetTeam == null ? true : teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
        }
    }
}
```

## 4. 이동 수정자

### Game/Components/Abilities/MovementModifiers/NormalMoveModifier.cs

```csharp
using UnityEngine;
using Game.Interfaces;

namespace Game.Components.Abilities
{
    public class NormalMoveModifier : IMovementModifier
    {
        public string ModifierName => "일반 이동";
        public ActionType ActionType => ActionType.Movement;
        public int Priority { get; private set; }
        public ChainBehavior ChainBehavior => ChainBehavior.AlwaysTerminate;
        public Unit Owner { get; private set; }
        public int MaxMoveRange { get; private set; }
        
        private IActionModifier nextModifier;
        private IGridManager gridManager;
        
        public NormalMoveModifier(Unit owner, int moveRange = 1, int priority = 10)
        {
            Owner = owner;
            MaxMoveRange = moveRange;
            Priority = priority;
            gridManager = ServiceLocator.Get<IGridManager>();
        }
        
        public void SetNext(IActionModifier next) => nextModifier = next;
        
        public ActionResult Evaluate(ActionContext context)
        {
            ActionResult result = new ActionResult
            {
                ActionType = ActionType.Movement,
                ChainBehavior = this.ChainBehavior
            };
            
            context.MoveRange = MaxMoveRange;
            Vector2Int? moveTarget = FindMoveTarget(context.ActorPosition);
            
            if (moveTarget.HasValue)
            {
                result.IsSuccess = true;
                result.MoveDestination = moveTarget.Value;
                result.SelectedModifier = this;
            }
            else
            {
                result.IsSuccess = false;
            }
            
            return result;
        }
        
        public void Execute(ActionContext context) { }
        
        public Vector2Int CalculateFinalDestination(Vector2Int intended, ActionContext context)
            => intended;
        
        private Vector2Int? FindMoveTarget(Vector2Int currentPos)
        {
            Vector2Int forward = new Vector2Int(currentPos.x, currentPos.y + 1);
            if (gridManager.IsValidPosition(forward) && !gridManager.IsBlocked(forward))
                return forward;
            
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(currentPos.x + 1, currentPos.y),
                new Vector2Int(currentPos.x - 1, currentPos.y),
                new Vector2Int(currentPos.x, currentPos.y - 1),
            };
            
            foreach (var dir in directions)
                if (gridManager.IsValidPosition(dir) && !gridManager.IsBlocked(dir))
                    return dir;
            
            return null;
        }
    }
}
```

### Game/Components/Abilities/MovementModifiers/BoosterModifier.cs

```csharp
using UnityEngine;
using Game.Interfaces;

namespace Game.Components.Abilities
{
    public class BoosterModifier : IMovementModifier
    {
        public string ModifierName => "부스터";
        public ActionType ActionType => ActionType.Movement;
        public int Priority { get; private set; }
        public ChainBehavior ChainBehavior => ChainBehavior.AlwaysTerminate;
        public Unit Owner { get; private set; }
        public int MaxMoveRange => 99;
        
        private IActionModifier nextModifier;
        private IGridManager gridManager;
        
        public BoosterModifier(Unit owner, int priority = 20)
        {
            Owner = owner;
            Priority = priority;
            gridManager = ServiceLocator.Get<IGridManager>();
        }
        
        public void SetNext(IActionModifier next) => nextModifier = next;
        
        public ActionResult Evaluate(ActionContext context)
        {
            ActionResult result = new ActionResult
            {
                ActionType = ActionType.Movement,
                ChainBehavior = this.ChainBehavior
            };
            
            Vector2Int? moveTarget = FindMoveTarget(context.ActorPosition);
            
            if (moveTarget.HasValue)
            {
                result.IsSuccess = true;
                result.MoveDestination = moveTarget.Value;
                result.SelectedModifier = this;
            }
            else
            {
                result.IsSuccess = false;
            }
            
            return result;
        }
        
        public void Execute(ActionContext context) { }
        
        public Vector2Int CalculateFinalDestination(Vector2Int intended, ActionContext context)
        {
            Vector2Int direction = new Vector2Int(
                Mathf.Clamp(intended.x - context.ActorPosition.x, -1, 1),
                Mathf.Clamp(intended.y - context.ActorPosition.y, -1, 1)
            );
            
            Vector2Int finalPos = context.ActorPosition;
            
            while (true)
            {
                Vector2Int nextPos = new Vector2Int(
                    finalPos.x + direction.x,
                    finalPos.y + direction.y
                );
                
                if (!gridManager.IsValidPosition(nextPos) || gridManager.IsBlocked(nextPos))
                    break;
                
                finalPos = nextPos;
            }
            
            return finalPos;
        }
        
        private Vector2Int? FindMoveTarget(Vector2Int currentPos)
        {
            Vector2Int forward = new Vector2Int(currentPos.x, currentPos.y + 1);
            return gridManager.IsValidPosition(forward) ? forward : (Vector2Int?)null;
        }
    }
}
```

## 5. 공격 효과

### Game/Components/Abilities/AttackEffects/SplashEffect.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class SplashEffect : IAttackEffect
    {
        public string EffectName { get; private set; }
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        
        private int splashDamage;
        private IGridManager gridManager;
        private ITeamComponent teamComponent;
        
        public SplashEffect(Unit owner, int damage, int priority = 10)
        {
            Owner = owner;
            splashDamage = damage;
            Priority = priority;
            EffectName = $"방사({damage})";
            gridManager = ServiceLocator.Get<IGridManager>();
            teamComponent = owner.GetComponent<ITeamComponent>();
        }
        
        public void ApplyEffectToTiles(List<Tile> primaryTiles, ActionContext context)
        {
            var gridController = gridManager.GetGridController();
            if (gridController == null) return;
            
            foreach (var primaryTile in primaryTiles)
            {
                if (primaryTile == null) continue;
                
                Vector2Int[] splashPositions = new Vector2Int[]
                {
                    new Vector2Int(primaryTile.X, primaryTile.Y + 1),
                    new Vector2Int(primaryTile.X, primaryTile.Y - 1)
                };
                
                foreach (Vector2Int splashPos in splashPositions)
                {
                    var tile = gridController.GetTileAtPosition(splashPos);
                    if (tile == null) continue;
                    
                    var targetHealth = tile.GetDamageableTarget();
                    if (targetHealth != null && targetHealth.IsAlive && IsEnemy(targetHealth.gameObject))
                        targetHealth.TakeDamage(splashDamage);
                }
            }
        }
        
        private bool IsEnemy(GameObject target)
        {
            if (teamComponent == null) return true;
            var targetTeam = target.GetComponent<ITeamComponent>();
            return targetTeam == null ? true : teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
        }
    }
}
```

### Game/Components/Abilities/AttackEffects/StunEffect.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class StunEffect : IAttackEffect
    {
        public string EffectName => "스턴";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        
        private int stunDuration;
        
        public StunEffect(Unit owner, int duration = 1, int priority = 5)
        {
            Owner = owner;
            stunDuration = duration;
            Priority = priority;
        }
        
        public void ApplyEffectToTiles(List<Tile> primaryTiles, ActionContext context)
        {
            foreach (var tile in primaryTiles)
            {
                if (tile == null) continue;
                
                var targetHealth = tile.GetDamageableTarget();
                if (targetHealth != null && targetHealth.IsAlive)
                {
                    var targetUnit = targetHealth.gameObject.GetComponent<Unit>();
                    if (targetUnit != null)
                        targetUnit.AddStun(stunDuration);
                }
            }
        }
    }
}
```

## 6. 컴포넌트

### Game/Components/CombatComponent.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using Game.Data;
using System.Collections.Generic;

namespace Game.Components
{
    public class CombatComponent : MonoBehaviour, IAdvancedCombatSystem
    {
        [Header("기본 공격 설정")]
        [SerializeField] private int baseAttackPower = 10;
        [SerializeField] private float criticalChance = 0.1f;
        [SerializeField] private float criticalMultiplier = 2f;
        [SerializeField] private DamageType attackType = DamageType.Physical;
        
        private List<IAttackEffect> attackEffects = new List<IAttackEffect>();
        private bool isAttacking = false;
        private ActionResult currentAttackResult;
        private ActionContext currentAttackContext;
        
        private IGridManager gridManager;
        private IAnimationController animationController;
        
        public event System.Action<GameObject, CombatResult> OnAttackPerformed;
        public event System.Action<GameObject, CombatResult> OnCriticalAttack;
        
        private void Awake()
        {
            gridManager = ServiceLocator.Get<IGridManager>();
            animationController = GetComponent<IAnimationController>();
        }
        
        public void AddAttackEffect(IAttackEffect effect)
        {
            if (effect == null) return;
            attackEffects.Add(effect);
            attackEffects.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
        
        public void ExecuteAttackWithResult(ActionResult result, ActionContext context)
        {
            if (isAttacking || result == null) return;
            
            isAttacking = true;
            currentAttackResult = result;
            currentAttackContext = context;
            
            if (animationController != null)
                animationController.PlayAttackAnimation();
            else
            {
                ApplyCurrentAttackDamage();
                OnAttackCompleted();
            }
        }
        
        public void OnAnimationAttackHit()
        {
            ApplyCurrentAttackDamage();
        }
        
        private void ApplyCurrentAttackDamage()
        {
            if (currentAttackResult == null || currentAttackResult.SelectedModifier == null)
                return;
            
            var attackModifier = currentAttackResult.SelectedModifier as IAttackModifier;
            if (attackModifier == null) return;
            
            int modifierDamage = attackModifier.CalculateDamage(currentAttackContext);
            
            foreach (var tile in currentAttackResult.ValidTiles)
            {
                if (tile == null) continue;
                
                var targetHealth = tile.GetDamageableTarget();
                if (targetHealth != null && targetHealth.IsAlive)
                    ApplyDamageToTarget(targetHealth.gameObject, modifierDamage);
            }
            
            ApplyAttackEffects();
        }
        
        private void ApplyDamageToTarget(GameObject target, int baseDamage)
        {
            var targetHealth = target.GetComponent<IHealthComponent>();
            if (targetHealth == null) return;
            
            bool isCritical = RollCritical();
            int finalDamage = isCritical ? Mathf.RoundToInt(baseDamage * criticalMultiplier) : baseDamage;
            
            targetHealth.TakeDamage(finalDamage);
            
            var result = CombatResult.Hit(finalDamage, target, attackType, isCritical, "Ability");
            OnAttackPerformed?.Invoke(target, result);
            
            if (isCritical)
                OnCriticalAttack?.Invoke(target, result);
        }
        
        private bool RollCritical() => UnityEngine.Random.value < criticalChance;
        
        private void ApplyAttackEffects()
        {
            if (attackEffects.Count == 0 || currentAttackResult == null) return;
            
            foreach (IAttackEffect effect in attackEffects)
                effect.ApplyEffectToTiles(currentAttackResult.ValidTiles, currentAttackContext);
        }
        
        public void OnAnimationAttackEnd()
        {
            OnAttackCompleted();
        }
        
        private void OnAttackCompleted()
        {
            isAttacking = false;
            var unit = GetComponent<Unit>();
            if (unit != null)
                unit.OnActionCompleted();
            
            currentAttackResult = null;
            currentAttackContext = null;
        }
        
        public int CurrentAttackPower => baseAttackPower;
        public bool CanAttack => !isAttacking;
    }
}
```

### Game/Components/MovementComponent.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;

namespace Game.Components
{
    public class MovementComponent : MonoBehaviour, IMovementSystem
    {
        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = 5f;
        
        private bool isMoving = false;
        private ActionResult currentMoveResult;
        private ActionContext currentMoveContext;
        
        private IGridManager gridManager;
        private IAnimationController animationController;
        
        public event System.Action<Vector2Int> OnMovementCompleted;
        
        private void Awake()
        {
            gridManager = ServiceLocator.Get<IGridManager>();
            animationController = GetComponent<IAnimationController>();
        }
        
        public void ExecuteMoveWithResult(ActionResult result, ActionContext context)
        {
            if (isMoving || !result.MoveDestination.HasValue)
            {
                OnMoveCompleted();
                return;
            }
            
            isMoving = true;
            currentMoveResult = result;
            currentMoveContext = context;
            
            var moveModifier = result.SelectedModifier as IMovementModifier;
            Vector2Int finalDestination = result.MoveDestination.Value;
            
            if (moveModifier != null)
                finalDestination = moveModifier.CalculateFinalDestination(result.MoveDestination.Value, context);
            
            currentMoveContext.TargetMovePosition = finalDestination;
            MoveTo(finalDestination);
        }
        
        private void MoveTo(Vector2Int newPosition)
        {
            var currentPos = gridManager.GetUnitPosition(gameObject);
            
            gridManager.RemoveUnit(currentPos);
            transform.position = gridManager.GridToWorld(newPosition);
            gridManager.RegisterUnit(gameObject, newPosition);
            
            if (animationController != null)
                animationController.PlayMoveAnimation();
            else
                OnMoveCompleted();
        }
        
        public void OnAnimationMoveEnd()
        {
            OnMoveCompleted();
        }
        
        private void OnMoveCompleted()
        {
            isMoving = false;
            
            if (currentMoveContext?.TargetMovePosition.HasValue == true)
                OnMovementCompleted?.Invoke(currentMoveContext.TargetMovePosition.Value);
            
            var unit = GetComponent<Unit>();
            if (unit != null)
                unit.OnActionCompleted();
            
            currentMoveResult = null;
            currentMoveContext = null;
        }
        
        public bool CanMove => !isMoving;
    }
}
```

## 7. Unit 클래스

### Game/Unit.cs

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using Game.Core.Executors;

public class Unit : MonoBehaviour
{
    private ActionEvaluator actionEvaluator;
    private ActionExecutorRegistry executorRegistry;
    
    private bool isExecutingAction = false;
    private ActionResult currentActionResult;
    private ActionContext currentActionContext;
    
    private int stunTurns = 0;
    
    private IGridManager gridManager;
    
    private void Awake()
    {
        gridManager = ServiceLocator.Get<IGridManager>();
        
        actionEvaluator = new ActionEvaluator();
        
        executorRegistry = new ActionExecutorRegistry();
        executorRegistry.RegisterExecutor(new AttackActionExecutor());
        executorRegistry.RegisterExecutor(new MovementActionExecutor());
    }
    
    public void AddActionModifier(IActionModifier modifier)
    {
        if (modifier == null) return;
        actionEvaluator.AddModifier(modifier);
    }
    
    public void ExecuteAITurn()
    {
        if (stunTurns > 0)
        {
            stunTurns--;
            return;
        }
        
        var myPosition = gridManager.GetUnitPosition(gameObject);
        currentActionContext = new ActionContext(myPosition);
        
        isExecutingAction = true;
        actionEvaluator.Reset();
        
        EvaluateAndExecuteNextAction();
    }
    
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
    
    public void OnActionCompleted()
    {
        if (currentActionResult == null)
        {
            OnAllActionsCompleted();
            return;
        }
        
        if (currentActionResult.ShouldContinueChain())
        {
            actionEvaluator.MoveToNextModifier();
            currentActionResult = null;
            EvaluateAndExecuteNextAction();
        }
        else
        {
            OnAllActionsCompleted();
        }
    }
    
    private void OnAllActionsCompleted()
    {
        isExecutingAction = false;
        currentActionResult = null;
        currentActionContext = null;
        actionEvaluator.Reset();
    }
    
    public void AddStun(int turns)
    {
        stunTurns = Mathf.Max(stunTurns, turns);
    }
}
```

## 8. 사용 예제

### UnitFactory.cs

```csharp
using UnityEngine;
using Game.Components.Abilities;

public class UnitFactory : MonoBehaviour
{
    [SerializeField] private GameObject unitPrefab;
    
    public Unit CreateNormalUnit()
    {
        var unitGO = Instantiate(unitPrefab);
        var unit = unitGO.GetComponent<Unit>();
        
        unit.AddActionModifier(new SniperModifier(unit, 100));
        unit.AddActionModifier(new RangedModifier(unit, 3, 90));
        unit.AddActionModifier(new MeleeModifier(unit, 80));
        unit.AddActionModifier(new NormalMoveModifier(unit, 1, 10));
        
        return unit;
    }
    
    public Unit CreateMobileUnit()
    {
        var unitGO = Instantiate(unitPrefab);
        var unit = unitGO.GetComponent<Unit>();
        
        unit.AddActionModifier(new RangedContinueModifier(unit, 3, 100));
        unit.AddActionModifier(new NormalMoveModifier(unit, 1, 10));
        
        return unit;
    }
    
    public Unit CreateBoosterUnit()
    {
        var unitGO = Instantiate(unitPrefab);
        var unit = unitGO.GetComponent<Unit>();
        
        unit.AddActionModifier(new RangedModifier(unit, 2, 100));
        unit.AddActionModifier(new BoosterModifier(unit, 20));
        unit.AddActionModifier(new NormalMoveModifier(unit, 1, 10));
        
        return unit;
    }
    
    public Unit CreateSplashUnit()
    {
        var unitGO = Instantiate(unitPrefab);
        var unit = unitGO.GetComponent<Unit>();
        
        unit.AddActionModifier(new RangedModifier(unit, 3, 100));
        unit.AddActionModifier(new MeleeModifier(unit, 90));
        
        var combatComp = unit.GetComponent<CombatComponent>();
        if (combatComp != null)
        {
            combatComp.AddAttackEffect(new SplashEffect(unit, 10));
            combatComp.AddAttackEffect(new StunEffect(unit, 1));
        }
        
        unit.AddActionModifier(new NormalMoveModifier(unit, 1, 10));
        
        return unit;
    }
}
```
