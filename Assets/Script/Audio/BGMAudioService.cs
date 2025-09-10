using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM 전용 오디오 서비스 구현
/// Unity의 AudioSource와 AudioMixer를 활용한 BGM 관리
/// </summary>
public class BGMAudioService : MonoBehaviour, IBGMAudioService
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private GameObject bgmPlayer;
    
    // Repository 패턴
    private IAudioClipRepository audioRepository;
    [SerializeField] private bool useRepository = true;
    
    private AudioSource audioSource;
    private Coroutine fadeCoroutine;
    private bool isInitialized = false;
    
    // 인터페이스 프로퍼티 구현
    public string CurrentBGMName { get; private set; } = string.Empty;
    public bool IsPlaying => audioSource != null && audioSource.isPlaying;
    public bool IsInitialized => isInitialized;
    
    // 이벤트 구현
    public event Action<string> OnBGMCompleted;
    public event Action<string> OnFadeCompleted;
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Unity Awake: 컴포넌트 초기화
    /// AudioSource 참조 획득 및 기본 설정
    /// </summary>
    private void Awake()
    {
        if (bgmPlayer != null)
        {
            audioSource = bgmPlayer.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = bgmPlayer.AddComponent<AudioSource>();
            }
            
            // BGM 기본 설정
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("BGM")?[0];
        }
    }
    
    /// <summary>
    /// Unity Start: 서비스 자동 초기화
    /// </summary>
    private void Start()
    {
        Initialize();
    }
    
    /// <summary>
    /// Unity Update: BGM 완료 상태 체크
    /// 루프가 아닌 BGM의 종료를 감지
    /// </summary>
    private void Update()
    {
        if (isInitialized && !string.IsNullOrEmpty(CurrentBGMName) && 
            audioSource != null && !audioSource.isPlaying && !audioSource.loop)
        {
            var completedBGM = CurrentBGMName;
            CurrentBGMName = string.Empty;
            OnBGMCompleted?.Invoke(completedBGM);
            
            // 전역 이벤트 알림
            var playInfo = new AudioPlayInfo(completedBGM, audioSource.volume);
            AudioServiceEvents.NotifyAudioPlayCompleted(playInfo);
        }
    }
    
    /// <summary>
    /// Unity OnDestroy: 리소스 정리
    /// </summary>
    private void OnDestroy()
    {
        Cleanup();
    }
    
    #endregion
    
    #region IAudioService 구현
    
    /// <summary>
    /// 서비스 초기화
    /// AudioSource 설정 및 이벤트 등록
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        try
        {
            // AudioSource 유효성 검사
            if (audioSource == null)
            {
                Debug.LogError("BGMAudioService: AudioSource가 없습니다.");
                return;
            }
            
            // AudioMixer 연결 확인
            if (audioMixer == null)
            {
                Debug.LogWarning("BGMAudioService: AudioMixer가 설정되지 않았습니다.");
            }
            
            isInitialized = true;
            AudioServiceEvents.NotifyServiceInitialized(typeof(BGMAudioService));
            
            Debug.Log("BGMAudioService 초기화 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"BGMAudioService 초기화 실패: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 서비스 정리
    /// 재생 중인 BGM 정지 및 코루틴 정리
    /// </summary>
    public void Cleanup()
    {
        if (!isInitialized) return;
        
        StopBGM();
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        OnBGMCompleted = null;
        OnFadeCompleted = null;
        
        isInitialized = false;
        AudioServiceEvents.NotifyServiceCleaned(typeof(BGMAudioService));
        
        Debug.Log("BGMAudioService 정리 완료");
    }
    
    /// <summary>
    /// 클립 존재 여부 확인
    /// </summary>
    public bool HasClip(string clipName)
    {
        return GetClip(clipName) != null;
    }
    
    #endregion
    
    #region IBGMAudioService 구현
    
    /// <summary>
    /// BGM 재생
    /// 기존 BGM을 중단하고 새로운 BGM을 재생
    /// </summary>
    public bool PlayBGM(string clipName, float startRate = 0.0f, bool loop = true)
    {
        return PlayBGM(clipName, out _, startRate, loop);
    }
    
    /// <summary>
    /// BGM 재생 (AudioClip 참조 반환)
    /// Unity의 AudioSource.time을 사용한 시작 지점 설정
    /// </summary>
    public bool PlayBGM(string clipName, out AudioClip audioClip, float startRate = 0.0f, bool loop = true)
    {
        audioClip = null;
        
        if (!isInitialized)
        {
            Debug.LogError("BGMAudioService가 초기화되지 않았습니다.");
            return false;
        }
        
        // 클립 유효성 검증
        audioClip = GetClip(clipName);
        if (audioClip == null)
        {
            Debug.LogError($"BGM 클립을 찾을 수 없습니다: {clipName}");
            AudioServiceEvents.NotifyAudioPlayError(clipName, "클립을 찾을 수 없음");
            return false;
        }
        
        // 시작 지점 유효성 검증
        if (startRate < 0.0f || startRate > 1.0f)
        {
            Debug.LogError($"잘못된 startRate 값: {startRate} (0.0~1.0 범위)");
            return false;
        }
        
        try
        {
            // 기존 페이드 중단
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            
            // BGM 설정 및 재생
            audioSource.clip = audioClip;
            audioSource.loop = loop;
            audioSource.time = startRate * audioClip.length;
            audioSource.Play();
            
            CurrentBGMName = clipName;
            
            // 이벤트 알림
            var playInfo = new AudioPlayInfo(clipName, audioSource.volume, 1.0f, loop);
            AudioServiceEvents.NotifyAudioPlayStarted(playInfo);
            
            Debug.Log($"BGM 재생 시작: {clipName} (시작지점: {startRate:F2})");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"BGM 재생 실패: {ex.Message}");
            AudioServiceEvents.NotifyAudioPlayError(clipName, ex.Message);
            return false;
        }
    }
    
    /// <summary>
    /// BGM 정지
    /// Unity AudioSource.Stop() 사용
    /// </summary>
    public void StopBGM()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log($"BGM 정지: {CurrentBGMName}");
        }
        CurrentBGMName = string.Empty;
    }
    
    /// <summary>
    /// BGM 일시정지
    /// Unity AudioSource.Pause() 사용
    /// </summary>
    public void PauseBGM()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
            Debug.Log($"BGM 일시정지: {CurrentBGMName}");
        }
    }
    
    /// <summary>
    /// BGM 재개
    /// Unity AudioSource.UnPause() 사용
    /// </summary>
    public void ResumeBGM()
    {
        if (audioSource != null && !audioSource.isPlaying && audioSource.clip != null)
        {
            audioSource.UnPause();
            Debug.Log($"BGM 재개: {CurrentBGMName}");
        }
    }
    
    /// <summary>
    /// BGM 페이드 인
    /// Unity Coroutine을 사용한 부드러운 볼륨 증가
    /// </summary>
    public void FadeInBGM(string clipName, float fadeTime = 1.0f, float startRate = 0.0f)
    {
        if (!PlayBGM(clipName, startRate))
            return;
            
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        fadeCoroutine = StartCoroutine(FadeCoroutine(0.0f, 1.0f, fadeTime, false));
    }
    
    /// <summary>
    /// BGM 페이드 아웃
    /// Unity Coroutine을 사용한 부드러운 볼륨 감소
    /// </summary>
    public void FadeOutBGM(float fadeTime = 1.0f, bool stopAfterFade = true)
    {
        if (audioSource == null || !audioSource.isPlaying)
            return;
            
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        fadeCoroutine = StartCoroutine(FadeCoroutine(audioSource.volume, 0.0f, fadeTime, stopAfterFade));
    }
    
    /// <summary>
    /// BGM 크로스페이드
    /// 현재 BGM을 페이드 아웃하면서 새 BGM을 페이드 인
    /// </summary>
    public void CrossFadeBGM(string newClipName, float crossFadeTime = 2.0f)
    {
        if (!HasClip(newClipName))
        {
            Debug.LogError($"크로스페이드할 BGM 클립을 찾을 수 없습니다: {newClipName}");
            return;
        }
        
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        fadeCoroutine = StartCoroutine(CrossFadeCoroutine(newClipName, crossFadeTime));
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// 클립 이름으로 AudioClip 찾기 (Repository 사용)
    /// </summary>
    private AudioClip GetClip(string clipName)
    {
        // Repository 사용
        if (useRepository && audioRepository != null)
        {
            var clip = audioRepository.GetClip(clipName);
            if (clip != null)
            {
                return clip;
            }
            
            Debug.LogWarning($"Repository에서 BGM 클립을 찾을 수 없습니다: {clipName}");
        }
        
        return null;
    }
    
    /// <summary>
    /// AudioClipRepository 주입 (의존성 주입)
    /// </summary>
    public void SetAudioRepository(IAudioClipRepository repository)
    {
        audioRepository = repository;
        Debug.Log($"BGMAudioService: AudioRepository 설정됨 ({(repository != null ? "활성" : "비활성")})");
    }
    
    /// <summary>
    /// Repository를 통해 BGM 클립 데이터 가져오기
    /// </summary>
    private AudioClipData GetClipData(string clipName)
    {
        if (useRepository && audioRepository != null)
        {
            return audioRepository.GetClipData(clipName);
        }
        
        return default(AudioClipData);
    }
    
    /// <summary>
    /// 페이드 코루틴
    /// Unity의 Time.deltaTime을 사용한 부드러운 볼륨 변경
    /// </summary>
    private IEnumerator FadeCoroutine(float startVolume, float targetVolume, float fadeTime, bool stopAfterFade)
    {
        if (audioSource == null) yield break;
        
        float elapsedTime = 0f;
        audioSource.volume = startVolume;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, normalizedTime);
            yield return null;
        }
        
        audioSource.volume = targetVolume;
        
        if (stopAfterFade && targetVolume <= 0.01f)
        {
            StopBGM();
        }
        
        OnFadeCompleted?.Invoke(CurrentBGMName);
        fadeCoroutine = null;
    }
    
    /// <summary>
    /// 크로스페이드 코루틴
    /// 두 개의 AudioSource를 사용하지 않고 단일 AudioSource로 크로스페이드 구현
    /// </summary>
    private IEnumerator CrossFadeCoroutine(string newClipName, float crossFadeTime)
    {
        string oldClipName = CurrentBGMName;
        float halfTime = crossFadeTime * 0.5f;
        
        // 1단계: 현재 BGM 페이드 아웃
        yield return StartCoroutine(FadeCoroutine(audioSource.volume, 0.0f, halfTime, false));
        
        // 2단계: 새 BGM으로 교체 후 페이드 인
        if (PlayBGM(newClipName, 0.0f))
        {
            yield return StartCoroutine(FadeCoroutine(0.0f, 1.0f, halfTime, false));
        }
        
        Debug.Log($"크로스페이드 완료: {oldClipName} → {newClipName}");
        fadeCoroutine = null;
    }
    
    #endregion
}