using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.SaveSystem;
using Game.Core;

namespace Game.UI.Components
{
    /// <summary>
    /// 좌우 버튼을 사용한 덱 전환 컴포넌트
    /// - 이전/다음 버튼으로 현재 덱 인덱스를 변경
    /// - 현재 덱 이름과 (현재 인덱스 / 전체 덱 수)를 텍스트로 표시
    /// - 선택된 덱을 SaveDataAdapter의 lastUsedDeckName으로 저장
    /// </summary>
    public class DeckButtonSwitcher : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text deckNameText;
        [SerializeField] private TMP_Text deckIndexText;

        [Header("Settings")]
        [SerializeField] private string noDeckMessage = "저장된 덱 없음";

        #region Dependencies

        private ISaveDataAdapter saveDataAdapter;

        #endregion

        #region Internal State

        private readonly List<string> deckNames = new List<string>();
        private bool hasDecks;
        private int currentIndex;

        #endregion

        #region Lifecycle

        /// <summary>
        /// ServiceLocator를 사용한 의존성 초기화
        /// </summary>
        protected override void OnInitializeWithDependencies()
        {
            // SaveDataAdapter 가져오기
            saveDataAdapter = ServiceLocator.Get<ISaveDataAdapter>();

            if (saveDataAdapter == null)
            {
                Debug.LogError("[DeckButtonSwitcher] SaveDataAdapter not found in ServiceLocator!");
                return;
            }

            // 버튼 이벤트 구독
            if (previousButton != null)
            {
                previousButton.onClick.AddListener(OnClickPrevious);
            }
            else
            {
                Debug.LogError("[DeckButtonSwitcher] Previous button reference is missing!");
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(OnClickNext);
            }
            else
            {
                Debug.LogError("[DeckButtonSwitcher] Next button reference is missing!");
            }

            // 덱 목록 로드 및 초기 선택/표시
            LoadDeckList();
            SelectLastUsedDeckIndex();
            UpdateUI();

            Debug.Log("[DeckButtonSwitcher] Initialized successfully");
        }

