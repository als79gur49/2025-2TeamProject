using Game.SceneManagement;
using UnityEngine;

namespace Game.Services
{
    /// <summary>
    /// 씬 전환 컨트롤러 인터페이스
    /// 글로벌 싱글턴 서비스 - ServiceLocator.RegisterSingleton으로 자동 생명주기 관리
    /// </summary>
    public interface ISceneTransitionController
    {
        /// <summary>
        /// 로딩 화면과 함께 씬을 비동기로 로드합니다
        /// </summary>
        /// <param name="sceneData">로드할 씬 데이터</param>
        void LoadSceneWithLoading(SceneData sceneData);

        /// <summary>
        /// 커스텀 로딩 화면과 함께 씬을 비동기로 로드합니다
        /// </summary>
        /// <param name="sceneData">로드할 씬 데이터</param>
        /// <param name="customLoadingPrefab">커스텀 로딩 화면 Prefab (null이면 기본 Prefab 사용)</param>
        void LoadSceneWithLoading(SceneData sceneData, GameObject customLoadingPrefab);

        /// <summary>
        /// 로딩 화면 없이 씬을 직접 로드합니다 (빠른 전환용)
        /// </summary>
        /// <param name="sceneData">로드할 씬 데이터</param>
        void LoadSceneImmediate(SceneData sceneData);

        /// <summary>
        /// 현재 씬 전환 중인지 여부
        /// </summary>
        bool IsTransitioning { get; }
    }
}
