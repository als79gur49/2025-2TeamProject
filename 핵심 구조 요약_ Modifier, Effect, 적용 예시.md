<img src="https://r2cdn.perplexity.ai/pplx-full-logo-primary-dark%402x.png" style="height:64px;margin-right:32px"/>

# 핵심 구조 요약: Modifier, Effect, 적용 예시

패턴 2(Unit별 Effect ― 모든 Modifier에 공통 Effect 적용)를 기준으로, 실제 사용하는 핵심 코드 구조와 단일 Modifier, 단일 Effect 구현 예시를 아래에 집약합니다.

***

## 1. Modifier 구조

### 1-1. ModifierData (ScriptableObject)

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;

namespace Game.Data
{
    public abstract class ModifierData : ScriptableObject
    {
        [Header("기본 설정")]
        public string modifierName = "Unnamed Modifier";
        [Range(0, 200)]
        public int priority = 50;
        public ChainBehavior chainBehavior = ChainBehavior.AlwaysContinue;
        public ActionType actionType;

        public abstract IActionModifier CreateModifier(Unit owner);
    }

    [CreateAssetMenu(fileName = "NewRangedModifier", menuName = "Modifiers/Attack/Ranged")]
    public class RangedModifierData : ModifierData
    {
        [Header("원거리 설정")]
        [Range(1, 10)]
        public int attackRange = 3;

        public override IActionModifier CreateModifier(Unit owner)
        {
            return new RangedModifier(owner, attackRange, priority);
        }
    }
}
```


### 1-2. Modifier (로직 클래스)

```csharp
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
        public virtual ChainBehavior ChainBehavior => ChainBehavior.AlwaysContinue;
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
            List<Tile> targetTiles = FindTilesInRange();

            if (targetTiles.Count > 0)
            {
                foreach (var tile in targetTiles)
                {
                    var damageResult = CalculateDamage(tile);
                    if (damageResult.HasValue)
                        tile.GetDamageableTarget()?.TakeDamage(damageResult.Value);
                }

                return new ActionResult
                {
                    ActionType = ActionType.Attack,
                    ChainBehavior = this.ChainBehavior,
                    TargetTiles = targetTiles,
                    IsSuccess = true
                };
            }

            return nextModifier?.Evaluate(context) ?? new ActionResult { IsSuccess = false };
        }

        private List<Tile> FindTilesInRange()
        {
            // ... 타겟 검색 로직
            return new List<Tile>();
        }
        private int? CalculateDamage(Tile targetTile)
        {
            // ... 데미지 계산 로직
            return 10;
        }
    }
}
```


***

## 2. Effect 구조

### 2-1. EffectData (ScriptableObject)

```csharp
using UnityEngine;
using Game.Interfaces;
using Game.Core;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewBleedEffect", menuName = "Effects/Attack/Bleed")]
    public class BleedEffectData : EffectData
    {
        [Header("출혈 설정")]
        [Range(1, 20)]
        public int damagePerTurn = 3;
        [Range(1, 10)]
        public int durationTurns = 3;

        public override IAttackEffect CreateEffect(Unit owner)
        {
            return new BleedEffect(owner, damagePerTurn, durationTurns, priority);
        }
    }
}
```


### 2-2. Effect (로직 클래스)

```csharp
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components.Abilities
{
    public class BleedEffect : IAttackEffect
    {
        public string EffectName { get; private set; }
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }

        private int damagePerTurn;
        private int durationTurns;

        public BleedEffect(Unit owner, int damage, int duration, int priority)
        {
            Owner = owner;
            damagePerTurn = damage;
            durationTurns = duration;
            Priority = priority;
            EffectName = $"출혈({damage}x{duration})";
        }

        public void ApplyEffectToTiles(List<Tile> primaryTiles, ActionContext context)
        {
            if (primaryTiles == null) return;

            foreach (var tile in primaryTiles)
            {
                if (tile == null) continue;
                var targetHealth = tile.GetDamageableTarget();
                if (targetHealth != null && targetHealth.IsAlive)
                {
                    var targetUnit = targetHealth.gameObject.GetComponent<Unit>();
                    if (targetUnit != null)
                        targetUnit.AddBleed(damagePerTurn, durationTurns);
                }
            }
        }
    }
}
```


***

## 3. Unit - AttackModifier와 Effect 통합 방식

```csharp
using UnityEngine;
using Game.Data;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;

namespace Game.Components
{
    public partial class Unit : MonoBehaviour
    {
        [Header("Modifier 구성")]
        [SerializeField] private ModifierPresetData modifierPreset;

        [Header("Attack Effects (모든 공격에 적용)")]
        [SerializeField]
        private List<EffectData> attackEffects = new List<EffectData>();

        private ActionEvaluator attackEvaluator;
        private List<IAttackEffect> runtimeAttackEffects = new List<IAttackEffect>();

        private void Start()
        {
            InitializeModifiers();
            InitializeAttackEffects();
        }

        private void InitializeModifiers()
        {
            attackEvaluator = new ActionEvaluator();
            if (modifierPreset != null && modifierPreset.Validate())
                modifierPreset.ApplyToUnit(this, attackEvaluator, null);
        }

        private void InitializeAttackEffects()
        {
            runtimeAttackEffects.Clear();
            if (attackEffects != null)
            {
                foreach (var effectData in attackEffects)
                {
                    if (effectData != null)
                        runtimeAttackEffects.Add(effectData.CreateEffect(this));
                }
                runtimeAttackEffects.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }

        public void ExecuteAttack(ActionContext context)
        {
            ActionResult result = attackEvaluator.EvaluateNextAction(context);
            if (result.IsSuccess && result.TargetTiles != null && result.TargetTiles.Count > 0)
                ApplyAttackEffects(result.TargetTiles, context);
        }

        private void ApplyAttackEffects(List<Tile> targetTiles, ActionContext context)
        {
            foreach (var effect in runtimeAttackEffects)
                effect.ApplyEffectToTiles(targetTiles, context);
        }
    }
}
```


***

## 4. 인터페이스

```csharp
namespace Game.Interfaces
{
    public interface IAttackEffect
    {
        string EffectName { get; }
        int Priority { get; }
        Unit Owner { get; }
        void ApplyEffectToTiles(List<Tile> primaryTiles, ActionContext context);
    }

    public interface IAttackModifier : IActionModifier
    {
        string ModifierName { get; }
        ActionType ActionType { get; }
        int Priority { get; }
        ChainBehavior ChainBehavior { get; }
        Unit Owner { get; }
        void SetNext(IActionModifier next);
        ActionResult Evaluate(ActionContext context);
    }
}
```


***

## 5. 실행 플로우 요약

1. **Unit**: ModifierPreset, AttackEffect(Bleed 등) 보유
2. **공격 시**:
    - `attackEvaluator.EvaluateNextAction(context)`로 Modifier(예: Ranged) 실행 → 타겟 + 데미지
    - `ApplyAttackEffects`로 모든 Effect(Bleed 등)가 타겟에 일괄 적용

***

### 이 구조면 **모든 Modifier에 공통 Effect(Bleed 등)가 일괄 적용**되며, Modifier 계층이 심플하고 프로젝트 유지 관리가 매우 용이합니다. 각 Modifier/Effect의 구현은 C\# 클래스 하나로 충분하며, ScriptableObject 에셋을 통해 데이터와 파라미터를 관리합니다.

