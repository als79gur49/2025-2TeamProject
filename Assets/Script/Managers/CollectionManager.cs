using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Game.Data;
using Game.Core;
using Game.SaveSystem;
using Game.Managers;

namespace Game.Managers
{
    /// <summary>
    /// 플레이어의 카드 컬렉션을 관리하는 매니저
    /// 소유한 카드 추가/제거, 저장/로드 기능 제공
    /// TitleSceneInitializer에서 ServiceLocator에 등록됨
    /// </summary>
    public class CollectionManager : MonoBehaviour, ICardCollection
    {
        [Header("Available Cards")]
        [SerializeField] private List<CardData> allAvailableCards = new List<CardData>();

        // 플레이어가 소유한 카드 (카드 → 소유 개수)
        private Dictionary<CardData, int> ownedCards = new Dictionary<CardData, int>();

        // SaveSystem 참조
        private ISaveDataAdapter saveAdapter;

        // 이벤트
        public event Action OnCollectionChanged;

        #region Lifecycle

        private void Start()
        {
            // ServiceBootstrap이 먼저 실행되므로 ServiceLocator는 이미 초기화됨
            // SaveAdapter 가져오기
            if (ServiceLocator.IsRegistered<ISaveDataAdapter>())
            {
                saveAdapter = ServiceLocator.Get<ISaveDataAdapter>();
                Debug.Log("[CollectionManager] SaveAdapter retrieved from ServiceLocator");

                // 저장된 컬렉션 로드
                LoadCollection();
            }
            else
            {
                Debug.LogWarning("[CollectionManager] SaveAdapter not registered, using legacy load");
                LoadCollectionLegacy(); // 기존 PlayerPrefs 방식 폴백
            }
        }

        #endregion

        #region Card Management

        /// <summary>
        /// 내부용 카드 추가 헬퍼 (저장/이벤트 없음)
        /// </summary>
        private bool AddCardInternal(CardData card, int count)
        {
            if (card == null)
            {
                Debug.LogWarning("[CollectionManager] Attempted to add null card");
                return false;
            }

            if (count <= 0)
            {
                Debug.LogWarning($"[CollectionManager] Attempted to add non-positive count: {count}");
                return false;
            }

            if (ownedCards.ContainsKey(card))
            {
                ownedCards[card] += count;
            }
            else
            {
                ownedCards[card] = count;
            }

            return true;
        }

        /// <summary>
        /// 컬렉션에 카드 추가
        /// </summary>
        public void AddCardToCollection(CardData card, int count = 1)
        {
            if (!AddCardInternal(card, count))
                return;

            OnCollectionChanged?.Invoke();
            SaveCollection();

            Debug.Log($"[CollectionManager] Added {count}x {card.CardName} to collection");
        }

        /// <summary>
        /// 컬렉션에 카드 추가 (ICardCollection 인터페이스 구현)
        /// </summary>
        public void AddCard(CardData card, int quantity = 1)
        {
            AddCardToCollection(card, quantity);
        }

        /// <summary>
        /// 여러 카드를 한 번에 컬렉션에 추가 (배치 처리)
        /// </summary>
        public void AddCards(IEnumerable<CardData> cards)
        {
            if (cards == null)
                return;

            bool anyAdded = false;

            foreach (var card in cards)
            {
                if (AddCardInternal(card, 1))
                {
                    anyAdded = true;
                }
            }

            if (!anyAdded)
                return;

            OnCollectionChanged?.Invoke();
            SaveCollection();

            Debug.Log($"[CollectionManager] Batch added cards: {GetUniqueCardCount()} unique cards, {GetTotalCardCount()} total");
        }

        /// <summary>
        /// 컬렉션에서 카드 제거
        /// </summary>
        public void RemoveCardFromCollection(CardData card, int count = 1)
        {
            if (card == null || !ownedCards.ContainsKey(card))
                return;

            ownedCards[card] -= count;

            if (ownedCards[card] <= 0)
            {
                ownedCards.Remove(card);
            }

            OnCollectionChanged?.Invoke();
            SaveCollection();

            Debug.Log($"[CollectionManager] Removed {count}x {card.CardName} from collection");
        }

        /// <summary>
        /// 특정 카드의 소유 개수 반환
        /// </summary>
        public int GetOwnedCount(CardData card)
        {
            return ownedCards.ContainsKey(card) ? ownedCards[card] : 0;
        }

        /// <summary>
        /// 소유한 모든 카드 목록 반환
        /// </summary>
        public List<CardData> GetAllOwnedCards()
        {
            return ownedCards.Keys.ToList();
        }

