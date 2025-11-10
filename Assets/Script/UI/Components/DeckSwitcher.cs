using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Game.SaveSystem;
using Game.Core;

namespace Game.UI.Components
{
    /// <summary>
    /// 스테이지 씬에서 빠른 덱 전환을 위한 드롭다운 컴포넌트
    /// TMP_Dropdown을 사용하여 저장된 덱 목록을 표시하고,
    /// 선택 시 PlayerData의 lastUsedDeckName을 업데이트합니다.
    /// </summary>
    public class DeckSwitcher : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private TMP_Dropdown deckDropdown;

        [Header("Settings")]
        [SerializeField] private string noDeckMessage = "저장된 덱 없음";

        #region Dependencies
        private ISaveDataAdapter saveDataAdapter;
        #endregion

        #region Internal State
        private List<string> deckNames = new List<string>();
        private bool hasDecks = false;
        #endregion

        #region Lifecycle

        /// <summary>
        /// 의존성이 필요한 초기화 (ServiceLocator 사용)
        /// </summary>
        protected override void OnInitializeWithDependencies()
        {
            // SaveDataAdapter 가져오기
            saveDataAdapter = ServiceLocator.Get<ISaveDataAdapter>();

            if (saveDataAdapter == null)
            {
                Debug.LogError("[DeckSwitcher] SaveDataAdapter not found in ServiceLocator!");
                return;
            }

            // 드롭다운 이벤트 구독
            if (deckDropdown != null)
            {
                deckDropdown.onValueChanged.AddListener(OnDeckSelected);
            }
            else
            {
                Debug.LogError("[DeckSwitcher] TMP_Dropdown reference is missing!");
                return;
            }

            // 덱 목록 로드 및 UI 초기화
            LoadDeckList();
            InitializeDropdown();

            Debug.Log("[DeckSwitcher] Initialized successfully");
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (deckDropdown != null)
            {
                deckDropdown.onValueChanged.RemoveListener(OnDeckSelected);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 저장된 덱 목록 로드
        /// </summary>
        private void LoadDeckList()
        {
            deckNames = saveDataAdapter.GetSavedDeckNames();
            hasDecks = deckNames != null && deckNames.Count > 0;

            if (hasDecks)
            {
                Debug.Log($"[DeckSwitcher] Loaded {deckNames.Count} deck(s): {string.Join(", ", deckNames)}");
            }
            else
            {
                Debug.Log("[DeckSwitcher] No saved decks found");
            }
        }

        /// <summary>
        /// 드롭다운 UI 초기화
        /// </summary>
        private void InitializeDropdown()
        {
            deckDropdown.ClearOptions();

            if (!hasDecks)
            {
                // 덱이 없을 경우: "저장된 덱 없음" 표시 및 비활성화
                deckDropdown.options.Add(new TMP_Dropdown.OptionData(noDeckMessage));
                deckDropdown.value = 0;
                deckDropdown.interactable = false;
                deckDropdown.RefreshShownValue();

                Debug.Log("[DeckSwitcher] Dropdown disabled - no decks available");
                return;
            }

            // 덱 목록을 드롭다운 옵션으로 추가
            deckDropdown.AddOptions(deckNames);
            deckDropdown.interactable = true;

            // 현재 lastUsedDeckName에 해당하는 덱을 자동 선택
            SelectLastUsedDeck();
        }

        /// <summary>
        /// lastUsedDeckName에 해당하는 덱을 드롭다운에서 자동 선택
        /// </summary>
        private void SelectLastUsedDeck()
        {
            string lastUsedDeck = saveDataAdapter.LoadLastUsedDeckName();

            if (string.IsNullOrEmpty(lastUsedDeck))
            {
                // lastUsedDeckName이 비어있으면 첫 번째 덱 선택
                deckDropdown.value = 0;
                Debug.Log($"[DeckSwitcher] No lastUsedDeckName found, defaulting to: {deckNames[0]}");
            }
            else
            {
                // lastUsedDeckName과 일치하는 덱 찾기
                int index = deckNames.IndexOf(lastUsedDeck);

                if (index >= 0)
                {
                    deckDropdown.value = index;
                    Debug.Log($"[DeckSwitcher] Auto-selected last used deck: {lastUsedDeck}");
                }
                else
                {
                    // 덱 목록에 없으면 첫 번째 덱 선택
                    deckDropdown.value = 0;
                    Debug.LogWarning($"[DeckSwitcher] LastUsedDeck '{lastUsedDeck}' not found in deck list, defaulting to: {deckNames[0]}");
                }
            }

            deckDropdown.RefreshShownValue();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 드롭다운에서 덱 선택 시 호출
        /// </summary>
        private void OnDeckSelected(int index)
        {
            if (!hasDecks || index < 0 || index >= deckNames.Count)
            {
                Debug.LogWarning($"[DeckSwitcher] Invalid deck index: {index}");
                return;
            }

            string selectedDeckName = deckNames[index];

            // lastUsedDeckName 즉시 업데이트
            saveDataAdapter.SaveLastUsedDeckName(selectedDeckName);

            Debug.Log($"[DeckSwitcher] Deck switched to: {selectedDeckName}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// 덱 목록을 새로고침 (외부에서 덱이 추가/삭제된 경우 호출)
        /// </summary>
        public void RefreshDeckList()
        {
            LoadDeckList();
            InitializeDropdown();
            Debug.Log("[DeckSwitcher] Deck list refreshed");
        }

        /// <summary>
        /// 현재 선택된 덱 이름 반환
        /// </summary>
        public string GetSelectedDeckName()
        {
            if (!hasDecks || deckDropdown.value < 0 || deckDropdown.value >= deckNames.Count)
            {
                return string.Empty;
            }

            return deckNames[deckDropdown.value];
        }

        /// <summary>
        /// 특정 덱을 선택 (외부에서 프로그래밍 방식으로 선택 변경)
        /// </summary>
        public void SelectDeck(string deckName)
        {
            if (!hasDecks)
            {
                Debug.LogWarning("[DeckSwitcher] Cannot select deck - no decks available");
                return;
            }

            int index = deckNames.IndexOf(deckName);

            if (index >= 0)
            {
                deckDropdown.value = index;
                Debug.Log($"[DeckSwitcher] Programmatically selected deck: {deckName}");
            }
            else
            {
                Debug.LogWarning($"[DeckSwitcher] Deck '{deckName}' not found in deck list");
            }
        }

        #endregion
    }
}
