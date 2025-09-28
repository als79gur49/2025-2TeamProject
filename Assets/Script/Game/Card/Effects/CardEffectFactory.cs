using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 카드 효과 팩토리
    /// EffectData를 기반으로 ICardEffect 인스턴스를 생성하는 팩토리 클래스입니다.
    /// </summary>
    public static class CardEffectFactory
    {
        // 효과 타입별 생성자 등록
        private static readonly Dictionary<EffectType, Func<EffectData, ICardEffect>> _effectCreators =
            new Dictionary<EffectType, Func<EffectData, ICardEffect>>
            {
                { EffectType.Damage, data => new DamageEffect(data) },
                { EffectType.Heal, data => new HealEffect(data) },
                { EffectType.Summon, data => new SummonEffect(data) }
            };

        /// <summary>
        /// EffectData로부터 ICardEffect 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="effectData">효과 데이터</param>
        /// <returns>생성된 효과 인스턴스</returns>
        public static ICardEffect CreateEffect(EffectData effectData)
        {
            if (effectData == null)
            {
                Debug.LogError("CardEffectFactory: effectData가 null입니다.");
                return null;
            }

            if (!effectData.IsValid())
            {
                Debug.LogError($"CardEffectFactory: 유효하지 않은 effectData입니다. {effectData}");
                return null;
            }

            if (_effectCreators.TryGetValue(effectData.Type, out var creator))
            {
                try
                {
                    return creator(effectData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"CardEffectFactory: {effectData.Type} 효과 생성 실패: {ex.Message}");
                    return null;
                }
            }

            Debug.LogError($"CardEffectFactory: 지원하지 않는 효과 타입입니다: {effectData.Type}");
            return null;
        }

        /// <summary>
        /// 여러 EffectData로부터 ICardEffect 리스트를 생성합니다.
        /// </summary>
        /// <param name="effectDataList">효과 데이터 리스트</param>
        /// <returns>생성된 효과 인스턴스 리스트</returns>
        public static List<ICardEffect> CreateEffects(IEnumerable<EffectData> effectDataList)
        {
            var effects = new List<ICardEffect>();

            if (effectDataList == null)
            {
                Debug.LogWarning("CardEffectFactory: effectDataList가 null입니다.");
                return effects;
            }

            foreach (var effectData in effectDataList)
            {
                var effect = CreateEffect(effectData);
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
        /// 새로운 효과 타입을 등록합니다.
        /// </summary>
        /// <param name="effectType">효과 타입</param>
        /// <param name="creator">생성자 함수</param>
        public static void RegisterEffectType(EffectType effectType, Func<EffectData, ICardEffect> creator)
        {
            if (creator == null)
            {
                Debug.LogError("CardEffectFactory: creator가 null입니다.");
                return;
            }

            _effectCreators[effectType] = creator;
            Debug.Log($"CardEffectFactory: {effectType} 효과 타입이 등록되었습니다.");
        }

        /// <summary>
        /// 등록된 효과 타입들을 반환합니다.
        /// </summary>
        public static IEnumerable<EffectType> GetRegisteredEffectTypes()
        {
            return _effectCreators.Keys;
        }

        /// <summary>
        /// 특정 효과 타입이 지원되는지 확인합니다.
        /// </summary>
        /// <param name="effectType">확인할 효과 타입</param>
        /// <returns>지원되면 true</returns>
        public static bool IsEffectTypeSupported(EffectType effectType)
        {
            return _effectCreators.ContainsKey(effectType);
        }
    }
}