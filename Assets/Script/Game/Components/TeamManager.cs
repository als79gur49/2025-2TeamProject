using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Interfaces;
using Game.Data;

namespace Game.Components
{
    /// <summary>
    /// 팀 관리 시스템 - 모든 팀 멤버들을 중앙에서 관리
    /// </summary>
    public class TeamManager : MonoBehaviour
    {
        [Header("팀 관리 설정")]
        [SerializeField] private TeamConfigSO defaultTeamConfig;
        [SerializeField] private bool enableTeamBalancing = true;
        [SerializeField] private bool logTeamChanges = true;
        
        // ✅ 팀별 멤버 관리
        private readonly Dictionary<TeamType, List<ITeamComponent>> teamMembers = 
            new Dictionary<TeamType, List<ITeamComponent>>();
            
        // ✅ 전체 등록된 멤버들
        private readonly HashSet<ITeamComponent> allMembers = new HashSet<ITeamComponent>();
        
        // ✅ 이벤트
        public event Action<ITeamComponent, TeamType> OnMemberJoinedTeam;
        public event Action<ITeamComponent, TeamType> OnMemberLeftTeam;
        public event Action<TeamType, int> OnTeamMemberCountChanged;
        
        private void Awake()
        {
            // 팀 딕셔너리 초기화
            InitializeTeamDictionaries();
        }
        
        private void Start()
        {
            // 씬에 있는 모든 팀 컴포넌트 자동 등록
            RegisterExistingTeamComponents();
        }
        
        /// <summary>
        /// 팀 멤버 등록
        /// </summary>
        public void RegisterTeamMember(ITeamComponent member)
        {
            if (member == null || allMembers.Contains(member))
                return;
                
            allMembers.Add(member);
            AddToTeam(member, member.Team);
            
            // 팀 변경 이벤트 구독
            member.OnTeamChanged += OnMemberTeamChanged;
            
            if (logTeamChanges)
            {
                Debug.Log($"[TeamManager] Registered: {GetMemberName(member)} to {member.Team}");
            }
        }
        
        /// <summary>
        /// 팀 멤버 등록 해제
        /// </summary>
        public void UnregisterTeamMember(ITeamComponent member)
        {
            if (member == null || !allMembers.Contains(member))
                return;
                
            allMembers.Remove(member);
            RemoveFromTeam(member, member.Team);
            
            // 팀 변경 이벤트 구독 해제
            member.OnTeamChanged -= OnMemberTeamChanged;
            
            if (logTeamChanges)
            {
                Debug.Log($"[TeamManager] Unregistered: {GetMemberName(member)} from {member.Team}");
            }
        }
        
        /// <summary>
        /// 특정 팀의 멤버들 조회
        /// </summary>
        public IEnumerable<ITeamComponent> GetTeamMembers(TeamType team)
        {
            return teamMembers.TryGetValue(team, out var members) 
                ? members.AsEnumerable() 
                : Enumerable.Empty<ITeamComponent>();
        }
        
        /// <summary>
        /// 팀 멤버 수 조회
        /// </summary>
        public int GetTeamMemberCount(TeamType team)
        {
            return teamMembers.TryGetValue(team, out var members) ? members.Count : 0;
        }
        
        /// <summary>
        /// 모든 팀 정보 조회
        /// </summary>
        public Dictionary<TeamType, int> GetAllTeamCounts()
        {
            return teamMembers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }
        
        /// <summary>
        /// 특정 팀의 살아있는 멤버들 조회
        /// </summary>
        public IEnumerable<ITeamComponent> GetAliveTeamMembers(TeamType team)
        {
            return GetTeamMembers(team).Where(member => IsAlive(member));
        }
        
        /// <summary>
        /// 특정 팀의 살아있는 멤버 수 조회
        /// </summary>
        public int GetAliveTeamMemberCount(TeamType team)
        {
            return GetAliveTeamMembers(team).Count();
        }
        
        /// <summary>
        /// 팀이 전멸했는지 확인
        /// </summary>
        public bool IsTeamEliminated(TeamType team)
        {
            return GetAliveTeamMemberCount(team) == 0;
        }
        
