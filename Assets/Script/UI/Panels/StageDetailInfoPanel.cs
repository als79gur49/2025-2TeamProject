using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Data;

namespace Game.UI.Panels
{
    /// <summary>
    /// 스테이지의 점수 임계값, 턴/체력 보너스, 보상 정보를 표시하는 패널
    /// StageButton에서 전달받은 StageDataSO를 기반으로 UI를 갱신합니다.
    /// </summary>
    public class StageDetailInfoPanel : UIPanel, IDualClosePanel
    {
        [Header("Event Channels")]
        [SerializeField]
        private StageInfoEventChannelSO stageInfoEventChannel;

        [Header("Panel GameObjects")]
        [SerializeField]
        private GameObject infoPanel;

        [SerializeField]
        private GameObject dim;

        [Header("Basic Info")]
        [SerializeField]
        private TextMeshProUGUI stageNameText;

        [SerializeField]
        private TextMeshProUGUI difficultyText;

        [Header("Scoring Info")]
        [SerializeField]
        private TextMeshProUGUI starThresholdsText;

        [SerializeField]
        private TextMeshProUGUI turnBonusText;

        [SerializeField]
        private TextMeshProUGUI healthBonusText;

        [Header("Rewards Info")]
        [SerializeField]
        private TextMeshProUGUI rewardsText;

        [Header("Close Buttons")]
        [SerializeField]
        private Button primaryCloseButton;

        [SerializeField]
        private Button secondaryCloseButton;

        public Button PrimaryCloseButton => primaryCloseButton;
        public Button SecondaryCloseButton => secondaryCloseButton;

        /// <summary>
        /// 의존성 없는 초기화 (Awake에서 호출됨)
        /// UI 컴포넌트 검증 및 초기 상태 설정
        /// </summary>
        protected override void OnInitializeSelf()
        {
            base.OnInitializeSelf();
            ValidateReferences();

            // 초기 상태: 모든 자식 패널 비활성화
            if (infoPanel != null) infoPanel.SetActive(false);
            if (dim != null) dim.SetActive(false);
            Debug.Log("[StageDetailInfoPanel] Self-initialized successfully");
        }

