using UnityEngine;
using UnityEditor;
using Game.Services;
using Game.Interfaces;
using Game.Core;
using System.Collections.Generic;
using System.Reflection;

namespace Game.Editor
{
    /// <summary>
    /// ResourceManager 에디터 창
    /// Play Mode에서 자원 관리 시스템 테스트 및 디버깅
    /// </summary>
    public class ResourceManagerEditor : EditorWindow
    {
        #region 창 상태

        private Vector2 scrollPosition;
        private bool autoRefresh = true;
        private float refreshRate = 0.1f;
        private double lastRefreshTime;

        #endregion

        #region 서비스 캐시

        private IResourceManager resourceManager;

        #endregion

        #region 테스트 상태

        private int customManaAmount = 5;
        private bool showHistory = false;
        private List<string> operationHistory = new List<string>();
        private const int maxHistoryCount = 20;

        #endregion

        #region 프리셋 정의

        private enum ResourcePreset
        {
            EarlyGame,          // 초반전: 낮은 자원
            MidGame,            // 중반전: 중간 자원
            LateGame,           // 후반전: 최대 자원
            PlayerAdvantage,    // 플레이어 우세
            EnemyAdvantage,     // 적군 우세
            ResourceStarvation  // 자원 부족 상황
        }

        #endregion

        #region Unity Lifecycle

