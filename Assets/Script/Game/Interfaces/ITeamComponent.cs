using System;
using UnityEngine;

namespace Game.Interfaces
{
    /// <summary>
    /// 팀 타입 열거형
    /// </summary>
    public enum TeamType
    {
        None = 0,        // 팀 없음
        Player = 1,      // 플레이어 팀
        Enemy = 2,       // 적 팀  
        Neutral = 3,     // 중립 팀
        Ally = 4         // 동맹 팀
    }

    /// <summary>
    /// 팀 관계 열거형
    /// </summary>
    public enum TeamRelation  
    {
        Self,            // 자신
        Ally,            // 아군
        Enemy,           // 적군
        Neutral          // 중립
    }

    /// <summary>
    /// 팀 컴포넌트 인터페이스 - 의존성 역전 원칙 적용
    /// </summary>
    public interface ITeamComponent
    {
        // ✅ 팀 정보
        TeamType Team { get; set; }
        string TeamName { get; }
        Color TeamColor { get; }
        
        // ✅ 팀 관계 확인
        TeamRelation GetRelationTo(ITeamComponent other);
        bool IsSameTeam(ITeamComponent other);
        bool IsEnemy(ITeamComponent other);
        bool IsAlly(ITeamComponent other);
        
        // ✅ 이벤트
        event Action<TeamType, TeamType> OnTeamChanged;
    }
}