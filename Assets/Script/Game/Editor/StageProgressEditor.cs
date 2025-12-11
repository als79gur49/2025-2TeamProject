using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Core;
using Game.Managers;
using Game.Data;
using Game.SaveSystem;

namespace Game.Editor
{
    /// <summary>
    /// 스테이지 진행도/클리어 상태를 제어하는 에디터 창
    /// - Play 모드에서 현재 스테이지를 강제 클리어
    /// - 특정 스테이지(에셋 선택 또는 ID 입력)를 원하는 점수로 클리어 처리
    /// </summary>
    public class StageProgressEditor : EditorWindow
    {
        // Runtime state
        private IStageProgressManager stageProgressManager;
        private bool hasRuntimeManager;
        private string runtimeStatusMessage;

        // Current stage section
        private int currentStageScore = 10000;
        private bool currentStagePerfectStats = true;

        // Manual stage section
        private StageDataSO selectedStageAsset;
        private string manualStageId = "";
        private int manualStageScore = 10000;
        private bool manualStagePerfectStats = true;

        [MenuItem("Tools/Stage/Stage Progress Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<StageProgressEditor>("Stage Progress");
            window.minSize = new Vector2(420, 520);
        }

        private void OnEnable()
        {
            RefreshRuntimeState();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Stage Progress Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            DrawRuntimeStatusSection();
            EditorGUILayout.Space(10);

            DrawCurrentStageSection();
            EditorGUILayout.Space(10);

            DrawManualStageSection();
        }

        #region Runtime State

        private void RefreshRuntimeState()
        {
            hasRuntimeManager = false;
            runtimeStatusMessage = string.Empty;
            stageProgressManager = null;

            if (!Application.isPlaying)
            {
                runtimeStatusMessage = "Play 모드가 아닙니다. 스테이지 클리어 기능은 Play 모드에서만 동작합니다.";
                return;
            }

            if (!ServiceLocator.IsRegistered<IStageProgressManager>())
            {
                runtimeStatusMessage = "ServiceLocator에 IStageProgressManager가 등록되지 않았습니다.\nServiceBootstrap 초기화가 완료되었는지 확인하세요.";
                return;
            }

            stageProgressManager = ServiceLocator.Get<IStageProgressManager>();
            if (stageProgressManager == null)
            {
                runtimeStatusMessage = "IStageProgressManager 인스턴스를 가져오지 못했습니다.";
                return;
            }

            hasRuntimeManager = true;
        }

        private void DrawRuntimeStatusSection()
        {
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            RefreshRuntimeState();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(runtimeStatusMessage, MessageType.Info);
            }
            else if (!hasRuntimeManager)
            {
                EditorGUILayout.HelpBox(runtimeStatusMessage, MessageType.Warning);
            }
            else
            {
                var progressData = stageProgressManager.GetProgressData();
                int totalRecords = progressData?.stageRecords != null ? progressData.stageRecords.Count : 0;

                EditorGUILayout.LabelField("StageProgressManager 상태", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("등록 상태", "OK");
                EditorGUILayout.LabelField("저장된 스테이지 레코드 수", totalRecords.ToString());
            }

            if (GUILayout.Button("Refresh", GUILayout.Height(22)))
            {
                RefreshRuntimeState();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Current Stage Section

        private void DrawCurrentStageSection()
        {
            EditorGUILayout.LabelField("Current Stage (Play Mode)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (!Application.isPlaying || !hasRuntimeManager)
            {
                EditorGUILayout.HelpBox("현재 스테이지 강제 클리어는 Play 모드에서 StageProgressManager가 활성화된 경우에만 가능합니다.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            string currentStageId = stageProgressManager.GetCurrentStageId();
            if (string.IsNullOrEmpty(currentStageId))
            {
                EditorGUILayout.HelpBox("현재 준비된 스테이지가 없습니다.\nStageSceneInitializer에서 PrepareStageForPlay가 호출되었는지 확인하세요.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            var stageData = stageProgressManager.GetStageData(currentStageId);
            if (stageData == null)
            {
                EditorGUILayout.HelpBox($"현재 스테이지 ID는 \"{currentStageId}\" 이지만 StageDataSO를 찾을 수 없습니다.", MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            var record = stageProgressManager.GetStageRecord(currentStageId);

            // Basic info
            EditorGUILayout.LabelField("현재 스테이지 정보", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Stage ID", currentStageId);
            EditorGUILayout.LabelField("Name", stageData.DisplayName);
            EditorGUILayout.LabelField("Chapter", stageData.ChapterId);
            EditorGUILayout.LabelField("Stage Number", stageData.StageNumber.ToString());

            if (record != null)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("진행 상태", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("State", record.state.ToString());
                EditorGUILayout.LabelField("Best Score", record.bestScore.ToString());
                EditorGUILayout.LabelField("Best Stars", record.bestStars.ToString());
                EditorGUILayout.LabelField("Clear Count", record.ClearCount.ToString());
            }

            EditorGUILayout.Space(5);

            // Scoring helpers
            var scoring = stageData.Scoring;
            if (scoring != null && scoring.starThresholds != null && scoring.starThresholds.Length > 0)
            {
                EditorGUILayout.LabelField("별 임계값 (Score Thresholds)", EditorStyles.boldLabel);
                string thresholdsText = string.Join(", ", scoring.starThresholds);
                EditorGUILayout.LabelField($"Star Thresholds: {thresholdsText}");

                EditorGUILayout.BeginHorizontal();
                if (scoring.starThresholds.Length >= 1 &&
                    GUILayout.Button($"1★ ({scoring.starThresholds[0]})"))
                {
                    currentStageScore = scoring.starThresholds[0];
                }
                if (scoring.starThresholds.Length >= 2 &&
                    GUILayout.Button($"2★ ({scoring.starThresholds[1]})"))
                {
                    currentStageScore = scoring.starThresholds[1];
                }
                if (scoring.starThresholds.Length >= 3 &&
                    GUILayout.Button($"3★ ({scoring.starThresholds[2]})"))
                {
                    currentStageScore = scoring.starThresholds[2];
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(3);

            currentStageScore = EditorGUILayout.IntField("강제 클리어 점수 (Score)", currentStageScore);
            if (currentStageScore < 0) currentStageScore = 0;

            currentStagePerfectStats = EditorGUILayout.Toggle("Perfect Clear 통계 (노데미지)", currentStagePerfectStats);

            EditorGUILayout.Space(5);

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Force Clear Current Stage", GUILayout.Height(32)))
            {
                ForceClearStage(currentStageId, currentStageScore, currentStagePerfectStats);
            }
            GUI.backgroundColor = prevColor;

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Manual Stage Section

        private void DrawManualStageSection()
        {
            EditorGUILayout.LabelField("Manual Stage Completion", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("스테이지 선택", EditorStyles.boldLabel);
            selectedStageAsset = (StageDataSO)EditorGUILayout.ObjectField(
                "Stage Asset",
                selectedStageAsset,
                typeof(StageDataSO),
                false);

            manualStageId = EditorGUILayout.TextField(
                new GUIContent("Stage ID (직접 입력)", "StageDataSO를 선택하지 않은 경우 사용할 스테이지 ID"),
                manualStageId);

            string resolvedStageId = null;
            StageDataSO resolvedStageData = null;

            if (selectedStageAsset != null)
            {
                resolvedStageId = selectedStageAsset.StageId;
                resolvedStageData = selectedStageAsset;
            }
            else if (!string.IsNullOrEmpty(manualStageId))
            {
                resolvedStageId = manualStageId;
                if (hasRuntimeManager)
                {
                    resolvedStageData = stageProgressManager.GetStageData(manualStageId);
                }
            }

            if (!string.IsNullOrEmpty(resolvedStageId))
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("선택된 스테이지 정보", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Stage ID", resolvedStageId);

                if (resolvedStageData != null)
                {
                    EditorGUILayout.LabelField("Name", resolvedStageData.DisplayName);
                    EditorGUILayout.LabelField("Chapter", resolvedStageData.ChapterId);
                    EditorGUILayout.LabelField("Stage Number", resolvedStageData.StageNumber.ToString());

                    if (hasRuntimeManager)
                    {
                        var record = stageProgressManager.GetStageRecord(resolvedStageId);
                        if (record != null)
                        {
                            EditorGUILayout.Space(3);
                            EditorGUILayout.LabelField("진행 상태", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField("State", record.state.ToString());
                            EditorGUILayout.LabelField("Best Score", record.bestScore.ToString());
                            EditorGUILayout.LabelField("Best Stars", record.bestStars.ToString());
                            EditorGUILayout.LabelField("Clear Count", record.ClearCount.ToString());
                        }
                    }

                    var scoring = resolvedStageData.Scoring;
                    if (scoring != null && scoring.starThresholds != null && scoring.starThresholds.Length > 0)
                    {
                        EditorGUILayout.Space(3);
                        EditorGUILayout.LabelField("별 임계값 (Score Thresholds)", EditorStyles.boldLabel);
                        string thresholdsText = string.Join(", ", scoring.starThresholds);
                        EditorGUILayout.LabelField($"Star Thresholds: {thresholdsText}");

                        EditorGUILayout.BeginHorizontal();
                        if (scoring.starThresholds.Length >= 1 &&
                            GUILayout.Button($"1★ ({scoring.starThresholds[0]})"))
                        {
                            manualStageScore = scoring.starThresholds[0];
                        }
                        if (scoring.starThresholds.Length >= 2 &&
                            GUILayout.Button($"2★ ({scoring.starThresholds[1]})"))
                        {
                            manualStageScore = scoring.starThresholds[1];
                        }
                        if (scoring.starThresholds.Length >= 3 &&
                            GUILayout.Button($"3★ ({scoring.starThresholds[2]})"))
                        {
                            manualStageScore = scoring.starThresholds[2];
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("StageDataSO를 찾을 수 없습니다. StageDataSO 에셋을 직접 선택하는 것을 권장합니다.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("StageDataSO를 선택하거나 Stage ID를 입력하면 해당 스테이지를 클리어 처리할 수 있습니다.", MessageType.Info);
            }

            EditorGUILayout.Space(5);

            manualStageScore = EditorGUILayout.IntField("강제 클리어 점수 (Score)", manualStageScore);
            if (manualStageScore < 0) manualStageScore = 0;

            manualStagePerfectStats = EditorGUILayout.Toggle("Perfect Clear 통계 (노데미지)", manualStagePerfectStats);

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(
                !Application.isPlaying ||
                !hasRuntimeManager ||
                string.IsNullOrEmpty(resolvedStageId) ||
                resolvedStageData == null);

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Force Clear Selected Stage", GUILayout.Height(32)))
            {
                ForceClearStage(resolvedStageId, manualStageScore, manualStagePerfectStats);
            }
            GUI.backgroundColor = prevColor;

            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("수동 스테이지 클리어는 Play 모드에서만 StageProgressManager를 통해 적용됩니다.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Core Logic

        private void ForceClearStage(string stageId, int score, bool perfectStats)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[StageProgressEditor] Play 모드가 아니므로 스테이지를 클리어할 수 없습니다.");
                EditorUtility.DisplayDialog("Stage Progress", "Play 모드에서만 스테이지를 클리어할 수 있습니다.", "OK");
                return;
            }

            if (!hasRuntimeManager || stageProgressManager == null)
            {
                Debug.LogWarning("[StageProgressEditor] StageProgressManager가 준비되지 않았습니다.");
                EditorUtility.DisplayDialog("Stage Progress", "StageProgressManager가 준비되지 않았습니다. ServiceBootstrap 초기화를 확인하세요.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(stageId))
            {
                Debug.LogWarning("[StageProgressEditor] 유효한 Stage ID가 아닙니다.");
                EditorUtility.DisplayDialog("Stage Progress", "유효한 Stage ID를 입력하거나 StageDataSO를 선택하세요.", "OK");
                return;
            }

            var stageData = stageProgressManager.GetStageData(stageId);
            if (stageData == null)
            {
                Debug.LogError($"[StageProgressEditor] StageDataSO를 찾을 수 없습니다: {stageId}");
                EditorUtility.DisplayDialog("Stage Progress", $"StageDataSO를 찾을 수 없습니다: {stageId}", "OK");
                return;
            }

            if (score < 0) score = 0;

            var statistics = new Dictionary<string, int>();

            // 플레이 시간 기본값 (초) - 필요 시 변경 가능
            statistics["play_time"] = 60;

            if (perfectStats)
            {
                // 노데미지 통계로 기록 (퍼펙트 클리어 카운트 증가용)
                statistics["damage_taken"] = 0;
            }

            stageProgressManager.RecordStageCompletion(stageId, score, statistics);

            // autoSave 설정과 무관하게 강제 저장
            stageProgressManager.SaveProgress();

            int stars = stageData.CalculateStars(score);

            Debug.Log($"[StageProgressEditor] Stage '{stageId}' 강제 클리어 적용 - Score: {score}, Stars: {stars}");
            EditorUtility.DisplayDialog(
                "Stage Cleared",
                $"Stage '{stageId}' 클리어 처리 완료\nScore: {score}\nStars (calculated): {stars}",
                "OK");
        }

        #endregion
    }
}

