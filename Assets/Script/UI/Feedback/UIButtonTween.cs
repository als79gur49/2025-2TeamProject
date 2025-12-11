using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.UI.Feedback
{
    /// <summary>
    /// 공통 UI 버튼 DOTween 애니메이션
    /// Hover/Click 시 Color + Alpha + Scale을 상태별로 제어합니다.
    /// </summary>
    public class UIButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Targets")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private bool autoAssignTargets = true;

        [Header("State Settings")]
        [SerializeField] private ButtonVisualState normalState = ButtonVisualState.DefaultNormal();
        [SerializeField] private ButtonVisualState hoverState = ButtonVisualState.DefaultHover();
        [SerializeField] private ButtonVisualState pressedState = ButtonVisualState.DefaultPressed();

        [Header("Options")]
        [SerializeField] private bool useHoverAnimation = true;
        [SerializeField] private bool usePressAnimation = true;
        [SerializeField] private bool useInitialAsNormal = true;

        // Runtime
        private Tween colorTween;
        private Tween scaleTween;
        private bool isPointerInside;

        #region Lifecycle

        private void Awake()
        {
            if (autoAssignTargets)
            {
                if (targetTransform == null)
                {
                    targetTransform = transform;
                }

                if (targetGraphic == null)
                {
                    targetGraphic = GetComponent<Graphic>();
                }
            }

            // 현재 값을 Normal 상태로 초기화할 수 있도록 옵션 제공
            if (useInitialAsNormal)
            {
                if (targetTransform != null)
                {
                    normalState.scale = targetTransform.localScale;
                }

                if (targetGraphic != null)
                {
                    Color current = targetGraphic.color;
                    normalState.color = new Color(current.r, current.g, current.b, 1f);
                    normalState.alpha = current.a;
                }
            }
        }

        private void OnEnable()
        {
            ApplyStateInstant(normalState);
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        #endregion

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;

            if (!useHoverAnimation)
                return;

            ApplyStateTween(hoverState);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;

            if (!useHoverAnimation)
                return;

            ApplyStateTween(normalState);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!usePressAnimation)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            ApplyStateTween(pressedState);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!usePressAnimation)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // 눌렀던 버튼을 뗄 때, 마우스가 여전히 버튼 위에 있으면 Hover, 아니면 Normal로 복귀
            if (useHoverAnimation && isPointerInside)
            {
                ApplyStateTween(hoverState);
            }
            else
            {
                ApplyStateTween(normalState);
            }
        }

        #endregion

        #region State Apply Helpers

        private void ApplyStateTween(ButtonVisualState state)
        {
            KillTweens();

            if (targetTransform != null)
            {
                scaleTween = targetTransform
                    .DOScale(state.scale, state.duration)
                    .SetEase(state.ease);
            }

            if (targetGraphic != null)
            {
                Color targetColor = state.color;
                targetColor.a = state.alpha;

                colorTween = targetGraphic
                    .DOColor(targetColor, state.duration)
                    .SetEase(state.ease);
            }
        }

        private void ApplyStateInstant(ButtonVisualState state)
        {
            KillTweens();

            if (targetTransform != null)
            {
                targetTransform.localScale = state.scale;
            }

            if (targetGraphic != null)
            {
                Color c = state.color;
                c.a = state.alpha;
                targetGraphic.color = c;
            }
        }

        private void KillTweens()
        {
            if (scaleTween != null && scaleTween.IsActive())
            {
                scaleTween.Kill();
            }

            if (colorTween != null && colorTween.IsActive())
            {
                colorTween.Kill();
            }

            scaleTween = null;
            colorTween = null;
        }

        #endregion

        #region Nested Types

        [System.Serializable]
        public struct ButtonVisualState
        {
            [Header("Visual")]
            public Color color;
            [Range(0f, 1f)]
            public float alpha;
            public Vector3 scale;

            [Header("Animation")]
            public float duration;
            public Ease ease;

            public static ButtonVisualState DefaultNormal()
            {
                return new ButtonVisualState
                {
                    color = Color.white,
                    alpha = 1f,
                    scale = Vector3.one,
                    duration = 0.15f,
                    ease = Ease.OutQuad
                };
            }

            public static ButtonVisualState DefaultHover()
            {
                return new ButtonVisualState
                {
                    color = Color.white,
                    alpha = 1f,
                    scale = new Vector3(1.05f, 1.05f, 1f),
                    duration = 0.15f,
                    ease = Ease.OutQuad
                };
            }

            public static ButtonVisualState DefaultPressed()
            {
                return new ButtonVisualState
                {
                    color = Color.white,
                    alpha = 0.9f,
                    scale = new Vector3(0.95f, 0.95f, 1f),
                    duration = 0.08f,
                    ease = Ease.OutQuad
                };
            }
        }

        #endregion
    }
}

