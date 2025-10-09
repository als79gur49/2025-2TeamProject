using System;
using UnityEngine;
using Game.Data;
using Game.VFX;
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

        [Header("VFX 설정 (VFX Dynamic Data System)")]
        [SerializeField] private VFXData vfxData;

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

        /// <summary>VFX 데이터 (VFX Dynamic Data System)</summary>
        public VFXData VFXData => vfxData;

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

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor에서 값이 변경될 때 자동 호출 - 자동 마이그레이션
        /// Summon 효과가 AffectedType.None을 사용하면 자동으로 NotAny로 변경
        /// </summary>
        private void OnValidate()
        {
            // 자동 마이그레이션: Summon 효과는 빈 타일(NotAny)을 사용해야 함
            if (type == EffectType.Summon && affectedType == AffectedType.None)
            {
                affectedType = AffectedType.NotAny;
                UnityEngine.Debug.Log($"[Migration] Summon effect auto-migrated to AffectedType.NotAny");
            }
        }
#endif
    }

    /// <summary>
    /// 효과 적용 대상 타입 - 타일 기반 필터링
    /// Phase 3.x: 모든 타겟팅을 타일 중심으로 설계
    /// 핵심 원칙: "타일을 선택하고, 타일에 있는 유닛에게 효과 적용"
    /// </summary>
    [Serializable]
    public enum AffectedType
    {
        /// <summary>
        /// Range 내 모든 타일 (필터링 없음)
        /// 사용처: 광역 효과, 지형 변경, Resource 증감
        /// 필터: 없음 - Range 내 모든 타일 반환
        /// VFX 통합: Range 내 모든 Tile 위치 리스트 반환
        /// 예시: Range=1 → 다이아몬드 5개 타일, Range=2 → 13개 타일
        /// </summary>
        None,

        /// <summary>
        /// 아군 유닛이 있는 타일만 선택
        /// 필터: Tile.OccupyingUnit != null && IsSameTeam(unit, casterTeam)
        /// VFX 통합: 필터링된 타일의 위치 리스트 반환
        /// 효과 적용: 타일의 OccupyingUnit에 효과 적용
        /// </summary>
        Ally,

        /// <summary>
        /// 적군 유닛이 있는 타일만 선택
        /// 필터: Tile.OccupyingUnit != null && !IsSameTeam(unit, casterTeam)
        /// VFX 통합: 필터링된 타일의 위치 리스트 반환
        /// 효과 적용: 타일의 OccupyingUnit에 효과 적용
        /// </summary>
        Enemy,

        /// <summary>
        /// 유닛이 있는 모든 타일 (팀 무관)
        /// 필터: Tile.OccupyingUnit != null
        /// VFX 통합: 필터링된 타일의 위치 리스트 반환
        /// 효과 적용: 타일의 OccupyingUnit에 효과 적용
        /// </summary>
        Any,

        /// <summary>
        /// 유닛이 없는 빈 타일만 선택
        /// 필터: Tile.OccupyingUnit == null
        /// VFX 통합: 필터링된 타일의 위치 리스트 반환
        /// 효과 적용: 타일에 유닛 소환 또는 지형 효과
        /// </summary>
        NotAny
    }
}