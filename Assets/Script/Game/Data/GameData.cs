using UnityEngine;

/// <summary>
/// 스테이지 정보 구조체
/// </summary>
[System.Serializable]
public struct StageInfo
{
    public string stageName;
    public string sceneName;
    public StageClearState stageClearState;
    public int achievedStars;
    
    public StageInfo(string stageName, string sceneName, StageClearState stageClearState = StageClearState.NotCleard, int achievedStars = 0)
    {
        this.stageName = stageName;
        this.sceneName = sceneName;
        this.stageClearState = stageClearState;
        this.achievedStars = Mathf.Clamp(achievedStars, 0, 3);
    }
}

/// <summary>
/// 스테이지 클리어 상태 열거형
/// </summary>
public enum StageClearState
{
    NotCleard,    // 잠김
    InProgress,   // 플레이 가능
    Cleard        // 클리어됨
}

/// <summary>
/// 플레이어 데이터 구조체
/// </summary>
[System.Serializable]
public struct PlayerData
{
    public int currentStageLevel;
    public int totalScore;
    public int totalPlayTime;
    public StageInfo[] stageInfos;
    
    public PlayerData(int stageCount)
    {
        currentStageLevel = 0;
        totalScore = 0;
        totalPlayTime = 0;
        stageInfos = new StageInfo[stageCount];
        
        for (int i = 0; i < stageCount; i++)
        {
            stageInfos[i] = new StageInfo($"Stage {i + 1}", $"Stage{i + 1}", 
                i == 0 ? StageClearState.InProgress : StageClearState.NotCleard, 0);
        }
    }
}