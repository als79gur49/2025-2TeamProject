using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 오디오 서비스 컨테이너 구현
/// 모든 오디오 서비스의 생명주기를 관리하고 의존성 주입을 제공
/// Unity Singleton 패턴과 DontDestroyOnLoad 사용
/// </summary>
public class AudioServiceContainer : MonoBehaviour, IAudioServiceContainer
{
    [Header("AudioMixer 설정")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("오디오 플레이어")]
    [SerializeField] private GameObject bgmPlayer;
    [SerializeField] private GameObject effectPlayer;

    [Header("이벤트 채널 (Event Channel Integration)")]
    [SerializeField] private SoundEventChannelSO soundEventChannel;

    [Header("서비스 자동 생성")]
    [SerializeField] private bool autoCreateServices = true;
    [SerializeField] private bool initializeOnStart = true;
    
    // 서비스 인스턴스들
    private IBGMAudioService bgmService;
    private IEffectAudioService effectService;
    private IVolumeController volumeController;
    
    // 서비스 레지스트리 (의존성 주입용)
    private readonly Dictionary<Type, object> serviceRegistry = new Dictionary<Type, object>();
    
    // 서비스 팩토리
    private IAudioServiceFactory serviceFactory;
    
    // Singleton 패턴
    private static AudioServiceContainer instance;
    public static AudioServiceContainer Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioServiceContainer>();
                
