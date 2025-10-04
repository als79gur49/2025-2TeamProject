using UnityEngine;
using UnityEditor;
using Game.Components;
using System.Linq;

namespace Game.Editor
{
    /// <summary>
    /// UnitAnimationController 커스텀 인스펙터
    /// Animation Event 설정 상태 표시 및 빠른 설정 버튼 제공
    /// </summary>
    [CustomEditor(typeof(UnitAnimationController))]
    public class UnitAnimationControllerInspector : UnityEditor.Editor
    {
        private SerializedProperty animatorProp;
        private SerializedProperty useAnimatorProp;
        private SerializedProperty animationSpeedMultiplierProp;
        private SerializedProperty skipAnimationsProp;
        private SerializedProperty logAnimationEventsProp;

        private bool showAnimationClipStatus = true;

        private void OnEnable()
        {
            animatorProp = serializedObject.FindProperty("animator");
            useAnimatorProp = serializedObject.FindProperty("useAnimator");
            animationSpeedMultiplierProp = serializedObject.FindProperty("animationSpeedMultiplier");
            skipAnimationsProp = serializedObject.FindProperty("skipAnimations");
            logAnimationEventsProp = serializedObject.FindProperty("logAnimationEvents");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var controller = target as UnitAnimationController;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Unit Animation Controller", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Animation Event 기반 애니메이션 컨트롤러\n" +
                "Move/Attack Animation Clip에 Animation Event가 설정되어야 합니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Animator Settings
            EditorGUILayout.LabelField("Animator Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(animatorProp);
            EditorGUILayout.PropertyField(useAnimatorProp);

            EditorGUILayout.Space(5);

            // Advanced Settings
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(animationSpeedMultiplierProp);
            EditorGUILayout.PropertyField(skipAnimationsProp);

            EditorGUILayout.Space(5);

            // Debug Settings
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(logAnimationEventsProp);

            EditorGUILayout.Space(10);

            // Animation Clip Status
            DrawAnimationClipStatus(controller);

            EditorGUILayout.Space(10);

            // Quick Actions
            DrawQuickActions(controller);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAnimationClipStatus(UnitAnimationController controller)
        {
            showAnimationClipStatus = EditorGUILayout.Foldout(showAnimationClipStatus, "Animation Clip Status");

            if (!showAnimationClipStatus) return;

            EditorGUI.indentLevel++;

            var animator = (controller as MonoBehaviour)?.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                EditorGUILayout.HelpBox("No Animator or AnimatorController found.", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            var animatorController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (animatorController == null)
            {
                EditorGUILayout.HelpBox("AnimatorController is not editable.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            // Move Animation 체크
            var moveClip = FindAnimationClip(animatorController, "Move");
            DrawClipStatus("Move Animation", moveClip, new[] { "AnimEvent_OnAnimationStart", "AnimEvent_OnAnimationEnd" });

            // Attack Animation 체크
            var attackClip = FindAnimationClip(animatorController, "Attack");
            DrawClipStatus("Attack Animation", attackClip, new[] { "AnimEvent_OnAnimationStart", "AnimEvent_OnAttackImpact", "AnimEvent_OnAnimationEnd" });

            EditorGUI.indentLevel--;
        }

        private void DrawClipStatus(string label, AnimationClip clip, string[] requiredEvents)
        {
            EditorGUILayout.BeginHorizontal();

            if (clip == null)
            {
                EditorGUILayout.LabelField(label, "NOT FOUND", EditorStyles.miniLabel);
            }
            else
            {
                var events = AnimationUtility.GetAnimationEvents(clip);
                var eventNames = events.Select(e => e.functionName).ToArray();

                bool hasAllEvents = requiredEvents.All(required => eventNames.Contains(required));

                if (hasAllEvents)
                {
                    EditorGUILayout.LabelField(label, "✓ READY", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField(label, "⚠ MISSING EVENTS", EditorStyles.miniLabel);
                }

                EditorGUILayout.LabelField($"({events.Length} events)", EditorStyles.miniLabel, GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();

            if (clip != null)
            {
                EditorGUI.indentLevel++;
                var events = AnimationUtility.GetAnimationEvents(clip);
                foreach (var evt in events)
                {
                    EditorGUILayout.LabelField($"• {evt.functionName} @ {evt.time:F2}s", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3);
        }

        private void DrawQuickActions(UnitAnimationController controller)
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Animation Event Setup Tool", GUILayout.Height(30)))
            {
                AnimationEventSetupTool.ShowWindow();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Test Move Animation", GUILayout.Height(25)))
            {
                if (Application.isPlaying)
                {
                    controller.PlayMoveAnimation(Vector2Int.zero, Vector2Int.one);
                    Debug.Log("[Inspector] Triggered Move Animation");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Enter Play Mode to test animations.", "OK");
                }
            }

            if (GUILayout.Button("Test Attack Animation", GUILayout.Height(25)))
            {
                if (Application.isPlaying)
                {
                    controller.PlayAttackAnimation(controller.gameObject);
                    Debug.Log("[Inspector] Triggered Attack Animation");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Enter Play Mode to test animations.", "OK");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private AnimationClip FindAnimationClip(UnityEditor.Animations.AnimatorController controller, string stateName)
        {
            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.name.Contains(stateName))
                    {
                        return state.state.motion as AnimationClip;
                    }
                }
            }
            return null;
        }
    }
}