        /// <summary>
        /// 특정 카드를 소유하고 있는지 확인
        /// </summary>
        public bool HasCard(CardData card)
        {
            return ownedCards.ContainsKey(card) && ownedCards[card] > 0;
        }

        /// <summary>
        /// 총 카드 개수 반환 (모든 카드의 합)
        /// </summary>
        public int GetTotalCardCount()
        {
            return ownedCards.Values.Sum();
        }

        /// <summary>
        /// 고유 카드 종류 개수 반환
        /// </summary>
        public int GetUniqueCardCount()
        {
            return ownedCards.Count;
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// 컬렉션 저장 (새로운 SaveSystem 사용)
        /// </summary>
        public void SaveCollection()
        {
            if (saveAdapter != null && saveAdapter.IsInitialized)
            {
                saveAdapter.SaveSpecific(SaveFileType.CardCollection);
                Debug.Log($"[CollectionManager] Collection saved via SaveAdapter: {GetUniqueCardCount()} unique cards, {GetTotalCardCount()} total cards");
            }
            else
            {
                Debug.LogWarning("[CollectionManager] SaveAdapter not available, using legacy save");
                SaveCollectionLegacy();
            }
        }

        /// <summary>
        /// 컬렉션 로드 (새로운 SaveSystem 사용)
        /// </summary>
        public void LoadCollection()
        {
            if (saveAdapter != null && saveAdapter.IsInitialized)
            {
                saveAdapter.LoadSpecific(SaveFileType.CardCollection);
                Debug.Log($"[CollectionManager] Collection loaded via SaveAdapter: {GetUniqueCardCount()} unique cards, {GetTotalCardCount()} total cards");
            }
            else
            {
                Debug.LogWarning("[CollectionManager] SaveAdapter not available, using legacy load");
                LoadCollectionLegacy();
            }
        }

        /// <summary>
        /// 로드된 데이터를 컬렉션에 직접 적용 (저장 없이)
        /// SaveDataAdapter에서만 사용
        /// </summary>
        public void SetCollectionFromLoadedData(List<CardData> cards, List<int> counts)
        {
            if (cards == null || counts == null || cards.Count != counts.Count)
            {
                Debug.LogWarning("[CollectionManager] Invalid data for SetCollectionFromLoadedData");
                return;
            }

            // 기존 컬렉션 초기화
            ownedCards.Clear();

            // 로드된 데이터로 채우기
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && counts[i] > 0)
                {
                    ownedCards[cards[i]] = counts[i];
                }
            }

            // UI 업데이트를 위한 이벤트 발동
            OnCollectionChanged?.Invoke();

            Debug.Log($"[CollectionManager] Collection set from loaded data: {GetUniqueCardCount()} unique cards, {GetTotalCardCount()} total");
        }

        /// <summary>
        /// 컬렉션 저장 (기존 PlayerPrefs 방식 - 폴백용)
        /// </summary>
        private void SaveCollectionLegacy()
        {
            var saveData = new CollectionSaveData
            {
                cardIds = ownedCards.Keys.Select(c => c.name).ToList(),
                counts = ownedCards.Values.ToList()
            };

            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString("PlayerCollection", json);
            PlayerPrefs.Save();

            Debug.Log($"[CollectionManager] Collection saved (legacy mode): {GetUniqueCardCount()} unique cards, {GetTotalCardCount()} total cards");
        }

        /// <summary>
        /// 컬렉션 로드 (기존 PlayerPrefs 방식 - 폴백용)
        /// </summary>
        private void LoadCollectionLegacy()
        {
            string json = PlayerPrefs.GetString("PlayerCollection", "");

            if (string.IsNullOrEmpty(json))
            {
                // 처음 실행 시 기본 카드 지급
                GiveStarterCards();
                return;
            }

            var saveData = JsonUtility.FromJson<CollectionSaveData>(json);

            ownedCards.Clear();

            for (int i = 0; i < saveData.cardIds.Count; i++)
            {
                var card = allAvailableCards.Find(c => c.name == saveData.cardIds[i]);
                if (card != null)
                {
                    ownedCards[card] = saveData.counts[i];
                }
                else
                {
                    Debug.LogWarning($"[CollectionManager] Card not found: {saveData.cardIds[i]}");
                }
            }

            OnCollectionChanged?.Invoke();

            Debug.Log($"[CollectionManager] Collection loaded (legacy mode): {GetUniqueCardCount()} unique cards, {GetTotalCardCount()} total cards");
        }