                if (instance == null)
                {
                    // 런타임에 자동 생성
                    var containerGO = new GameObject("AudioServiceContainer");
                    instance = containerGO.AddComponent<AudioServiceContainer>();
                    DontDestroyOnLoad(containerGO);
                    
                    Debug.Log("AudioServiceContainer 자동 생성됨");
                }
            }
            
            return instance;
        }
    }
    
    #region 인터페이스 프로퍼티 구현
    
    /// <summary>
    /// 전체 서비스 초기화 상태
    /// </summary>
    public bool IsFullyInitialized
    {
        get
        {
            var bgmSvc = GetService<IBGMAudioService>();
            var effectSvc = GetService<IEffectAudioService>();
            var volumeCtrl = GetService<IVolumeController>();
            
            return bgmSvc?.IsInitialized == true &&
                   effectSvc?.IsInitialized == true &&
                   volumeCtrl != null;
        }
    }
    
    #endregion
    
    #region Unity Lifecycle

    /// <summary>
    /// Unity OnEnable: Subscribe to event channel
    /// </summary>
    private void OnEnable()
    {
        if (soundEventChannel != null)
        {
            soundEventChannel.OnSoundRequested += HandleSoundRequest;
            soundEventChannel.OnStopLoopRequested += HandleStopLoopRequest;
            Debug.Log("AudioServiceContainer: Subscribed to SoundEventChannel (Unified Sound + Legacy Loop + StopLoop events)");
        }
        else
        {
            Debug.LogWarning("AudioServiceContainer: No SoundEventChannel assigned. Event-based audio will not work!");
        }
    }

    /// <summary>
    /// Unity Awake: Singleton 초기화 및 DontDestroyOnLoad 설정
    /// </summary>
    private void Awake()
    {
        // Singleton 패턴 구현
        if (instance != null && instance != this)
        {
            Debug.LogWarning("AudioServiceContainer 중복 인스턴스 제거");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 서비스 팩토리 초기화
        if (serviceFactory == null)
        {
            serviceFactory = new AudioServiceFactory();
        }
        
        // AudioMixer 유효성 검사
        if (audioMixer == null)
        {
            Debug.LogWarning("AudioServiceContainer: AudioMixer가 설정되지 않았습니다.");
        }
        
        Debug.Log("AudioServiceContainer Awake 완료");
    }
    
    /// <summary>
    /// Unity Start: 자동 초기화
    /// </summary>
    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeAllServices();
        }
    }
    
    /// <summary>
    /// Unity OnDisable: Unsubscribe from event channel
    /// </summary>
    private void OnDisable()
    {
        if (soundEventChannel != null)
        {
            soundEventChannel.OnSoundRequested -= HandleSoundRequest;
            soundEventChannel.OnStopLoopRequested -= HandleStopLoopRequest;
            Debug.Log("AudioServiceContainer: Unsubscribed from SoundEventChannel (Sound + LoopSound + StopLoop events)");
        }
    }

    /// <summary>
    /// Unity OnDestroy: 정리 작업
    /// </summary>
    private void OnDestroy()
    {
        if (instance == this)
        {
            CleanupAllServices();
            instance = null;
        }
    }
    
    #endregion
    
    #region IAudioServiceContainer 구현
    
    /// <summary>
    /// 모든 서비스 초기화
    /// Unity 생명주기를 고려한 순차적 초기화
    /// </summary>
    public void InitializeAllServices()
    {
        try
        {
            Debug.Log("AudioServiceContainer: 모든 서비스 초기화 시작");
            
            // 1. 서비스 생성 (아직 생성되지 않은 경우)
            if (autoCreateServices)
            {
                EnsureAllServicesCreated();
            }
            
            // 2. 볼륨 컨트롤러 먼저 초기화 (AudioMixer 설정)
            var volumeCtrl = GetService<IVolumeController>();
            if (volumeCtrl != null && volumeCtrl is VolumeController vc)
            {
                vc.InitializeVolume();
            }
            
            // 3. 오디오 서비스들 초기화
            var bgmSvc = GetService<IBGMAudioService>();
            var effectSvc = GetService<IEffectAudioService>();
            bgmSvc?.Initialize();
            effectSvc?.Initialize();
            
            // 4. 초기화 상태 확인
            if (IsFullyInitialized)
            {
                Debug.Log("AudioServiceContainer: 모든 서비스 초기화 완료");
                
                // 전역 이벤트 알림
                AudioServiceEvents.NotifyServiceInitialized(typeof(AudioServiceContainer));
            }
            else
            {
                Debug.LogWarning("AudioServiceContainer: 일부 서비스 초기화 실패");
                LogInitializationStatus();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"AudioServiceContainer 초기화 실패: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 모든 서비스 정리
    /// Unity OnDestroy에서 호출
    /// </summary>
    public void CleanupAllServices()
    {
        Debug.Log("AudioServiceContainer: 모든 서비스 정리 시작");
        
        try
        {
            // 역순으로 정리 (초기화의 반대 순서)
            var effectSvc = GetService<IEffectAudioService>();
            var bgmSvc = GetService<IBGMAudioService>();
            var volumeCtrl = GetService<IVolumeController>();
            
            effectSvc?.Cleanup();
            bgmSvc?.Cleanup();

            // 볼륨 설정은 SettingsCoordinator를 통해 이미 저장됨 (SaveDataAdapter 사용)
            // 게임 종료 시 별도의 저장 불필요

            // 서비스 참조 제거 (백워드 호환성용 필드들)
            bgmService = null;
            effectService = null;
            volumeController = null;
            
            // 레지스트리 정리
            serviceRegistry.Clear();
            
            Debug.Log("AudioServiceContainer: 모든 서비스 정리 완료");
            AudioServiceEvents.NotifyServiceCleaned(typeof(AudioServiceContainer));
        }
        catch (Exception ex)
        {
            Debug.LogError($"AudioServiceContainer 정리 실패: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 특정 서비스 가져오기 (제네릭)
    /// 의존성 주입 지원 - serviceRegistry 전용
    /// </summary>
    public T GetService<T>() where T : class
    {
        Type serviceType = typeof(T);
        
        // serviceRegistry에서 서비스 검색
        if (serviceRegistry.TryGetValue(serviceType, out object service))
        {
            return service as T;
        }
        
        // 지연 초기화: 서비스가 없으면 자동 생성 시도
        if (autoCreateServices)
        {
            if (serviceType == typeof(IBGMAudioService) && bgmService == null)
            {
                CreateBGMService();
                if (serviceRegistry.TryGetValue(serviceType, out service))
                    return service as T;
            }
            else if (serviceType == typeof(IEffectAudioService) && effectService == null)
            {
                CreateEffectService();
                if (serviceRegistry.TryGetValue(serviceType, out service))
                    return service as T;
            }
            else if (serviceType == typeof(IVolumeController) && volumeController == null)
            {
                CreateVolumeController();
                if (serviceRegistry.TryGetValue(serviceType, out service))
                    return service as T;
            }
        }
        
        Debug.LogWarning($"서비스를 찾을 수 없습니다: {serviceType.Name}");
        return null;
    }
    
    /// <summary>
    /// 서비스 등록 (테스트 및 커스텀 서비스용)
    /// 의존성 주입 지원 - serviceRegistry 전용
    /// </summary>
    public void RegisterService<T>(T service) where T : class
    {
        if (service == null)
        {
            Debug.LogError("null 서비스는 등록할 수 없습니다.");
            return;
        }
        
        Type serviceType = typeof(T);
        
        // IVolumeController는 IAudioService를 상속하지 않으므로 별도 처리
        object serviceToStore = service;
        if (service is IVolumeController)
        {
            serviceToStore = service;
        }
        else if (service is IAudioService)
        {
            serviceToStore = service;
        }
        else
        {
            Debug.LogError($"지원되지 않는 서비스 타입입니다: {serviceType.Name}");
            return;
        }
        
        if (serviceRegistry.ContainsKey(serviceType))
        {
            Debug.LogWarning($"서비스가 이미 등록되어 있습니다: {serviceType.Name}");
            serviceRegistry[serviceType] = serviceToStore;
        }
        else
        {
            serviceRegistry.Add(serviceType, serviceToStore);
        }
        
        // 백워드 호환성을 위해 필드 참조도 유지 (내부 사용)
        if (service is IBGMAudioService bgmSvc)
            bgmService = bgmSvc;
        else if (service is IEffectAudioService effectSvc)
            effectService = effectSvc;
        
        Debug.Log($"서비스 등록 완료: {serviceType.Name}");
    }
    
    /// <summary>
    /// 볼륨 컨트롤러 등록 (테스트용)
    /// serviceRegistry 통합 사용
    /// </summary>
    public void RegisterVolumeController(IVolumeController controller)
    {
        if (controller == null)
        {
            Debug.LogError("null 볼륨 컨트롤러는 등록할 수 없습니다.");
            return;
        }
        
        Type serviceType = typeof(IVolumeController);
        
        if (serviceRegistry.ContainsKey(serviceType))
        {
            Debug.LogWarning("볼륨 컨트롤러가 이미 등록되어 있습니다.");
            serviceRegistry[serviceType] = controller;
        }
        else
        {
            serviceRegistry.Add(serviceType, controller);
        }
        
        // 백워드 호환성을 위해 필드 참조도 유지 (내부 사용)
        volumeController = controller;
        Debug.Log("볼륨 컨트롤러 등록 완료");
    }
    
    #endregion
    
    #region Private Service Creation Methods
    
    /// <summary>
    /// 모든 서비스가 생성되었는지 확인하고 필요 시 생성
    /// </summary>
    private void EnsureAllServicesCreated()
    {
        if (GetService<IBGMAudioService>() == null) CreateBGMService();
        if (GetService<IEffectAudioService>() == null) CreateEffectService();  
        if (GetService<IVolumeController>() == null) CreateVolumeController();
    }
    
    /// <summary>
    /// BGM 서비스 생성 (Factory 사용)
    /// </summary>
    private void CreateBGMService()
    {
        try
        {
            // BGM 플레이어 GameObject 확인/생성
            if (bgmPlayer == null)
            {
                bgmPlayer = new GameObject("BGMPlayer");
                bgmPlayer.transform.SetParent(transform);
            }
            
            // Factory를 통해 BGM 서비스 생성
            var createdBGMService = serviceFactory.CreateBGMService(audioMixer, bgmPlayer);
            
            // RegisterService를 통해 등록 (필드 할당 및 레지스트리 등록 모두 처리)
            if (createdBGMService != null)
            {
                RegisterService<IBGMAudioService>(createdBGMService);
            }
            
            Debug.Log("BGM 서비스 생성 완료 (Factory 사용)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"BGM 서비스 생성 실패: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 효과음 서비스 생성 (Factory 사용)
    /// </summary>
    private void CreateEffectService()
    {
        try
        {
            // 효과음 플레이어 GameObject 확인/생성
            if (effectPlayer == null)
            {
                effectPlayer = new GameObject("EffectPlayer");
                effectPlayer.transform.SetParent(transform);
            }
            
            // Factory를 통해 효과음 서비스 생성
            var createdEffectService = serviceFactory.CreateEffectService(audioMixer, effectPlayer);
            
            // RegisterService를 통해 등록 (필드 할당 및 레지스트리 등록 모두 처리)
            if (createdEffectService != null)
            {
                RegisterService<IEffectAudioService>(createdEffectService);
            }
            
            Debug.Log("효과음 서비스 생성 완료 (Factory 사용)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"효과음 서비스 생성 실패: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 볼륨 컨트롤러 생성 (Factory 사용)
    /// </summary>
    private void CreateVolumeController()
    {
        try
        {
            if(serviceFactory == null)
            {
                serviceFactory = new AudioServiceFactory();
            }
            // Factory를 통해 Container 자체에 VolumeController 추가
            var createdVolumeController = serviceFactory.CreateVolumeControllerOnGameObject(audioMixer, gameObject);
            // RegisterVolumeController를 통해 등록
            if (createdVolumeController != null)
            {
                RegisterVolumeController(createdVolumeController);
            }
            
            Debug.Log("볼륨 컨트롤러 생성 완료 (Factory 사용, Container에 추가)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"볼륨 컨트롤러 생성 실패: {ex.Message}");
        }
    }
    
    #endregion

    #region Event Channel Integration

    /// <summary>
    /// Handle sound requests from the event channel
    /// Routes sounds to appropriate service based on AudioData.AudioType and Loop properties
    /// </summary>
    private void HandleSoundRequest(AudioData soundData, object owner = null)
    {
        if (soundData == null)
        {
            Debug.LogWarning("AudioServiceContainer: Received null AudioData from event channel");
            return;
        }

        // Check if services are initialized
        if (!IsFullyInitialized)
        {
            Debug.LogWarning($"AudioServiceContainer: Cannot play '{soundData.name}' - services not initialized");
            return;
        }

        // Route based on AudioType, then check Loop property for playback behavior
        switch (soundData.AudioType)
        {
            case AudioType.BGM:
                // Route to BGM service (background music)
                var bgmSvc = GetService<IBGMAudioService>();
                if (bgmSvc != null)
                {
                    bgmSvc.PlayBGM(soundData);
                    Debug.Log($"AudioServiceContainer: Playing '{soundData.name}' as BGM (AudioType.BGM) via event channel");
                }
                else
                {
                    Debug.LogWarning($"AudioServiceContainer: BGM service not available for '{soundData.name}'");
                }
                break;

            case AudioType.Effect:
                // Route to Effect service (sound effects)
                var effectSvc = GetService<IEffectAudioService>();
                if (effectSvc != null)
                {
                    // Check Loop property to determine playback behavior
                    if (soundData.Loop)
                    {
                        int loopId = effectSvc.PlayEffectLoop(soundData, owner);
                        Debug.Log($"AudioServiceContainer: Playing '{soundData.name}' as loop (Loop=true) via event channel (LoopID: {loopId})");
                    }
                    else
                    {
                        // One-shot playback
                        effectSvc.PlayEffect(soundData);
                        Debug.Log($"AudioServiceContainer: Playing '{soundData.name}' as one-shot (Loop=false) via event channel");
                    }
                }
                else
                {
                    Debug.LogWarning($"AudioServiceContainer: Effect service not available for '{soundData.name}'");
                }
                break;

            default:
                Debug.LogWarning($"AudioServiceContainer: Unknown AudioType '{soundData.AudioType}' for '{soundData.name}'");
                break;
        }
    }

    /// <summary>
    /// [DEPRECATED - 레거시 지원] Handle loop sound playback requests from the event channel
    /// 하위 호환성을 위해 유지되며, 내부적으로 HandleSoundRequest를 호출합니다.
    /// 새 코드는 RaiseSoundEvent(audioData, owner)를 사용하세요.
    /// </summary>
    private void HandleLoopSoundRequest(AudioData audioData, object owner)
    {
        // Redirect to unified handler
        HandleSoundRequest(audioData, owner);
    }

    /// <summary>
    /// [통합 핸들러] Handle loop sound stop requests from the event channel
    /// Stops owner-based loop sounds via unified API
    /// </summary>
    /// <param name="owner">The owner of the loop sounds</param>
    /// <param name="audioData">The specific AudioData to stop (null = stop all loops for this owner)</param>
    private void HandleStopLoopRequest(object owner, AudioData audioData)
    {
        if (owner == null)
        {
            Debug.LogWarning("AudioServiceContainer: Received null owner in StopLoop event");
            return;
        }

        // Check if services are initialized
        if (!IsFullyInitialized)
        {
            Debug.LogWarning($"AudioServiceContainer: Cannot stop loops - services not initialized");
            return;
        }

        // Route to Effect service (루프 사운드는 Effect service에서 관리)
        var effectSvc = GetService<IEffectAudioService>();
        if (effectSvc != null)
        {
            // 통합 메서드 호출: audioData가 null이면 모든 루프 중지, 있으면 특정 루프만 중지
            effectSvc.StopLoopsByOwner(owner, audioData);

            string target = audioData == null ? "모든 루프" : $"'{audioData.name}' 루프";
            Debug.Log($"AudioServiceContainer: Stopped {target} for owner '{owner}' via event channel");
        }
        else
        {
            Debug.LogWarning($"AudioServiceContainer: Effect service not available for stopping loops");
        }
    }

    #endregion

    #region Public Utility Methods
    
    /// <summary>
    /// 서비스 초기화 상태 로깅 (디버깅용)
    /// </summary>
    public void LogInitializationStatus()
    {
        Debug.Log("=== AudioServiceContainer 상태 ===");
        
        var bgmSvc = GetService<IBGMAudioService>();
        var effectSvc = GetService<IEffectAudioService>();
        var volumeCtrl = GetService<IVolumeController>();
        
        Debug.Log($"BGM Service: {(bgmSvc?.IsInitialized == true ? "✓" : "✗")}");
        Debug.Log($"Effect Service: {(effectSvc?.IsInitialized == true ? "✓" : "✗")}");
        Debug.Log($"Volume Controller: {(volumeCtrl != null ? "✓" : "✗")}");
        Debug.Log($"Fully Initialized: {(IsFullyInitialized ? "✓" : "✗")}");
        Debug.Log($"Registry Count: {serviceRegistry.Count}");
        Debug.Log("===========================");
    }
    
    /// <summary>
    /// 모든 서비스 재시작 (디버깅용)
    /// </summary>
    public void RestartAllServices()
    {
        Debug.Log("모든 서비스 재시작");
        CleanupAllServices();
        InitializeAllServices();
    }
    
    /// <summary>
    /// 특정 서비스만 재시작
    /// </summary>
    public void RestartService<T>() where T : class, IAudioService
    {
        var service = GetService<T>();
        if (service != null)
        {
            service.Cleanup();
            service.Initialize();
            Debug.Log($"서비스 재시작 완료: {typeof(T).Name}");
        }
    }
    
    
    /// <summary>
    /// AudioMixer 설정 (런타임 변경용)
    /// </summary>
    public void SetAudioMixer(AudioMixer newAudioMixer)
    {
        if (newAudioMixer == null)
        {
            Debug.LogError("AudioMixer는 null일 수 없습니다.");
            return;
        }
        
        audioMixer = newAudioMixer;
        
        // 모든 서비스에 새 AudioMixer 적용
        if (IsFullyInitialized)
        {
            Debug.Log("새 AudioMixer로 모든 서비스 재시작");
            RestartAllServices();
        }
    }
    
    /// <summary>
    /// 현재 오디오 상태 요약 (디버깅용)
    /// </summary>
    public string GetAudioStatusSummary()
    {
        var summary = "=== Audio Status Summary ===\n";
        
        if (IsFullyInitialized)
        {
            var bgmSvc = GetService<IBGMAudioService>();
            var effectSvc = GetService<IEffectAudioService>();
            var volCtrl = GetService<IVolumeController>();
            
            summary += $"BGM: {(bgmSvc?.IsPlaying == true ? $"Playing '{bgmSvc.CurrentBGMName}'" : "Stopped")}\n";
            summary += $"Effects: {effectSvc?.ActiveEffectsCount ?? 0} active\n";
            
            if (volCtrl != null)
            {
                summary += $"Master Volume: {volCtrl.GetMasterVolumeNormalized():P0} ";
                summary += $"{(volCtrl.IsMasterMuted ? "(Muted)" : "")}\n";
            }
        }
        else
        {
            summary += "Services not fully initialized\n";
        }
        
        summary += "========================";
        return summary;
    }
    
    #endregion
}

/// <summary>
/// 오디오 서비스 팩토리 구현
/// 테스트용 Mock 객체 생성 지원
/// </summary>
public class AudioServiceFactory : IAudioServiceFactory
{
    /// <summary>
    /// BGM 서비스 생성
    /// </summary>
    public IBGMAudioService CreateBGMService(AudioMixer audioMixer, GameObject bgmPlayer)
    {
        if (bgmPlayer == null)
        {
            Debug.LogError("BGM 플레이어 GameObject가 필요합니다.");
            return null;
        }

        var bgmService = bgmPlayer.GetComponent<BGMAudioService>() ??
                        bgmPlayer.AddComponent<BGMAudioService>();

        // AudioMixer 설정 (Reflection 사용)
        var audioMixerField = typeof(BGMAudioService).GetField("audioMixer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        audioMixerField?.SetValue(bgmService, audioMixer);

        return bgmService;
    }

    /// <summary>
    /// 효과음 서비스 생성
    /// </summary>
    public IEffectAudioService CreateEffectService(AudioMixer audioMixer, GameObject effectPlayer)
    {
        if (effectPlayer == null)
        {
            Debug.LogError("효과음 플레이어 GameObject가 필요합니다.");
            return null;
        }

        var effectService = effectPlayer.GetComponent<EffectAudioService>() ??
                           effectPlayer.AddComponent<EffectAudioService>();

        // AudioMixer 설정
        var audioMixerField = typeof(EffectAudioService).GetField("audioMixer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        audioMixerField?.SetValue(effectService, audioMixer);

        return effectService;
    }
    
    /// <summary>
    /// 볼륨 컨트롤러 생성 (별도 GameObject)
    /// </summary>
    public IVolumeController CreateVolumeController(AudioMixer audioMixer)
    {
        var containerGO = new GameObject("VolumeController");
        var volumeController = containerGO.AddComponent<VolumeController>();
        
        // AudioMixer 설정
        var audioMixerField = typeof(VolumeController).GetField("audioMixer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        audioMixerField?.SetValue(volumeController, audioMixer);
        
        return volumeController;
    }
    
    /// <summary>
    /// 볼륨 컨트롤러를 기존 GameObject에 추가
    /// </summary>
    public IVolumeController CreateVolumeControllerOnGameObject(AudioMixer audioMixer, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogError("대상 GameObject가 필요합니다.");
            return null;
        }
        
        var volumeController = targetObject.GetComponent<VolumeController>();
        if (volumeController == null)
        {
            volumeController = targetObject.AddComponent<VolumeController>();
        }
        
        // AudioMixer 설정
        var audioMixerField = typeof(VolumeController).GetField("audioMixer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        audioMixerField?.SetValue(volumeController, audioMixer);
        
        return volumeController;
    }
    
    /// <summary>
    /// 서비스 컨테이너 생성
    /// </summary>
    public IAudioServiceContainer CreateServiceContainer()
    {
        var containerGO = new GameObject("AudioServiceContainer");
        return containerGO.AddComponent<AudioServiceContainer>();
    }
}