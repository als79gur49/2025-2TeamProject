using System.Collections.Generic;
using Game.Interfaces;

namespace Game.Core
{
    /// <summary>
    /// 팀 관계 매트릭스 - 팀 간 관계 정의 및 조회
    /// </summary>
    public static class TeamRelationMatrix
    {
        private static readonly Dictionary<(TeamType, TeamType), TeamRelation> relations = 
            new Dictionary<(TeamType, TeamType), TeamRelation>
            {
                // ✅ Player 팀 관계
                {(TeamType.Player, TeamType.Player), TeamRelation.Ally},
                {(TeamType.Player, TeamType.Enemy), TeamRelation.Enemy},
                {(TeamType.Player, TeamType.Neutral), TeamRelation.Neutral},
                {(TeamType.Player, TeamType.Ally), TeamRelation.Ally},
                {(TeamType.Player, TeamType.None), TeamRelation.Neutral},
                
                // ✅ Enemy 팀 관계  
                {(TeamType.Enemy, TeamType.Player), TeamRelation.Enemy},
                {(TeamType.Enemy, TeamType.Enemy), TeamRelation.Ally},
                {(TeamType.Enemy, TeamType.Neutral), TeamRelation.Neutral},
                {(TeamType.Enemy, TeamType.Ally), TeamRelation.Enemy},
                {(TeamType.Enemy, TeamType.None), TeamRelation.Neutral},
                
                // ✅ Neutral 팀 관계
                {(TeamType.Neutral, TeamType.Player), TeamRelation.Neutral},
                {(TeamType.Neutral, TeamType.Enemy), TeamRelation.Neutral},
                {(TeamType.Neutral, TeamType.Neutral), TeamRelation.Ally},
                {(TeamType.Neutral, TeamType.Ally), TeamRelation.Neutral},
                {(TeamType.Neutral, TeamType.None), TeamRelation.Neutral},
                
                // ✅ Ally 팀 관계
                {(TeamType.Ally, TeamType.Player), TeamRelation.Ally},
                {(TeamType.Ally, TeamType.Enemy), TeamRelation.Enemy},
                {(TeamType.Ally, TeamType.Neutral), TeamRelation.Neutral},
                {(TeamType.Ally, TeamType.Ally), TeamRelation.Ally},
                {(TeamType.Ally, TeamType.None), TeamRelation.Neutral},
                
                // ✅ None 팀 관계 (팀 없음)
                {(TeamType.None, TeamType.Player), TeamRelation.Neutral},
                {(TeamType.None, TeamType.Enemy), TeamRelation.Neutral},
                {(TeamType.None, TeamType.Neutral), TeamRelation.Neutral},
                {(TeamType.None, TeamType.Ally), TeamRelation.Neutral},
                {(TeamType.None, TeamType.None), TeamRelation.Neutral},
            };
        
        /// <summary>
        /// 두 팀 간의 관계 조회
        /// </summary>
        public static TeamRelation GetRelation(TeamType fromTeam, TeamType toTeam)
        {
            return relations.TryGetValue((fromTeam, toTeam), out var relation) 
                ? relation 
                : TeamRelation.Neutral;
        }
        
        /// <summary>
        /// 팀이 적대적인지 확인
        /// </summary>
        public static bool IsHostile(TeamType fromTeam, TeamType toTeam)
        {
            return GetRelation(fromTeam, toTeam) == TeamRelation.Enemy;
        }
        
        /// <summary>
        /// 팀이 우호적인지 확인
        /// </summary>
        public static bool IsFriendly(TeamType fromTeam, TeamType toTeam)
        {
            var relation = GetRelation(fromTeam, toTeam);
            return relation == TeamRelation.Ally;
        }
        
        /// <summary>
        /// 커스텀 팀 관계 추가/수정
        /// </summary>
        public static void SetCustomRelation(TeamType fromTeam, TeamType toTeam, TeamRelation relation)
        {
            relations[(fromTeam, toTeam)] = relation;
        }
        
        /// <summary>
        /// 모든 관계 초기화 (기본값으로 복원)
        /// </summary>
        public static void ResetToDefaults()
        {
            // 현재 구현에서는 정적으로 정의되어 있으므로 
            // 필요시 기본값 재설정 로직 추가
        }
    }
}