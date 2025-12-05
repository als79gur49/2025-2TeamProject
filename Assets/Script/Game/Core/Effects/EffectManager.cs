using System;
using System.Collections.Generic;
using Game.Components;

namespace Game.Core.Effects
{
    /// <summary>
    /// Unit에 귀속된 모든 Effect를 관리합니다.
    /// </summary>
    public class EffectManager
    {
        private readonly Unit owner;
        private readonly Dictionary<EffectTrigger, List<IEffect>> effectsByTrigger;

        public event Action<IEffect> OnEffectAdded;
        public event Action<IEffect> OnEffectRemoved;

        public EffectManager(Unit owner)
        {
            this.owner = owner;
            effectsByTrigger = new Dictionary<EffectTrigger, List<IEffect>>();

            foreach (EffectTrigger trigger in Enum.GetValues(typeof(EffectTrigger)))
            {
                effectsByTrigger[trigger] = new List<IEffect>();
            }
        }

        public void AddEffect(IEffect effect)
        {
            if (effect == null) return;

            var list = effectsByTrigger[effect.Trigger];
            if (!list.Contains(effect))
            {
                list.Add(effect);
                list.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                OnEffectAdded?.Invoke(effect);

                // IImmediateEffect를 구현한 Effect는 추가 시점에 한 번 즉시 적용합니다.
                if (effect is IImmediateEffect immediateEffect)
                {
                    var context = new EffectContext();
                    immediateEffect.ApplyImmediately(context);
                }
            }
        }

        public void RemoveEffect(IEffect effect)
        {
            if (effect == null) return;
            if (!effectsByTrigger.TryGetValue(effect.Trigger, out var list)) return;
            if (list.Remove(effect))
            {
                OnEffectRemoved?.Invoke(effect);
            }
        }

        public void ClearAllEffects()
        {
            foreach (var kv in effectsByTrigger)
            {
                kv.Value.Clear();
            }
        }

        public void TriggerEffects(EffectTrigger trigger, EffectContext context)
        {
            if (!effectsByTrigger.TryGetValue(trigger, out var list)) return;
            if (list.Count == 0) return;

            // 복사본 순회: 실행 중 제거에 안전
            var snapshot = new List<IEffect>(list);
            foreach (var effect in snapshot)
            {
                if (effect == null) continue;
                if (effect.CanApply(context))
                {
                    effect.Apply(context);
                }
            }
        }

        /// <summary>
        /// 전역 턴 종료(TurnEnd) 시점에서 DurationType이 TimeBased인 Effect들의
        /// 지속 턴을 감소시키고, 만료된 Effect를 제거합니다.
        /// </summary>
        public void TickDurationsOnTurnEnd()
        {
            var context = new DurationTickContext(DurationTickSource.TurnEnd, owner, null);

            foreach (var kv in effectsByTrigger)
            {
                var list = kv.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var effect = list[i];
                    if (effect == null)
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    effect.TickDuration(context);

                    if (effect.RemainingDuration == 0)
                    {
                        list.RemoveAt(i);
                        OnEffectRemoved?.Invoke(effect);
                    }
                }
            }
        }

        /// <summary>
        /// 유닛의 행동 턴(Action Turn)이 종료되었을 때,
        /// DurationType이 ActionBased인 Effect들의 지속 턴을 감소시키고,
        /// 만료된 Effect를 제거합니다.
        /// </summary>
        public void TickDurationsOnActionEnd(ActionTurnOutcome actionOutcome)
        {
            var context = new DurationTickContext(DurationTickSource.ActionEnd, owner, actionOutcome);

            foreach (var kv in effectsByTrigger)
            {
                var list = kv.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var effect = list[i];
                    if (effect == null)
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    effect.TickDuration(context);

                    if (effect.RemainingDuration == 0)
                    {
                        list.RemoveAt(i);
                        OnEffectRemoved?.Invoke(effect);
                    }
                }
            }
        }

        public bool HasEffect(string effectName)
        {
            foreach (var kv in effectsByTrigger)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var effect = list[i];
                    if (effect != null && effect.EffectName == effectName)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 특정 타입의 Effect가 하나라도 존재하는지 확인합니다.
        /// </summary>
        public bool HasEffect<T>() where T : class, IEffect
        {
            foreach (var kv in effectsByTrigger)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is T)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 런타임 타입으로 Effect 존재 여부를 확인합니다.
        /// </summary>
        public bool HasEffect(Type effectType)
        {
            if (effectType == null || !typeof(IEffect).IsAssignableFrom(effectType))
                return false;

            foreach (var kv in effectsByTrigger)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var effect = list[i];
                    if (effect != null && effectType.IsInstanceOfType(effect))
                        return true;
                }
            }

            return false;
        }
    }
}
