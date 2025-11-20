using UnityEngine;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// 점수 계산 서비스 구현체
    /// StageDataSO의 ScoringSettings를 기반으로 실제 점수 계산 수행
    /// </summary>
    public class ScoringService : IScoringService
    {
        private StageDataSO stageData;
        private bool isInitialized;

        /// <summary>
        /// ScoringService 초기화
        /// </summary>
        /// <param name="stageData">스테이지 데이터 (점수 계산 규칙 포함)</param>
        public void Initialize(StageDataSO stageData)
        {
            if (stageData == null)
            {
                Debug.LogError("[ScoringService] StageData is null");
                return;
            }

            this.stageData = stageData;
            isInitialized = true;

            Debug.Log($"[ScoringService] Initialized for stage: {stageData.StageId}");
        }

        /// <summary>
        /// 적 처치 점수 계산
        /// </summary>
        /// <param name="enemyLevel">적 레벨</param>
        /// <returns>계산된 점수</returns>
        public int CalculateEnemyScore(int enemyLevel)
        {
            if (!ValidateInitialization()) return 0;

            // 기본 점수 * 레벨
            int baseScore = 100; // TODO: StageDataSO에 baseEnemyScore 추가 필요 시 사용
            int score = baseScore * enemyLevel;

            return Mathf.Max(0, score);
        }

        /// <summary>
        /// 콤보 보너스 점수 계산
        /// </summary>
        /// <param name="comboCount">콤보 횟수</param>
        /// <returns>계산된 보너스 점수</returns>
        public int CalculateComboBonus(int comboCount)
        {
            if (!ValidateInitialization()) return 0;

            // 콤보 * 기본 점수 * 배율
            float baseComboScore = 100f;
            float multiplier = stageData.Scoring.comboMultiplier;
            float bonus = baseComboScore * comboCount * multiplier;

            return Mathf.RoundToInt(Mathf.Max(0, bonus));
        }

        /// <summary>
        /// 턴 보너스 점수 계산 (등급 기반)
        /// </summary>
        /// <param name="turnCount">클리어 턴 수 (0-indexed)</param>
        /// <returns>계산된 턴 보너스 점수</returns>
        public int CalculateTurnBonus(int turnCount)
        {
            if (!ValidateInitialization()) return 0;

            var turnTiers = stageData.Scoring.turnBonusTiers;
            if (turnTiers == null || turnTiers.Length == 0)
            {
                Debug.LogWarning("[ScoringService] Turn bonus tiers not configured");
                return 0;
            }

            // 가장 높은 등급부터 확인 (턴 수 오름차순 정렬 가정)
            for (int i = 0; i < turnTiers.Length; i++)
            {
                if (turnCount <= turnTiers[i].turnCount)
                {
                    Debug.Log($"[ScoringService] Turn bonus: {turnTiers[i].bonusScore} points " +
                              $"(cleared in {turnCount} turns, threshold: ≤{turnTiers[i].turnCount})");
                    return turnTiers[i].bonusScore;
                }
            }

            // 어떤 등급도 만족하지 못함
            Debug.Log($"[ScoringService] No turn bonus (cleared in {turnCount} turns)");
            return 0;
        }

        /// <summary>
        /// 체력 보너스 점수 계산 (등급 기반)
        /// </summary>
        /// <param name="healthPercent">남은 체력 퍼센트 (0~100)</param>
        /// <returns>계산된 보너스 점수</returns>
        public int CalculateNoDamageBonus(float healthPercent)
        {
            if (!ValidateInitialization()) return 0;

            var healthTiers = stageData.Scoring.healthBonusTiers;
            if (healthTiers == null || healthTiers.Length == 0) return 0;

            // 가장 높은 등급부터 확인 (체력 내림차순 정렬 가정)
            for (int i = 0; i < healthTiers.Length; i++)
            {
                if (healthPercent >= healthTiers[i].healthPercent)
                {
                    return healthTiers[i].bonusScore;
                }
            }

            // 어떤 등급도 만족하지 못함
            return 0;
        }

        /// <summary>
        /// 초기화 상태 검증
        /// </summary>
        private bool ValidateInitialization()
        {
            if (!isInitialized || stageData == null)
            {
                Debug.LogWarning("[ScoringService] Not initialized. Call Initialize() first.");
                return false;
            }

            return true;
        }
    }
}