        private void OnDestroy()
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(OnClickPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(OnClickNext);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 저장된 덱 이름 목록 로드
        /// </summary>
        private void LoadDeckList()
        {
            deckNames.Clear();

            if (saveDataAdapter == null)
            {
                hasDecks = false;
                Debug.LogError("[DeckButtonSwitcher] Cannot load deck list - SaveDataAdapter is null");
                return;
            }

            var savedDeckNames = saveDataAdapter.GetSavedDeckNames();

            if (savedDeckNames != null)
            {
                deckNames.AddRange(savedDeckNames);
            }

            hasDecks = deckNames.Count > 0;

            if (hasDecks)
            {
                Debug.Log($"[DeckButtonSwitcher] Loaded {deckNames.Count} deck(s): {string.Join(", ", deckNames)}");
            }
            else
            {
                Debug.Log("[DeckButtonSwitcher] No saved decks found");
            }
        }

        /// <summary>
        /// lastUsedDeckName 기반으로 초기 인덱스 선택
        /// </summary>
        private void SelectLastUsedDeckIndex()
        {
            if (!hasDecks)
            {
                currentIndex = 0;
                return;
            }

            string lastUsedDeck = saveDataAdapter.LoadLastUsedDeckName();

            if (string.IsNullOrEmpty(lastUsedDeck))
            {
                currentIndex = 0;
                Debug.Log($"[DeckButtonSwitcher] No lastUsedDeckName found, defaulting to: {deckNames[0]}");
                return;
            }

            int index = deckNames.IndexOf(lastUsedDeck);
            if (index >= 0)
            {
                currentIndex = index;
                Debug.Log($"[DeckButtonSwitcher] Auto-selected last used deck: {lastUsedDeck}");
            }
            else
            {
                currentIndex = 0;
                Debug.LogWarning($"[DeckButtonSwitcher] LastUsedDeck '{lastUsedDeck}' not found in deck list, defaulting to: {deckNames[0]}");
            }
        }

        /// <summary>
        /// 현재 인덱스를 기반으로 UI 텍스트 및 버튼 상태 갱신
        /// </summary>
        private void UpdateUI()
        {
            if (!hasDecks)
            {
                if (deckNameText != null)
                {
                    deckNameText.text = noDeckMessage;
                }

                if (deckIndexText != null)
                {
                    deckIndexText.text = "0 / 0";
                }

                if (previousButton != null)
                {
                    previousButton.interactable = false;
                }

                if (nextButton != null)
                {
                    nextButton.interactable = false;
                }

                return;
            }

            currentIndex = Mathf.Clamp(currentIndex, 0, deckNames.Count - 1);
            string currentDeckName = deckNames[currentIndex];

            if (deckNameText != null)
            {
                deckNameText.text = currentDeckName;
            }

            if (deckIndexText != null)
            {
                deckIndexText.text = $"{currentIndex + 1} / {deckNames.Count}";
            }

            bool enableButtons = deckNames.Count > 1;

            if (previousButton != null)
            {
                previousButton.interactable = enableButtons;
            }

            if (nextButton != null)
            {
                nextButton.interactable = enableButtons;
            }
        }

        #endregion

        #region Event Handlers

        private void OnClickPrevious()
        {
            if (!hasDecks || deckNames.Count == 0)
            {
                return;
            }

            currentIndex = (currentIndex - 1 + deckNames.Count) % deckNames.Count;
            ApplyDeckSelection();
        }

        private void OnClickNext()
        {
            if (!hasDecks || deckNames.Count == 0)
            {
                return;
            }

            currentIndex = (currentIndex + 1) % deckNames.Count;
            ApplyDeckSelection();
        }

        /// <summary>
        /// 현재 인덱스를 기준으로 UI 및 SaveData 갱신
        /// </summary>
        private void ApplyDeckSelection()
        {
            if (!hasDecks || currentIndex < 0 || currentIndex >= deckNames.Count)
            {
                Debug.LogWarning($"[DeckButtonSwitcher] Invalid deck index: {currentIndex}");
                UpdateUI();
                return;
            }

            UpdateUI();

            if (saveDataAdapter != null)
            {
                string selectedDeckName = deckNames[currentIndex];
                saveDataAdapter.SaveLastUsedDeckName(selectedDeckName);
                Debug.Log($"[DeckButtonSwitcher] Deck switched to: {selectedDeckName}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 덱 목록을 새로고침 (외부에서 덱이 추가/삭제된 경우 호출)
        /// </summary>
        public void RefreshDeckList()
        {
            if (saveDataAdapter == null)
            {
                Debug.LogError("[DeckButtonSwitcher] Cannot refresh deck list - SaveDataAdapter is null");
                return;
            }

            LoadDeckList();
            SelectLastUsedDeckIndex();
            UpdateUI();

            Debug.Log("[DeckButtonSwitcher] Deck list refreshed");
        }

        /// <summary>
        /// 현재 선택된 덱 이름 반환
        /// </summary>
        public string GetSelectedDeckName()
        {
            if (!hasDecks || currentIndex < 0 || currentIndex >= deckNames.Count)
            {
                return string.Empty;
            }

            return deckNames[currentIndex];
        }

        /// <summary>
        /// 특정 덱 이름을 프로그래밍 방식으로 선택
        /// </summary>
        public void SelectDeck(string deckName)
        {
            if (!hasDecks)
            {
                Debug.LogWarning("[DeckButtonSwitcher] Cannot select deck - no decks available");
                return;
            }

            int index = deckNames.IndexOf(deckName);

            if (index >= 0)
            {
                currentIndex = index;
                ApplyDeckSelection();
                Debug.Log($"[DeckButtonSwitcher] Programmatically selected deck: {deckName}");
            }
            else
            {
                Debug.LogWarning($"[DeckButtonSwitcher] Deck '{deckName}' not found in deck list");
            }
        }

        #endregion
    }
}

