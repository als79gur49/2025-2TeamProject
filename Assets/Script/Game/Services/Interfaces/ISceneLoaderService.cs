using System;
using Game.SceneManagement;

namespace Game.Services
{
    /// <summary>
    /// 씬 로딩 서비스 인터페이스
    /// SceneData ScriptableObject를 사용한 타입 안전 씬 관리
    /// </summary>
    public interface ISceneLoaderService
    {
        /// <summary>
        /// 현재 로딩 중인지 여부
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// 현재 로딩 진행률 (0.0 ~ 1.0)
        /// </summary>
        float LoadingProgress { get; }

        /// <summary>
        /// 씬 로딩 시작 이벤트 (SceneData)
        /// </summary>
        event Action<SceneData> OnSceneLoadStarted;

        /// <summary>
        /// 씬 로딩 진행 이벤트 (SceneData, progress)
        /// </summary>
        event Action<SceneData, float> OnSceneLoadProgress;

        /// <summary>
        /// 씬 로딩 완료 이벤트 (SceneData)
        /// </summary>
        event Action<SceneData> OnSceneLoadCompleted;

        /// <summary>
        /// 씬 로딩 실패 이벤트 (SceneData, error message)
        /// </summary>
        event Action<SceneData, string> OnSceneLoadFailed;

        /// <summary>
        /// 씬을 동기적으로 로드합니다 (즉시 로딩, 화면 멈춤)
        /// </summary>
        /// <param name="sceneData">로드할 씬 데이터</param>
        void LoadScene(SceneData sceneData);

        /// <summary>
        /// 씬을 비동기적으로 로드합니다 (진행률 표시 가능)
        /// </summary>
        /// <param name="sceneData">로드할 씬 데이터</param>
        /// <param name="onProgress">진행률 콜백 (0.0 ~ 1.0)</param>
        /// <param name="minimumLoadingDuration">최소 로딩 표시 시간 (초). 0이면 무시</param>
        /// <param name="fadeOutDuration">FadeOut 애니메이션 지속 시간 (초). 씬 전환 후 FadeOut에 사용</param>
        void LoadSceneAsync(SceneData sceneData, Action<float> onProgress = null, float minimumLoadingDuration = 0f, float fadeOutDuration = 0f);

        /// <summary>
        /// 씬 로딩이 가능한지 검증합니다
        /// </summary>
        /// <param name="sceneData">검증할 씬 데이터</param>
        /// <returns>로딩 가능 여부</returns>
        bool CanLoadScene(SceneData sceneData);

        /// <summary>
        /// 씬 로딩 전 검증을 수행합니다 (에러 메시지 반환)
        /// </summary>
        /// <param name="sceneData">검증할 씬 데이터</param>
        /// <param name="errorMessage">검증 실패 시 에러 메시지</param>
        /// <returns>검증 통과 여부</returns>
        bool ValidateSceneBeforeLoad(SceneData sceneData, out string errorMessage);

        /// <summary>
        /// 현재 활성화된 씬의 이름을 반환합니다
        /// </summary>
        /// <returns>현재 씬 이름</returns>
        string GetCurrentSceneName();
    }
}
