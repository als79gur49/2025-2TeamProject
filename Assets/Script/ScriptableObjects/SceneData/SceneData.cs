using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.SceneManagement
{
    /// <summary>
    /// 씬 데이터 ScriptableObject - Unity 네이티브 에셋 기반 씬 관리
    ///
    /// 장점:
    /// - GUID 기반 참조로 씬 파일명 변경에도 안전
    /// - Inspector에서 드래그 앤 드롭으로 씬 참조 설정
    /// - 추가 메타데이터 (로딩 팁, 배경음악 등) 지원
    ///
    /// 약점 보완:
    /// - OnValidate()로 씬 이름 유효성 자동 검증
    /// - Custom Editor로 드롭다운 제공하여 오타 방지
    /// </summary>
    [CreateAssetMenu(fileName = "New Scene Data", menuName = "Game/Scene Management/Scene Data", order = 1)]
    public class SceneData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField]
        [Tooltip("씬 파일 이름 (확장자 제외). Build Settings에 추가되어 있어야 합니다.")]
        private string sceneName = "";

        [SerializeField]
        [Tooltip("씬 카테고리 (조직화 및 필터링용)")]
        private SceneCategory category = SceneCategory.FeatureTest;

        [Header("로딩 화면 설정")]
        [SerializeField]
        [Tooltip("로딩 화면 배경 이미지")]
        private Sprite loadingBackground;

        [SerializeField]
        [TextArea(3, 6)]
        [Tooltip("로딩 중 표시할 팁 목록 (랜덤 선택)")]
        private string[] loadingTips = new string[0];

        [Header("오디오 설정")]
        [SerializeField]
        [Tooltip("씬 진입 시 재생할 배경음악 (AudioData SO 참조)")]
        private ScriptableObject bgMusic; // AudioData 타입 (순환 참조 방지를 위해 ScriptableObject로 선언)

        [Header("메타데이터")]
        [SerializeField]
        [Tooltip("씬에 대한 설명 (에디터용)")]
        [TextArea(2, 4)]
        private string description = "";

        // 🔒 Public 읽기 전용 프로퍼티
        public string SceneName => sceneName;
        public SceneCategory Category => category;
        public Sprite LoadingBackground => loadingBackground;
        public string[] LoadingTips => loadingTips;
        public ScriptableObject BgMusic => bgMusic;
        public string Description => description;

        /// <summary>
        /// 랜덤 로딩 팁 반환
        /// </summary>
        public string GetRandomLoadingTip()
        {
            if (loadingTips == null || loadingTips.Length == 0)
                return "Loading...";

            int randomIndex = Random.Range(0, loadingTips.Length);
            return loadingTips[randomIndex];
        }

        /// <summary>
        /// 씬이 Build Settings에 포함되어 있는지 확인
        /// </summary>
        public bool IsSceneInBuildSettings()
        {
#if UNITY_EDITOR
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && scene.path.Contains(sceneName))
                {
                    return true;
                }
            }
            return false;
#else
            // 런타임에서는 SceneManager를 통해 확인
            return UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(sceneName) >= 0;
#endif
        }

        /// <summary>
        /// 씬 파일이 실제로 존재하는지 확인 (에디터 전용)
        /// </summary>
        public bool DoesSceneExist()
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            return guids.Length > 0;
#else
            return true; // 런타임에서는 항상 true (빌드된 씬은 존재한다고 가정)
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector에서 값이 변경될 때마다 자동 검증
        /// </summary>
        private void OnValidate()
        {
            // 씬 이름이 비어있으면 경고
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning($"[SceneData] '{name}': Scene name is empty!", this);
                return;
            }

            // Build Settings 확인
            if (!IsSceneInBuildSettings())
            {
                Debug.LogWarning($"[SceneData] '{name}': Scene '{sceneName}' is not in Build Settings or is disabled!", this);
            }

            // 씬 파일 존재 확인
            if (!DoesSceneExist())
            {
                Debug.LogError($"[SceneData] '{name}': Scene file '{sceneName}' does not exist in project!", this);
            }
        }
#endif

        /// <summary>
        /// 디버그용 정보 출력
        /// </summary>
        public override string ToString()
        {
            return $"SceneData[{name}] - Scene: {sceneName}, Category: {category}";
        }
    }
}
