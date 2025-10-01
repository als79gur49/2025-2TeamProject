using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Game.Card.Effects;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Data
{
    /// <summary>
    /// Phase 3.13: CardData 마이그레이션 유틸리티
    /// 기존 카드 데이터를 새로운 EffectData 시스템으로 변환합니다.
    ///
    /// 변환 규칙:
    /// - Unit cards → Summon effects (unitToSummon 필드 기반)
    /// - Spell cards → Damage/Heal effects (SpellType 기반)
    /// - 기존 설정값들을 새로운 시스템으로 매핑
    /// </summary>
    public static class CardDataMigrator
    {
        /// <summary>
        /// 마이그레이션 결과 정보
        /// </summary>
        [Serializable]
        public class MigrationResult
        {
            public int ProcessedCards { get; set; } = 0;
            public int SuccessfulMigrations { get; set; } = 0;
            public int FailedMigrations { get; set; } = 0;
            public int SkippedCards { get; set; } = 0;
            public List<string> ErrorMessages { get; set; } = new List<string>();
            public List<string> WarningMessages { get; set; } = new List<string>();
            public List<string> ProcessedCardNames { get; set; } = new List<string>();
            public DateTime MigrationTimestamp { get; set; } = DateTime.Now;

            public bool IsSuccessful => FailedMigrations == 0 && ProcessedCards > 0;

            public override string ToString()
            {
                return $"Migration Result: {SuccessfulMigrations}/{ProcessedCards} cards migrated successfully" +
                       (FailedMigrations > 0 ? $", {FailedMigrations} failed" : "") +
                       (SkippedCards > 0 ? $", {SkippedCards} skipped" : "");
            }
        }

        /// <summary>
        /// 백업 정보 구조체
        /// </summary>
        [Serializable]
        public class BackupInfo
        {
            public string BackupPath;
            public DateTime BackupTimestamp;
            public int BackupFileCount;
            public bool BackupSuccessful;
        }

        /// <summary>
        /// 레거시 SpellType 열거형 (마이그레이션용)
        /// </summary>
        private enum LegacySpellType
        {
            Damage = 0,
            Heal = 1,
            Buff = 2,
            Debuff = 3,
            Shield = 4,
            Teleport = 5,
            Summon = 6
        }

        /// <summary>
        /// 레거시 TargetType 열거형 (마이그레이션용)
        /// </summary>
        private enum LegacyTargetType
        {
            None = 0,
            Self = 1,
            Ally = 2,
            Enemy = 3,
            Any = 4,
            Ground = 5,
            AllAllies = 6,
            AllEnemies = 7,
            All = 8
        }

        #if UNITY_EDITOR
        /// <summary>
        /// 모든 카드 데이터를 마이그레이션합니다 (에디터 전용)
        /// </summary>
        /// <param name="createBackup">백업 생성 여부</param>
        /// <param name="dryRun">테스트 실행 (실제 변경 없음)</param>
        /// <returns>마이그레이션 결과</returns>
        public static MigrationResult MigrateAllCards(bool createBackup = true, bool dryRun = false)
        {
            var result = new MigrationResult();

            try
            {
                Debug.Log($"CardDataMigrator: 마이그레이션 시작 (백업: {createBackup}, 테스트: {dryRun})");

                // 1. 모든 CardData 에셋 찾기
                var cardGuids = AssetDatabase.FindAssets("t:CardData");
                var cardPaths = cardGuids.Select(AssetDatabase.GUIDToAssetPath).ToList();

                Debug.Log($"CardDataMigrator: {cardPaths.Count}개의 카드 데이터 발견");

                if (cardPaths.Count == 0)
                {
                    result.WarningMessages.Add("마이그레이션할 카드 데이터가 없습니다.");
                    return result;
                }

                // 2. 백업 생성
                BackupInfo backupInfo = null;
                if (createBackup && !dryRun)
                {
                    backupInfo = CreateBackup(cardPaths);
                    if (!backupInfo.BackupSuccessful)
                    {
                        result.ErrorMessages.Add("백업 생성에 실패했습니다. 마이그레이션을 중단합니다.");
                        return result;
                    }
                    Debug.Log($"CardDataMigrator: 백업 생성 완료 ({backupInfo.BackupPath})");
                }

                // 3. 각 카드 데이터 마이그레이션
                result.ProcessedCards = cardPaths.Count;

                foreach (var path in cardPaths)
                {
                    try
                    {
                        var cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);
                        if (cardData == null)
                        {
                            result.ErrorMessages.Add($"카드 로드 실패: {path}");
                            result.FailedMigrations++;
                            continue;
                        }

                        var cardResult = MigrateCard(cardData, dryRun);
                        result.ProcessedCardNames.Add(cardData.CardName);

                        if (cardResult.success)
                        {
                            result.SuccessfulMigrations++;
                            if (cardResult.wasAlreadyMigrated)
                            {
                                result.SkippedCards++;
                                result.WarningMessages.Add($"카드가 이미 마이그레이션됨: {cardData.CardName}");
                            }
                        }
                        else
                        {
                            result.FailedMigrations++;
                            result.ErrorMessages.Add($"카드 마이그레이션 실패: {cardData.CardName} - {cardResult.errorMessage}");
                        }

                        if (!string.IsNullOrEmpty(cardResult.warningMessage))
                        {
                            result.WarningMessages.Add($"{cardData.CardName}: {cardResult.warningMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorMessages.Add($"카드 처리 중 예외 발생: {path} - {ex.Message}");
                        result.FailedMigrations++;
                    }
                }

                // 4. 변경사항 저장
                if (!dryRun && result.SuccessfulMigrations > 0)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log("CardDataMigrator: 변경사항 저장 완료");
                }

                // 5. 결과 로깅
                Debug.Log($"CardDataMigrator: 마이그레이션 완료 - {result}");

                if (result.ErrorMessages.Count > 0)
                {
                    Debug.LogWarning($"CardDataMigrator: {result.ErrorMessages.Count}개의 오류 발생");
                    foreach (var error in result.ErrorMessages)
                    {
                        Debug.LogError($"  - {error}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"마이그레이션 중 치명적 오류 발생: {ex.Message}");
                Debug.LogError($"CardDataMigrator: 치명적 오류 - {ex.Message}\n{ex.StackTrace}");
                return result;
            }
        }

        /// <summary>
        /// 단일 카드 데이터를 마이그레이션합니다
        /// </summary>
        /// <param name="cardData">마이그레이션할 카드 데이터</param>
        /// <param name="dryRun">테스트 실행 여부</param>
        /// <returns>마이그레이션 결과</returns>
        public static (bool success, bool wasAlreadyMigrated, string errorMessage, string warningMessage) MigrateCard(CardData cardData, bool dryRun = false)
        {
            if (cardData == null)
            {
                return (false, false, "CardData가 null입니다.", "");
            }

            try
            {
                Debug.Log($"CardDataMigrator: 카드 마이그레이션 시작 - {cardData.CardName}");

                // 1. 이미 마이그레이션된 카드인지 확인
                if (cardData.IsEffectBasedCard)
                {
                    Debug.Log($"CardDataMigrator: 카드가 이미 마이그레이션됨 - {cardData.CardName}");
                    return (true, true, "", "이미 새로운 EffectData 시스템을 사용 중입니다.");
                }

                // 2. 효과 타입 추론 및 마이그레이션
                var newEffects = new List<EffectData>();
                string warningMessage = "";

                // 레거시 필드에서 효과 타입 추론
                var inferredResult = InferEffectsFromLegacyData(cardData);
                newEffects = inferredResult.effects;
                warningMessage = inferredResult.warning;

                if (newEffects.Count == 0)
                {
                    return (false, false, "변환할 효과가 없습니다.", warningMessage);
                }

                // 3. 실제 마이그레이션 수행
                if (!dryRun)
                {
                    ApplyMigration(cardData, newEffects);
                    EditorUtility.SetDirty(cardData);
                    Debug.Log($"CardDataMigrator: 카드 마이그레이션 완료 - {cardData.CardName} ({newEffects.Count}개 효과)");
                }
                else
                {
                    Debug.Log($"CardDataMigrator: 드라이런 - {cardData.CardName} ({newEffects.Count}개 효과 변환됨)");
                }

                return (true, false, "", warningMessage);
            }
            catch (Exception ex)
            {
                return (false, false, $"마이그레이션 중 오류 발생: {ex.Message}", "");
            }
        }

        /// <summary>
        /// 레거시 데이터에서 효과 타입 추론
        /// </summary>
        private static (List<EffectData> effects, string warning) InferEffectsFromLegacyData(CardData cardData)
        {
            var effects = new List<EffectData>();
            string warning = "";

            // 레거시 unitToSummon 필드가 있는지 확인 (유닛 카드)
            var unitToSummon = GetLegacyFieldValue<UnitData>(cardData, "unitToSummon");
            if (unitToSummon != null)
            {
                var unitResult = MigrateAsUnitCard(cardData, unitToSummon);
                effects.AddRange(unitResult.effects);
                warning = unitResult.warning;
                return (effects, warning);
            }

            // 레거시 spellType 필드가 있는지 확인 (주문 카드)
            var spellType = GetLegacyFieldValue<int>(cardData, "spellType", -1);
            if (spellType >= 0)
            {
                var spellResult = MigrateAsSpellCard(cardData, spellType);
                effects.AddRange(spellResult.effects);
                warning = spellResult.warning;
                return (effects, warning);
            }

            // 추론 실패 - 기본 데미지 효과로 생성
            warning = "카드 타입을 추론할 수 없습니다. 기본 데미지 효과로 생성됩니다.";
            effects.Add(new EffectData(EffectType.Damage, 1, AffectedType.Enemy, 0));
            Debug.LogWarning($"CardDataMigrator: {cardData.CardName} - {warning}");

            return (effects, warning);
        }

        /// <summary>
        /// 유닛 카드로 마이그레이션
        /// </summary>
        private static (List<EffectData> effects, string warning) MigrateAsUnitCard(CardData cardData, UnitData unitToSummon)
        {
            var effects = new List<EffectData>();
            string warning = "";

            if (unitToSummon != null)
            {
                // UnitData가 있는 경우 정상적인 소환 효과 생성
                var summonEffect = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);

                // UnitToSummon 필드 설정 (reflection 사용)
                SetPrivateField(summonEffect, "unitToSummon", unitToSummon);

                effects.Add(summonEffect);
                Debug.Log($"CardDataMigrator: 유닛 카드 변환 완료 - {cardData.CardName} → {unitToSummon.name} 소환");
            }
            else
            {
                // UnitToSummon이 없는 경우 기본 소환 효과 생성
                var summonEffect = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);
                effects.Add(summonEffect);
                warning = "unitToSummon 필드가 설정되지 않았습니다. 기본 소환 효과로 변환됩니다.";
                Debug.LogWarning($"CardDataMigrator: {cardData.CardName} - {warning}");
            }

            return (effects, warning);
        }

        /// <summary>
        /// 주문 카드로 마이그레이션
        /// </summary>
        private static (List<EffectData> effects, string warning) MigrateAsSpellCard(CardData cardData, int spellType)
        {
            var effects = new List<EffectData>();
            string warning = "";

            // 레거시 필드 읽기
            var spellEffectValue = GetLegacyFieldValue<int>(cardData, "spellEffectValue", 1);
            var spellRange = GetLegacyFieldValue<int>(cardData, "spellRange", 0);

            // 레거시 targetType 변환
            var legacyTargetType = (LegacyTargetType)GetLegacyFieldValue<int>(cardData, "targetType", 3); // 기본값: Enemy
            var affectedType = ConvertLegacyTargetTypeToAffectedType(legacyTargetType);

            var legacySpellType = (LegacySpellType)spellType;

            switch (legacySpellType)
            {
                case LegacySpellType.Damage:
                    var damageEffect = new EffectData(EffectType.Damage, spellEffectValue, affectedType, spellRange);
                    effects.Add(damageEffect);
                    Debug.Log($"CardDataMigrator: 주문 카드 변환 - {cardData.CardName} → {spellEffectValue} 데미지");
                    break;

                case LegacySpellType.Heal:
                    var healEffect = new EffectData(EffectType.Heal, spellEffectValue, affectedType, spellRange);
                    effects.Add(healEffect);
                    Debug.Log($"CardDataMigrator: 주문 카드 변환 - {cardData.CardName} → {spellEffectValue} 회복");
                    break;

                case LegacySpellType.Summon:
                    // 주문 타입의 소환 효과를 Summon으로 변환
                    var summonEffect = new EffectData(EffectType.Summon, spellEffectValue, AffectedType.None, 0);
                    effects.Add(summonEffect);
                    Debug.Log($"CardDataMigrator: 주문 카드 변환 - {cardData.CardName} → 소환 효과");
                    break;

                case LegacySpellType.Buff:
                case LegacySpellType.Debuff:
                case LegacySpellType.Shield:
                case LegacySpellType.Teleport:
                    // 현재 새로운 시스템에서 지원하지 않는 효과들
                    warning = $"지원되지 않는 주문 타입입니다 ({legacySpellType}). Damage 효과로 대체됩니다.";
                    var fallbackEffect = new EffectData(EffectType.Damage, spellEffectValue, affectedType, spellRange);
                    effects.Add(fallbackEffect);
                    Debug.LogWarning($"CardDataMigrator: {cardData.CardName} - {warning}");
                    break;

                default:
                    warning = $"알 수 없는 주문 타입입니다 ({spellType}). 기본 데미지 효과로 생성됩니다.";
                    var defaultEffect = new EffectData(EffectType.Damage, 1, AffectedType.Enemy, 0);
                    effects.Add(defaultEffect);
                    Debug.LogWarning($"CardDataMigrator: {cardData.CardName} - {warning}");
                    break;
            }

            return (effects, warning);
        }

        /// <summary>
        /// 레거시 TargetType을 새로운 AffectedType으로 변환
        /// </summary>
        private static AffectedType ConvertLegacyTargetTypeToAffectedType(LegacyTargetType legacyTargetType)
        {
            return legacyTargetType switch
            {
                LegacyTargetType.None => AffectedType.None,
                LegacyTargetType.Self => AffectedType.Ally,
                LegacyTargetType.Ally => AffectedType.Ally,
                LegacyTargetType.Enemy => AffectedType.Enemy,
                LegacyTargetType.Any => AffectedType.Any,
                LegacyTargetType.Ground => AffectedType.None,
                LegacyTargetType.AllAllies => AffectedType.Ally,
                LegacyTargetType.AllEnemies => AffectedType.Enemy,
                LegacyTargetType.All => AffectedType.Any,
                _ => AffectedType.Enemy // 기본값
            };
        }

        /// <summary>
        /// 마이그레이션 결과를 CardData에 적용
        /// </summary>
        private static void ApplyMigration(CardData cardData, List<EffectData> newEffects)
        {
            // effectDataList 필드에 새로운 효과들 설정
            SetPrivateField(cardData, "effectDataList", newEffects);

            // 기존 effects 리스트는 유지 (호환성을 위해)
            // 필요에 따라 나중에 정리할 수 있음
        }

        /// <summary>
        /// Reflection을 사용하여 private 필드 값 읽기
        /// </summary>
        private static T GetLegacyFieldValue<T>(object obj, string fieldName, T defaultValue = default(T))
        {
            try
            {
                var field = obj.GetType().GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    var value = field.GetValue(obj);
                    if (value is T result)
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CardDataMigrator: 필드 읽기 실패 ({fieldName}): {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// Reflection을 사용하여 private 필드 값 설정
        /// </summary>
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            try
            {
                var field = obj.GetType().GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    field.SetValue(obj, value);
                }
                else
                {
                    Debug.LogWarning($"CardDataMigrator: 필드를 찾을 수 없음 ({fieldName})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CardDataMigrator: 필드 설정 실패 ({fieldName}): {ex.Message}");
            }
        }

        /// <summary>
        /// 카드 데이터 백업 생성
        /// </summary>
        private static BackupInfo CreateBackup(List<string> cardPaths)
        {
            var backupInfo = new BackupInfo
            {
                BackupTimestamp = DateTime.Now,
                BackupSuccessful = false
            };

            try
            {
                var timestamp = backupInfo.BackupTimestamp.ToString("yyyyMMdd_HHmmss");
                backupInfo.BackupPath = Path.Combine(Application.dataPath, "..", "backup_carddata_migration_" + timestamp);

                // 백업 디렉토리 생성
                Directory.CreateDirectory(backupInfo.BackupPath);

                // 각 카드 데이터 파일 백업
                int copiedFiles = 0;
                foreach (var cardPath in cardPaths)
                {
                    try
                    {
                        var fileName = Path.GetFileName(cardPath);
                        var backupFilePath = Path.Combine(backupInfo.BackupPath, fileName);
                        File.Copy(cardPath, backupFilePath, true);
                        copiedFiles++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"CardDataMigrator: 파일 백업 실패 ({cardPath}): {ex.Message}");
                    }
                }

                backupInfo.BackupFileCount = copiedFiles;
                backupInfo.BackupSuccessful = copiedFiles > 0;

                // 백업 정보 파일 생성
                var infoPath = Path.Combine(backupInfo.BackupPath, "backup_info.json");
                var infoJson = JsonUtility.ToJson(backupInfo, true);
                File.WriteAllText(infoPath, infoJson);

                Debug.Log($"CardDataMigrator: 백업 생성 완료 - {copiedFiles}개 파일, 경로: {backupInfo.BackupPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"CardDataMigrator: 백업 생성 실패: {ex.Message}");
                backupInfo.BackupSuccessful = false;
            }

            return backupInfo;
        }

        /// <summary>
        /// 마이그레이션 결과를 검증합니다
        /// </summary>
        public static bool ValidateMigration(CardData cardData)
        {
            if (cardData == null)
            {
                return false;
            }

            // 새로운 시스템으로 마이그레이션되었는지 확인
            if (!cardData.IsEffectBasedCard)
            {
                Debug.LogError($"CardDataMigrator: 마이그레이션 검증 실패 - {cardData.CardName}: EffectData가 없습니다.");
                return false;
            }

            // 각 EffectData 유효성 검사
            foreach (var effectData in cardData.EffectDataList)
            {
                if (!effectData.IsValid())
                {
                    Debug.LogError($"CardDataMigrator: 마이그레이션 검증 실패 - {cardData.CardName}: 유효하지 않은 EffectData ({effectData})");
                    return false;
                }
            }

            // CardData 전체 유효성 검사
            if (!cardData.IsValid())
            {
                Debug.LogError($"CardDataMigrator: 마이그레이션 검증 실패 - {cardData.CardName}: CardData 유효성 검사 실패");
                return false;
            }

            Debug.Log($"CardDataMigrator: 마이그레이션 검증 성공 - {cardData.CardName}");
            return true;
        }

        #endif

        /// <summary>
        /// 런타임에서 단일 카드를 마이그레이션합니다 (제한적 기능)
        /// </summary>
        public static bool MigrateCardRuntime(CardData cardData)
        {
            if (cardData == null || cardData.IsEffectBasedCard)
            {
                return false;
            }

            try
            {
                Debug.LogWarning($"CardDataMigrator: 런타임 마이그레이션은 제한적입니다 - {cardData.CardName}");

                // 런타임에서는 레거시 필드 추론 시도
                var newEffects = new List<EffectData>();

                // unitToSummon 필드 확인
                var unitToSummon = GetLegacyFieldValue<UnitData>(cardData, "unitToSummon");
                if (unitToSummon != null)
                {
                    newEffects.Add(new EffectData(EffectType.Summon, 1, AffectedType.None, 0));
                }
                else
                {
                    // spellType 필드 확인
                    var spellType = GetLegacyFieldValue<int>(cardData, "spellType", -1);
                    if (spellType >= 0)
                    {
                        // 기본 효과 생성 (상세 변환은 에디터에서만 가능)
                        newEffects.Add(new EffectData(EffectType.Damage, 1, AffectedType.Enemy, 0));
                    }
                    else
                    {
                        // 추론 실패 - 기본 데미지 효과
                        newEffects.Add(new EffectData(EffectType.Damage, 1, AffectedType.Enemy, 0));
                    }
                }

                if (newEffects.Count > 0)
                {
                    SetPrivateField(cardData, "effectDataList", newEffects);
                    Debug.Log($"CardDataMigrator: 런타임 마이그레이션 완료 - {cardData.CardName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CardDataMigrator: 런타임 마이그레이션 실패 - {cardData.CardName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 마이그레이션 상태 출력 (디버깅용)
        /// </summary>
        public static void PrintMigrationStatus(CardData cardData)
        {
            if (cardData == null)
            {
                Debug.Log("CardDataMigrator: CardData가 null입니다.");
                return;
            }

            Debug.Log($"=== {cardData.CardName} 마이그레이션 상태 ===");
            Debug.Log($"새로운 시스템 사용: {cardData.IsEffectBasedCard}");
            Debug.Log($"EffectData 개수: {cardData.EffectDataList.Count}");

            if (cardData.IsEffectBasedCard)
            {
                foreach (var effect in cardData.EffectDataList)
                {
                    Debug.Log($"  - {effect}");
                }
            }
            else
            {
                Debug.Log("  레거시 시스템을 사용하고 있습니다.");
            }

            Debug.Log($"유효성: {cardData.IsValid()}");
            Debug.Log("=================================");
        }
    }
}