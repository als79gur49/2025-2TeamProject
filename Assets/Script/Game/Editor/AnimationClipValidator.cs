using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Game.Editor
{
    /// <summary>
    /// Animation Clip 검증 유틸리티
    /// Animation Event가 올바르게 설정되었는지 검증
    /// </summary>
    public static class AnimationClipValidator
    {
        #region Validation Rules

        private static readonly string[] REQUIRED_MOVE_EVENTS = new[]
        {
            "AnimEvent_OnAnimationStart",
            "AnimEvent_OnAnimationEnd"
        };

        private static readonly string[] REQUIRED_ATTACK_EVENTS = new[]
        {
            "AnimEvent_OnAnimationStart",
            "AnimEvent_OnAttackImpact",
            "AnimEvent_OnAnimationEnd"
        };

        private static readonly string[] ALL_VALID_EVENTS = new[]
        {
            "AnimEvent_OnAnimationStart",
            "AnimEvent_OnAnimationEnd",
            "AnimEvent_OnAttackImpact",
            "AnimEvent_OnMoveComplete",
            "AnimEvent_OnSkillCast"
        };

        #endregion

        #region Validation Methods

        /// <summary>
        /// Animation Clip의 Animation Event 검증
        /// </summary>
        public static ValidationResult ValidateClip(AnimationClip clip, AnimationType type)
        {
            if (clip == null)
            {
                return ValidationResult.Error("Animation Clip is null");
            }

            var events = AnimationUtility.GetAnimationEvents(clip);
            var result = new ValidationResult();

            // 필수 이벤트 체크
            string[] requiredEvents = type == AnimationType.Move ? REQUIRED_MOVE_EVENTS : REQUIRED_ATTACK_EVENTS;
            var eventNames = events.Select(e => e.functionName).ToArray();

            foreach (var required in requiredEvents)
            {
                if (!eventNames.Contains(required))
                {
                    result.AddError($"Missing required event: {required}");
                }
            }

            // 이벤트 함수명 검증
            foreach (var evt in events)
            {
                if (!ALL_VALID_EVENTS.Contains(evt.functionName))
                {
                    result.AddWarning($"Unknown event function: {evt.functionName}");
                }
            }

            // 타이밍 검증
            ValidateEventTiming(events, clip.length, result);

            // 타입별 특수 검증
            if (type == AnimationType.Attack)
            {
                ValidateAttackTiming(events, clip.length, result);
            }

            result.ClipName = clip.name;
            result.ClipLength = clip.length;
            result.EventCount = events.Length;

            return result;
        }

        private static void ValidateEventTiming(AnimationEvent[] events, float clipLength, ValidationResult result)
        {
            foreach (var evt in events)
            {
                if (evt.time < 0f || evt.time > clipLength)
                {
                    result.AddError($"Event {evt.functionName} time {evt.time:F2}s is out of range (0 ~ {clipLength:F2}s)");
                }
            }

            // Start 이벤트는 0.0s에 있어야 함
            var startEvent = events.FirstOrDefault(e => e.functionName == "AnimEvent_OnAnimationStart");
            if (startEvent != null && startEvent.time > 0.01f)
            {
                result.AddWarning($"Start event should be at 0.0s (current: {startEvent.time:F2}s)");
            }

            // End 이벤트는 마지막에 있어야 함
            var endEvent = events.FirstOrDefault(e => e.functionName == "AnimEvent_OnAnimationEnd");
            if (endEvent != null && Mathf.Abs(endEvent.time - clipLength) > 0.01f)
            {
                result.AddWarning($"End event should be at {clipLength:F2}s (current: {endEvent.time:F2}s)");
            }
        }

        private static void ValidateAttackTiming(AnimationEvent[] events, float clipLength, ValidationResult result)
        {
            var impactEvent = events.FirstOrDefault(e => e.functionName == "AnimEvent_OnAttackImpact");
            if (impactEvent != null)
            {
                float normalizedTime = impactEvent.time / clipLength;

                // Impact는 보통 30% ~ 80% 사이
                if (normalizedTime < 0.3f || normalizedTime > 0.8f)
                {
                    result.AddWarning($"Attack impact timing {normalizedTime:P0} is unusual (recommended: 30% ~ 80%)");
                }
            }
        }

        #endregion

        #region Batch Validation

        [MenuItem("Tools/Animation/Validate All Animation Clips")]
        public static void ValidateAllClips()
        {
            var allClips = AssetDatabase.FindAssets("t:AnimationClip")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<AnimationClip>(path))
                .Where(clip => clip != null)
                .ToArray();

            var results = new List<ValidationResult>();

            foreach (var clip in allClips)
            {
                AnimationType type = AnimationType.Other;

                if (clip.name.ToLower().Contains("move"))
                    type = AnimationType.Move;
                else if (clip.name.ToLower().Contains("attack"))
                    type = AnimationType.Attack;

                if (type != AnimationType.Other)
                {
                    var result = ValidateClip(clip, type);
                    results.Add(result);
                }
            }

            ShowValidationReport(results);
        }

        private static void ShowValidationReport(List<ValidationResult> results)
        {
            int total = results.Count;
            int passed = results.Count(r => r.IsValid);
            int failed = results.Count(r => !r.IsValid);

            string report = $"Animation Clip Validation Report\n\n";
            report += $"Total: {total} | Passed: {passed} | Failed: {failed}\n";
            report += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

            foreach (var result in results.Where(r => !r.IsValid))
            {
                report += result.ToString() + "\n\n";
            }

            if (failed == 0)
            {
                report += "✓ All animation clips are valid!\n";
            }

            Debug.Log(report);

            EditorUtility.DisplayDialog(
                "Validation Report",
                $"Total: {total}\nPassed: {passed}\nFailed: {failed}\n\nSee Console for details.",
                "OK");
        }

        #endregion

        #region Context Menu

        [MenuItem("Assets/Validate Animation Clip", true)]
        private static bool ValidateAnimationClipValidation()
        {
            return Selection.activeObject is AnimationClip;
        }

        [MenuItem("Assets/Validate Animation Clip")]
        private static void ValidateAnimationClip()
        {
            var clip = Selection.activeObject as AnimationClip;
            if (clip == null) return;

            AnimationType type = AnimationType.Other;
            if (clip.name.ToLower().Contains("move"))
                type = AnimationType.Move;
            else if (clip.name.ToLower().Contains("attack"))
                type = AnimationType.Attack;

            if (type == AnimationType.Other)
            {
                EditorUtility.DisplayDialog("Info", "This is not a Move or Attack animation clip.", "OK");
                return;
            }

            var result = ValidateClip(clip, type);

            Debug.Log(result.ToString());

            EditorUtility.DisplayDialog(
                result.IsValid ? "Validation Passed" : "Validation Failed",
                result.ToString(),
                "OK");
        }

        #endregion

        #region Helper Types

        public enum AnimationType
        {
            Move,
            Attack,
            Other
        }

        public class ValidationResult
        {
            public string ClipName { get; set; }
            public float ClipLength { get; set; }
            public int EventCount { get; set; }
            public List<string> Errors { get; private set; } = new List<string>();
            public List<string> Warnings { get; private set; } = new List<string>();

            public bool IsValid => Errors.Count == 0;

            public void AddError(string error)
            {
                Errors.Add(error);
            }

            public void AddWarning(string warning)
            {
                Warnings.Add(warning);
            }

            public static ValidationResult Error(string error)
            {
                var result = new ValidationResult();
                result.AddError(error);
                return result;
            }

            public override string ToString()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Clip: {ClipName} ({ClipLength:F2}s, {EventCount} events)");

                if (IsValid && Warnings.Count == 0)
                {
                    sb.AppendLine("✓ Valid");
                }
                else
                {
                    if (Errors.Count > 0)
                    {
                        sb.AppendLine("❌ Errors:");
                        foreach (var error in Errors)
                        {
                            sb.AppendLine($"  • {error}");
                        }
                    }

                    if (Warnings.Count > 0)
                    {
                        sb.AppendLine("⚠ Warnings:");
                        foreach (var warning in Warnings)
                        {
                            sb.AppendLine($"  • {warning}");
                        }
                    }
                }

                return sb.ToString();
            }
        }

        #endregion
    }
}
