using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 카드 효과 팩토리
    /// EffectDefinition을 기반으로 ICardEffect 인스턴스를 생성하는 팩토리 클래스입니다.
    /// </summary>
    public static class CardEffectFactory
    {
        /// <summary>
        /// 새로운 EffectDefinition 기반으로 ICardEffect 인스턴스를 생성합니다.
        /// 각 EffectDefinition은 자신의 타입에 맞는 런타임 효과를 생성할 책임을 가집니다.
        /// </summary>
        public static ICardEffect CreateEffect(EffectDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("CardEffectFactory: EffectDefinition이 null입니다.");
                return null;
            }

            try
            {
                var effect = definition.CreateRuntimeEffect();
                return effect;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CardEffectFactory: EffectDefinition 기반 효과 생성 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 여러 EffectDefinition으로부터 ICardEffect 리스트를 생성합니다.
        /// </summary>
        public static List<ICardEffect> CreateEffects(IEnumerable<EffectDefinition> definitions)
        {
            var effects = new List<ICardEffect>();

            if (definitions == null)
            {
                Debug.LogWarning("CardEffectFactory: EffectDefinition 리스트가 null입니다.");
                return effects;
            }

            foreach (var def in definitions)
            {
                var effect = CreateEffect(def);
                if (effect != null)
                {
                    effects.Add(effect);
                }
            }

            // 우선순위별로 정렬 (낮은 값일수록 먼저 실행)
            effects.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            return effects;
        }

        /// <summary>
        // EffectDefinition 기반 경로만 사용하므로,
        // EffectType 등록/조회 유틸은 현재 필요하지 않습니다.
    }
}
