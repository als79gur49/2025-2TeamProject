using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Data;
using Game.Card.Migration;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Card.Backup
{
    /// <summary>
    /// CardData 마이그레이션 백업 및 롤백 시스템
    /// Phase 1.3: 마이그레이션 전략 수립의 안전성 보장 컴포넌트
    /// </summary>
    public static class CardMigrationBackup
    {
        /// <summary>
        /// 백업 정보를 담는 구조체
        /// </summary>
        [Serializable]
        public struct BackupInfo
        {
            public string BackupId;
            public DateTime BackupTime;
            public string BackupPath;
            public int CardCount;
            public string Description;
            public List<string> BackedUpCardNames;

            public BackupInfo(string id, string path, int cardCount, string description)
            {
                BackupId = id;
                BackupTime = DateTime.Now;
                BackupPath = path;
                CardCount = cardCount;
                Description = description;
                BackedUpCardNames = new List<string>();
            }
        }

        /// <summary>
        /// 백업 메타데이터
        /// </summary>
        [Serializable]
        public class BackupMetadata
        {
            public List<BackupInfo> Backups = new List<BackupInfo>();
            public string LastBackupId = "";
            public DateTime LastBackupTime = DateTime.MinValue;
        }

        private static readonly string BackupRootPath = Application.dataPath + "/CardDataBackups";
        private static readonly string MetadataFileName = "backup_metadata.json";

        /// <summary>
        /// 프로젝트 내 모든 CardData를 백업
        /// </summary>
        public static BackupInfo CreateFullBackup(string description = "자동 백업")
        {
            var backupId = GenerateBackupId();
            var backupPath = Path.Combine(BackupRootPath, backupId);

            try
            {
                // 백업 디렉터리 생성
                Directory.CreateDirectory(backupPath);

                // 모든 CardData 찾기
                var allCards = FindAllCardDataAssets();
                var backupInfo = new BackupInfo(backupId, backupPath, allCards.Count, description);

                Debug.Log($"[CardMigrationBackup] {allCards.Count}개의 CardData 백업 시작...");

                // 각 카드를 개별 파일로 백업
                foreach (var card in allCards)
                {
                    BackupSingleCard(card, backupPath);
                    backupInfo.BackedUpCardNames.Add(card.CardName);
                }

                // 백업 정보를 메타데이터에 저장
                SaveBackupMetadata(backupInfo);

                Debug.Log($"[CardMigrationBackup] 백업 완료: {backupId}");
                return backupInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardMigrationBackup] 백업 실패: {ex.Message}");
                return default(BackupInfo);
            }
        }

        /// <summary>
        /// 특정 CardData들을 백업
        /// </summary>
        public static BackupInfo CreateSelectiveBackup(List<CardData> cards, string description = "선택적 백업")
        {
            if (cards == null || cards.Count == 0)
            {
                Debug.LogWarning("[CardMigrationBackup] 백업할 카드가 없습니다.");
                return default(BackupInfo);
            }

            var backupId = GenerateBackupId();
            var backupPath = Path.Combine(BackupRootPath, backupId);

            try
            {
                Directory.CreateDirectory(backupPath);
                var backupInfo = new BackupInfo(backupId, backupPath, cards.Count, description);

                foreach (var card in cards)
                {
                    BackupSingleCard(card, backupPath);
                    backupInfo.BackedUpCardNames.Add(card.CardName);
                }

                SaveBackupMetadata(backupInfo);

                Debug.Log($"[CardMigrationBackup] 선택적 백업 완료: {backupId} ({cards.Count}개 카드)");
                return backupInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardMigrationBackup] 선택적 백업 실패: {ex.Message}");
                return default(BackupInfo);
            }
        }

        /// <summary>
        /// 백업에서 복원
        /// </summary>
        public static bool RestoreFromBackup(string backupId)
        {
            var metadata = LoadBackupMetadata();
            var backupInfo = metadata.Backups.Find(b => b.BackupId == backupId);

            if (string.IsNullOrEmpty(backupInfo.BackupId))
            {
                Debug.LogError($"[CardMigrationBackup] 백업을 찾을 수 없습니다: {backupId}");
                return false;
            }

            if (!Directory.Exists(backupInfo.BackupPath))
            {
                Debug.LogError($"[CardMigrationBackup] 백업 폴더가 존재하지 않습니다: {backupInfo.BackupPath}");
                return false;
            }

            try
            {
                Debug.Log($"[CardMigrationBackup] 백업에서 복원 시작: {backupId}");

                var backupFiles = Directory.GetFiles(backupInfo.BackupPath, "*.json");
                var restoredCount = 0;

                foreach (var backupFile in backupFiles)
                {
                    if (RestoreSingleCard(backupFile))
                    {
                        restoredCount++;
                    }
                }

#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif

                Debug.Log($"[CardMigrationBackup] 복원 완료: {restoredCount}/{backupFiles.Length}개 카드");
                return restoredCount > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardMigrationBackup] 복원 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 단일 카드를 백업
        /// </summary>
        private static void BackupSingleCard(CardData card, string backupPath)
        {
            var cardData = new CardBackupData(card);
            var json = JsonUtility.ToJson(cardData, true);
            var fileName = SanitizeFileName(card.CardName) + ".json";
            var filePath = Path.Combine(backupPath, fileName);

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 단일 카드를 복원
        /// </summary>
        private static bool RestoreSingleCard(string backupFilePath)
        {
            try
            {
                var json = File.ReadAllText(backupFilePath);
                var backupData = JsonUtility.FromJson<CardBackupData>(json);

                // 원본 CardData 찾기
                var originalCard = FindCardDataByName(backupData.CardName);
                if (originalCard == null)
                {
                    Debug.LogWarning($"[CardMigrationBackup] 원본 카드를 찾을 수 없습니다: {backupData.CardName}");
                    return false;
                }

                // 백업 데이터로 복원
                RestoreCardDataFromBackup(originalCard, backupData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardMigrationBackup] 카드 복원 실패 ({backupFilePath}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 백업 데이터를 CardData에 적용
        /// </summary>
        private static void RestoreCardDataFromBackup(CardData card, CardBackupData backupData)
        {
            // 리플렉션을 사용하여 private 필드 복원
            SetPrivateField(card, "cardName", backupData.CardName);
            SetPrivateField(card, "description", backupData.Description);
            SetPrivateField(card, "cardType", backupData.CardType);
            SetPrivateField(card, "rarity", backupData.Rarity);
            SetPrivateField(card, "manaCost", backupData.ManaCost);
            SetPrivateField(card, "targetType", backupData.TargetType);
            SetPrivateField(card, "range", backupData.Range);
            SetPrivateField(card, "areaOfEffect", backupData.AreaOfEffect);

            // 주문 관련 데이터 복원
            SetPrivateField(card, "spellType", backupData.SpellType);
            SetPrivateField(card, "spellEffectValue", backupData.SpellEffectValue);
            SetPrivateField(card, "spellRange", backupData.SpellRange);
            SetPrivateField(card, "spellCooldown", backupData.SpellCooldown);

            // Keywords 복원
            var keywordsList = new List<string>(backupData.Keywords ?? new string[0]);
            SetPrivateField(card, "keywords", keywordsList);

#if UNITY_EDITOR
            EditorUtility.SetDirty(card);
#endif
        }

        /// <summary>
        /// 리플렉션을 사용하여 private 필드 설정
        /// </summary>
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        /// <summary>
        /// 백업 ID 생성
        /// </summary>
        private static string GenerateBackupId()
        {
            return $"backup_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// 파일명 안전화
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        /// <summary>
        /// 프로젝트 내 모든 CardData 에셋 찾기
        /// </summary>
        private static List<CardData> FindAllCardDataAssets()
        {
            var allCards = new List<CardData>();

#if UNITY_EDITOR
            // Unity 에디터에서 모든 CardData 에셋 찾기
            var guids = AssetDatabase.FindAssets($"t:{typeof(CardData).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null)
                {
                    allCards.Add(card);
                }
            }
#else
            // 런타임에서 Resources 폴더의 CardData 찾기
            var resourceCards = Resources.LoadAll<CardData>("");
            allCards.AddRange(resourceCards);
#endif

            return allCards;
        }

        /// <summary>
        /// 이름으로 CardData 찾기
        /// </summary>
        private static CardData FindCardDataByName(string cardName)
        {
            var allCards = FindAllCardDataAssets();
            return allCards.Find(c => c.CardName == cardName);
        }

        /// <summary>
        /// 백업 메타데이터 저장
        /// </summary>
        private static void SaveBackupMetadata(BackupInfo newBackup)
        {
            var metadata = LoadBackupMetadata();
            metadata.Backups.Add(newBackup);
            metadata.LastBackupId = newBackup.BackupId;
            metadata.LastBackupTime = newBackup.BackupTime;

            // 오래된 백업 정리 (10개 초과시 가장 오래된 것부터 삭제)
            while (metadata.Backups.Count > 10)
            {
                var oldestBackup = metadata.Backups[0];
                metadata.Backups.RemoveAt(0);

                // 실제 백업 폴더도 삭제
                if (Directory.Exists(oldestBackup.BackupPath))
                {
                    Directory.Delete(oldestBackup.BackupPath, true);
                }
            }

            var metadataPath = Path.Combine(BackupRootPath, MetadataFileName);
            Directory.CreateDirectory(BackupRootPath);

            var json = JsonUtility.ToJson(metadata, true);
            File.WriteAllText(metadataPath, json);
        }

        /// <summary>
        /// 백업 메타데이터 로드
        /// </summary>
        private static BackupMetadata LoadBackupMetadata()
        {
            var metadataPath = Path.Combine(BackupRootPath, MetadataFileName);

            if (!File.Exists(metadataPath))
            {
                return new BackupMetadata();
            }

            try
            {
                var json = File.ReadAllText(metadataPath);
                return JsonUtility.FromJson<BackupMetadata>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardMigrationBackup] 메타데이터 로드 실패: {ex.Message}");
                return new BackupMetadata();
            }
        }

        /// <summary>
        /// 모든 백업 목록 조회
        /// </summary>
        public static List<BackupInfo> GetAllBackups()
        {
            return LoadBackupMetadata().Backups;
        }

        /// <summary>
        /// 특정 백업 삭제
        /// </summary>
        public static bool DeleteBackup(string backupId)
        {
            var metadata = LoadBackupMetadata();
            var backupIndex = metadata.Backups.FindIndex(b => b.BackupId == backupId);

            if (backupIndex == -1)
            {
                Debug.LogWarning($"[CardMigrationBackup] 백업을 찾을 수 없습니다: {backupId}");
                return false;
            }

            var backup = metadata.Backups[backupIndex];

            try
            {
                // 백업 폴더 삭제
                if (Directory.Exists(backup.BackupPath))
                {
                    Directory.Delete(backup.BackupPath, true);
                }

                // 메타데이터에서 제거
                metadata.Backups.RemoveAt(backupIndex);

                // 메타데이터 저장
                var metadataPath = Path.Combine(BackupRootPath, MetadataFileName);
                var json = JsonUtility.ToJson(metadata, true);
                File.WriteAllText(metadataPath, json);

                Debug.Log($"[CardMigrationBackup] 백업 삭제 완료: {backupId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardMigrationBackup] 백업 삭제 실패: {ex.Message}");
                return false;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 메뉴 - 전체 백업 생성
        /// </summary>
        [MenuItem("Tools/Card Migration/Create Full Backup")]
        public static void CreateFullBackupFromMenu()
        {
            var backup = CreateFullBackup("수동 전체 백업");
            if (!string.IsNullOrEmpty(backup.BackupId))
            {
                EditorUtility.DisplayDialog("백업 완료",
                    $"백업이 완료되었습니다.\n백업 ID: {backup.BackupId}\n카드 수: {backup.CardCount}",
                    "확인");
            }
        }

        /// <summary>
        /// 에디터 메뉴 - 백업 목록 조회
        /// </summary>
        [MenuItem("Tools/Card Migration/Show Backup List")]
        public static void ShowBackupListFromMenu()
        {
            var backups = GetAllBackups();
            var message = $"총 {backups.Count}개의 백업이 있습니다.\n\n";

            foreach (var backup in backups)
            {
                message += $"ID: {backup.BackupId}\n";
                message += $"시간: {backup.BackupTime:yyyy-MM-dd HH:mm:ss}\n";
                message += $"카드 수: {backup.CardCount}\n";
                message += $"설명: {backup.Description}\n\n";
            }

            EditorUtility.DisplayDialog("백업 목록", message, "확인");
        }
#endif
    }

    /// <summary>
    /// CardData 백업을 위한 직렬화 가능한 데이터 클래스
    /// </summary>
    [Serializable]
    public class CardBackupData
    {
        public string CardName;
        public string Description;
        public CardData.CardType CardType;
        public CardData.CardRarity Rarity;
        public int ManaCost;
        public CardData.TargetType TargetType;
        public int Range;
        public int AreaOfEffect;
        public CardData.SpellType SpellType;
        public int SpellEffectValue;
        public float SpellRange;
        public float SpellCooldown;
        public string[] Keywords;
        public DateTime BackupTime;

        public CardBackupData(CardData card)
        {
            CardName = card.CardName;
            Description = card.Description;
            CardType = card.Type;
            Rarity = card.Rarity;
            ManaCost = card.ManaCost;
            TargetType = card.Target;
            Range = card.Range;
            AreaOfEffect = card.AreaOfEffect;
            SpellType = card.SpellCategory;
            SpellEffectValue = card.SpellEffectValue;
            SpellRange = card.SpellRange;
            SpellCooldown = card.SpellCooldown;
            Keywords = card.Keywords.ToArray();
            BackupTime = DateTime.Now;
        }

        public CardBackupData()
        {
            // 기본 생성자 (JsonUtility 용)
        }
    }
}