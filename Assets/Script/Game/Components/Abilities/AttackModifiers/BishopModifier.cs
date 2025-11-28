using System.Linq;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;
using Game.Services.Modifiers;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 비숍 공격 Modifier
    /// 대각선 방향으로 무제한 사거리 공격
    /// (투사체/레이저 기반 공격으로 동작)
    /// </summary>
    public class BishopModifier : IProjectileAttackModifier
    {
        public string ModifierName => config.Name;
        public ActionType ActionType => config.ActionType;
        public int Priority => config.Priority;
        public ChainBehavior ChainBehavior => config.ChainBehavior;
        public Unit Owner { get; private set; }

        // IProjectileAttackModifier 구현
        public AttackConfig AttackConfig => config.AttackConfig.Value;

        private readonly ModifierConfig config;
        private readonly IModifierDependencies dependencies;
        private IActionModifier nextModifier;

        public BishopModifier(Unit owner, ModifierConfig config, IModifierDependencies dependencies)
        {
            Owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
            this.config = config;
            this.dependencies = dependencies ?? throw new System.ArgumentNullException(nameof(dependencies));

            if (!config.AttackConfig.HasValue)
            {
                throw new System.ArgumentException("BishopModifier requires AttackConfig");
            }
        }

        public void SetNext(IActionModifier next) => nextModifier = next;

        public ActionResult Evaluate(ActionContext context)
        {
            var result = new ActionResult
            {
                ActionType = ActionType.Attack,
                ChainBehavior = ChainBehavior
            };

            if (!CheckAllConditions(context))
            {
                result.IsSuccess = false;
                return HandleFailure(result, context);
            }

            var teamComponent = Owner.GetComponent<ITeamComponent>();
            var selector = dependencies.TargetSelectorProvider.GetSelector(config.Type);
            var targets = selector.FindTargets(
                context.ActorPosition,
                config.TargetingParams,
                teamComponent
            );

            if (targets.Count > 0)
            {
                result.ValidTiles = targets;
                result.IsSuccess = true;
                result.SelectedModifier = this;

                Debug.Log($"[BishopModifier] Found {targets.Count} diagonal targets");
            }
            else
            {
                result.IsSuccess = false;
                result = HandleFailure(result, context);
            }

            return result;
        }

        public void Execute(ActionContext context)
        {
            // 실제 실행은 CombatComponent에서 처리 (투사체/레이저 기반)
        }

        public int CalculateDamage(ActionContext context)
        {
            var combatSystem = Owner.GetComponent<ICombatSystem>();
            int baseDamage = combatSystem?.CurrentAttackPower ?? 0;

            return dependencies.CombatCalculator.CalculateDamage(
                baseDamage,
                config.AttackConfig.Value.DamageModifier
            );
        }

        private bool CheckAllConditions(ActionContext context)
        {
            if (config.Conditions == null || config.Conditions.Count == 0)
                return true;

            return config.Conditions.All(condition => condition.Evaluate(Owner, context));
        }

        private ActionResult HandleFailure(ActionResult result, ActionContext context)
        {
            if (ChainBehavior == ChainBehavior.FallbackOnFailure && nextModifier != null)
            {
                Debug.Log("[BishopModifier] Fallback to next modifier");
                return nextModifier.Evaluate(new ActionContext(context.ActorPosition));
            }

            return result;
        }
    }
}
