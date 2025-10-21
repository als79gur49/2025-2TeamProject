#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Game.SceneManagement.Editor
{
    /// <summary>
    /// SceneData의 커스텀 인스펙터
    ///
    /// 기능:
    /// 1. 씬 이름 드롭다운 (Build Settings 기반)
    /// 2. 실시간 유효성 검사
    /// 3. 씬 파일 빠른 열기 버튼
    /// 4. 경고 메시지 표시
    /// 5. Build Settings 동기화 버튼
    /// </summary>
    [CustomEditor(typeof(SceneData))]
    public class SceneDataEditor : UnityEditor.Editor
    {
        private SceneData _sceneData;
        private SerializedProperty _sceneNameProp;
        private SerializedProperty _categoryProp;
        private SerializedProperty _loadingBackgroundProp;
        private SerializedProperty _loadingTipsProp;
        private SerializedProperty _bgMusicProp;
        private SerializedProperty _descriptionProp;

        private List<string> _availableScenes = new List<string>();
        private int _selectedSceneIndex = -1;

        private void OnEnable()
        {
            _sceneData = (SceneData)target;

            // SerializedProperty 바인딩
            _sceneNameProp = serializedObject.FindProperty("sceneName");
            _categoryProp = serializedObject.FindProperty("category");
            _loadingBackgroundProp = serializedObject.FindProperty("loadingBackground");
            _loadingTipsProp = serializedObject.FindProperty("loadingTips");
            _bgMusicProp = serializedObject.FindProperty("bgMusic");
            _descriptionProp = serializedObject.FindProperty("description");

            RefreshAvailableScenes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawBasicInfo();
            EditorGUILayout.Space(10);

            DrawLoadingScreenSettings();
            EditorGUILayout.Space(10);

            DrawAudioSettings();
            EditorGUILayout.Space(10);

            DrawMetadata();
            EditorGUILayout.Space(10);

            DrawValidationInfo();
            EditorGUILayout.Space(10);

            DrawUtilityButtons();

            serializedObject.ApplyModifiedProperties();
        }

        #region Drawing Sections

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("씬 데이터 설정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ScriptableObject 기반 씬 관리 시스템입니다.\n" +
                "GUID 기반 참조로 씬 파일명 변경에 안전하며, 추가 메타데이터를 지원합니다.",
                MessageType.Info
            );
        }

        private void DrawBasicInfo()
        {
            EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);

            // 씬 이름 드롭다운
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("씬 이름");

            RefreshAvailableScenes();
            UpdateSelectedSceneIndex();

            int newIndex = EditorGUILayout.Popup(_selectedSceneIndex, _availableScenes.ToArray());

            if (newIndex != _selectedSceneIndex && newIndex >= 0 && newIndex < _availableScenes.Count)
            {
                _selectedSceneIndex = newIndex;
                _sceneNameProp.stringValue = _availableScenes[newIndex];
                serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("새로고침", GUILayout.Width(70)))
            {
                RefreshAvailableScenes();
            }

            EditorGUILayout.EndHorizontal();

            // 씬 이름 직접 입력 (고급 사용자용)
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_sceneNameProp, new GUIContent("씬 이름 (수동 입력)"));
            EditorGUI.indentLevel--;

            // 카테고리
            EditorGUILayout.PropertyField(_categoryProp, new GUIContent("카테고리"));
        }

        private void DrawLoadingScreenSettings()
        {
            EditorGUILayout.LabelField("로딩 화면 설정", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_loadingBackgroundProp, new GUIContent("배경 이미지"));
            EditorGUILayout.PropertyField(_loadingTipsProp, new GUIContent("로딩 팁"), true);

            if (_loadingTipsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("로딩 팁이 없습니다. 기본 메시지가 표시됩니다.", MessageType.Info);
            }
        }

        private void DrawAudioSettings()
        {
            EditorGUILayout.LabelField("오디오 설정", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_bgMusicProp, new GUIContent("배경음악 (AudioData SO)"));

            if (_bgMusicProp.objectReferenceValue != null)
            {
                var bgMusic = _bgMusicProp.objectReferenceValue;
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("참조됨:", bgMusic.name, EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawMetadata()
        {
            EditorGUILayout.LabelField("메타데이터", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_descriptionProp, new GUIContent("설명 (에디터용)"));
        }

        private void DrawValidationInfo()
        {
            EditorGUILayout.LabelField("유효성 검사", EditorStyles.boldLabel);

            string sceneName = _sceneNameProp.stringValue;

            // 씬 이름 비어있음
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                EditorGUILayout.HelpBox("⚠ 씬 이름이 비어있습니다!", MessageType.Error);
                return;
            }

            // Build Settings 확인
            bool inBuildSettings = _sceneData.IsSceneInBuildSettings();
            if (!inBuildSettings)
            {
                EditorGUILayout.HelpBox(
                    $"⚠ 씬 '{sceneName}'이(가) Build Settings에 없거나 비활성화되어 있습니다!\n" +
                    "아래 'Build Settings에 추가' 버튼을 사용하세요.",
                    MessageType.Warning
                );
            }
            else
            {
                EditorGUILayout.HelpBox($"✓ 씬이 Build Settings에 있습니다.", MessageType.Info);
            }

            // 씬 파일 존재 확인
            bool sceneExists = _sceneData.DoesSceneExist();
            if (!sceneExists)
            {
                EditorGUILayout.HelpBox(
                    $"⚠ 씬 파일 '{sceneName}'을(를) 프로젝트에서 찾을 수 없습니다!",
                    MessageType.Error
                );
            }
            else
            {
                EditorGUILayout.HelpBox($"✓ 씬 파일이 존재합니다.", MessageType.Info);
            }
        }

        private void DrawUtilityButtons()
        {
            EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // 씬 열기 버튼
            GUI.enabled = _sceneData.DoesSceneExist();
            if (GUILayout.Button("씬 열기"))
            {
                OpenSceneInEditor();
            }
            GUI.enabled = true;

            // Build Settings에 추가 버튼
            GUI.enabled = !_sceneData.IsSceneInBuildSettings() && _sceneData.DoesSceneExist();
            if (GUILayout.Button("Build Settings에 추가"))
            {
                AddSceneToBuildSettings();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // Build Settings 열기 버튼
            if (GUILayout.Button("Build Settings 열기"))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Build Settings에 있는 모든 씬 목록 새로고침
        /// </summary>
        private void RefreshAvailableScenes()
        {
            _availableScenes.Clear();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    string sceneName = Path.GetFileNameWithoutExtension(scene.path);
                    _availableScenes.Add(sceneName);
                }
            }

            if (_availableScenes.Count == 0)
            {
                _availableScenes.Add("(Build Settings에 씬이 없음)");
            }
        }

        /// <summary>
        /// 현재 선택된 씬 인덱스 업데이트
        /// </summary>
        private void UpdateSelectedSceneIndex()
        {
            string currentSceneName = _sceneNameProp.stringValue;

            if (string.IsNullOrWhiteSpace(currentSceneName))
            {
                _selectedSceneIndex = -1;
                return;
            }

            _selectedSceneIndex = _availableScenes.IndexOf(currentSceneName);

            if (_selectedSceneIndex < 0 && _availableScenes.Count > 0)
            {
                _selectedSceneIndex = 0; // 기본값
            }
        }

        /// <summary>
        /// 씬을 에디터에서 엽니다
        /// </summary>
        private void OpenSceneInEditor()
        {
            string sceneName = _sceneNameProp.stringValue;
            string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("오류", $"씬 '{sceneName}'을(를) 찾을 수 없습니다.", "확인");
                return;
            }

            string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);

            if (EditorUtility.DisplayDialog(
                "씬 열기",
                $"씬 '{sceneName}'을(를) 열시겠습니까?\n현재 씬의 변경사항은 저장됩니다.",
                "열기",
                "취소"))
            {
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }
            }
        }

        /// <summary>
        /// Build Settings에 씬을 추가합니다
        /// </summary>
        private void AddSceneToBuildSettings()
        {
            string sceneName = _sceneNameProp.stringValue;
            string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("오류", $"씬 '{sceneName}'을(를) 찾을 수 없습니다.", "확인");
                return;
            }

            string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);

            // 기존 Build Settings 씬 목록 가져오기
            List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.scenes.ToList();

            // 이미 있는지 확인
            bool alreadyExists = buildScenes.Any(s => s.path == scenePath);

            if (alreadyExists)
            {
                EditorUtility.DisplayDialog("정보", $"씬 '{sceneName}'은(는) 이미 Build Settings에 있습니다.", "확인");
                return;
            }

            // 새 씬 추가
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            Debug.Log($"[SceneDataEditor] Scene '{sceneName}' added to Build Settings");
            EditorUtility.DisplayDialog("성공", $"씬 '{sceneName}'이(가) Build Settings에 추가되었습니다.", "확인");

            // 씬 목록 새로고침
            RefreshAvailableScenes();
        }

        #endregion
    }
}
#endif
