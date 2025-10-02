using UnityEngine;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// GameSettings 에디터 창
    /// 게임 설정을 Unity Editor에서 제어
    /// </summary>
    public class GameSettingsEditor : EditorWindow
    {
        private float tempAnimationSpeed;
        private bool tempEnableAnimations;
        private bool tempEnableVFX;
        private bool tempEnableSFX;
        private int tempTargetFPS;
        private bool tempVSync;

        private bool settingsLoaded = false;

        [MenuItem("Tools/Game Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameSettingsEditor>("Game Settings");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            LoadTempSettings();
            settingsLoaded = true;
        }

        private void OnGUI()
        {
            if (!settingsLoaded)
            {
                LoadTempSettings();
                settingsLoaded = true;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Game Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            DrawAnimationSettings();
            EditorGUILayout.Space(10);

            DrawEffectSettings();
            EditorGUILayout.Space(10);

            DrawPerformanceSettings();
            EditorGUILayout.Space(10);

            DrawPresets();
            EditorGUILayout.Space(10);

            DrawActions();
        }

        #region GUI Drawing

        private void DrawAnimationSettings()
        {
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Animation Speed
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Animation Speed", GUILayout.Width(150));
            tempAnimationSpeed = EditorGUILayout.Slider(tempAnimationSpeed, 0.1f, 3.0f);
            EditorGUILayout.LabelField($"{tempAnimationSpeed:F1}x", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            // Quick speed buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("0.5x", GUILayout.Width(60))) tempAnimationSpeed = 0.5f;
            if (GUILayout.Button("1.0x", GUILayout.Width(60))) tempAnimationSpeed = 1.0f;
            if (GUILayout.Button("1.5x", GUILayout.Width(60))) tempAnimationSpeed = 1.5f;
            if (GUILayout.Button("2.0x", GUILayout.Width(60))) tempAnimationSpeed = 2.0f;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Enable Animations
            tempEnableAnimations = EditorGUILayout.Toggle("Enable Unit Animations", tempEnableAnimations);

            if (!tempEnableAnimations)
            {
                EditorGUILayout.HelpBox("Animations are disabled. Units will act instantly.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEffectSettings()
        {
            EditorGUILayout.LabelField("Effect Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            tempEnableVFX = EditorGUILayout.Toggle("Enable VFX (Particles)", tempEnableVFX);
            tempEnableSFX = EditorGUILayout.Toggle("Enable SFX (Sounds)", tempEnableSFX);

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceSettings()
        {
            EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Target FPS
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target FPS", GUILayout.Width(150));

            string[] fpsOptions = { "30", "60", "120", "Unlimited" };
            int[] fpsValues = { 30, 60, 120, -1 };
            int currentIndex = System.Array.IndexOf(fpsValues, tempTargetFPS);
            if (currentIndex < 0) currentIndex = 1; // default 60

            int selectedIndex = EditorGUILayout.Popup(currentIndex, fpsOptions);
            tempTargetFPS = fpsValues[selectedIndex];
            EditorGUILayout.EndHorizontal();

            // VSync
            tempVSync = EditorGUILayout.Toggle("VSync", tempVSync);

            EditorGUILayout.EndVertical();
        }

        private void DrawPresets()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Fast Play Preset
            if (GUILayout.Button("Fast Play"))
            {
                tempAnimationSpeed = 2.0f;
                tempEnableAnimations = true;
                tempEnableVFX = true;
                tempEnableSFX = true;
                EditorUtility.DisplayDialog("Preset Applied", "Fast Play preset applied (2x speed)", "OK");
            }

            // Speed Run Preset
            if (GUILayout.Button("Speed Run"))
            {
                tempAnimationSpeed = 1.0f;
                tempEnableAnimations = false;
                tempEnableVFX = false;
                tempEnableSFX = false;
                EditorUtility.DisplayDialog("Preset Applied", "Speed Run preset applied (animations disabled)", "OK");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // Performance Preset
            if (GUILayout.Button("Performance"))
            {
                tempAnimationSpeed = 1.5f;
                tempEnableAnimations = true;
                tempEnableVFX = false;
                tempEnableSFX = true;
                tempTargetFPS = 30;
                tempVSync = false;
                EditorUtility.DisplayDialog("Preset Applied", "Performance preset applied (effects reduced)", "OK");
            }

            // Quality Preset
            if (GUILayout.Button("Quality"))
            {
                tempAnimationSpeed = 1.0f;
                tempEnableAnimations = true;
                tempEnableVFX = true;
                tempEnableSFX = true;
                tempTargetFPS = 60;
                tempVSync = true;
                EditorUtility.DisplayDialog("Preset Applied", "Quality preset applied (all effects on)", "OK");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Apply Button
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Apply Settings", GUILayout.Height(40)))
            {
                ApplySettings();
            }
            GUI.backgroundColor = prevColor;

            // Reset Button
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Reset Settings", "Reset all settings to defaults?", "Yes", "No"))
                {
                    GameSettings.ResetToDefaults();
                    LoadTempSettings();
                    EditorUtility.DisplayDialog("Reset Complete", "Settings reset to defaults", "OK");
                }
            }
            GUI.backgroundColor = prevColor;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Current Settings Display
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Current Active Settings:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Animation Speed: {GameSettings.GlobalAnimationSpeed:F1}x");
            EditorGUILayout.LabelField($"Animations: {(GameSettings.EnableUnitAnimations ? "ON" : "OFF")}");
            EditorGUILayout.LabelField($"VFX: {(GameSettings.EnableVFX ? "ON" : "OFF")}");
            EditorGUILayout.LabelField($"SFX: {(GameSettings.EnableSFX ? "ON" : "OFF")}");
            EditorGUILayout.LabelField($"Target FPS: {(GameSettings.TargetFPS == -1 ? "Unlimited" : GameSettings.TargetFPS.ToString())}");
            EditorGUILayout.LabelField($"VSync: {(GameSettings.VSyncEnabled ? "ON" : "OFF")}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Settings Management

        private void LoadTempSettings()
        {
            tempAnimationSpeed = GameSettings.GlobalAnimationSpeed;
            tempEnableAnimations = GameSettings.EnableUnitAnimations;
            tempEnableVFX = GameSettings.EnableVFX;
            tempEnableSFX = GameSettings.EnableSFX;
            tempTargetFPS = GameSettings.TargetFPS;
            tempVSync = GameSettings.VSyncEnabled;
        }

        private void ApplySettings()
        {
            GameSettings.GlobalAnimationSpeed = tempAnimationSpeed;
            GameSettings.EnableUnitAnimations = tempEnableAnimations;
            GameSettings.EnableVFX = tempEnableVFX;
            GameSettings.EnableSFX = tempEnableSFX;
            GameSettings.TargetFPS = tempTargetFPS;
            GameSettings.VSyncEnabled = tempVSync;

            Debug.Log($"[GameSettingsEditor] Settings applied - Speed: {tempAnimationSpeed}x, " +
                     $"Animations: {(tempEnableAnimations ? "ON" : "OFF")}, " +
                     $"VFX: {(tempEnableVFX ? "ON" : "OFF")}");

            EditorUtility.DisplayDialog("Settings Applied", "Game settings have been applied successfully!", "OK");
        }

        #endregion
    }
}
