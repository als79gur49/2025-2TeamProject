using System.Linq;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;
using Game.Services.Modifiers;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 리팩토링된 부스터 이동 Modifier
    /// 전체 맵 이동 가능
    /// </summary>
    public class BoosterModifier : IMovementModifier
    {
        public string ModifierName => config.Name;
        public ActionType ActionType => config.ActionType;
        public int Priority => config.Priority;
        public ChainBehavior ChainBehavior => config.ChainBehavior;
        public Unit Owner { get; private set; }
        public int MaxMoveRange => int.MaxValue; // Unlimited range for booster

        private readonly ModifierConfig config;
        private readonly IModifierDependencies dependencies;
        private IActionModifier nextModifier;

        public BoosterModifier(Unit owner, ModifierConfig config, IModifierDependencies dependencies)
        {
            Owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
            this.config = config;
            this.dependencies = dependencies ?? throw new System.ArgumentNullException(nameof(dependencies));

            if (!config.MovementConfig.HasValue)
            {
                throw new System.ArgumentException("BoosterModifier requires MovementConfig");
            }
        }

        public void SetNext(IActionModifier next) => nextModifier = next;

        public ActionResult Evaluate(ActionContext context)
        {
            ActionResult result = new ActionResult
            {
                ActionType = ActionType.Movement,
                ChainBehavior = this.ChainBehavior
            };

            // 조건 체크
            if (!CheckAllConditions(context))
            {
                result.IsSuccess = false;
                return HandleFailure(result, context);
            }

            // 전체 맵의 이동 가능한 타일 검색
            var teamComponent = Owner.GetComponent<ITeamComponent>();
            var selector = dependencies.TargetSelectorProvider.GetSelector(config.Type);
            var allReachableTiles = selector.FindTargets(
                context.ActorPosition,
                config.TargetingParams,
                teamComponent
            );

            if (allReachableTiles.Count > 0)
            {
                result.ValidTiles = allReachableTiles;
                result.IsSuccess = true;
                result.SelectedModifier = this;

                Debug.Log($"[BoosterModifier] {allReachableTiles.Count} tiles reachable");
            }
            else
            {
                result.IsSuccess = false;
                result = HandleFailure(result, context);
            }

            return result;
        }

        public void Execute(ActionContext context) { }

        public Vector2Int CalculateFinalDestination(Vector2Int intended, ActionContext context)
        {
            // Booster allows movement to any valid tile on the map
            return intended;
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
