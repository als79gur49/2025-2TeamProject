using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Card.Migration;
using Game.Card.Backup;
using Game.Card.Testing;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Card.Migration
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.3: 마이그레이션 통합 코디네이터
    /// 전체 마이그레이션 프로세스를 조율하고 안전성을 보장하는 중앙 관리자
    /// </summary>
    public static class CardMigrationCoordinator
    {
        /// <summary>
        /// 마이그레이션 단계 정의
        /// </summary>
        public enum MigrationPhase
        {
            /// <summary>마이그레이션 준비 및 검증</summary>
            Preparation,
            /// <summary>백업 생성</summary>
            Backup,
            /// <summary>마이그레이션 테스트</summary>
            Testing,
            /// <summary>실제 마이그레이션 실행</summary>
            Execution,
            /// <summary>마이그레이션 후 검증</summary>
            Validation,
            /// <summary>완료</summary>
            Completed,
            /// <summary>실패</summary>
            Failed
        }

        /// <summary>
        /// 전체 마이그레이션 상태
        /// </summary>
        [Serializable]
        public struct MigrationStatus
        {
            public MigrationPhase CurrentPhase;
            public float Progress; // 0.0 ~ 1.0
            public string CurrentOperation;
            public int TotalCards;
            public int ProcessedCards;
            public int SuccessfulCards;
            public int FailedCards;
            public List<string> Errors;
            public string BackupId;
            public DateTime StartTime;
            public DateTime? EndTime;

            public bool IsCompleted => CurrentPhase == MigrationPhase.Completed;
            public bool IsFailed => CurrentPhase == MigrationPhase.Failed;
            public bool IsInProgress => !IsCompleted && !IsFailed;
            public TimeSpan ElapsedTime => (EndTime ?? DateTime.Now) - StartTime;

            public MigrationStatus(int totalCards)
            {
                CurrentPhase = MigrationPhase.Preparation;
                Progress = 0f;
                CurrentOperation = "마이그레이션 준비 중...";
                TotalCards = totalCards;
                ProcessedCards = 0;
                SuccessfulCards = 0;
                FailedCards = 0;
                Errors = new List<string>();
                BackupId = "";
                StartTime = DateTime.Now;
                EndTime = null;
            }
        }

        /// <summary>
        /// 마이그레이션 설정
        /// </summary>
        [Serializable]
        public class MigrationSettings
        {
            [Header("안전성 설정")]
            public bool CreateBackupBeforeMigration = true;
            public bool RunTestsBeforeMigration = true;
            public bool ValidateAfterMigration = true;
            public bool AutoRollbackOnFailure = true;

            [Header("실행 설정")]
            public bool DryRunMode = false; // 실제 변경 없이 테스트만
            public bool LogDetailedProgress = true;
            public bool PauseOnErrors = false;

            [Header("성능 설정")]
            public int BatchSize = 10; // 한 번에 처리할 카드 수
            public float DelayBetweenBatches = 0.1f; // 배치 간 대기 시간 (초)

            [Header("품질 설정")]
            public float MinimumSuccessRate = 0.9f; // 최소 성공률 (90%)
            public int MaxRetryAttempts = 3;
        }

        private static MigrationStatus currentStatus;
        private static MigrationSettings currentSettings;
        private static readonly object statusLock = new object();

        /// <summary>
        /// 현재 마이그레이션 상태 조회
        /// </summary>
        public static MigrationStatus GetCurrentStatus()
        {
            lock (statusLock)
            {
                return currentStatus;
            }
        }

        /// <summary>
        /// 전체 마이그레이션 프로세스 실행
        /// </summary>
        /// <param name="settings">마이그레이션 설정</param>
        /// <returns>마이그레이션 성공 여부</returns>
        public static bool ExecuteFullMigration(MigrationSettings settings = null)
        {
            settings ??= new MigrationSettings();
            currentSettings = settings;

            // 프로젝트 내 모든 CardData 찾기
            var allCards = FindAllProjectCards();
            if (allCards.Count == 0)
            {
                Debug.LogWarning("[CardMigrationCoordinator] 마이그레이션할 카드가 없습니다.");
                return false;
            }

            // 마이그레이션 상태 초기화
            lock (statusLock)
            {
                currentStatus = new MigrationStatus(allCards.Count);
            }

            Debug.Log($"[CardMigrationCoordinator] 마이그레이션 시작: {allCards.Count}개 카드");

            try
            {
                // Phase 1: 준비 및 검증
                if (!ExecutePreparationPhase(allCards))
                {
                    return FailMigration("준비 단계 실패");
                }

                // Phase 2: 백업 생성
                if (settings.CreateBackupBeforeMigration && !ExecuteBackupPhase(allCards))
                {
                    return FailMigration("백업 단계 실패");
                }

                // Phase 3: 테스트 실행
                if (settings.RunTestsBeforeMigration && !ExecuteTestingPhase())
                {
                    return FailMigration("테스트 단계 실패");
                }

                // Phase 4: 마이그레이션 실행
                if (!ExecuteMigrationPhase(allCards))
                {
                    if (settings.AutoRollbackOnFailure && !string.IsNullOrEmpty(currentStatus.BackupId))
                    {
                        Debug.LogWarning("[CardMigrationCoordinator] 마이그레이션 실패. 롤백을 시도합니다...");
                        CardMigrationBackup.RestoreFromBackup(currentStatus.BackupId);
                    }
                    return FailMigration("마이그레이션 실행 실패");
                }

                // Phase 5: 검증
                if (settings.ValidateAfterMigration && !ExecuteValidationPhase())
                {
                    return FailMigration("검증 단계 실패");
                }

                // 완료
                return CompleteMigration();
            }
            catch (Exception ex)
            {
                return FailMigration($"예기치 않은 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Phase 1: 준비 및 검증
        /// </summary>
        private static bool ExecutePreparationPhase(List<Game.Data.CardData> allCards)
        {
            UpdateStatus(MigrationPhase.Preparation, "마이그레이션 대상 카드 분석 중...", 0.1f);

            // 마이그레이션 가능성 사전 분석
            var analysisResults = CardDataMigrator.AnalyzeAllCards();
            var successCount = analysisResults.Count(r => r.Success);
            var successRate = (float)successCount / analysisResults.Count;

            if (successRate < currentSettings.MinimumSuccessRate)
            {
                AddError($"예상 성공률이 너무 낮습니다: {successRate:P1} (최소 요구: {currentSettings.MinimumSuccessRate:P1})");
                return false;
            }

            Debug.Log($"[CardMigrationCoordinator] 준비 단계 완료: 예상 성공률 {successRate:P1}");
            return true;
        }

        /// <summary>
        /// Phase 2: 백업 생성
        /// </summary>
        private static bool ExecuteBackupPhase(List<Game.Data.CardData> allCards)
        {
            UpdateStatus(MigrationPhase.Backup, "카드 데이터 백업 생성 중...", 0.2f);

            var backupInfo = CardMigrationBackup.CreateFullBackup("마이그레이션 전 자동 백업");
            if (string.IsNullOrEmpty(backupInfo.BackupId))
            {
                AddError("백업 생성 실패");
                return false;
            }

            lock (statusLock)
            {
                var status = currentStatus;
                status.BackupId = backupInfo.BackupId;
                currentStatus = status;
            }

            Debug.Log($"[CardMigrationCoordinator] 백업 생성 완료: {backupInfo.BackupId}");
            return true;
        }

        /// <summary>
        /// Phase 3: 테스트 실행
        /// </summary>
        private static bool ExecuteTestingPhase()
        {
            UpdateStatus(MigrationPhase.Testing, "마이그레이션 테스트 실행 중...", 0.3f);

            var testResults = CardMigrationTester.RunAllTests();
            var passedCount = testResults.Count(r => r.Passed);
            var passRate = (float)passedCount / testResults.Count;

            if (passRate < currentSettings.MinimumSuccessRate)
            {
                AddError($"테스트 통과율이 너무 낮습니다: {passRate:P1}");
                return false;
            }

            Debug.Log($"[CardMigrationCoordinator] 테스트 완료: {passedCount}/{testResults.Count} 통과");
            return true;
        }

        /// <summary>
        /// Phase 4: 마이그레이션 실행
        /// </summary>
        private static bool ExecuteMigrationPhase(List<Game.Data.CardData> allCards)
        {
            UpdateStatus(MigrationPhase.Execution, "마이그레이션 실행 중...", 0.4f);

            var processedCount = 0;
            var successCount = 0;
            var batchCount = 0;

            // 배치 단위로 처리
            for (int i = 0; i < allCards.Count; i += currentSettings.BatchSize)
            {
                var batch = allCards.Skip(i).Take(currentSettings.BatchSize).ToList();
                batchCount++;

                Debug.Log($"[CardMigrationCoordinator] 배치 {batchCount} 처리 중... ({batch.Count}개 카드)");

                foreach (var card in batch)
                {
                    if (currentSettings.DryRunMode)
                    {
                        // 드라이런 모드: 실제 변경 없이 분석만
                        var analysisResult = CardDataMigrator.MigrateCardData(card);
                        if (analysisResult.Success) successCount++;
                    }
                    else
                    {
                        // 실제 마이그레이션 실행
                        if (ExecuteSingleCardMigration(card))
                        {
                            successCount++;
                        }
                    }

                    processedCount++;

                    // 진행률 업데이트
                    var progress = 0.4f + (0.4f * processedCount / allCards.Count);
                    UpdateStatus(MigrationPhase.Execution,
                        $"카드 마이그레이션 중... ({processedCount}/{allCards.Count})", progress);
                }

                // 배치 간 대기
                if (currentSettings.DelayBetweenBatches > 0 && i + currentSettings.BatchSize < allCards.Count)
                {
                    System.Threading.Thread.Sleep((int)(currentSettings.DelayBetweenBatches * 1000));
                }
            }

            // 성공률 검증
            var finalSuccessRate = (float)successCount / processedCount;
            if (finalSuccessRate < currentSettings.MinimumSuccessRate)
            {
                AddError($"마이그레이션 성공률이 너무 낮습니다: {finalSuccessRate:P1}");
                return false;
            }

            lock (statusLock)
            {
                var status = currentStatus;
                status.ProcessedCards = processedCount;
                status.SuccessfulCards = successCount;
                status.FailedCards = processedCount - successCount;
                currentStatus = status;
            }

            Debug.Log($"[CardMigrationCoordinator] 마이그레이션 실행 완료: {successCount}/{processedCount} 성공");
            return true;
        }

        /// <summary>
        /// 단일 카드 마이그레이션 실행
        /// </summary>
        private static bool ExecuteSingleCardMigration(Game.Data.CardData card)
        {
            var attempts = 0;
            while (attempts < currentSettings.MaxRetryAttempts)
            {
                attempts++;

                try
                {
                    var result = CardDataMigrator.MigrateCardData(card);
                    if (result.Success)
                    {
                        // TODO: 실제로 CardData에 변환된 데이터 적용
                        // 이 부분은 Phase 2에서 구현될 예정
                        return true;
                    }
                    else if (attempts >= currentSettings.MaxRetryAttempts)
                    {
                        AddError($"카드 '{card.CardName}' 마이그레이션 실패 (재시도 {attempts}회): {result.Message}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    if (attempts >= currentSettings.MaxRetryAttempts)
                    {
                        AddError($"카드 '{card.CardName}' 마이그레이션 중 예외 발생: {ex.Message}");
                        return false;
                    }
                }

                // 재시도 대기
                System.Threading.Thread.Sleep(100);
            }

            return false;
        }

        /// <summary>
        /// Phase 5: 검증
        /// </summary>
        private static bool ExecuteValidationPhase()
        {
            UpdateStatus(MigrationPhase.Validation, "마이그레이션 결과 검증 중...", 0.9f);

            // 마이그레이션 후 재검증
            var postMigrationTests = CardMigrationTester.RunAllTests();
            var validationPassCount = postMigrationTests.Count(r => r.Passed);
            var validationPassRate = (float)validationPassCount / postMigrationTests.Count;

            if (validationPassRate < currentSettings.MinimumSuccessRate)
            {
                AddError($"마이그레이션 후 검증 실패: {validationPassRate:P1}");
                return false;
            }

            Debug.Log($"[CardMigrationCoordinator] 검증 완료: {validationPassCount}/{postMigrationTests.Count} 통과");
            return true;
        }

        /// <summary>
        /// 마이그레이션 완료 처리
        /// </summary>
        private static bool CompleteMigration()
        {
            lock (statusLock)
            {
                var status = currentStatus;
                status.CurrentPhase = MigrationPhase.Completed;
                status.Progress = 1.0f;
                status.CurrentOperation = "마이그레이션 완료";
                status.EndTime = DateTime.Now;
                currentStatus = status;
            }

            Debug.Log($"[CardMigrationCoordinator] 마이그레이션 완료! " +
                     $"처리 시간: {currentStatus.ElapsedTime.TotalSeconds:F1}초, " +
                     $"성공: {currentStatus.SuccessfulCards}/{currentStatus.TotalCards}");

            return true;
        }

        /// <summary>
        /// 마이그레이션 실패 처리
        /// </summary>
        private static bool FailMigration(string reason)
        {
            lock (statusLock)
            {
                var status = currentStatus;
                status.CurrentPhase = MigrationPhase.Failed;
                status.CurrentOperation = $"실패: {reason}";
                status.EndTime = DateTime.Now;
                currentStatus = status;
            }

            Debug.LogError($"[CardMigrationCoordinator] 마이그레이션 실패: {reason}");
            return false;
        }

        /// <summary>
        /// 상태 업데이트
        /// </summary>
        private static void UpdateStatus(MigrationPhase phase, string operation, float progress)
        {
            lock (statusLock)
            {
                var status = currentStatus;
                status.CurrentPhase = phase;
                status.CurrentOperation = operation;
                status.Progress = progress;
                currentStatus = status;
            }

            if (currentSettings?.LogDetailedProgress == true)
            {
                Debug.Log($"[CardMigrationCoordinator] {operation} ({progress:P1})");
            }
        }

        /// <summary>
        /// 오류 추가
        /// </summary>
        private static void AddError(string error)
        {
            lock (statusLock)
            {
                var status = currentStatus;
                status.Errors.Add($"[{DateTime.Now:HH:mm:ss}] {error}");
                currentStatus = status;
            }

            Debug.LogError($"[CardMigrationCoordinator] {error}");
        }

        /// <summary>
        /// 프로젝트 내 모든 CardData 찾기
        /// </summary>
        private static List<Game.Data.CardData> FindAllProjectCards()
        {
            var allCards = new List<Game.Data.CardData>();

#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets($"t:{typeof(Game.Data.CardData).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<Game.Data.CardData>(path);
                if (card != null) allCards.Add(card);
            }
#else
            var resourceCards = Resources.LoadAll<Game.Data.CardData>("");
            allCards.AddRange(resourceCards);
#endif

            return allCards;
        }

        /// <summary>
        /// 마이그레이션 보고서 생성
        /// </summary>
        public static string GenerateDetailedReport()
        {
            var status = GetCurrentStatus();
            var report = "# CardData 마이그레이션 상세 보고서\n\n";

            report += $"## 실행 정보\n";
            report += $"- 시작 시간: {status.StartTime:yyyy-MM-dd HH:mm:ss}\n";
            report += $"- 종료 시간: {(status.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "진행 중")}\n";
            report += $"- 실행 시간: {status.ElapsedTime.TotalSeconds:F1}초\n";
            report += $"- 현재 상태: {status.CurrentPhase}\n";
            report += $"- 진행률: {status.Progress:P1}\n\n";

            report += $"## 처리 결과\n";
            report += $"- 총 카드 수: {status.TotalCards}\n";
            report += $"- 처리된 카드: {status.ProcessedCards}\n";
            report += $"- 성공한 카드: {status.SuccessfulCards}\n";
            report += $"- 실패한 카드: {status.FailedCards}\n";

            if (status.ProcessedCards > 0)
            {
                var successRate = (float)status.SuccessfulCards / status.ProcessedCards;
                report += $"- 성공률: {successRate:P1}\n";
            }

            if (status.Errors.Count > 0)
            {
                report += $"\n## 오류 목록\n";
                foreach (var error in status.Errors)
                {
                    report += $"- {error}\n";
                }
            }

            return report;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 메뉴 - 전체 마이그레이션 실행
        /// </summary>
        [MenuItem("Tools/Card Migration/Execute Full Migration")]
        public static void ExecuteFullMigrationFromMenu()
        {
            var settings = new MigrationSettings
            {
                DryRunMode = EditorUtility.DisplayDialog("마이그레이션 모드 선택",
                    "실제 마이그레이션을 실행하시겠습니까?\n\n" +
                    "예: 실제 변경 사항 적용\n" +
                    "아니요: 드라이런 모드 (테스트만)",
                    "실제 실행", "드라이런 모드") == false
            };

            var success = ExecuteFullMigration(settings);
            var status = GetCurrentStatus();

            var message = success ? "마이그레이션이 성공적으로 완료되었습니다!" : "마이그레이션이 실패했습니다.";
            message += $"\n\n처리된 카드: {status.ProcessedCards}/{status.TotalCards}";
            message += $"\n성공한 카드: {status.SuccessfulCards}";
            message += $"\n실행 시간: {status.ElapsedTime.TotalSeconds:F1}초";

            EditorUtility.DisplayDialog(success ? "마이그레이션 완료" : "마이그레이션 실패", message, "확인");

            // 상세 보고서 저장
            var report = GenerateDetailedReport();
            var reportPath = Application.dataPath + "/CardMigrationDetailedReport.md";
            System.IO.File.WriteAllText(reportPath, report);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 에디터 메뉴 - 마이그레이션 상태 조회
        /// </summary>
        [MenuItem("Tools/Card Migration/Show Migration Status")]
        public static void ShowMigrationStatusFromMenu()
        {
            var status = GetCurrentStatus();
            var message = $"현재 상태: {status.CurrentPhase}\n";
            message += $"진행률: {status.Progress:P1}\n";
            message += $"현재 작업: {status.CurrentOperation}\n";
            message += $"처리된 카드: {status.ProcessedCards}/{status.TotalCards}\n";

            if (status.IsInProgress)
            {
                message += $"경과 시간: {status.ElapsedTime.TotalSeconds:F1}초";
            }

            EditorUtility.DisplayDialog("마이그레이션 상태", message, "확인");
        }
#endif
    }
}