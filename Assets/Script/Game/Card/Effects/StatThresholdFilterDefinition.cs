using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 유닛의 스탯(공격력, 체력, 이동거리)을 기준으로
    /// 일정 수치/퍼센트 이상/이하 조건을 평가하는 타겟 필터
    /// </summary>
    [CreateAssetMenu(
        fileName = "StatThresholdFilter",
        menuName = "Game/Effects/Filters/Stats/StatThreshold")]
    public class StatThresholdFilterDefinition : TargetFilterDefinition
    {
        public enum StatType
        {
            AttackPower,
            CurrentHealth,
            MovementRange
        }

        public enum ComparisonType
        {
            GreaterOrEqual, // 이상
            LessOrEqual     // 이하
        }

        public enum ValueMode
        {
            Absolute,      // 절대 수치 기준
            PercentOfBase  // 기준값(최대/기본) 대비 퍼센트
        }

        [Header("Stat Settings")]
        [SerializeField] private StatType statType = StatType.CurrentHealth;
        [SerializeField] private ComparisonType comparison = ComparisonType.GreaterOrEqual;
        [SerializeField] private ValueMode valueMode = ValueMode.Absolute;

        [Header("Thresholds")]
        [SerializeField] private int absoluteThreshold = 1;
        [SerializeField, Range(0f, 1f)] private float percentThreshold = 0.5f;

        public StatType Stat => statType;
        public ComparisonType Comparison => comparison;
        public ValueMode Mode => valueMode;
        public int AbsoluteThreshold => absoluteThreshold;
        public float PercentThreshold => percentThreshold;

        public override bool Matches(Tile tile, Unit unit, GameContext context)
        {
            if (unit == null)
            {
                return false;
            }

            float currentValue;
            float baseValue;

            switch (statType)
            {
                case StatType.AttackPower:
                    GetAttackValues(unit, out currentValue, out baseValue);
                    break;
                case StatType.CurrentHealth:
                    GetHealthValues(unit, out currentValue, out baseValue);
                    break;
                case StatType.MovementRange:
                    GetMovementValues(unit, out currentValue, out baseValue);
                    break;
                default:
                    return false;
            }

            if (valueMode == ValueMode.Absolute)
            {
                float threshold = absoluteThreshold;
                return Compare(currentValue, threshold);
            }

            // PercentOfBase 모드
            if (baseValue <= 0f)
            {
                return false;
            }

            float currentPercent = currentValue / baseValue;
            float thresholdPercent = percentThreshold;
            return Compare(currentPercent, thresholdPercent);
        }

        private bool Compare(float value, float threshold)
        {
            return comparison == ComparisonType.GreaterOrEqual
                ? value >= threshold
                : value <= threshold;
        }

        private void GetAttackValues(Unit unit, out float currentValue, out float baseValue)
        {
            var combat = unit.GetComponent<Game.Components.CombatComponent>();
            if (combat != null)
            {
                currentValue = combat.CurrentAttackPower;
                baseValue = combat.BaseAttackPower;
            }
            else
            {
                currentValue = unit.AttackPower;
                baseValue = currentValue;
            }
        }

        private void GetHealthValues(Unit unit, out float currentValue, out float baseValue)
        {
            currentValue = unit.Health;
            baseValue = Mathf.Max(1, unit.MaxHealth);
        }

        private void GetMovementValues(Unit unit, out float currentValue, out float baseValue)
        {
            var movement = unit.GetComponent<Game.Components.MovementComponent>();
            if (movement != null)
            {
                currentValue = movement.MovementRange;
                baseValue = movement.BaseMovementRange;
            }
            else
            {
                currentValue = unit.MovementRange;
                baseValue = currentValue;
            }
        }

        public override string GetTargetDescription()
        {
            string statLabel = statType switch
            {
                StatType.AttackPower   => "공격력",
                StatType.CurrentHealth => "체력",
                StatType.MovementRange => "이동거리",
                _                      => "스탯"
            };

            string comparisonLabel = comparison == ComparisonType.GreaterOrEqual
                ? "이상"
                : "이하";

            if (valueMode == ValueMode.Absolute)
            {
                return $"{statLabel} {absoluteThreshold} {comparisonLabel}";
            }

            int percent = Mathf.RoundToInt(percentThreshold * 100f);
            string baseLabel = statType == StatType.CurrentHealth
                ? "최대 체력의"
                : $"기본 {statLabel}의";

            return $"{baseLabel} {percent}% {comparisonLabel}";
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 값 검증/보정
        /// </summary>
        private void OnValidate()
        {
            absoluteThreshold = Mathf.Max(0, absoluteThreshold);
            percentThreshold = Mathf.Clamp01(percentThreshold);
        }
#endif
    }
}

