using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 효과음 전용 오디오 서비스 구현
/// Unity의 AudioSource.PlayOneShot과 Object Pooling을 활용한 다중 효과음 관리
/// </summary>
public class EffectAudioService : MonoBehaviour, IEffectAudioService
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private GameObject effectPlayer;
    [SerializeField] private int poolSize = 10;
    
    // Repository 패턴
    private IAudioClipRepository audioRepository;
    [SerializeField] private bool useRepository = true;
    
    private AudioSource mainAudioSource;
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private List<LoopedEffect> loopedEffects = new List<LoopedEffect>();
    private List<DelayedEffect> delayedEffects = new List<DelayedEffect>();
    private List<string> activeEffects = new List<string>();
    
    private bool isInitialized = false;
    private int nextPlayId = 1;
    
    // 인터페이스 프로퍼티 구현
    public int ActiveEffectsCount => activeEffects.Count;
    public bool IsInitialized => isInitialized;
    
    // 이벤트 구현
    public event Action<string> OnEffectCompleted;
    public event Action<string> OnEffectStarted;
    
    #region Helper Classes
    
    /// <summary>
    /// 루프 효과음 정보 클래스
    /// Unity GameObject와 AudioSource 생명주기 관리
    /// </summary>
    [Serializable]
    private class LoopedEffect
    {
        public int loopId;
        public string clipName;
        public AudioSource audioSource;
        public GameObject gameObject;
        
        public LoopedEffect(int id, string name, AudioSource source, GameObject obj)
        {
            loopId = id;
            clipName = name;
            audioSource = source;
            gameObject = obj;
        }
    }
    
    /// <summary>
    /// 지연 효과음 정보 클래스
    /// Unity Coroutine 관리용
    /// </summary>
    private class DelayedEffect
    {
        public int playId;
        public Coroutine coroutine;
        public string clipName;
        
        public DelayedEffect(int id, Coroutine cor, string name)
        {
            playId = id;
            coroutine = cor;
            clipName = name;
        }
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Unity Awake: 컴포넌트 초기화
    /// 메인 AudioSource 설정 및 풀 생성
    /// </summary>
    private void Awake()
    {
        if (effectPlayer != null)
        {
            mainAudioSource = effectPlayer.GetComponent<AudioSource>();
            if (mainAudioSource == null)
            {
                mainAudioSource = effectPlayer.AddComponent<AudioSource>();
            }
            
            // 효과음 기본 설정
            mainAudioSource.loop = false;
            mainAudioSource.playOnAwake = false;
            mainAudioSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("Effect")?[0];
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
    /// Unity Update: 활성 효과음 상태 체크
    /// 루프 효과음 및 지연 효과음 상태 관리
    /// </summary>
    private void Update()
    {
        if (!isInitialized) return;
        
        // 완료된 루프 효과음 정리
        for (int i = loopedEffects.Count - 1; i >= 0; i--)
        {
            var loopedEffect = loopedEffects[i];
            if (loopedEffect.audioSource == null || !loopedEffect.audioSource.isPlaying)
            {
                CleanupLoopedEffect(loopedEffect);
                loopedEffects.RemoveAt(i);
            }
        }
        
        // 활성 효과음 목록 업데이트 (Unity의 AudioSource 상태 기반)
        UpdateActiveEffectsList();
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
    /// AudioSource 풀 생성 및 AudioMixer 연결
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        try
        {
            // 메인 AudioSource 유효성 검사
            if (mainAudioSource == null)
            {
                Debug.LogError("EffectAudioService: AudioSource가 없습니다.");
                return;
            }
            
            // AudioSource 풀 생성
            CreateAudioSourcePool();
            
            // AudioMixer 연결 확인
            if (audioMixer == null)
            {
                Debug.LogWarning("EffectAudioService: AudioMixer가 설정되지 않았습니다.");
            }
            
            isInitialized = true;
            AudioServiceEvents.NotifyServiceInitialized(typeof(EffectAudioService));
            
            Debug.Log($"EffectAudioService 초기화 완료 (풀 크기: {poolSize})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"EffectAudioService 초기화 실패: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 서비스 정리
    /// 모든 효과음 정지 및 풀 정리
    /// </summary>
    public void Cleanup()
    {
        if (!isInitialized) return;
        
        StopAllEffects();
        
        // 지연 효과음 정리
        foreach (var delayed in delayedEffects)
        {
            if (delayed.coroutine != null)
                StopCoroutine(delayed.coroutine);
        }
        delayedEffects.Clear();
        
        // 풀 정리
        ClearAudioSourcePool();
        
        OnEffectCompleted = null;
        OnEffectStarted = null;
        
        isInitialized = false;
        AudioServiceEvents.NotifyServiceCleaned(typeof(EffectAudioService));
        
        Debug.Log("EffectAudioService 정리 완료");
    }
    
    /// <summary>
    /// 클립 존재 여부 확인
    /// </summary>
    public bool HasClip(string clipName)
    {
        return GetClip(clipName) != null;
    }
    
    #endregion
    
    #region IEffectAudioService 구현
    
    /// <summary>
    /// 효과음 재생
    /// Unity AudioSource.PlayOneShot 사용으로 중첩 재생 지원
    /// </summary>
    public bool PlayEffect(string clipName, float volume = 1.0f, float pitch = 1.0f)
    {
        return PlayEffect(clipName, out _, volume, pitch);
    }
    
    /// <summary>
    /// 효과음 재생 (AudioClip 참조 반환)
    /// pitch 변경을 위한 임시 AudioSource 사용
    /// </summary>
    public bool PlayEffect(string clipName, out AudioClip audioClip, float volume = 1.0f, float pitch = 1.0f)
    {
        audioClip = null;
        
        if (!isInitialized)
        {
            Debug.LogError("EffectAudioService가 초기화되지 않았습니다.");
            return false;
        }
        
        // 클립 유효성 검증
        audioClip = GetClip(clipName);
        if (audioClip == null)
        {
            Debug.LogError($"효과음 클립을 찾을 수 없습니다: {clipName}");
            AudioServiceEvents.NotifyAudioPlayError(clipName, "클립을 찾을 수 없음");
            return false;
        }
        
        try
        {
            // Pitch 변경이 필요한 경우 풀에서 AudioSource 사용
            if (Mathf.Abs(pitch - 1.0f) > 0.01f)
            {
                var pooledSource = GetPooledAudioSource();
                if (pooledSource != null)
                {
                    pooledSource.clip = audioClip;
                    pooledSource.volume = volume;
                    pooledSource.pitch = pitch;
                    pooledSource.Play();
                    
                    // 재생 완료 후 풀로 반환하는 코루틴 시작
                    StartCoroutine(ReturnToPoolAfterPlay(pooledSource, clipName));
                }
                else
                {
                    // 풀이 부족한 경우 기본 AudioSource 사용 (pitch 무시)
                    mainAudioSource.PlayOneShot(audioClip, volume);
                    Debug.LogWarning($"AudioSource 풀 부족으로 pitch 무시: {clipName}");
                }
            }
            else
            {
                // 기본 pitch(1.0)인 경우 PlayOneShot 사용
                mainAudioSource.PlayOneShot(audioClip, volume);
            }
            
            // 활성 효과음 목록에 추가
            activeEffects.Add(clipName);
            
            // 이벤트 알림
            OnEffectStarted?.Invoke(clipName);
            var playInfo = new AudioPlayInfo(clipName, volume, pitch, false);
            AudioServiceEvents.NotifyAudioPlayStarted(playInfo);
            
            Debug.Log($"효과음 재생: {clipName} (볼륨: {volume:F2}, 피치: {pitch:F2})");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"효과음 재생 실패: {ex.Message}");
            AudioServiceEvents.NotifyAudioPlayError(clipName, ex.Message);
            return false;
        }
    }
    
    /// <summary>
    /// 지연 효과음 재생
    /// Unity Coroutine을 사용한 지연 실행
    /// </summary>
    public int PlayEffectDelayed(string clipName, float delay, float volume = 1.0f, float pitch = 1.0f)
    {
        if (!HasClip(clipName))
        {
            Debug.LogError($"지연 재생할 효과음 클립을 찾을 수 없습니다: {clipName}");
            return -1;
        }
        
        int playId = nextPlayId++;
        var coroutine = StartCoroutine(DelayedPlayCoroutine(playId, clipName, delay, volume, pitch));
        delayedEffects.Add(new DelayedEffect(playId, coroutine, clipName));
        
        Debug.Log($"지연 효과음 예약: {clipName} ({delay:F2}초 후)");
        return playId;
    }
    
    /// <summary>
    /// 3D 위치 기반 효과음 재생
    /// Unity의 3D Audio 기능 활용
    /// </summary>
    public bool PlayEffect3D(string clipName, Vector3 position, float volume = 1.0f, 
                           float minDistance = 1.0f, float maxDistance = 500.0f)
    {
        var audioClip = GetClip(clipName);
        if (audioClip == null)
        {
            Debug.LogError($"3D 효과음 클립을 찾을 수 없습니다: {clipName}");
            return false;
        }
        
        // 임시 GameObject 생성하여 3D 위치 설정
        var tempGO = new GameObject($"3D_Effect_{clipName}");
        tempGO.transform.position = position;
        
        var audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 1.0f; // 완전한 3D 사운드
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.outputAudioMixerGroup = mainAudioSource.outputAudioMixerGroup;
        audioSource.Play();
        
        // 재생 완료 후 GameObject 정리
        StartCoroutine(Destroy3DEffectAfterPlay(tempGO, audioClip.length, clipName));
        
        Debug.Log($"3D 효과음 재생: {clipName} at {position}");
        return true;
    }
    
    /// <summary>
    /// 루프 효과음 재생
    /// 별도 GameObject와 AudioSource 생성하여 관리
    /// </summary>
    public int PlayEffectLoop(string clipName, float volume = 1.0f)
    {
        var audioClip = GetClip(clipName);
        if (audioClip == null)
        {
            Debug.LogError($"루프 효과음 클립을 찾을 수 없습니다: {clipName}");
            return -1;
        }
        
        // 루프용 GameObject 및 AudioSource 생성
        var loopGO = new GameObject($"Loop_Effect_{clipName}");
        var loopAudioSource = loopGO.AddComponent<AudioSource>();
        
        loopAudioSource.clip = audioClip;
        loopAudioSource.volume = volume;
        loopAudioSource.loop = true;
        loopAudioSource.outputAudioMixerGroup = mainAudioSource.outputAudioMixerGroup;
        loopAudioSource.Play();
        
        int loopId = nextPlayId++;
        var loopedEffect = new LoopedEffect(loopId, clipName, loopAudioSource, loopGO);
        loopedEffects.Add(loopedEffect);
        
        Debug.Log($"루프 효과음 시작: {clipName} (ID: {loopId})");
        return loopId;
    }
    
    /// <summary>
    /// 루프 효과음 정지
    /// GameObject 정리 포함
    /// </summary>
    public void StopEffectLoop(int loopId)
    {
        for (int i = loopedEffects.Count - 1; i >= 0; i--)
        {
            var loopedEffect = loopedEffects[i];
            if (loopedEffect.loopId == loopId)
            {
                CleanupLoopedEffect(loopedEffect);
                loopedEffects.RemoveAt(i);
                Debug.Log($"루프 효과음 정지: {loopedEffect.clipName} (ID: {loopId})");
                return;
            }
        }
        
        Debug.LogWarning($"루프 효과음을 찾을 수 없습니다: ID {loopId}");
    }
    
    /// <summary>
    /// 지연 효과음 재생 취소
    /// Coroutine 정리
    /// </summary>
    public void CancelDelayedEffect(int playId)
    {
        for (int i = delayedEffects.Count - 1; i >= 0; i--)
        {
            var delayed = delayedEffects[i];
            if (delayed.playId == playId)
            {
                if (delayed.coroutine != null)
                    StopCoroutine(delayed.coroutine);
                    
                delayedEffects.RemoveAt(i);
                Debug.Log($"지연 효과음 취소: {delayed.clipName} (ID: {playId})");
                return;
            }
        }
        
        Debug.LogWarning($"지연 효과음을 찾을 수 없습니다: ID {playId}");
    }
    
    /// <summary>
    /// 모든 효과음 정지
    /// 메인, 풀, 루프, 지연 효과음 모두 정리
    /// </summary>
    public void StopAllEffects()
    {
        // 메인 AudioSource 정지
        if (mainAudioSource != null)
            mainAudioSource.Stop();
        
        // 풀의 모든 AudioSource 정지
        foreach (var pooledSource in audioSourcePool)
        {
            if (pooledSource != null && pooledSource.isPlaying)
                pooledSource.Stop();
        }
        
        // 모든 루프 효과음 정리
        foreach (var loopedEffect in loopedEffects)
        {
            CleanupLoopedEffect(loopedEffect);
        }
        loopedEffects.Clear();
        
        // 모든 지연 효과음 취소
        foreach (var delayed in delayedEffects)
        {
            if (delayed.coroutine != null)
                StopCoroutine(delayed.coroutine);
        }
        delayedEffects.Clear();
        
        activeEffects.Clear();
        
        Debug.Log("모든 효과음 정지");
    }
    
    /// <summary>
    /// 특정 클립명의 모든 효과음 정지
    /// </summary>
    public void StopAllEffects(string clipName)
    {
        // 루프 효과음 중에서 해당 클립 정지
        for (int i = loopedEffects.Count - 1; i >= 0; i--)
        {
            var loopedEffect = loopedEffects[i];
            if (loopedEffect.clipName == clipName)
            {
                CleanupLoopedEffect(loopedEffect);
                loopedEffects.RemoveAt(i);
            }
        }
        
        // 지연 효과음 중에서 해당 클립 취소
        for (int i = delayedEffects.Count - 1; i >= 0; i--)
        {
            var delayed = delayedEffects[i];
            if (delayed.clipName == clipName)
            {
                if (delayed.coroutine != null)
                    StopCoroutine(delayed.coroutine);
                delayedEffects.RemoveAt(i);
            }
        }
        
        Debug.Log($"특정 효과음 정지: {clipName}");
    }
    
    /// <summary>
    /// 효과음 풀 크기 설정
    /// 런타임 중 풀 크기 동적 조정
    /// </summary>
    public void SetPoolSize(int newPoolSize)
    {
        if (newPoolSize <= 0)
        {
            Debug.LogError("풀 크기는 0보다 커야 합니다.");
            return;
        }
        
        poolSize = newPoolSize;
        
        if (isInitialized)
        {
            ClearAudioSourcePool();
            CreateAudioSourcePool();
            Debug.Log($"효과음 풀 크기 변경: {poolSize}");
        }
    }
    
    /// <summary>
    /// 현재 재생 중인 효과음 목록 조회
    /// </summary>
    public IReadOnlyList<string> GetActiveEffects()
    {
        return activeEffects.AsReadOnly();
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
            
            Debug.LogWarning($"Repository에서 Effect 클립을 찾을 수 없습니다: {clipName}");
        }
        
        return null;
    }
    
    /// <summary>
    /// AudioClipRepository 주입 (의존성 주입)
    /// </summary>
    public void SetAudioRepository(IAudioClipRepository repository)
    {
        audioRepository = repository;
        Debug.Log($"EffectAudioService: AudioRepository 설정됨 ({(repository != null ? "활성" : "비활성")})");
    }
    
    /// <summary>
    /// Repository를 통해 Effect 클립 데이터 가져오기
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
    /// AudioSource 풀 생성
    /// Unity GameObject 계층 구조 고려한 풀 관리
    /// </summary>
    private void CreateAudioSourcePool()
    {
        var poolParent = new GameObject("EffectAudioSourcePool");
        poolParent.transform.SetParent(transform);
        
        for (int i = 0; i < poolSize; i++)
        {
            var poolGO = new GameObject($"PooledAudioSource_{i}");
            poolGO.transform.SetParent(poolParent.transform);
            
            var audioSource = poolGO.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = mainAudioSource.outputAudioMixerGroup;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            
            audioSourcePool.Add(audioSource);
        }
    }
    
    /// <summary>
    /// AudioSource 풀 정리
    /// </summary>
    private void ClearAudioSourcePool()
    {
        foreach (var audioSource in audioSourcePool)
        {
            if (audioSource != null && audioSource.gameObject != null)
                DestroyImmediate(audioSource.gameObject);
        }
        audioSourcePool.Clear();
    }
    
    /// <summary>
    /// 풀에서 사용 가능한 AudioSource 가져오기
    /// </summary>
    private AudioSource GetPooledAudioSource()
    {
        foreach (var audioSource in audioSourcePool)
        {
            if (audioSource != null && !audioSource.isPlaying)
                return audioSource;
        }
        return null;
    }
    
    /// <summary>
    /// 재생 완료 후 풀로 AudioSource 반환
    /// </summary>
    private IEnumerator ReturnToPoolAfterPlay(AudioSource audioSource, string clipName)
    {
        yield return new WaitWhile(() => audioSource.isPlaying);
        
        // 풀로 반환 (초기화)
        audioSource.clip = null;
        audioSource.volume = 1.0f;
        audioSource.pitch = 1.0f;
        
        // 완료 이벤트 발생
        OnEffectCompleted?.Invoke(clipName);
        var playInfo = new AudioPlayInfo(clipName, audioSource.volume, audioSource.pitch, false);
        AudioServiceEvents.NotifyAudioPlayCompleted(playInfo);
    }
    
    /// <summary>
    /// 지연 재생 코루틴
    /// </summary>
    private IEnumerator DelayedPlayCoroutine(int playId, string clipName, float delay, float volume, float pitch)
    {
        yield return new WaitForSeconds(delay);
        
        // 지연 목록에서 제거
        for (int i = delayedEffects.Count - 1; i >= 0; i--)
        {
            if (delayedEffects[i].playId == playId)
            {
                delayedEffects.RemoveAt(i);
                break;
            }
        }
        
        // 실제 재생
        PlayEffect(clipName, volume, pitch);
    }
    
    /// <summary>
    /// 3D 효과음 GameObject 정리
    /// </summary>
    private IEnumerator Destroy3DEffectAfterPlay(GameObject effectGO, float clipLength, string clipName)
    {
        yield return new WaitForSeconds(clipLength + 0.1f); // 여유시간 추가
        
        if (effectGO != null)
            Destroy(effectGO);
            
        OnEffectCompleted?.Invoke(clipName);
    }
    
    /// <summary>
    /// 루프 효과음 정리
    /// </summary>
    private void CleanupLoopedEffect(LoopedEffect loopedEffect)
    {
        if (loopedEffect.audioSource != null)
            loopedEffect.audioSource.Stop();
            
        if (loopedEffect.gameObject != null)
            Destroy(loopedEffect.gameObject);
    }
    
    /// <summary>
    /// 활성 효과음 목록 업데이트
    /// Unity AudioSource 상태 기반으로 자동 정리
    /// </summary>
    private void UpdateActiveEffectsList()
    {
        // PlayOneShot은 isPlaying으로 추적하기 어려우므로
        // 시간 기반 또는 이벤트 기반으로 관리하는 것이 좋습니다.
        // 여기서는 간단히 일정 시간 후 자동 정리하는 방식을 사용합니다.
        
        // 실제 구현에서는 각 효과음의 재생 시간을 추적하여
        // 더 정확한 상태 관리를 할 수 있습니다.
    }
    
    #endregion
}