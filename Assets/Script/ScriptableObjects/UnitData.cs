using UnityEngine;
using System.Collections.Generic;
using Game.Data.Modifiers;

namespace Game.Data
{
    /// <summary>
    /// 간소화된 유닛 데이터 ScriptableObject - 핵심 속성만 포함
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit", menuName = "Game/Unit Data", order = 1)]
    public class UnitData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string unitName = "New Unit";
        [SerializeField] private string description = "";
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;

        [Header("기본 스탯")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int attackPower = 10;
        [SerializeField] private int movementRange = 3;

        [Header("능력 Modifiers")]
        [SerializeField] private List<ModifierData> modifiers = new List<ModifierData>();

        // 읽기 전용 속성으로 안전한 외부 접근
        public string UnitName => unitName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public int MaxHealth => maxHealth;
        public int AttackPower => attackPower;
        public int MovementRange => movementRange;
        public List<ModifierData> Modifiers => modifiers;

        // 데이터 유효성 검증
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(unitName) &&
                   prefab != null &&
                   maxHealth > 0 &&
                   attackPower >= 0 &&
                   movementRange >= 0 &&
                   modifiers != null;
        }

        // 개발자용 디버그 정보
        public override string ToString()
        {
            int modifierCount = modifiers != null ? modifiers.Count : 0;
            return $"UnitData[{unitName}, HP:{maxHealth}, ATK:{attackPower}, MOV:{movementRange}, Modifiers:{modifierCount}]";
        }

        // 에디터용 검증 (Unity Editor에서만 실행)
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // 유닛 이름이 비어있으면 파일명으로 설정
            if (string.IsNullOrEmpty(unitName))
            {
                unitName = name;
            }

            // 스탯 값 검증
            maxHealth = Mathf.Max(1, maxHealth);
            attackPower = Mathf.Max(0, attackPower);
            movementRange = Mathf.Max(0, movementRange);
        }
        #endif
    }
}