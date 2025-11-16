using System.Linq;
using UnityEngine;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;
using Game.Services.Modifiers;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 리팩토링된 일반 이동 Modifier
    /// </summary>
    public class NormalMoveModifier : IMovementModifier
    {
        public string ModifierName => config.Name;
        public ActionType ActionType => config.ActionType;
        public int Priority => config.Priority;
        public ChainBehavior ChainBehavior => config.ChainBehavior;
        public Unit Owner { get; private set; }
        public int MaxMoveRange
        {
            get
            {
                var movementSystem = Owner != null ? Owner.GetComponent<IMovementSystem>() : null;
                return movementSystem?.MovementRange ?? 0;
            }
        }

        private readonly ModifierConfig config;
        private readonly IModifierDependencies dependencies;
        private IActionModifier nextModifier;

        public NormalMoveModifier(Unit owner, ModifierConfig config, IModifierDependencies dependencies)
        {
            Owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
            this.config = config;
            this.dependencies = dependencies ?? throw new System.ArgumentNullException(nameof(dependencies));

            if (!config.MovementConfig.HasValue)
            {
                throw new System.ArgumentException("NormalMoveModifier requires MovementConfig");
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

            var movementSystem = Owner.GetComponent<IMovementSystem>();
            if (movementSystem == null || !movementSystem.CanMove || movementSystem.CurrentMovementPoints <= 0)
            {
                result.IsSuccess = false;
                return HandleFailure(result, context);
            }

            int maxRange = Mathf.Min(movementSystem.MovementRange, movementSystem.CurrentMovementPoints);
            if (maxRange <= 0)
            {
                result.IsSuccess = false;
                return HandleFailure(result, context);
            }

            // 이동 가능한 타일 검색
            var teamComponent = Owner.GetComponent<ITeamComponent>();
            var selector = dependencies.TargetSelectorProvider.GetSelector(config.Type);
            var baseParams = config.TargetingParams;
            var dynamicParams = new TargetingParams(
                range: maxRange,
                piercing: baseParams.Piercing,
                diagonalAllowed: baseParams.DiagonalAllowed,
                requiresClearLane: baseParams.RequiresClearLane,
                targetRelation: baseParams.TargetRelation,
                targetType: baseParams.TargetType,
                targetNexusOnly: baseParams.TargetNexusOnly
            );

            var movableTiles = selector.FindTargets(
                context.ActorPosition,
                dynamicParams,
                teamComponent
            );

            if (movableTiles.Count > 0)
            {
                result.ValidTiles = movableTiles;
                result.IsSuccess = true;
                result.SelectedModifier = this;

                // 가장 먼 거리의 타일을 목적지로 선택
                var furthestTile = movableTiles[movableTiles.Count - 1];
                result.MoveDestination = new Vector2Int(furthestTile.X, furthestTile.Y);

                Debug.Log($"[NormalMoveModifier] Found {movableTiles.Count} movable tiles, destination: ({furthestTile.X}, {furthestTile.Y})");
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
            // For normal movement, return the intended destination
            // (already validated by Evaluate() method)
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
