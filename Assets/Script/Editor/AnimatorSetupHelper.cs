using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

namespace Game.Editor
{
    /// <summary>
    /// Animator Controller 설정 도우미 Editor 스크립트
    ///
    /// 사용법:
    /// 1. Unity Editor 메뉴: Tools > Animation > Setup Attack State Behaviours
    /// 2. Animator Controller 선택 창이 뜸
    /// 3. 설정할 Animator Controller 선택
    /// 4. Attack State에 자동으로 AttackStateBehaviour 추가
    /// </summary>
    public class AnimatorSetupHelper : EditorWindow
    {
        private AnimatorController animatorController;
        private Vector2 scrollPosition;
        private List<AnimatorState> attackStates = new List<AnimatorState>();
        private Dictionary<AnimatorState, bool> stateSelections = new Dictionary<AnimatorState, bool>();

        [MenuItem("Tools/Animation/Setup Attack State Behaviours")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimatorSetupHelper>("Attack State Setup");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Attack State Behaviour Setup Helper", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "이 도구는 Animator Controller의 Attack State에 AttackStateBehaviour를 자동으로 추가합니다.\n\n" +
                "기능:\n" +
                "1. Attack 태그가 있는 State 자동 검색\n" +
                "2. AttackStateBehaviour 자동 추가\n" +
                "3. 중복 추가 방지",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Animator Controller 선택
            EditorGUILayout.LabelField("Step 1: Select Animator Controller", EditorStyles.boldLabel);
            AnimatorController newController = (AnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller",
                animatorController,
                typeof(AnimatorController),
                false
            );

            if (newController != animatorController)
            {
                animatorController = newController;
                if (animatorController != null)
                {
                    ScanAttackStates();
                }
            }

            EditorGUILayout.Space();

            if (animatorController == null)
            {
                EditorGUILayout.HelpBox("Animator Controller를 선택하세요.", MessageType.Warning);
                return;
            }

            // Attack State 목록 표시
            EditorGUILayout.LabelField($"Step 2: Select Attack States ({attackStates.Count} found)", EditorStyles.boldLabel);

            if (attackStates.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Attack 태그가 설정된 State를 찾을 수 없습니다.\n\n" +
                    "해결 방법:\n" +
                    "1. Animator Controller를 열어주세요\n" +
                    "2. Attack State를 선택하세요\n" +
                    "3. Inspector에서 Tag를 'Attack'으로 설정하세요",
                    MessageType.Warning
                );

                if (GUILayout.Button("Refresh"))
                {
                    ScanAttackStates();
                }
                return;
            }

            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var state in attackStates)
            {
                EditorGUILayout.BeginHorizontal();

                if (!stateSelections.ContainsKey(state))
                {
                    stateSelections[state] = true;
                }

                stateSelections[state] = EditorGUILayout.Toggle(stateSelections[state], GUILayout.Width(20));

                bool hasBehaviour = HasAttackStateBehaviour(state);
                string status = hasBehaviour ? "[Already Added]" : "[Not Added]";
                Color originalColor = GUI.color;
                GUI.color = hasBehaviour ? Color.green : Color.yellow;
                EditorGUILayout.LabelField($"{state.name} {status}");
                GUI.color = originalColor;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 버튼들
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All"))
            {
                foreach (var state in attackStates)
                {
                    stateSelections[state] = true;
                }
            }

            if (GUILayout.Button("Deselect All"))
            {
                foreach (var state in attackStates)
                {
                    stateSelections[state] = false;
                }
            }

            if (GUILayout.Button("Refresh"))
            {
                ScanAttackStates();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Step 3: Apply
            EditorGUILayout.LabelField("Step 3: Apply Changes", EditorStyles.boldLabel);

            var selectedStates = stateSelections.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            int toAdd = selectedStates.Count(s => !HasAttackStateBehaviour(s));
            int alreadyAdded = selectedStates.Count(s => HasAttackStateBehaviour(s));

            EditorGUILayout.HelpBox(
                $"Selected: {selectedStates.Count} states\n" +
                $"To Add: {toAdd} behaviours\n" +
                $"Already Added: {alreadyAdded} states (will skip)",
                MessageType.Info
            );

            GUI.enabled = toAdd > 0;
            if (GUILayout.Button("Add AttackStateBehaviour to Selected States", GUILayout.Height(40)))
            {
                ApplyAttackStateBehaviours();
            }
            GUI.enabled = true;
        }

        private void ScanAttackStates()
        {
            attackStates.Clear();
            stateSelections.Clear();

            if (animatorController == null)
                return;

            foreach (var layer in animatorController.layers)
            {
                ScanStatesRecursive(layer.stateMachine);
            }

            Debug.Log($"[AnimatorSetupHelper] Found {attackStates.Count} Attack states");
        }

        private void ScanStatesRecursive(AnimatorStateMachine stateMachine)
        {
            foreach (var state in stateMachine.states)
            {
                if (state.state.tag == "Attack")
                {
                    attackStates.Add(state.state);
                }
            }

            foreach (var subMachine in stateMachine.stateMachines)
            {
                ScanStatesRecursive(subMachine.stateMachine);
            }
        }

        private bool HasAttackStateBehaviour(AnimatorState state)
        {
            foreach (var behaviour in state.behaviours)
            {
                if (behaviour is Game.Components.AttackStateBehaviour)
                {
                    return true;
                }
            }
            return false;
        }

        private void ApplyAttackStateBehaviours()
        {
            int addedCount = 0;
            int skippedCount = 0;

            foreach (var kvp in stateSelections)
            {
                if (!kvp.Value) // Not selected
                    continue;

                var state = kvp.Key;

                if (HasAttackStateBehaviour(state))
                {
                    Debug.Log($"[AnimatorSetupHelper] Skipped '{state.name}' - already has AttackStateBehaviour");
                    skippedCount++;
                    continue;
                }

                state.AddStateMachineBehaviour<Game.Components.AttackStateBehaviour>();
                addedCount++;
                Debug.Log($"[AnimatorSetupHelper] Added AttackStateBehaviour to '{state.name}'");
            }

            // Save changes
            EditorUtility.SetDirty(animatorController);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Setup Complete",
                $"AttackStateBehaviour Setup Complete!\n\n" +
                $"Added: {addedCount} states\n" +
                $"Skipped: {skippedCount} states (already added)",
                "OK"
            );

            // Refresh
            ScanAttackStates();
        }
    }

    /// <summary>
    /// Animation Event 검증 도우미
    ///
    /// 사용법:
    /// 1. Unity Editor 메뉴: Tools > Animation > Validate Animation Events
    /// 2. Animator Controller 선택
    /// 3. Animation Clip의 Event 설정 검증
    /// </summary>
    public class AnimationEventValidator : EditorWindow
    {
        private AnimatorController animatorController;
        private Vector2 scrollPosition;
        private List<ValidationResult> validationResults = new List<ValidationResult>();

        private class ValidationResult
        {
            public AnimationClip clip;
            public string stateName;
            public bool hasAttackHit;
            public bool hasAttackEnd;
            public List<string> issues = new List<string>();
        }

        [MenuItem("Tools/Animation/Validate Animation Events")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationEventValidator>("Animation Event Validator");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Animation Event Validator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "이 도구는 Animation Clip의 Event 설정을 검증합니다.\n\n" +
                "검증 항목:\n" +
                "1. AttackHit 이벤트 존재 여부\n" +
                "2. AttackEnd 이벤트 제거 확인 (StateMachineBehaviour 사용)\n" +
                "3. Event 타이밍 검증",
                MessageType.Info
            );

            EditorGUILayout.Space();

            animatorController = (AnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller",
                animatorController,
                typeof(AnimatorController),
                false
            );

            EditorGUILayout.Space();

            if (animatorController == null)
            {
                EditorGUILayout.HelpBox("Animator Controller를 선택하세요.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Validate", GUILayout.Height(30)))
            {
                ValidateAnimationEvents();
            }

            EditorGUILayout.Space();

            if (validationResults.Count == 0)
            {
                EditorGUILayout.HelpBox("Validate 버튼을 클릭하여 검증을 시작하세요.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var result in validationResults)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"State: {result.stateName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Clip: {result.clip.name}");

                bool hasIssues = result.issues.Count > 0;
                Color originalColor = GUI.color;
                GUI.color = hasIssues ? Color.yellow : Color.green;

                EditorGUILayout.LabelField($"AttackHit Event: {(result.hasAttackHit ? "✓ Found" : "✗ Missing")}");
                EditorGUILayout.LabelField($"AttackEnd Event: {(result.hasAttackEnd ? "⚠ Found (Should Remove)" : "✓ Not Found (Correct)")}");

                GUI.color = originalColor;

                if (hasIssues)
                {
                    EditorGUILayout.LabelField("Issues:", EditorStyles.boldLabel);
                    foreach (var issue in result.issues)
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField($"  • {issue}");
                        GUI.color = originalColor;
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }

        private void ValidateAnimationEvents()
        {
            validationResults.Clear();

            foreach (var layer in animatorController.layers)
            {
                ValidateStatesRecursive(layer.stateMachine);
            }

            int totalStates = validationResults.Count;
            int issueCount = validationResults.Count(r => r.issues.Count > 0);

            Debug.Log($"[AnimationEventValidator] Validation complete: {totalStates} states checked, {issueCount} issues found");
        }

        private void ValidateStatesRecursive(AnimatorStateMachine stateMachine)
        {
            foreach (var state in stateMachine.states)
            {
                if (state.state.tag == "Attack" && state.state.motion is AnimationClip)
                {
                    var clip = state.state.motion as AnimationClip;
                    var result = new ValidationResult
                    {
                        clip = clip,
                        stateName = state.state.name
                    };

                    var events = AnimationUtility.GetAnimationEvents(clip);

                    foreach (var evt in events)
                    {
                        if (evt.functionName == "AttackHit")
                        {
                            result.hasAttackHit = true;

                            // 타이밍 검증 (50~70% 권장)
                            if (evt.time / clip.length < 0.5f || evt.time / clip.length > 0.7f)
                            {
                                result.issues.Add($"AttackHit timing unusual: {evt.time / clip.length:P0} (recommended: 50~70%)");
                            }
                        }
                        else if (evt.functionName == "AttackEnd")
                        {
                            result.hasAttackEnd = true;
                            result.issues.Add("AttackEnd event found - should be removed (use StateMachineBehaviour instead)");
                        }
                    }

                    if (!result.hasAttackHit)
                    {
                        result.issues.Add("AttackHit event missing - add at 60% timing");
                    }

                    validationResults.Add(result);
                }
            }

            foreach (var subMachine in stateMachine.stateMachines)
            {
                ValidateStatesRecursive(subMachine.stateMachine);
            }
        }
    }
}
