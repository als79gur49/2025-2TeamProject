using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Game.Data;
using Game.Card.Effects;

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
        private GameObject enemyCardIconPrefab;

        [Header("Sorting Options")]
        [SerializeField]
        private bool sortByRarity = true;

        [SerializeField]
        private bool sortByManaCost = true;

        [SerializeField]
        private bool removeDuplicateCards = true;

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
        }

        private void UnsubscribeFromEvents()
        {
            if (stageInfoEventChannel != null)
            {
                stageInfoEventChannel.Unsubscribe(ShowStagePreview);
                Debug.Log("[StageEnemyPreviewPanel] Unsubscribed from stage info events");
            }
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

            ClearIcons();

            var cards = GetEnemyUnitSummonCards(stageData);
            int count = 0;

            foreach (var card in cards)
            {
                if (card == null || card.IconSprite == null)
                    continue;

                var go = Object.Instantiate(enemyCardIconPrefab, contentRoot);
                var iconUI = go.GetComponent<EnemyCardIconUI>();
                if (iconUI != null)
                {
                    iconUI.SetIcon(card.IconSprite);
                }
                else
                {
                    // Fallback: 기존 방식 유지 (예전 프리팹 호환용)
                    var image = go.GetComponentInChildren<Image>();
                    if (image != null)
                    {
                        image.sprite = card.IconSprite;
                    }
                }

                count++;
            }

            Debug.Log($"[StageEnemyPreviewPanel] Previewing {count} enemy summon cards for stage: {stageData.StageId}");

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