        [MenuItem("Tools/Resource Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<ResourceManagerEditor>("Resource Manager");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private void Update()
        {
            // 자동 갱신이 비활성화되었거나 Play Mode가 아니면 무시
            if (!autoRefresh || !Application.isPlaying)
                return;

            // 갱신 주기 체크
            if (EditorApplication.timeSinceStartup - lastRefreshTime > refreshRate)
            {
                Repaint();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Play Mode가 아닌 경우
            if (!Application.isPlaying)
            {
                DrawNotPlayingMessage();
                EditorGUILayout.EndScrollView();
                return;
            }

            // 서비스 초기화 확인
            var manager = GetResourceManager();
            if (manager == null || !manager.IsInitialized)
            {
                DrawNotInitializedMessage();
                EditorGUILayout.EndScrollView();
                return;
            }

            // 정상 동작: GUI 그리기
            DrawResourceManagerGUI(manager);

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region 서비스 접근

        /// <summary>
        /// ResourceManager 서비스를 가져옵니다. Play Mode가 아니거나 초기화되지 않으면 null 반환
        /// </summary>
        private IResourceManager GetResourceManager()
        {
            // Play Mode 체크
            if (!Application.isPlaying)
                return null;

            // 캐시된 참조가 없거나 서비스가 등록되지 않은 경우 갱신
            if (resourceManager == null || !ServiceLocator.IsRegistered<IResourceManager>())
            {
                resourceManager = ServiceLocator.Get<IResourceManager>();
            }

            return resourceManager;
        }

        #endregion

        #region GUI 그리기 - 메시지

        private void DrawNotPlayingMessage()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "Resource Manager는 Play Mode에서만 사용할 수 있습니다.\n" +
                "Play 버튼(▶)을 눌러 게임을 실행하세요.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Play Mode로 전환", GUILayout.Height(30), GUILayout.Width(200)))
            {
                EditorApplication.isPlaying = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawNotInitializedMessage()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "Resource Manager가 초기화되지 않았습니다.\n" +
                "씬에 GameInitializer가 실행되었는지 확인하세요.",
                MessageType.Warning
            );

            EditorGUILayout.Space(10);

            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorGUILayout.LabelField("현재 씬:", currentScene.name);
            EditorGUILayout.LabelField("ServiceLocator 등록:", ServiceLocator.IsRegistered<IResourceManager>() ? "예" : "아니오");
        }

        #endregion

        #region GUI 그리기 - 메인

        private void DrawResourceManagerGUI(IResourceManager manager)
        {
            EditorGUILayout.Space(10);

            // 헤더 섹션
            DrawHeaderSection();

            EditorGUILayout.Space(10);

            // 현재 상태 섹션
            DrawCurrentStateSection(manager);

            EditorGUILayout.Space(10);

            // 빠른 조작 섹션
            DrawQuickOperationsSection(manager);

            EditorGUILayout.Space(10);

            // 고급 조작 섹션
            DrawAdvancedOperationsSection(manager);

            EditorGUILayout.Space(10);

            // 테스트 프리셋 섹션
            DrawPresetsSection();

            EditorGUILayout.Space(10);

            // 작업 히스토리 섹션
            if (showHistory)
            {
                DrawHistorySection();
            }
        }

        #endregion

        #region GUI 그리기 - 헤더

        private void DrawHeaderSection()
        {
            EditorGUILayout.LabelField("Resource Manager 디버거", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 상태 표시
            var manager = GetResourceManager();
            string statusText = manager != null && manager.IsInitialized ? "✅ 초기화 완료" : "❌ 초기화 안 됨";
            EditorGUILayout.LabelField("서비스 상태:", statusText);

            // 자동 갱신 토글
            EditorGUILayout.BeginHorizontal();
            autoRefresh = EditorGUILayout.Toggle("자동 갱신", autoRefresh, GUILayout.Width(200));
            if (autoRefresh)
            {
                EditorGUILayout.LabelField($"({refreshRate:F1}초마다)", GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region GUI 그리기 - 현재 상태

        private void DrawCurrentStateSection(IResourceManager manager)
        {
            EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 플레이어 자원
            EditorGUILayout.LabelField("🔵 플레이어 자원", EditorStyles.boldLabel);
            DrawResourceBar("마나", manager.PlayerMana, manager.PlayerMaxMana, new Color(0.3f, 0.6f, 1.0f));
            EditorGUILayout.LabelField($"상한: {manager.PlayerMaxMana}/10");

            EditorGUILayout.Space(5);

            // 적군 자원
            EditorGUILayout.LabelField("🔴 적군 자원", EditorStyles.boldLabel);
            DrawResourceBar("마나", manager.EnemyMana, manager.EnemyMaxMana, new Color(1.0f, 0.3f, 0.3f));
            EditorGUILayout.LabelField($"상한: {manager.EnemyMaxMana}/10");

            EditorGUILayout.EndVertical();
        }

        private void DrawResourceBar(string label, int current, int max, Color barColor)
        {
            EditorGUILayout.BeginHorizontal();

            // 라벨
            EditorGUILayout.LabelField(label, GUILayout.Width(50));

            // 진행 바 렉트 계산
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField",
                GUILayout.ExpandWidth(true), GUILayout.Height(18));

            // 진행 바 그리기
            float ratio = max > 0 ? (float)current / max : 0f;
            EditorGUI.ProgressBar(rect, ratio, $"{current}/{max}");

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region GUI 그리기 - 빠른 조작

        private void DrawQuickOperationsSection(IResourceManager manager)
        {
            EditorGUILayout.LabelField("빠른 조작", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 플레이어 조작
            EditorGUILayout.LabelField("플레이어:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+1 마나"))
            {
                manager.RestorePlayerResources(1);
                LogOperation("플레이어 +1 마나");
            }

            EditorGUILayout.LabelField("사용자 정의:", GUILayout.Width(80));
            customManaAmount = EditorGUILayout.IntField(customManaAmount, GUILayout.Width(50));
            if (GUILayout.Button($"+{customManaAmount}"))
            {
                manager.RestorePlayerResources(customManaAmount);
                LogOperation($"플레이어 +{customManaAmount} 마나");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("최대 채우기"))
            {
                manager.RefillPlayerResources();
                LogOperation("플레이어 마나 최대 채우기");
            }

            if (GUILayout.Button("0으로 설정"))
            {
                SetResourceValues(0, manager.PlayerMaxMana, manager.EnemyMana, manager.EnemyMaxMana);
                LogOperation("플레이어 마나를 0으로 설정");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 적군 조작
            EditorGUILayout.LabelField("적군:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+1 마나"))
            {
                manager.RestoreEnemyResources(1);
                LogOperation("적군 +1 마나");
            }

            EditorGUILayout.LabelField("사용자 정의:", GUILayout.Width(80));
            if (GUILayout.Button($"+{customManaAmount}"))
            {
                manager.RestoreEnemyResources(customManaAmount);
                LogOperation($"적군 +{customManaAmount} 마나");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("최대 채우기"))
            {
                manager.RefillEnemyResources();
                LogOperation("적군 마나 최대 채우기");
            }

            if (GUILayout.Button("0으로 설정"))
            {
                SetResourceValues(manager.PlayerMana, manager.PlayerMaxMana, 0, manager.EnemyMaxMana);
                LogOperation("적군 마나를 0으로 설정");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region GUI 그리기 - 고급 조작

        private void DrawAdvancedOperationsSection(IResourceManager manager)
        {
            EditorGUILayout.LabelField("고급 조작", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("모두 리셋"))
            {
                manager.ResetResources();
                LogOperation("모든 자원 리셋");
            }

            if (GUILayout.Button("턴 마나 증가"))
            {
                manager.IncreaseTurnlyMana();
                LogOperation("턴 마나 증가 (최대 마나 +1, 현재 마나 = 최대)");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region GUI 그리기 - 프리셋

        private void DrawPresetsSection()
        {
            EditorGUILayout.LabelField("테스트 프리셋", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("게임 단계:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("초반전"))
            {
                ApplyPreset(ResourcePreset.EarlyGame);
            }

            if (GUILayout.Button("중반전"))
            {
                ApplyPreset(ResourcePreset.MidGame);
            }

            if (GUILayout.Button("후반전"))
            {
                ApplyPreset(ResourcePreset.LateGame);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("밸런스 테스트:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("플레이어 우세"))
            {
                ApplyPreset(ResourcePreset.PlayerAdvantage);
            }

            if (GUILayout.Button("적군 우세"))
            {
                ApplyPreset(ResourcePreset.EnemyAdvantage);
            }

            if (GUILayout.Button("자원 부족"))
            {
                ApplyPreset(ResourcePreset.ResourceStarvation);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region GUI 그리기 - 히스토리

        private void DrawHistorySection()
        {
            EditorGUILayout.LabelField("작업 히스토리", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (operationHistory.Count == 0)
            {
                EditorGUILayout.LabelField("(작업 기록 없음)");
            }
            else
            {
                for (int i = operationHistory.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.LabelField(operationHistory[i], EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("히스토리 지우기"))
            {
                operationHistory.Clear();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region 프리셋 적용

        private void ApplyPreset(ResourcePreset preset)
        {
            switch (preset)
            {
                case ResourcePreset.EarlyGame:
                    SetResourceValues(3, 5, 3, 5);
                    LogOperation("프리셋 적용: 초반전 (3/5, 3/5)");
                    break;

                case ResourcePreset.MidGame:
                    SetResourceValues(7, 10, 7, 10);
                    LogOperation("프리셋 적용: 중반전 (7/10, 7/10)");
                    break;

                case ResourcePreset.LateGame:
                    SetResourceValues(10, 10, 10, 10);
                    LogOperation("프리셋 적용: 후반전 (10/10, 10/10)");
                    break;

                case ResourcePreset.PlayerAdvantage:
                    SetResourceValues(10, 10, 3, 10);
                    LogOperation("프리셋 적용: 플레이어 우세 (10/10, 3/10)");
                    break;

                case ResourcePreset.EnemyAdvantage:
                    SetResourceValues(3, 10, 10, 10);
                    LogOperation("프리셋 적용: 적군 우세 (3/10, 10/10)");
                    break;

                case ResourcePreset.ResourceStarvation:
                    SetResourceValues(1, 5, 1, 5);
                    LogOperation("프리셋 적용: 자원 부족 (1/5, 1/5)");
                    break;
            }
        }

        #endregion

        #region 자원 값 설정 (리플렉션)

        /// <summary>
        /// 리플렉션을 사용하여 ResourceManager의 private 필드에 직접 접근하여 값 설정
        /// </summary>
        private void SetResourceValues(int playerCurrent, int playerMax, int enemyCurrent, int enemyMax)
        {
            var manager = GetResourceManager();
            if (manager == null) return;

            var type = manager.GetType();

            // 플레이어 마나 설정
            SetField(type, manager, "playerMana", playerCurrent);
            SetField(type, manager, "playerMaxMana", playerMax);

            // 적군 마나 설정
            SetField(type, manager, "enemyMana", enemyCurrent);
            SetField(type, manager, "enemyMaxMana", enemyMax);

            // 이벤트 발생 (UI 업데이트)
            InvokeMethod(type, manager, "NotifyResourceChanged", new object[] { true });
            InvokeMethod(type, manager, "NotifyResourceChanged", new object[] { false });
        }

        private void SetField(System.Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(instance, value);
            }
            else
            {
                Debug.LogError($"[ResourceManagerEditor] 필드를 찾을 수 없음: {fieldName}");
            }
        }

        private void InvokeMethod(System.Type type, object instance, string methodName, object[] parameters)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(instance, parameters);
            }
            else
            {
                Debug.LogError($"[ResourceManagerEditor] 메서드를 찾을 수 없음: {methodName}");
            }
        }

        #endregion

        #region 히스토리 로깅

        private void LogOperation(string operation)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {operation}";

            operationHistory.Add(logEntry);

            // 최대 개수 유지
            if (operationHistory.Count > maxHistoryCount)
            {
                operationHistory.RemoveAt(0);
            }

            // 히스토리가 비활성화되어 있으면 자동으로 활성화
            if (!showHistory && operationHistory.Count > 0)
            {
                showHistory = true;
            }
        }

        #endregion
    }
}
