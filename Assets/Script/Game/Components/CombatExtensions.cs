using UnityEngine;
using Game.Interfaces;
using Game.Data;

namespace Game.Components
{
    /// <summary>
    /// 전투 시스템 확장 메서드
    /// </summary>
    public static class CombatExtensions
    {
        /// <summary>
        /// 유효한 공격 대상인지 확인
        /// </summary>
        public static bool IsValidTarget(this ICombatSystem combat, GameObject target)
        {
            if (target == null) return false;
            
            // 살아있는지 확인
            var health = target.GetComponent<IHealthComponent>();
            if (health == null || !health.IsAlive) return false;
            
            // 자기 자신이 아닌지 확인
            if (combat is MonoBehaviour combatMono && combatMono.gameObject == target)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// 팀 기준으로 공격 가능한지 확인
        /// </summary>
        public static bool CanAttackByTeam(this ICombatSystem combat, GameObject target)
        {
            if (!(combat is MonoBehaviour combatMono)) return false;
            
            var myTeam = combatMono.GetComponent<ITeamComponent>();
            var targetTeam = target.GetComponent<ITeamComponent>();
            
            // 팀 컴포넌트가 없으면 공격 가능 (기본 동작)
            if (myTeam == null || targetTeam == null) return true;
            
            // 같은 팀이면 공격 불가
            return myTeam.Team != targetTeam.Team;
        }
        
        /// <summary>
        /// 크리티컬 판정
        /// </summary>
        public static bool RollCritical(this ICombatSystem combat)
        {
            if (combat is IAdvancedCombatSystem advanced)
            {
                return Random.Range(0f, 1f) <= advanced.CriticalChance;
            }
            return false;
        }
        
        /// <summary>
        /// 최종 피해량 계산
        /// </summary>
        public static int CalculateFinalDamage(this ICombatSystem combat, int baseDamage, bool isCritical)
        {
            if (isCritical && combat is IAdvancedCombatSystem advanced)
            {
                return Mathf.RoundToInt(baseDamage * advanced.CriticalMultiplier);
            }
            return baseDamage;
        }
        
        /// <summary>
        /// 고급 크리티컬 판정 (IAdvancedCombatSystem 전용)
        /// </summary>
        public static bool RollCritical(this IAdvancedCombatSystem advanced)
        {
            return Random.Range(0f, 1f) <= advanced.CriticalChance;
        }
        
        /// <summary>
        /// 고급 최종 피해량 계산 (IAdvancedCombatSystem 전용)
        /// </summary>
        public static int CalculateFinalDamage(this IAdvancedCombatSystem advanced, int baseDamage, bool isCritical)
        {
            if (isCritical)
            {
                return Mathf.RoundToInt(baseDamage * advanced.CriticalMultiplier);
            }
            return baseDamage;
        }
    }
}