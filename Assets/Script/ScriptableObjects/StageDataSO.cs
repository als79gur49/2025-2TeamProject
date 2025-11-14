using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.SaveSystem;

namespace Game.Data
{
    /// <summary>
    /// 스테이지 타입 분류
    /// </summary>
    public enum StageType
    {
        Tutorial,    // 튜토리얼
        Normal,      // 일반
        Boss,        // 보스
        Bonus,       // 보너스
        Challenge,   // 챌린지
        Secret      // 숨겨진 스테이지
    }

    /// <summary>
    /// 스테이지 데이터 ScriptableObject
    /// 유니크 ID 기반 관리
    /// </summary>
    [CreateAssetMenu(fileName = "StageData_", menuName = "Game/Stage Data")]
    public class StageDataSO : ScriptableObject
    {
        [Header("Stage Identification")]
        [SerializeField]
        [Tooltip("유니크 스테이지 ID (예: chapter1_stage3)")]
        private string stageId = "";
        
        [SerializeField]
        private string chapterId = "chapter1";
        
        [SerializeField]
        [Min(1)]
        private int stageNumber = 1;
        
        [Header("Stage Information")]
        [SerializeField]
        private string displayName = "New Stage";
        
        [SerializeField]
        [TextArea(3, 5)]
        private string description = "";

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private Sprite thumbnail;

        [SerializeField]
        [Tooltip("씬 데이터 ScriptableObject")]
        private Game.SceneManagement.SceneData sceneData;

        [Header("Enemy Configuration")]
        [SerializeField]
        [Tooltip("적군 카드 풀 ScriptableObject")]
        private EnemyCardPoolSO enemyCardPool;

        [Header("Stage Properties")]
        [SerializeField]
        private StageType stageType = StageType.Normal;
        
        [SerializeField]
        [Range(1, 10)]
        private int difficulty = 1;
        
        [SerializeField]
        private bool isHidden = false;  // 숨겨진 스테이지 여부
        
        [Header("Unlock Conditions")]
        [SerializeField]
        [Tooltip("모든 조건을 만족해야 해금")]
        private List<UnlockCondition> unlockConditions = new List<UnlockCondition>();

        [SerializeField]
        [Tooltip("추가 보너스 해금 조건 (선택적)")]
        private List<UnlockCondition> bonusUnlockConditions = new List<UnlockCondition>();
        
        [Header("Score System")]
        [SerializeField]
        private ScoringSettings scoringSettings = new ScoringSettings();
        
        [Header("Rewards")]
        [SerializeField]
        private StageRewards rewards = new StageRewards();
        
        [Header("Stage Metadata")]
        [SerializeField]
        private List<string> tags = new List<string>();
        
        [SerializeField]
        private Color themeColor = Color.white;

        // Properties
        public string StageId => string.IsNullOrEmpty(stageId) ? name : stageId;
        public string ChapterId => chapterId;
        public int StageNumber => stageNumber;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Sprite Thumbnail => thumbnail;
        public string SceneToLoad => sceneData != null ? sceneData.SceneName : "";
        public Game.SceneManagement.SceneData SceneData => sceneData;
        public EnemyCardPoolSO EnemyCardPool => enemyCardPool;
        public StageType Type => stageType;
        public int Difficulty => difficulty;
        public bool IsHidden => isHidden;
        public ScoringSettings Scoring => scoringSettings;
        public StageRewards Rewards => rewards;
        public Color ThemeColor => themeColor;