        /// <summary>
        /// 필수 참조 검증
        /// </summary>
        private void ValidateReferences()
        {
            if (stageInfoEventChannel == null)
                Debug.LogWarning("[StageDetailInfoPanel] StageInfoEventChannel is not assigned!");

            if (infoPanel == null)
                Debug.LogWarning("[StageDetailInfoPanel] InfoPanel is not assigned!");
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (stageInfoEventChannel != null)
            {
                stageInfoEventChannel.Subscribe(ShowStageDetails);
                Debug.Log("[StageDetailInfoPanel] Subscribed to stage info events");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (stageInfoEventChannel != null)
            {
                stageInfoEventChannel.Unsubscribe(ShowStageDetails);
                Debug.Log("[StageDetailInfoPanel] Unsubscribed from stage info events");
            }
        }

        /// <summary>
        /// 패널 표시 - 자식 패널들만 활성화 (StageDetailInfoPanel 자체는 항상 활성화 상태 유지)
        /// </summary>
        public override void OnShow()
        {
            // base.OnShow()를 호출하지 않음 (gameObject.SetActive 방지)
            if (currentState == UIPanelState.Active) return;

            currentState = UIPanelState.Showing;

            if (infoPanel != null) infoPanel.SetActive(true);
            if (dim != null) dim.SetActive(true);

            OnShowPanel();
            currentState = UIPanelState.Active;
            RaiseOnPanelShown();

            Debug.Log("[StageDetailInfoPanel] Child panels shown");
        }

        /// <summary>
        /// 패널 숨김 - 자식 패널들만 비활성화 (StageDetailInfoPanel 자체는 활성화 상태 유지)
        /// </summary>
        public override void OnHide()
        {
            // base.OnHide()를 호출하지 않음 (gameObject.SetActive 방지)
            if (currentState == UIPanelState.Inactive) return;

            currentState = UIPanelState.Hiding;
            OnHidePanel();

            if (infoPanel != null) infoPanel.SetActive(false);
            if (dim != null) dim.SetActive(false);

            currentState = UIPanelState.Inactive;
            RaiseOnPanelHidden();

            Debug.Log("[StageDetailInfoPanel] Child panels hidden");
        }

        /// <summary>
        /// 선택된 스테이지의 상세 정보를 표시합니다.
        /// StageButton의 UnityEvent(StageDataSO)를 통해 호출됩니다.
        /// </summary>
        public void ShowStageDetails(StageDataSO stageData)
        {
            if (stageData == null)
            {
                Debug.LogWarning("[StageDetailInfoPanel] StageData is null");
                return;
            }

            UpdateBasicInfo(stageData);
            UpdateStarThresholds(stageData.Scoring);
            UpdateTurnBonus(stageData.Scoring);
            UpdateHealthBonus(stageData.Scoring);
            UpdateRewards(stageData.Rewards);

            OnShow();

            Debug.Log($"[StageDetailInfoPanel] Showing details for stage: {stageData.StageId}");
        }

        private void UpdateBasicInfo(StageDataSO stageData)
        {
            if (stageNameText != null)
            {
                stageNameText.text = stageData.DisplayName;
            }

            if (difficultyText != null)
            {
                difficultyText.text = $"Lv.{stageData.Difficulty}";
            }
        }

        private void UpdateStarThresholds(ScoringSettings scoring)
        {
            if (starThresholdsText == null)
                return;

            if (scoring == null || scoring.starThresholds == null || scoring.starThresholds.Length == 0)
            {
                starThresholdsText.text = "Star Thresholds: N/A";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Max Score: {scoring.maxScore:N0}");

            for (int i = 0; i < scoring.starThresholds.Length; i++)
            {
                int threshold = scoring.starThresholds[i];
                int starIndex = i + 1;
                sb.AppendLine($"별 {starIndex}: {threshold:N0} 점 이상");
            }

            starThresholdsText.text = sb.ToString();
        }

        private void UpdateTurnBonus(ScoringSettings scoring)
        {
            if (turnBonusText == null)
                return;

            if (scoring == null || scoring.turnBonusTiers == null || scoring.turnBonusTiers.Length == 0)
            {
                turnBonusText.text = "Turn Bonus: N/A";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Turn Bonus:");

            foreach (var tier in scoring.turnBonusTiers)
            {
                if (tier == null) continue;
                sb.AppendLine($"- {tier.turnCount}턴 이하: +{tier.bonusScore:N0} 점");
            }

            turnBonusText.text = sb.ToString();
        }

        private void UpdateHealthBonus(ScoringSettings scoring)
        {
            if (healthBonusText == null)
                return;

            if (scoring == null || scoring.healthBonusTiers == null || scoring.healthBonusTiers.Length == 0)
            {
                healthBonusText.text = "Health Bonus: N/A";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Health Bonus:");

            foreach (var tier in scoring.healthBonusTiers)
            {
                if (tier == null) continue;
                sb.AppendLine($"- HP ≥ {tier.healthPercent}%: +{tier.bonusScore:N0} 점");
            }

            healthBonusText.text = sb.ToString();
        }

        private void UpdateRewards(StageRewards rewards)
        {
            if (rewardsText == null)
                return;

            if (rewards == null)
            {
                rewardsText.text = "Rewards: N/A";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Rewards:");
            sb.AppendLine($"- Base Coins: {rewards.baseCoin:N0}");

            if (rewards.starBonusCoins != null && rewards.starBonusCoins.Length > 0)
            {
                for (int i = 0; i < rewards.starBonusCoins.Length; i++)
                {
                    int starIndex = i + 1;
                    int bonus = rewards.starBonusCoins[i];
                    sb.AppendLine($"- ★{starIndex} Bonus: +{bonus:N0}");
                }
            }
            else
            {
                sb.AppendLine("- Star Bonus: N/A");
            }

            sb.AppendLine($"- First Clear Bonus: +{rewards.firstClearBonus:N0}");

            rewardsText.text = sb.ToString();
        }

        /// <summary>
        /// 스테이지 정보 패널을 숨깁니다.
        /// (이벤트/버튼/외부 호출용 공통 진입점)
        /// </summary>
        private void Hide()
        {
            OnHide();
            Debug.Log("[StageDetailInfoPanel] Hidden");
        }

        /// <summary>
        /// 수동으로 패널을 숨깁니다 (외부 호출용)
        /// </summary>
        public void HidePanel()
        {
            Hide();
        }
    }
}
