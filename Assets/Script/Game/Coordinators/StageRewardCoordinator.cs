using UnityEngine;
using Game.Data;
using Game.Managers;
using Game.Core;

namespace Game.Coordinators
{
    /// <summary>
    /// 스테이지 클리어 보상 조율자
    /// 
    /// 책임:
    /// - GameSessionManager.OnSessionEnded 이벤트를 수신
    /// - GameSessionData.outcome, score를 기반으로 StageDataSO의 보상 규칙 적용
    /// - PlayerDataManager를 통해 실제 골드 지급
    ///
    /// 씬 종속 객체로, StageDataSO는 GameInitializer에서 주입한다.
    /// </summary>
    public class StageRewardCoordinator : MonoBehaviour
    {
        private StageDataSO stageData;
        private IPlayerDataManager playerDataManager;
        private IGameSessionManager sessionManager;

        private bool isInitialized;

        /// <summary>
        /// StageContext 및 의존성 초기화
        /// </summary>
        public void Initialize(StageDataSO stageData)
        {
            if (stageData == null)
            {
                Debug.LogError("[StageRewardCoordinator] Initialize called with null StageDataSO");
                return;
            }

            this.stageData = stageData;

            // ServiceLocator를 통해 런타임 매니저 의존성 조회
            if (ServiceLocator.IsRegistered<IPlayerDataManager>())
            {
                playerDataManager = ServiceLocator.Get<IPlayerDataManager>();
            }
            else
            {
                Debug.LogError("[StageRewardCoordinator] IPlayerDataManager not found in ServiceLocator");
            }

            if (ServiceLocator.IsRegistered<IGameSessionManager>())
            {
                sessionManager = ServiceLocator.Get<IGameSessionManager>();
            }
            else
            {
                Debug.LogError("[StageRewardCoordinator] IGameSessionManager not found in ServiceLocator");
            }

            if (sessionManager != null)
            {
                sessionManager.OnSessionEnded += OnSessionEnded;
                isInitialized = true;
                Debug.Log("[StageRewardCoordinator] Initialized and subscribed to OnSessionEnded");
            }
        }

        private void OnDestroy()
        {
            if (isInitialized && sessionManager != null)
            {
                sessionManager.OnSessionEnded -= OnSessionEnded;
            }
        }

        /// <summary>
        /// 세션 종료 이벤트 처리
        /// </summary>
        private void OnSessionEnded(GameSessionData data)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[StageRewardCoordinator] OnSessionEnded called before initialization");
                return;
            }

            if (data == null)
            {
                Debug.LogWarning("[StageRewardCoordinator] Received null GameSessionData");
                return;
            }

            // 패배, 중단, 미정 결과는 보상 지급하지 않음
            if (data.outcome != SessionOutcome.Victory)
            {
                Debug.Log($"[StageRewardCoordinator] Session ended with outcome {data.outcome}, no rewards applied.");
                return;
            }

            if (playerDataManager == null)
            {
                Debug.LogError("[StageRewardCoordinator] PlayerDataManager is null, cannot apply rewards.");
                return;
            }

            if (stageData == null)
            {
                Debug.LogError("[StageRewardCoordinator] StageData is null, cannot calculate rewards.");
                return;
            }

            // 점수 기반 별 계산
            int stars = stageData.CalculateStars(data.score);

            // TODO: 첫 클리어 여부 연동이 필요하다면 StageProgressManager나 추가 요약 정보를 통해 전달
            bool isFirstClear = false;

            var rewardResult = stageData.CalculateRewards(data.score, stars, isFirstClear);

            if (rewardResult != null && rewardResult.coins > 0)
            {
                playerDataManager.AddGold(rewardResult.coins);
                Debug.Log($"[StageRewardCoordinator] Applied rewards: Coins={rewardResult.coins} (Score={data.score}, Stars={stars})");
            }
            else
            {
                Debug.Log("[StageRewardCoordinator] No coin rewards to apply.");
            }
        }
    }
}

