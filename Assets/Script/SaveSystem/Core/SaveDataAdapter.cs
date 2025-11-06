using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;
using Game.Managers;
using Game.Core;

namespace Game.SaveSystem
{
    /// <summary>
    /// SaveGameManager와 실제 게임 시스템을 연결하는 어댑터 클래스
    /// ISaveDataAdapter 인터페이스 구현
    /// </summary>
    public class SaveDataAdapter : MonoBehaviour, ISaveDataAdapter
    {
        #region Dependencies
        private ISaveGameManager saveManager;
        private IAudioServiceContainer audioServiceContainer;
        private IVolumeController volumeController;
        private CollectionManager collectionManager;
        #endregion

        #region Properties
        public bool IsInitialized { get; private set; }
        #endregion

        #region Initialization

        /// <summary>
        /// 의존성 주입을 통한 초기화
        /// ServiceBootstrap에서 호출
        /// </summary>
        public void Initialize(ISaveGameManager saveGameManager)
        {
            if (saveGameManager == null)
            {
                throw new ArgumentNullException(nameof(saveGameManager),
                    "SaveDataAdapter requires ISaveGameManager");
            }

            this.saveManager = saveGameManager;
            InitializeOtherServices();
            SubscribeEvents();

            IsInitialized = true;
            Debug.Log("[SaveDataAdapter] Initialized with dependency injection");
        }

        private void InitializeOtherServices()
        {
            // ServiceLocator를 통한 서비스 참조
            // IsInitialized 체크 대신 개별 서비스 존재 여부로 확인
            try
            {
                // AudioServiceContainer 가져오기
                if (ServiceLocator.IsRegistered<IAudioServiceContainer>())
                {
                    audioServiceContainer = ServiceLocator.Get<IAudioServiceContainer>();
                    Debug.Log($"[SaveDataAdapter] Get AudioServiceContainer");
                }
                else
                {
                    Debug.LogWarning($"[SaveDataAdapter] IAudioServiceContainer not registered yet");
                }

                // VolumeController 가져오기
                if (ServiceLocator.IsRegistered<IVolumeController>())
                {
                    volumeController = ServiceLocator.Get<IVolumeController>();
                    Debug.Log($"[SaveDataAdapter] Get VolumeController");
                }
                else
                {
                    Debug.LogWarning($"[SaveDataAdapter] IVolumeController not registered yet");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveDataAdapter] Some services not available: {e.Message}");
            }

            // CollectionManager 참조
            collectionManager = CollectionManager.Instance;
        }

        private void SubscribeEvents()
        {
            if (saveManager != null)
            {
                saveManager.OnDataSaved += HandleDataSaved;
                saveManager.OnDataLoaded += HandleDataLoaded;
                saveManager.OnSaveError += HandleSaveError;
                saveManager.OnLoadError += HandleLoadError;
            }
        }

        private void UnsubscribeEvents()
        {
            if (saveManager != null)
            {
                saveManager.OnDataSaved -= HandleDataSaved;
                saveManager.OnDataLoaded -= HandleDataLoaded;
                saveManager.OnSaveError -= HandleSaveError;
                saveManager.OnLoadError -= HandleLoadError;
            }
        }
        #endregion

        #region ISaveDataAdapter Implementation

        public void QuickSave()
        {
            ValidateSaveManager();

            Debug.Log("[SaveDataAdapter] Starting quick save...");

            // 오디오 설정 저장
            var audioSettings = CollectAudioSettings();
            saveManager.SaveToFile(GetFileName(SaveFileType.AudioSettings), audioSettings, SaveFileType.AudioSettings);

            // 플레이어 데이터 저장
            var playerData = CollectPlayerData();
            saveManager.SaveToFile(GetFileName(SaveFileType.PlayerData), playerData, SaveFileType.PlayerData);

            // 스테이지 진행도 저장
            var stageProgress = CollectStageProgress();
            saveManager.SaveToFile(GetFileName(SaveFileType.StageProgress), stageProgress, SaveFileType.StageProgress);

            // 카드 컬렉션 저장
            var cardCollection = CollectCardCollection();
            saveManager.SaveToFile(GetFileName(SaveFileType.CardCollection), cardCollection, SaveFileType.CardCollection);

            Debug.Log("[SaveDataAdapter] Quick save completed");
        }

