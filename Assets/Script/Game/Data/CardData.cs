using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Card.Effects;

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
        //    Equipment,  // 장비
        //    Building,   // 건물
         //   Event       // 이벤트
        }

        public enum CardRarity
        {
            Common,     // 일반
            Uncommon,   // 희귀
            Rare,       // 전설
            Epic,       // 영웅
            Legendary   // 신화
        }

        /// <summary>
        /// Phase 2.5: 배치 가능 위치 검증을 위한 대상 타입
        /// 기존 주문 대상 지정에서 카드 배치 위치 검증으로 의미 변경
        /// </summary>
        public enum TargetType
        {
            None,           // 타일이 없는 곳에서도 배치 가능
            Ally,           // 아군 위치에서만 배치 가능
            Enemy,          // 적군 위치에서만 배치 가능
            Any,            // 적군, 아군 모두 배치 가능
            Ground          // 타일이 있는 곳 어디든 배치 가능
        }

        /// <summary>
        /// 주문 카드 타입 열거형 - Phase 4에서 확장 구현
        /// CardSpawnService에서 주문 효과 적용에 사용됩니다.
        /// </summary>
        public enum SpellType
        {
            /// <summary>데미지 주문 - 대상에게 피해를 줍니다</summary>
            Damage,

            /// <summary>힐 주문 - 대상을 회복시킵니다</summary>
            Heal,

            /// <summary>버프 주문 - 대상을 강화합니다</summary>
            Buff,

            /// <summary>디버프 주문 - 대상을 약화시킵니다</summary>
            Debuff,

            /// <summary>실드 주문 - 대상에게 보호막을 제공합니다</summary>
            Shield,

            /// <summary>텔레포트 주문 - 대상을 이동시킵니다</summary>
            Teleport,

            /// <summary>소환 주문 - 새로운 유닛을 소환합니다</summary>
            Summon
        }

        // ✅ private 필드 + SerializeField로 Unity Inspector 지원하면서 캡슐화 유지
        [Header("기본 정보")]
        [SerializeField] private string cardName = "New Card";
        [SerializeField] private string description = "";
        [SerializeField] private Sprite cardArt;
        [SerializeField] private Sprite iconSprite;

        [Header("카드 속성")]
        [SerializeField] private CardType cardType = CardType.Spell;
        [SerializeField] private CardRarity rarity = CardRarity.Common;
        [SerializeField] private int manaCost = 1;

        [Header("대상 및 범위")]
        [SerializeField] private TargetType targetType = TargetType.None;
        [SerializeField] private int range = 0;
        [SerializeField] private int areaOfEffect = 0; // 0 = 단일 대상, 1+ = 범위 효과

        [Header("Phase 2.6: 배치 거리 제한")]
        [SerializeField] private int targetRange = -1; // -1: 거리 무관, 0+: 해당 거리까지

        [Header("Phase 2.7: 효과 범위")]
        [SerializeField] private int affectedRange = 0; // 0: 단일 대상, 1+: 범위 효과

        [Header("효과")]
        [SerializeField] private List<CardEffect> effects = new List<CardEffect>();
        [SerializeField] private List<string> keywords = new List<string>();

        [Header("새로운 효과 시스템 (Phase 2.4)")]
        [SerializeField] private List<EffectData> effectDataList = new List<EffectData>();

        [Header("제약사항")]
        [SerializeField] private int maxCopiesInDeck = 3;
        [SerializeField] private bool isPlayableFromHand = true;
        [SerializeField] private List<string> requiredTags = new List<string>();

        [Header("유닛 관련 (유닛 카드인 경우)")]
        [SerializeField] private UnitData unitToSummon;

        [Header("주문 관련 (주문 카드인 경우)")]
        [SerializeField] private SpellType spellType = SpellType.Damage;
        [SerializeField] private int spellEffectValue = 0;
        [SerializeField] private float spellRange = 0f;
        [SerializeField] private float spellCooldown = 0f;
        [SerializeField] private GameObject spellEffectPrefab;

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
        public Sprite CardArt => cardArt;
        public Sprite IconSprite => iconSprite;
        public CardType Type => cardType;
        public CardRarity Rarity => rarity;
        public int ManaCost => manaCost;
        public TargetType Target => targetType;
        public int Range => range;
        public int AreaOfEffect => areaOfEffect;
        public int TargetRange => targetRange;
        public int AffectedRange => affectedRange;
        public int MaxCopiesInDeck => maxCopiesInDeck;
        public bool IsPlayableFromHand => isPlayableFromHand;
        public IReadOnlyList<CardEffect> Effects => effects.AsReadOnly();
        public IReadOnlyList<string> Keywords => keywords.AsReadOnly();
        public IReadOnlyList<string> RequiredTags => requiredTags.AsReadOnly();

        // ✅ 새로운 EffectData 시스템 접근자 (Phase 2.4)
        public IReadOnlyList<EffectData> EffectDataList => effectDataList.AsReadOnly();

        // ✅ 유닛 소환 관련 안전한 접근
        public UnitData GetUnitToSummon()
        {
            return unitToSummon; // ScriptableObject는 참조를 반환해도 원본이 수정되지 않음 (에디터에서만 수정 가능)
        }

        public bool CanSummonUnit => cardType == CardType.Unit && unitToSummon != null;
        public bool IsSpellCard => cardType == CardType.Spell;

        // ✅ 주문 관련 읽기 전용 속성
        public SpellType SpellCategory => spellType;
        public int SpellEffectValue => spellEffectValue;
        public float SpellRange => spellRange;
        public float SpellCooldown => spellCooldown;
        public GameObject SpellEffectPrefab => spellEffectPrefab;
        public bool HasValidSpellData => cardType == CardType.Spell && spellEffectValue > 0;

        // ✅ 카드 비용 관련 메서드
        public bool CanAfford(int availableMana)
        {
            return availableMana >= manaCost;
        }

        public int GetTotalCost()
        {
            return manaCost;
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

        // ✅ 배치 위치 유효성 검사 (Phase 2.5: 의미 변경)
        // 주의: 이 메서드는 거리만 확인하며, 실제 팀 기반 배치 검증은 SpawnValidator에서 수행됩니다.
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

        // ✅ 새로운 EffectData 시스템 메서드들 (Phase 2.4)

        /// <summary>
        /// 지정된 효과 타입의 모든 EffectData를 반환합니다
        /// </summary>
        public List<EffectData> GetEffectsByType(EffectType effectType)
        {
            return effectDataList.Where(e => e.Type == effectType).ToList();
        }

        /// <summary>
        /// 지정된 효과 타입을 가지고 있는지 확인합니다
        /// </summary>
        public bool HasEffectType(EffectType effectType)
        {
            return effectDataList.Any(e => e.Type == effectType);
        }

        /// <summary>
        /// 지정된 효과 타입의 총 값을 반환합니다
        /// </summary>
        public int GetTotalEffectValue(EffectType effectType)
        {
            return effectDataList.Where(e => e.Type == effectType).Sum(e => e.Value);
        }

        /// <summary>
        /// 카드의 주요 효과 타입을 반환합니다 (가장 높은 값을 가진 효과)
        /// </summary>
        public EffectType? GetPrimaryEffectType()
        {
            if (effectDataList.Count == 0) return null;

            var primaryEffect = effectDataList.OrderByDescending(e => e.Value).FirstOrDefault();
            return primaryEffect?.Type;
        }

        /// <summary>
        /// 새로운 EffectData 시스템을 사용하는지 확인합니다
        /// </summary>
        public bool IsEffectBasedCard => effectDataList.Count > 0;

        /// <summary>
        /// EffectData 시스템에서의 카드 설명을 생성합니다
        /// </summary>
        public string GetEffectDataDescription()
        {
            if (effectDataList.Count == 0) return "";

            var descriptions = effectDataList.Select(e => e.GetDescription());
            return string.Join(", ", descriptions);
        }

        /// <summary>
        /// Phase 2.8: AffectedRange 기반 범위 내 위치들을 반환합니다
        /// </summary>
        public List<Vector2Int> GetAffectedPositions(Vector2Int targetPosition, EffectData effectData)
        {
            var positions = new List<Vector2Int>();

            if (effectData == null) return positions;

            // AffectedRange가 0이면 단일 위치만 영향
            if (effectData.AffectedRange == 0)
            {
                positions.Add(targetPosition);
                return positions;
            }

            // AffectedRange가 1+이면 범위 내 모든 위치 포함
            for (int x = -effectData.AffectedRange; x <= effectData.AffectedRange; x++)
            {
                for (int y = -effectData.AffectedRange; y <= effectData.AffectedRange; y++)
                {
                    var pos = new Vector2Int(targetPosition.x + x, targetPosition.y + y);
                    positions.Add(pos);
                }
            }

            return positions;
        }

        /// <summary>
        /// Phase 2.8: 지정된 위치가 AffectedRange 내에 있는지 확인합니다
        /// </summary>
        public bool IsPositionInAffectedRange(Vector2Int targetPosition, Vector2Int checkPosition, EffectData effectData)
        {
            if (effectData == null) return false;

            // AffectedRange가 0이면 정확히 동일한 위치만 유효
            if (effectData.AffectedRange == 0)
            {
                return targetPosition == checkPosition;
            }

            // 맨하탄 거리로 범위 체크
            int distance = Mathf.Abs(targetPosition.x - checkPosition.x) + Mathf.Abs(targetPosition.y - checkPosition.y);
            return distance <= effectData.AffectedRange;
        }

        /// <summary>
        /// Phase 2.8: 카드의 최대 AffectedRange 값을 반환합니다
        /// </summary>
        public int GetMaxAffectedRange()
        {
            if (effectDataList.Count == 0) return affectedRange; // 레거시 시스템 fallback

            return effectDataList.Max(e => e.AffectedRange);
        }

        /// <summary>
        /// Phase 2.8: 지정된 효과 타입의 AffectedRange 값을 반환합니다
        /// </summary>
        public int GetAffectedRangeForEffectType(EffectType effectType)
        {
            var effects = GetEffectsByType(effectType);
            if (effects.Count == 0) return 0;

            return effects.Max(e => e.AffectedRange);
        }

        // ✅ 주문 관련 헬퍼 메서드
        /// <summary>
        /// 주문 타입별 설명 텍스트 생성
        /// </summary>
        public string GetSpellDescription()
        {
            if (!IsSpellCard) return "";
            
            return spellType switch
            {
                SpellType.Damage => $"{spellEffectValue} 피해를 입힙니다",
                SpellType.Heal => $"{spellEffectValue} 체력을 회복시킵니다",
                SpellType.Buff => $"{spellEffectValue}만큼 강화합니다",
                SpellType.Debuff => $"{spellEffectValue}만큼 약화시킵니다",
                SpellType.Shield => $"{spellEffectValue} 보호막을 생성합니다",
                SpellType.Teleport => $"최대 {spellRange} 거리만큼 이동시킵니다",
                SpellType.Summon => $"{spellEffectValue}개의 유닛을 소환합니다",
                _ => "알 수 없는 주문 효과"
            };
        }

        /// <summary>
        /// 주문 범위 유효성 검사
        /// </summary>
        public bool IsSpellInRange(Vector2Int casterPosition, Vector2Int targetPosition)
        {
            if (!IsSpellCard) return false;
            if (spellRange <= 0) return true; // 범위 제한 없음
            
            float distance = Vector2Int.Distance(casterPosition, targetPosition);
            return distance <= spellRange;
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
            
            // 주문 카드 전용 설명 추가
            if (IsSpellCard && HasValidSpellData)
            {
                desc += $"<b>주문 효과:</b> {GetSpellDescription()}\n";
                if (spellRange > 0)
                {
                    desc += $"<b>주문 범위:</b> {spellRange}\n";
                }
                if (spellCooldown > 0)
                {
                    desc += $"<b>재사용 대기시간:</b> {spellCooldown}초\n";
                }
                desc += "\n";
            }
            
            if (effects.Count > 0)
            {
                desc += "<b>효과:</b>\n";
                foreach (var effect in effects)
                {
                    desc += $"• {effect}\n";
                }
                desc += "\n";
            }

            // EffectData 시스템 정보 표시 (Phase 2.4 + 2.8)
            if (effectDataList.Count > 0)
            {
                desc += "<b>새로운 효과 시스템:</b>\n";
                foreach (var effectData in effectDataList)
                {
                    desc += $"• {effectData.GetDescription()}";

                    // Phase 2.8: AffectedType과 AffectedRange 정보 추가
                    if (effectData.AffectedType != AffectedType.None)
                    {
                        string affectedTypeDesc = effectData.AffectedType switch
                        {
                            AffectedType.Ally => "아군",
                            AffectedType.Enemy => "적군",
                            AffectedType.Any => "모두",
                            _ => effectData.AffectedType.ToString()
                        };
                        desc += $" ({affectedTypeDesc} 대상";

                        if (effectData.AffectedRange > 0)
                        {
                            desc += $", 효과범위: {effectData.AffectedRange}";
                        }
                        desc += ")";
                    }
                    desc += "\n";
                }
                desc += "\n";
            }
            
            desc += $"<b>비용:</b> 마나 {manaCost}\n";
            
            if (targetType != TargetType.None)
            {
                string targetDescription = targetType switch
                {
                    TargetType.Ally => "아군 위치에만 배치 가능",
                    TargetType.Enemy => "적군 위치에만 배치 가능",
                    TargetType.Any => "아군/적군 위치 모두 배치 가능",
                    TargetType.Ground => "타일이 있는 곳 어디든 배치 가능",
                    _ => targetType.ToString()
                };
                desc += $"<b>배치 제한:</b> {targetDescription}";
                if (range > 0) desc += $" (사거리: {range})";
                if (targetRange >= 0) desc += $" (배치 거리: {targetRange})";
                if (areaOfEffect > 0) desc += $" (범위: {areaOfEffect})";
                if (affectedRange > 0) desc += $" (효과 범위: {affectedRange})";
                desc += "\n";
            }
            
            if (keywords.Count > 0)
            {
                desc += $"<b>키워드:</b> {string.Join(", ", keywords)}\n";
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
            bool baseValid = !string.IsNullOrEmpty(cardName) &&
                            manaCost >= 0 &&
                            range >= 0 &&
                            areaOfEffect >= 0 &&
                            targetRange >= -1 &&
                            affectedRange >= 0 &&
                            maxCopiesInDeck > 0;

            // 카드 타입별 추가 검증 (레거시 시스템)
            bool typeValid = cardType switch
            {
                CardType.Unit => unitToSummon != null,
                CardType.Spell => spellEffectValue > 0,
                _ => true
            };

            // EffectData 시스템 검증 (Phase 2.4 + 2.8)
            bool effectDataValid = true;
            if (effectDataList.Count > 0)
            {
                effectDataValid = effectDataList.All(e => e.IsValid() && e.AffectedRange >= 0);
            }

            return baseValid && typeValid && effectDataValid;
        }

        // ✅ 디버깅용 ToString
        public override string ToString()
        {
            return $"CardData[{cardName} ({cardType}, {rarity}), Cost: {manaCost}M]";
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

            // 비용과 범위는 음수가 될 수 없음 (targetRange는 -1 허용)
            manaCost = Mathf.Max(0, manaCost);
            range = Mathf.Max(0, range);
            areaOfEffect = Mathf.Max(0, areaOfEffect);
            targetRange = Mathf.Max(-1, targetRange); // -1은 거리 무관을 의미
            affectedRange = Mathf.Max(0, affectedRange); // 0은 단일 대상을 의미
            maxCopiesInDeck = Mathf.Max(1, maxCopiesInDeck);

            // 유닛 카드가 아니면 unitToSummon을 null로 설정
            if (cardType != CardType.Unit)
            {
                unitToSummon = null;
            }

            // 주문 카드 검증 추가
            if (cardType == CardType.Spell)
            {
                spellEffectValue = Mathf.Max(0, spellEffectValue);
                spellRange = Mathf.Max(0f, spellRange);
                spellCooldown = Mathf.Max(0f, spellCooldown);
            }
            else
            {
                // 주문이 아닌 카드의 주문 데이터 초기화
                spellType = SpellType.Damage;
                spellEffectValue = 0;
                spellRange = 0f;
                spellCooldown = 0f;
                spellEffectPrefab = null;
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

            // EffectData 검증 및 정리 (Phase 2.4)
            if (effectDataList != null)
            {
                // 무효한 EffectData 제거
                effectDataList = effectDataList.Where(e => e != null && e.IsValid()).ToList();

                // 중복 효과 제거 (같은 타입과 값을 가진 효과)
                var distinctEffects = new List<EffectData>();
                foreach (var effect in effectDataList)
                {
                    if (!distinctEffects.Any(de => de.Type == effect.Type &&
                                                  de.Value == effect.Value &&
                                                  de.AffectedType == effect.AffectedType))
                    {
                        distinctEffects.Add(effect);
                    }
                }
                effectDataList = distinctEffects;
            }
        }
        #endif

        // ✅ 런타임 생성용 팩토리 메서드
        public static CardData CreateSpellCard(string name, string desc, int manaCost, params CardEffect[] effects)
        {
            var card = CreateInstance<CardData>();
            card.cardName = name;
            card.description = desc;
            card.cardType = CardType.Spell;
            card.manaCost = manaCost;
            card.effects = effects?.ToList() ?? new List<CardEffect>();
            
            return card;
        }

        /// <summary>
        /// 주문 카드 생성을 위한 팩토리 메서드 (확장)
        /// </summary>
        public static CardData CreateSpellCard(string name, string desc, int manaCost,
            SpellType spellType, int effectValue, float range = 0f, float cooldown = 0f)
        {
            var card = CreateInstance<CardData>();
            card.cardName = name;
            card.description = desc;
            card.cardType = CardType.Spell;
            card.manaCost = manaCost;
            card.spellType = spellType;
            card.spellEffectValue = effectValue;
            card.spellRange = range;
            card.spellCooldown = cooldown;
            
            return card;
        }

        public static CardData CreateUnitCard(string name, UnitData unit, int manaCost)
        {
            var card = CreateInstance<CardData>();
            card.cardName = name;
            card.cardType = CardType.Unit;
            card.unitToSummon = unit;
            card.manaCost = manaCost;
            card.description = $"{unit.UnitName}을(를) 소환합니다.";
            
            return card;
        }
    }
}