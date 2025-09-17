using System;
using UnityEngine;
using Game.Interfaces;
using Game.Data;
using Game.Core;

namespace Game.Components
{
    /// <summary>
    /// 팀 컴포넌트 - 유닛의 팀 소속 및 관계 관리
    /// </summary>
    public class TeamComponent : MonoBehaviour, ITeamComponent
    {
        [Header("팀 설정")]
        [SerializeField] private TeamType team = TeamType.None;
        [SerializeField] private TeamConfigSO teamConfig;
        
        [Header("커스텀 설정")]
        [SerializeField] private bool useCustomSettings = false;
        [SerializeField] private string customTeamName = "";
        [SerializeField] private Color customTeamColor = Color.white;
        
        // ✅ 런타임 캐싱
        private TeamConfigSO.TeamSettings currentTeamSettings;
        
        // ✅ 인터페이스 구현
        public TeamType Team 
        { 
            get => team;
            set => ChangeTeam(value);
        }
        
        public string TeamName => useCustomSettings ? customTeamName : 
                                  currentTeamSettings?.displayName ?? team.ToString();
        
        public Color TeamColor => useCustomSettings ? customTeamColor :
                                  currentTeamSettings?.teamColor ?? Color.white;
        
        public event Action<TeamType, TeamType> OnTeamChanged;
        
        private void Awake()
        {
            RefreshTeamSettings();
        }
        
        // 팀 매니저 캐싱
        private static TeamManager cachedTeamManager;
        
        private void Start()
        {
            // 팀 매니저에 등록 (있다면) - 캐싱으로 성능 개선
            if (cachedTeamManager == null)
                cachedTeamManager = FindObjectOfType<TeamManager>();
            cachedTeamManager?.RegisterTeamMember(this);
        }
        
        private void OnDestroy()
        {
            // 팀 매니저에서 해제 (있다면)
            cachedTeamManager?.UnregisterTeamMember(this);
        }
        
        /// <summary>
        /// 다른 팀 컴포넌트와의 관계 확인
        /// </summary>
        public TeamRelation GetRelationTo(ITeamComponent other)
        {
            if (other == null) return TeamRelation.Neutral;
            if (other == this) return TeamRelation.Self;
            
            return TeamRelationMatrix.GetRelation(Team, other.Team);
        }
        
        /// <summary>
        /// 같은 팀인지 확인
        /// </summary>
        public bool IsSameTeam(ITeamComponent other)
        {
            return other != null && Team == other.Team && Team != TeamType.None;
        }
        
        /// <summary>
        /// 적군인지 확인
        /// </summary>
        public bool IsEnemy(ITeamComponent other)
        {
            return GetRelationTo(other) == TeamRelation.Enemy;
        }
        
        /// <summary>
        /// 아군인지 확인 (자신 포함)
        /// </summary>
        public bool IsAlly(ITeamComponent other)
        {
            var relation = GetRelationTo(other);
            return relation == TeamRelation.Ally || relation == TeamRelation.Self;
        }
        
        /// <summary>
        /// 팀 변경
        /// </summary>
        private void ChangeTeam(TeamType newTeam)
        {
            if (team == newTeam) return;
            
            var oldTeam = team;
            team = newTeam;
            RefreshTeamSettings();
            
            // 이벤트 발생
            OnTeamChanged?.Invoke(oldTeam, newTeam);
            
            // 디버그 로그
            Debug.Log($"[TeamComponent] {gameObject.name}: {oldTeam} → {newTeam}");
        }
        
        /// <summary>
        /// 팀 설정 새로고침
        /// </summary>
        private void RefreshTeamSettings()
        {
            if (teamConfig != null)
            {
                currentTeamSettings = teamConfig.GetTeamSettings(team);
            }
        }
        
        /// <summary>
        /// 팀 설정 적용
        /// </summary>
        public void ApplyTeamConfig(TeamConfigSO config)
        {
            teamConfig = config;
            RefreshTeamSettings();
        }
        
        /// <summary>
        /// 커스텀 팀 이름 설정
        /// </summary>
        public void SetCustomTeamName(string customName)
        {
            useCustomSettings = true;
            customTeamName = customName;
        }
        
        /// <summary>
        /// 커스텀 팀 색상 설정
        /// </summary>
        public void SetCustomTeamColor(Color color)
        {
            useCustomSettings = true;
            customTeamColor = color;
        }
        
        /// <summary>
        /// 팀 데미지 배율 조회
        /// </summary>
        public float GetDamageMultiplier()
        {
            return currentTeamSettings?.damageMultiplier ?? 1f;
        }
        
        /// <summary>
        /// 팀 방어 배율 조회
        /// </summary>
        public float GetDefenseMultiplier()
        {
            return currentTeamSettings?.defenseMultiplier ?? 1f;
        }
        
        /// <summary>
        /// 플레이 가능한 팀인지 확인
        /// </summary>
        public bool IsPlayableTeam()
        {
            return currentTeamSettings?.isPlayableTeam ?? false;
        }
        
        // ✅ Unity Editor용 검증
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                RefreshTeamSettings();
            }
        }
        
        // ✅ 디버깅용 메서드
        public override string ToString()
        {
            return $"TeamComponent[{team}:{TeamName}, Color:{TeamColor}]";
        }
        
        /// <summary>
        /// Gizmo로 팀 색상 표시
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = TeamColor;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}