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
        // 씬 종속 의존성은 ServiceLocator를 통해 필요 시 조회
        // GetCardRegistry(), GetCardCollection(), GetPlayerDataManager() → IPlayerDataManager, GetStageProgressManager() → IStageProgressManager 사용
        #endregion

        #region Properties
        public bool IsInitialized { get; private set; }
        #endregion

        #region Initialization

        /// <summary>
        /// 의존성 주입을 통한 초기화
        /// ServiceBootstrap에서 호출
        /// 씬 종속 의존성(CardRegistry, CardCollection, PlayerDataManager, StageProgressManager)은
        /// ServiceLocator를 통해 필요 시 조회됩니다.
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
            Debug.Log("[SaveDataAdapter] Initialized with ISaveGameManager. Scene-specific dependencies will be resolved via ServiceLocator.");
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

            // CollectionManager 참조는 Initialize()에서 주입받음
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

        public void SaveLastUsedDeckName(string deckName)
        {
            ValidateSaveManager();

            if (string.IsNullOrWhiteSpace(deckName))
            {
                Debug.LogWarning("[SaveDataAdapter] Refusing to save empty deck name");
                return;
            }

            var playerDataMgr = GetPlayerDataManager();
            if (playerDataMgr == null)
            {
                Debug.LogWarning("[SaveDataAdapter] PlayerDataManager not available, cannot save last used deck");
                return;
            }

            // PlayerData에 덱 이름 설정
            var playerData = playerDataMgr.GetCurrentPlayerData();
            playerData.lastUsedDeckName = deckName;

            // 즉시 저장
            SaveSpecific(SaveFileType.PlayerData);

            Debug.Log($"[SaveDataAdapter] Last used deck name saved: {deckName}");
        }

        public string LoadLastUsedDeckName()
        {
            ValidateSaveManager();

            var playerDataMgr = GetPlayerDataManager();
            if (playerDataMgr == null)
            {
                Debug.LogWarning("[SaveDataAdapter] PlayerDataManager not available, cannot load last used deck");
                return null;
            }

            var playerData = playerDataMgr.GetCurrentPlayerData();
            return playerData?.lastUsedDeckName;
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
            var playerDataMgr = GetPlayerDataManager();
            if (playerDataMgr == null)
            {
                Debug.LogWarning("[SaveDataAdapter] PlayerDataManager not available, returning empty PlayerData");
                return new PlayerData();
            }

            // PlayerDataManager에서 현재 런타임 데이터 가져오기
            return playerDataMgr.GetCurrentPlayerData();
        }

        private void ApplyPlayerData(PlayerData data)
        {
            var playerDataMgr = GetPlayerDataManager();
            if (data == null || playerDataMgr == null) return;

            // PlayerDataManager에 로드된 데이터 적용
            playerDataMgr.SetPlayerData(data);

            Debug.Log($"[SaveDataAdapter] Player data applied - ID: {data.playerID}, Gold: {data.gold}");
        }

        private StageProgressData CollectStageProgress()
        {
            var stageProgressMgr = GetStageProgressManager();
            if (stageProgressMgr == null)
            {
                Debug.LogWarning("[SaveDataAdapter] StageProgressManager not available, returning empty StageProgressData");
                return new StageProgressData();
            }

            // StageProgressManager에서 현재 런타임 데이터 가져오기
            return stageProgressMgr.GetProgressData();
        }

        private void ApplyStageProgress(StageProgressData progress)
        {
            var stageProgressMgr = GetStageProgressManager();
            if (progress == null || stageProgressMgr == null) return;

            // StageProgressManager에 로드된 데이터 적용
            stageProgressMgr.SetProgressData(progress);

            Debug.Log($"[SaveDataAdapter] Stage progress applied - Current: {progress.currentStageId}");
        }

        private CardCollectionData CollectCardCollection()
        {
            var collection = new CardCollectionData();
            var cardCol = GetCardCollection();

            if (cardCol != null)
            {
                var allCards = cardCol.GetAllOwnedCards();

                foreach (var cardData in allCards)
                {
                    var enhancedCard = new EnhancedCardData
                    {
                        cardID = cardData.CardID,
                        cardName = cardData.CardName,
                        quantity = cardCol.GetOwnedCount(cardData),
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
            var cardCol = GetCardCollection();
            if (collection == null || cardCol == null) return;

            // EnhancedCardData를 CardData + counts로 변환
            List<CardData> cards = new List<CardData>();
            List<int> counts = new List<int>();

            foreach (var enhancedCard in collection.ownedCards)
            {
                CardData cardData = FindCardDataByID(enhancedCard.cardID);
                if (cardData != null)
                {
                    cards.Add(cardData);
                    counts.Add(enhancedCard.quantity);
                }
                else
                {
                    Debug.LogWarning($"[SaveDataAdapter] Card not found: {enhancedCard.cardID}");
                }
            }

            // 직접 데이터 설정 (LoadCollection 호출 제거 - 무한 재귀 방지)
            cardCol.SetCollectionFromLoadedData(cards, counts);

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


        private CardData FindCardDataByID(string cardID)
        {
            // CardRegistry를 통한 O(1) 조회
            var registry = GetCardRegistry();
            return registry?.GetCardByID(cardID);
        }

        /// <summary>
        /// ServiceLocator를 통해 ICardRegistry를 조회합니다.
        /// </summary>
        private ICardRegistry GetCardRegistry()
        {
            if (ServiceLocator.IsRegistered<ICardRegistry>())
            {
                return ServiceLocator.Get<ICardRegistry>();
            }
            return null;
        }

        /// <summary>
        /// ServiceLocator를 통해 ICardCollection을 조회합니다.
        /// </summary>
        private ICardCollection GetCardCollection()
        {
            if (ServiceLocator.IsRegistered<ICardCollection>())
            {
                return ServiceLocator.Get<ICardCollection>();
            }
            return null;
        }

        /// <summary>
        /// ServiceLocator를 통해 IPlayerDataManager를 조회합니다.
        /// </summary>
        private IPlayerDataManager GetPlayerDataManager()
        {
            if (ServiceLocator.IsRegistered<IPlayerDataManager>())
            {
                return ServiceLocator.Get<IPlayerDataManager>();
            }
            return null;
        }

        /// <summary>
        /// ServiceLocator를 통해 IStageProgressManager를 조회합니다.
        /// </summary>
        private IStageProgressManager GetStageProgressManager()
        {
            if (ServiceLocator.IsRegistered<IStageProgressManager>())
            {
                return ServiceLocator.Get<IStageProgressManager>();
            }
            return null;
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
