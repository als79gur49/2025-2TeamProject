using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Card.Effects;
using Game.Utilities;

namespace Game.Data
{
    /// <summary>
    /// 개선된 카드 데이터 ScriptableObject - 완전한 캡슐화와 안전한 접근
    /// </summary>
    [CreateAssetMenu(fileName = "New Card", menuName = "Game/Card Data", order = 2)]
    public class CardData : ScriptableObject
    {
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
            Ground          // 타일이 있는 곳 어디든 배치 가능 (빈 곳)
        }


        // ✅ private 필드 + SerializeField로 Unity Inspector 지원하면서 캡슐화 유지
        [Header("기본 정보")]
        [SerializeField] private string cardID = "";
        [SerializeField] private string cardName = "New Card";
        [SerializeField] private string description = "New Description";
        [SerializeField] private Sprite cardArt;
        [SerializeField] private Sprite iconSprite;

        [Header("카드 속성")]
        [SerializeField] private CardRarity rarity = CardRarity.Common;
        [SerializeField] private int manaCost = 1;

        [Header("대상 및 범위")]
        [SerializeField] private TargetType targetType = TargetType.None;

        [Header("Phase 2.6: 배치 거리 제한")]
        [SerializeField] private int targetRange = -1; // -1: 거리 무관, 0+: 해당 거리까지

        [Header("효과")]
        [SerializeField] private List<string> keywords = new List<string>();

        [Header("새 효과 정의 시스템 (EffectDefinition 기반)")]
        [SerializeField] private List<Game.Card.Effects.EffectDefinition> effectDefinitions = new List<Game.Card.Effects.EffectDefinition>();

        [Header("제약사항")]
        [SerializeField] private int maxCopiesInDeck = 3;
        [SerializeField] private bool isPlayableFromHand = true;
        [SerializeField] private List<string> requiredTags = new List<string>();



        // ✅ 읽기 전용 속성으로 안전한 외부 접근
        public string CardID => cardID;
        public string CardName => cardName;
        public string Description => description;
        public Sprite CardArt => cardArt;
        public Sprite IconSprite => iconSprite;
        public CardRarity Rarity => rarity;
        public int ManaCost => manaCost;
        public TargetType Target => targetType;
        public int TargetRange => targetRange;
        public int MaxCopiesInDeck => maxCopiesInDeck;
        public bool IsPlayableFromHand => isPlayableFromHand;
        public IReadOnlyList<string> Keywords => keywords.AsReadOnly();
        public IReadOnlyList<string> RequiredTags => requiredTags.AsReadOnly();

        /// <summary>
        /// 새로운 EffectDefinition 기반 효과 정의 리스트
        /// EffectDefinitions가 비어있지 않은 경우, 이 정의들을 우선적으로 사용합니다.
        /// </summary>
        public IReadOnlyList<Game.Card.Effects.EffectDefinition> EffectDefinitions => effectDefinitions.AsReadOnly();


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

