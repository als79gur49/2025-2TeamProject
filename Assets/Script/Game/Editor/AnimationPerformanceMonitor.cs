using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Game.Components;

namespace Game.Editor
{
    /// <summary>
    /// 애니메이션 성능 모니터링 에디터 창
    /// 실시간 성능 추적, 통계, 프로파일링
    /// </summary>
    public class AnimationPerformanceMonitor : EditorWindow
    {
        #region Performance Data

        private class AnimationStats
        {
            public string unitName;
            public string animationType;
            public float duration;
            public float startTime;
            public float endTime;
            public int frameDrops;
        }

        private static List<AnimationStats> recordedAnimations = new List<AnimationStats>();
        private static Dictionary<string, float> activeAnimations = new Dictionary<string, float>();
        private static bool isMonitoring = false;
        private static float lastFrameTime;
        private static int totalFrameDrops = 0;

        #endregion

        #region UI State

        private Vector2 scrollPosition;
        private bool showDetailedStats = true;
        private bool showFrameDrops = true;
        private int maxRecords = 100;

        #endregion

        [MenuItem("Tools/Animation/Performance Monitor")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationPerformanceMonitor>("Animation Performance");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            StartMonitoring();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopMonitoring();
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || !isMonitoring)
                return;

            // 프레임 드롭 감지
            float currentFrameTime = Time.realtimeSinceStartup;
            float deltaTime = currentFrameTime - lastFrameTime;

            if (deltaTime > 1f / 30f) // 30 FPS 이하면 프레임 드롭
            {
                totalFrameDrops++;
            }

            lastFrameTime = currentFrameTime;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawControls();
            DrawStatistics();
            DrawRecordedAnimations();
        }

