using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM 전용 오디오 서비스 구현
/// AudioData ScriptableObject 기반 BGM 관리
/// </summary>
public class BGMAudioService : MonoBehaviour, IBGMAudioService
{
    private const string MIXER_GROUP_NAME = "BGM";

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private GameObject bgmPlayer;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;
    private bool isInitialized = false;
    private AudioData currentBGM;

    // 인터페이스 프로퍼티 구현
    public AudioData CurrentBGM => currentBGM;
    public string CurrentBGMName => currentBGM != null ? currentBGM.name : string.Empty;
    public bool IsPlaying => audioSource != null && audioSource.isPlaying;
    public bool IsInitialized => isInitialized;

    // 이벤트 구현
    public event Action<AudioData> OnBGMCompleted;
    public event Action<AudioData> OnFadeCompleted;

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
            audioSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups(MIXER_GROUP_NAME)?[0];
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
        if (isInitialized && currentBGM != null &&
            audioSource != null && !audioSource.isPlaying && !audioSource.loop)
        {
            var completedBGM = currentBGM;
            currentBGM = null;
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

    #endregion

    #region IBGMAudioService 구현

    /// <summary>
    /// BGM 재생 (AudioData 기반)
    /// AudioData의 메타데이터(volume, pitch, mixer)를 적용하여 재생
    /// </summary>
    public bool PlayBGM(AudioData audioData)
    {
        if (!isInitialized || audioData == null)
        {
            Debug.LogError("BGMAudioService가 초기화되지 않았거나 AudioData가 null입니다.");
            return false;
        }

        // Cooldown check
        if (!audioData.CanPlay())
        {
            Debug.Log($"BGM이 쿨다운 중입니다: {audioData.name}");
            return false;
        }

        // Get random clip from AudioData
        AudioClip clip = audioData.GetRandomClip();
        if (clip == null)
        {
            Debug.LogError($"AudioData에 유효한 클립이 없습니다: {audioData.name}");
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

            // Apply settings from AudioData
            audioSource.clip = clip;
            audioSource.volume = audioData.GetRandomVolume();
            audioSource.pitch = audioData.GetRandomPitch();
            audioSource.loop = audioData.Loop;
            audioSource.priority = audioData.Priority;

            // Apply mixer group if specified
            if (audioData.MixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = audioData.MixerGroup;
            }

            audioSource.Play();

            currentBGM = audioData;

            // 이벤트 알림
            var playInfo = new AudioPlayInfo(audioData, audioSource.volume, audioSource.pitch, audioData.Loop);
            AudioServiceEvents.NotifyAudioPlayStarted(playInfo);

            Debug.Log($"BGM 재생 (AudioData): {audioData.name} (볼륨: {audioSource.volume:F2}, 피치: {audioSource.pitch:F2})");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"BGM 재생 실패 (AudioData): {ex.Message}");
            AudioServiceEvents.NotifyAudioPlayError(audioData, ex.Message);
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
        currentBGM = null;
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
    /// BGM 페이드 인 (AudioData 기반)
    /// AudioData의 FadeInTime을 사용하거나 지정된 fadeTime 사용
    /// </summary>
    public void FadeInBGM(AudioData audioData, float fadeTime)
    {
        if (!PlayBGM(audioData))
            return;

        // Use specified fadeTime or AudioData's FadeInTime
        float actualFadeTime = fadeTime > 0 ? fadeTime : audioData.FadeInTime;
        if (actualFadeTime <= 0)
            actualFadeTime = 1.0f; // Default fade time

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeCoroutine(0.0f, audioData.GetRandomVolume(), actualFadeTime, false));

        Debug.Log($"BGM 페이드 인 (AudioData): {audioData.name} ({actualFadeTime:F2}초)");
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
    /// BGM 크로스페이드 (AudioData 기반)
    /// 현재 BGM을 페이드 아웃하면서 새 AudioData BGM을 페이드 인
    /// </summary>
    public void CrossFadeBGM(AudioData newAudioData, float crossFadeTime)
    {
        if (newAudioData == null)
        {
            Debug.LogError("크로스페이드할 AudioData가 null입니다.");
            return;
        }

        if (newAudioData.GetRandomClip() == null)
        {
            Debug.LogError($"AudioData에 유효한 클립이 없습니다: {newAudioData.name}");
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(CrossFadeCoroutineAudioData(newAudioData, crossFadeTime));

        Debug.Log($"BGM 크로스페이드 시작 (AudioData): {CurrentBGMName} → {newAudioData.name}");
    }

    #endregion

    #region Private Methods

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

        OnFadeCompleted?.Invoke(currentBGM);
        fadeCoroutine = null;
    }

    /// <summary>
    /// 크로스페이드 코루틴 (AudioData 기반)
    /// AudioData의 설정을 적용하여 크로스페이드 구현
    /// </summary>
    private IEnumerator CrossFadeCoroutineAudioData(AudioData newAudioData, float crossFadeTime)
    {
        AudioData oldBGM = currentBGM;
        float halfTime = crossFadeTime * 0.5f;

        // 1단계: 현재 BGM 페이드 아웃
        yield return StartCoroutine(FadeCoroutine(audioSource.volume, 0.0f, halfTime, false));

        // 2단계: 새 AudioData BGM으로 교체 후 페이드 인
        if (PlayBGM(newAudioData))
        {
            float targetVolume = newAudioData.GetRandomVolume();
            yield return StartCoroutine(FadeCoroutine(0.0f, targetVolume, halfTime, false));
        }

        string oldName = oldBGM != null ? oldBGM.name : "None";
        Debug.Log($"크로스페이드 완료 (AudioData): {oldName} → {newAudioData.name}");
        fadeCoroutine = null;
    }

    #endregion
}
