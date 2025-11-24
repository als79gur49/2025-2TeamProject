using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 효과음 전용 오디오 서비스 구현
/// AudioData ScriptableObject 기반 효과음 관리
/// </summary>
public class EffectAudioService : MonoBehaviour, IEffectAudioService
{
    private const string MIXER_GROUP_NAME = "Effect";

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private GameObject effectPlayer;
    [SerializeField] private int poolSize = 10;

    private AudioSource mainAudioSource;
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private List<LoopedEffect> loopedEffects = new List<LoopedEffect>();
    private List<DelayedEffect> delayedEffects = new List<DelayedEffect>();

    private bool isInitialized = false;
    private int nextPlayId = 1;

    // 인터페이스 프로퍼티 구현
    public int ActiveEffectsCount => loopedEffects.Count;
    public bool IsInitialized => isInitialized;

    // 이벤트 구현
    public event Action<AudioData> OnEffectCompleted;
    public event Action<AudioData> OnEffectStarted;

    #region Helper Classes

    /// <summary>
    /// 루프 효과음 정보 클래스
    /// Unity GameObject와 AudioSource 생명주기 관리
    /// Owner 기반 추적 지원
    /// </summary>
    [Serializable]
    private class LoopedEffect
    {
        public int loopId;
        public AudioData audioData;
        public AudioSource audioSource;
        public GameObject gameObject;
        public object Owner; // Owner 기반 사운드 제어를 위한 필드

        // 페이드 상태 관리용 필드
        public Coroutine fadeCoroutine;
        public bool isFadingIn;
        public bool isFadingOut;

        public LoopedEffect(int id, AudioData data, AudioSource source, GameObject obj, object owner = null)
        {
            loopId = id;
            audioData = data;
            audioSource = source;
            gameObject = obj;
            Owner = owner;
        }

        /// <summary>
        /// 클립 이름 반환 (하위 호환성용)
        /// </summary>
        public string ClipName => audioData != null ? audioData.name : "None";
    }

    /// <summary>
    /// 지연 효과음 정보 클래스
    /// Unity Coroutine 관리용
    /// </summary>
    private class DelayedEffect
    {
        public int playId;
        public Coroutine coroutine;
        public AudioData audioData;

        public DelayedEffect(int id, Coroutine cor, AudioData data)
        {
            playId = id;
            coroutine = cor;
            audioData = data;
        }

        /// <summary>
        /// 클립 이름 반환 (하위 호환성용)
        /// </summary>
        public string ClipName => audioData != null ? audioData.name : "None";
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Unity Awake: 컴포넌트 초기화
    /// 메인 AudioSource 설정 및 풀 생성
    /// ServiceBootstrap보다 먼저 초기화되도록 Awake에서 Initialize() 호출
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
            mainAudioSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups(MIXER_GROUP_NAME)?[0];
        }

        // ServiceBootstrap.Start()의 ValidateServices()보다 먼저 초기화
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

    #endregion

    #region IEffectAudioService 구현

    /// <summary>
    /// 효과음 재생 (AudioData 기반)
    /// AudioData의 메타데이터(volume, pitch, mixer)를 적용하여 재생
    /// </summary>
    public bool PlayEffect(AudioData audioData)
    {
        if (audioData == null)
        {
            Debug.LogError("EffectAudioService.PlayEffect(AudioData): AudioData가 null입니다.");
            return false;
        }

        // 기존 AudioData 기반 코드는 AudioPlayRequest 래퍼를 통해 공통 로직을 사용합니다.
        var request = AudioPlayRequest.Create(audioData);
        return PlayEffect(request);
    }

