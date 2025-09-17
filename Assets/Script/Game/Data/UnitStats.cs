using System;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 개선된 유닛 스탯 클래스 - 완전한 캡슐화와 데이터 보호
    /// </summary>
    [System.Serializable]
    public class UnitStats
    {
        // ✅ private 필드 + SerializeField로 Unity Inspector 지원하면서 캡슐화 유지
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int attackPower = 10;
        [SerializeField] private int movementRange = 2;
        [SerializeField] private int actionPointsPerTurn = 1;
        [SerializeField] private int armor = 0;
        [SerializeField] private float criticalChance = 0.1f;

        // ✅ 읽기 전용 속성으로 안전한 외부 접근
        public int MaxHealth => maxHealth;
        public int AttackPower => attackPower;
        public int MovementRange => movementRange;
        public int ActionPointsPerTurn => actionPointsPerTurn;
        public int Armor => armor;
        public float CriticalChance => criticalChance;

        // ✅ 기본 생성자
        public UnitStats() { }

        // ✅ 복사 생성자 - 원본 데이터 보호를 위한 깊은 복사
        public UnitStats(UnitStats original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            maxHealth = original.maxHealth;
            attackPower = original.attackPower;
            movementRange = original.movementRange;
            actionPointsPerTurn = original.actionPointsPerTurn;
            armor = original.armor;
            criticalChance = original.criticalChance;
        }

        // ✅ 매개변수 생성자
        public UnitStats(int maxHealth, int attackPower, int movementRange, int actionPointsPerTurn, int armor = 0, float criticalChance = 0.1f)
        {
            this.maxHealth = Mathf.Max(1, maxHealth);
            this.attackPower = Mathf.Max(0, attackPower);
            this.movementRange = Mathf.Max(1, movementRange);
            this.actionPointsPerTurn = Mathf.Max(1, actionPointsPerTurn);
            this.armor = Mathf.Max(0, armor);
            this.criticalChance = Mathf.Clamp01(criticalChance);
        }

        // ✅ 안전한 수정 메서드 - 유효성 검증 포함
        public void ModifyStats(int healthMod = 0, int attackMod = 0, int movementMod = 0, int armorMod = 0)
        {
            maxHealth = Mathf.Max(1, maxHealth + healthMod);
            attackPower = Mathf.Max(0, attackPower + attackMod);
            movementRange = Mathf.Max(1, movementRange + movementMod);
            armor = Mathf.Max(0, armor + armorMod);
        }

        // ✅ 임시 스탯 수정을 위한 메서드 (버프/디버프용)
        public UnitStats GetModifiedStats(int healthMod = 0, int attackMod = 0, int movementMod = 0, int armorMod = 0, float criticalMod = 0f)
        {
            return new UnitStats(
                maxHealth + healthMod,
                attackPower + attackMod,
                movementRange + movementMod,
                actionPointsPerTurn,
                armor + armorMod,
                Mathf.Clamp01(criticalChance + criticalMod)
            );
        }

        // ✅ 스탯 비교 메서드
        public bool Equals(UnitStats other)
        {
            if (other == null) return false;
            
            return maxHealth == other.maxHealth &&
                   attackPower == other.attackPower &&
                   movementRange == other.movementRange &&
                   actionPointsPerTurn == other.actionPointsPerTurn &&
                   armor == other.armor &&
                   Mathf.Approximately(criticalChance, other.criticalChance);
        }

        // ✅ 디버깅을 위한 ToString 오버라이드
        public override string ToString()
        {
            return $"UnitStats[HP:{maxHealth}, ATK:{attackPower}, MOV:{movementRange}, AP:{actionPointsPerTurn}, ARM:{armor}, CRIT:{criticalChance:P1}]";
        }

        // ✅ 유효성 검증 메서드
        public bool IsValid()
        {
            return maxHealth > 0 && 
                   attackPower >= 0 && 
                   movementRange > 0 && 
                   actionPointsPerTurn > 0 && 
                   armor >= 0 && 
                   criticalChance >= 0f && criticalChance <= 1f;
        }

        // ✅ 스탯 정규화 메서드 (범위를 벗어난 값들을 유효 범위로 조정)
        public void Normalize()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            attackPower = Mathf.Max(0, attackPower);
            movementRange = Mathf.Max(1, movementRange);
            actionPointsPerTurn = Mathf.Max(1, actionPointsPerTurn);
            armor = Mathf.Max(0, armor);
            criticalChance = Mathf.Clamp01(criticalChance);
        }
    }
}