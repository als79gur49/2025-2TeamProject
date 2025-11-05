using UnityEngine;
using Game.Interfaces;
using Game.Components;
using Game.Components.Abilities;

namespace Game
{
    /// <summary>
    /// 예제 팩토리 클래스 - 새로운 Action System을 사용하는 다양한 유닛 구성 예제
    /// </summary>
    public static class UnitFactory
    {
        /// <summary>
        /// 일반 유닛 생성 예제
        /// - 공격: Sniper -> Ranged -> Melee (우선순위 순)
        /// - 이동: Normal Move
        /// </summary>
        public static void SetupNormalUnit(Unit unit)
        {
            // Attack Modifiers
            var sniperModifier = new SniperModifier(unit, priority: 100);
            var rangedModifier = new RangedModifier(unit, range: 3, priority: 90);
            var meleeModifier = new MeleeModifier(unit, priority: 80);

            unit.AddActionModifier(sniperModifier);
            unit.AddActionModifier(rangedModifier);
            unit.AddActionModifier(meleeModifier);

            // Movement Modifiers
            var normalMoveModifier = new NormalMoveModifier(unit, priority: 10);
            unit.AddActionModifier(normalMoveModifier);

            Debug.Log($"[UnitFactory] NormalUnit setup completed for {unit.name}");
        }

        /// <summary>
        /// 기동형 유닛 생성 예제
        /// - 공격: RangedContinue (실패 시 다음 수정자로 계속 진행)
        /// - 이동: Normal Move
        /// </summary>
        public static void SetupMobileUnit(Unit unit)
        {
            // Attack Modifiers (AlwaysContinue 동작)
            var rangedContinueModifier = new RangedContinueModifier(unit, range: 3, priority: 90);
            unit.AddActionModifier(rangedContinueModifier);

            // Movement Modifiers
            var normalMoveModifier = new NormalMoveModifier(unit, priority: 10);
            unit.AddActionModifier(normalMoveModifier);

            Debug.Log($"[UnitFactory] MobileUnit setup completed for {unit.name}");
        }

        /// <summary>
        /// 부스터 유닛 생성 예제
        /// - 공격: Ranged
        /// - 이동: Booster (방향으로 계속 이동)
        /// </summary>
        public static void SetupBoosterUnit(Unit unit)
        {
            // Attack Modifiers
            var rangedModifier = new RangedModifier(unit, range: 3, priority: 90);
            unit.AddActionModifier(rangedModifier);

            // Movement Modifiers (우선순위가 더 높아서 먼저 평가됨)
            var boosterModifier = new BoosterModifier(unit, priority: 20);
            var normalMoveModifier = new NormalMoveModifier(unit, priority: 10);

            unit.AddActionModifier(boosterModifier);
            unit.AddActionModifier(normalMoveModifier);

            Debug.Log($"[UnitFactory] BoosterUnit setup completed for {unit.name}");
        }

        /// <summary>
        /// 광역 공격 유닛 생성 예제
        /// - 공격: Ranged + Melee with Splash Effect + Stun Effect
        /// - 이동: Normal Move
        /// </summary>
        public static void SetupSplashUnit(Unit unit)
        {
            // Attack Modifiers
            var rangedModifier = new RangedModifier(unit, range: 3, priority: 90);
            var meleeModifier = new MeleeModifier(unit, priority: 80);

            unit.AddActionModifier(rangedModifier);
            unit.AddActionModifier(meleeModifier);

            // Attack Effects (CombatComponent를 통해 등록)
            var combatComponent = unit.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                var splashEffect = new SplashEffect(unit, damage: 3, priority: 10);
                var stunEffect = new StunEffect(unit, duration: 1, priority: 5);

                combatComponent.AddAttackEffect(splashEffect);
                combatComponent.AddAttackEffect(stunEffect);
            }

            // Movement Modifiers
            var normalMoveModifier = new NormalMoveModifier(unit, priority: 10);
            unit.AddActionModifier(normalMoveModifier);

            Debug.Log($"[UnitFactory] SplashUnit setup completed for {unit.name}");
        }

        /// <summary>
        /// 커스텀 유닛 예제 - 직접 수정자를 구성하는 방법
        /// </summary>
        public static void SetupCustomUnit(Unit unit,
            IActionModifier[] attackModifiers,
            IActionModifier[] movementModifiers,
            IAttackEffect[] attackEffects = null)
        {
            // Attack Modifiers 등록
            if (attackModifiers != null)
            {
                foreach (var modifier in attackModifiers)
                {
                    unit.AddActionModifier(modifier);
                }
            }

            // Movement Modifiers 등록
            if (movementModifiers != null)
            {
                foreach (var modifier in movementModifiers)
                {
                    unit.AddActionModifier(modifier);
                }
            }

            // Attack Effects 등록
            if (attackEffects != null)
            {
                var combatComponent = unit.GetComponent<CombatComponent>();
                if (combatComponent != null)
                {
                    foreach (var effect in attackEffects)
                    {
                        combatComponent.AddAttackEffect(effect);
                    }
                }
            }

            Debug.Log($"[UnitFactory] CustomUnit setup completed for {unit.name}");
        }
    }
}
