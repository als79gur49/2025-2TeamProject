using System;
using UnityEngine;
using Game.Interfaces;
using Game.Services;
using Game.VFX;
using Game.Core;

namespace Game.Card.Effects
{
    /// <summary>
    /// 전역 카드 드로우 효과
    /// - ICardServiceManager.DrawCardsForTeam을 호출합니다.
    /// - VFX는 SpellEffectExecutor에서 처리하며, 여기서는 TriggerData를 로직에 사용하지 않습니다.
    /// </summary>
    public class DrawCardsEffect : IVFXAwareEffect
    {
        private readonly DrawCardsEffectDefinition _definition;

        public EffectType EffectType => EffectType.DrawCards;
        public int Priority => _definition?.Priority ?? 0;

        public DrawCardsEffect(DrawCardsEffectDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_definition == null)
            {
                Debug.LogWarning("[DrawCardsEffect] Invalid definition");
                return false;
            }

            if (context == null)
            {
                Debug.LogWarning("[DrawCardsEffect] Invalid GameContext");
                return false;
            }

            if (_definition.DrawCount <= 0)
            {
                Debug.LogWarning("[DrawCardsEffect] DrawCount must be greater than zero");
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
                Debug.LogWarning("[DrawCardsEffect] Cannot execute");
                return;
            }

            var cardServiceManager = ServiceLocator.Get<ICardServiceManager>();
            if (cardServiceManager == null)
            {
                Debug.LogError("[DrawCardsEffect] ICardServiceManager not found in ServiceLocator");
                return;
            }

            TeamType casterTeam = context.CasterTeam;
            TeamType targetTeam = ResolveTargetTeam(casterTeam, _definition.TargetRelation);

            if (targetTeam == TeamType.None)
            {
                Debug.LogWarning("[DrawCardsEffect] Target team is None");
                return;
            }

            cardServiceManager.DrawCardsForTeam(targetTeam, _definition.DrawCount);

            Debug.Log($"[DrawCardsEffect] Team {targetTeam} drew {_definition.DrawCount} card(s)");
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
