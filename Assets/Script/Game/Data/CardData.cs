using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 개선된 카드 데이터 ScriptableObject - 완전한 캡슐화와 안전한 접근
    /// </summary>
    [CreateAssetMenu(fileName = "New Card", menuName = "Game/Card Data", order = 2)]
    public class CardData : ScriptableObject
    {
        // ✅ 카드 타입 열거형
        public enum CardType
        {
            Unit,       // 유닛 소환
            Spell,      // 주문
            Equipment,  // 장비
            Building,   // 건물
            Event       // 이벤트
        }

        public enum CardRarity
        {
            Common,     // 일반
            Uncommon,   // 희귀
            Rare,       // 전설
            Epic,       // 영웅
            Legendary   // 신화
        }

        public enum TargetType
        {
            None,           // 대상 없음
            Self,           // 자신
            Ally,           // 아군
            Enemy,          // 적군
            Any,            // 아무나
            Ground,         // 지면
            AllAllies,      // 모든 아군
            AllEnemies,     // 모든 적군
            All             // 모든 유닛
        }

        // ✅ private 필드 + SerializeField로 Unity Inspector 지원하면서 캡슐화 유지
        [Header("기본 정보")]
        [SerializeField] private string cardName = "New Card";
        [SerializeField] private string description = "";
        [SerializeField] private string flavorText = "";
        [SerializeField] private Sprite cardArt;
        [SerializeField] private Sprite iconSprite;

        [Header("카드 속성")]
        [SerializeField] private CardType cardType = CardType.Spell;
        [SerializeField] private CardRarity rarity = CardRarity.Common;
        [SerializeField] private int manaCost = 1;
        [SerializeField] private int actionCost = 1;

        [Header("대상 및 범위")]
        [SerializeField] private TargetType targetType = TargetType.None;
        [SerializeField] private int range = 0;
        [SerializeField] private int areaOfEffect = 0; // 0 = 단일 대상, 1+ = 범위 효과

        [Header("효과")]
        [SerializeField] private List<CardEffect> effects = new List<CardEffect>();
        [SerializeField] private List<string> keywords = new List<string>();

        [Header("제약사항")]
        [SerializeField] private int maxCopiesInDeck = 3;
        [SerializeField] private bool isPlayableFromHand = true;
        [SerializeField] private List<string> requiredTags = new List<string>();

        [Header("유닛 관련 (유닛 카드인 경우)")]
        [SerializeField] private UnitData unitToSummon;

        // ✅ 카드 효과 정의
        [System.Serializable]
        public class CardEffect
        {
            [SerializeField] private string effectType = "Damage";
            [SerializeField] private int value = 1;
            [SerializeField] private string targetFilter = "";
            [SerializeField] private bool isInstant = true;
            [SerializeField] private float duration = 0f;

            public string EffectType => effectType;
            public int Value => value;
            public string TargetFilter => targetFilter;
            public bool IsInstant => isInstant;
            public float Duration => duration;

            public CardEffect(string type, int val, string filter = "", bool instant = true, float dur = 0f)
            {
                effectType = type;
                value = val;
                targetFilter = filter;
                isInstant = instant;
                duration = dur;
            }

            public override string ToString()
            {
                return $"{effectType}: {value} {(IsInstant ? "(즉시)" : $"({duration}s)")}";
            }
        }

        // ✅ 읽기 전용 속성으로 안전한 외부 접근
        public string CardName => cardName;
        public string Description => description;
        public string FlavorText => flavorText;
        public Sprite CardArt => cardArt;
        public Sprite IconSprite => iconSprite;
        public CardType Type => cardType;
        public CardRarity Rarity => rarity;
        public int ManaCost => manaCost;
        public int ActionCost => actionCost;
        public TargetType Target => targetType;
        public int Range => range;
        public int AreaOfEffect => areaOfEffect;
        public int MaxCopiesInDeck => maxCopiesInDeck;
        public bool IsPlayableFromHand => isPlayableFromHand;
        public IReadOnlyList<CardEffect> Effects => effects.AsReadOnly();
        public IReadOnlyList<string> Keywords => keywords.AsReadOnly();
        public IReadOnlyList<string> RequiredTags => requiredTags.AsReadOnly();

        // ✅ 유닛 소환 관련 안전한 접근
        public UnitData GetUnitToSummon()
        {
            return unitToSummon; // ScriptableObject는 참조를 반환해도 원본이 수정되지 않음 (에디터에서만 수정 가능)
        }

        public bool CanSummonUnit => cardType == CardType.Unit && unitToSummon != null;

        // ✅ 카드 비용 관련 메서드
        public bool CanAfford(int availableMana, int availableActions)
        {
            return availableMana >= manaCost && availableActions >= actionCost;
        }

        public int GetTotalCost()
        {
            return manaCost + actionCost;
        }

        // ✅ 키워드 관련 안전한 메서드
        public bool HasKeyword(string keyword)
        {
            return !string.IsNullOrEmpty(keyword) && keywords.Contains(keyword);
        }

        public bool HasAnyKeyword(params string[] checkKeywords)
        {
            if (checkKeywords == null || checkKeywords.Length == 0) return false;
            return checkKeywords.Any(keyword => HasKeyword(keyword));
        }

        // ✅ 요구사항 검증
        public bool MeetsRequirements(List<string> availableTags)
        {
            if (requiredTags.Count == 0) return true;
            if (availableTags == null) return false;
            
            return requiredTags.All(tag => availableTags.Contains(tag));
        }

        // ✅ 대상 유효성 검사
        public bool IsValidTarget(Vector2Int casterPosition, Vector2Int targetPosition)
        {
            if (targetType == TargetType.None) return true;
            
            float distance = Vector2Int.Distance(casterPosition, targetPosition);
            return range <= 0 || distance <= range;
        }

        // ✅ 효과 관련 메서드
        public List<CardEffect> GetEffectsOfType(string effectType)
        {
            return effects.Where(e => e.EffectType.Equals(effectType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public int GetTotalEffectValue(string effectType)
        {
            return GetEffectsOfType(effectType).Sum(e => e.Value);
        }

        public bool HasEffect(string effectType)
        {
            return effects.Any(e => e.EffectType.Equals(effectType, StringComparison.OrdinalIgnoreCase));
        }

        // ✅ 카드 레어리티별 컬러 반환
        public Color GetRarityColor()
        {
            return rarity switch
            {
                CardRarity.Common => Color.white,
                CardRarity.Uncommon => Color.green,
                CardRarity.Rare => Color.blue,
                CardRarity.Epic => Color.magenta,
                CardRarity.Legendary => Color.yellow,
                _ => Color.gray
            };
        }

        // ✅ UI용 상세 설명 생성
        public string GetDetailedDescription()
        {
            var desc = $"<b><color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor())}>{cardName}</color></b>\n";
            desc += $"<i>{cardType} - {rarity}</i>\n\n";
            desc += $"{description}\n\n";
            
            if (effects.Count > 0)
            {
                desc += "<b>효과:</b>\n";
                foreach (var effect in effects)
                {
                    desc += $"• {effect}\n";
                }
                desc += "\n";
            }
            
            desc += $"<b>비용:</b> 마나 {manaCost}, 행동력 {actionCost}\n";
            
            if (targetType != TargetType.None)
            {
                desc += $"<b>대상:</b> {targetType}";
                if (range > 0) desc += $" (사거리: {range})";
                if (areaOfEffect > 0) desc += $" (범위: {areaOfEffect})";
                desc += "\n";
            }
            
            if (keywords.Count > 0)
            {
                desc += $"<b>키워드:</b> {string.Join(", ", keywords)}\n";
            }
            
            if (!string.IsNullOrEmpty(flavorText))
            {
                desc += $"\n<i>\"{flavorText}\"</i>";
            }
            
            return desc;
        }

        // ✅ 카드 복사 (덱 구성용)
        public CardData CreateCopy()
        {
            var copy = Instantiate(this);
            copy.name = $"{cardName}_Copy";
            return copy;
        }

        // ✅ 데이터 유효성 검증
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(cardName) &&
                   manaCost >= 0 &&
                   actionCost >= 0 &&
                   range >= 0 &&
                   areaOfEffect >= 0 &&
                   maxCopiesInDeck > 0 &&
                   (cardType != CardType.Unit || unitToSummon != null);
        }

        // ✅ 디버깅용 ToString
        public override string ToString()
        {
            return $"CardData[{cardName} ({cardType}, {rarity}), Cost: {manaCost}M/{actionCost}A]";
        }

        // ✅ 에디터용 검증
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // 카드 이름이 비어있으면 파일명으로 설정
            if (string.IsNullOrEmpty(cardName))
            {
                cardName = name;
            }

            // 비용과 범위는 음수가 될 수 없음
            manaCost = Mathf.Max(0, manaCost);
            actionCost = Mathf.Max(0, actionCost);
            range = Mathf.Max(0, range);
            areaOfEffect = Mathf.Max(0, areaOfEffect);
            maxCopiesInDeck = Mathf.Max(1, maxCopiesInDeck);

            // 유닛 카드가 아니면 unitToSummon을 null로 설정
            if (cardType != CardType.Unit)
            {
                unitToSummon = null;
            }

            // 중복 키워드 제거
            if (keywords != null)
            {
                keywords = keywords.Where(k => !string.IsNullOrEmpty(k))
                                 .Distinct()
                                 .ToList();
            }

            // 중복 요구 태그 제거
            if (requiredTags != null)
            {
                requiredTags = requiredTags.Where(t => !string.IsNullOrEmpty(t))
                                         .Distinct()
                                         .ToList();
            }
        }
        #endif

        // ✅ 런타임 생성용 팩토리 메서드
        public static CardData CreateSpellCard(string name, string desc, int manaCost, int actionCost, params CardEffect[] effects)
        {
            var card = CreateInstance<CardData>();
            card.cardName = name;
            card.description = desc;
            card.cardType = CardType.Spell;
            card.manaCost = manaCost;
            card.actionCost = actionCost;
            card.effects = effects?.ToList() ?? new List<CardEffect>();
            
            return card;
        }

        public static CardData CreateUnitCard(string name, UnitData unit, int manaCost, int actionCost)
        {
            var card = CreateInstance<CardData>();
            card.cardName = name;
            card.cardType = CardType.Unit;
            card.unitToSummon = unit;
            card.manaCost = manaCost;
            card.actionCost = actionCost;
            card.description = $"{unit.UnitName}을(를) 소환합니다.";
            
            return card;
        }
    }
}