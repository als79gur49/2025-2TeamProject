using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Game.Data;
using Game.Card.Core;

namespace Game.Tools
{
    /// <summary>
    /// Phase 3: 데이터 마이그레이션 도구 클래스
    /// 기존 Spell 객체에서 CardData로 데이터를 이전하고 ScriptableObject 에셋을 업데이트합니다.
    /// </summary>
    public static class Phase3DataMigration
    {
        // 마이그레이션될 샘플 주문 데이터
        private static readonly SpellMigrationData[] sampleSpellData = new SpellMigrationData[]
        {
            new SpellMigrationData("Fire Bolt", "적에게 화염 피해를 입힙니다", 2, 1, SpellType.Damage, 50, 3f, 0f),
            new SpellMigrationData("Healing Light", "아군을 치유합니다", 1, 1, SpellType.Heal, 30, 2f, 0f),
            new SpellMigrationData("Lightning Strike", "강력한 번개 공격", 4, 2, SpellType.Damage, 100, 5f, 1f),
            new SpellMigrationData("Shield of Protection", "보호막을 생성합니다", 2, 1, SpellType.Shield, 40, 0f, 0f),
            new SpellMigrationData("Teleport", "단거리 순간이동", 3, 1, SpellType.Teleport, 1, 4f, 0f),
            new SpellMigrationData("Blessing of Strength", "아군을 강화합니다", 3, 2, SpellType.Buff, 25, 2f, 0f),
            new SpellMigrationData("Curse of Weakness", "적군을 약화시킵니다", 2, 1, SpellType.Debuff, 20, 3f, 0f),
            new SpellMigrationData("Summon Minion", "소환수를 불러냅니다", 5, 3, SpellType.Summon, 2, 0f, 2f)
        };

        /// <summary>
        /// 마이그레이션 데이터 구조체
        /// </summary>
        private struct SpellMigrationData
        {
            public string name;
            public string description;
            public int manaCost;
            public int actionCost;
            public SpellType spellType;
            public int effectValue;
            public float range;
            public float cooldown;

            public SpellMigrationData(string name, string desc, int mana, int action, SpellType type, int value, float range, float cooldown)
            {
                this.name = name;
                this.description = desc;
                this.manaCost = mana;
                this.actionCost = action;
                this.spellType = type;
                this.effectValue = value;
                this.range = range;
                this.cooldown = cooldown;
            }
        }

