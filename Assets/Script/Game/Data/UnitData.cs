using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 개선된 유닛 데이터 ScriptableObject - 완전한 캡슐화와 원본 데이터 보호
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit", menuName = "Game/Unit Data", order = 1)]
    public class UnitData : ScriptableObject
    {
        // ✅ private 필드 + SerializeField로 Unity Inspector 지원하면서 캡슐화 유지
        [Header("기본 정보")]
        [SerializeField] private string unitName = "New Unit";
        [SerializeField] private string description = "";
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;

        [Header("기본 스탯")]
        [SerializeField] private UnitStats baseStats = new UnitStats();

        [Header("특성")]
        [SerializeField] private List<string> tags = new List<string>();
        [SerializeField] private UnitType unitType = UnitType.Infantry;
        [SerializeField] private TeamType defaultTeam = TeamType.Player;

        [Header("비용")]
        [SerializeField] private int deployCost = 1;
        [SerializeField] private int maintainCost = 0;

        // ✅ 유닛 타입 열거형
        public enum UnitType
        {
            Infantry,   // 보병
            Cavalry,    // 기병
            Archer,     // 궁수
            Mage,       // 마법사
            Support,    // 지원
            Structure   // 구조물
        }

        public enum TeamType
        {
            Player,
            Enemy,
            Neutral
        }

        // ✅ 읽기 전용 속성으로 안전한 외부 접근
        public string UnitName => unitName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public UnitType Type => unitType;
        public TeamType DefaultTeam => defaultTeam;
        public int DeployCost => deployCost;
        public int MaintainCost => maintainCost;
        public IReadOnlyList<string> Tags => tags.AsReadOnly();

        // ✅ 스탯 복사본 반환 - 원본 보호
        public UnitStats GetBaseStats()
        {
            return new UnitStats(baseStats);
        }

        // ✅ 특정 스탯 값만 안전하게 조회
        public int GetMaxHealth() => baseStats.MaxHealth;
        public int GetAttackPower() => baseStats.AttackPower;
        public int GetMovementRange() => baseStats.MovementRange;
        public int GetActionPoints() => baseStats.ActionPointsPerTurn;
        public int GetArmor() => baseStats.Armor;
        public float GetCriticalChance() => baseStats.CriticalChance;

        // ✅ 태그 관련 안전한 메서드
        public bool HasTag(string tag)
        {
            return !string.IsNullOrEmpty(tag) && tags.Contains(tag);
        }

        public bool HasAnyTag(params string[] checkTags)
        {
            if (checkTags == null || checkTags.Length == 0) return false;
            return checkTags.Any(tag => HasTag(tag));
        }

        public bool HasAllTags(params string[] checkTags)
        {
            if (checkTags == null || checkTags.Length == 0) return true;
            return checkTags.All(tag => HasTag(tag));
        }

        // ✅ 임시 스탯 수정이 적용된 스탯 계산 (버프/디버프 적용)
        public UnitStats GetModifiedStats(List<StatModifier> modifiers = null)
        {
            UnitStats modifiedStats = GetBaseStats();
            
            if (modifiers == null || modifiers.Count == 0)
                return modifiedStats;

            // 활성 상태이고 만료되지 않은 수정자만 필터링
            var activeModifiers = modifiers
                .Where(m => m.IsActive && !m.IsExpired)
                .OrderBy(m => m, Comparer<StatModifier>.Create((a, b) => a.CompareTo(b)))
                .ToList();

            // 스탯별로 수정자 적용 (실제 구현에서는 더 세밀한 제어 필요)
            foreach (var modifier in activeModifiers)
            {
                // 여기서는 예시로 공격력만 적용
                // 실제로는 각 스탯별로 수정자를 적용하는 더 복잡한 로직 필요
                modifiedStats = new UnitStats(
                    modifiedStats.MaxHealth,
                    Mathf.RoundToInt(modifier.ApplyModifier(modifiedStats.AttackPower)),
                    modifiedStats.MovementRange,
                    modifiedStats.ActionPointsPerTurn,
                    modifiedStats.Armor,
                    modifiedStats.CriticalChance
                );
            }

            return modifiedStats;
        }

        // ✅ 유닛 호환성 검사
        public bool IsCompatibleWith(UnitData other)
        {
            if (other == null) return false;
            
            // 같은 팀이고 서로 상극이 아닌 경우
            return defaultTeam == other.defaultTeam && 
                   unitType != UnitType.Structure; // 구조물은 다른 유닛과 호환되지 않음
        }

        // ✅ 배치 가능 여부 검사
        public bool CanDeploy(int availableResources)
        {
            return availableResources >= deployCost && 
                   prefab != null && 
                   baseStats.IsValid();
        }

        // ✅ 유닛 설명 생성 (UI용)
        public string GetDetailedDescription()
        {
            var description = $"<b>{unitName}</b> ({unitType})\n";
            description += $"{this.description}\n\n";
            description += $"<b>스탯:</b>\n";
            description += $"체력: {baseStats.MaxHealth}\n";
            description += $"공격력: {baseStats.AttackPower}\n";
            description += $"이동력: {baseStats.MovementRange}\n";
            description += $"행동력: {baseStats.ActionPointsPerTurn}\n";
            description += $"방어력: {baseStats.Armor}\n";
            description += $"치명타: {baseStats.CriticalChance:P1}\n\n";
            description += $"<b>비용:</b> 배치 {deployCost}, 유지 {maintainCost}\n";
            
            if (tags.Count > 0)
            {
                description += $"<b>태그:</b> {string.Join(", ", tags)}";
            }
            
            return description;
        }

        // ✅ 데이터 유효성 검증
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(unitName) &&
                   prefab != null &&
                   baseStats != null &&
                   baseStats.IsValid() &&
                   deployCost >= 0 &&
                   maintainCost >= 0;
        }

        // ✅ 개발자용 디버그 정보
        public override string ToString()
        {
            return $"UnitData[{unitName} ({unitType}), Cost:{deployCost}, {baseStats}]";
        }

        // ✅ 에디터용 검증 (Unity Editor에서만 실행)
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // 유닛 이름이 비어있으면 파일명으로 설정
            if (string.IsNullOrEmpty(unitName))
            {
                unitName = name;
            }

            // 비용이 음수면 0으로 설정
            deployCost = Mathf.Max(0, deployCost);
            maintainCost = Mathf.Max(0, maintainCost);

            // 스탯 정규화
            if (baseStats != null)
            {
                baseStats.Normalize();
            }

            // 중복 태그 제거
            if (tags != null)
            {
                tags = tags.Where(tag => !string.IsNullOrEmpty(tag))
                          .Distinct()
                          .ToList();
            }
        }
        #endif

        // ✅ 런타임 초기화용 팩토리 메서드
        public static UnitData CreateRuntimeInstance(string name, UnitStats stats, UnitType type = UnitType.Infantry)
        {
            var instance = CreateInstance<UnitData>();
            instance.unitName = name;
            instance.baseStats = new UnitStats(stats);
            instance.unitType = type;
            instance.deployCost = 1;
            instance.maintainCost = 0;
            instance.tags = new List<string>();
            
            return instance;
        }
    }
}