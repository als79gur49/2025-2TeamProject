using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Data;
using Game.Card.Effects;
using Game.UI;

namespace Game.UI.Panels
{
    /// <summary>
    /// 스테이지 선택 화면에서 적 유닛 소환 카드를 아이콘으로 표시하는 패널
    /// StageDataSO.EnemyCardPool.Cards를 UI 계층에서 해석하여
    /// 유닛 소환 카드만 필터링/정렬해 보여줍니다.
    /// </summary>
    public class StageEnemyPreviewPanel : UIPanel
    {
        [Header("Event Channels")]
        [SerializeField]
        private StageInfoEventChannelSO stageInfoEventChannel;

        [Header("UI Components")]
        [SerializeField]
        private Transform contentRoot;

        [SerializeField]
        private EnemyCardIconUI enemyCardIconPrefab;

        [SerializeField]
        private TextMeshProUGUI stageNameText;

        [Header("Sorting Options")]
        [SerializeField]
        private bool sortByRarity = true;

        [SerializeField]
        private bool sortByManaCost = true;

        [SerializeField]
        private bool removeDuplicateCards = true;

        // StageButton 단일 클릭 프리뷰 이벤트 구독용
        private readonly List<StageButton> subscribedStageButtons = new List<StageButton>();

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
                stageInfoEventChannel.Subscribe(ShowStagePreview);
                Debug.Log("[StageEnemyPreviewPanel] Subscribed to stage info events");
            }

            SubscribeToStageButtons();
        }

        private void UnsubscribeFromEvents()
        {
            if (stageInfoEventChannel != null)
            {
                stageInfoEventChannel.Unsubscribe(ShowStagePreview);
                Debug.Log("[StageEnemyPreviewPanel] Unsubscribed from stage info events");
            }

            UnsubscribeFromStageButtons();
        }

        /// <summary>
        /// 현재 씬의 모든 StageButton에 단일 클릭 프리뷰 리스너를 등록합니다.
        /// </summary>
        private void SubscribeToStageButtons()
        {
            subscribedStageButtons.Clear();

            var stageButtons = Object.FindObjectsOfType<StageButton>();
            foreach (var stageButton in stageButtons)
            {
                if (stageButton == null)
                    continue;

                stageButton.RegisterStagePreviewListener(ShowStagePreview);
                subscribedStageButtons.Add(stageButton);
            }

            Debug.Log($"[StageEnemyPreviewPanel] Subscribed to {subscribedStageButtons.Count} StageButton preview events");
        }

        /// <summary>
        /// 등록되어 있던 모든 StageButton에서 프리뷰 리스너를 제거합니다.
        /// </summary>
        private void UnsubscribeFromStageButtons()
        {
            foreach (var stageButton in subscribedStageButtons)
            {
                if (stageButton == null)
                    continue;

                stageButton.UnregisterStagePreviewListener(ShowStagePreview);
            }

            subscribedStageButtons.Clear();
        }

        /// <summary>
        /// 선택된 스테이지의 적 유닛 소환 카드를 프리뷰합니다.
        /// </summary>
        public void ShowStagePreview(StageDataSO stageData)
        {
            if (stageData == null)
            {
                Debug.LogWarning("[StageEnemyPreviewPanel] StageData is null");
                return;
            }

            if (contentRoot == null || enemyCardIconPrefab == null)
            {
                Debug.LogError("[StageEnemyPreviewPanel] UI references are not assigned");
                return;
            }

            if (stageNameText != null)
            {
                stageNameText.text = stageData.DisplayName;
            }
            else
            {
                Debug.LogWarning("[StageEnemyPreviewPanel] Stage name text is not assigned");
            }

            ClearIcons();

            var cards = GetEnemyUnitSummonCards(stageData);
            int count = 0;

            foreach (var card in cards)
            {
                if (card == null || card.CardArt == null)
                    continue;

                var iconUI = Object.Instantiate(enemyCardIconPrefab, contentRoot);
                if (iconUI != null)
                {
                    iconUI.SetIcon(card.CardArt);
                }

                count++;
            }

            Debug.Log($"[StageEnemyPreviewPanel] Previewing {count} enemy summon cards for stage: {stageData.StageId}, Name: {stageData.DisplayName}");

            OnShow();
        }

        /// <summary>
        /// 현재 표시 중인 아이콘을 모두 제거합니다.
        /// </summary>
        public void ClearIcons()
        {
            if (contentRoot == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = contentRoot.GetChild(i);
                if (child != null)
                {
                    Object.Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// EnemyCardPool에서 유닛 소환 카드만 필터링하고 UI용으로 정렬합니다.
        /// SO에는 헬퍼를 추가하지 않고, UI 계층에서만 해석 책임을 가집니다.
        /// </summary>
        private IEnumerable<CardData> GetEnemyUnitSummonCards(StageDataSO stageData)
        {
            var pool = stageData.EnemyCardPool;
            if (pool == null)
            {
                Debug.LogWarning($"[StageEnemyPreviewPanel] EnemyCardPool is null for stage: {stageData.StageId}");
                return Enumerable.Empty<CardData>();
            }

            var cards = pool.Cards;
            if (cards == null || cards.Count == 0)
            {
                return Enumerable.Empty<CardData>();
            }

            var query2 = cards
                .Where(card => card != null &&
                                card.EffectDefinitions != null &&
                                card.EffectDefinitions.Count > 0);

            // 1) 유닛 소환 카드 필터링
            var query = cards
                .Where(card => card != null &&
                                card.EffectDefinitions != null &&
                                card.EffectDefinitions.Count > 0 &&
                                card.EffectDefinitions.Any(d => d is SummonEffectDefinition s && s.UnitToSummon != null));

            // 2) 중복 제거 (CardID 기준)
            if (removeDuplicateCards)
            {
                query = query
                    .GroupBy(c => c.CardID)
                    .Select(g => g.First());
            }

            // 3) 정렬: 레어도 → 마나코스트 → 카드 이름
            IOrderedEnumerable<CardData> ordered = null;

            if (sortByRarity)
            {
                ordered = query.OrderBy(c => c.Rarity);
            }
            else
            {
                ordered = query.OrderBy(c => 0);
            }

            if (sortByManaCost)
            {
                ordered = ordered.ThenBy(c => c.ManaCost);
            }

            ordered = ordered.ThenBy(c => c.CardName);

            return ordered.ToList();
        }

        protected override void OnHidePanel()
        {
            // 숨길 때 아이콘을 정리할지 여부는 선택 사항
            // 여기서는 상태 유지 편의를 위해 정리는 하지 않음
        }
    }
}