        /// <summary>
        /// Phase 3 마이그레이션 실행 - 주문 카드 데이터 생성 및 저장
        /// </summary>
        /// <param name="outputPath">저장할 폴더 경로 (Assets 폴더 기준)</param>
        /// <returns>마이그레이션 성공 여부</returns>
        public static bool ExecutePhase3Migration(string outputPath = "Assets/Data/Cards/Spells")
        {
            Debug.Log("🔄 Phase 3 Data Migration: Starting spell data migration...");

            try
            {
                // 1. 출력 폴더 생성
                if (!CreateOutputDirectory(outputPath))
                {
                    Debug.LogError("❌ Failed to create output directory");
                    return false;
                }

                // 2. 마이그레이션 결과 추적
                var migrationResults = new List<MigrationResult>();

                // 3. 각 샘플 주문 데이터를 CardData로 마이그레이션
                foreach (var spellData in sampleSpellData)
                {
                    var result = MigrateSpellToCardData(spellData, outputPath);
                    migrationResults.Add(result);
                }

                // 4. 마이그레이션 결과 출력
                ReportMigrationResults(migrationResults);

                // 5. 에셋 데이터베이스 새로고침
                #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log("✅ Asset Database refreshed");
                #endif

                Debug.Log($"✅ Phase 3 Migration completed successfully. {migrationResults.Count} spell cards created.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Phase 3 Migration failed with exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 출력 디렉토리 생성
        /// </summary>
        private static bool CreateOutputDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Debug.Log($"📁 Created output directory: {path}");
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Failed to create directory {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 개별 주문 데이터를 CardData로 마이그레이션
        /// </summary>
        private static MigrationResult MigrateSpellToCardData(SpellMigrationData spellData, string outputPath)
        {
            var result = new MigrationResult
            {
                originalName = spellData.name,
                success = false,
                errorMessage = ""
            };

            try
            {
                // 1. CardData 인스턴스 생성
                CardData cardData = ScriptableObject.CreateInstance<CardData>();

                // 2. 기본 카드 정보 설정
                SetBasicCardProperties(cardData, spellData);

                // 3. 주문 전용 속성 설정
                SetSpellSpecificProperties(cardData, spellData);

                // 4. 파일명 생성 및 저장
                string fileName = GenerateFileName(spellData.name);
                string fullPath = Path.Combine(outputPath, $"{fileName}.asset");
                
                #if UNITY_EDITOR
                // Unity 에디터에서만 에셋 생성
                UnityEditor.AssetDatabase.CreateAsset(cardData, fullPath);
                result.assetPath = fullPath;
                #else
                // 런타임에서는 메모리에만 보관
                result.assetPath = "Runtime Asset (not saved to disk)";
                #endif

                result.success = true;
                Debug.Log($"✅ Migrated spell: {spellData.name} → {fileName}.asset");

                return result;
            }
            catch (System.Exception ex)
            {
                result.errorMessage = ex.Message;
                Debug.LogError($"❌ Failed to migrate spell {spellData.name}: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// CardData의 기본 속성 설정
        /// </summary>
        private static void SetBasicCardProperties(CardData cardData, SpellMigrationData spellData)
        {
            // Private 필드는 리플렉션을 통해 설정 (Unity Inspector 전용)
            var cardDataType = typeof(CardData);
            
            // 기본 정보 설정
            SetPrivateField(cardData, "cardName", spellData.name);
            SetPrivateField(cardData, "description", spellData.description);
            SetPrivateField(cardData, "cardType", CardData.CardType.Spell);
            SetPrivateField(cardData, "rarity", CardData.CardRarity.Common);
            SetPrivateField(cardData, "manaCost", spellData.manaCost);
            SetPrivateField(cardData, "actionCost", spellData.actionCost);
            
            // 대상 설정 (주문 타입에 따라 기본값)
            var targetType = GetDefaultTargetType(spellData.spellType);
            SetPrivateField(cardData, "targetType", targetType);
            SetPrivateField(cardData, "range", Mathf.RoundToInt(spellData.range));
        }

        /// <summary>
        /// 주문 전용 속성 설정
        /// </summary>
        private static void SetSpellSpecificProperties(CardData cardData, SpellMigrationData spellData)
        {
            SetPrivateField(cardData, "spellType", spellData.spellType);
            SetPrivateField(cardData, "spellEffectValue", spellData.effectValue);
            SetPrivateField(cardData, "spellRange", spellData.range);
            SetPrivateField(cardData, "spellCooldown", spellData.cooldown);
            
            Debug.Log($"🎯 Set spell properties: {spellData.spellType}, value: {spellData.effectValue}, range: {spellData.range}");
        }

        /// <summary>
        /// 리플렉션을 통한 private 필드 설정
        /// </summary>
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var fieldInfo = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"⚠️ Field '{fieldName}' not found in CardData");
            }
        }

        /// <summary>
        /// 주문 타입에 따른 기본 대상 타입 결정
        /// </summary>
        private static CardData.TargetType GetDefaultTargetType(SpellType spellType)
        {
            return spellType switch
            {
                SpellType.Damage or SpellType.Debuff => CardData.TargetType.Enemy,
                SpellType.Heal or SpellType.Buff or SpellType.Shield => CardData.TargetType.Ally,
                SpellType.Teleport => CardData.TargetType.Any,
                SpellType.Summon => CardData.TargetType.Ground,
                _ => CardData.TargetType.Any
            };
        }

        /// <summary>
        /// 파일명 생성 (특수문자 제거 및 공백을 언더스코어로 변경)
        /// </summary>
        private static string GenerateFileName(string cardName)
        {
            string fileName = cardName.Replace(" ", "_")
                                    .Replace("'", "")
                                    .Replace("\"", "")
                                    .Replace("/", "_")
                                    .Replace("\\", "_")
                                    .Replace(":", "_")
                                    .Replace("?", "")
                                    .Replace("*", "")
                                    .Replace("<", "")
                                    .Replace(">", "")
                                    .Replace("|", "_");
            
            return fileName;
        }

        /// <summary>
        /// 마이그레이션 결과 보고
        /// </summary>
        private static void ReportMigrationResults(List<MigrationResult> results)
        {
            int successCount = 0;
            int failureCount = 0;

            Debug.Log("📊 Phase 3 Migration Results:");
            Debug.Log("=====================================");

            foreach (var result in results)
            {
                if (result.success)
                {
                    successCount++;
                    Debug.Log($"✅ {result.originalName} → {result.assetPath}");
                }
                else
                {
                    failureCount++;
                    Debug.LogError($"❌ {result.originalName} → Failed: {result.errorMessage}");
                }
            }

            Debug.Log("=====================================");
            Debug.Log($"📈 Migration Summary: {successCount} successful, {failureCount} failed");
            
            if (successCount > 0)
            {
                Debug.Log($"🎉 Successfully created {successCount} spell card assets!");
            }
        }

        /// <summary>
        /// 마이그레이션된 CardData의 유효성 검사
        /// </summary>
        public static bool ValidateMigratedCardData(CardData cardData)
        {
            if (cardData == null)
            {
                Debug.LogError("❌ CardData is null");
                return false;
            }

            // 기본 검증
            if (!cardData.IsValid())
            {
                Debug.LogError($"❌ CardData {cardData.CardName} failed basic validation");
                return false;
            }

            // 주문 카드 전용 검증
            if (cardData.IsSpellCard && !cardData.HasValidSpellData)
            {
                Debug.LogError($"❌ Spell card {cardData.CardName} has invalid spell data");
                return false;
            }

            // 주문 속성 확인
            if (cardData.IsSpellCard)
            {
                Debug.Log($"✅ Validated spell card: {cardData.CardName}");
                Debug.Log($"   - Type: {cardData.SpellType}");
                Debug.Log($"   - Effect Value: {cardData.SpellEffectValue}");
                Debug.Log($"   - Range: {cardData.SpellRange}");
                Debug.Log($"   - Cooldown: {cardData.SpellCooldown}");
            }

            return true;
        }

        /// <summary>
        /// 마이그레이션 결과 추적용 구조체
        /// </summary>
        private struct MigrationResult
        {
            public string originalName;
            public bool success;
            public string errorMessage;
            public string assetPath;
        }

        #region Unity Editor 전용 메서드

        #if UNITY_EDITOR
        /// <summary>
        /// Unity 에디터에서 Phase 3 마이그레이션 실행 (메뉴 항목)
        /// </summary>
        [UnityEditor.MenuItem("Game Tools/Phase 3: Execute Data Migration")]
        public static void ExecuteMigrationFromMenu()
        {
            bool success = ExecutePhase3Migration();
            
            if (success)
            {
                UnityEditor.EditorUtility.DisplayDialog(
                    "Phase 3 Migration", 
                    "주문 카드 데이터 마이그레이션이 성공적으로 완료되었습니다!", 
                    "OK");
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog(
                    "Phase 3 Migration", 
                    "마이그레이션 중 오류가 발생했습니다. 콘솔을 확인해주세요.", 
                    "OK");
            }
        }

        /// <summary>
        /// 생성된 카드 데이터 검증 (메뉴 항목)
        /// </summary>
        [UnityEditor.MenuItem("Game Tools/Phase 3: Validate Migrated Cards")]
        public static void ValidateAllCardsFromMenu()
        {
            string[] cardAssets = UnityEditor.AssetDatabase.FindAssets("t:CardData");
            int validCount = 0;
            int totalCount = cardAssets.Length;

            foreach (string assetGUID in cardAssets)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGUID);
                CardData cardData = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
                
                if (ValidateMigratedCardData(cardData))
                {
                    validCount++;
                }
            }

            string message = $"검증 완료: {validCount}/{totalCount} 카드가 유효합니다.";
            Debug.Log($"📊 Validation Results: {message}");
            
            UnityEditor.EditorUtility.DisplayDialog(
                "Phase 3 Validation", 
                message, 
                "OK");
        }
        #endif

        #endregion
    }
}