using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// 점수 계산 서비스 인터페이스
    /// StageDataSO의 ScoringSettings를 기반으로 점수를 계산
    /// </summary>
    public interface IScoringService
    {
        /// <summary>
        /// ScoringService 초기화
        /// </summary>
        /// <param name="stageData">스테이지 데이터 (점수 계산 규칙 포함)</param>
        void Initialize(StageDataSO stageData);

        /// <summary>
        /// 적 처치 점수 계산
        /// </summary>
        /// <param name="enemyLevel">적 레벨</param>
        /// <returns>계산된 점수</returns>
        int CalculateEnemyScore(int enemyLevel);

        /// <summary>
        /// 콤보 보너스 점수 계산
        /// </summary>
        /// <param name="comboCount">콤보 횟수</param>
        /// <returns>계산된 보너스 점수</returns>
        int CalculateComboBonus(int comboCount);

        /// <summary>
        /// 턴 보너스 점수 계산
        /// </summary>
        /// <param name="turnCount">클리어 턴 수 (0-indexed)</param>
        /// <returns>계산된 턴 보너스 점수</returns>
        int CalculateTurnBonus(int turnCount);

        /// <summary>
        /// 체력 보너스 점수 계산
        /// </summary>
        /// <param name="healthPercent">남은 체력 퍼센트 (0~100)</param>
        /// <returns>계산된 보너스 점수</returns>
        int CalculateNoDamageBonus(float healthPercent);
    }
}
