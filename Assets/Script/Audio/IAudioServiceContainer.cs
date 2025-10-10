using UnityEngine;

/// <summary>
/// 오디오 서비스 컨테이너 인터페이스
/// 의존성 주입과 서비스 생명주기 관리를 위한 통합 인터페이스
/// </summary>
public interface IAudioServiceContainer
{
    
    /// <summary>
    /// 전체 서비스 초기화 상태
    /// </summary>
    bool IsFullyInitialized { get; }
    
    /// <summary>
    /// 모든 서비스 초기화
    /// Unity의 Awake/Start 생명주기에 맞춘 비동기 초기화
    /// </summary>
    void InitializeAllServices();
    
    /// <summary>
    /// 모든 서비스 정리
    /// </summary>
    void CleanupAllServices();
    
    /// <summary>
    /// 특정 서비스 가져오기
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <returns>서비스 인스턴스</returns>
    T GetService<T>() where T : class;
    
    /// <summary>
    /// 서비스 등록 (테스트용)
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <param name="service">서비스 인스턴스</param>
    void RegisterService<T>(T service) where T : class;
    
    /// <summary>
    /// 볼륨 컨트롤러 등록 (테스트용)
    /// </summary>
    /// <param name="volumeController">볼륨 컨트롤러 인스턴스</param>
    void RegisterVolumeController(IVolumeController volumeController);
}

/// <summary>
/// 오디오 서비스 팩토리 인터페이스
/// 테스트 가능한 서비스 생성을 위한 팩토리 패턴
/// </summary>
public interface IAudioServiceFactory
{
    /// <summary>
    /// BGM 서비스 생성
    /// </summary>
    /// <param name="audioMixer">오디오 믹서</param>
    /// <param name="bgmPlayer">BGM 플레이어 GameObject</param>
    /// <returns>BGM 서비스</returns>
    IBGMAudioService CreateBGMService(UnityEngine.Audio.AudioMixer audioMixer, GameObject bgmPlayer);
    
    /// <summary>
    /// 효과음 서비스 생성
    /// </summary>
    /// <param name="audioMixer">오디오 믹서</param>
    /// <param name="effectPlayer">효과음 플레이어 GameObject</param>
    /// <returns>효과음 서비스</returns>
    IEffectAudioService CreateEffectService(UnityEngine.Audio.AudioMixer audioMixer, GameObject effectPlayer);
    
    /// <summary>
    /// 볼륨 컨트롤러 생성
    /// </summary>
    /// <param name="audioMixer">오디오 믹서</param>
    /// <returns>볼륨 컨트롤러</returns>
    IVolumeController CreateVolumeController(UnityEngine.Audio.AudioMixer audioMixer);
    
    /// <summary>
    /// 볼륨 컨트롤러를 기존 GameObject에 추가
    /// </summary>
    /// <param name="audioMixer">오디오 믹서</param>
    /// <param name="targetObject">대상 GameObject</param>
    /// <returns>볼륨 컨트롤러</returns>
    IVolumeController CreateVolumeControllerOnGameObject(UnityEngine.Audio.AudioMixer audioMixer, GameObject targetObject);
    
    /// <summary>
    /// 서비스 컨테이너 생성
    /// </summary>
    /// <returns>서비스 컨테이너</returns>
    IAudioServiceContainer CreateServiceContainer();
}