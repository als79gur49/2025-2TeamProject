using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Data;

namespace Game.Systems
{
    /// <summary>
    /// 덱 저장/로드 시스템
    /// JSON 파일 기반으로 덱 데이터를 영구 저장 및 로드
    /// </summary>
    public static class DeckSaveSystem
    {
        private const string SAVE_FOLDER = "Decks";
        private const string FILE_EXTENSION = ".json";

        /// <summary>
        /// 덱 저장 경로
        /// </summary>
        private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

        /// <summary>
        /// 덱 저장
        /// </summary>
        /// <param name="deckName">덱 이름</param>
        /// <param name="deckCards">덱 카드 목록</param>
        /// <returns>저장 성공 여부</returns>
        public static bool SaveDeck(string deckName, Dictionary<CardData, int> deckCards)
        {
            try
            {
                // 저장 폴더 확인 및 생성
                if (!Directory.Exists(SavePath))
                {
                    Directory.CreateDirectory(SavePath);
                    Debug.Log($"[DeckSaveSystem] Created save directory: {SavePath}");
                }

                // 덱 데이터를 직렬화 가능한 형태로 변환
                DeckSaveData saveData = new DeckSaveData
                {
                    deckName = deckName,
                    cards = deckCards.Select(kvp => new CardEntry
                    {
                        cardId = kvp.Key.CardID,
                        count = kvp.Value
                    }).ToList()
                };

                // JSON 변환
                string json = JsonUtility.ToJson(saveData, true);

                // 파일 저장
                string fileName = SanitizeFileName(deckName) + FILE_EXTENSION;
                string filePath = Path.Combine(SavePath, fileName);
                File.WriteAllText(filePath, json);

                Debug.Log($"[DeckSaveSystem] Deck saved successfully: {filePath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DeckSaveSystem] Failed to save deck '{deckName}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 덱 로드
        /// </summary>
        /// <param name="deckName">덱 이름</param>
        /// <returns>로드된 덱 카드 목록 (실패 시 null)</returns>
        public static Dictionary<CardData, int> LoadDeck(string deckName)
        {
            try
            {
                string fileName = SanitizeFileName(deckName) + FILE_EXTENSION;
                string filePath = Path.Combine(SavePath, fileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[DeckSaveSystem] Deck file not found: {filePath}");
                    return null;
                }

                // JSON 읽기
                string json = File.ReadAllText(filePath);
                DeckSaveData saveData = JsonUtility.FromJson<DeckSaveData>(json);

                if (saveData == null || saveData.cards == null)
                {
                    Debug.LogError($"[DeckSaveSystem] Invalid deck data in file: {filePath}");
                    return null;
                }

                // CardData 참조 복원 (CardID를 통해)
                Dictionary<CardData, int> deckCards = new Dictionary<CardData, int>();
                foreach (var entry in saveData.cards)
                {
                    // CardID를 통해 CardData 찾기
                    // TODO: CardDatabase나 CollectionManager를 통해 CardData 찾기
                    // 현재는 임시로 null 체크만 수행
                    CardData cardData = FindCardDataById(entry.cardId);
                    if (cardData != null)
                    {
                        deckCards[cardData] = entry.count;
                    }
                    else
                    {
                        Debug.LogWarning($"[DeckSaveSystem] CardData not found for CardID: {entry.cardId}");
                    }
                }

                Debug.Log($"[DeckSaveSystem] Deck loaded successfully: {deckName} ({deckCards.Count} unique cards)");
                return deckCards;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DeckSaveSystem] Failed to load deck '{deckName}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 저장된 덱 목록 반환
        /// </summary>
        /// <returns>덱 이름 목록</returns>
        public static List<string> GetSavedDeckNames()
        {
            try
            {
                if (!Directory.Exists(SavePath))
                {
                    return new List<string>();
                }

                string[] files = Directory.GetFiles(SavePath, $"*{FILE_EXTENSION}");
                List<string> deckNames = files
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToList();

                Debug.Log($"[DeckSaveSystem] Found {deckNames.Count} saved decks");
                return deckNames;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DeckSaveSystem] Failed to get saved deck names: {e.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 덱 삭제
        /// </summary>
        /// <param name="deckName">덱 이름</param>
        /// <returns>삭제 성공 여부</returns>
        public static bool DeleteDeck(string deckName)
        {
            try
            {
                string fileName = SanitizeFileName(deckName) + FILE_EXTENSION;
                string filePath = Path.Combine(SavePath, fileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[DeckSaveSystem] Deck file not found: {filePath}");
                    return false;
                }

                File.Delete(filePath);
                Debug.Log($"[DeckSaveSystem] Deck deleted successfully: {deckName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DeckSaveSystem] Failed to delete deck '{deckName}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 덱 이름 변경
        /// </summary>
        /// <param name="oldName">기존 이름</param>
        /// <param name="newName">새 이름</param>
        /// <returns>변경 성공 여부</returns>
        public static bool RenameDeck(string oldName, string newName)
        {
            try
            {
                string oldFileName = SanitizeFileName(oldName) + FILE_EXTENSION;
                string newFileName = SanitizeFileName(newName) + FILE_EXTENSION;
                string oldFilePath = Path.Combine(SavePath, oldFileName);
                string newFilePath = Path.Combine(SavePath, newFileName);

                if (!File.Exists(oldFilePath))
                {
                    Debug.LogWarning($"[DeckSaveSystem] Deck file not found: {oldFilePath}");
                    return false;
                }

                if (File.Exists(newFilePath))
                {
                    Debug.LogWarning($"[DeckSaveSystem] Deck with new name already exists: {newName}");
                    return false;
                }

                File.Move(oldFilePath, newFilePath);
                Debug.Log($"[DeckSaveSystem] Deck renamed successfully: {oldName} -> {newName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DeckSaveSystem] Failed to rename deck '{oldName}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 파일 이름으로 사용할 수 없는 문자 제거
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = string.Join("_", fileName.Split(invalidChars));
            return sanitized;
        }

        /// <summary>
        /// CardID로 CardData 찾기
        /// TODO: CardDatabase나 CollectionManager와 연동
        /// </summary>
        private static CardData FindCardDataById(string cardId)
        {
            // CardDatabase 구현 후 여기서 카드 조회
            // 임시로 Resources에서 찾거나 CollectionManager 사용
            CardData[] allCards = Resources.LoadAll<CardData>("Cards");
            return allCards.FirstOrDefault(c => c.CardID == cardId);
        }

        /// <summary>
        /// 저장된 덱이 존재하는지 확인
        /// </summary>
        public static bool DeckExists(string deckName)
        {
            string fileName = SanitizeFileName(deckName) + FILE_EXTENSION;
            string filePath = Path.Combine(SavePath, fileName);
            return File.Exists(filePath);
        }
    }

    /// <summary>
    /// 덱 저장 데이터 (JSON 직렬화용)
    /// </summary>
    [System.Serializable]
    public class DeckSaveData
    {
        public string deckName;
        public List<CardEntry> cards;
    }

    /// <summary>
    /// 카드 엔트리 (JSON 직렬화용)
    /// </summary>
    [System.Serializable]
    public class CardEntry
    {
        public string cardId;
        public int count;
    }
}
