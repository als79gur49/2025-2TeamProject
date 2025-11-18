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
            list.Remove(effect);
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
        /// 모든 Effect의 지속 턴을 1씩 감소시키고,
        /// 남은 지속 턴이 0인 Effect를 제거합니다.
        /// </summary>
        public void TickDurationsAndCleanup()
        {
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

                    effect.TickDuration();

                    if (effect.RemainingDuration == 0)
                    {
                        list.RemoveAt(i);
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
