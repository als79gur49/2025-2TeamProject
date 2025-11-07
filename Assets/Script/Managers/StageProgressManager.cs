using UnityEngine;
using System.Collections.Generic;
using Game.Data;
using Game.SaveSystem;

namespace Game.Managers
{
    /// <summary>
    /// StageProgressData의 런타임 메모리 관리자
    /// SaveDataAdapter는 이 매니저에서 데이터를 수집/적용
    /// </summary>
    public class StageProgressManager : MonoBehaviour
    {
        #region Current State
        private StageProgressData currentProgress;
        #endregion

        #region Initialization

        /// <summary>
        /// 초기화 - 기본 스테이지 진행도 생성
        /// </summary>
        public void Initialize()
        {
            currentProgress = new StageProgressData
            {
                currentChapter = 1,
                currentStage = 1,
                stageRecords = new List<StageRecord>
                {
                    new StageRecord
                    {
                        chapter = 1,
                        stage = 1,
                        isUnlocked = true,
                        isCleared = false
                    }
                }
            };

            Debug.Log("[StageProgressManager] Initialized with Chapter 1-1");
        }

        #endregion

        #region Data Access

        /// <summary>
        /// 현재 스테이지 진행도 전체 반환 (SaveDataAdapter에서 사용)
        /// </summary>
        public StageProgressData GetCurrentProgress()
        {
            return currentProgress;
        }

        /// <summary>
        /// 스테이지 진행도 전체 설정 (SaveDataAdapter에서 로드 시 사용)
        /// </summary>
        public void SetProgress(StageProgressData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[StageProgressManager] SetProgress called with null data");
                return;
            }

            currentProgress = data;
            Debug.Log($"[StageProgressManager] Progress set - Current: {data.currentChapter}-{data.currentStage}");
        }

        #endregion

        #region Current Stage Access

        /// <summary>
        /// 현재 챕터 조회
        /// </summary>
        public int GetCurrentChapter()
        {
            return currentProgress?.currentChapter ?? 1;
        }

        /// <summary>
        /// 현재 스테이지 조회
        /// </summary>
        public int GetCurrentStage()
        {
            return currentProgress?.currentStage ?? 1;
        }

        /// <summary>
        /// 현재 스테이지 설정
        /// </summary>
        public void SetCurrentStage(int chapter, int stage)
        {
            if (currentProgress == null)
            {
                Debug.LogWarning("[StageProgressManager] Cannot set current stage - data not initialized");
                return;
            }

            currentProgress.currentChapter = chapter;
            currentProgress.currentStage = stage;
            Debug.Log($"[StageProgressManager] Current stage changed to: {chapter}-{stage}");
        }

        #endregion

        #region Stage Records Management

        /// <summary>
        /// 특정 스테이지 레코드 조회
        /// </summary>
        public StageRecord GetStageRecord(int chapter, int stage)
        {
            if (currentProgress?.stageRecords == null)
            {
                return null;
            }

            return currentProgress.stageRecords.Find(r => r.chapter == chapter && r.stage == stage);
        }

        /// <summary>
        /// 스테이지 잠금 해제
        /// </summary>
        public void UnlockStage(int chapter, int stage)
        {
            if (currentProgress?.stageRecords == null)
            {
                Debug.LogWarning("[StageProgressManager] Cannot unlock stage - data not initialized");
                return;
            }

            var record = GetStageRecord(chapter, stage);
            if (record != null)
            {
                record.isUnlocked = true;
                Debug.Log($"[StageProgressManager] Stage {chapter}-{stage} unlocked");
            }
            else
            {
                // 레코드가 없으면 새로 생성
                currentProgress.stageRecords.Add(new StageRecord
                {
                    chapter = chapter,
                    stage = stage,
                    isUnlocked = true,
                    isCleared = false
                });
                Debug.Log($"[StageProgressManager] New stage record created and unlocked: {chapter}-{stage}");
            }
        }

        /// <summary>
        /// 스테이지 클리어 처리
        /// </summary>
        public void ClearStage(int chapter, int stage, int stars)
        {
            if (currentProgress?.stageRecords == null)
            {
                Debug.LogWarning("[StageProgressManager] Cannot clear stage - data not initialized");
                return;
            }

            var record = GetStageRecord(chapter, stage);
            if (record != null)
            {
                record.isCleared = true;
                if (stars > record.bestStars)
                {
                    record.bestStars = stars;
                }
                Debug.Log($"[StageProgressManager] Stage {chapter}-{stage} cleared with {stars} stars");
            }
            else
            {
                // 레코드가 없으면 새로 생성
                currentProgress.stageRecords.Add(new StageRecord
                {
                    chapter = chapter,
                    stage = stage,
                    isUnlocked = true,
                    isCleared = true,
                    bestStars = stars
                });
                Debug.Log($"[StageProgressManager] New stage record created and cleared: {chapter}-{stage} ({stars} stars)");
            }
        }

        /// <summary>
        /// 스테이지 잠금 상태 확인
        /// </summary>
        public bool IsStageUnlocked(int chapter, int stage)
        {
            var record = GetStageRecord(chapter, stage);
            return record != null && record.isUnlocked;
        }

        /// <summary>
        /// 스테이지 클리어 상태 확인
        /// </summary>
        public bool IsStageCleared(int chapter, int stage)
        {
            var record = GetStageRecord(chapter, stage);
            return record != null && record.isCleared;
        }

        /// <summary>
        /// 스테이지 별 개수 조회
        /// </summary>
        public int GetStageStars(int chapter, int stage)
        {
            var record = GetStageRecord(chapter, stage);
            return record?.bestStars ?? 0;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// 총 클리어한 스테이지 수
        /// </summary>
        public int GetTotalClearedStages()
        {
            if (currentProgress?.stageRecords == null)
            {
                return 0;
            }

            int count = 0;
            foreach (var record in currentProgress.stageRecords)
            {
                if (record.isCleared)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 총 획득한 별 개수
        /// </summary>
        public int GetTotalStars()
        {
            if (currentProgress?.stageRecords == null)
            {
                return 0;
            }

            int total = 0;
            foreach (var record in currentProgress.stageRecords)
            {
                total += record.bestStars;
            }
            return total;
        }

        #endregion
    }
}
