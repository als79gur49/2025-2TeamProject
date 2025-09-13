using UnityEngine;

/// <summary>
/// 게임 데이터 관리자
/// 싱글톤 패턴으로 구현된 데이터 매니저
/// </summary>
public class DataManager : MonoBehaviour
{
    private static DataManager instance;
    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DataManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DataManager");
                    instance = go.AddComponent<DataManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("플레이어 데이터")]
    [SerializeField] private PlayerData playerData;
    public PlayerData PlayerData => playerData;

    [Header("게임 설정")]
    [SerializeField] private int totalStageCount = 10;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeData();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 데이터 초기화
    /// </summary>
    private void InitializeData()
    {
        playerData = new PlayerData(totalStageCount);
        Debug.Log($"DataManager 초기화 완료 - 총 스테이지 수: {totalStageCount}");
    }

    /// <summary>
    /// 스테이지 정보 업데이트
    /// </summary>
    public void UpdateStageInfo(int stageLevel, StageClearState clearState, int stars = 0)
    {
        if (stageLevel < 0 || stageLevel >= playerData.stageInfos.Length) return;

        var stageInfo = playerData.stageInfos[stageLevel];
        stageInfo.stageClearState = clearState;
        stageInfo.achievedStars = Mathf.Max(stageInfo.achievedStars, stars);
        playerData.stageInfos[stageLevel] = stageInfo;

        // 다음 스테이지 잠금 해제
        if (clearState == StageClearState.Cleard && stageLevel + 1 < playerData.stageInfos.Length)
        {
            var nextStage = playerData.stageInfos[stageLevel + 1];
            if (nextStage.stageClearState == StageClearState.NotCleard)
            {
                nextStage.stageClearState = StageClearState.InProgress;
                playerData.stageInfos[stageLevel + 1] = nextStage;
            }
        }

        SaveData();
    }

    /// <summary>
    /// 데이터 저장
    /// </summary>
    public void SaveData()
    {
        string json = JsonUtility.ToJson(playerData);
        PlayerPrefs.SetString("PlayerData", json);
        PlayerPrefs.Save();
        Debug.Log("데이터 저장 완료");
    }

    /// <summary>
    /// 데이터 로드
    /// </summary>
    public void LoadData()
    {
        if (PlayerPrefs.HasKey("PlayerData"))
        {
            string json = PlayerPrefs.GetString("PlayerData");
            playerData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("데이터 로드 완료");
        }
        else
        {
            Debug.Log("저장된 데이터가 없습니다. 기본값 사용");
        }
    }
}