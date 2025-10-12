using UnityEngine;

namespace Game.Interfaces
{
    /// <summary>
    /// 팀별 설정(Material, 색상, 이펙트 등)을 관리하는 인터페이스
    /// TeamType에 따른 시각적 표현과 관련된 모든 설정을 중앙에서 관리합니다.
    /// </summary>
    public interface ITeamConfigurationManager
    {
        /// <summary>
        /// 팀 타입에 따른 Material을 반환합니다.
        /// </summary>
        /// <param name="teamType">팀 타입 (Player 또는 Enemy)</param>
        /// <returns>해당 팀의 Material, null이면 적용하지 않음</returns>
        Material GetTeamMaterial(TeamType teamType);

        /// <summary>
        /// 유닛에 팀별 Material을 적용합니다.
        /// 유닛의 자식 오브젝트에 있는 모든 Renderer에 Material을 적용합니다.
        /// </summary>
        /// <param name="unit">Material을 적용할 유닛</param>
        /// <param name="teamType">팀 타입 (Player 또는 Enemy)</param>
        void ApplyTeamMaterialToUnit(Unit unit, TeamType teamType);

        /// <summary>
        /// 초기화 여부를 반환합니다.
        /// </summary>
        bool IsInitialized { get; }
    }
}
