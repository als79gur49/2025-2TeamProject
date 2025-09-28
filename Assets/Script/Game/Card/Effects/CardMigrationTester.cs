using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;
using Game.Card.Effects;
using Game.Card.Migration;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Card.Testing
{
    /// <summary>
    /// CardData 마이그레이션 테스팅 및 검증 유틸리티
    /// Phase 1.3: 마이그레이션 전략 수립의 일환으로 안전성 확보
    /// </summary>
    public static class CardMigrationTester
    {
        /// <summary>
        /// 테스트 케이스 정의
        /// </summary>
        [Serializable]
        public class TestCase
        {
            public string TestName;
            public CardData.CardType InputCardType;
            public CardData.SpellType InputSpellType;
            public CardData.TargetType InputTargetType;
            public int InputValue;
            public EffectType ExpectedEffectType;
            public AffectedType ExpectedAffectedType;
            public bool ShouldSucceed;
            public string Description;

            public TestCase(string name, CardData.CardType cardType, EffectType expectedEffect, bool shouldSucceed, string desc)
            {
                TestName = name;
                InputCardType = cardType;
                InputSpellType = CardData.SpellType.Damage;
                InputTargetType = CardData.TargetType.Enemy;
                InputValue = 1;
                ExpectedEffectType = expectedEffect;
                ExpectedAffectedType = AffectedType.Enemy;
                ShouldSucceed = shouldSucceed;
                Description = desc;
            }
        }

        /// <summary>
        /// 테스트 결과
        /// </summary>
        [Serializable]
        public struct TestResult
        {
            public string TestName;
            public bool Passed;
            public string Message;
            public CardDataMigrator.MigrationResult MigrationResult;

            public TestResult(string testName, bool passed, string message, CardDataMigrator.MigrationResult migrationResult = default)
            {
                TestName = testName;
                Passed = passed;
                Message = message;
                MigrationResult = migrationResult;
            }
        }

        /// <summary>
        /// 사전 정의된 테스트 케이스들
        /// </summary>
        private static readonly List<TestCase> TestCases = new List<TestCase>
        {
            new TestCase("기본 유닛 카드", CardData.CardType.Unit, EffectType.Summon, true, "일반적인 유닛 카드 소환 효과 변환"),
            new TestCase("데미지 스펠", CardData.CardType.Spell, EffectType.Damage, true, "적에게 데미지를 주는 주문"),
            new TestCase("힐 스펠", CardData.CardType.Spell, EffectType.Heal, true, "아군을 회복시키는 주문"),
            new TestCase("소환 스펠", CardData.CardType.Spell, EffectType.Summon, true, "유닛을 소환하는 주문")
        };

        /// <summary>
        /// 런타임 테스트 실행
        /// </summary>
        public static List<TestResult> RunAllTests()
        {
            Debug.Log("[CardMigrationTester] 마이그레이션 테스트 시작...");

            var results = new List<TestResult>();

            // 기본 테스트 케이스 실행
            foreach (var testCase in TestCases)
            {
                var result = RunSingleTest(testCase);
                results.Add(result);
            }

            // 실제 프로젝트 카드 데이터 테스트
            var projectResults = TestProjectCardData();
            results.AddRange(projectResults);

            // 결과 요약
            var passedCount = results.Count(r => r.Passed);
            var failedCount = results.Count - passedCount;

            Debug.Log($"[CardMigrationTester] 테스트 완료: 통과 {passedCount}, 실패 {failedCount}");

            return results;
        }

        /// <summary>
        /// 단일 테스트 케이스 실행
        /// </summary>
        private static TestResult RunSingleTest(TestCase testCase)
        {
            try
            {
                // 테스트용 CardData 생성
                var testCard = CreateTestCardData(testCase);
                if (testCard == null)
                {
                    return new TestResult(testCase.TestName, false, "테스트 카드 생성 실패");
                }

                // 마이그레이션 실행
                var migrationResult = CardDataMigrator.MigrateCardData(testCard);

                // 결과 검증
                return ValidateTestResult(testCase, migrationResult);
            }
            catch (Exception ex)
            {
                return new TestResult(testCase.TestName, false, $"테스트 실행 중 예외 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 테스트용 CardData 생성
        /// </summary>
        private static CardData CreateTestCardData(TestCase testCase)
        {
            var card = ScriptableObject.CreateInstance<CardData>();

            // 리플렉션을 사용하여 private 필드 설정
            var cardType = typeof(CardData);

            // 기본 필드 설정
            SetPrivateField(card, "cardName", testCase.TestName);
            SetPrivateField(card, "description", testCase.Description);
            SetPrivateField(card, "cardType", testCase.InputCardType);
            SetPrivateField(card, "targetType", testCase.InputTargetType);
            SetPrivateField(card, "manaCost", 1);

            if (testCase.InputCardType == CardData.CardType.Unit)
            {
                // 유닛 카드의 경우 UnitData 생성
                var unitData = CreateTestUnitData(testCase.TestName);
                SetPrivateField(card, "unitToSummon", unitData);
            }
            else if (testCase.InputCardType == CardData.CardType.Spell)
            {
                // 스펠 카드의 경우 스펠 데이터 설정
                SetPrivateField(card, "spellType", testCase.InputSpellType);
                SetPrivateField(card, "spellEffectValue", testCase.InputValue);
                SetPrivateField(card, "spellRange", 3f);
            }

            return card;
        }

        /// <summary>
        /// 테스트용 UnitData 생성
        /// </summary>
        private static UnitData CreateTestUnitData(string unitName)
        {
            var unitData = ScriptableObject.CreateInstance<UnitData>();
            SetPrivateField(unitData, "unitName", $"Test_{unitName}");
            SetPrivateField(unitData, "health", 10);
            SetPrivateField(unitData, "attack", 5);
            return unitData;
        }

        /// <summary>
        /// 리플렉션을 사용하여 private 필드 설정
        /// </summary>
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        /// <summary>
        /// 테스트 결과 검증
        /// </summary>
        private static TestResult ValidateTestResult(TestCase testCase, CardDataMigrator.MigrationResult migrationResult)
        {
            // 성공/실패 예상 검증
            if (testCase.ShouldSucceed != migrationResult.Success)
            {
                var expected = testCase.ShouldSucceed ? "성공" : "실패";
                var actual = migrationResult.Success ? "성공" : "실패";
                return new TestResult(testCase.TestName, false,
                    $"예상 결과 불일치: 예상 {expected}, 실제 {actual}", migrationResult);
            }

            // 성공한 경우 추가 검증
            if (migrationResult.Success)
            {
                // 효과 타입 검증
                if (migrationResult.ConvertedEffects == null || migrationResult.ConvertedEffects.Count == 0)
                {
                    return new TestResult(testCase.TestName, false, "변환된 효과가 없음", migrationResult);
                }

                var firstEffect = migrationResult.ConvertedEffects[0];
                if (firstEffect.Type != testCase.ExpectedEffectType)
                {
                    return new TestResult(testCase.TestName, false,
                        $"효과 타입 불일치: 예상 {testCase.ExpectedEffectType}, 실제 {firstEffect.Type}", migrationResult);
                }

                // AffectedType 검증 (Summon 제외)
                if (testCase.ExpectedEffectType != EffectType.Summon &&
                    firstEffect.AffectedType != testCase.ExpectedAffectedType)
                {
                    return new TestResult(testCase.TestName, false,
                        $"영향 대상 타입 불일치: 예상 {testCase.ExpectedAffectedType}, 실제 {firstEffect.AffectedType}", migrationResult);
                }
            }

            return new TestResult(testCase.TestName, true, "테스트 통과", migrationResult);
        }

        /// <summary>
        /// 프로젝트 내 실제 CardData 테스트
        /// </summary>
        private static List<TestResult> TestProjectCardData()
        {
            var results = new List<TestResult>();

            // 프로젝트에서 CardData 찾기
            var allCards = Resources.LoadAll<CardData>("");

            if (allCards.Length == 0)
            {
                results.Add(new TestResult("프로젝트 카드 테스트", false, "프로젝트에서 CardData를 찾을 수 없음"));
                return results;
            }

            Debug.Log($"[CardMigrationTester] {allCards.Length}개의 프로젝트 카드 테스트 중...");

            foreach (var card in allCards)
            {
                var migrationResult = CardDataMigrator.MigrateCardData(card);
                var testResult = new TestResult(
                    $"프로젝트_카드_{card.CardName}",
                    migrationResult.Success,
                    migrationResult.Message,
                    migrationResult
                );
                results.Add(testResult);
            }

            return results;
        }

        /// <summary>
        /// 테스트 보고서 생성
        /// </summary>
        public static string GenerateTestReport(List<TestResult> results)
        {
            var report = "# CardData 마이그레이션 테스트 보고서\n\n";
            report += $"테스트 실행 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";

            var passedResults = results.Where(r => r.Passed).ToList();
            var failedResults = results.Where(r => !r.Passed).ToList();

            report += "## 요약\n";
            report += $"- 총 테스트: {results.Count}\n";
            report += $"- 통과: {passedResults.Count}\n";
            report += $"- 실패: {failedResults.Count}\n";
            report += $"- 성공률: {(passedResults.Count * 100.0 / results.Count):F1}%\n\n";

            if (failedResults.Count > 0)
            {
                report += "## 실패한 테스트\n\n";
                foreach (var result in failedResults)
                {
                    report += $"### {result.TestName}\n";
                    report += $"- 오류: {result.Message}\n\n";
                }
            }

            report += "## 모든 테스트 결과\n\n";
            foreach (var result in results)
            {
                var status = result.Passed ? "✅ 통과" : "❌ 실패";
                report += $"- **{result.TestName}**: {status}\n";
                if (!string.IsNullOrEmpty(result.Message))
                {
                    report += $"  - {result.Message}\n";
                }
            }

            return report;
        }

        /// <summary>
        /// 성능 테스트 실행
        /// </summary>
        public static void RunPerformanceTest(int iterations = 1000)
        {
            Debug.Log($"[CardMigrationTester] 성능 테스트 시작 ({iterations}회 반복)...");

            var testCard = CreateTestCardData(TestCases[0]); // 첫 번째 테스트 케이스 사용
            var startTime = DateTime.Now;

            for (int i = 0; i < iterations; i++)
            {
                CardDataMigrator.MigrateCardData(testCard);
            }

            var endTime = DateTime.Now;
            var totalTime = (endTime - startTime).TotalMilliseconds;
            var avgTime = totalTime / iterations;

            Debug.Log($"[CardMigrationTester] 성능 테스트 완료:");
            Debug.Log($"- 총 시간: {totalTime:F2}ms");
            Debug.Log($"- 평균 시간: {avgTime:F3}ms/회");
            Debug.Log($"- 초당 처리량: {1000.0 / avgTime:F0}회/초");
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터용 테스트 실행 메뉴
        /// </summary>
        [MenuItem("Tools/Card Migration/Run Tests")]
        public static void RunTestsFromMenu()
        {
            var results = RunAllTests();
            var report = GenerateTestReport(results);

            // 보고서를 파일로 저장
            var path = Application.dataPath + "/CardMigrationTestReport.md";
            System.IO.File.WriteAllText(path, report);

            Debug.Log($"테스트 보고서가 {path}에 저장되었습니다.");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 에디터용 성능 테스트 메뉴
        /// </summary>
        [MenuItem("Tools/Card Migration/Run Performance Test")]
        public static void RunPerformanceTestFromMenu()
        {
            RunPerformanceTest();
        }

        /// <summary>
        /// 에디터용 전체 프로젝트 분석 메뉴
        /// </summary>
        [MenuItem("Tools/Card Migration/Analyze All Cards")]
        public static void AnalyzeAllCardsFromMenu()
        {
            var results = CardDataMigrator.AnalyzeAllCards();
            var report = CardDataMigrator.GenerateMigrationReport(results);

            // 보고서를 파일로 저장
            var path = Application.dataPath + "/CardMigrationAnalysisReport.md";
            System.IO.File.WriteAllText(path, report);

            Debug.Log($"분석 보고서가 {path}에 저장되었습니다.");
            AssetDatabase.Refresh();
        }
#endif
    }
}