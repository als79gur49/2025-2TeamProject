using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Game.UI.Feedback
{
    public class DropdownValueChangeTween : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private Transform targetTransform;

        [Header("Punch Settings")]
        [SerializeField] private Vector3 punchScale = new Vector3(0.1f, 0.1f, 0f);
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private int vibrato = 8;
        [SerializeField] private float elasticity = 1f;
        [SerializeField] private Ease ease = Ease.OutElastic;

        private Tween currentTween;

        private void Awake()
        {
            if (dropdown == null)
                dropdown = GetComponent<TMP_Dropdown>();

            if (targetTransform == null)
                targetTransform = transform;
        }

        private void OnEnable()
        {
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void OnDisable()
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);

            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
                currentTween = null;
            }
        }

        private void OnDropdownValueChanged(int index)
        {
            if (targetTransform == null)
                return;

            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }

            currentTween = targetTransform
                .DOPunchScale(punchScale, duration, vibrato, elasticity)
                .SetEase(ease);
        }
    }
}

