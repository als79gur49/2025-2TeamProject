using Game.Data;

namespace Game.Managers
{
    /// <summary>
    /// 보상/연출 큐 인터페이스
    /// 상점, 전투 보상 등에서 발생한 보상을 UI 연출 시스템으로 전달
    /// </summary>
    public interface IRewardPresentationQueue
    {
        /// <summary>
        /// 단일 카드 보상 큐잉
        /// </summary>
        void EnqueueSingleCardReward(CardData card);

        /// <summary>
        /// 카드팩 개봉 결과 큐잉
        /// </summary>
        void EnqueueCardPackReward(CardPackOpenResult result);
    }
}

