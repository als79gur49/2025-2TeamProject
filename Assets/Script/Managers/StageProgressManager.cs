using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.SaveSystem;
using Game.Core;
using Game.Data;

namespace Game.Managers
{
    /// <summary>
    /// 스테이지 진행도 관리자
    /// Dictionary 기반 효율적인 데이터 관리
    /// </summary>
    public class StageProgressManager : MonoBehaviour
    {
        #region Singleton
        private static StageProgressManager instance;
        public static StageProgressManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<StageProgressManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("StageProgressManager");
                        instance = go.AddComponent<StageProgressManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        #endregion

        #region Fields
        [Header("Stage Database")]
        [SerializeField]
        private List<StageDataSO> stageDatabase = new List<StageDataSO>();
        
        [Header("Settings")]
        [SerializeField]
        private bool autoSaveEnabled = true;
        
        [SerializeField]
        private float autoSaveInterval = 60f;  // 60초마다 자동 저장
        
        [SerializeField]
        private bool debugMode = false;

        // Runtime Data
        private StageProgressData progressData;
        private Dictionary<string, StageDataSO> stageLookup;
        private ISaveDataAdapter saveAdapter;
        private PlaySession currentSession;
        private float lastAutoSaveTime;
        #endregion

        #region Events
        public event Action<string> OnStageUnlocked;
        public event Action<string, int, int> OnStageCompleted;
        public event Action<string, int> OnStageScoreUpdated;
        public event Action<string, StageState> OnStageStateChanged;
        public event Action<StageProgressData> OnProgressUpdated;
        public event Action<float> OnOverallProgressChanged;
        #endregion

        #region Initialization

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }

        public void Initialize()
        {
            // 새로운 진행도 데이터 생성
            progressData = new StageProgressData();
            
            // 스테이지 룩업 테이블 생성
            BuildStageLookup();
            
            // SaveDataAdapter 연결
            ConnectSaveAdapter();
            
            // ServiceLocator 등록
            RegisterToServiceLocator();
            
            // 첫 스테이지 자동 해금
            UnlockInitialStages();
            
            Debug.Log($"[StageProgressManager] Initialized with {stageDatabase.Count} stages");
        }

        private void BuildStageLookup()
        {
            stageLookup = new Dictionary<string, StageDataSO>();
            
            foreach (var stage in stageDatabase)
            {
                if (stage != null)
                {
                    stageLookup[stage.StageId] = stage;
                }
            }
        }

        private void ConnectSaveAdapter()
        {
            if (ServiceLocator.IsRegistered<ISaveDataAdapter>())
            {
                saveAdapter = ServiceLocator.Get<ISaveDataAdapter>();
                Debug.Log("[StageProgressManager] Connected to SaveDataAdapter");
            }
            else
            {
                Debug.LogWarning("[StageProgressManager] SaveDataAdapter not found in ServiceLocator");
            }
        }

        private void RegisterToServiceLocator()
        {
            if (!ServiceLocator.IsRegistered<StageProgressManager>())
            {
                ServiceLocator.RegisterSingleton<StageProgressManager, StageProgressManager>(this);
            }
        }

        private void UnlockInitialStages()
        {
            // 튜토리얼 및 첫 스테이지 자동 해금
            foreach (var stage in stageDatabase)
            {
                if (stage.Type == StageType.Tutorial || 
                    (stage.ChapterId == "chapter1" && stage.StageNumber == 1))
                {
                    UnlockStage(stage.StageId, false);
                }
            }
        }

        #endregion

        #region Stage Operations

        /// <summary>
        /// 스테이지 시작
        /// </summary>
        public void StartStage(string stageId)
        {
            if (!IsStageUnlocked(stageId))
            {
                Debug.LogError($"[StageProgressManager] Cannot start locked stage: {stageId}");
                return;
            }

            // 현재 세션 생성
            currentSession = new PlaySession();
            
            // 레코드 가져오기 또는 생성
            var record = progressData.GetOrCreateRecord(stageId);
            
            // 상태 업데이트
            if (record.state == StageState.Unlocked)
            {
                record.state = StageState.InProgress;
                OnStageStateChanged?.Invoke(stageId, StageState.InProgress);
            }
            
            // 현재 진행중 스테이지 설정
            progressData.currentStageId = stageId;
            
            Debug.Log($"[StageProgressManager] Started stage: {stageId}");
        }

