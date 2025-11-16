using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    /// <summary>
    /// 유닛에 부착되는 Effect의 ScriptableObject 데이터 베이스 클래스입니다.
    /// </summary>
    public abstract class UnitEffectData : ScriptableObject
    {
        [Header("기본 설정")]
        [SerializeField] private string effectName = "New Effect";
        [SerializeField] private EffectTrigger trigger = EffectTrigger.OnAttack;
        [SerializeField] private int priority = 0;

        [Header("지속 시간 (턴 단위)")]
        [Tooltip("-1이면 무한, 1 이상만 사용합니다.")]
        [SerializeField] private int durationTurns = -1;

        public string EffectName => effectName;
        public EffectTrigger Trigger => trigger;
        public int Priority => priority;
        public int DurationTurns => durationTurns;

        /// <summary>
        /// 런타임 IEffect 인스턴스를 생성합니다.
        /// </summary>
        public abstract IEffect CreateEffect(Unit owner);

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(effectName))
            {
                effectName = name;
            }

            // 0은 허용하지 않음: 1 이상 또는 -1(무한)만 사용
            if (durationTurns == 0)
            {
                durationTurns = 1;
            }
            else if (durationTurns < -1)
            {
                durationTurns = -1;
            }
        }
#endif
    }
}
