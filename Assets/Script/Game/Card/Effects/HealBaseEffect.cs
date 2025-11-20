using System;
using UnityEngine;
using Game;
using Game.Interfaces;
using Game.Services;
using Game.VFX;
using Game.Core;

namespace Game.Card.Effects
{
    /// <summary>
    /// 전역 베이스 회복 카드 효과
    /// - IBaseManager를 통해 대상 베이스를 찾아 HealthComponent.Heal을 호출합니다.
    /// - VFX는 SpellEffectExecutor에서 처리합니다.
    /// </summary>
    public class HealBaseEffect : IVFXAwareEffect
    {
        private readonly HealBaseEffectDefinition _definition;

        public EffectType EffectType => EffectType.HealBase;
        public int Priority => _definition?.Priority ?? 0;

        public HealBaseEffect(HealBaseEffectDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_definition == null)
            {
                Debug.LogWarning("[HealBaseEffect] Invalid definition");
                return false;
            }

            if (context == null)
            {
                Debug.LogWarning("[HealBaseEffect] Invalid GameContext");
                return false;
            }

            if (_definition.HealAmount <= 0)
            {
                Debug.LogWarning("[HealBaseEffect] HealAmount must be greater than zero");
                return false;
            }

            return true;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            ExecuteInternal(context);
        }

        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            ExecuteInternal(context);
        }

        private void ExecuteInternal(GameContext context)
        {
            if (!CanExecute(Vector2Int.zero, context))
            {
                Debug.LogWarning("[HealBaseEffect] Cannot execute");
                return;
            }

            var baseManager = ServiceLocator.Get<IBaseManager>();
            if (baseManager == null)
            {
                Debug.LogError("[HealBaseEffect] IBaseManager not found in ServiceLocator");
                return;
            }

            TeamType casterTeam = context.CasterTeam;
            TeamType targetTeam = ResolveTargetTeam(casterTeam, _definition.TargetRelation);

            if (targetTeam == TeamType.None)
            {
                Debug.LogWarning("[HealBaseEffect] Target team is None");
                return;
            }

            Base targetBase = null;
            if (targetTeam == TeamType.Player)
            {
                targetBase = baseManager.PlayerBase;
            }
            else if (targetTeam == TeamType.Enemy)
            {
                targetBase = baseManager.EnemyBase;
            }

            if (targetBase == null || targetBase.HealthComponent == null)
            {
                Debug.LogWarning("[HealBaseEffect] Target base or health component is null");
                return;
            }

            var health = targetBase.HealthComponent;
            if (!health.CanHeal(_definition.HealAmount))
            {
                Debug.Log($"[HealBaseEffect] Base cannot be healed by {_definition.HealAmount}");
                return;
            }

            health.Heal(_definition.HealAmount);

            Debug.Log($"[HealBaseEffect] Healed {targetTeam} base for {_definition.HealAmount}");
        }

        private TeamType ResolveTargetTeam(TeamType casterTeam, TeamRelation relation)
        {
            switch (relation)
            {
                case TeamRelation.Self:
                case TeamRelation.Ally:
                    return casterTeam;
                case TeamRelation.Enemy:
                    if (casterTeam == TeamType.Player) return TeamType.Enemy;
                    if (casterTeam == TeamType.Enemy) return TeamType.Player;
                    return TeamType.None;
                case TeamRelation.Neutral:
                    return TeamType.Neutral;
                case TeamRelation.Any:
                default:
                    return casterTeam;
            }
        }
    }
}