        /// <summary>
        /// 스테이지 해금 여부 확인
        /// </summary>
        public bool IsUnlocked(StageProgressData progressData)
        {
            // 튜토리얼은 항상 해금
            if (stageType == StageType.Tutorial)
                return true;

            // 조건이 없으면 기본 해금
            if (unlockConditions == null || unlockConditions.Count == 0)
                return true;

            // 모든 조건 체크
            foreach (var condition in unlockConditions.Where(c => c != null))
            {
                if (!condition.Evaluate(progressData))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 보너스 조건 충족 여부
        /// </summary>
        public bool IsBonusUnlocked(StageProgressData progressData)
        {
            if (bonusUnlockConditions == null || bonusUnlockConditions.Count == 0)
                return true;

            foreach (var condition in bonusUnlockConditions.Where(c => c != null))
            {
                if (!condition.Evaluate(progressData))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 스테이지 정보 조회
        /// </summary>
        public StageInfo GetStageInfo(StageProgressData progressData)
        {
            var record = progressData?.GetRecord(StageId);
            
            return new StageInfo
            {
                stageId = StageId,
                state = record?.state ?? StageState.Locked,
                bestScore = record?.bestScore ?? 0,
                bestStars = record?.bestStars ?? 0,
                clearCount = record?.ClearCount ?? 0,
                totalPlayTime = record?.TotalPlayTime ?? 0f,
                isUnlocked = IsUnlocked(progressData),
                isBonusUnlocked = IsBonusUnlocked(progressData)
            };
        }

        /// <summary>
        /// 점수에 따른 별 개수 계산
        /// </summary>
        public int CalculateStars(int score)
        {
            return scoringSettings.CalculateStars(score);
        }

        /// <summary>
        /// 등급 계산
        /// </summary>
        public string CalculateRank(int score)
        {
            return scoringSettings.CalculateRank(score);
        }

        /// <summary>
        /// 보상 계산
        /// </summary>
        public StageRewardResult CalculateRewards(int score, int stars, bool isFirstClear)
        {
            return rewards.Calculate(score, stars, isFirstClear);
        }

        /// <summary>
        /// 해금 조건 설명 문자열 리스트 반환
        /// </summary>
        public List<string> GetUnlockRequirements()
        {
            var requirements = new List<string>();

            if (unlockConditions == null || unlockConditions.Count == 0)
            {
                requirements.Add("No unlock conditions");
                return requirements;
            }

            foreach (var condition in unlockConditions.Where(c => c != null))
            {
                requirements.Add(condition.GetDescription());
            }

            return requirements;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 자동 ID 생성
            if (string.IsNullOrEmpty(stageId))
            {
                stageId = $"{chapterId}_stage{stageNumber}";
            }

            // 디스플레이 이름 자동 설정
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = $"Stage {stageNumber}";
            }

            // 점수 설정 검증
            scoringSettings?.Validate();
        }
#endif
    }

    /// <summary>
    /// 시간 보너스 등급
    /// </summary>
    [System.Serializable]
    public class TimeBonusTier
    {
        [Tooltip("클리어 시간 임계값 (초) - 이 시간 이하로 클리어 시 보너스 획득")]
        public float timeInSeconds;

        [Tooltip("부여되는 보너스 점수")]
        public int bonusScore;
    }

    /// <summary>
    /// 체력 보너스 등급
    /// </summary>
    [System.Serializable]
    public class HealthBonusTier
    {
        [Tooltip("남은 체력 퍼센트 임계값 (0~100) - 이 퍼센트 이상 남았을 때 보너스 획득")]
        public float healthPercent;

        [Tooltip("부여되는 보너스 점수")]
        public int bonusScore;
    }

    /// <summary>
    /// 점수 설정
    /// </summary>
    [System.Serializable]
    public class ScoringSettings
    {
        [Header("Score Thresholds")]
        public int maxScore = 10000;
        public int[] starThresholds = new int[] { 3000, 6000, 9000 };  // 1성, 2성, 3성

        [Header("Rank Thresholds")]
        public bool useRankSystem = true;
        public int sRankScore = 9500;   // S랭크
        public int aRankScore = 8000;   // A랭크
        public int bRankScore = 6000;   // B랭크
        public int cRankScore = 4000;   // C랭크

        [Header("Time Bonus Tiers")]
        [Tooltip("시간 보너스 등급 (3등급) - 빠른 클리어 시간순으로 정렬됨")]
        public TimeBonusTier[] timeBonusTiers = new TimeBonusTier[]
        {
            new TimeBonusTier { timeInSeconds = 60f, bonusScore = 1000 },   // 1분 이하: 1000점
            new TimeBonusTier { timeInSeconds = 120f, bonusScore = 500 },   // 2분 이하: 500점
            new TimeBonusTier { timeInSeconds = 180f, bonusScore = 200 }    // 3분 이하: 200점
        };

        [Header("Health Bonus Tiers")]
        [Tooltip("체력 보너스 등급 (3등급) - 높은 체력 퍼센트순으로 정렬됨")]
        public HealthBonusTier[] healthBonusTiers = new HealthBonusTier[]
        {
            new HealthBonusTier { healthPercent = 100f, bonusScore = 2000 }, // 100% 체력: 2000점
            new HealthBonusTier { healthPercent = 70f, bonusScore = 1000 },  // 70% 이상: 1000점
            new HealthBonusTier { healthPercent = 40f, bonusScore = 500 }    // 40% 이상: 500점
        };

        [Header("Score Multipliers")]
        public float comboMultiplier = 1.2f; // 콤보 배율

        public int CalculateStars(int score)
        {
            for (int i = starThresholds.Length - 1; i >= 0; i--)
            {
                if (score >= starThresholds[i])
                    return i + 1;
            }
            return 0;
        }

        public string CalculateRank(int score)
        {
            if (!useRankSystem) return "";

            if (score >= sRankScore) return "S";
            if (score >= aRankScore) return "A";
            if (score >= bRankScore) return "B";
            if (score >= cRankScore) return "C";
            return "D";
        }

        public void Validate()
        {
            // 별 임계값 정렬
            System.Array.Sort(starThresholds);

            // 최대 점수 검증
            if (maxScore < starThresholds[starThresholds.Length - 1])
                maxScore = starThresholds[starThresholds.Length - 1] + 100;

            // 시간 보너스 등급 정렬 (시간 오름차순)
            if (timeBonusTiers != null && timeBonusTiers.Length > 0)
            {
                System.Array.Sort(timeBonusTiers, (a, b) => a.timeInSeconds.CompareTo(b.timeInSeconds));
            }

            // 체력 보너스 등급 정렬 (체력 내림차순)
            if (healthBonusTiers != null && healthBonusTiers.Length > 0)
            {
                System.Array.Sort(healthBonusTiers, (a, b) => b.healthPercent.CompareTo(a.healthPercent));
            }
        }
    }

    /// <summary>
    /// 스테이지 보상 설정
    /// </summary>
    [System.Serializable]
    public class StageRewards
    {
        [Header("Clear Rewards")]
        public int baseCoin = 100;
        public int baseExp = 50;
        public List<string> clearItems = new List<string>();

        [Header("Star Rewards")]
        public int[] starBonusCoins = new int[] { 50, 100, 200 };  // 1,2,3성 추가 보상
        
        [Header("First Clear Bonus")]
        public int firstClearBonus = 500;
        public List<string> firstClearItems = new List<string>();

        public StageRewardResult Calculate(int score, int stars, bool isFirstClear)
        {
            var result = new StageRewardResult();
            
            result.coins = baseCoin;
            result.exp = baseExp;
            
            // 별 보너스
            if (stars > 0 && stars <= starBonusCoins.Length)
            {
                result.coins += starBonusCoins[stars - 1];
            }
            
            // 첫 클리어 보너스
            if (isFirstClear)
            {
                result.coins += firstClearBonus;
                result.items.AddRange(firstClearItems);
            }
            
            result.items.AddRange(clearItems);
            
            return result;
        }
    }

    /// <summary>
    /// 보상 결과
    /// </summary>
    public class StageRewardResult
    {
        public int coins;
        public int exp;
        public List<string> items = new List<string>();
    }

    /// <summary>
    /// 스테이지 정보 (런타임용)
    /// </summary>
    public struct StageInfo
    {
        public string stageId;
        public StageState state;
        public int bestScore;
        public int bestStars;
        public int clearCount;
        public float totalPlayTime;
        public bool isUnlocked;
        public bool isBonusUnlocked;
    }
}
