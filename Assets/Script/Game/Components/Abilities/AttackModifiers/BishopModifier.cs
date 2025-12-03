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

            // 비숍 전용 우선순위 규칙 적용:
            // - 비관통: 상/하단 대각 중 하나의 "가장 가까운" 적만 선택
            // - 관통: 상/하단 대각 중 하나의 라인만 선택하고, 그 라인 위의 적들을 모두 타격
            var prioritizedTargets = ApplyDiagonalPriority(context.ActorPosition, targets, AttackConfig.Piercing);

            if (prioritizedTargets.Count > 0)
            {
                result.ValidTiles = prioritizedTargets;
                result.IsSuccess = true;
                result.SelectedModifier = this;

                Debug.Log($"[BishopModifier] Selected {prioritizedTargets.Count} diagonal targets (from {targets.Count} candidates, piercing={AttackConfig.Piercing})");
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

        /// <summary>
        /// 비숍 전용 대각선 우선순위 로직
        /// - 비관통(Piercing=false): 상/하단 대각 중 하나의 "가장 가까운" 적 1개만 반환
        ///   - 두 방향 모두에 적이 있으면 더 가까운 쪽 우선, 거리가 같으면 상단 대각 우선
        /// - 관통(Piercing=true): 선택된 하나의 대각선 라인에 속한 적들만 근거리→원거리 순으로 반환
        /// - 대각선 분류에 실패한 경우(예: 설정이 바뀐 경우)는 원본 targets를 그대로 사용
        /// </summary>
        private System.Collections.Generic.List<Tile> ApplyDiagonalPriority(
            Vector2Int origin,
            System.Collections.Generic.List<Tile> targets,
            bool piercing)
        {
            if (targets == null || targets.Count == 0)
            {
                return new System.Collections.Generic.List<Tile>();
            }

            // 상단/하단 대각 라인으로 후보 분류
            var upperLine = new System.Collections.Generic.List<(Tile tile, int distance)>();
            var lowerLine = new System.Collections.Generic.List<(Tile tile, int distance)>();
            var otherTargets = new System.Collections.Generic.List<Tile>();

            foreach (var tile in targets)
            {
                if (tile == null) continue;

                var pos = new Vector2Int(tile.X, tile.Y);
                int dx = pos.x - origin.x;
                int dy = pos.y - origin.y;

                // x가 양수면 "상단 대각", 음수면 "하단 대각"으로 분류
                if (dx > 0)
                {
                    int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                    upperLine.Add((tile, dist));
                }
                else if (dx < 0)
                {
                    int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                    lowerLine.Add((tile, dist));
                }
                else
                {
                    // 순수 전진 방향 등, 비숍의 대각선이 아닌 경우
                    otherTargets.Add(tile);
                }
            }

            bool hasUpper = upperLine.Count > 0;
            bool hasLower = lowerLine.Count > 0;

            // 대각선으로 분류된 타겟이 전혀 없다면, 기존 동작을 유지하기 위해 원본을 그대로 사용
            if (!hasUpper && !hasLower)
            {
                return targets;
            }

            if (!piercing)
            {
                // 비관통: 상/하단 대각 중 하나에서 "가장 가까운" 타일만 선택
                (Tile tile, int distance)? upFirst = hasUpper
                    ? upperLine.OrderBy(t => t.distance).First()
                    : null;

                (Tile tile, int distance)? downFirst = hasLower
                    ? lowerLine.OrderBy(t => t.distance).First()
                    : null;
                //(Tile, int)?
                Tile selected = null;

                if (upFirst.HasValue && !downFirst.HasValue)
                {
                    selected = upFirst.Value.tile;
                }
                else if (!upFirst.HasValue && downFirst.HasValue)
                {
                    selected = downFirst.Value.tile;
                }
                else if (upFirst.HasValue && downFirst.HasValue)
                {
                    if (upFirst.Value.distance < downFirst.Value.distance)
                    {
                        selected = upFirst.Value.tile;
                    }
                    else if (downFirst.Value.distance < upFirst.Value.distance)
                    {
                        selected = downFirst.Value.tile;
                    }
                    else
                    {
                        // 거리가 동일하면 상단 대각 우선
                        selected = upFirst.Value.tile;
                    }
                }

                if (selected != null)
                {
                    return new System.Collections.Generic.List<Tile> { selected };
                }

                // 이론상 여기까지 오는 경우는 거의 없지만,
                // 안전을 위해 기존 타겟 리스트를 반환
                return targets;
            }
            else
            {
                // 관통: 상/하단 대각 중 하나의 "라인"만 선택하고, 그 라인 위의 타일들을 모두 사용
                if (!hasUpper && hasLower)
                {
                    return lowerLine
                        .OrderBy(t => t.distance)
                        .Select(t => t.tile)
                        .ToList();
                }

                if (hasUpper && !hasLower)
                {
                    return upperLine
                        .OrderBy(t => t.distance)
                        .Select(t => t.tile)
                        .ToList();
                }

                // 두 라인 모두 있는 경우: 각 라인의 가장 가까운 타일 거리 비교 후 선택
                var upFirst = upperLine.OrderBy(t => t.distance).First();
                var downFirst = lowerLine.OrderBy(t => t.distance).First();

                if (upFirst.distance < downFirst.distance)
                {
                    return upperLine
                        .OrderBy(t => t.distance)
                        .Select(t => t.tile)
                        .ToList();
                }

                if (downFirst.distance < upFirst.distance)
                {
                    return lowerLine
                        .OrderBy(t => t.distance)
                        .Select(t => t.tile)
                        .ToList();
                }

                // 거리가 동일하면 상단 대각 라인 우선
                return upperLine
                    .OrderBy(t => t.distance)
                    .Select(t => t.tile)
                    .ToList();
            }
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
