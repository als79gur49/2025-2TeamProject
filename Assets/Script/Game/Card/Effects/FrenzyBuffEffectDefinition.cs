using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 지정된 범위/필터에 해당하는 유닛들에게
    /// 광란(FrenzyOnKillEffect)을 일정 턴 동안 부여하는 카드 효과 정의입니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/FrenzyBuffEffect")]
    public class FrenzyBuffEffectDefinition : EffectDefinition
    {
        [Header("광란 버프 설정")]
        [Tooltip("광란 효과 지속 턴 수 (-1: 무한)")]
        [SerializeField] private int frenzyDurationTurns = 1;

        [Tooltip("한 번에 광란을 부여할 최대 유닛 수")]
        [SerializeField] private int maxTargets = 1;

        /// <summary>
        /// FrenzyOnKillEffect가 유지되는 턴 수입니다.
        /// -1이면 무한 지속을 의미합니다.
        /// </summary>
        public int FrenzyDurationTurns => frenzyDurationTurns;

        /// <summary>
        /// 한 번의 카드 사용으로 광란을 부여할 최대 유닛 수입니다.
        /// </summary>
        public int MaxTargets => maxTargets;

        public override ICardEffect CreateRuntimeEffect()
        {
            return new FrenzyBuffEffect(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 0턴은 의미가 없으므로 1 이상 또는 -1(무한)만 허용
            if (frenzyDurationTurns == 0)
            {
                frenzyDurationTurns = 1;
            }
            else if (frenzyDurationTurns < -1)
            {
                frenzyDurationTurns = -1;
            }

            if (maxTargets < 0)
            {
                maxTargets = 0;
            }
        }
#endif
    }
}

