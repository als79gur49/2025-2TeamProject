using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data.Modifiers;
using Game.Interfaces;
using Game.Services.Modifiers;
using Game.Components.Abilities;
using Game.Core;

namespace Game.Core.Modifiers
{
    /// <summary>
    /// Modifier 생성 팩토리
    /// ModifierConfig로부터 실제 IActionModifier 인스턴스를 생성
    /// Type Registry 패턴으로 확장성 확보
    /// </summary>
    public class ModifierFactory : IModifierFactory
    {
        private readonly IModifierDependencies dependencies;
        private readonly Dictionary<ModifierType, Func<ModifierConfig, Unit, IActionModifier>> creators;

        public ModifierFactory(IModifierDependencies dependencies)
        {
            this.dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            creators = new Dictionary<ModifierType, Func<ModifierConfig, Unit, IActionModifier>>();
            RegisterCreators();
        }

        /// <summary>
        /// 모든 Modifier 타입의 생성 로직 등록
        /// </summary>
        private void RegisterCreators()
        {
            // 공격 Modifiers
            creators[ModifierType.RangedAttack] = (config, owner) =>
                new RangedModifier(owner, config, dependencies);

            creators[ModifierType.MeleeAttack] = (config, owner) =>
                new MeleeModifier(owner, config, dependencies);

            creators[ModifierType.SniperAttack] = (config, owner) =>
                new SniperModifier(owner, config, dependencies);

            // 이동 Modifiers
            creators[ModifierType.NormalMovement] = (config, owner) =>
                new NormalMoveModifier(owner, config, dependencies);

            creators[ModifierType.BoosterMovement] = (config, owner) =>
                new BoosterModifier(owner, config, dependencies);
        }

        /// <summary>
        /// ModifierConfig로부터 Modifier 인스턴스 생성
        /// </summary>
        public IActionModifier Create(ModifierConfig config, Unit owner)
        {
            if (owner == null)
            {
                Debug.LogError("[ModifierFactory] Owner cannot be null");
                return null;
            }

            if (!config.IsValid())
            {
                Debug.LogError($"[ModifierFactory] Invalid config: {config}");
                return null;
            }

            if (!creators.TryGetValue(config.Type, out var creator))
            {
                Debug.LogError($"[ModifierFactory] No creator registered for type: {config.Type}");
                return null;
            }

            try
            {
                var modifier = creator(config, owner);
                Debug.Log($"[ModifierFactory] Created modifier: {config.Name} for {owner.name}");
                return modifier;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModifierFactory] Failed to create modifier {config.Name}: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// ModifierData ScriptableObject로부터 직접 생성
        /// </summary>
        public IActionModifier CreateFromData(ModifierData data, Unit owner)
        {
            if (data == null)
            {
                Debug.LogError("[ModifierFactory] ModifierData cannot be null");
                return null;
            }

            var config = data.ToConfig();
            return Create(config, owner);
        }

        /// <summary>
        /// 여러 ModifierData로부터 Modifier 리스트 생성
        /// </summary>
        public List<IActionModifier> CreateMultiple(List<ModifierData> dataList, Unit owner)
        {
            var modifiers = new List<IActionModifier>();

            if (dataList == null || dataList.Count == 0)
                return modifiers;

            foreach (var data in dataList)
            {
                if (data == null) continue;

                var modifier = CreateFromData(data, owner);
                if (modifier != null)
                {
                    modifiers.Add(modifier);
                }
            }

            return modifiers;
        }
    }

    /// <summary>
    /// Modifier 팩토리 인터페이스
    /// </summary>
    public interface IModifierFactory
    {
        /// <summary>
        /// Config로부터 Modifier 생성
        /// </summary>
        IActionModifier Create(ModifierConfig config, Unit owner);

        /// <summary>
        /// ModifierData로부터 Modifier 생성
        /// </summary>
        IActionModifier CreateFromData(ModifierData data, Unit owner);

        /// <summary>
        /// 여러 ModifierData로부터 Modifier 리스트 생성
        /// </summary>
        List<IActionModifier> CreateMultiple(List<ModifierData> dataList, Unit owner);
    }
}
