using UnityEngine;
using System.Collections;
using Game.Data;

namespace Game.UI.Feedback
{
    /// <summary>
    /// 카드 전송 시 시각/청각 피드백 제공
    /// 인벤토리 ↔ 덱 간 카드 이동 시 파티클, 사운드, 애니메이션 재생
    /// </summary>
    public class CardTransferFeedback : MonoBehaviour
    {
        [Header("Particle Effects")]
        [SerializeField] private ParticleSystem addToDeckParticle;
        [SerializeField] private ParticleSystem removeFromDeckParticle;
        [SerializeField] private Color addColor = Color.green;
        [SerializeField] private Color removeColor = Color.red;

        [Header("Sound Effects")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip addCardSound;
        [SerializeField] private AudioClip removeCardSound;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

        [Header("Animation Settings")]
        [SerializeField] private float pulseDuration = 0.3f;
        [SerializeField] private float pulseScale = 1.2f;

        #region Lifecycle

        private void Awake()
        {
            // AudioSource 자동 생성
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = volume;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 덱에 카드 추가 피드백
        /// </summary>
        /// <param name="position">파티클 생성 위치</param>
        public void PlayAddToDeckFeedback(Vector3 position)
        {
            // 파티클 효과
            PlayParticleEffect(addToDeckParticle, position, addColor);

            // 사운드 효과
            PlaySound(addCardSound);

            Debug.Log("[CardTransferFeedback] Played add to deck feedback");
        }

        /// <summary>
        /// 덱에서 카드 제거 피드백
        /// </summary>
        /// <param name="position">파티클 생성 위치</param>
        public void PlayRemoveFromDeckFeedback(Vector3 position)
        {
            // 파티클 효과
            PlayParticleEffect(removeFromDeckParticle, position, removeColor);

            // 사운드 효과
            PlaySound(removeCardSound);

            Debug.Log("[CardTransferFeedback] Played remove from deck feedback");
        }

        /// <summary>
        /// 카드 UI 펄스 애니메이션
        /// </summary>
        /// <param name="cardTransform">애니메이션 대상 Transform</param>
        public void PlayCardPulseAnimation(Transform cardTransform)
        {
            if (cardTransform != null)
            {
                StartCoroutine(PulseAnimation(cardTransform));
            }
        }

        /// <summary>
        /// 드롭 성공 피드백 (종합)
        /// </summary>
        /// <param name="cardTransform">카드 Transform</param>
        /// <param name="isAddingToDeck">덱에 추가 여부</param>
        public void PlayDropSuccessFeedback(Transform cardTransform, bool isAddingToDeck)
        {
            if (cardTransform == null) return;

            Vector3 position = cardTransform.position;

            if (isAddingToDeck)
            {
                PlayAddToDeckFeedback(position);
            }
            else
            {
                PlayRemoveFromDeckFeedback(position);
            }

            PlayCardPulseAnimation(cardTransform);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 파티클 효과 재생
        /// </summary>
        private void PlayParticleEffect(ParticleSystem particleSystem, Vector3 position, Color color)
        {
            if (particleSystem == null) return;

            // 파티클 위치 설정
            particleSystem.transform.position = position;

            // 파티클 색상 설정
            var main = particleSystem.main;
            main.startColor = color;

            // 파티클 재생
            particleSystem.Play();
        }

        /// <summary>
        /// 사운드 효과 재생
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;

            audioSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// 펄스 애니메이션 코루틴
        /// </summary>
        private IEnumerator PulseAnimation(Transform target)
        {
            Vector3 originalScale = target.localScale;
            Vector3 targetScale = originalScale * pulseScale;

            float elapsed = 0f;
            float halfDuration = pulseDuration / 2f;

            // 확대
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                target.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;

            // 축소
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                target.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            // 원래 크기로 복원 (부동소수점 오차 보정)
            target.localScale = originalScale;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 테스트용 메서드
        /// </summary>
        [ContextMenu("Test Add Feedback")]
        private void TestAddFeedback()
        {
            PlayAddToDeckFeedback(transform.position);
        }

        [ContextMenu("Test Remove Feedback")]
        private void TestRemoveFeedback()
        {
            PlayRemoveFromDeckFeedback(transform.position);
        }

        [ContextMenu("Test Pulse Animation")]
        private void TestPulseAnimation()
        {
            PlayCardPulseAnimation(transform);
        }
#endif

        #endregion
    }
}
