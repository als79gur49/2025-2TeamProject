using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Core;
using Game.Managers;
using Game.SaveSystem;
using Newtonsoft.Json;

namespace Game.Editor
{
    /// <summary>
    /// 플레이어 골드 에디터 창
    /// 런타임 PlayerDataManager 및 저장 파일(PlayerData) 모두에 골드 변경을 반영
    /// </summary>
    public class PlayerGoldEditor : EditorWindow
    {
        private const string PLAYER_DATA_DIRECTORY_NAME = "SaveData";
        private const string PLAYER_DATA_FILE_NAME = "player_data.json";

        // Runtime state
        private IPlayerDataManager playerDataManager;
        private ISaveDataAdapter saveDataAdapter;
        private bool hasRuntimeData;
        private int runtimeGold;
        private string runtimeStatusMessage;

        // Saved file state
        private string saveFilePath;
        private bool hasSaveFile;
        private int savedGold;
        private string saveFileStatusMessage;

        // Editing values
        private int targetGold;
        private int deltaGold = 100;

        [MenuItem("Tools/Player/Gold Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<PlayerGoldEditor>("Player Gold Editor");
            window.minSize = new Vector2(420, 360);
        }

        private void OnEnable()
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, PLAYER_DATA_DIRECTORY_NAME, PLAYER_DATA_FILE_NAME);

            RefreshRuntimeState();
            RefreshSavedState();
            InitializeTargetGold();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Player Gold Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            DrawRuntimeSection();
            EditorGUILayout.Space(10);

            DrawSavedFileSection();
            EditorGUILayout.Space(10);

            DrawEditSection();
            EditorGUILayout.Space(10);

            DrawActionsSection();
        }

        #region GUI Sections

