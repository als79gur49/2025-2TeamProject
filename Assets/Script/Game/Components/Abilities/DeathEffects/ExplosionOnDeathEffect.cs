using Game.Core;
using Game.Core.Effects;
using Game.Interfaces;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 사망 시 주변에 폭발 피해를 주는 Effect입니다.
    /// </summary>
    public class ExplosionOnDeathEffect : IEffect
    {
        public string EffectName => "사망 폭발";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly int damage;
        private readonly int radius;
        private readonly IGridManager gridManager;
        private readonly ITeamComponent teamComponent;

        public ExplosionOnDeathEffect(Unit owner, int damage, int radius, EffectTrigger trigger, int priority, int durationTurns)
        {
            Owner = owner;
            this.damage = damage;
            this.radius = radius;
            Trigger = trigger;
            Priority = priority;
            RemainingDuration = durationTurns;

            gridManager = ServiceLocator.Get<IGridManager>();
            teamComponent = owner.GetComponent<ITeamComponent>();
        }

        public void TickDuration()
        {
            if (RemainingDuration > 0)
                RemainingDuration--;
        }

        public bool CanApply(EffectContext context)
        {
            return Owner != null;
        }

        public void Apply(EffectContext context)
        {
            if (Owner == null || gridManager == null || Owner.CurrentTile == null) return;

            var gridController = gridManager.GetGridController();
            if (gridController == null) return;

            var center = new Vector2Int(Owner.CurrentTile.X, Owner.CurrentTile.Y);
            var tilesInRange = gridController.GetTilesInRange(center, radius);

            foreach (var tile in tilesInRange)
            {
                var target = tile.GetDamageableTarget();
                if (target != null && target.IsAlive && target.gameObject != Owner.gameObject && IsEnemy(target.gameObject))
                {
                    target.TakeDamage(damage);
                }
            }

            Debug.Log($"[ExplosionOnDeathEffect] {Owner.name} exploded for {damage} damage in radius {radius}");
        }

        private bool IsEnemy(GameObject target)
        {
            if (teamComponent == null) return true;
            var targetTeam = target.GetComponent<ITeamComponent>();
            return targetTeam == null ? true : teamComponent.GetRelationTo(targetTeam) == TeamRelation.Enemy;
        }
    }
}