    /// <summary>
    /// 효과음 재생 (AudioPlayRequest 기반)
    /// AudioData 기본값에 volume/pitch 배율을 곱하여 최종 값을 결정합니다.
    /// </summary>
    public bool PlayEffect(AudioPlayRequest request)
    {
        var audioData = request.audioData;

        if (!isInitialized || audioData == null)
        {
            Debug.LogError("EffectAudioService가 초기화되지 않았거나 AudioData가 null입니다.");
            return false;
        }

        // Cooldown check
        if (!audioData.CanPlay())
        {
            Debug.Log($"효과음이 쿨다운 중입니다: {audioData.name}");
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
            float baseVolume = audioData.GetRandomVolume();
            float basePitch = audioData.GetRandomPitch();

            // multiplier 적용 후 안전 범위로 클램프
            float volume = Mathf.Clamp01(baseVolume * request.volumeMultiplier);
            float pitch = Mathf.Clamp(basePitch * request.pitchMultiplier, 0.1f, 3.0f);

            // Pitch 변경이 필요한 경우 풀에서 AudioSource 사용
            if (Mathf.Abs(pitch - 1.0f) > 0.01f)
            {
                var pooledSource = GetPooledAudioSource();
                if (pooledSource != null)
                {
                    pooledSource.clip = clip;
                    pooledSource.volume = volume;
                    pooledSource.pitch = pitch;
                    pooledSource.priority = audioData.Priority;

                    // Apply mixer group if specified
                    if (audioData.MixerGroup != null)
                    {
                        pooledSource.outputAudioMixerGroup = audioData.MixerGroup;
                    }

                    pooledSource.Play();

                    // 재생 완료 후 풀로 반환하는 코루틴 시작
                    StartCoroutine(ReturnToPoolAfterPlay(pooledSource, audioData));
                }
                else
                {
                    // 풀이 부족한 경우 기본 AudioSource 사용 (pitch 무시)
                    mainAudioSource.PlayOneShot(clip, volume);
                    Debug.LogWarning($"AudioSource 풀 부족으로 pitch 무시: {audioData.name}");
                }
            }
            else
            {
                // 기본 pitch(1.0)인 경우 PlayOneShot 사용
                mainAudioSource.PlayOneShot(clip, volume);
            }

            // 이벤트 알림
            OnEffectStarted?.Invoke(audioData);
            var playInfo = new AudioPlayInfo(audioData, volume, pitch, false);
            AudioServiceEvents.NotifyAudioPlayStarted(playInfo);

            Debug.Log($"효과음 재생 (AudioData): {audioData.name} (볼륨: {volume:F2}, 피치: {pitch:F2})");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"효과음 재생 실패 (AudioData): {ex.Message}");
            AudioServiceEvents.NotifyAudioPlayError(audioData, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 루프 효과음 재생 (AudioData 기반)
    /// AudioData의 설정을 적용하여 루프 재생
    /// </summary>
    /// <param name="audioData">재생할 AudioData</param>
    /// <param name="owner">루프를 시작한 소유자 객체 (선택사항, Owner 기반 제어용)</param>
    /// <returns>루프 ID (정지용)</returns>
    public int PlayEffectLoop(AudioData audioData, object owner = null)
    {
        if (audioData == null)
        {
            Debug.LogError("EffectAudioService.PlayEffectLoop(AudioData): AudioData가 null입니다.");
            return -1;
        }

        var request = AudioPlayRequest.Create(audioData, owner);
        return PlayEffectLoop(request);
    }

    /// <summary>
    /// 루프 효과음 페이드 인 재생 (AudioData 기반)
    /// </summary>
    public int FadeInEffectLoop(AudioData audioData, float fadeTime = -1f, object owner = null)
    {
        if (audioData == null)
        {
            Debug.LogError("EffectAudioService.FadeInEffectLoop(AudioData): AudioData가 null입니다.");
            return -1;
        }

        var request = AudioPlayRequest.Create(audioData, owner);
        return FadeInEffectLoop(request, fadeTime);
    }

    /// <summary>
    /// 루프 효과음 재생 (AudioPlayRequest 기반)
    /// AudioData의 설정과 runtime modifier를 적용하여 루프 재생
    /// </summary>
    /// <param name="request">재생할 요청 객체</param>
    /// <returns>루프 ID (정지용)</returns>
    public int PlayEffectLoop(AudioPlayRequest request)
    {
        float targetVolume;
        var loopedEffect = CreateLoopedEffect(request, null, out targetVolume);

        if (loopedEffect == null)
            return -1;

        return loopedEffect.loopId;
    }

    /// <summary>
    /// 루프 효과음 페이드 인 재생 (AudioPlayRequest 기반)
    /// </summary>
    public int FadeInEffectLoop(AudioPlayRequest request, float fadeTime = -1f)
    {
        float targetVolume;
        var loopedEffect = CreateLoopedEffect(request, 0f, out targetVolume);

        if (loopedEffect == null)
            return -1;

        float resolvedFadeTime = ResolveFadeInTime(loopedEffect.audioData, fadeTime);

        if (resolvedFadeTime <= 0f)
        {
            // 페이드 시간이 의미 없으면 즉시 목표 볼륨 적용
            if (loopedEffect.audioSource != null)
            {
                loopedEffect.audioSource.volume = targetVolume;
            }
            return loopedEffect.loopId;
        }

        if (loopedEffect.fadeCoroutine != null)
        {
            StopCoroutine(loopedEffect.fadeCoroutine);
        }

        loopedEffect.isFadingIn = true;
        loopedEffect.isFadingOut = false;
        loopedEffect.fadeCoroutine = StartCoroutine(
            FadeLoopCoroutine(loopedEffect, 0f, targetVolume, resolvedFadeTime, false));

        Debug.Log($"루프 효과음 페이드 인 시작: {loopedEffect.ClipName} (ID: {loopedEffect.loopId}, FadeTime: {resolvedFadeTime:F2})");
        return loopedEffect.loopId;
    }

    /// <summary>
    /// 루프 효과음 정지
    /// GameObject 정리 포함
    /// </summary>
    public void StopEffectLoop(int loopId, float fadeTime = 0f)
    {
        for (int i = loopedEffects.Count - 1; i >= 0; i--)
        {
            var loopedEffect = loopedEffects[i];
            if (loopedEffect.loopId == loopId)
            {
                if (fadeTime <= 0f)
                {
                    // 기존 즉시 정지 동작
                    CleanupLoopedEffect(loopedEffect);
                    loopedEffects.RemoveAt(i);
                    Debug.Log($"루프 효과음 정지: {loopedEffect.ClipName} (ID: {loopId})");
                }
                else
                {
                    // 페이드 아웃 정지
                    if (loopedEffect.fadeCoroutine != null)
                    {
                        StopCoroutine(loopedEffect.fadeCoroutine);
                    }

                    float startVolume = loopedEffect.audioSource != null ? loopedEffect.audioSource.volume : 1.0f;
                    float targetVolume = 0.0f;

                    loopedEffect.isFadingOut = true;
                    loopedEffect.isFadingIn = false;
                    loopedEffect.fadeCoroutine = StartCoroutine(
                        FadeLoopCoroutine(loopedEffect, startVolume, targetVolume, fadeTime, true));

                    Debug.Log($"루프 효과음 페이드 아웃 시작: {loopedEffect.ClipName} (ID: {loopId}, FadeTime: {fadeTime:F2})");
                }
                return;
            }
        }

        Debug.LogWarning($"루프 효과음을 찾을 수 없습니다: ID {loopId}");
    }

    /// <summary>
    /// [통합 메서드] 소유자 기반으로 루프 효과음을 정지시킵니다.
    /// audioDataToStop이 null이면 해당 소유자의 모든 루프를,
    /// 특정 AudioData가 주어지면 해당 사운드만 정지시킵니다.
    /// </summary>
    /// <param name="owner">루프 사운드를 시작한 소유자 객체</param>
    /// <param name="audioDataToStop">정지할 대상 AudioData (null일 경우 모두 정지)</param>
    public void StopLoopsByOwner(object owner, AudioData audioDataToStop = null)
    {
        if (owner == null)
        {
            Debug.LogWarning("Owner가 null이므로 루프 효과음을 중지할 수 없습니다.");
            return;
        }

        int stoppedCount = 0;

        // 리스트를 역순으로 순회
        for (int i = loopedEffects.Count - 1; i >= 0; i--)
        {
            var loopedEffect = loopedEffects[i];

            // 1. 소유자가 일치하는지 확인
            if (loopedEffect.Owner == owner)
            {
                // 2. audioDataToStop이 null(모두 중지)이거나,
                //    loopedEffect의 audioData와 일치(특정 사운드 중지)하는 경우
                if (audioDataToStop == null || loopedEffect.audioData == audioDataToStop)
                {
                    CleanupLoopedEffect(loopedEffect);
                    loopedEffects.RemoveAt(i);
                    stoppedCount++;
                }
            }
        }

        string target = audioDataToStop == null ? "모든" : $"'{audioDataToStop.name}'";
        Debug.Log($"소유자({owner})의 {target} 루프 효과음 정지 완료 (정지된 수: {stoppedCount})");
    }

    /// <summary>
    /// [통합 메서드] 소유자 기반으로 루프 효과음을 페이드 아웃 정지시킵니다.
    /// </summary>
    public void FadeOutLoopsByOwner(object owner, float fadeTime = -1f, AudioData audioDataToStop = null)
    {
        if (owner == null)
        {
            Debug.LogWarning("FadeOutLoopsByOwner: Owner가 null이므로 루프 효과음을 페이드 아웃할 수 없습니다.");
            return;
        }

        for (int i = loopedEffects.Count - 1; i >= 0; i--)
        {
            var loopedEffect = loopedEffects[i];

            if (loopedEffect.Owner != owner)
                continue;

            if (audioDataToStop != null && loopedEffect.audioData != audioDataToStop)
                continue;

            float resolvedFadeTime = ResolveFadeOutTime(loopedEffect.audioData, fadeTime);
            StopEffectLoop(loopedEffect.loopId, resolvedFadeTime);
        }
    }

    /// <summary>
    /// [오버로드] 소유자의 모든 루프 효과음을 정지시킵니다.
    /// </summary>
    /// <param name="owner">루프 사운드를 시작한 소유자 객체</param>
    public void StopAllLoopsByOwner(object owner)
    {
        StopLoopsByOwner(owner, null);
    }

    /// <summary>
    /// [오버로드] 소유자의 특정 AudioData에 해당하는 루프 효과음만 정지시킵니다.
    /// </summary>
    /// <param name="owner">루프 사운드를 시작한 소유자 객체</param>
    /// <param name="audioData">정지할 대상 AudioData</param>
    public void StopSpecificLoopByOwner(object owner, AudioData audioData)
    {
        StopLoopsByOwner(owner, audioData);
    }

    /// <summary>
    /// 루프 효과음을 페이드 아웃 정지 (ID 기반)
    /// </summary>
    public void FadeOutEffectLoop(int loopId, float fadeTime = -1f)
    {
        for (int i = 0; i < loopedEffects.Count; i++)
        {
            var loopedEffect = loopedEffects[i];
            if (loopedEffect.loopId != loopId)
                continue;

            float resolvedFadeTime = ResolveFadeOutTime(loopedEffect.audioData, fadeTime);
            StopEffectLoop(loopId, resolvedFadeTime);
            return;
        }

        Debug.LogWarning($"FadeOutEffectLoop: 루프 효과음을 찾을 수 없습니다: ID {loopId}");
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
                Debug.Log($"지연 효과음 취소: {delayed.ClipName} (ID: {playId})");
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

        Debug.Log("모든 효과음 정지");
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

    #endregion

    #region Private Methods

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
    private IEnumerator ReturnToPoolAfterPlay(AudioSource audioSource, AudioData audioData)
    {
        yield return new WaitWhile(() => audioSource.isPlaying);

        // 풀로 반환 (초기화)
        audioSource.clip = null;
        audioSource.volume = 1.0f;
        audioSource.pitch = 1.0f;

        // 완료 이벤트 발생
        OnEffectCompleted?.Invoke(audioData);
        var playInfo = new AudioPlayInfo(audioData, audioSource.volume, audioSource.pitch, false);
        AudioServiceEvents.NotifyAudioPlayCompleted(playInfo);
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
    /// 루프 효과음 페이드 인/아웃용 공통 코루틴
    /// </summary>
    private IEnumerator FadeLoopCoroutine(LoopedEffect loop, float startVolume, float targetVolume, float fadeTime, bool stopAfterFade)
    {
        if (loop == null || loop.audioSource == null)
            yield break;

        var source = loop.audioSource;
        float elapsed = 0f;

        source.volume = startVolume;

        if (fadeTime <= 0f)
        {
            source.volume = targetVolume;
        }
        else
        {
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeTime);
                source.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }

            source.volume = targetVolume;
        }

        if (stopAfterFade && targetVolume <= 0.01f)
        {
            // 정지만 수행하고, 실제 정리 및 리스트 제거는 Update 루프에서 처리합니다.
            source.Stop();
        }

        loop.fadeCoroutine = null;
        loop.isFadingIn = false;
        loop.isFadingOut = false;
    }

    /// <summary>
    /// 페이드 인 시간 해석
    /// </summary>
    private float ResolveFadeInTime(AudioData data, float requested)
    {
        if (requested > 0f) return requested;
        if (data != null && data.FadeInTime > 0f) return data.FadeInTime;
        return 0.25f;
    }

    /// <summary>
    /// 페이드 아웃 시간 해석
    /// </summary>
    private float ResolveFadeOutTime(AudioData data, float requested)
    {
        if (requested > 0f) return requested;
        if (data != null && data.FadeOutTime > 0f) return data.FadeOutTime;
        return 0.25f;
    }

    /// <summary>
    /// 루프 효과음 생성 공통 로직
    /// initialVolumeOverride가 null이면 계산된 최종 볼륨을 사용합니다.
    /// </summary>
    private LoopedEffect CreateLoopedEffect(AudioPlayRequest request, float? initialVolumeOverride, out float targetVolume)
    {
        targetVolume = 0f;

        var audioData = request.audioData;

        if (!isInitialized || audioData == null)
        {
            Debug.LogError("EffectAudioService가 초기화되지 않았거나 AudioData가 null입니다.");
            return null;
        }

        // Cooldown check
        if (!audioData.CanPlay())
        {
            Debug.Log($"루프 효과음이 쿨다운 중입니다: {audioData.name}");
            return null;
        }

        // Get random clip from AudioData
        AudioClip clip = audioData.GetRandomClip();
        if (clip == null)
        {
            Debug.LogError($"AudioData에 유효한 클립이 없습니다: {audioData.name}");
            return null;
        }

        // 루프용 GameObject 및 AudioSource 생성
        var loopGO = new GameObject($"Loop_Effect_{audioData.name}");
        var loopAudioSource = loopGO.AddComponent<AudioSource>();

        loopAudioSource.clip = clip;

        float baseVolume = audioData.GetRandomVolume();
        float basePitch = audioData.GetRandomPitch();

        targetVolume = Mathf.Clamp01(baseVolume * request.volumeMultiplier);
        float pitch = Mathf.Clamp(basePitch * request.pitchMultiplier, 0.1f, 3.0f);

        loopAudioSource.volume = initialVolumeOverride.HasValue ? initialVolumeOverride.Value : targetVolume;
        loopAudioSource.pitch = pitch;
        loopAudioSource.loop = true;
        loopAudioSource.priority = audioData.Priority;

        // Apply mixer group if specified
        if (audioData.MixerGroup != null)
        {
            loopAudioSource.outputAudioMixerGroup = audioData.MixerGroup;
        }
        else
        {
            loopAudioSource.outputAudioMixerGroup = mainAudioSource.outputAudioMixerGroup;
        }

        loopAudioSource.Play();

        int loopId = nextPlayId++;
        var loopedEffect = new LoopedEffect(loopId, audioData, loopAudioSource, loopGO, request.owner);
        loopedEffect.isFadingIn = false;
        loopedEffect.isFadingOut = false;
        loopedEffect.fadeCoroutine = null;

        loopedEffects.Add(loopedEffect);

        string ownerInfo = request.owner != null ? $", Owner: {request.owner}" : "";
        Debug.Log($"루프 효과음 시작 (AudioData): {audioData.name} (ID: {loopId}, 볼륨: {loopAudioSource.volume:F2}, 피치: {loopAudioSource.pitch:F2}{ownerInfo})");

        return loopedEffect;
    }

    #endregion
}
