using System.Linq;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;
using Game.Services.Modifiers;
using Game.Interfaces;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 리팩토링된 근접 공격 Modifier
    /// </summary>
    public class MeleeModifier : IAttackModifier
    {
        public string ModifierName => config.Name;
        public ActionType ActionType => config.ActionType;
        public int Priority => config.Priority;
        public ChainBehavior ChainBehavior => config.ChainBehavior;
        public Unit Owner { get; private set; }

        private readonly ModifierConfig config;
        private readonly IModifierDependencies dependencies;
        private IActionModifier nextModifier;

        public MeleeModifier(Unit owner, ModifierConfig config, IModifierDependencies dependencies)
        {
            Owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
            this.config = config;
            this.dependencies = dependencies ?? throw new System.ArgumentNullException(nameof(dependencies));

            if (!config.AttackConfig.HasValue)
            {
                throw new System.ArgumentException("MeleeModifier requires AttackConfig");
            }
        }

        public void SetNext(IActionModifier next) => nextModifier = next;

        public ActionResult Evaluate(ActionContext context)
        {
            ActionResult result = new ActionResult
            {
                ActionType = ActionType.Attack,
                ChainBehavior = this.ChainBehavior
            };

            // 조건 체크
            if (!CheckAllConditions(context))
            {
                result.IsSuccess = false;
                return HandleFailure(result, context);
            }

            // 타겟 검색
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

                Debug.Log($"[MeleeModifier] Found adjacent enemy target");
            }
            else
            {
                result.IsSuccess = false;
                result = HandleFailure(result, context);
            }

            return result;
        }

        public void Execute(ActionContext context) { }

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
                return nextModifier.Evaluate(new ActionContext(context.ActorPosition));
            }

            return result;
        }
    }
}
