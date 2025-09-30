using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Game.Data;
using Game.Card.Effects;
using System.Linq;

namespace Game.Editor
{
    /// <summary>
    /// Phase 3.13: CardDataMigrator 테스트 클래스
    /// 마이그레이션 기능을 검증하기 위한 단위 테스트를 제공합니다.
    /// </summary>
    public static class CardDataMigratorTests
    {
        [MenuItem("Tools/Card System/Test Migration")]
        public static void RunTests()
        {
            Debug.Log("=== CardDataMigrator 테스트 시작 ===");

            bool allTestsPassed = true;

            // 테스트 실행
            allTestsPassed &= TestUnitCardMigration();
            allTestsPassed &= TestSpellCardMigration();
            allTestsPassed &= TestAlreadyMigratedCard();
            allTestsPassed &= TestValidation();

            // 결과 출력
            if (allTestsPassed)
            {
                Debug.Log("✅ 모든 테스트 통과!");
                EditorUtility.DisplayDialog("테스트 완료", "모든 테스트가 성공적으로 통과했습니다!", "확인");
            }
            else
            {
                Debug.LogError("❌ 일부 테스트 실패");
                EditorUtility.DisplayDialog("테스트 실패", "일부 테스트가 실패했습니다. 콘솔을 확인해주세요.", "확인");
            }

            Debug.Log("=== CardDataMigrator 테스트 완료 ===");
        }

        private static bool TestUnitCardMigration()
        {
            Debug.Log("테스트 1: 유닛 카드 마이그레이션");

            try
            {
                // 테스트 유닛 카드 생성 (레거시 구조 시뮬레이션)
                var unitCard = ScriptableObject.CreateInstance<CardData>();
                SetPrivateField(unitCard, "cardName", "테스트 나이트");
                SetPrivateField(unitCard, "cardType", CardData.CardType.Unit);
                SetPrivateField(unitCard, "manaCost", 3);
                SetPrivateField(unitCard, "effectDataList", new List<EffectData>()); // 빈 리스트로 레거시 상태 시뮬레이션

                // 마이그레이션 테스트 (드라이런)
                var result = CardDataMigrator.MigrateCard(unitCard, true);

                if (!result.success)
                {
                    Debug.LogError($"유닛 카드 마이그레이션 실패: {result.errorMessage}");
                    return false;
                }

                if (result.wasAlreadyMigrated)
                {
                    Debug.LogError("유닛 카드가 이미 마이그레이션된 것으로 인식됨");
                    return false;
                }

                Debug.Log("✅ 유닛 카드 마이그레이션 테스트 통과");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"유닛 카드 마이그레이션 테스트 예외: {ex.Message}");
                return false;
            }
        }

