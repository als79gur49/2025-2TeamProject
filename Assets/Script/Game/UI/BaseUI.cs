using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// BaseUI - 플레이어/적 Base의 HUD(UI)를 담당하는 컴포넌트
    ///
    /// 책임:
    /// - Base HP 텍스트 표시
    /// - 피격 시 HUD 상단에 데미지 텍스트를 잠시 띄우고,
    ///   위로 이동시키면서 페이드 아웃 애니메이션 처리
    ///
    /// 사용 방법:
    /// - Canvas 아래에 HP 텍스트와 데미지 텍스트를 배치한 GameObject에 이 컴포넌트를 부착
    /// - Inspector에서 healthText, damageText를 연결
    /// - BaseManager가 이벤트를 받아서 SetHealth / ShowDamage 를 호출
    /// </summary>
    public class BaseUI : MonoBehaviour
    {
        [Header("Health UI")]
        [SerializeField]
        [Tooltip("Base HP를 표시할 TextMeshProUGUI")]
        private TextMeshProUGUI healthText;

        [Header("Damage Popup UI (HUD)")]
        [SerializeField]
        [Tooltip("피격 데미지를 표시할 TextMeshProUGUI (HUD 상단)")]
        private TextMeshProUGUI damageText;

        [SerializeField]
        [Tooltip("데미지 텍스트의 투명도 제어용 CanvasGroup (없으면 자동 추가)")]
        private CanvasGroup damageCanvasGroup;

        [SerializeField]
        [Tooltip("데미지 텍스트가 위로 이동하는 거리 (anchoredPosition 기준, UI 단위)")]
        private float damageMoveUpDistance = 50f;

        [SerializeField]
        [Tooltip("데미지 텍스트가 표시되고 사라지기까지의 시간 (초)")]
        private float damageDuration = 0.6f;

        // 내부 상태
        private RectTransform damageRectTransform;
        private Vector2 damageInitialAnchoredPosition;
        private Coroutine damageCoroutine;

        private void Awake()
        {
            // 데미지 텍스트 관련 초기화
            if (damageText != null)
            {
                damageRectTransform = damageText.rectTransform;
                damageInitialAnchoredPosition = damageRectTransform.anchoredPosition;

                // CanvasGroup 없으면 자동 추가
                if (damageCanvasGroup == null)
                {
                    damageCanvasGroup = damageText.GetComponent<CanvasGroup>();
                    if (damageCanvasGroup == null)
                    {
                        damageCanvasGroup = damageText.gameObject.AddComponent<CanvasGroup>();
                    }
                }

                HideDamageImmediate();
            }
        }

        /// <summary>
        /// Base HP 텍스트 갱신
        /// </summary>
        public void SetHealth(int currentHP, int maxHP)
        {
            if (healthText == null)
            {
                return;
            }

            // 현재 요구사항: HP 숫자만 표시
            healthText.text = $"{currentHP}";
        }

        /// <summary>
        /// 데미지 팝업 표시
        /// - 처음 위치를 기준으로 위로 이동하면서 페이드 아웃
        /// </summary>
        public void ShowDamage(int amount)
        {
            if (damageText == null || amount <= 0)
            {
                return;
            }

            // 기존 코루틴이 돌고 있으면 중단
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }

            damageCoroutine = StartCoroutine(DamagePopupRoutine(amount));
        }

        /// <summary>
        /// 즉시 데미지 텍스트 숨기기 (초기화 용도)
        /// </summary>
        private void HideDamageImmediate()
        {
            if (damageText == null)
            {
                return;
            }

            damageText.gameObject.SetActive(false);

            if (damageRectTransform != null)
            {
                damageRectTransform.anchoredPosition = damageInitialAnchoredPosition;
            }

            if (damageCanvasGroup != null)
            {
                damageCanvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// 데미지 팝업 코루틴
        /// - HUD 기준으로 위로 이동 + 알파 페이드 아웃
        /// </summary>
        private IEnumerator DamagePopupRoutine(int amount)
        {
            // 시작 상태 설정
            damageText.text = $"{amount}";
            damageText.gameObject.SetActive(true);

            if (damageRectTransform != null)
            {
                damageRectTransform.anchoredPosition = damageInitialAnchoredPosition;
            }

            if (damageCanvasGroup != null)
            {
                damageCanvasGroup.alpha = 1f;
            }

            float elapsed = 0f;

            while (elapsed < damageDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / damageDuration);

                // 위로 이동
                if (damageRectTransform != null)
                {
                    Vector2 offset = Vector2.up * damageMoveUpDistance * t;
                    damageRectTransform.anchoredPosition = damageInitialAnchoredPosition + offset;
                }

                // 페이드 아웃
                if (damageCanvasGroup != null)
                {
                    damageCanvasGroup.alpha = 1f - t;
                }

                yield return null;
            }

            HideDamageImmediate();
            damageCoroutine = null;
        }

        private void OnDestroy()
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }

        private void OnValidate()
        {
            damageMoveUpDistance = Mathf.Max(0f, damageMoveUpDistance);
            damageDuration = Mathf.Max(0.1f, damageDuration);
        }
    }
}