        private void DrawRuntimeSection()
        {
            EditorGUILayout.LabelField("Runtime (Play Mode)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play 모드가 아니므로 런타임 데이터를 사용할 수 없습니다.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            RefreshRuntimeState();

            if (hasRuntimeData)
            {
                EditorGUILayout.LabelField("현재 런타임 골드", runtimeGold.ToString());
            }
            else
            {
                EditorGUILayout.HelpBox(runtimeStatusMessage, MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSavedFileSection()
        {
            EditorGUILayout.LabelField("Saved Player Data (player_data.json)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Save File Path:");
            EditorGUILayout.SelectableLabel(saveFilePath, EditorStyles.textField, GUILayout.Height(18));

            RefreshSavedState();

            if (hasSaveFile)
            {
                EditorGUILayout.LabelField("저장 파일 골드", savedGold.ToString());
            }
            else
            {
                EditorGUILayout.HelpBox(saveFileStatusMessage, MessageType.Info);
            }

            if (GUILayout.Button("Refresh"))
            {
                RefreshRuntimeState();
                RefreshSavedState();
                InitializeTargetGold();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEditSection()
        {
            EditorGUILayout.LabelField("Edit Gold", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            targetGold = EditorGUILayout.IntField("Target Gold", targetGold);
            if (targetGold < 0) targetGold = 0;

            deltaGold = EditorGUILayout.IntField("Delta (+/-)", deltaGold);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+100")) targetGold += 100;
            if (GUILayout.Button("+1,000")) targetGold += 1000;
            if (GUILayout.Button("+10,000")) targetGold += 10000;
            if (GUILayout.Button("Set 0")) targetGold = 0;
            EditorGUILayout.EndHorizontal();

            if (deltaGold != 0)
            {
                EditorGUILayout.HelpBox($"Delta 적용 시: {targetGold} → {targetGold + deltaGold}", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !hasRuntimeData);
            if (GUILayout.Button("Apply To Runtime (Play Mode Only)", GUILayout.Height(30)))
            {
                int value = Mathf.Max(0, targetGold + deltaGold);
                ApplyToRuntime(value);
                InitializeTargetGold();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Apply To Save File", GUILayout.Height(30)))
            {
                int value = Mathf.Max(0, targetGold + deltaGold);
                ApplyToSaveFile(value, useRuntimePipelineIfAvailable: true);
                InitializeTargetGold();
            }

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !hasRuntimeData);
            if (GUILayout.Button("Apply To Both (Runtime + Save)", GUILayout.Height(30)))
            {
                int value = Mathf.Max(0, targetGold + deltaGold);
                ApplyToRuntime(value);
                ApplyToSaveFile(value, useRuntimePipelineIfAvailable: true);
                InitializeTargetGold();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Runtime & Save State

        private void RefreshRuntimeState()
        {
            hasRuntimeData = false;
            runtimeStatusMessage = string.Empty;
            runtimeGold = 0;
            playerDataManager = null;
            saveDataAdapter = null;

            if (!Application.isPlaying)
            {
                runtimeStatusMessage = "Play 모드가 아닙니다.";
                return;
            }

            if (!ServiceLocator.IsRegistered<IPlayerDataManager>())
            {
                runtimeStatusMessage = "ServiceLocator에 IPlayerDataManager가 등록되지 않았습니다.";
                return;
            }

            playerDataManager = ServiceLocator.Get<IPlayerDataManager>();
            if (playerDataManager == null)
            {
                runtimeStatusMessage = "PlayerDataManager 인스턴스를 가져오지 못했습니다.";
                return;
            }

            hasRuntimeData = true;
            runtimeGold = playerDataManager.CurrentGold;

            if (ServiceLocator.IsRegistered<ISaveDataAdapter>())
            {
                saveDataAdapter = ServiceLocator.Get<ISaveDataAdapter>();
            }
        }

        private void RefreshSavedState()
        {
            hasSaveFile = false;
            savedGold = 0;
            saveFileStatusMessage = string.Empty;

            try
            {
                if (!File.Exists(saveFilePath))
                {
                    saveFileStatusMessage = "player_data.json 파일이 아직 없습니다. 한 번 세이브 후 생성됩니다.";
                    return;
                }

                var data = LoadPlayerDataFromFile();
                if (data == null)
                {
                    saveFileStatusMessage = "저장 파일을 읽는 중 오류가 발생했습니다. 콘솔 로그를 확인하세요.";
                    return;
                }

                hasSaveFile = true;
                savedGold = data.gold;
            }
            catch (Exception e)
            {
                saveFileStatusMessage = "저장 파일 접근 중 예외가 발생했습니다. 콘솔 로그를 확인하세요.";
                Debug.LogError($"[PlayerGoldEditor] Error while reading save file: {e.Message}");
            }
        }

        private void InitializeTargetGold()
        {
            if (hasRuntimeData)
            {
                targetGold = runtimeGold;
            }
            else if (hasSaveFile)
            {
                targetGold = savedGold;
            }
            else
            {
                targetGold = 0;
            }
        }

        #endregion

        #region Apply Methods

        private void ApplyToRuntime(int value)
        {
            value = Mathf.Max(0, value);

            if (!Application.isPlaying || !hasRuntimeData || playerDataManager == null)
            {
                Debug.LogWarning("[PlayerGoldEditor] 런타임에 적용할 수 없습니다. Play 모드 및 PlayerDataManager 상태를 확인하세요.");
                return;
            }

            playerDataManager.SetGold(value);
            runtimeGold = playerDataManager.CurrentGold;

            Debug.Log($"[PlayerGoldEditor] 런타임 골드 적용: {value}");
        }

        private void ApplyToSaveFile(int value, bool useRuntimePipelineIfAvailable)
        {
            value = Mathf.Max(0, value);

            // Play 모드에서 런타임 파이프라인을 통해 저장
            if (useRuntimePipelineIfAvailable &&
                Application.isPlaying &&
                hasRuntimeData &&
                playerDataManager != null &&
                saveDataAdapter != null &&
                saveDataAdapter.IsInitialized)
            {
                playerDataManager.SetGold(value);
                runtimeGold = playerDataManager.CurrentGold;

                saveDataAdapter.SaveSpecific(SaveFileType.PlayerData);
                Debug.Log($"[PlayerGoldEditor] SaveDataAdapter를 통해 PlayerData에 골드 저장: {value}");

                RefreshSavedState();
                return;
            }

            // Editor 모드 또는 SaveDataAdapter를 사용할 수 없는 경우, 파일 직접 수정
            try
            {
                var data = LoadPlayerDataFromFile() ?? new PlayerData();
                data.gold = value;
                SavePlayerDataToFile(data);

                Debug.Log($"[PlayerGoldEditor] player_data.json 직접 수정: gold = {value}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerGoldEditor] 저장 파일에 골드를 쓰는 중 오류: {e.Message}");
            }

            RefreshSavedState();
        }

        #endregion

        #region File Helpers

        private PlayerData LoadPlayerDataFromFile()
        {
            try
            {
                if (!File.Exists(saveFilePath))
                {
                    return null;
                }

                string json = File.ReadAllText(saveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<PlayerData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerGoldEditor] Failed to deserialize PlayerData: {e.Message}");
                return null;
            }
        }

        private void SavePlayerDataToFile(PlayerData data)
        {
            if (data == null) return;

            try
            {
                string directory = Path.GetDirectoryName(saveFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerGoldEditor] Failed to save PlayerData: {e.Message}");
            }
        }

        #endregion
    }
}