        private static bool TestSpellCardMigration()
        {
            Debug.Log("테스트 2: 주문 카드 마이그레이션");

            try
            {
                // 테스트 주문 카드 생성 (레거시 구조 시뮬레이션)
                var spellCard = ScriptableObject.CreateInstance<CardData>();
                SetPrivateField(spellCard, "cardName", "테스트 화염구");
                SetPrivateField(spellCard, "cardType", CardData.CardType.Spell);
                SetPrivateField(spellCard, "manaCost", 2);
                SetPrivateField(spellCard, "effectDataList", new List<EffectData>()); // 빈 리스트로 레거시 상태 시뮬레이션

                // 레거시 spellType 필드 시뮬레이션 (0 = Damage)
                SetPrivateField(spellCard, "spellType", 0);
                SetPrivateField(spellCard, "spellEffectValue", 3);
                SetPrivateField(spellCard, "spellRange", 1);

                // 마이그레이션 테스트 (드라이런)
                var result = CardDataMigrator.MigrateCard(spellCard, true);

                if (!result.success)
                {
                    Debug.LogError($"주문 카드 마이그레이션 실패: {result.errorMessage}");
                    return false;
                }

                if (result.wasAlreadyMigrated)
                {
                    Debug.LogError("주문 카드가 이미 마이그레이션된 것으로 인식됨");
                    return false;
                }

                Debug.Log("✅ 주문 카드 마이그레이션 테스트 통과");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"주문 카드 마이그레이션 테스트 예외: {ex.Message}");
                return false;
            }
        }

        private static bool TestAlreadyMigratedCard()
        {
            Debug.Log("테스트 3: 이미 마이그레이션된 카드");

            try
            {
                // 이미 마이그레이션된 카드 생성
                var migratedCard = CardData.CreateDamageCard("테스트 마이그레이션됨", "이미 마이그레이션된 카드", 1, 2);

                // 마이그레이션 테스트
                var result = CardDataMigrator.MigrateCard(migratedCard, true);

                if (!result.success)
                {
                    Debug.LogError($"이미 마이그레이션된 카드 처리 실패: {result.errorMessage}");
                    return false;
                }

                if (!result.wasAlreadyMigrated)
                {
                    Debug.LogError("이미 마이그레이션된 카드가 새로 마이그레이션되려고 함");
                    return false;
                }

                Debug.Log("✅ 이미 마이그레이션된 카드 테스트 통과");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"이미 마이그레이션된 카드 테스트 예외: {ex.Message}");
                return false;
            }
        }

        private static bool TestValidation()
        {
            Debug.Log("테스트 4: 마이그레이션 검증");

            try
            {
                // 유효한 마이그레이션된 카드 생성
                var validCard = CardData.CreateDamageCard("테스트 유효", "유효한 카드", 2, 3);

                // 검증 테스트
                bool isValid = CardDataMigrator.ValidateMigration(validCard);

                if (!isValid)
                {
                    Debug.LogError("유효한 카드가 검증에 실패함");
                    return false;
                }

                // null 카드 검증 테스트
                bool nullValidation = CardDataMigrator.ValidateMigration(null);
                if (nullValidation)
                {
                    Debug.LogError("null 카드가 유효한 것으로 검증됨");
                    return false;
                }

                Debug.Log("✅ 마이그레이션 검증 테스트 통과");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"마이그레이션 검증 테스트 예외: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reflection을 사용하여 private 필드 설정 (테스트용)
        /// </summary>
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        /// <summary>
        /// 샘플 카드 데이터 생성 (테스트용)
        /// </summary>
        [MenuItem("Tools/Card System/Create Test Cards")]
        public static void CreateTestCards()
        {
            Debug.Log("테스트 카드 데이터 생성 중...");

            var testFolderPath = "Assets/TestCards";
            if (!AssetDatabase.IsValidFolder(testFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "TestCards");
            }

            // 1. 데미지 카드
            var fireball = CardData.CreateDamageCard("화염구", "적에게 3 피해를 줍니다", 2, 3, AffectedType.Enemy, 1);
            AssetDatabase.CreateAsset(fireball, $"{testFolderPath}/Fireball.asset");

            // 2. 회복 카드
            var heal = CardData.CreateHealCard("치유", "아군을 2 회복시킵니다", 1, 2, AffectedType.Ally, 0);
            AssetDatabase.CreateAsset(heal, $"{testFolderPath}/Heal.asset");

            // 3. 복합 효과 카드
            var complexEffects = new EffectData[]
            {
                new EffectData(EffectType.Damage, 2, AffectedType.Enemy, 1),
                new EffectData(EffectType.Heal, 1, AffectedType.Ally, 0)
            };
            var vampireBolt = CardData.CreateMultiEffectCard("흡혈 화살", "적에게 2 피해, 자신을 1 회복", CardData.CardType.Spell, 3, complexEffects);
            AssetDatabase.CreateAsset(vampireBolt, $"{testFolderPath}/VampireBolt.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"테스트 카드 생성 완료: {testFolderPath}");
            EditorUtility.DisplayDialog("테스트 카드 생성", $"3개의 테스트 카드가 '{testFolderPath}'에 생성되었습니다.", "확인");
        }

        /// <summary>
        /// 마이그레이션 상태 리포트 생성
        /// </summary>
        [MenuItem("Tools/Card System/Generate Migration Report")]
        public static void GenerateMigrationReport()
        {
            var guids = AssetDatabase.FindAssets("t:CardData");
            var report = new System.Text.StringBuilder();

            report.AppendLine("CardData Migration Report");
            report.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("=====================================");
            report.AppendLine();

            int total = guids.Length;
            int migrated = 0;
            int legacy = 0;
            int invalid = 0;

            report.AppendLine($"Total CardData Assets: {total}");
            report.AppendLine();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);

                if (cardData != null)
                {
                    var status = "";
                    if (cardData.IsEffectBasedCard)
                    {
                        if (cardData.IsValid())
                        {
                            migrated++;
                            status = "MIGRATED";
                        }
                        else
                        {
                            invalid++;
                            status = "INVALID";
                        }
                    }
                    else
                    {
                        legacy++;
                        status = "LEGACY";
                    }

                    report.AppendLine($"[{status}] {cardData.CardName} ({cardData.Type}) - {path}");

                    if (cardData.IsEffectBasedCard)
                    {
                        foreach (var effect in cardData.EffectDataList)
                        {
                            report.AppendLine($"    - {effect}");
                        }
                    }
                }
            }

            report.AppendLine();
            report.AppendLine("Summary:");
            report.AppendLine($"- Migrated: {migrated} ({(float)migrated / total * 100:F1}%)");
            report.AppendLine($"- Legacy: {legacy} ({(float)legacy / total * 100:F1}%)");
            report.AppendLine($"- Invalid: {invalid} ({(float)invalid / total * 100:F1}%)");

            var reportPath = Application.dataPath + "/../CardData_Migration_Report.txt";
            System.IO.File.WriteAllText(reportPath, report.ToString());

            Debug.Log($"마이그레이션 리포트 생성 완료: {reportPath}");
            EditorUtility.DisplayDialog("리포트 생성", $"마이그레이션 리포트가 생성되었습니다:\n{reportPath}", "확인");

            // 파일 열기 (Windows에서)
            if (EditorUtility.DisplayDialog("리포트 열기", "생성된 리포트를 지금 여시겠습니까?", "예", "아니오"))
            {
                System.Diagnostics.Process.Start(reportPath);
            }
        }
    }
}