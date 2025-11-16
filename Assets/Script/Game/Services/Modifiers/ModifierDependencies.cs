using UnityEngine;
using Game.Interfaces;
using Game.Core;

namespace Game.Services.Modifiers
{
    /// <summary>
    /// IModifierDependencies의 기본 구현
    /// Modifier에 필요한 모든 의존성을 제공
    /// </summary>
    public class ModifierDependencies : IModifierDependencies
    {
        public IGridManager GridManager { get; private set; }
        public ITargetSelectorProvider TargetSelectorProvider { get; private set; }
        public ICombatCalculator CombatCalculator { get; private set; }

        public ModifierDependencies(
            IGridManager gridManager,
            ITargetSelectorProvider targetSelectorProvider,
            ICombatCalculator combatCalculator)
        {
            GridManager = gridManager;
            TargetSelectorProvider = targetSelectorProvider;
            CombatCalculator = combatCalculator;
        }
    }

    /// <summary>
    /// 기본 전투 계산기
    /// 단순한 데미지 계산만 수행 (baseDamage + modifier)
    /// </summary>
    public class DefaultCombatCalculator : ICombatCalculator
    {
        public int CalculateDamage(int baseDamage, int damageModifier)
        {
            // 순수 데미지 계산 - 방어력 및 효과는 별도 처리
            return baseDamage + damageModifier;
        }
    }
}
