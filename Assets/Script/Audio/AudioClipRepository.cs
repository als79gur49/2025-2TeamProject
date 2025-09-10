using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 오디오 클립 저장소 구현
/// Unity Inspector에서 설정 가능하며 런타임 관리 지원
/// </summary>
public class AudioClipRepository : MonoBehaviour, IAudioClipRepository
{
    [Header("오디오 클립 데이터")]
    [SerializeField] private AudioClipData[] clipDataArray = new AudioClipData[0];
    
    
    [Header("설정")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool logWarnings = true;
    [SerializeField] private bool supportRuntimeAddition = true;
    
    // 내부 캐시
    private Dictionary<string, AudioClipData> clipDataCache = new Dictionary<string, AudioClipData>();
    private Dictionary<AudioType, List<AudioClipData>> typeCache = new Dictionary<AudioType, List<AudioClipData>>();
    private Dictionary<string, List<AudioClipData>> tagCache = new Dictionary<string, List<AudioClipData>>();
    
    private bool isInitialized = false;
    
    // 프로퍼티
    public bool IsInitialized => isInitialized;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (autoInitialize)
        {
            Initialize();
        }
    }
    
    private void OnValidate()
    {
        // Inspector에서 변경사항이 있을 때 캐시 무효화
        if (isInitialized && Application.isPlaying)
        {
            RebuildCaches();
        }
    }
    
    #endregion
    
    #region IAudioClipRepository 구현
    
    public void Initialize()
    {
        try
        {
            Debug.Log("AudioClipRepository 초기화 시작");
            
            // 캐시 초기화
            clipDataCache.Clear();
            typeCache.Clear();
            tagCache.Clear();
            
            
            // 캐시 구축
            BuildCaches();
            
            // 유효성 검사
            ValidateData();
            
            isInitialized = true;
            Debug.Log($"AudioClipRepository 초기화 완료: {clipDataCache.Count}개 클립 로드됨");
        }
        catch (Exception ex)
        {
            Debug.LogError($"AudioClipRepository 초기화 실패: {ex.Message}");
            isInitialized = false;
        }
    }
    
    public AudioClipData GetClipData(string clipName)
    {
        if (!isInitialized)
        {
            LogWarning("Repository가 초기화되지 않았습니다.");
            return default(AudioClipData);
        }
        
        if (string.IsNullOrEmpty(clipName))
        {
            LogWarning("클립 이름이 비어있습니다.");
            return default(AudioClipData);
        }
        
        if (clipDataCache.TryGetValue(clipName, out AudioClipData clipData))
        {
            return clipData;
        }
        
        LogWarning($"클립을 찾을 수 없습니다: {clipName}");
        return default(AudioClipData);
    }
    
    public AudioClip GetClip(string clipName)
    {
        var clipData = GetClipData(clipName);
        return clipData.IsValid ? clipData.clip : null;
    }
    
    public IEnumerable<AudioClipData> GetClipsByType(AudioType audioType)
    {
        if (!isInitialized)
        {
            LogWarning("Repository가 초기화되지 않았습니다.");
            return Enumerable.Empty<AudioClipData>();
        }
        
        if (typeCache.TryGetValue(audioType, out List<AudioClipData> clips))
        {
            return clips.AsReadOnly();
        }
        
        return Enumerable.Empty<AudioClipData>();
    }
    
    public IEnumerable<AudioClipData> GetClipsByTag(string tag)
    {
        if (!isInitialized || string.IsNullOrEmpty(tag))
        {
            return Enumerable.Empty<AudioClipData>();
        }
        
        if (tagCache.TryGetValue(tag.ToLowerInvariant(), out List<AudioClipData> clips))
        {
            return clips.AsReadOnly();
        }
        
        return Enumerable.Empty<AudioClipData>();
    }
    
    public IEnumerable<AudioClipData> GetAllClips()
    {
        if (!isInitialized)
        {
            return Enumerable.Empty<AudioClipData>();
        }
        
        return clipDataCache.Values.ToList().AsReadOnly();
    }
    
    public bool HasClip(string clipName)
    {
        return isInitialized && !string.IsNullOrEmpty(clipName) && clipDataCache.ContainsKey(clipName);
    }
    
    public void AddClip(AudioClipData clipData)
    {
        if (!supportRuntimeAddition)
        {
            LogWarning("런타임 클립 추가가 비활성화되어 있습니다.");
            return;
        }
        
        if (!clipData.IsValid)
        {
            LogWarning("유효하지 않은 AudioClipData입니다.");
            return;
        }
        
        if (clipDataCache.ContainsKey(clipData.clipName))
        {
            LogWarning($"이미 존재하는 클립입니다: {clipData.clipName}");
            clipDataCache[clipData.clipName] = clipData; // 덮어쓰기
        }
        else
        {
            clipDataCache.Add(clipData.clipName, clipData);
        }
        
        // 캐시 업데이트
        UpdateCacheForClip(clipData);
        
        Debug.Log($"클립 추가됨: {clipData.clipName}");
    }
    
