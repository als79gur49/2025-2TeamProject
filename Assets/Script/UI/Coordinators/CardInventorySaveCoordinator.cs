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

            // DeckBuilderPanel 이벤트 구독
            if (deckBuilderPanel != null)
            {
                deckBuilderPanel.OnDeckSaveRequested += HandleDeckSaveRequest;
                deckBuilderPanel.OnDeckLoadRequested += HandleDeckLoadRequest;
                deckBuilderPanel.OnDeckCreateRequested += HandleDeckCreateRequest;
                deckBuilderPanel.OnDeckDeleteRequested += HandleDeckDeleteRequest;
                Debug.Log("[CardInventorySaveCoordinator] Subscribed to DeckBuilderPanel events");
            }
            else
            {
                Debug.LogWarning("[CardInventorySaveCoordinator] DeckBuilderPanel reference not assigned!");
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

            // DeckBuilderPanel 이벤트 구독 해제
            if (deckBuilderPanel != null)
            {
                deckBuilderPanel.OnDeckSaveRequested -= HandleDeckSaveRequest;
                deckBuilderPanel.OnDeckLoadRequested -= HandleDeckLoadRequest;
                deckBuilderPanel.OnDeckCreateRequested -= HandleDeckCreateRequest;
                deckBuilderPanel.OnDeckDeleteRequested -= HandleDeckDeleteRequest;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 패널이 열릴 때 자동으로 카드 컬렉션 및 마지막 사용 덱 로드
        /// </summary>
        private void HandlePanelShown(IUIPanel panel)
        {
            if (saveAdapter == null || !saveAdapter.IsInitialized)
            {
                Debug.LogWarning("[CardInventorySaveCoordinator] SaveDataAdapter not available for loading");
                return;
            }

            // 1. 카드 컬렉션 데이터 로드
            saveAdapter.LoadSpecific(SaveFileType.CardCollection);
            Debug.Log("[CardInventorySaveCoordinator] Card collection loaded on panel shown");

            // 2. 저장된 덱 목록 가져오기 및 드롭다운 갱신
            if (deckBuilderPanel != null)
            {
                var savedDecks = saveAdapter.GetSavedDeckNames();
                deckBuilderPanel.SetAvailableDecks(savedDecks);

                // 3. 마지막 사용 덱 자동 로드
                string lastDeckName = saveAdapter.LoadLastUsedDeckName();

                // 빈 문자열 또는 null 체크
                if (string.IsNullOrEmpty(lastDeckName))
                {
                    Debug.Log("[CardInventorySaveCoordinator] No last used deck found, starting with empty deck");
                    return;
                }

                // 덱 파일 존재 여부 확인
                if (!savedDecks.Contains(lastDeckName))
                {
                    Debug.LogWarning($"[CardInventorySaveCoordinator] Last used deck '{lastDeckName}' not found");

                    // 다른 덱이 존재하면 첫 번째 덱 로드
                    if (savedDecks.Count > 0)
                    {
                        Debug.Log($"[CardInventorySaveCoordinator] Loading first available deck: {savedDecks[0]}");
                        var firstDeck = saveAdapter.LoadDeck(savedDecks[0]);
                        if (firstDeck != null)
                        {
                            deckBuilderPanel.LoadDeck(savedDecks[0], firstDeck);
                            deckBuilderPanel.SyncDropdownToDeck(savedDecks[0]);
                        }
                    }
                    return;
                }

                try
                {
                    var deckCards = saveAdapter.LoadDeck(lastDeckName);
                    if (deckCards != null)
                    {
                        deckBuilderPanel.LoadDeck(lastDeckName, deckCards);
                        deckBuilderPanel.SyncDropdownToDeck(lastDeckName);
                        Debug.Log($"[CardInventorySaveCoordinator] Auto-loaded last deck: {lastDeckName}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[CardInventorySaveCoordinator] Failed to auto-load deck '{lastDeckName}': {e.Message}");
                }
            }
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

        /// <summary>
        /// 사용자가 Save 버튼을 클릭했을 때 처리
        /// </summary>
        private void HandleDeckSaveRequest(string deckName, System.Collections.Generic.Dictionary<Game.Data.CardData, int> deckCards)
        {
            if (saveAdapter == null || !saveAdapter.IsInitialized)
            {
                Debug.LogError("[CardInventorySaveCoordinator] SaveAdapter not available");
                return;
            }

            bool success = saveAdapter.SaveDeck(deckName, deckCards);

            if (success)
            {
                // 마지막 사용 덱으로 설정
                saveAdapter.SaveLastUsedDeckName(deckName);
                Debug.Log($"[CardInventorySaveCoordinator] Deck '{deckName}' saved manually with {deckCards.Count} unique cards");

                // 드롭다운 갱신 (새로 저장된 덱 포함)
                var updatedDecks = saveAdapter.GetSavedDeckNames();
                if (deckBuilderPanel != null)
                {
                    deckBuilderPanel.SetAvailableDecks(updatedDecks);

                    // 드롭다운을 저장된 덱으로 동기화
                    deckBuilderPanel.SyncDropdownToDeck(deckName);
                }
            }
            else
            {
                Debug.LogError($"[CardInventorySaveCoordinator] Failed to save deck '{deckName}'");
            }
        }

        /// <summary>
        /// 사용자가 드롭다운에서 덱을 선택했을 때 처리
        /// </summary>
        private void HandleDeckLoadRequest(string deckName)
        {
            if (saveAdapter == null || !saveAdapter.IsInitialized)
            {
                Debug.LogError("[CardInventorySaveCoordinator] SaveAdapter not available");
                return;
            }

            var deckCards = saveAdapter.LoadDeck(deckName);

            if (deckCards != null)
            {
                if (deckBuilderPanel != null)
                {
                    deckBuilderPanel.LoadDeck(deckName, deckCards);
                    saveAdapter.SaveLastUsedDeckName(deckName);
                    Debug.Log($"[CardInventorySaveCoordinator] Deck '{deckName}' loaded from dropdown");
                }
            }
            else
            {
                Debug.LogError($"[CardInventorySaveCoordinator] Failed to load deck '{deckName}'");
            }
        }

        /// <summary>
        /// 사용자가 AddDeck 버튼을 클릭했을 때 처리
        /// </summary>
        private void HandleDeckCreateRequest()
        {
            if (saveAdapter == null || !saveAdapter.IsInitialized)
            {
                Debug.LogError("[CardInventorySaveCoordinator] SaveAdapter not available");
                return;
            }

            // 1. 고유한 덱 이름 생성
            string newDeckName = GenerateUniqueDeckName();

            // 2. 빈 덱 생성
            var emptyDeck = new System.Collections.Generic.Dictionary<Game.Data.CardData, int>();

            // 3. 새 덱 저장
            bool success = saveAdapter.SaveDeck(newDeckName, emptyDeck);

            if (success)
            {
                Debug.Log($"[CardInventorySaveCoordinator] New deck '{newDeckName}' created");

                // 4. 드롭다운 갱신
                var updatedDecks = saveAdapter.GetSavedDeckNames();
                if (deckBuilderPanel != null)
                {
                    deckBuilderPanel.SetAvailableDecks(updatedDecks);

                    // 5. 새 덱으로 전환
                    deckBuilderPanel.LoadDeck(newDeckName, emptyDeck);
                    deckBuilderPanel.SyncDropdownToDeck(newDeckName);

                    // 6. 마지막 사용 덱으로 설정
                    saveAdapter.SaveLastUsedDeckName(newDeckName);
                }
            }
            else
            {
                Debug.LogError($"[CardInventorySaveCoordinator] Failed to create deck '{newDeckName}'");
            }
        }

        /// <summary>
        /// 사용자가 RemoveDeck 버튼을 클릭했을 때 처리
        /// </summary>
        private void HandleDeckDeleteRequest(string deckName)
        {
            if (saveAdapter == null || !saveAdapter.IsInitialized)
            {
                Debug.LogError("[CardInventorySaveCoordinator] SaveAdapter not available");
                return;
            }

            // 1. 덱 삭제
            bool success = saveAdapter.DeleteDeck(deckName);

            if (success)
            {
                Debug.Log($"[CardInventorySaveCoordinator] Deck '{deckName}' deleted");

                // 2. 업데이트된 덱 목록 가져오기
                var updatedDecks = saveAdapter.GetSavedDeckNames();

                if (deckBuilderPanel != null)
                {
                    // 3. 드롭다운 갱신
                    deckBuilderPanel.SetAvailableDecks(updatedDecks);

                    // 4. 첫 번째 덱으로 전환 또는 빈 상태
                    if (updatedDecks.Count > 0)
                    {
                        // 첫 번째 덱 로드
                        string firstDeck = updatedDecks[0];
                        var deckCards = saveAdapter.LoadDeck(firstDeck);

                        if (deckCards != null)
                        {
                            deckBuilderPanel.LoadDeck(firstDeck, deckCards);
                            deckBuilderPanel.SyncDropdownToDeck(firstDeck);
                            saveAdapter.SaveLastUsedDeckName(firstDeck);
                            Debug.Log($"[CardInventorySaveCoordinator] Switched to first deck: {firstDeck}");
                        }
                    }
                    else
                    {
                        // 빈 상태로 전환
                        var emptyDeck = new System.Collections.Generic.Dictionary<Game.Data.CardData, int>();
                        deckBuilderPanel.LoadDeck("", emptyDeck);
                        Debug.Log("[CardInventorySaveCoordinator] No decks remaining, empty state");
                    }
                }
            }
            else
            {
                Debug.LogError($"[CardInventorySaveCoordinator] Failed to delete deck '{deckName}'");
            }
        }

        /// <summary>
        /// 고유한 덱 이름 생성 ("새로운 덱 1", "새로운 덱 2", ...)
        /// </summary>
        private string GenerateUniqueDeckName()
        {
            var existingDecks = saveAdapter.GetSavedDeckNames();
            int counter = 1;
            string baseName = "새로운 덱";
            string newName;

            do
            {
                newName = $"{baseName} {counter}";
                counter++;
            }
            while (existingDecks.Contains(newName));

            return newName;
        }

        #endregion
    }
}
