using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;
using Game.Services.Modifiers;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 리팩토링된 원거리 공격 Modifier
    /// Config 기반으로 동작하며 의존성이 주입됨
    /// 투사체 기반 원거리 공격을 담당한다.
    /// </summary>
    public class RangedModifier : IProjectileAttackModifier
    {
        // IActionModifier 구현
        public string ModifierName => config.Name;
        public ActionType ActionType => config.ActionType;
        public int Priority => config.Priority;
        public ChainBehavior ChainBehavior => config.ChainBehavior;
        public Unit Owner { get; private set; }

        // IProjectileAttackModifier 구현
        public AttackConfig AttackConfig => config.AttackConfig.Value;

        // 내부 필드
        private readonly ModifierConfig config;
        private readonly IModifierDependencies dependencies;
        private IActionModifier nextModifier;

        public RangedModifier(Unit owner, ModifierConfig config, IModifierDependencies dependencies)
        {
            Owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
            this.config = config;
            this.dependencies = dependencies ?? throw new System.ArgumentNullException(nameof(dependencies));

            if (!config.AttackConfig.HasValue)
            {
                throw new System.ArgumentException("RangedModifier requires AttackConfig");
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

                Debug.Log($"[RangedModifier] Found {targets.Count} targets in range {config.AttackConfig.Value.Range}");
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
            // 실제 실행은 CombatComponent에서 처리
            // 필요 시 추가 로직 구현
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
                Debug.Log($"[RangedModifier] Fallback to next modifier");
                return nextModifier.Evaluate(new ActionContext(context.ActorPosition));
            }

            return result;
        }
    }
}
