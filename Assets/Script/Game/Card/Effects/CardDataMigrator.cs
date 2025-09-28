using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;
using Game.Card.Effects;

namespace Game.Card.Migration
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.3: 마이그레이션 전략 수립
    /// 기존 카드 데이터를 새로운 효과 기반 시스템으로 변환하는 유틸리티
    /// </summary>
    public static class CardDataMigrator
    {
        /// <summary>
        /// 마이그레이션 결과를 담는 구조체
        /// </summary>
        [Serializable]
        public struct MigrationResult
        {
            public bool Success;
            public string Message;
            public CardData OriginalCard;
            public List<EffectData> ConvertedEffects;
            public CardData.TargetType NewTargetType;
            public int NewTargetRange;
            public AffectedType NewAffectedType;
            public int NewAffectedRange;

            public MigrationResult(bool success, string message)
            {
                Success = success;
                Message = message;
                OriginalCard = null;
                ConvertedEffects = new List<EffectData>();
                NewTargetType = CardData.TargetType.None;
                NewTargetRange = -1;
                NewAffectedType = AffectedType.None;
                NewAffectedRange = 0;
            }
        }

        /// <summary>
        /// 마이그레이션 설정 옵션
        /// </summary>
        [Serializable]
        public class MigrationOptions
        {
            [Header("변환 옵션")]
            public bool PreserveOriginalDescription = true;
            public bool CreateBackup = true;
            public bool ValidateAfterConversion = true;
            public bool LogConversionDetails = true;

            [Header("기본값 설정")]
            public CardData.TargetType DefaultUnitTargetType = CardData.TargetType.Ground;
            public int DefaultUnitTargetRange = -1;
            public CardData.TargetType DefaultSpellTargetType = CardData.TargetType.Enemy;
            public int DefaultSpellTargetRange = 3;

            [Header("호환성 유지")]
            public bool KeepLegacyFields = true;
            public bool AddConversionNotes = true;
        }

        private static readonly MigrationOptions DefaultOptions = new MigrationOptions();

        /// <summary>
        /// 단일 CardData를 새로운 시스템으로 마이그레이션
        /// </summary>
        /// <param name="originalCard">원본 카드 데이터</param>
        /// <param name="options">마이그레이션 옵션</param>
        /// <returns>마이그레이션 결과</returns>
        public static MigrationResult MigrateCardData(CardData originalCard, MigrationOptions options = null)
        {
            if (originalCard == null)
            {
                return new MigrationResult(false, "원본 카드 데이터가 null입니다.");
            }

            options ??= DefaultOptions;
            var result = new MigrationResult(true, "마이그레이션 준비 완료");
            result.OriginalCard = originalCard;

            try
            {
                // 카드 타입에 따른 변환 실행
                switch (originalCard.Type)
                {
                    case CardData.CardType.Unit:
                        result = MigrateUnitCard(originalCard, options);
                        break;
                    case CardData.CardType.Spell:
                        result = MigrateSpellCard(originalCard, options);
                        break;
                    default:
                        result = new MigrationResult(false, $"지원하지 않는 카드 타입: {originalCard.Type}");
                        break;
                }

                // 마이그레이션 후 검증
                if (result.Success && options.ValidateAfterConversion)
                {
                    result = ValidateMigrationResult(result, options);
                }

                // 로그 출력
                if (options.LogConversionDetails)
                {
                    LogMigrationResult(result);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new MigrationResult(false, $"마이그레이션 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Unit 카드를 Summon Effect로 변환
        /// </summary>
        private static MigrationResult MigrateUnitCard(CardData unitCard, MigrationOptions options)
        {
            var result = new MigrationResult(true, "Unit 카드 마이그레이션 시작");
            result.OriginalCard = unitCard;

            // Unit 카드 유효성 검사
            if (!unitCard.CanSummonUnit)
            {
                return new MigrationResult(false, $"Unit 카드 '{unitCard.CardName}'에 소환할 유닛 데이터가 없습니다.");
            }

            // Summon 효과 생성
            var summonEffect = new EffectData(
                EffectType.Summon,
                1, // 1개 유닛 소환
                AffectedType.None, // 소환은 대상에게 영향을 주는 것이 아님
                0 // 소환은 특정 위치에 하는 것
            );

            // 리플렉션을 통해 UnitToSummon 설정 (private 필드 접근)
            var unitToSummonField = typeof(EffectData).GetField("unitToSummon",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            unitToSummonField?.SetValue(summonEffect, unitCard.GetUnitToSummon());

            result.ConvertedEffects = new List<EffectData> { summonEffect };

            // 타겟팅 시스템 변환
            result.NewTargetType = ConvertToPlacementTargetType(unitCard.Target, options.DefaultUnitTargetType);
            result.NewTargetRange = unitCard.Range != 0 ? unitCard.Range : options.DefaultUnitTargetRange;
            result.NewAffectedType = AffectedType.None; // 소환은 영향을 주는 것이 아님
            result.NewAffectedRange = 0;

            result.Message = $"Unit 카드 '{unitCard.CardName}' → Summon Effect 변환 완료";
            return result;
        }

        /// <summary>
        /// Spell 카드를 Damage/Heal Effect로 변환
        /// </summary>
        private static MigrationResult MigrateSpellCard(CardData spellCard, MigrationOptions options)
        {
            var result = new MigrationResult(true, "Spell 카드 마이그레이션 시작");
            result.OriginalCard = spellCard;

            // Spell 카드 유효성 검사
            if (!spellCard.HasValidSpellData)
            {
                return new MigrationResult(false, $"Spell 카드 '{spellCard.CardName}'에 유효한 주문 데이터가 없습니다.");
            }

            // SpellType에 따른 효과 변환
            var effectType = ConvertSpellTypeToEffectType(spellCard.SpellCategory);
            if (effectType == null)
            {
                return new MigrationResult(false, $"지원하지 않는 SpellType: {spellCard.SpellCategory}");
            }

            // 효과 데이터 생성
            var affectedType = ConvertToAffectedType(spellCard.Target, effectType.Value);
            var effect = new EffectData(
                effectType.Value,
                spellCard.SpellEffectValue,
                affectedType,
                spellCard.AreaOfEffect
            );

            result.ConvertedEffects = new List<EffectData> { effect };

            // 타겟팅 시스템 변환
            result.NewTargetType = ConvertToPlacementTargetType(spellCard.Target, options.DefaultSpellTargetType);
            result.NewTargetRange = spellCard.SpellRange > 0 ? (int)spellCard.SpellRange : options.DefaultSpellTargetRange;
            result.NewAffectedType = affectedType;
            result.NewAffectedRange = spellCard.AreaOfEffect;

            result.Message = $"Spell 카드 '{spellCard.CardName}' ({spellCard.SpellCategory}) → {effectType.Value} Effect 변환 완료";
            return result;
        }

        /// <summary>
        /// SpellType을 EffectType으로 변환
        /// </summary>
        private static EffectType? ConvertSpellTypeToEffectType(CardData.SpellType spellType)
        {
            return spellType switch
            {
                CardData.SpellType.Damage => EffectType.Damage,
                CardData.SpellType.Heal => EffectType.Heal,
                CardData.SpellType.Summon => EffectType.Summon,
                // 현재 Phase에서는 Damage, Heal, Summon만 지원
                CardData.SpellType.Buff => null,
                CardData.SpellType.Debuff => null,
                CardData.SpellType.Shield => null,
                CardData.SpellType.Teleport => null,
                _ => null
            };
        }

        /// <summary>
        /// 기존 CardData.TargetType을 배치 가능 위치 검증용 CardData.TargetType으로 변환
        /// </summary>
        private static CardData.TargetType ConvertToPlacementTargetType(CardData.TargetType originalTarget, CardData.TargetType defaultTarget)
        {
            return originalTarget switch
            {
                CardData.TargetType.None => CardData.TargetType.None,
                CardData.TargetType.Ground => CardData.TargetType.Ground,
                CardData.TargetType.Ally => CardData.TargetType.Ally,
                CardData.TargetType.Enemy => CardData.TargetType.Enemy,
                CardData.TargetType.Any => CardData.TargetType.Any,
                // Phase 2.5: obsolete TargetType 값들이 제거되어 더 이상 처리할 수 없음
                // 필요시 string 기반 변환이나 별도 migration 로직 구현 필요
                _ => defaultTarget
            };
        }

        /// <summary>
        /// CardData.TargetType을 효과 적용 대상 AffectedType으로 변환
        /// </summary>
        private static AffectedType ConvertToAffectedType(CardData.TargetType originalTarget, EffectType effectType)
        {
            // 소환 효과는 대상에게 직접 영향을 주지 않음
            if (effectType == EffectType.Summon)
            {
                return AffectedType.None;
            }

            return originalTarget switch
            {
                CardData.TargetType.None => AffectedType.None,
                CardData.TargetType.Ally => AffectedType.Ally,
                CardData.TargetType.Enemy => AffectedType.Enemy,
                CardData.TargetType.Any => AffectedType.Any,
                CardData.TargetType.Ground => AffectedType.None, // 지면 대상은 영향 없음
                // Phase 2.5: obsolete TargetType 값들 제거됨 (Self, AllAllies, AllEnemies, All)
                _ => AffectedType.Enemy // 기본값
            };
        }

        /// <summary>
        /// 마이그레이션 결과 검증
        /// </summary>
        private static MigrationResult ValidateMigrationResult(MigrationResult result, MigrationOptions options)
        {
            if (!result.Success)
            {
                return result;
            }

            var validationErrors = new List<string>();

            // 효과 데이터 검증
            if (result.ConvertedEffects == null || result.ConvertedEffects.Count == 0)
            {
                validationErrors.Add("변환된 효과가 없습니다.");
            }
            else
            {
                for (int i = 0; i < result.ConvertedEffects.Count; i++)
                {
                    var effect = result.ConvertedEffects[i];
                    if (!effect.IsValid())
                    {
                        validationErrors.Add($"효과 {i}: 유효하지 않은 효과 데이터");
                    }
                }
            }

            // 타겟팅 시스템 검증
            if (result.NewTargetRange < -1)
            {
                validationErrors.Add("타겟 범위가 유효하지 않습니다 (최소값: -1).");
            }

            if (result.NewAffectedRange < 0)
            {
                validationErrors.Add("영향 범위가 유효하지 않습니다 (최소값: 0).");
            }

            if (validationErrors.Count > 0)
            {
                result.Success = false;
                result.Message = $"검증 실패: {string.Join(", ", validationErrors)}";
            }
            else
            {
                result.Message += " (검증 통과)";
            }

            return result;
        }

        /// <summary>
        /// 마이그레이션 결과 로그 출력
        /// </summary>
        private static void LogMigrationResult(MigrationResult result)
        {
            if (result.Success)
            {
                Debug.Log($"[CardDataMigrator] 성공: {result.Message}");
                if (result.ConvertedEffects?.Count > 0)
                {
                    Debug.Log($"[CardDataMigrator] 변환된 효과 수: {result.ConvertedEffects.Count}");
                    foreach (var effect in result.ConvertedEffects)
                    {
                        Debug.Log($"[CardDataMigrator] - {effect}");
                    }
                }
                Debug.Log($"[CardDataMigrator] 타겟팅: {result.NewTargetType}(범위:{result.NewTargetRange}) → 영향: {result.NewAffectedType}(범위:{result.NewAffectedRange})");
            }
            else
            {
                Debug.LogError($"[CardDataMigrator] 실패: {result.Message}");
            }
        }

        /// <summary>
        /// 프로젝트 내 모든 CardData를 찾아서 마이그레이션 분석
        /// </summary>
        public static List<MigrationResult> AnalyzeAllCards(MigrationOptions options = null)
        {
            options ??= DefaultOptions;
            var results = new List<MigrationResult>();

            // 프로젝트 내 모든 CardData ScriptableObject 찾기
            var allCards = Resources.LoadAll<CardData>("");
            if (allCards == null || allCards.Length == 0)
            {
                Debug.LogWarning("[CardDataMigrator] 프로젝트에서 CardData를 찾을 수 없습니다.");
                return results;
            }

            Debug.Log($"[CardDataMigrator] {allCards.Length}개의 CardData 발견. 마이그레이션 분석 시작...");

            foreach (var card in allCards)
            {
                var result = MigrateCardData(card, options);
                results.Add(result);
            }

            // 분석 결과 요약
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count - successCount;

            Debug.Log($"[CardDataMigrator] 분석 완료: 성공 {successCount}, 실패 {failureCount}");

            return results;
        }

        /// <summary>
        /// 마이그레이션 보고서 생성
        /// </summary>
        public static string GenerateMigrationReport(List<MigrationResult> results)
        {
            var report = "# CardData 마이그레이션 보고서\n\n";
            report += $"분석 일시: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";

            var successResults = results.Where(r => r.Success).ToList();
            var failureResults = results.Where(r => !r.Success).ToList();

            report += $"## 요약\n";
            report += $"- 총 카드 수: {results.Count}\n";
            report += $"- 마이그레이션 가능: {successResults.Count}\n";
            report += $"- 마이그레이션 불가: {failureResults.Count}\n\n";

            if (successResults.Count > 0)
            {
                report += "## 마이그레이션 가능한 카드\n\n";
                foreach (var result in successResults)
                {
                    report += $"### {result.OriginalCard.CardName}\n";
                    report += $"- 타입: {result.OriginalCard.Type}\n";
                    report += $"- 변환된 효과: {result.ConvertedEffects?.Count ?? 0}개\n";
                    report += $"- 타겟팅: {result.NewTargetType} (범위: {result.NewTargetRange})\n";
                    report += $"- 영향: {result.NewAffectedType} (범위: {result.NewAffectedRange})\n\n";
                }
            }

            if (failureResults.Count > 0)
            {
                report += "## 마이그레이션 불가능한 카드\n\n";
                foreach (var result in failureResults)
                {
                    report += $"### {result.OriginalCard?.CardName ?? "알 수 없음"}\n";
                    report += $"- 오류: {result.Message}\n\n";
                }
            }

            return report;
        }
    }
}