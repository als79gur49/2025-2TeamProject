using System;
using System.Collections.Generic;
using Game.Data.Modifiers;

namespace Game.Services.Modifiers.TargetSelectors
{
    /// <summary>
    /// ModifierType에 따라 적절한 ITargetSelector를 반환하는 프로바이더
    /// </summary>
    public class TargetSelectorProvider : ITargetSelectorProvider
    {
        private readonly Dictionary<ModifierType, ITargetSelector> selectorMap;
        private readonly ITargetSelector defaultSelector;

        public TargetSelectorProvider(
            ITargetSelector rangedSelector,
            ITargetSelector meleeSelector,
            ITargetSelector movementSelector,
            ITargetSelector nexusSelector,
            ITargetSelector directionalSelector)
        {
            selectorMap = new Dictionary<ModifierType, ITargetSelector>
            {
                { ModifierType.RangedAttack, rangedSelector },
                { ModifierType.MeleeAttack, meleeSelector },
                { ModifierType.SniperAttack, nexusSelector },
                { ModifierType.BishopAttack, directionalSelector },
                { ModifierType.QueenAttack, directionalSelector },
                { ModifierType.NormalMovement, movementSelector },
                { ModifierType.BoosterMovement, movementSelector }
            };

            // 기본값은 원거리 공격용 선택기를 사용
            defaultSelector = rangedSelector;
        }

        public ITargetSelector GetSelector(ModifierType modifierType)
        {
            if (selectorMap.TryGetValue(modifierType, out var selector) && selector != null)
            {
                return selector;
            }

            return defaultSelector ?? throw new InvalidOperationException(
                $"[TargetSelectorProvider] No selector registered for modifier type: {modifierType}");
        }
    }
}
