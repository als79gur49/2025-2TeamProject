using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Card.Effects;
using Game.Card;
    /// <summary>
    /// 개선된 카드 데이터 ScriptableObject - 완전한 캡슐화와 안전한 접근
    /// </summary>
    [CreateAssetMenu(fileName = "Ne", menuName = "Ga", order = 2)]
    public class TestSOSerial : ScriptableObject
    {
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
            None,           // 타일이 없는 곳에서도 배치 가능
            Ally,           // 아군 위치에서만 배치 가능
            Enemy,          // 적군 위치에서만 배치 가능
            Any,            // 적군, 아군 모두 배치 가능
            Ground          // 타일이 있는 곳 어디든 배치 가능
        }


        [Header("기본 정보")]
        [SerializeField] private string cardName = "New Card";
        [SerializeField] private string description = "";
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

        [Header("효과 시스템 (Phase 2.4)")]
        [SerializeField] private List<EffectData> effectDataList = new List<EffectData>();

        [Header("제약사항")]
        [SerializeField] private int maxCopiesInDeck = 3;
        [SerializeField] private bool isPlayableFromHand = true;
        [SerializeField] private List<string> requiredTags = new List<string>();


        
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

       
        public IReadOnlyList<EffectData> EffectDataList => effectDataList.AsReadOnly();


    private void OnValidate()
    {
        // EffectData 검증 및 정리 (Phase 2.4)
        if (effectDataList != null)
        {
            // Unity Inspector에서 추가된 새 EffectData 인스턴스 초기화
            foreach (var effect in effectDataList)
            {
                if (effect != null && !effect.Initialized)
                {
                    effect.Initialize();
                }
            }

            // 무효한 EffectData 제거
             effectDataList = effectDataList.Where(e => e != null && e.IsValid()).ToList();
            
             // 중복 효과 제거 (같은 타입과 값을 가진 효과) 필요 시 로그를 띄우는 방식으로 변경하기
           //  var distinctEffects = new List<EffectData>();
           //  foreach (var effect in effectDataList)
           //  {
           //      if (!distinctEffects.Any(de => de.Type == effect.Type &&
           //                                    de.Value == effect.Value &&
           //                                    de.AffectedType == effect.AffectedType))
           //      {
           //          distinctEffects.Add(effect);
           //      }
           //  }
           //  effectDataList = distinctEffects;
        }
    }


    public bool CanAfford(int availableMana)
        {
            return availableMana >= manaCost;
        }

        public int GetTotalCost()
        {
            return manaCost;
        }

      
        public bool HasKeyword(string keyword)
        {
            return !string.IsNullOrEmpty(keyword) && keywords.Contains(keyword);
        }

        public bool HasAnyKeyword(params string[] checkKeywords)
        {
            if (checkKeywords == null || checkKeywords.Length == 0) return false;
            return checkKeywords.Any(keyword => HasKeyword(keyword));
        }

      
        public bool MeetsRequirements(List<string> availableTags)
        {
            if (requiredTags.Count == 0) return true;
            if (availableTags == null) return false;

            return requiredTags.All(tag => availableTags.Contains(tag));
        }

        
        public bool IsValidTarget(Vector2Int casterPosition, Vector2Int targetPosition)
        {
            // TargetType.None은 타일이 없는 곳에서도 배치 가능
            if (targetType == TargetType.None) return true;

            // Phase 2.11: TargetRange를 사용한 거리 제한 검증
            // -1이면 거리 제한 없음, 0+면 해당 거리까지만 가능
            if (targetRange >= 0)
            {
                int distance = CalculateManhattanDistance(casterPosition, targetPosition);
                if (distance > targetRange)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Phase 2.11: 맨하탄 거리 계산 (TargetRange 검증용)
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>맨하탄 거리</returns>
        public static int CalculateManhattanDistance(Vector2Int from, Vector2Int to)
        {
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
            if (effectDataList.Count == 0) return 0; // 효과가 없으면 0

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

     
        public List<ICardEffect> CreateEffectInstances()
        {
            var effects = new List<ICardEffect>();

            if (effectDataList == null || effectDataList.Count == 0)
            {
                Debug.LogWarning($"CardData[{cardName}]: effectDataList가 비어있습니다. 효과가 생성되지 않습니다.");
                return effects;
            }

            foreach (var effectData in effectDataList)
            {
                var effect = CardEffectFactory.CreateEffect(effectData);
                if (effect != null)
                {
                    effects.Add(effect);
                }
                else
                {
                    Debug.LogError($"CardData[{cardName}]: EffectData {effectData}로부터 효과 생성에 실패했습니다.");
                }
            }

            // 우선순위별로 정렬 (낮은 값일수록 먼저 실행)
            effects.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            Debug.Log($"CardData[{cardName}]: {effects.Count}개의 효과 인스턴스가 생성되었습니다.");
            return effects;
        }

        /// <summary>
        /// Phase 2.10: 지정된 위치에서 카드의 모든 효과를 실행합니다
        /// </summary>
        /// <param name="targetPosition">목표 위치</param>
        /// <param name="context">게임 컨텍스트 (서비스 참조)</param>
        /// <returns>성공적으로 실행된 효과 개수</returns>
        public int ExecuteEffects(Vector2Int targetPosition, GameContext context)
        {
            if (context == null || !context.IsValid())
            {
                Debug.LogError($"CardData[{cardName}]: 유효하지 않은 GameContext입니다.");
                return 0;
            }

            var effects = CreateEffectInstances();
            if (effects.Count == 0)
            {
                Debug.LogWarning($"CardData[{cardName}]: 실행할 효과가 없습니다.");
                return 0;
            }

            int executedCount = 0;
            Debug.Log($"CardData[{cardName}]: {targetPosition}에서 {effects.Count}개 효과 실행을 시작합니다.");

            foreach (var effect in effects)
            {
                try
                {
                    if (effect.CanExecute(targetPosition, context))
                    {
                        effect.Execute(targetPosition, context);
                        executedCount++;
                        Debug.Log($"CardData[{cardName}]: {effect.EffectType} 효과가 성공적으로 실행되었습니다.");
                    }
                    else
                    {
                        Debug.LogWarning($"CardData[{cardName}]: {effect.EffectType} 효과 실행 조건을 만족하지 않습니다.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"CardData[{cardName}]: {effect.EffectType} 효과 실행 중 오류 발생: {ex.Message}");
                }
            }

            Debug.Log($"CardData[{cardName}]: 총 {executedCount}/{effects.Count}개 효과가 실행되었습니다.");
            return executedCount;
        }

        /// <summary>
        /// Phase 2.10: 카드의 모든 효과가 지정된 위치에서 실행 가능한지 확인합니다
        /// </summary>
        /// <param name="targetPosition">목표 위치</param>
        /// <param name="context">게임 컨텍스트</param>
        /// <returns>모든 효과가 실행 가능하면 true</returns>
        public bool CanExecuteAllEffects(Vector2Int targetPosition, GameContext context)
        {
            if (context == null || !context.IsValid())
            {
                return false;
            }

            var effects = CreateEffectInstances();
            if (effects.Count == 0)
            {
                return false; // 효과가 없으면 실행 불가
            }

            foreach (var effect in effects)
            {
                if (!effect.CanExecute(targetPosition, context))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Phase 2.10: 실행 가능한 효과들만 필터링하여 반환합니다
        /// </summary>
        /// <param name="targetPosition">목표 위치</param>
        /// <param name="context">게임 컨텍스트</param>
        /// <returns>실행 가능한 효과들의 리스트</returns>
        public List<ICardEffect> GetExecutableEffects(Vector2Int targetPosition, GameContext context)
        {
            var executableEffects = new List<ICardEffect>();

            if (context == null || !context.IsValid())
            {
                return executableEffects;
            }

            var allEffects = CreateEffectInstances();
            foreach (var effect in allEffects)
            {
                if (effect.CanExecute(targetPosition, context))
                {
                    executableEffects.Add(effect);
                }
            }

            return executableEffects;
        }

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

            // 새로운 EffectData 시스템 사용
            if (IsEffectBasedCard)
            {
                int executedEffects = ExecuteEffects(targetPosition, context);
                bool success = executedEffects > 0;

                if (success)
                {
                    Debug.Log($"CardData[{cardName}]: 효과 시스템으로 성공적으로 실행됨 ({executedEffects}개 효과)");
                }
                else
                {
                    Debug.LogWarning($"CardData[{cardName}]: 실행된 효과가 없습니다.");
                }

                return success;
            }
            else
            {
                Debug.LogError($"CardData[{cardName}]: EffectData가 설정되지 않았습니다.");
                return false;
            }
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

            // EffectData 시스템 사용
            if (IsEffectBasedCard)
            {
                return CanExecuteAllEffects(targetPosition, context);
            }

            // 효과가 없는 경우
            return false;
        }


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

            // Phase 3.16: EffectData 시스템 표시
            if (IsEffectBasedCard)
            {
                desc += "<b>효과:</b>\n";
                desc += GenerateEffectDataDescriptions();
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
        /// Phase 3.16: EffectData 시스템 기반 효과 설명 생성
        /// </summary>
        private string GenerateEffectDataDescriptions()
        {
            var effectDesc = "";

            // 효과 타입별로 그룹화하여 표시
            var groupedEffects = effectDataList.GroupBy(e => e.Type);

            foreach (var group in groupedEffects)
            {
                var effectType = group.Key;
                var effects = group.ToList();

                string effectIcon = GetEffectTypeIcon(effectType);
                string effectColor = GetEffectTypeColor(effectType);

                if (effects.Count == 1)
                {
                    var effect = effects[0];
                    effectDesc += $"• <color={effectColor}>{effectIcon} {GenerateSingleEffectDescription(effect)}</color>\n";
                }
                else
                {
                    // 같은 타입의 여러 효과
                    var totalValue = effects.Sum(e => e.Value);
                    effectDesc += $"• <color={effectColor}>{effectIcon} {GetEffectTypeName(effectType)} {totalValue}</color>";

                    // 복합 효과의 세부사항
                    effectDesc += " (";
                    effectDesc += string.Join(" + ", effects.Select(e => e.Value.ToString()));
                    effectDesc += ")\n";
                }
            }

            return effectDesc;
        }

        /// <summary>
        /// Phase 3.16: 개별 효과 설명 생성
        /// </summary>
        private string GenerateSingleEffectDescription(EffectData effect)
        {
            var desc = $"{GetEffectTypeName(effect.Type)} {effect.Value}";

            // 대상 정보 추가
            if (effect.AffectedType != AffectedType.None)
            {
                string targetDesc = effect.AffectedType switch
                {
                    AffectedType.Ally => "아군",
                    AffectedType.Enemy => "적군",
                    AffectedType.Any => "모든 유닛",
                    _ => effect.AffectedType.ToString()
                };
                desc += $" ({targetDesc}";

                // 범위 정보 추가
                if (effect.AffectedRange > 0)
                {
                    desc += $", 범위 {effect.AffectedRange}";
                }
                desc += ")";
            }

            // 소환 효과의 경우 특별 처리
            if (effect.Type == EffectType.Summon && effect.UnitToSummon != null)
            {
                desc = $"{effect.UnitToSummon.UnitName} 소환";
                if (effect.AffectedRange > 0)
                {
                    desc += $" (소환 범위 {effect.AffectedRange})";
                }
            }

            return desc;
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

            // 범위 효과 정보
            var maxRange = GetMaxAffectedRange();
            if (maxRange > 0)
            {
                targetDesc += $"<b>효과 범위:</b> <color=#32CD32>주변 {maxRange}칸</color>\n";
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
                _ => effectType.ToString()
            };
        }
        
    }