        public void QuickLoad()
        {
            ValidateSaveManager();

            Debug.Log("[SaveDataAdapter] Starting quick load...");

            // 오디오 설정 로드
            var audioSettings = saveManager.LoadData<AudioSettingsData>(SaveFileType.AudioSettings);
            ApplyAudioSettings(audioSettings);

            // 플레이어 데이터 로드
            var playerData = saveManager.LoadData<PlayerData>(SaveFileType.PlayerData);
            ApplyPlayerData(playerData);

            // 스테이지 진행도 로드
            var stageProgress = saveManager.LoadData<StageProgressData>(SaveFileType.StageProgress);
            ApplyStageProgress(stageProgress);

            // 카드 컬렉션 로드
            var cardCollection = saveManager.LoadData<CardCollectionData>(SaveFileType.CardCollection);
            ApplyCardCollection(cardCollection);

            Debug.Log("[SaveDataAdapter] Quick load completed");
        }

        public void SaveSpecific(SaveFileType fileType)
        {
            ValidateSaveManager();

            switch (fileType)
            {
                case SaveFileType.AudioSettings:
                    var audio = CollectAudioSettings();
                    saveManager.SaveToFile(GetFileName(fileType), audio, fileType);
                    break;

                case SaveFileType.PlayerData:
                    var player = CollectPlayerData();
                    saveManager.SaveToFile(GetFileName(fileType), player, fileType);
                    break;

                case SaveFileType.StageProgress:
                    var stage = CollectStageProgress();
                    saveManager.SaveToFile(GetFileName(fileType), stage, fileType);
                    break;

                case SaveFileType.CardCollection:
                    var cards = CollectCardCollection();
                    saveManager.SaveToFile(GetFileName(fileType), cards, fileType);
                    break;
            }
        }

        public void LoadSpecific(SaveFileType fileType)
        {
            ValidateSaveManager();

            switch (fileType)
            {
                case SaveFileType.AudioSettings:
                    var audio = saveManager.LoadData<AudioSettingsData>(fileType);
                    ApplyAudioSettings(audio);
                    break;

                case SaveFileType.PlayerData:
                    var player = saveManager.LoadData<PlayerData>(fileType);
                    ApplyPlayerData(player);
                    break;

                case SaveFileType.StageProgress:
                    var stage = saveManager.LoadData<StageProgressData>(fileType);
                    ApplyStageProgress(stage);
                    break;

                case SaveFileType.CardCollection:
                    var cards = saveManager.LoadData<CardCollectionData>(fileType);
                    ApplyCardCollection(cards);
                    break;
            }
        }

        public bool HasSaveData()
        {
            ValidateSaveManager();
            return saveManager.HasSaveFile(SaveFileType.PlayerData);
        }

        public void DeleteAllSaveData()
        {
            ValidateSaveManager();
            saveManager.DeleteAllSaveFiles();
        }

        public bool SaveDeck(string deckName, Dictionary<CardData, int> deckCards)
        {
            ValidateSaveManager();
            var enhancedCards = ConvertDeckToEnhancedCards(deckCards);
            return saveManager.SaveDeck(deckName, enhancedCards);
        }

        public Dictionary<CardData, int> LoadDeck(string deckName)
        {
            ValidateSaveManager();
            var deckData = saveManager.LoadDeck(deckName);
            return deckData != null ? ConvertEnhancedCardsToDeck(deckData.cards) : null;
        }

        public List<string> GetSavedDeckNames()
        {
            ValidateSaveManager();
            return saveManager.GetSavedDeckNames();
        }

        public bool DeleteDeck(string deckName)
        {
            ValidateSaveManager();
            return saveManager.DeleteDeck(deckName);
        }