        /// <summary>
        /// 스타터 카드 지급 (처음 실행 시)
        /// </summary>
        private void GiveStarterCards()
        {
            Debug.Log("[CollectionManager] First run detected - giving starter cards");

            // 사용 가능한 카드 중 처음 10종류를 각 3장씩 지급
            foreach (var card in allAvailableCards.Take(10))
            {
                AddCardToCollection(card, 3);
            }

            Debug.Log("[CollectionManager] Starter cards given");
        }

        #endregion

        #region Test Functions

        /// <summary>
        /// [테스트 전용] allAvailableCards 리스트를 저장
        /// 개발/디버깅 목적으로만 사용
        /// </summary>
        public void SaveAvailableCards()
        {
            var saveData = new CollectionSaveData
            {
                cardIds = allAvailableCards.Select(c => c.name).ToList(),
                counts = allAvailableCards.Select(c => 3).ToList() // 각 카드 3장씩 기본값
            };

            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString("PlayerCollection", json);
            PlayerPrefs.Save();

            Debug.Log($"[CollectionManager] Available cards saved: {allAvailableCards.Count} cards (3 copies each)");
        }

        /// <summary>
        /// [테스트 전용] TestCardRare 카드로 저장/로드 기능 테스트
        /// Unity Inspector에서 우클릭 → "Test - Save and Load TestCardRare" 실행
        /// </summary>
        [ContextMenu("Test - Save and Load TestCardRare")]
        private void TestSaveLoadCard()
        {
            Debug.Log("========== [CollectionManager] 저장/로드 테스트 시작 ==========");

            // 1단계: CardDatabase에서 TestCardRare 카드 가져오기
            ICardRegistry cardRegistry = ServiceLocator.IsRegistered<ICardRegistry>()
                ? ServiceLocator.Get<ICardRegistry>()
                : null;

            if (cardRegistry == null)
            {
                Debug.LogError("[테스트 실패] CardRegistry가 ServiceLocator에 등록되지 않았습니다.");
                return;
            }

            CardData testCard = cardRegistry.GetCardByID("TestCardRare");
            if (testCard == null)
            {
                Debug.LogError("[테스트 실패] TestCardRare 카드를 찾을 수 없습니다. Resources/Cards에 있는지 확인하세요.");
                return;
            }

            Debug.Log($"[1단계 완료] TestCardRare 카드 로드 성공: {testCard.CardName}");

            // 2단계: 컬렉션에 카드 3장 추가
            int initialCount = GetOwnedCount(testCard);
            AddCardToCollection(testCard, 3);
            int afterAddCount = GetOwnedCount(testCard);

            Debug.Log($"[2단계 완료] 카드 추가 완료 - 이전: {initialCount}장, 추가 후: {afterAddCount}장");

            // 3단계: 컬렉션 저장 (자동으로 SaveCollection이 호출되지만 명시적으로 재호출)
            SaveCollection();
            Debug.Log($"[3단계 완료] 컬렉션 저장 완료");

            // 4단계: 컬렉션 초기화 (로드 테스트를 위해)
            ownedCards.Clear();
            int afterClearCount = GetOwnedCount(testCard);
            Debug.Log($"[4단계 완료] 컬렉션 초기화 - 현재 카드 수: {afterClearCount}장");

            // 5단계: 컬렉션 로드
            LoadCollection();
            int afterLoadCount = GetOwnedCount(testCard);
            Debug.Log($"[5단계 완료] 컬렉션 로드 완료 - 로드 후 카드 수: {afterLoadCount}장");

            // 6단계: 결과 검증
            if (afterLoadCount == afterAddCount)
            {
                Debug.Log($"<color=green>========== [테스트 성공] TestCardRare 카드가 정확히 {afterLoadCount}장으로 복원되었습니다! ==========</color>");
            }
            else
            {
                Debug.LogError($"[테스트 실패] 예상: {afterAddCount}장, 실제: {afterLoadCount}장");
            }
        }

        #endregion
    }

    /// <summary>
    /// 컬렉션 저장 데이터 구조
    /// </summary>
    [System.Serializable]
    public class CollectionSaveData
    {
        public List<string> cardIds;
        public List<int> counts;
    }

    /// <summary>
    /// [테스트 전용] 사용 가능한 카드 목록 저장 데이터 구조
    /// </summary>
    [System.Serializable]
    public class AvailableCardsSaveData
    {
        public List<string> cardIds;
    }
}