    public bool RemoveClip(string clipName)
    {
        if (!supportRuntimeAddition || !isInitialized || string.IsNullOrEmpty(clipName))
        {
            return false;
        }
        
        if (clipDataCache.TryGetValue(clipName, out AudioClipData clipData))
        {
            clipDataCache.Remove(clipName);
            RemoveFromCaches(clipData);
            Debug.Log($"클립 제거됨: {clipName}");
            return true;
        }
        
        return false;
    }
    
    public void Clear()
    {
        clipDataCache.Clear();
        typeCache.Clear();
        tagCache.Clear();
        isInitialized = false;
        Debug.Log("AudioClipRepository 정리 완료");
    }
    
    #endregion
    
    #region Private Methods
    
    
    /// <summary>
    /// 캐시 구축
    /// </summary>
    private void BuildCaches()
    {
        foreach (var clipData in clipDataArray)
        {
            if (!clipData.IsValid)
            {
                LogWarning($"유효하지 않은 클립 데이터: {clipData.clipName}");
                continue;
            }
            
            // 메인 캐시
            if (!clipDataCache.ContainsKey(clipData.clipName))
            {
                clipDataCache.Add(clipData.clipName, clipData);
            }
            else
            {
                LogWarning($"중복된 클립 이름: {clipData.clipName}");
            }
            
            // 캐시 업데이트
            UpdateCacheForClip(clipData);
        }
    }
    
    /// <summary>
    /// 특정 클립에 대한 캐시 업데이트
    /// </summary>
    private void UpdateCacheForClip(AudioClipData clipData)
    {
        // 타입별 캐시
        if (!typeCache.ContainsKey(clipData.audioType))
        {
            typeCache[clipData.audioType] = new List<AudioClipData>();
        }
        
        if (!typeCache[clipData.audioType].Any(c => c.clipName == clipData.clipName))
        {
            typeCache[clipData.audioType].Add(clipData);
        }
        
        // 태그별 캐시
        if (clipData.tags != null)
        {
            foreach (var tag in clipData.tags)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                
                var lowerTag = tag.ToLowerInvariant();
                if (!tagCache.ContainsKey(lowerTag))
                {
                    tagCache[lowerTag] = new List<AudioClipData>();
                }
                
                if (!tagCache[lowerTag].Any(c => c.clipName == clipData.clipName))
                {
                    tagCache[lowerTag].Add(clipData);
                }
            }
        }
    }
    
    /// <summary>
    /// 캐시에서 클립 제거
    /// </summary>
    private void RemoveFromCaches(AudioClipData clipData)
    {
        // 타입 캐시에서 제거
        if (typeCache.ContainsKey(clipData.audioType))
        {
            typeCache[clipData.audioType].RemoveAll(c => c.clipName == clipData.clipName);
        }
        
        // 태그 캐시에서 제거
        if (clipData.tags != null)
        {
            foreach (var tag in clipData.tags)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                
                var lowerTag = tag.ToLowerInvariant();
                if (tagCache.ContainsKey(lowerTag))
                {
                    tagCache[lowerTag].RemoveAll(c => c.clipName == clipData.clipName);
                }
            }
        }
    }
    
    /// <summary>
    /// 캐시 재구축
    /// </summary>
    private void RebuildCaches()
    {
        if (!isInitialized) return;
        
        typeCache.Clear();
        tagCache.Clear();
        
        foreach (var clipData in clipDataCache.Values)
        {
            UpdateCacheForClip(clipData);
        }
        
        Debug.Log("AudioClipRepository 캐시 재구축 완료");
    }
    
    /// <summary>
    /// 데이터 유효성 검사
    /// </summary>
    private void ValidateData()
    {
        int validCount = 0;
        int invalidCount = 0;
        
        foreach (var kvp in clipDataCache)
        {
            if (kvp.Value.IsValid)
            {
                validCount++;
            }
            else
            {
                invalidCount++;
                LogWarning($"유효하지 않은 클립: {kvp.Key}");
            }
        }
        
        Debug.Log($"데이터 유효성 검사 완료: {validCount}개 유효, {invalidCount}개 무효");
    }
    
    /// <summary>
    /// 경고 로그 출력
    /// </summary>
    private void LogWarning(string message)
    {
        if (logWarnings)
        {
            Debug.LogWarning($"[AudioClipRepository] {message}");
        }
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// 통계 정보 가져오기
    /// </summary>
    public string GetStatistics()
    {
        if (!isInitialized)
        {
            return "Repository가 초기화되지 않았습니다.";
        }
        
        var stats = "=== AudioClipRepository 통계 ===\n";
        stats += $"총 클립 수: {clipDataCache.Count}\n";
        
        foreach (AudioType type in Enum.GetValues(typeof(AudioType)))
        {
            var count = typeCache.ContainsKey(type) ? typeCache[type].Count : 0;
            stats += $"{type}: {count}개\n";
        }
        
        stats += $"태그 수: {tagCache.Count}\n";
        stats += "========================";
        
        return stats;
    }
    
    /// <summary>
    /// Inspector에서 클립 배열 직접 설정 (에디터용)
    /// </summary>
    public void SetClipDataArray(AudioClipData[] newClipDataArray)
    {
        clipDataArray = newClipDataArray ?? new AudioClipData[0];
        
        if (isInitialized)
        {
            RebuildCaches();
        }
    }
    
    #endregion
}