        /// <summary>
        /// 스테이지 완료
        /// </summary>
        public void CompleteStage(string stageId, int score, Dictionary<string, int> statistics = null)
        {
            if (currentSession == null)
            {
                Debug.LogWarning("[StageProgressManager] No active session");
                currentSession = new PlaySession();
            }

            // 세션 정보 업데이트
            currentSession.endTime = DateTime.Now;
            currentSession.score = score;
            currentSession.completed = true;
            currentSession.playTime = (float)(currentSession.endTime - currentSession.startTime).TotalSeconds;
            
            if (statistics != null)
            {
                currentSession.statistics = statistics;
            }

            // 스테이지 데이터 가져오기
            var stageData = GetStageData(stageId);
            if (stageData == null)
            {
                Debug.LogError($"[StageProgressManager] Stage data not found: {stageId}");
                return;
            }

            // 별 계산
            int stars = stageData.CalculateStars(score);
            currentSession.stars = stars;

            // 레코드 업데이트
            var record = progressData.GetOrCreateRecord(stageId);
            bool isFirstClear = record.state != StageState.Cleared && record.state != StageState.Perfect;
            
            // 세션 추가
            record.playSessions.Add(currentSession);
            
            // 최고 기록 업데이트
            if (score > record.bestScore)
            {
                record.bestScore = score;
                record.bestStars = stars;
                OnStageScoreUpdated?.Invoke(stageId, score);
            }

            // 상태 업데이트
            if (isFirstClear)
            {
                record.firstClearDate = DateTime.Now;
            }
            
            record.state = stars >= 3 ? StageState.Perfect : StageState.Cleared;
            record.lastPlayDate = DateTime.Now;
            
            // 통계 업데이트
            UpdateStatistics(record, currentSession);
            
            // 전체 진행도 업데이트
            UpdateGlobalProgress();
            
            // 다음 스테이지 자동 해금 체크
            CheckAndUnlockNextStages();
            
            // 보상 계산
            var rewards = stageData.CalculateRewards(score, stars, isFirstClear);
            ProcessRewards(rewards);
            
            // 이벤트 발생
            OnStageCompleted?.Invoke(stageId, score, stars);
            OnStageStateChanged?.Invoke(stageId, record.state);
            OnProgressUpdated?.Invoke(progressData);
            
            // 자동 저장
            if (autoSaveEnabled)
            {
                SaveProgress();
            }
            
            Debug.Log($"[StageProgressManager] Completed stage {stageId}: Score={score}, Stars={stars}");
            
            // 세션 클리어
            currentSession = null;
        }

        /// <summary>
        /// 스테이지 해금
        /// </summary>
        public void UnlockStage(string stageId, bool save = true)
        {
            var record = progressData.GetOrCreateRecord(stageId);
            
            if (record.state == StageState.Locked)
            {
                record.state = StageState.Unlocked;
                record.firstUnlockDate = DateTime.Now;
                
                progressData.lastUnlockedStageId = stageId;
                progressData.globalStats.totalStagesUnlocked++;
                
                OnStageUnlocked?.Invoke(stageId);
                OnStageStateChanged?.Invoke(stageId, StageState.Unlocked);
                OnProgressUpdated?.Invoke(progressData);
                
                if (save && autoSaveEnabled)
                {
                    SaveProgress();
                }
                
                Debug.Log($"[StageProgressManager] Unlocked stage: {stageId}");
            }
        }

