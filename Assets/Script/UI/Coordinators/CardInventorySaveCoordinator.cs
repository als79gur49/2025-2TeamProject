using UnityEngine;
using Game.UI.Panels;
using Game.SaveSystem;
using Game.Core;

namespace Game.UI.Coordinators
{
    /// <summary>
    /// CardInventoryPanel의 자동 저장/로드를 관리하는 Coordinator
    /// SettingsPanel/SettingsCoordinator 패턴을 따름
    /// 패널 열림 시 자동 로드, 닫힘 시 자동 저장
    /// </summary>
    public class CardInventorySaveCoordinator : MonoBehaviour
    {
        [Header("Panel Reference")]
        [SerializeField] private CardInventoryPanel cardInventoryPanel;
        [SerializeField] private DeckBuilderPanel deckBuilderPanel;

        private ISaveDataAdapter saveAdapter;

        #region Lifecycle

        private void Start()
        {
            // ServiceLocator에서 SaveDataAdapter 가져오기
            if (ServiceLocator.IsRegistered<ISaveDataAdapter>())
            {
                saveAdapter = ServiceLocator.Get<ISaveDataAdapter>();
                Debug.Log("[CardInventorySaveCoordinator] SaveDataAdapter retrieved from ServiceLocator");
            }
            else
            {
                Debug.LogError("[CardInventorySaveCoordinator] ISaveDataAdapter not registered in ServiceLocator!");
                return;
            }

            // 패널 이벤트 구독
            if (cardInventoryPanel != null)
            {
                cardInventoryPanel.OnPanelShown += HandlePanelShown;
                cardInventoryPanel.OnPanelHidden += HandlePanelHidden;
                Debug.Log("[CardInventorySaveCoordinator] Subscribed to CardInventoryPanel events");
            }
            else
            {
                Debug.LogWarning("[CardInventoryPanel] CardInventoryPanel reference not assigned!");
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (cardInventoryPanel != null)
            {
                cardInventoryPanel.OnPanelShown -= HandlePanelShown;
                cardInventoryPanel.OnPanelHidden -= HandlePanelHidden;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 패널이 열릴 때 자동으로 카드 컬렉션 로드
        /// </summary>
        private void HandlePanelShown(IUIPanel panel)
        {
            if (saveAdapter == null || !saveAdapter.IsInitialized)
            {
                Debug.LogWarning("[CardInventorySaveCoordinator] SaveDataAdapter not available for loading");
                return;
            }

            // 카드 컬렉션 데이터 로드
            saveAdapter.LoadSpecific(SaveFileType.CardCollection);
            Debug.Log("[CardInventorySaveCoordinator] Card collection loaded on panel shown");
        }

        /// <summary>
        /// 패널이 닫힐 때 자동으로 카드 컬렉션 및 덱 데이터 저장
        /// </summary>
        private void HandlePanelHidden(IUIPanel panel)
        {
            if (saveAdapter == null || !saveAdapter.IsInitialized)
            {
                Debug.LogWarning("[CardInventorySaveCoordinator] SaveDataAdapter not available for saving");
                return;
            }

            // 카드 컬렉션 데이터 저장
            saveAdapter.SaveSpecific(SaveFileType.CardCollection);
            Debug.Log("[CardInventorySaveCoordinator] Card collection saved on panel hidden");

            // 현재 편집 중인 덱 자동 저장
            if (deckBuilderPanel != null)
            {
                string currentDeckName = deckBuilderPanel.GetCurrentDeckName();

                // 유효한 덱 이름이 있으면 덱 데이터 저장
                if (!string.IsNullOrWhiteSpace(currentDeckName))
                {
                    var deckCards = deckBuilderPanel.GetCurrentDeckCards();

                    // 빈 덱이 아닌 경우에만 저장
                    if (deckCards != null && deckCards.Count > 0)
                    {
                        bool deckSaved = saveAdapter.SaveDeck(currentDeckName, deckCards);
                        if (deckSaved)
                        {
                            // 덱 저장 성공 시 마지막 사용 덱으로 설정
                            saveAdapter.SaveLastUsedDeckName(currentDeckName);
                            Debug.Log($"[CardInventorySaveCoordinator] Deck '{currentDeckName}' auto-saved with {deckCards.Count} unique cards");
                        }
                        else
                        {
                            Debug.LogWarning($"[CardInventorySaveCoordinator] Failed to auto-save deck '{currentDeckName}'");
                        }
                    }
                    else
                    {
                        Debug.Log($"[CardInventorySaveCoordinator] Deck '{currentDeckName}' is empty, skipping auto-save");
                    }
                }
                else
                {
                    Debug.Log("[CardInventorySaveCoordinator] No valid deck name to save");
                }
            }
        }

        #endregion
    }
}
