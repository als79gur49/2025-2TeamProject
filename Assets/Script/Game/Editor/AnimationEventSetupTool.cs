using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Game.Editor
{
    /// <summary>
    /// Animation Event 자동 설정 에디터 도구
    /// Move/Attack Animation Clip에 Animation Event를 자동으로 추가
    /// </summary>
    public class AnimationEventSetupTool : EditorWindow
    {
        #region Window Settings

        private const string WINDOW_TITLE = "Animation Event Setup";
        private const int WINDOW_WIDTH = 500;
        private const int WINDOW_HEIGHT = 600;

        #endregion

        #region Animation Event Settings

        [System.Serializable]
        private class AnimationEventConfig
        {
            public string eventName;
            public string functionName;
            public float normalizedTime; // 0.0 ~ 1.0
            public string description;

            public AnimationEventConfig(string name, string function, float time, string desc)
            {
                eventName = name;
                functionName = function;
                normalizedTime = time;
                description = desc;
            }
        }

        // Move Animation Events
        private readonly AnimationEventConfig[] moveEvents = new[]
        {
            new AnimationEventConfig("Start", "AnimEvent_OnAnimationStart", 0.0f, "애니메이션 시작"),
            new AnimationEventConfig("End", "AnimEvent_OnAnimationEnd", 1.0f, "애니메이션 종료"),
            new AnimationEventConfig("MoveComplete", "AnimEvent_OnMoveComplete", 1.0f, "이동 완료 (선택적)")
        };

        // Attack Animation Events
        private readonly AnimationEventConfig[] attackEvents = new[]
        {
            new AnimationEventConfig("Start", "AnimEvent_OnAnimationStart", 0.0f, "애니메이션 시작"),
            new AnimationEventConfig("Impact", "AnimEvent_OnAttackImpact", 0.6f, "공격 타격 순간"),
            new AnimationEventConfig("End", "AnimEvent_OnAnimationEnd", 1.0f, "애니메이션 종료")
        };

        #endregion

        #region GUI State

        private AnimationClip moveAnimationClip;
        private AnimationClip attackAnimationClip;
        private Vector2 scrollPosition;
        private float attackImpactTiming = 0.6f; // 공격 타격 타이밍 (0.0 ~ 1.0)
        private bool includeMoveComplete = false; // Move Complete 이벤트 포함 여부

        #endregion

        #region Menu Item

        [MenuItem("Tools/Animation/Setup Animation Events")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationEventSetupTool>(WINDOW_TITLE);
            window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            window.Show();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawMoveAnimationSection();
            DrawAttackAnimationSection();
            DrawBatchOperationsSection();
            DrawInstructions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Animation Event Setup Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Move/Attack Animation Clip에 Animation Event를 자동으로 추가합니다.\n" +
                "UnitAnimationController가 이 이벤트를 수신하여 게임 로직을 실행합니다.",
                MessageType.Info);
            EditorGUILayout.Space(10);
        }

        private void DrawMoveAnimationSection()
        {
            EditorGUILayout.LabelField("Move Animation", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            moveAnimationClip = EditorGUILayout.ObjectField(
                "Move Animation Clip",
                moveAnimationClip,
                typeof(AnimationClip),
                false) as AnimationClip;

            if (moveAnimationClip != null)
            {
                EditorGUILayout.LabelField($"Length: {moveAnimationClip.length:F2}s", EditorStyles.miniLabel);

                EditorGUILayout.Space(5);
                includeMoveComplete = EditorGUILayout.Toggle("Include Move Complete Event", includeMoveComplete);

                if (GUILayout.Button("Setup Move Animation Events", GUILayout.Height(30)))
                {
                    SetupMoveAnimationEvents(moveAnimationClip, includeMoveComplete);
                }

                if (GUILayout.Button("Clear Move Animation Events", GUILayout.Height(25)))
                {
                    ClearAnimationEvents(moveAnimationClip);
                }
            }

            EditorGUILayout.Space(10);
        }

        private void DrawAttackAnimationSection()
        {
            EditorGUILayout.LabelField("Attack Animation", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            attackAnimationClip = EditorGUILayout.ObjectField(
                "Attack Animation Clip",
                attackAnimationClip,
                typeof(AnimationClip),
                false) as AnimationClip;

            if (attackAnimationClip != null)
            {
                EditorGUILayout.LabelField($"Length: {attackAnimationClip.length:F2}s", EditorStyles.miniLabel);

                EditorGUILayout.Space(5);
                attackImpactTiming = EditorGUILayout.Slider(
                    "Impact Timing (0-1)",
                    attackImpactTiming,
                    0.0f,
                    1.0f);

                float impactTime = attackAnimationClip.length * attackImpactTiming;
                EditorGUILayout.LabelField($"Impact at: {impactTime:F3}s", EditorStyles.miniLabel);

                if (GUILayout.Button("Setup Attack Animation Events", GUILayout.Height(30)))
                {
                    SetupAttackAnimationEvents(attackAnimationClip, attackImpactTiming);
                }

                if (GUILayout.Button("Clear Attack Animation Events", GUILayout.Height(25)))
                {
                    ClearAnimationEvents(attackAnimationClip);
                }
            }

            EditorGUILayout.Space(10);
        }

        private void DrawBatchOperationsSection()
        {
            EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Setup Both Animations", GUILayout.Height(35)))
            {
                if (moveAnimationClip != null)
                {
                    SetupMoveAnimationEvents(moveAnimationClip, includeMoveComplete);
                }
                if (attackAnimationClip != null)
                {
                    SetupAttackAnimationEvents(attackAnimationClip, attackImpactTiming);
                }
            }

            if (GUILayout.Button("Clear Both Animations", GUILayout.Height(30)))
            {
                if (moveAnimationClip != null)
                {
                    ClearAnimationEvents(moveAnimationClip);
                }
                if (attackAnimationClip != null)
                {
                    ClearAnimationEvents(attackAnimationClip);
                }
            }

            EditorGUILayout.Space(10);
        }

        private void DrawInstructions()
        {
            EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Move/Attack Animation Clip을 위 필드에 드래그\n" +
                "2. Attack Impact Timing 조정 (0.6 = 60% 지점)\n" +
                "3. 'Setup' 버튼 클릭하여 Animation Event 추가\n" +
                "4. Animation 창(Window → Animation)에서 이벤트 확인\n" +
                "5. 타이밍 미세 조정은 Animation 창에서 직접 드래그",
                MessageType.None);
        }

        #endregion

        #region Animation Event Setup Logic

        private void SetupMoveAnimationEvents(AnimationClip clip, bool includeMoveCompleteEvent)
        {
            if (clip == null)
            {
                EditorUtility.DisplayDialog("Error", "Animation Clip is null!", "OK");
                return;
            }

            // 기존 이벤트 제거
            AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);

            // 새 이벤트 추가
            var events = new System.Collections.Generic.List<AnimationEvent>();

            // Start Event
            events.Add(CreateAnimationEvent(
                "AnimEvent_OnAnimationStart",
                0.0f,
                clip.length));

            // End Event
            events.Add(CreateAnimationEvent(
                "AnimEvent_OnAnimationEnd",
                clip.length,
                clip.length));

            // Move Complete Event (선택적)
            if (includeMoveCompleteEvent)
            {
                events.Add(CreateAnimationEvent(
                    "AnimEvent_OnMoveComplete",
                    clip.length,
                    clip.length));
            }

            AnimationUtility.SetAnimationEvents(clip, events.ToArray());

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AnimationEventSetup] Move Animation Events added to {clip.name}");
            EditorUtility.DisplayDialog(
                "Success",
                $"Move Animation Events added to {clip.name}\n\n" +
                $"Events: {events.Count}\n" +
                $"- AnimEvent_OnAnimationStart (0.0s)\n" +
                $"- AnimEvent_OnAnimationEnd ({clip.length:F2}s)\n" +
                (includeMoveCompleteEvent ? $"- AnimEvent_OnMoveComplete ({clip.length:F2}s)" : ""),
                "OK");
        }

        private void SetupAttackAnimationEvents(AnimationClip clip, float impactNormalizedTime)
        {
            if (clip == null)
            {
                EditorUtility.DisplayDialog("Error", "Animation Clip is null!", "OK");
                return;
            }

            // 기존 이벤트 제거
            AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);

            // 새 이벤트 추가
            var events = new System.Collections.Generic.List<AnimationEvent>();

            // Start Event
            events.Add(CreateAnimationEvent(
                "AnimEvent_OnAnimationStart",
                0.0f,
                clip.length));

            // Impact Event (타격 순간)
            float impactTime = clip.length * impactNormalizedTime;
            events.Add(CreateAnimationEvent(
                "AnimEvent_OnAttackImpact",
                impactTime,
                clip.length));

            // End Event
            events.Add(CreateAnimationEvent(
                "AnimEvent_OnAnimationEnd",
                clip.length,
                clip.length));

            AnimationUtility.SetAnimationEvents(clip, events.ToArray());

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AnimationEventSetup] Attack Animation Events added to {clip.name}");
            EditorUtility.DisplayDialog(
                "Success",
                $"Attack Animation Events added to {clip.name}\n\n" +
                $"Events: {events.Count}\n" +
                $"- AnimEvent_OnAnimationStart (0.0s)\n" +
                $"- AnimEvent_OnAttackImpact ({impactTime:F2}s / {impactNormalizedTime:P0})\n" +
                $"- AnimEvent_OnAnimationEnd ({clip.length:F2}s)",
                "OK");
        }

        private void ClearAnimationEvents(AnimationClip clip)
        {
            if (clip == null)
            {
                EditorUtility.DisplayDialog("Error", "Animation Clip is null!", "OK");
                return;
            }

            int eventCount = AnimationUtility.GetAnimationEvents(clip).Length;

            if (eventCount == 0)
            {
                EditorUtility.DisplayDialog("Info", $"{clip.name} has no animation events.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog(
                "Confirm Clear",
                $"Clear {eventCount} animation events from {clip.name}?",
                "Clear",
                "Cancel"))
            {
                AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();

                Debug.Log($"[AnimationEventSetup] Cleared {eventCount} events from {clip.name}");
                EditorUtility.DisplayDialog("Success", $"Cleared {eventCount} events from {clip.name}", "OK");
            }
        }

        private AnimationEvent CreateAnimationEvent(string functionName, float time, float clipLength)
        {
            var animEvent = new AnimationEvent
            {
                functionName = functionName,
                time = Mathf.Clamp(time, 0f, clipLength)
            };
            return animEvent;
        }

        #endregion

        #region Validation

        private void OnEnable()
        {
            // 기본값 설정
            attackImpactTiming = 0.6f;
            includeMoveComplete = false;
        }

        #endregion
    }
}
