using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Services
{
    /// <summary>
    /// 팀별 설정(Material, 색상, 이펙트 등)을 관리하는 매니저
    /// TeamType에 따른 시각적 표현과 관련된 모든 설정을 중앙에서 관리합니다.
    /// </summary>
    public class TeamConfigurationManager : MonoBehaviour, ITeamConfigurationManager
    {
        [Header("팀별 Material 설정")]
        [SerializeField]
        [Tooltip("플레이어 유닛에 적용할 Material")]
        private Material playerMaterial;

        [SerializeField]
        [Tooltip("적 유닛에 적용할 Material")]
        private Material enemyMaterial;

        [Header("설정")]
        [SerializeField] private bool enableLogging = true;

        // 초기화 상태
        private bool isInitialized = false;

        /// <summary>초기화 여부</summary>
        public bool IsInitialized => isInitialized;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        #endregion

        #region 초기화

        /// <summary>
        /// TeamConfigurationManager 초기화
        /// </summary>
        public void Initialize()
        {
            Log("🎨 Initializing TeamConfigurationManager...");

            // Material 검증
            ValidateMaterials();

            isInitialized = true;
            Log("✅ TeamConfigurationManager initialized successfully");
        }

        /// <summary>
        /// Material 설정 검증
        /// </summary>
        private void ValidateMaterials()
        {
            if (playerMaterial == null)
            {
                LogWarning("⚠️ Player Material is not set. Player units will not have team-specific material applied.");
            }

            if (enemyMaterial == null)
            {
                LogWarning("⚠️ Enemy Material is not set. Enemy units will not have team-specific material applied.");
            }

            if (playerMaterial != null && enemyMaterial != null)
            {
                Log($"✅ Materials validated: Player={playerMaterial.name}, Enemy={enemyMaterial.name}");
            }
        }

        #endregion

        #region ITeamConfigurationManager 구현

        /// <summary>
        /// 팀 타입에 따른 Material을 반환합니다.
        /// </summary>
        /// <param name="teamType">팀 타입 (Player 또는 Enemy)</param>
        /// <returns>해당 팀의 Material, null이면 적용하지 않음</returns>
        public Material GetTeamMaterial(TeamType teamType)
        {
            return teamType == TeamType.Player ? playerMaterial : enemyMaterial;
        }

        /// <summary>
        /// 유닛에 팀별 Material을 적용합니다.
        /// 유닛의 자식 오브젝트에 있는 모든 Renderer에 Material을 적용합니다.
        /// </summary>
        /// <param name="unit">Material을 적용할 유닛</param>
        /// <param name="teamType">팀 타입 (Player 또는 Enemy)</param>
        public void ApplyTeamMaterialToUnit(Unit unit, TeamType teamType)
        {
            if (unit == null)
            {
                LogError("❌ Cannot apply material to null unit");
                return;
            }

            Material targetMaterial = GetTeamMaterial(teamType);

            if (targetMaterial == null)
            {
                LogWarning($"⚠️ {unit.name}: Material for {teamType} is not set. Skipping material application.");
                return;
            }

            // 자식 오브젝트의 모든 Renderer 컴포넌트 가져오기
            Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                LogWarning($"⚠️ {unit.name}: No Renderer found in child objects.");
                return;
            }

            // 각 Renderer에 Material 적용
            int appliedCount = 0;
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.material = targetMaterial;
                    appliedCount++;
                }
            }

            Log($"✅ {unit.name}: Applied {teamType} material to {appliedCount} Renderer(s)");
        }

        #endregion

        #region 로깅 시스템

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[TeamConfigurationManager] {System.DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (enableLogging)
            {
                Debug.LogWarning($"[TeamConfigurationManager] {System.DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[TeamConfigurationManager] {System.DateTime.Now:HH:mm:ss.fff} {message}");
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// Manager 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetStatus()
        {
            return $"TeamConfigurationManager Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Player Material: {(playerMaterial != null ? playerMaterial.name : "None")}\n" +
                   $"- Enemy Material: {(enemyMaterial != null ? enemyMaterial.name : "None")}\n";
        }

        #endregion

        #region 에디터용 디버깅

#if UNITY_EDITOR
        [Header("에디터 도구")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(920, 10, 300, 200));
            GUILayout.Box("TeamConfiguration Manager");

            if (isInitialized)
            {
                GUILayout.Label("✅ Initialized");
            }
            else
            {
                GUILayout.Label("❌ Not Initialized");
            }

            GUILayout.Space(10);

            GUILayout.Label($"🔵 Player Material: {(playerMaterial != null ? playerMaterial.name : "None")}");
            GUILayout.Label($"🔴 Enemy Material: {(enemyMaterial != null ? enemyMaterial.name : "None")}");

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
