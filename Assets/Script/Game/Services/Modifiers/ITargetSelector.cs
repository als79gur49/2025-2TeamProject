using System.Collections.Generic;
using UnityEngine;
using Game.Data.Modifiers;

namespace Game.Services.Modifiers
{
    /// <summary>
    /// 타겟 선택 서비스 인터페이스
    /// Modifier의 타겟팅 로직을 분리하여 재사용 가능하게 함
    /// </summary>
    public interface ITargetSelector
    {
        /// <summary>
        /// 지정된 파라미터에 맞는 타겟 타일 목록 반환
        /// </summary>
        /// <param name="origin">시작 위치</param>
        /// <param name="parameters">타겟팅 파라미터</param>
        /// <param name="teamComponent">팀 정보 (적/아군 판별용)</param>
        /// <returns>유효한 타겟 타일 목록</returns>
        List<Tile> FindTargets(Vector2Int origin, TargetingParams parameters, Game.Interfaces.ITeamComponent teamComponent);
    }

    /// <summary>
    /// Modifier 타입에 따라 적절한 타겟 선택기를 제공하는 프로바이더
    /// </summary>
    public interface ITargetSelectorProvider
    {
        /// <summary>
        /// 주어진 Modifier 타입에 대한 타겟 선택기를 반환
        /// </summary>
        /// <param name="modifierType">Modifier 타입</param>
        /// <returns>해당 타입에 대한 타겟 선택기</returns>
        ITargetSelector GetSelector(ModifierType modifierType);
    }
}
