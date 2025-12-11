using System;
using System.Collections.Generic;
using Game.Data;
using Game.SaveSystem;

namespace Game.Managers
{
    /// <summary>
    /// 스테이지 진행도 관리 인터페이스
    ///
    /// 책임:
    /// - 스테이지 해금 및 완료 관리
    /// - 진행도 데이터 저장/로드
    /// - 스테이지 통계 조회
    /// </summary>
    public interface IStageProgressManager
    {
        #region Events
        /// <summary>
        /// 스테이지 해금 이벤트
        /// </summary>
        event Action<string> OnStageUnlocked;

        /// <summary>
        /// 스테이지 완료 이벤트 (stageId, score, stars)
        /// </summary>
        event Action<string, int, int> OnStageCompleted;

        /// <summary>
        /// 스테이지 상태 변경 이벤트
        /// </summary>
        event Action<string, StageState> OnStageStateChanged;

        /// <summary>
        /// 진행도 업데이트 이벤트
        /// </summary>
        event Action<StageProgressData> OnProgressUpdated;
        #endregion

        #region Core Methods
        /// <summary>
        /// 초기화
        /// </summary>
        void Initialize();

        /// <summary>
        /// 스테이지 플레이 준비 (씬 로드 전 호출)
        /// </summary>
        /// <param name="stageId">준비할 스테이지 ID</param>
        void PrepareStageForPlay(string stageId);

        /// <summary>
        /// 현재 준비된 스테이지 ID 가져오기
        /// </summary>
        /// <returns>현재 스테이지 ID</returns>
        string GetCurrentStageId();

        /// <summary>
        /// 현재 준비된 스테이지 ID 초기화
        /// (예: 타이틀 씬 진입 시 세션 컨텍스트를 비울 때 사용)
        /// </summary>
        void ClearCurrentStage();

        /// <summary>
        /// 스테이지 완료 기록
        /// </summary>
        /// <param name="stageId">완료한 스테이지 ID</param>
        /// <param name="score">최종 점수</param>
        /// <param name="statistics">게임 통계</param>
        void RecordStageCompletion(string stageId, int score, Dictionary<string, int> statistics = null);

        /// <summary>
        /// 스테이지 해금
        /// </summary>
        /// <param name="stageId">해금할 스테이지 ID</param>
        /// <param name="save">저장 여부</param>
        void UnlockStage(string stageId, bool save = true);
        #endregion

        #region Query Methods
        /// <summary>
        /// 스테이지 데이터 가져오기
        /// </summary>
        StageDataSO GetStageData(string stageId);

        /// <summary>
        /// 스테이지 해금 여부
        /// </summary>
        bool IsStageUnlocked(string stageId);

        /// <summary>
        /// 스테이지 클리어 여부
        /// </summary>
        bool IsStageCleared(string stageId);

        /// <summary>
        /// 스테이지 상태 가져오기
        /// </summary>
        StageState GetStageState(string stageId);

        /// <summary>
        /// 스테이지 레코드 가져오기
        /// </summary>
        StageRecord GetStageRecord(string stageId);

        /// <summary>
        /// 챕터의 모든 스테이지 가져오기
        /// </summary>
        List<StageDataSO> GetStagesInChapter(string chapterId);

        /// <summary>
        /// 해금된 스테이지 목록
        /// </summary>
        List<StageDataSO> GetUnlockedStages();

        /// <summary>
        /// 진행도 데이터 전체 가져오기
        /// </summary>
        StageProgressData GetProgressData();

        /// <summary>
        /// 전체 진행률
        /// </summary>
        float GetOverallProgress();

        /// <summary>
        /// 챕터 진행률
        /// </summary>
        float GetChapterProgress(string chapterId);

        /// <summary>
        /// 다음 스테이지 데이터 가져오기 (순차 진행용)
        /// </summary>
        /// <param name="currentStageId">현재 스테이지 ID</param>
        /// <returns>다음 스테이지 데이터 (없으면 null)</returns>
        StageDataSO GetNextStageData(string currentStageId);
        #endregion

        #region Save/Load
        /// <summary>
        /// 진행도 저장
        /// </summary>
        void SaveProgress();

        /// <summary>
        /// 진행도 불러오기
        /// </summary>
        void LoadProgress();

        /// <summary>
        /// 진행도 설정 (SaveDataAdapter에서 사용)
        /// </summary>
        void SetProgressData(StageProgressData data);
        #endregion
    }
}
