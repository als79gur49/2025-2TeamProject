using System;
using UnityEngine;
using Game.Data;
using UnityEditor.PackageManager.Requests;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 직렬화 가능한 효과 데이터
    /// Unity 인스펙터에서 카드 설정을 용이하게 하기 위한 데이터 구조입니다.
    /// 기존의 unitToSummon, spellEffectValue 등 레거시 필드를 대체합니다.
    /// </summary>
    [Serializable]
    public class EffectData
    {
        [Header("효과 기본 정보")]
        [SerializeField] private EffectType type = EffectType.Damage;
        [SerializeField] private int value = 1;
        [SerializeField] private int priority = 0;

        [Header("타겟팅 정보")]
        [SerializeField] private AffectedType affectedType = AffectedType.Enemy;
        [SerializeField] private int affectedRange = 0;

        [Header("Summon 효과 전용")]
        [SerializeField] private UnitData unitToSummon;

        [Header("Damage/Heal 효과 전용")]
        [SerializeField] private bool ignoreArmor = false;
        [SerializeField] private float duration = 0f; // 0 = 즉시, 0+ = 지속 시간

        [Header("시각적 효과")]
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private string effectAnimation = "";

        [Header("초기화 상태 (Unity Serialization 대응)")]
        [SerializeField] private bool initialized = false;

        /// <summary>효과 타입</summary>
        public EffectType Type => type;

        /// <summary>초기화 완료 여부</summary>
        public bool Initialized => initialized;

        /// <summary>효과 값 (데미지량, 회복량, 소환 개수 등)</summary>
        public int Value => value;

        /// <summary>효과 우선순위 (낮을수록 먼저 실행)</summary>
        public int Priority => priority;

        /// <summary>영향받는 대상 타입</summary>
        public AffectedType AffectedType => affectedType;

        /// <summary>영향 범위 (0: 단일 대상, 1+: 범위 효과)</summary>
        public int AffectedRange => affectedRange;

        /// <summary>소환할 유닛 데이터 (Summon 효과 전용)</summary>
        public UnitData UnitToSummon => unitToSummon;

        /// <summary>방어력 무시 여부 (Damage 효과 전용)</summary>
        public bool IgnoreArmor => ignoreArmor;

        /// <summary>지속 시간 (0 = 즉시, 0+ = 지속 효과)</summary>
        public float Duration => duration;

        /// <summary>효과 프리팹</summary>
        public GameObject EffectPrefab => effectPrefab;

        /// <summary>효과 애니메이션 이름</summary>
        public string EffectAnimation => effectAnimation;

        /// <summary>
        /// 기본 생성자 (Unity 직렬화용)
        /// Unity Inspector에서 + 버튼 또는 Size 변경 시 이 생성자를 사용합니다.
        /// 필드의 기본값이 자동으로 적용됩니다.
        /// </summary>
        public EffectData()
        {
            // 필드 초기화는 SerializeField의 기본값으로 자동 처리됨
            // type = EffectType.Damage (기본값)
            // value = 1 (기본값)
            // affectedType = AffectedType.Enemy (기본값)
            // affectedRange = 0 (기본값)
            type = EffectType.Damage;
            value = 1;
            affectedType = AffectedType.Enemy;
            affectedRange = 0;
        }
        public void Reset()
        {
            value = 1; // 원하는 초기값 설정
        }

        /// <summary>
        /// Unity Inspector에서 추가된 인스턴스를 초기화합니다.
        /// OnValidate()에서 호출되어 기본값을 설정합니다.
        /// </summary>
        public void Initialize()
        {
            if (!initialized)
            {
                type = EffectType.Damage;
                value = 1;
                affectedType = AffectedType.Enemy;
                affectedRange = 0;
                priority = 0;
                ignoreArmor = false;
                duration = 0f;
                initialized = true;
            }
        }

        /// <summary>
        /// 효과 데이터 생성자 (코드에서 사용)
        /// </summary>
        public EffectData(EffectType effectType, int effectValue, AffectedType targetType = AffectedType.Enemy, int range = 0)
        {
            type = effectType;
            value = effectValue;
            affectedType = targetType;
            affectedRange = range;
            initialized = true; // 코드로 생성된 인스턴스는 초기화 완료 표시
        }

        /// <summary>
        /// 데이터 유효성 검증
        /// </summary>
        public bool IsValid()
        {
            bool baseValid = value > 0 && affectedRange >= 0;

            return type switch
            {
                EffectType.Summon => baseValid && unitToSummon != null,
                EffectType.Damage => baseValid,
                EffectType.Heal => baseValid,
                _ => false
            };
        }

        /// <summary>
        /// 효과 설명 생성
        /// </summary>
        public string GetDescription()
        {
            var description = type switch
            {
                EffectType.Damage => $"{value} 피해",
                EffectType.Heal => $"{value} 회복",
                EffectType.Summon when unitToSummon != null => $"{unitToSummon.UnitName} 소환",
                EffectType.Summon => $"{value}개 소환",
                _ => "알 수 없는 효과"
            };

            if (affectedRange > 0)
            {
                description += $" (범위: {affectedRange})";
            }

            if (duration > 0)
            {
                description += $" ({duration}초간)";
            }

            return description;
        }

        public override string ToString()
        {
            return $"EffectData[{type}: {value}, Target: {affectedType}, Range: {affectedRange}]";
        }
    }

    /// <summary>
    /// 효과 적용 대상 타입
    /// 기존 TargetType과 분리하여 효과 적용 대상을 명확히 구분합니다.
    /// </summary>
    [Serializable]
    public enum AffectedType
    {
        /// <summary>아무에게도 영향 없음</summary>
        None,

        /// <summary>아군에게만 영향</summary>
        Ally,

        /// <summary>적군에게만 영향</summary>
        Enemy,

        /// <summary>모두에게 영향</summary>
        Any
    }
}