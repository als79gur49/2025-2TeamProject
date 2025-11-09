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
    public class StageProgressManager : MonoBehaviour, IStageProgressManager
    {
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
        #endregion

        #region Events
        public event Action<string> OnStageUnlocked;
        public event Action<string, int, int> OnStageCompleted;
        public event Action<string, StageState> OnStageStateChanged;
        public event Action<StageProgressData> OnProgressUpdated;
        #endregion

        #region Initialization

        public void Initialize()
        {
            // 새로운 진행도 데이터 생성
            progressData = new StageProgressData();

            // 스테이지 룩업 테이블 생성
            BuildStageLookup();

            // SaveDataAdapter 연결0
            ConnectSaveAdapter();

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

        public void UnlockInitialStages()
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
        /// 스테이지 플레이 준비 (씬 로드 전 호출)
        /// </summary>
        /// <param name="stageId">준비할 스테이지 ID</param>
        public void PrepareStageForPlay(string stageId)
        {
            if (!IsStageUnlocked(stageId))
            {
                Debug.LogError($"[StageProgressManager] Cannot prepare locked stage: {stageId}");
                return;
            }

            progressData.currentStageId = stageId;
            Debug.Log($"[StageProgressManager] Stage prepared for play: {stageId}");
        }

        /// <summary>
        /// 현재 준비된 스테이지 ID 가져오기
        /// </summary>
        /// <returns>현재 스테이지 ID (없으면 빈 문자열)</returns>
        public string GetCurrentStageId()
        {
            return progressData?.currentStageId ?? "";
        }

        /// <summary>
        /// 스테이지 완료 기록 (외부에서 최종 데이터 받음)
        /// </summary>
        /// <param name="stageId">완료한 스테이지 ID</param>
        /// <param name="score">최종 점수</param>
        /// <param name="statistics">게임 통계 (적 처치 수, 데미지 등)</param>
        public void RecordStageCompletion(string stageId, int score, Dictionary<string, int> statistics = null)
        {
            // 통계에서 플레이 시간 가져오기 (초 단위)
            float playTime = 0f;
            if (statistics != null && statistics.ContainsKey("play_time"))
            {
                playTime = statistics["play_time"];
            }

            // PlaySession 생성 (매개변수에서 데이터 받아 생성)
            var session = new PlaySession
            {
                startTime = DateTime.Now.AddSeconds(-playTime),
                endTime = DateTime.Now,
                score = score,
                statistics = statistics ?? new Dictionary<string, int>(),
                completed = true,
                playTime = playTime
            };

            // 스테이지 데이터 가져오기
            var stageData = GetStageData(stageId);
            if (stageData == null)
            {
                Debug.LogError($"[StageProgressManager] Stage data not found: {stageId}");
                return;
            }

            // 별 계산
            int stars = stageData.CalculateStars(score);
            session.stars = stars;

            // 레코드 업데이트
            var record = progressData.GetOrCreateRecord(stageId);
            bool isFirstClear = record.state != StageState.Cleared && record.state != StageState.Perfect;

            // 세션 추가
            record.playSessions.Add(session);

            // 최고 기록 업데이트
            if (score > record.bestScore)
            {
                record.bestScore = score;
                record.bestStars = stars;
            }

            // 상태 업데이트
            if (isFirstClear)
            {
                record.firstClearDate = DateTime.Now;
            }

            record.state = stars >= 3 ? StageState.Perfect : StageState.Cleared;
            record.lastPlayDate = DateTime.Now;

            // 통계 업데이트
            UpdateStatistics(record, session);
            
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

            Debug.Log($"[StageProgressManager] Stage {stageId} completion recorded: Score={score}, Stars={stars}");
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

                // SaveDataAdapter를 통한 저장
                saveAdapter.SaveSpecific(SaveFileType.StageProgress);

                Debug.Log("[StageProgressManager] Progress saved via SaveDataAdapter");
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
            if (saveAdapter == null)
            {
                Debug.LogWarning("[StageProgressManager] SaveAdapter not available");
                return;
            }

            try
            {
                // PlayerPrefs 레거시 데이터 마이그레이션 체크
                MigrateFromPlayerPrefsIfNeeded();

                // SaveDataAdapter를 통한 로드
                saveAdapter.LoadSpecific(SaveFileType.StageProgress);

                Debug.Log("[StageProgressManager] Progress loaded via SaveDataAdapter");

                // 로드 후 자동으로 해금 조건 재평가
                // 과거에 클리어한 스테이지가 있다면, 새로 추가된 스테이지도 조건 충족 시 해금
                CheckAndUnlockNextStages();

                if (debugMode)
                {
                    Debug.Log("[StageProgressManager] Unlock conditions re-evaluated after load");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[StageProgressManager] Load failed: {e.Message}");
            }
        }

        /// <summary>
        /// PlayerPrefs에서 SaveDataAdapter로 마이그레이션 (1회 실행)
        /// </summary>
        private void MigrateFromPlayerPrefsIfNeeded()
        {
            // PlayerPrefs에 레거시 데이터가 있는지 확인
            string legacyJson = PlayerPrefs.GetString("StageProgress", "");
            if (string.IsNullOrEmpty(legacyJson))
            {
                return; // 마이그레이션할 데이터 없음
            }

            try
            {
                Debug.Log("[StageProgressManager] Migrating legacy data from PlayerPrefs...");

                // 레거시 데이터 파싱
                var saveData = JsonUtility.FromJson<StageProgressSaveData>(legacyJson);
                if (saveData != null && saveData.progressData != null)
                {
                    // 현재 progressData에 적용
                    progressData = saveData.progressData;

                    // SaveDataAdapter를 통해 새로운 형식으로 저장
                    saveAdapter.SaveSpecific(SaveFileType.StageProgress);

                    // PlayerPrefs 레거시 데이터 삭제
                    PlayerPrefs.DeleteKey("StageProgress");
                    PlayerPrefs.Save();

                    Debug.Log("[StageProgressManager] Migration completed successfully");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StageProgressManager] Migration failed: {e.Message}");
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
