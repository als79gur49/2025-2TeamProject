using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Panels
{
    /// <summary>
    /// 적 유닛 소환 카드 아이콘 표시용 UI 컴포넌트.
    /// Image를 직렬화하여 직접 참조함으로써 GetComponent 검색 없이 안전하게 스프라이트를 설정합니다.
    /// </summary>
    public class EnemyCardIconUI : MonoBehaviour
    {
        [Header("Icon")]
        [SerializeField]
        private Image iconImage;

        /// <summary>
        /// 아이콘 스프라이트를 설정합니다.
        /// </summary>
        public void SetIcon(Sprite sprite)
        {
            if (iconImage == null)
            {
                Debug.LogWarning("[EnemyCardIconUI] Icon Image is not assigned.");
                return;
            }

            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }

        /// <summary>
        /// 아이콘을 비웁니다.
        /// </summary>
        public void ClearIcon()
        {
            if (iconImage == null)
                return;

            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }
}

