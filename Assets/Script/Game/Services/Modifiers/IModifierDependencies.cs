using Game.Interfaces;
using Game.Services.Modifiers;
using UnityEngine;

namespace Game.Services.Modifiers
{
    /// <summary>
    /// Modifier가 필요로 하는 모든 의존성을 캡슐화하는 인터페이스
    /// Dependency Injection Container 역할
    /// </summary>
    public interface IModifierDependencies
    {
        /// <summary>
        /// 그리드 관리자
        /// </summary>
        IGridManager GridManager { get; }

        /// <summary>
        /// 타겟 선택기 프로바이더
        /// </summary>
        ITargetSelectorProvider TargetSelectorProvider { get; }

        /// <summary>
        /// 전투 계산 서비스 (데미지 계산 등)
        /// </summary>
        ICombatCalculator CombatCalculator { get; }
    }

    /// <summary>
    /// 전투 계산 서비스 인터페이스
    /// </summary>
    public interface ICombatCalculator
    {
        /// <summary>
        /// 원시 데미지 계산 (기본 데미지 + 수정값)
        /// 방어력 및 기타 효과는 CombatComponent 또는 HealthComponent에서 처리
        /// </summary>
        /// <param name="baseDamage">기본 데미지</param>
        /// <param name="damageModifier">데미지 수정값</param>
        /// <returns>계산된 원시 데미지</returns>
        int CalculateDamage(int baseDamage, int damageModifier);
    }
}
