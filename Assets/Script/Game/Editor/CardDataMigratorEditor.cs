using UnityEngine;
using UnityEditor;
using Game.Data;

namespace Game.Editor
{
    /// <summary>
    /// Phase 3.13: CardDataMigrator용 에디터 창
    /// CardData 마이그레이션을 위한 사용자 인터페이스를 제공합니다.
    /// </summary>
    public class CardDataMigratorEditor : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool createBackup = true;
        private bool dryRun = false;
        private CardDataMigrator.MigrationResult lastResult;
        private bool showResult = false;

        [MenuItem("Tools/Card System/CardData Migrator")]
        public static void ShowWindow()
        {
            var window = GetWindow<CardDataMigratorEditor>("CardData Migrator");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 제목
            EditorGUILayout.Space();
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("CardData Migration Tool", titleStyle);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Phase 3.13: 기존 카드 데이터를 새로운 EffectData 시스템으로 마이그레이션합니다.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(20);

            // 경고 메시지
            EditorGUILayout.HelpBox(
                "⚠️ 중요: 마이그레이션은 카드 데이터를 영구적으로 변경합니다.\n" +
                "• 작업 전 프로젝트를 백업하세요\n" +
                "• 먼저 드라이런(테스트)을 실행해보세요\n" +
                "• 마이그레이션 후에는 레거시 필드들이 사용되지 않습니다",
                MessageType.Warning
            );

            EditorGUILayout.Space(10);

            // 설정 옵션
            EditorGUILayout.LabelField("마이그레이션 설정", EditorStyles.boldLabel);

            createBackup = EditorGUILayout.Toggle(new GUIContent("백업 생성", "마이그레이션 전 원본 파일들을 백업합니다"), createBackup);
            dryRun = EditorGUILayout.Toggle(new GUIContent("드라이런 (테스트)", "실제 변경 없이 마이그레이션을 시뮬레이션합니다"), dryRun);

            EditorGUILayout.Space(10);

            // 변환 규칙 설명
            EditorGUILayout.LabelField("변환 규칙", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Unit 카드 → Summon 효과 (unitToSummon 필드 기반)\n" +
                "• Spell 카드 → Damage/Heal 효과 (SpellType 필드 기반)\n" +
                "  - Damage → Damage 효과\n" +
                "  - Heal → Heal 효과\n" +
                "  - Summon → Summon 효과\n" +
                "  - 기타 → Damage 효과로 대체 (경고 발생)",
                MessageType.Info
            );

            EditorGUILayout.Space(20);

            // 마이그레이션 버튼
            GUI.enabled = !EditorApplication.isPlaying;

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fixedHeight = 40
            };

