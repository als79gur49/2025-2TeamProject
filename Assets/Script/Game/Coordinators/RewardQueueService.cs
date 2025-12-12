using System.Collections.Generic;
using UnityEngine;

namespace Game.Coordinators
{
    /// <summary>
    /// 전역 보상 큐 서비스
    /// - 도메인 계층에서 발생한 카드팩 보상 이벤트를 큐에 쌓고
    /// - UI 계층에 순차적으로 프레젠테이션 요청을 전달
    /// - UI에서 프레젠테이션 완료 신호를 받으면 다음 보상을 처리
    ///
    /// 모든 상호작용은 ScriptableObject 이벤트 채널을 통해 이루어져
    /// 도메인, 전역 서비스, UI 간 결합도를 최소화한다.
    /// </summary>
    public class RewardQueueService : MonoBehaviour
    {
        [Header("Card Pack Event Channels")]
        [SerializeField] private CardPackRewardEventChannelSO cardPackRewardChannel;
        [SerializeField] private CardPackPresentationRequestEventChannelSO cardPackPresentationRequestChannel;
        [SerializeField] private CardPackPresentationFinishedEventChannelSO cardPackPresentationFinishedChannel;

        private readonly Queue<CardPackPresentationData> _cardPackQueue = new Queue<CardPackPresentationData>();
        private bool _isPlayingCardPack;

        #region Unity Lifecycle

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        private void OnEnable()
        {
            if (cardPackRewardChannel != null)
            {
                cardPackRewardChannel.Subscribe(OnCardPackRewardRaised);
            }

            if (cardPackPresentationFinishedChannel != null)
            {
                cardPackPresentationFinishedChannel.Subscribe(OnCardPackPresentationFinished);
            }
        }

        private void OnDisable()
        {
            if (cardPackRewardChannel != null)
            {
                cardPackRewardChannel.Unsubscribe(OnCardPackRewardRaised);
            }

            if (cardPackPresentationFinishedChannel != null)
            {
                cardPackPresentationFinishedChannel.Unsubscribe(OnCardPackPresentationFinished);
            }
        }

        /// <summary>
        /// 도메인 계층에서 카드팩 보상 이벤트가 발생했을 때 호출되는 콜백
        /// </summary>
        private void OnCardPackRewardRaised(CardPackPresentationData data)
        {
            if (data == null || data.result == null)
            {
                Debug.LogWarning("[RewardQueueService] Received invalid CardPackPresentationData");
                return;
            }

            _cardPackQueue.Enqueue(data);

            if (!_isPlayingCardPack)
            {
                TryPlayNextCardPack();
            }
        }

        /// <summary>
        /// 카드팩 프레젠테이션이 모두 끝났을 때 호출되는 콜백
        /// </summary>
        private void OnCardPackPresentationFinished()
        {
            _isPlayingCardPack = false;
            TryPlayNextCardPack();
        }

        /// <summary>
        /// 큐에 쌓인 다음 카드팩 보상을 UI 계층에 전달
        /// </summary>
        private void TryPlayNextCardPack()
        {
            if (_cardPackQueue.Count == 0)
            {
                return;
            }

            // 프레젠테이션 리스너가 없으면 모든 보상을 스킵 (애니메이션 없이 통과)
            if (cardPackPresentationRequestChannel == null ||
                cardPackPresentationRequestChannel.GetListenerCount() == 0)
            {
                Debug.Log("[RewardQueueService] No listeners for CardPackPresentationRequestChannel - skipping visual presentation");
                _cardPackQueue.Clear();
                _isPlayingCardPack = false;
                return;
            }

            var next = _cardPackQueue.Dequeue();
            _isPlayingCardPack = true;

            cardPackPresentationRequestChannel.RaiseEvent(next);
        }
    }
}