        #region GUI Drawing

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Animation Performance Monitor", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to start monitoring", MessageType.Info);
                return;
            }

            string status = isMonitoring ? "MONITORING" : "PAUSED";
            Color statusColor = isMonitoring ? Color.green : Color.yellow;

            var prevColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"Status: {status}", EditorStyles.boldLabel);
            GUI.color = prevColor;

            EditorGUILayout.Space(10);
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(isMonitoring ? "Stop Monitoring" : "Start Monitoring"))
            {
                if (isMonitoring)
                    StopMonitoring();
                else
                    StartMonitoring();
            }

            if (GUILayout.Button("Clear Records"))
            {
                recordedAnimations.Clear();
                totalFrameDrops = 0;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            showDetailedStats = EditorGUILayout.Toggle("Show Detailed Stats", showDetailedStats);
            showFrameDrops = EditorGUILayout.Toggle("Show Frame Drops", showFrameDrops);
            EditorGUILayout.EndHorizontal();

            maxRecords = EditorGUILayout.IntSlider("Max Records", maxRecords, 10, 500);

            EditorGUILayout.Space(10);
        }

        private void DrawStatistics()
        {
            if (recordedAnimations.Count == 0)
            {
                EditorGUILayout.HelpBox("No animation data recorded yet", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Performance Statistics", EditorStyles.boldLabel);

            // 전체 통계
            int totalAnimations = recordedAnimations.Count;
            float avgDuration = recordedAnimations.Average(a => a.duration);
            float maxDuration = recordedAnimations.Max(a => a.duration);
            float minDuration = recordedAnimations.Min(a => a.duration);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Total Animations: {totalAnimations}");
            EditorGUILayout.LabelField($"Avg Duration: {avgDuration:F3}s");
            EditorGUILayout.LabelField($"Max Duration: {maxDuration:F3}s");
            EditorGUILayout.LabelField($"Min Duration: {minDuration:F3}s");

            if (showFrameDrops)
            {
                EditorGUILayout.LabelField($"Total Frame Drops: {totalFrameDrops}");
            }
            EditorGUILayout.EndVertical();

            // 애니메이션 타입별 통계
            if (showDetailedStats)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("By Animation Type:", EditorStyles.boldLabel);

                var groupedByType = recordedAnimations.GroupBy(a => a.animationType);
                foreach (var group in groupedByType)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{group.Key}:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"  Count: {group.Count()}");
                    EditorGUILayout.LabelField($"  Avg: {group.Average(a => a.duration):F3}s");
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space(10);
        }

        private void DrawRecordedAnimations()
        {
            EditorGUILayout.LabelField("Recent Animations", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var anim in recordedAnimations.TakeLast(50).Reverse())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.LabelField(anim.unitName, GUILayout.Width(100));
                EditorGUILayout.LabelField(anim.animationType, GUILayout.Width(80));
                EditorGUILayout.LabelField($"{anim.duration:F3}s", GUILayout.Width(60));

                if (showFrameDrops && anim.frameDrops > 0)
                {
                    var prevColor = GUI.color;
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField($"Drops: {anim.frameDrops}", GUILayout.Width(80));
                    GUI.color = prevColor;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Monitoring Control

        private static void StartMonitoring()
        {
            if (isMonitoring) return;

            isMonitoring = true;
            lastFrameTime = Time.realtimeSinceStartup;

            // BlendTree 기반 UnitAnimationController 이벤트 구독
            var controllers = FindObjectsOfType<UnitAnimationController>();
            foreach (var controller in controllers)
            {
                controller.OnMoveStart += OnMoveStart;
                controller.OnMoveEnd += OnMoveEnd;
                controller.OnAttackStart += OnAttackStart;
                controller.OnAttackEnd += OnAttackEnd;
            }

            Debug.Log("[AnimationPerformanceMonitor] Monitoring started");
        }

        private static void StopMonitoring()
        {
            if (!isMonitoring) return;

            isMonitoring = false;

            // 이벤트 구독 해제
            var controllers = FindObjectsOfType<UnitAnimationController>();
            foreach (var controller in controllers)
            {
                controller.OnMoveStart -= OnMoveStart;
                controller.OnMoveEnd -= OnMoveEnd;
                controller.OnAttackStart -= OnAttackStart;
                controller.OnAttackEnd -= OnAttackEnd;
            }

            Debug.Log("[AnimationPerformanceMonitor] Monitoring stopped");
        }

        private static void OnMoveStart(Vector2Int from, Vector2Int to)
        {
            if (!isMonitoring) return;

            string key = $"Move_{Time.frameCount}";
            activeAnimations[key] = Time.realtimeSinceStartup;
        }

        private static void OnMoveEnd(Vector2Int targetPos)
        {
            if (!isMonitoring) return;

            string key = $"Move_{Time.frameCount}";
            if (activeAnimations.TryGetValue(key, out float startTime))
            {
                float endTime = Time.realtimeSinceStartup;
                float duration = endTime - startTime;

                // 임시 frame drops 계산 (실제로는 더 정교한 로직 필요)
                int frameDrops = duration > 0.1f ? 1 : 0;

                // 기록 제한
                if (recordedAnimations.Count >= 100)
                {
                    recordedAnimations.RemoveAt(0);
                }

                recordedAnimations.Add(new AnimationStats
                {
                    unitName = "Unit",
                    animationType = "Move",
                    duration = duration,
                    startTime = startTime,
                    endTime = endTime,
                    frameDrops = frameDrops
                });

                activeAnimations.Remove(key);
            }
        }

        private static void OnAttackStart()
        {
            if (!isMonitoring) return;

            string key = $"Attack_{Time.frameCount}";
            activeAnimations[key] = Time.realtimeSinceStartup;
        }

        private static void OnAttackEnd()
        {
            if (!isMonitoring) return;

            string key = $"Attack_{Time.frameCount}";
            if (activeAnimations.TryGetValue(key, out float startTime))
            {
                float endTime = Time.realtimeSinceStartup;
                float duration = endTime - startTime;

                int frameDrops = duration > 0.1f ? 1 : 0;

                if (recordedAnimations.Count >= 100)
                {
                    recordedAnimations.RemoveAt(0);
                }

                recordedAnimations.Add(new AnimationStats
                {
                    unitName = "Unit",
                    animationType = "Attack",
                    duration = duration,
                    startTime = startTime,
                    endTime = endTime,
                    frameDrops = frameDrops
                });

                activeAnimations.Remove(key);
            }
        }

        #endregion

        #region Context Menu Actions

        [MenuItem("Tools/Animation/Reset Performance Data")]
        private static void ResetPerformanceData()
        {
            recordedAnimations.Clear();
            activeAnimations.Clear();
            totalFrameDrops = 0;
            Debug.Log("[AnimationPerformanceMonitor] Performance data reset");
        }

        [MenuItem("Tools/Animation/Export Performance Report")]
        private static void ExportPerformanceReport()
        {
            if (recordedAnimations.Count == 0)
            {
                EditorUtility.DisplayDialog("No Data", "No animation data to export", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Performance Report", "", "animation_performance.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var lines = new List<string>
            {
                "Unit Name,Animation Type,Duration,Frame Drops,Start Time,End Time"
            };

            foreach (var anim in recordedAnimations)
            {
                lines.Add($"{anim.unitName},{anim.animationType},{anim.duration},{anim.frameDrops},{anim.startTime},{anim.endTime}");
            }

            System.IO.File.WriteAllLines(path, lines);
            Debug.Log($"[AnimationPerformanceMonitor] Report exported to: {path}");
            EditorUtility.DisplayDialog("Export Complete", $"Report saved to:\n{path}", "OK");
        }

        #endregion
    }
}
