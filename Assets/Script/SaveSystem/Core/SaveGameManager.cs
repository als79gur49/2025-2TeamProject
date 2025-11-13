using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Game.SaveSystem
{
    /// <summary>
    /// 게임의 모든 저장 데이터를 통합 관리하는 매니저
    /// ISaveGameManager 인터페이스 구현
    /// </summary>
    public class SaveGameManager : MonoBehaviour, ISaveGameManager
    {
        #region Constants & Configuration
        private static string SAVE_DIRECTORY => Path.Combine(Application.persistentDataPath, "SaveData");

        // 파일 이름 상수
        private const string AUDIO_SETTINGS_FILE = "audio_settings.json";
        private const string PLAYER_DATA_FILE = "player_data.json";
        private const string STAGE_PROGRESS_FILE = "stage_progress.json";
        private const string CARD_COLLECTION_FILE = "card_collection.json";
        private const string SHOP_DATA_FILE = "shop_data.json";
        private const string DECK_DIRECTORY = "Decks";

        // JSON 설정
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            }
        };
        #endregion

        #region Properties
        public bool IsInitialized { get; private set; }
        public DateTime LastSaveTime { get; private set; }

        // 이벤트
        public event Action<SaveFileType> OnDataSaved;
        public event Action<SaveFileType> OnDataLoaded;
        public event Action<string> OnSaveError;
        public event Action<string> OnLoadError;
        #endregion

        #region Lifecycle
        private void Awake()
        {
            InitializeSaveSystem();
        }

        private void InitializeSaveSystem()
        {
            try
            {
                // 저장 디렉토리 생성
                if (!Directory.Exists(SAVE_DIRECTORY))
                {
                    Directory.CreateDirectory(SAVE_DIRECTORY);
                    Debug.Log($"[SaveGameManager] Created save directory: {SAVE_DIRECTORY}");
                }

                // 덱 디렉토리 생성
                string deckPath = Path.Combine(SAVE_DIRECTORY, DECK_DIRECTORY);
                if (!Directory.Exists(deckPath))
                {
                    Directory.CreateDirectory(deckPath);
                    Debug.Log($"[SaveGameManager] Created deck directory: {deckPath}");
                }

                IsInitialized = true;
                Debug.Log("[SaveGameManager] Save system initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveGameManager] Failed to initialize: {e.Message}");
                IsInitialized = false;
            }
        }
        #endregion

        #region IDataWriter Implementation

        public void SaveToFile<T>(string fileName, T data, SaveFileType fileType)
        {
            try
            {
                string filePath = Path.Combine(SAVE_DIRECTORY, fileName);
                string json = JsonConvert.SerializeObject(data, JsonSettings);
                File.WriteAllText(filePath, json);

                LastSaveTime = DateTime.Now;
                Debug.Log($"[SaveGameManager] {fileType} saved successfully");
                OnDataSaved?.Invoke(fileType);
            }
            catch (Exception e)
            {
                string error = $"Failed to save {fileType}: {e.Message}";
                Debug.LogError($"[SaveGameManager] {error}");
                OnSaveError?.Invoke(error);
            }
        }

        public bool DeleteSaveFile(SaveFileType fileType)
        {
            try
            {
                string fileName = GetFileName(fileType);
                string filePath = Path.Combine(SAVE_DIRECTORY, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[SaveGameManager] {fileType} file deleted");
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveGameManager] Failed to delete {fileType}: {e.Message}");
                return false;
            }
        }

        public void DeleteAllSaveFiles()
        {
            try
            {
                if (Directory.Exists(SAVE_DIRECTORY))
                {
                    Directory.Delete(SAVE_DIRECTORY, true);
                    InitializeSaveSystem(); // 디렉토리 재생성
                    Debug.Log("[SaveGameManager] All save files deleted");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveGameManager] Failed to delete all saves: {e.Message}");
            }
        }
        #endregion

        #region IDataReader Implementation

        public T LoadData<T>(SaveFileType fileType) where T : new()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SaveGameManager] System not initialized");
                return new T();
            }

            string fileName = GetFileName(fileType);
            return LoadFromFile<T>(fileName, fileType);
        }

        public bool HasSaveFile(SaveFileType fileType)
        {
            string fileName = GetFileName(fileType);
            string filePath = Path.Combine(SAVE_DIRECTORY, fileName);
            return File.Exists(filePath);
        }

        private T LoadFromFile<T>(string fileName, SaveFileType fileType) where T : new()
        {
            try
            {
                string filePath = Path.Combine(SAVE_DIRECTORY, fileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[SaveGameManager] {fileType} file not found, returning defaults");
                    return new T();
                }

                string json = File.ReadAllText(filePath);
                T data = JsonConvert.DeserializeObject<T>(json, JsonSettings);

                Debug.Log($"[SaveGameManager] {fileType} loaded successfully");
                OnDataLoaded?.Invoke(fileType);

                return data;
            }
            catch (Exception e)
            {
                string error = $"Failed to load {fileType}: {e.Message}";
                Debug.LogError($"[SaveGameManager] {error}");
                OnLoadError?.Invoke(error);
                return new T();
            }
        }
        #endregion

        #region IDeckManager Implementation

        public bool SaveDeck(string deckName, List<EnhancedCardData> cards)
        {
            try
            {
                string fileName = $"{SanitizeFileName(deckName)}.json";
                string filePath = Path.Combine(SAVE_DIRECTORY, DECK_DIRECTORY, fileName);

                var deckData = new DeckSaveData
                {
                    deckName = deckName,
                    cards = cards,
                    lastModified = DateTime.Now
                };

                string json = JsonConvert.SerializeObject(deckData, JsonSettings);
                File.WriteAllText(filePath, json);

                Debug.Log($"[SaveGameManager] Deck '{deckName}' saved successfully");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveGameManager] Failed to save deck '{deckName}': {e.Message}");
                return false;
            }
        }

        public DeckSaveData LoadDeck(string deckName)
        {
            try
            {
                string fileName = $"{SanitizeFileName(deckName)}.json";
                string filePath = Path.Combine(SAVE_DIRECTORY, DECK_DIRECTORY, fileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[SaveGameManager] Deck '{deckName}' not found");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var deckData = JsonConvert.DeserializeObject<DeckSaveData>(json, JsonSettings);

                Debug.Log($"[SaveGameManager] Deck '{deckName}' loaded successfully");
                return deckData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveGameManager] Failed to load deck '{deckName}': {e.Message}");
                return null;
            }
        }

        public List<string> GetSavedDeckNames()
        {
            try
            {
                string deckPath = Path.Combine(SAVE_DIRECTORY, DECK_DIRECTORY);
                if (!Directory.Exists(deckPath))
                    return new List<string>();

                string[] files = Directory.GetFiles(deckPath, "*.json");
                return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveGameManager] Failed to get deck names: {e.Message}");
                return new List<string>();
            }
        }

        public bool DeleteDeck(string deckName)
        {
            try
            {
                string fileName = $"{SanitizeFileName(deckName)}.json";
                string filePath = Path.Combine(SAVE_DIRECTORY, DECK_DIRECTORY, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[SaveGameManager] Deck '{deckName}' deleted");
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveGameManager] Failed to delete deck '{deckName}': {e.Message}");
                return false;
            }
        }
        #endregion

        #region ISaveGameManager Implementation

        public void SaveAllData()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SaveGameManager] System not initialized");
                return;
            }

            Debug.Log("[SaveGameManager] Saving all game data...");

            // Note: 실제 데이터 수집은 SaveDataAdapter에서 처리
            LastSaveTime = DateTime.Now;
            Debug.Log($"[SaveGameManager] All data save initiated at {LastSaveTime:yyyy-MM-dd HH:mm:ss}");
        }

        public void LoadAllData()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SaveGameManager] System not initialized");
                return;
            }

            Debug.Log("[SaveGameManager] Loading all game data...");
            // Note: 실제 데이터 적용은 SaveDataAdapter에서 처리
        }

        public void SaveData(SaveFileType fileType)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SaveGameManager] System not initialized");
                return;
            }

            // Note: 실제 구현은 SaveDataAdapter에서 데이터를 받아 처리
            Debug.Log($"[SaveGameManager] Save {fileType} initiated");
        }
        #endregion

        #region Helper Methods

        private string GetFileName(SaveFileType fileType)
        {
            switch (fileType)
            {
                case SaveFileType.AudioSettings:
                    return AUDIO_SETTINGS_FILE;
                case SaveFileType.PlayerData:
                    return PLAYER_DATA_FILE;
                case SaveFileType.StageProgress:
                    return STAGE_PROGRESS_FILE;
                case SaveFileType.CardCollection:
                    return CARD_COLLECTION_FILE;
                case SaveFileType.ShopData:
                    return SHOP_DATA_FILE;
                default:
                    return "";
            }
        }

        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars));
        }
        #endregion
    }
}
