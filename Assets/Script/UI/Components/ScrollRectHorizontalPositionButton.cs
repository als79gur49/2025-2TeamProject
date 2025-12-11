using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Components
{
    public enum ScrollAxis
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// 버튼 클릭 시 ScrollRect의 normalizedPosition을
    /// 인스펙터에서 설정한 0~1 값으로 이동시키는 유틸리티 컴포넌트
    /// - GridLayoutGroup, Vertical/Horizontal LayoutGroup 모두 사용 가능
    /// </summary>
    public class ScrollRectHorizontalPositionButton : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private ScrollRect targetScrollRect;

        [Header("Axis")]
        [SerializeField] private ScrollAxis axis = ScrollAxis.Vertical;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)]
        private float targetNormalizedPosition = 0f;

        /// <summary>
        /// Button.onClick에 연결해서 사용
        /// </summary>
        public void ApplyPosition()
        {
            if (targetScrollRect == null)
            {
                Debug.LogWarning("[ScrollRectHorizontalPositionButton] Target ScrollRect is not assigned");
                return;
            }

            float clamped = Mathf.Clamp01(targetNormalizedPosition);

            if (axis == ScrollAxis.Horizontal)
            {
                targetScrollRect.horizontalNormalizedPosition = clamped;
            }
            else
            {
                targetScrollRect.verticalNormalizedPosition = clamped;
            }
        }

        /// <summary>
        /// 코드에서 직접 값 지정 후 스크롤 이동이 필요할 때 사용
        /// </summary>
        public void SetPosition(float value)
        {
            targetNormalizedPosition = Mathf.Clamp01(value);
            ApplyPosition();
        }
    }
}
