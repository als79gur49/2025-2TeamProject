using UnityEngine;

/// <summary>
/// AudioServiceContainer 테스트 스크립트
/// serviceRegistry 기능 및 GetService<T>() 메서드 검증
/// </summary>
public class AudioServiceContainerTest : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool runTestOnStart = true;
    [SerializeField] private bool verboseLogging = true;
    
    private void Start()
    {
        if (runTestOnStart)
        {
            StartCoroutine(RunTests());
        }
    }
    
    private System.Collections.IEnumerator RunTests()
    {
        // Container 인스턴스 확인 대기
        yield return new WaitForSeconds(0.5f);
        
        Log("=== AudioServiceContainer 테스트 시작 ===");
        
        // 1. Container 인스턴스 테스트
        TestContainerInstance();
        
        // 2. 서비스 초기화 테스트
        TestServiceInitialization();
        
        // 3. GetService<T>() 메서드 테스트
        TestGetServiceMethod();
        
        // 4. serviceRegistry 기능 테스트
        TestServiceRegistry();
        
        // 5. 지연 초기화 테스트
        TestLazyInitialization();
        
        Log("=== AudioServiceContainer 테스트 완료 ===");
    }
    
    private void TestContainerInstance()
    {
        Log("1. Container 인스턴스 테스트");
        
        var container = AudioServiceContainer.Instance;
        
        if (container != null)
        {
            Log("✓ Container 인스턴스 생성 성공");
            container.LogInitializationStatus();
        }
        else
        {
            Log("✗ Container 인스턴스 생성 실패");
        }
    }
    
    private void TestServiceInitialization()
    {
        Log("\n2. 서비스 초기화 테스트");
        
        var container = AudioServiceContainer.Instance;
        container.InitializeAllServices();
        
        if (container.IsFullyInitialized)
        {
            Log("✓ 모든 서비스 초기화 성공");
        }
        else
        {
            Log("✗ 서비스 초기화 실패");
            container.LogInitializationStatus();
        }
    }
    
    private void TestGetServiceMethod()
    {
        Log("\n3. GetService<T>() 메서드 테스트");
        
        var container = AudioServiceContainer.Instance;
        
        // BGM 서비스 테스트
        var bgmService = container.GetService<IBGMAudioService>();
        Log($"BGM Service: {(bgmService != null ? "✓" : "✗")} ({bgmService?.GetType().Name})");
        
        // 효과음 서비스 테스트
        var effectService = container.GetService<IEffectAudioService>();
        Log($"Effect Service: {(effectService != null ? "✓" : "✗")} ({effectService?.GetType().Name})");
        
        // 볼륨 컨트롤러 테스트
        var volumeController = container.GetService<IVolumeController>();
        Log($"Volume Controller: {(volumeController != null ? "✓" : "✗")} ({volumeController?.GetType().Name})");
        
        // 존재하지 않는 서비스 테스트
        var nonExistentService = container.GetService<AudioServiceContainerTest>();
        Log($"Non-existent Service: {(nonExistentService == null ? "✓" : "✗")} (올바르게 null 반환)");
    }
    
    private void TestServiceRegistry()
    {
        Log("\n4. serviceRegistry 기능 테스트");
        
        var container = AudioServiceContainer.Instance;
        
        // 현재 등록된 서비스 수 확인
        container.LogInitializationStatus();
        
        // 테스트용 Mock 서비스 등록
        var mockBGMService = new MockBGMAudioService();
        container.RegisterService<IBGMAudioService>(mockBGMService);
        
        // 등록된 Mock 서비스 확인
        var retrievedMockService = container.GetService<IBGMAudioService>();
        if (retrievedMockService is MockBGMAudioService)
        {
            Log("✓ Mock 서비스 등록 및 검색 성공");
        }
        else
        {
            Log("✗ Mock 서비스 등록 또는 검색 실패");
        }
    }
    
    private void TestLazyInitialization()
    {
        Log("\n5. 지연 초기화 테스트");
        
        // 새 Container 인스턴스 생성 (테스트용)
        var testGO = new GameObject("TestContainer");
        var testContainer = testGO.AddComponent<AudioServiceContainer>();
        
        // autoCreateServices가 true인 상태에서 서비스 요청
        var bgmService = testContainer.GetService<IBGMAudioService>();
        
        if (bgmService != null)
        {
            Log("✓ 지연 초기화를 통한 BGM 서비스 생성 성공");
        }
        else
        {
            Log("✗ 지연 초기화를 통한 BGM 서비스 생성 실패");
        }
        
        // 테스트 정리
        DestroyImmediate(testGO);
    }
    
    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[AudioServiceContainerTest] {message}");
        }
    }
}

/// <summary>
/// 테스트용 Mock BGM Audio Service
/// </summary>
public class MockBGMAudioService : IBGMAudioService
{
    public bool IsInitialized { get; private set; } = false;
    public bool IsPlaying => false;
    public string CurrentBGMName => "MockBGM";
    public int ActiveEffectsCount => 0;
    
    public void Initialize()
    {
        IsInitialized = true;
        Debug.Log("MockBGMAudioService Initialized");
    }
    
    public void Cleanup()
    {
        IsInitialized = false;
        Debug.Log("MockBGMAudioService Cleaned up");
    }
    
    public bool PlayBGM(string clipName, float startRate = 0.0f, bool loop = true) { return true; }
    public bool PlayBGM(string clipName, out AudioClip audioClip, float startRate = 0.0f, bool loop = true) 
    { 
        audioClip = null; 
        return true; 
    }
    public void StopBGM() { }
    public void PauseBGM() { }
    public void ResumeBGM() { }
    public void FadeInBGM(string clipName, float fadeTime = 1.0f, float startRate = 0.0f) { }
    public void FadeOutBGM(float fadeTime = 1.0f, bool stopAfterFade = true) { }
    public void CrossFadeBGM(string newClipName, float crossFadeTime = 2.0f) { }

    public bool HasClip(string clipName)
    {
        throw new System.NotImplementedException();
    }

    public event System.Action<string> OnBGMCompleted;
    public event System.Action<string> OnFadeCompleted;
}