        /// <summary>
        /// 가장 가까운 적 팀원 찾기
        /// </summary>
        public ITeamComponent FindNearestEnemy(ITeamComponent fromMember, float maxDistance = float.MaxValue)
        {
            if (fromMember == null) return null;
            
            var fromTransform = GetMemberTransform(fromMember);
            if (fromTransform == null) return null;
            
            ITeamComponent nearestEnemy = null;
            float nearestDistance = maxDistance;
            
            foreach (var member in allMembers)
            {
                if (member == fromMember || !fromMember.IsEnemy(member) || !IsAlive(member))
                    continue;
                    
                var memberTransform = GetMemberTransform(member);
                if (memberTransform == null) continue;
                
                float distance = Vector3.Distance(fromTransform.position, memberTransform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = member;
                }
            }
            
            return nearestEnemy;
        }
        
        /// <summary>
        /// 팀원 변경 이벤트 핸들러
        /// </summary>
        private void OnMemberTeamChanged(TeamType oldTeam, TeamType newTeam)
        {
            var member = allMembers.FirstOrDefault(m => m.Team == newTeam);
            if (member == null) return;
            
            RemoveFromTeam(member, oldTeam);
            AddToTeam(member, newTeam);
            
            if (logTeamChanges)
            {
                Debug.Log($"[TeamManager] {GetMemberName(member)}: {oldTeam} → {newTeam}");
            }
        }
        
        /// <summary>
        /// 팀에 멤버 추가
        /// </summary>
        private void AddToTeam(ITeamComponent member, TeamType team)
        {
            var teamList = teamMembers[team]; // 이미 초기화되어 있음
            if (!teamList.Contains(member))
            {
                teamList.Add(member);
                OnMemberJoinedTeam?.Invoke(member, team);
                OnTeamMemberCountChanged?.Invoke(team, teamList.Count);
            }
        }
        
        /// <summary>
        /// 팀에서 멤버 제거
        /// </summary>
        private void RemoveFromTeam(ITeamComponent member, TeamType team)
        {
            if (teamMembers.TryGetValue(team, out var members) && members.Remove(member))
            {
                OnMemberLeftTeam?.Invoke(member, team);
                OnTeamMemberCountChanged?.Invoke(team, members.Count);
            }
        }
        
        /// <summary>
        /// 팀 딕셔너리 초기화
        /// </summary>
        private void InitializeTeamDictionaries()
        {
            foreach (TeamType teamType in Enum.GetValues(typeof(TeamType)))
            {
                teamMembers[teamType] = new List<ITeamComponent>();
            }
        }
        
        /// <summary>
        /// 기존 팀 컴포넌트들 자동 등록
        /// </summary>
        private void RegisterExistingTeamComponents()
        {
            var existingComponents = FindObjectsOfType<MonoBehaviour>()
                .OfType<ITeamComponent>()
                .ToArray();
                
            foreach (var component in existingComponents)
            {
                RegisterTeamMember(component);
            }
            
            Debug.Log($"[TeamManager] Auto-registered {existingComponents.Length} team components");
        }
        
        /// <summary>
        /// 멤버가 살아있는지 확인
        /// </summary>
        private bool IsAlive(ITeamComponent member)
        {
            var memberTransform = GetMemberTransform(member);
            if (memberTransform == null) return false;
            
            // HealthComponent가 있다면 생존 상태 확인
            var healthComponent = memberTransform.GetComponent<IHealthComponent>();
            return healthComponent?.IsAlive ?? true;
        }
        
        /// <summary>
        /// 멤버의 Transform 조회
        /// </summary>
        private Transform GetMemberTransform(ITeamComponent member)
        {
            return (member as MonoBehaviour)?.transform;
        }
        
        /// <summary>
        /// 멤버 이름 조회
        /// </summary>
        private string GetMemberName(ITeamComponent member)
        {
            var memberTransform = GetMemberTransform(member);
            return memberTransform?.name ?? "Unknown";
        }
        
        // ✅ 디버깅용 메서드
        [ContextMenu("Log Team Status")]
        public void LogTeamStatus()
        {
            Debug.Log("=== Team Status ===");
            foreach (var kvp in teamMembers)
            {
                var alive = GetAliveTeamMemberCount(kvp.Key);
                var total = kvp.Value.Count;
                Debug.Log($"{kvp.Key}: {alive}/{total} alive");
            }
        }
        
        public override string ToString()
        {
            var totalMembers = allMembers.Count;
            var activeTeams = teamMembers.Where(kvp => kvp.Value.Count > 0).Count();
            return $"TeamManager[Members:{totalMembers}, ActiveTeams:{activeTeams}]";
        }
    }
}