        public List<EnhancedCardData> ConvertDeckToEnhancedCards(Dictionary<CardData, int> deckCards)
        {
            var enhancedCards = new List<EnhancedCardData>();

            if (deckCards == null) return enhancedCards;

            foreach (var kvp in deckCards)
            {
                var cardData = kvp.Key;
                var count = kvp.Value;

                for (int i = 0; i < count; i++)
                {
                    var enhancedCard = new EnhancedCardData
                    {
                        cardID = cardData.CardID,
                        cardName = cardData.CardName,
                        quantity = 1,
                        level = 1,
                        enhancementLevel = 0,
                        experience = 0,
                        obtainedDate = DateTime.Now
                    };
                    enhancedCards.Add(enhancedCard);
                }
            }

            return enhancedCards;
        }

        public Dictionary<CardData, int> ConvertEnhancedCardsToDeck(List<EnhancedCardData> enhancedCards)
        {
            var deckCards = new Dictionary<CardData, int>();

            if (enhancedCards == null) return deckCards;

            foreach (var enhancedCard in enhancedCards)
            {
                CardData cardData = FindCardDataByID(enhancedCard.cardID);

                if (cardData != null)
                {
                    if (deckCards.ContainsKey(cardData))
                        deckCards[cardData]++;
                    else
                        deckCards[cardData] = 1;
                }
            }

            return deckCards;
        }
        #endregion

        #region Data Collection Methods

        private AudioSettingsData CollectAudioSettings()
        {
            var settings = new AudioSettingsData();

            if (volumeController != null)
            {
                VolumeSettings current = volumeController.GetCurrentSettings();

                settings.masterVolume = current.masterVolume;
                settings.bgmVolume = current.bgmVolume;
                settings.effectVolume = current.effectVolume;
                settings.isMasterMuted = current.masterMuted;
                settings.isBgmMuted = current.bgmMuted;
                settings.isEffectMuted = current.effectMuted;
            }
            else
            {
                Debug.LogWarning("[SaveDataAdapter] VolumeController not found, using default audio settings");
            }

            settings.lastModified = DateTime.Now;
            return settings;
        }

        private void ApplyAudioSettings(AudioSettingsData settings)
        {
            if (settings == null || volumeController == null) return;

            VolumeSettings volumeSettings = new VolumeSettings(
                settings.masterVolume,
                settings.bgmVolume,
                settings.effectVolume,
                settings.isMasterMuted,
                settings.isBgmMuted,
                settings.isEffectMuted
            );

            volumeController.ApplySettings(volumeSettings);

            Debug.Log("[SaveDataAdapter] Audio settings applied");
        }

        private PlayerData CollectPlayerData()
        {
            var playerData = new PlayerData();

            // 임시 구현 - 실제 게임 매니저에서 가져와야 함
            playerData.playerID = PlayerPrefs.GetString("PlayerID", GeneratePlayerID());
            playerData.playerName = PlayerPrefs.GetString("PlayerName", "Player");
            playerData.gold = PlayerPrefs.GetInt("Gold", 0);
            playerData.totalPlayTime = PlayerPrefs.GetFloat("TotalPlayTime", 0f);

            return playerData;
        }

        private void ApplyPlayerData(PlayerData data)
        {
            if (data == null) return;

            // 임시 구현
            PlayerPrefs.SetString("PlayerID", data.playerID);
            PlayerPrefs.SetString("PlayerName", data.playerName);
            PlayerPrefs.SetInt("Gold", data.gold);
            PlayerPrefs.SetFloat("TotalPlayTime", data.totalPlayTime);
            PlayerPrefs.Save();

            Debug.Log($"[SaveDataAdapter] Player data applied - ID: {data.playerID}, Gold: {data.gold}");
        }

        private StageProgressData CollectStageProgress()
        {
            var progress = new StageProgressData();

            // 임시 구현
            string savedProgress = PlayerPrefs.GetString("StageProgress", "");
            if (!string.IsNullOrEmpty(savedProgress))
            {
                try
                {
                    progress = Newtonsoft.Json.JsonConvert.DeserializeObject<StageProgressData>(savedProgress);
                }
                catch
                {
                    progress = GenerateDefaultStageProgress();
                }
            }
            else
            {
                progress = GenerateDefaultStageProgress();
            }

            return progress;
        }

