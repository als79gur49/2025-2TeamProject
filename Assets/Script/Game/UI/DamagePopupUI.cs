using UnityEngine;
using TMPro;
using Game.Data;
using DG.Tweening;

namespace Game.UI
{
    /// <summary>
    /// 데미지를 표시하는 떠오르는 텍스트 UI 컴포넌트
    /// DOTween을 사용한 부드러운 애니메이션 구현
    ///
    /// 기능:
    /// - 카메라 빌보드 (항상 카메라를 향함)
    /// - DOTween을 이용한 위로 떠오르는 애니메이션
    /// - DOTween을 이용한 페이드 아웃 효과
    /// - 일반/크리티컬 데미지 구분 표시
    /// - 오브젝트 풀링 지원
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DamagePopupUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [Tooltip("위로 떠오르는 거리 (Unity Units)")]
        [SerializeField] private float floatDistance = 1f;

        [Tooltip("애니메이션 지속 시간 (초)")]
        [SerializeField] private float animationDuration = 1.5f;

        [Tooltip("이동 애니메이션 Easing")]
        [SerializeField] private Ease moveEase = Ease.OutQuad;

        [Tooltip("페이드 애니메이션 Easing")]
        [SerializeField] private Ease fadeEase = Ease.Linear;

        [Header("Visual Settings - Normal Damage")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private float normalFontSize = 36f;

        [Header("Visual Settings - Critical Damage")]
        [SerializeField] private Color criticalColor = Color.yellow;
        [SerializeField] private float criticalFontSize = 54f;

        [Header("Billboard")]
        [Tooltip("카메라를 항상 바라보도록 설정")]
        [SerializeField] private bool enableBillboard = true;

        // Cached references
        private Camera mainCamera;
        private Canvas canvas;
        private Sequence animationSequence;
        private System.Action<DamagePopupUI> onCompleteCallback;

        private void Awake()
        {
            // 메인 카메라 캐싱
            mainCamera = Camera.main;

            // Canvas worldCamera 설정 (World Space용)
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas.worldCamera = mainCamera;
            }

            // CanvasGroup이 할당되지 않은 경우 자동 가져오기
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            // DamageText가 할당되지 않은 경우 자동 찾기
            if (damageText == null)
            {
                damageText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        private void LateUpdate()
        {
            // 빌보드: 항상 카메라를 바라보도록 회전
            if (enableBillboard && mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
        }

        /// <summary>
        /// 데미지 팝업 표시 (DOTween 사용)
        /// </summary>
        /// <param name="data">표시할 데미지 데이터</param>
        /// <param name="onComplete">애니메이션 완료 시 호출될 콜백</param>
        public void Show(DamageDisplayData data, System.Action<DamagePopupUI> onComplete)
        {
            onCompleteCallback = onComplete;

            // 위치 설정 (데미지 발생 위치에서 약간 위로)
            Vector3 startPosition = data.WorldPosition + Vector3.up * 0.5f;
            transform.position = startPosition;

            // 텍스트 설정
            damageText.text = data.Amount.ToString();

            // 크리티컬 여부에 따른 비주얼 설정
            if (data.IsCritical)
            {
                // 크리티컬: 노란색, 큰 폰트, 굵게
                damageText.color = criticalColor;
                damageText.fontSize = criticalFontSize;
                damageText.fontStyle = FontStyles.Bold;
            }
            else
            {
                // 일반: 흰색, 기본 폰트, 일반
                damageText.color = normalColor;
                damageText.fontSize = normalFontSize;
                damageText.fontStyle = FontStyles.Normal;
            }

            // 알파값 초기화
            canvasGroup.alpha = 1f;

            // 오브젝트 활성화
            gameObject.SetActive(true);

            // 기존 애니메이션 중단
            animationSequence?.Kill();

            // DOTween Sequence로 애니메이션 구성
            animationSequence = DOTween.Sequence();

            // 위로 떠오르는 애니메이션 + 페이드 아웃 동시 실행
            animationSequence
                .Append(transform.DOMoveY(startPosition.y + floatDistance, animationDuration)
                    .SetEase(moveEase))
                .Join(canvasGroup.DOFade(0f, animationDuration)
                    .SetEase(fadeEase))
                .OnComplete(OnAnimationComplete);
        }

        /// <summary>
        /// DOTween 애니메이션 완료 콜백
        /// </summary>
        private void OnAnimationComplete()
        {
            // 비활성화 및 풀로 반환
            gameObject.SetActive(false);
            onCompleteCallback?.Invoke(this);
        }

        /// <summary>
        /// 오브젝트 풀링을 위한 초기화
        /// 팝업을 재사용 가능한 상태로 리셋
        /// </summary>
        public void ResetForPool()
        {
            // DOTween 애니메이션 중단 및 정리
            animationSequence?.Kill();
            animationSequence = null;

            // 오브젝트 비활성화
            gameObject.SetActive(false);

            // 알파값 초기화
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            // 콜백 초기화
            onCompleteCallback = null;
        }

        /// <summary>
        /// 파괴 시 DOTween 정리 (메모리 누수 방지)
        /// </summary>
        private void OnDestroy()
        {
            // DOTween Sequence 정리
            animationSequence?.Kill();
            animationSequence = null;
        }

        private void OnValidate()
        {
            // Inspector 값 검증
            floatDistance = Mathf.Max(0.1f, floatDistance);
            animationDuration = Mathf.Max(0.1f, animationDuration);
            normalFontSize = Mathf.Max(10f, normalFontSize);
            criticalFontSize = Mathf.Max(10f, criticalFontSize);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Normal Damage")]
        private void TestNormalDamage()
        {
            var testData = new DamageDisplayData(150, false, transform.position);
            Show(testData, (popup) => Debug.Log("Normal damage animation complete"));
        }

        [ContextMenu("Test Critical Damage")]
        private void TestCriticalDamage()
        {
            var testData = new DamageDisplayData(300, true, transform.position);
            Show(testData, (popup) => Debug.Log("Critical damage animation complete"));
        }
#endif
    }
}
