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
        /// 시간 보너스 점수 계산
        /// </summary>
        /// <param name="clearTime">클리어 시간 (초)</param>
        /// <param name="targetTime">목표 시간 (초)</param>
        /// <returns>계산된 시간 보너스 점수</returns>
        int CalculateTimeBonus(float clearTime, float targetTime);

        /// <summary>
        /// 노데미지 보너스 점수 계산
        /// </summary>
        /// <param name="baseScore">기본 점수</param>
        /// <returns>계산된 보너스 점수</returns>
        int CalculateNoDamageBonus(int baseScore);
    }
}
