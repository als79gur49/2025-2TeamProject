using System;
using System.Linq;
using UnityEngine;
using Game.Interfaces;

namespace Game.Data
{
    /// <summary>
    /// 팀 설정 ScriptableObject - 팀별 설정 데이터 관리
    /// </summary>
    [CreateAssetMenu(fileName = "TeamConfig", menuName = "Game/Team Configuration")]
    public class TeamConfigSO : ScriptableObject
    {
        [System.Serializable]
        public class TeamSettings
        {
            [Header("팀 기본 정보")]
            public TeamType teamType;
            public string displayName;
            public Color teamColor = Color.white;
            
            [Header("팀 시각적 요소")]
            public Sprite teamIcon;
            public Material teamMaterial;
            
            [Header("팀 특성")]
            [Range(0f, 2f)]
            public float damageMultiplier = 1f;
            [Range(0f, 2f)]
            public float defenseMultiplier = 1f;
            
            public bool isPlayableTeam = false;
        }
        
        [Header("팀 설정 목록")]
        [SerializeField] private TeamSettings[] teamConfigurations;
        
        /// <summary>
        /// 팀 타입에 대한 설정 조회
        /// </summary>
        public TeamSettings GetTeamSettings(TeamType team)
        {
            return teamConfigurations?.FirstOrDefault(config => config.teamType == team);
        }
        
        /// <summary>
        /// 모든 팀 설정 조회
        /// </summary>
        public TeamSettings[] GetAllTeamSettings()
        {
            return teamConfigurations;
        }
        
        /// <summary>
        /// 플레이 가능한 팀 목록 조회
        /// </summary>
        public TeamSettings[] GetPlayableTeams()
        {
            return teamConfigurations?.Where(config => config.isPlayableTeam).ToArray();
        }
        
        private void OnValidate()
        {
            // 중복 팀 타입 검증
            if (teamConfigurations != null)
            {
                var duplicates = teamConfigurations
                    .GroupBy(config => config.teamType)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key);
                
                foreach (var duplicate in duplicates)
                {
                    Debug.LogWarning($"[TeamConfigSO] 중복된 팀 타입 발견: {duplicate}");
                }
            }
        }
    }
}