        private void ApplyStageProgress(StageProgressData progress)
        {
            if (progress == null) return;

            // 임시 구현
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(progress);
            PlayerPrefs.SetString("StageProgress", json);
            PlayerPrefs.Save();

            Debug.Log($"[SaveDataAdapter] Stage progress applied - Current: {progress.currentChapter}-{progress.currentStage}");
        }

        private CardCollectionData CollectCardCollection()
        {
            var collection = new CardCollectionData();

            if (collectionManager != null)
            {
                var allCards = collectionManager.GetAllOwnedCards();

                foreach (var cardData in allCards)
                {
                    var enhancedCard = new EnhancedCardData
                    {
                        cardID = cardData.CardID,
                        cardName = cardData.CardName,
                        quantity = collectionManager.GetOwnedCount(cardData),
                        level = 1,
                        enhancementLevel = 0,
                        experience = 0,
                        isLocked = false,
                        obtainedDate = DateTime.Now
                    };

                    collection.ownedCards.Add(enhancedCard);
                }

                collection.totalCardsCollected = collection.ownedCards.Count;
            }

            collection.lastModified = DateTime.Now;
            return collection;
        }

        private void ApplyCardCollection(CardCollectionData collection)
        {
            if (collection == null || collectionManager == null) return;

            // CollectionSaveData 형식으로 변환 (기존 CollectionManager 호환)
            var saveData = new CollectionSaveData
            {
                cardIds = collection.ownedCards.Select(c => c.cardID).ToList(),
                counts = collection.ownedCards.Select(c => c.quantity).ToList()
            };

            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString("PlayerCollection", json);
            PlayerPrefs.Save();

            // CollectionManager 리로드
            collectionManager.LoadCollection();

            Debug.Log($"[SaveDataAdapter] Card collection applied - {collection.ownedCards.Count} cards");
        }
        #endregion

        #region Helper Methods

        private void ValidateSaveManager()
        {
            if (saveManager == null)
            {
                throw new InvalidOperationException(
                    "[SaveDataAdapter] SaveManager is not initialized. Call Initialize() first.");
            }
        }

        private string GetFileName(SaveFileType fileType)
        {
            switch (fileType)
            {
                case SaveFileType.AudioSettings:
                    return "audio_settings.json";
                case SaveFileType.PlayerData:
                    return "player_data.json";
                case SaveFileType.StageProgress:
                    return "stage_progress.json";
                case SaveFileType.CardCollection:
                    return "card_collection.json";
                default:
                    return "";
            }
        }

        private string GeneratePlayerID()
        {
            return "PLR" + UnityEngine.Random.Range(100000, 999999).ToString();
        }

        private StageProgressData GenerateDefaultStageProgress()
        {
            var progress = new StageProgressData();
            progress.currentChapter = 1;
            progress.currentStage = 1;
            progress.stageRecords.Add(new StageRecord
            {
                chapter = 1,
                stage = 1,
                isUnlocked = true,
                isCleared = false
            });

            return progress;
        }

        private CardData FindCardDataByID(string cardID)
        {
            // Resources에서 찾기
            CardData[] allCards = Resources.LoadAll<CardData>("Cards");
            return allCards.FirstOrDefault(c => c.CardID == cardID);
        }
        #endregion

        #region Event Handlers

        private void HandleDataSaved(SaveFileType fileType)
        {
            Debug.Log($"[SaveDataAdapter] {fileType} saved successfully");
        }

        private void HandleDataLoaded(SaveFileType fileType)
        {
            Debug.Log($"[SaveDataAdapter] {fileType} loaded successfully");
        }

        private void HandleSaveError(string error)
        {
            Debug.LogError($"[SaveDataAdapter] Save error: {error}");
        }

        private void HandleLoadError(string error)
        {
            Debug.LogError($"[SaveDataAdapter] Load error: {error}");
        }
        #endregion

        #region Lifecycle

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion
    }

    /// <summary>
    /// CollectionManager 호환을 위한 임시 데이터 구조
    /// </summary>
    [Serializable]
    public class CollectionSaveData
    {
        public List<string> cardIds = new List<string>();
        public List<int> counts = new List<int>();
    }
}