            if (dryRun)
            {
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("드라이런 실행 (테스트)", buttonStyle))
                {
                    ExecuteMigration();
                }
            }
            else
            {
                GUI.backgroundColor = createBackup ? Color.green : Color.red;
                var buttonText = createBackup ? "마이그레이션 실행" : "⚠️ 백업 없이 마이그레이션";
                if (GUILayout.Button(buttonText, buttonStyle))
                {
                    if (EditorUtility.DisplayDialog(
                        "마이그레이션 확인",
                        createBackup
                            ? "모든 CardData를 새로운 EffectData 시스템으로 마이그레이션하시겠습니까?\n백업이 생성됩니다."
                            : "정말로 백업 없이 마이그레이션을 진행하시겠습니까?\n이 작업은 되돌릴 수 없습니다!",
                        "실행", "취소"))
                    {
                        ExecuteMigration();
                    }
                }
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            // 개별 카드 테스트
            EditorGUILayout.LabelField("개별 카드 테스트", EditorStyles.boldLabel);

            var testCard = EditorGUILayout.ObjectField("테스트할 카드", null, typeof(CardData), false) as CardData;
            if (testCard != null && GUILayout.Button("이 카드만 테스트"))
            {
                TestSingleCard(testCard);
            }

            EditorGUILayout.Space(20);

            // 결과 표시
            if (showResult && lastResult != null)
            {
                DisplayMigrationResult();
            }

            // 유틸리티 버튼들
            EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);

            if (GUILayout.Button("프로젝트의 모든 CardData 찾기"))
            {
                FindAllCardData();
            }

            if (GUILayout.Button("마이그레이션된 카드 개수 확인"))
            {
                CheckMigrationStatus();
            }

            EditorGUILayout.EndScrollView();
        }

        private void ExecuteMigration()
        {
            EditorUtility.DisplayProgressBar("CardData Migration", "마이그레이션 진행 중...", 0.5f);

            try
            {
                lastResult = CardDataMigrator.MigrateAllCards(createBackup, dryRun);
                showResult = true;

                if (lastResult.IsSuccessful)
                {
                    EditorUtility.DisplayDialog(
                        "마이그레이션 완료",
                        $"마이그레이션이 완료되었습니다!\n\n" +
                        $"처리된 카드: {lastResult.ProcessedCards}\n" +
                        $"성공: {lastResult.SuccessfulMigrations}\n" +
                        $"실패: {lastResult.FailedMigrations}\n" +
                        $"건너뜀: {lastResult.SkippedCards}",
                        "확인"
                    );
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "마이그레이션 실패",
                        $"마이그레이션 중 오류가 발생했습니다.\n\n" +
                        $"오류 개수: {lastResult.ErrorMessages.Count}\n" +
                        $"자세한 내용은 콘솔을 확인해주세요.",
                        "확인"
                    );
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "치명적 오류",
                    $"마이그레이션 중 예기치 않은 오류가 발생했습니다:\n{ex.Message}",
                    "확인"
                );
                Debug.LogError($"CardDataMigratorEditor: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void TestSingleCard(CardData cardData)
        {
            if (cardData == null) return;

            Debug.Log($"=== {cardData.CardName} 단일 카드 테스트 ===");

            // 현재 상태 출력
            CardDataMigrator.PrintMigrationStatus(cardData);

            // 테스트 마이그레이션
            var result = CardDataMigrator.MigrateCard(cardData, true); // 드라이런으로 테스트

            if (result.success)
            {
                if (result.wasAlreadyMigrated)
                {
                    EditorUtility.DisplayDialog(
                        "테스트 결과",
                        $"카드 '{cardData.CardName}'는 이미 마이그레이션되었습니다.",
                        "확인"
                    );
                }
                else
                {
                    var message = $"카드 '{cardData.CardName}' 마이그레이션 테스트 성공!";
                    if (!string.IsNullOrEmpty(result.warningMessage))
                    {
                        message += $"\n\n경고: {result.warningMessage}";
                    }

                    EditorUtility.DisplayDialog("테스트 결과", message, "확인");
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "테스트 실패",
                    $"카드 '{cardData.CardName}' 마이그레이션 테스트 실패:\n{result.errorMessage}",
                    "확인"
                );
            }
        }

        private void FindAllCardData()
        {
            var guids = AssetDatabase.FindAssets("t:CardData");
            var count = guids.Length;

            Debug.Log($"=== 프로젝트 CardData 현황 ===");
            Debug.Log($"총 {count}개의 CardData 발견");

            int migratedCount = 0;
            int legacyCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);

                if (cardData != null)
                {
                    if (cardData.IsEffectBasedCard)
                    {
                        migratedCount++;
                        Debug.Log($"  [마이그레이션됨] {cardData.CardName} ({path})");
                    }
                    else
                    {
                        legacyCount++;
                        Debug.Log($"  [레거시] {cardData.CardName} ({path})");
                    }
                }
            }

            Debug.Log($"마이그레이션됨: {migratedCount}개");
            Debug.Log($"레거시: {legacyCount}개");
            Debug.Log("=============================");

            EditorUtility.DisplayDialog(
                "CardData 검색 완료",
                $"총 {count}개의 CardData 발견\n\n" +
                $"마이그레이션됨: {migratedCount}개\n" +
                $"레거시 시스템: {legacyCount}개\n\n" +
                "자세한 내용은 콘솔을 확인해주세요.",
                "확인"
            );
        }

        private void CheckMigrationStatus()
        {
            var guids = AssetDatabase.FindAssets("t:CardData");
            int total = guids.Length;
            int migrated = 0;
            int legacy = 0;
            int invalid = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);

                if (cardData != null)
                {
                    if (cardData.IsEffectBasedCard)
                    {
                        if (cardData.IsValid())
                        {
                            migrated++;
                        }
                        else
                        {
                            invalid++;
                            Debug.LogWarning($"유효하지 않은 마이그레이션된 카드: {cardData.CardName}");
                        }
                    }
                    else
                    {
                        legacy++;
                    }
                }
            }

            float migrationProgress = total > 0 ? (float)migrated / total * 100 : 0;

            var statusMessage = $"마이그레이션 상태\n\n" +
                              $"전체 카드: {total}개\n" +
                              $"마이그레이션 완료: {migrated}개 ({migrationProgress:F1}%)\n" +
                              $"레거시 시스템: {legacy}개\n" +
                              $"유효하지 않음: {invalid}개";

            if (invalid > 0)
            {
                statusMessage += "\n\n⚠️ 유효하지 않은 카드가 있습니다.\n콘솔에서 자세한 내용을 확인해주세요.";
            }

            EditorUtility.DisplayDialog("마이그레이션 상태", statusMessage, "확인");
        }

        private void DisplayMigrationResult()
        {
            EditorGUILayout.LabelField("마지막 마이그레이션 결과", EditorStyles.boldLabel);

            var resultStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.BeginVertical(resultStyle);

            EditorGUILayout.LabelField($"실행 시간: {lastResult.MigrationTimestamp:yyyy-MM-dd HH:mm:ss}");
            EditorGUILayout.LabelField($"처리된 카드: {lastResult.ProcessedCards}개");
            EditorGUILayout.LabelField($"성공: {lastResult.SuccessfulMigrations}개");
            EditorGUILayout.LabelField($"실패: {lastResult.FailedMigrations}개");
            EditorGUILayout.LabelField($"건너뜀: {lastResult.SkippedCards}개");

            if (lastResult.ErrorMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"오류 ({lastResult.ErrorMessages.Count}개):", EditorStyles.boldLabel);
                foreach (var error in lastResult.ErrorMessages)
                {
                    EditorGUILayout.LabelField($"• {error}", EditorStyles.wordWrappedLabel);
                }
            }

            if (lastResult.WarningMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"경고 ({lastResult.WarningMessages.Count}개):", EditorStyles.boldLabel);
                foreach (var warning in lastResult.WarningMessages)
                {
                    EditorGUILayout.LabelField($"• {warning}", EditorStyles.wordWrappedLabel);
                }
            }

            EditorGUILayout.EndVertical();

            if (GUILayout.Button("결과 숨기기"))
            {
                showResult = false;
            }
        }
    }
}