        /// <summary>
        /// 통계 업데이트
        /// </summary>
        private void UpdateStatistics(StageRecord record, PlaySession session)
        {
            if (session.statistics != null)
            {
                foreach (var stat in session.statistics)
                {
                    // 스테이지 통계
                    if (stat.Key == "enemies_defeated")
                        record.statistics.totalEnemiesDefeated += stat.Value;
                    else if (stat.Key == "damage_dealt")
                        record.statistics.totalDamageDealt += stat.Value;
                    else if (stat.Key == "damage_taken")
                        record.statistics.totalDamageTaken += stat.Value;
                    else
                        record.statistics.customStats[stat.Key] = stat.Value;
                }
                
                // 퍼펙트 클리어 체크
                if (session.statistics.ContainsKey("damage_taken") && 
                    session.statistics["damage_taken"] == 0)
                {
                    record.statistics.perfectClearCount++;
                }
            }
            
            // 최고 클리어 시간 업데이트
            if (record.statistics.bestClearTime == 0 || 
                session.playTime < record.statistics.bestClearTime)
            {
                record.statistics.bestClearTime = session.playTime;
            }
        }

        /// <summary>
        /// 전체 진행도 업데이트
        /// </summary>
        private void UpdateGlobalProgress()
        {
            var stats = progressData.globalStats;
            
            // 통계 재계산
            stats.totalStagesUnlocked = progressData.stageRecords.Count(r => 
                r.Value.state != StageState.Locked);
            
            stats.totalStagesCleared = progressData.stageRecords.Count(r => 
                r.Value.state == StageState.Cleared || r.Value.state == StageState.Perfect);
            
            stats.totalStarCount = progressData.stageRecords.Sum(r => r.Value.bestStars);
            stats.totalScore = progressData.stageRecords.Sum(r => r.Value.bestScore);
            stats.lastPlayDate = DateTime.Now;
            
            // 챕터별 진행도 업데이트
            UpdateChapterProgress();
            
            // 전체 진행률 이벤트
            float overallProgress = progressData.CalculateOverallProgress(stageDatabase.Count);
            OnOverallProgressChanged?.Invoke(overallProgress);
        }

        /// <summary>
        /// 챕터별 진행도 업데이트
        /// </summary>
        private void UpdateChapterProgress()
        {
            var chapterGroups = stageDatabase.GroupBy(s => s.ChapterId);
            
            foreach (var chapter in chapterGroups)
            {
                int totalInChapter = chapter.Count();
                int clearedInChapter = chapter.Count(s => IsStageCleared(s.StageId));
                
                float progress = totalInChapter > 0 ? (float)clearedInChapter / totalInChapter : 0f;
                progressData.UpdateChapterProgress(chapter.Key, progress);
            }
        }

        /// <summary>
        /// 다음 스테이지 자동 해금
        /// </summary>
        private void CheckAndUnlockNextStages()
        {
            foreach (var stage in stageDatabase)
            {
                if (!IsStageUnlocked(stage.StageId) && stage.IsUnlocked(progressData))
                {
                    UnlockStage(stage.StageId, false);
                }
            }
        }