        // ✅ Phase 2.11: 배치 위치 유효성 검사 (TargetType과 TargetRange 기반)
        // 주의: 이 메서드는 기본적인 거리 검증만 수행하며, 복잡한 팀 기반 배치 검증은 SpawnValidator에서 수행됩니다.
        public bool IsValidTarget(Vector2Int casterPosition, Vector2Int targetPosition)
        {
            // TargetType.None은 타일이 없는 곳에서도 배치 가능
            if (targetType == TargetType.None) return true;

            // Phase 2.11: TargetRange를 사용한 거리 제한 검증
            // -1이면 거리 제한 없음, 0+면 해당 거리까지만 가능
            if (targetRange >= 0)
            {
                //int distance = GridPositionHelper.CalculateYDistance(casterPosition, targetPosition);
                int distance = GridPositionHelper.CalculateYDistance(casterPosition, targetPosition);
                Debug.Log($"distance{distance} | targetRange{targetRange} casterPosition{casterPosition} | targetPosition{targetPosition} ");
                if (distance > targetRange)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Phase 2.11 & 3.1: 맨하탄 거리 계산 (TargetRange 검증용)
        /// 좌표계: 좌하단(0,0) 기준, Vector2Int(x,y) = (col,row) = (가로,세로)
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>맨하탄 거리 (|Δx| + |Δy|)</returns>
        public static int CalculateManhattanDistance(Vector2Int from, Vector2Int to)
        {
            // Phase 3.1: 실제 맨하탄 거리 계산 (x축 거리 + y축 거리)
            // Vector2Int.x = 가로(col), Vector2Int.y = 세로(row)
            return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
        }

        /// <summary>
        /// Phase 2.11: 게임 컨텍스트가 있을 때의 고급 배치 유효성 검사
        /// SpawnValidator가 사용할 수 있는 헬퍼 메서드
        /// </summary>
        /// <param name="originPosition">카드 시전자 위치</param>
        /// <param name="targetPosition">목표 위치</param>
        /// <param name="isPlayerCard">플레이어 카드인지 여부</param>
        /// <returns>기본 유효성 검사 결과</returns>
        public bool IsValidTargetWithContext(Vector2Int originPosition, Vector2Int targetPosition, bool isPlayerCard)
        {
            // 기본 거리 검증
            if (!IsValidTarget(originPosition, targetPosition))
            {
                return false;
            }

            // TargetType별 추가 검증은 SpawnValidator에서 수행되도록 true 반환
            // 실제 팀 기반 검증, 유닛 존재 여부 등은 SpawnValidator.ValidatePlacementTarget에서 처리
            return true;
        }


        /// <summary>
        /// EffectDefinition 기반 효과 시스템을 사용하는지 여부
        /// </summary>
        public bool IsEffectBasedCard => effectDefinitions.Count > 0;

        /// <summary>
        /// Phase 2.10: 카드 효과 실행 메서드
        /// </summary>
        /// <param name="targetPosition">실행 위치</param>
        /// <param name="context">게임 컨텍스트</param>
        /// <returns>실행 성공 여부</returns>
        public bool ExecuteCard(Vector2Int targetPosition, GameContext context)
        {
            if (context == null || !context.IsValid())
            {
                Debug.LogError($"CardData[{cardName}]: 유효하지 않은 GameContext입니다.");
                return false;
            }

            // 기존 ExecuteEffects/CanExecuteAllEffects는 EffectData 기반이므로
            // 현재는 CardSpawnService를 통해 실행하는 것을 권장합니다.
            Debug.LogWarning($"CardData[{cardName}]: ExecuteCard는 더 이상 사용되지 않습니다. CardSpawnService.TryExecuteCard를 사용하세요.");
            return false;
        }

        /// <summary>
        /// Phase 2.10: 카드가 실행 가능한지 종합적으로 판단합니다
        /// </summary>
        /// <param name="targetPosition">실행 위치</param>
        /// <param name="context">게임 컨텍스트</param>
        /// <returns>실행 가능하면 true</returns>
        public bool CanExecuteCard(Vector2Int targetPosition, GameContext context)
        {
            if (context == null || !context.IsValid())
            {
                return false;
            }

            // 마나 비용 체크
            // TODO: 실제 플레이어 마나 체크 로직 구현 필요
            // if (!CanAfford(context.GetAvailableMana(context.PlayerId))) return false;

            // 현재는 CardSpawnService/SpawnValidator에서 실행 가능 여부를 판단합니다.
            return false;
        }


        // ✅ 카드 레어리티별 컬러 반환
        public Color GetRarityColor()
        {
            return rarity switch
            {
                CardRarity.Common => Color.gray,
                CardRarity.Uncommon => Color.green,
                CardRarity.Rare => Color.blue,
                CardRarity.Epic => Color.magenta,
                CardRarity.Legendary => Color.yellow,
                _ => Color.gray
            };
        }

        /// <summary>
        /// Phase 3.16: UI용 상세 설명 생성 - 새로운 카드 구조에 맞게 완전히 재작성
        /// </summary>
        public string GetDetailedDescription()
        {
            var desc = $"<b><color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor())}>{cardName}</color></b>\n";
            desc += $"<i>{rarity}</i>\n\n";

            // 기본 설명
            if (!string.IsNullOrEmpty(description))
            {
                desc += $"{description}\n\n";
            }

            // 효과 시스템 표시 (EffectDefinition 기반)
            if (IsEffectBasedCard)
            {
                desc += "<b>효과:</b>\n";
                desc += GenerateEffectDefinitionDescriptions();
                desc += "\n";
            }

            // 비용 정보
            desc += $"<b>비용:</b> <color=#FFD700>{manaCost}</color> 마나\n";

            // 타겟팅 시스템 정보
            desc += GenerateTargetingDescription();

            // 키워드
            if (keywords.Count > 0)
            {
                desc += $"\n<b>키워드:</b> <color=#87CEEB>{string.Join(", ", keywords)}</color>\n";
            }

            // 데크 제한
            if (maxCopiesInDeck < 3)
            {
                desc += $"\n<color=#FF6B6B>데크에 최대 {maxCopiesInDeck}장 보유 가능</color>\n";
            }

            return desc;
        }

        /// <summary>
        /// EffectDefinition 기반 효과 설명 생성
        /// </summary>
        private string GenerateEffectDefinitionDescriptions()
        {
            if (effectDefinitions == null || effectDefinitions.Count == 0) return "";

            var effectDesc = "";

            foreach (var def in effectDefinitions)
            {
                if (def == null) continue;

                string effectIcon = GetEffectTypeIcon(def.EffectType);
                string effectColor = GetEffectTypeColor(def.EffectType);
                string typeName = GetEffectTypeName(def.EffectType);

                string detail = typeName;

                switch (def)
                {
                    case DamageEffectDefinition dmg:
                        detail = $"{typeName} {dmg.DamageAmount}";
                        break;
                    case HealEffectDefinition heal:
                        detail = $"{typeName} {heal.HealAmount}";
                        break;
                    case SummonEffectDefinition summon when summon.UnitToSummon != null:
                        detail = $"{summon.UnitToSummon.UnitName} 소환 x{summon.Count}";
                        break;
                    case ReturnUnitsToHandEffectDefinition returnDef:
                        if (returnDef.MaxUnitsToReturn > 0)
                        {
                            detail = $"{typeName} (최대 {returnDef.MaxUnitsToReturn} 유닛)";
                        }
                        else
                        {
                            detail = $"{typeName}";
                        }
                        break;
                }

                effectDesc += $"• <color={effectColor}>{effectIcon} {detail}</color>\n";
            }

            return effectDesc;
        }

        /// <summary>
        /// Phase 3.16: 타겟팅 시스템 설명 생성
        /// </summary>
        private string GenerateTargetingDescription()
        {
            var targetDesc = "";

            // 배치 제한 정보
            if (targetType != TargetType.None)
            {
                string placementDesc = targetType switch
                {
                    TargetType.Ally => "아군 위치에만",
                    TargetType.Enemy => "적군 위치에만",
                    TargetType.Any => "아군/적군 위치에",
                    TargetType.Ground => "빈 타일에",
                    _ => targetType.ToString()
                };

                targetDesc += $"<b>배치:</b> {placementDesc} 사용 가능";

                // 거리 제한
                if (targetRange >= 0)
                {
                    targetDesc += $" <color=#FFA500>(거리 제한: {targetRange})</color>";
                }

                targetDesc += "\n";
            }

            if (effectDefinitions != null && effectDefinitions.Count > 0)
            {
                // 범위 효과 정보 (AreaShape 기반)
                var shapes = effectDefinitions
                    .Where(d => d != null && d.AreaShape != null)
                    .Select(d => d.AreaShape)
                    .Distinct();

                var rangeDescriptions = shapes
                    .Select(s => s.GetRangeDescription())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();

                if (rangeDescriptions.Count > 0)
                {
                    var joined = string.Join(", ", rangeDescriptions);
                    targetDesc += $"<b>효과 범위:</b> <color=#32CD32>{joined}</color>\n";
                }

                // 대상 필터 정보 (TargetFilter 기반)
                var filterDescriptions = effectDefinitions
                    .Where(d => d != null && d.TargetFilter != null)
                    .Select(d => d.TargetFilter.GetTargetDescription())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();

                if (filterDescriptions.Count > 0)
                {
                    var joinedTargets = string.Join(", ", filterDescriptions);
                    targetDesc += $"<b>효과 대상:</b> {joinedTargets}\n";
                }
            }

            return targetDesc;
        }

        /// <summary>
        /// Phase 3.16: 효과 타입별 아이콘 반환
        /// </summary>
        private string GetEffectTypeIcon(EffectType effectType)
        {
            return effectType switch
            {
                EffectType.Damage => "⚔️",
                EffectType.Heal => "💚",
                EffectType.Summon => "🛡️",
                EffectType.DrawCards => "📥",
                EffectType.Stun => "💫",
                EffectType.HealBase => "🏰",
                EffectType.DamageBase => "🔥",
                EffectType.ReturnToHand => "🔄",
                _ => "✨"
            };
        }

        /// <summary>
        /// Phase 3.16: 효과 타입별 색상 반환
        /// </summary>
        private string GetEffectTypeColor(EffectType effectType)
        {
            return effectType switch
            {
                EffectType.Damage => "#FF6B6B",
                EffectType.Heal => "#51CF66",
                EffectType.Summon => "#4DABF7",
                EffectType.DrawCards => "#FFD43B",
                EffectType.Stun => "#B197FC",
                EffectType.HealBase => "#69DB7C",
                EffectType.DamageBase => "#FF922B",
                EffectType.ReturnToHand => "#74C0FC",
                _ => "#ADB5BD"
            };
        }

        /// <summary>
        /// Phase 3.16: 효과 타입별 이름 반환
        /// </summary>
        private string GetEffectTypeName(EffectType effectType)
        {
            return effectType switch
            {
                EffectType.Damage => "피해",
                EffectType.Heal => "회복",
                EffectType.Summon => "소환",
                EffectType.DrawCards => "드로우",
                EffectType.Stun => "기절",
                EffectType.HealBase => "베이스 회복",
                EffectType.DamageBase => "베이스 피해",
                EffectType.ReturnToHand => "손패로 되돌리기",
                _ => effectType.ToString()
            };
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
                            targetRange >= -1 &&
                            maxCopiesInDeck > 0;

            // EffectDefinition 기반 검증: 정의가 하나 이상 있어야 하며 null이 아니어야 함
            bool effectDefinitionsValid = effectDefinitions != null &&
                                          effectDefinitions.Count > 0 &&
                                          effectDefinitions.All(e => e != null);

            return baseValid && effectDefinitionsValid;
        }

        // ✅ 에디터용 검증
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // 카드 ID가 비어있으면 파일명으로 설정 (고유 식별자)
            if (string.IsNullOrEmpty(cardID))
            {
                cardID = name;
            }

            // 카드 이름이 비어있으면 파일명으로 설정
            if (string.IsNullOrEmpty(cardName))
            {
                cardName = name;
            }

            // 비용과 범위는 음수가 될 수 없음 (targetRange는 -1 허용)
            manaCost = Mathf.Max(0, manaCost);
            targetRange = Mathf.Max(-1, targetRange); // -1은 거리 무관을 의미
            maxCopiesInDeck = Mathf.Max(1, maxCopiesInDeck);

            // Phase 2.12: 레거시 필드들 완전 제거 완료
            // 새로운 EffectData 시스템만 사용

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



        // EffectDefinition 기반 카드 생성 헬퍼가 필요하면 이후 별도 도입 예정입니다.
    }
}