        /// <summary>
        /// 보상 처리
        /// </summary>
        private void ProcessRewards(StageRewardResult rewards)
        {
            // 여기서 실제 보상 처리 로직 구현
            // 예: PlayerDataManager를 통한 코인/경험치 추가
            
            if (debugMode)
            {
                Debug.Log($"[StageProgressManager] Rewards: Coins={rewards.coins}, Exp={rewards.exp}, Items={string.Join(",", rewards.items)}");
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// 스테이지 데이터 가져오기
        /// </summary>
        public StageDataSO GetStageData(string stageId)
        {
            return stageLookup.TryGetValue(stageId, out var stage) ? stage : null;
        }

        /// <summary>
        /// 스테이지 해금 여부
        /// </summary>
        public bool IsStageUnlocked(string stageId)
        {
            return progressData.IsStageUnlocked(stageId);
        }

        /// <summary>
        /// 스테이지 클리어 여부
        /// </summary>
        public bool IsStageCleared(string stageId)
        {
            return progressData.IsStageCleared(stageId);
        }

        /// <summary>
        /// 스테이지 상태 가져오기
        /// </summary>
        public StageState GetStageState(string stageId)
        {
            return progressData.GetStageState(stageId);
        }

        /// <summary>
        /// 스테이지 레코드 가져오기
        /// </summary>
        public StageRecord GetStageRecord(string stageId)
        {
            return progressData.GetRecord(stageId);
        }

        /// <summary>
        /// 챕터의 모든 스테이지 가져오기
        /// </summary>
        public List<StageDataSO> GetStagesInChapter(string chapterId)
        {
            return stageDatabase
                .Where(s => s.ChapterId == chapterId)
                .OrderBy(s => s.StageNumber)
                .ToList();
        }

        /// <summary>
        /// 해금된 스테이지 목록
        /// </summary>
        public List<StageDataSO> GetUnlockedStages()
        {
            return stageDatabase
                .Where(s => IsStageUnlocked(s.StageId))
                .ToList();
        }

        /// <summary>
        /// 진행도 데이터 전체 가져오기
        /// </summary>
        public StageProgressData GetProgressData()
        {
            return progressData;
        }

        /// <summary>
        /// 전체 진행률
        /// </summary>
        public float GetOverallProgress()
        {
            return progressData.CalculateOverallProgress(stageDatabase.Count);
        }

        /// <summary>
        /// 챕터 진행률
        /// </summary>
        public float GetChapterProgress(string chapterId)
        {
            return progressData.chapterProgress.TryGetValue(chapterId, out float progress) 
                ? progress : 0f;
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// 진행도 저장
        /// </summary>
        public void SaveProgress()
        {
            if (saveAdapter == null)
            {
                Debug.LogWarning("[StageProgressManager] SaveAdapter not available");
                return;
            }

            try
            {
                progressData.lastModified = DateTime.Now;

                // SaveDataAdapter를 통해 저장
                var saveData = new StageProgressSaveData(progressData);
                
                // 임시: JSON으로 직렬화하여 저장
                string json = JsonUtility.ToJson(saveData);
                PlayerPrefs.SetString("StageProgress", json);
                PlayerPrefs.Save();
                
                Debug.Log("[StageProgressManager] Progress saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"[StageProgressManager] Save failed: {e.Message}");
            }
        }

        /// <summary>
        /// 진행도 불러오기
        /// </summary>
        public void LoadProgress()
        {
            try
            {
                string json = PlayerPrefs.GetString("StageProgress", "");
                if (!string.IsNullOrEmpty(json))
                {
                    var saveData = JsonUtility.FromJson<StageProgressSaveData>(json);
                    progressData = saveData.progressData;
                    
                    OnProgressUpdated?.Invoke(progressData);
                    Debug.Log("[StageProgressManager] Progress loaded");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[StageProgressManager] Load failed: {e.Message}");
            }
        }

        /// <summary>
        /// 진행도 설정 (SaveDataAdapter에서 사용)
        /// </summary>
        public void SetProgressData(StageProgressData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[StageProgressManager] Null progress data");
                return;
            }

            progressData = data;
            OnProgressUpdated?.Invoke(progressData);
        }

        #endregion

        #region Auto Save

        private void Update()
        {
            if (!autoSaveEnabled) return;
            
            if (Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                SaveProgress();
                lastAutoSaveTime = Time.time;
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            progressData = new StageProgressData();
            UnlockInitialStages();
            SaveProgress();
            
            OnProgressUpdated?.Invoke(progressData);
            Debug.Log("[StageProgressManager] Progress reset");
        }

        [ContextMenu("Unlock All Stages")]
        public void UnlockAllStages()
        {
            foreach (var stage in stageDatabase)
            {
                UnlockStage(stage.StageId, false);
            }
            SaveProgress();
        }

        [ContextMenu("Print Progress Stats")]
        public void PrintProgressStats()
        {
            var stats = progressData.globalStats;
            Debug.Log($"=== Progress Statistics ===");
            Debug.Log($"Total Stages: {stageDatabase.Count}");
            Debug.Log($"Unlocked: {stats.totalStagesUnlocked}");
            Debug.Log($"Cleared: {stats.totalStagesCleared}");
            Debug.Log($"Total Stars: {stats.totalStarCount}");
            Debug.Log($"Total Score: {stats.totalScore:N0}");
            Debug.Log($"Overall Progress: {GetOverallProgress():P1}");
        }

        #endregion
    }
}
