using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Interfaces;
using Game.Card.UI;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// 플레이어의 카드 핸드를 관리하는 서비스 (UI 포함)
    /// 카드 드로우, 핸드 표시, 플레이어 상호작용을 담당
    /// Phase 3: UI 및 상호작용 구현 완료
    /// </summary>
    public class CardHandManager : MonoBehaviour, ICardHandManager
    {
        [Header("핸드 관리 설정")]
        [SerializeField] private int maxHandSize = 7;
        [SerializeField] private bool enableLogging = true;

        [Header("UI 설정")]
        [SerializeField] private Transform handUIParent;
        [SerializeField] private GameObject cardUIPrefab;
        [SerializeField] private float cardSpacing = 120f;
        [SerializeField] private bool arrangeCardsInArc = true;
        [SerializeField] private float arcRadius = 800f;

        [Header("카드 데이터 소스")]
        [SerializeField] private List<CardData> availableCards = new List<CardData>();

        [Header("상호작용 설정")]
        [SerializeField] private bool enablePlayerInteraction = false;

        // 핸드 상태
        private List<CardData> handCards = new List<CardData>();
        private List<CardUI> cardUIComponents = new List<CardUI>();
        private bool isInitialized = false;
        private bool isPlayerSummonMode = false;

        // 서비스 참조
        private ITurnService turnService;

        /// <summary>핸드 매니저가 초기화되었는지 여부</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>현재 핸드의 카드 수</summary>
        public int HandSize => handCards.Count;

        /// <summary>플레이어 소환 모드인지 여부</summary>
        public bool IsPlayerSummonMode => isPlayerSummonMode;

        #region Unity Lifecycle

        private void Awake()
        {
            // CardServiceManager에 의해 초기화되므로 여기서는 초기화하지 않음
        }

        #endregion

        #region CardServiceManager 호출 메서드

        /// <summary>
        /// CardServiceManager에 의해 호출되는 초기화 메서드
        /// Unit의 Init() 패턴을 따라 외부에서 의존성을 주입받음
        /// </summary>
        /// <param name="iTurnService">턴 서비스 인터페이스</param>
        public void Init(ITurnService iTurnService)
        {
            if (isInitialized)
            {
                Debug.LogWarning($"[CardHandManager] {gameObject.name} already initialized");
                return;
            }

            Log("🖐️ Initializing CardHandManager...");

            // 외부에서 주입받은 의존성 설정
            InjectDependencies(iTurnService);

            // UI 컴포넌트 초기화
            InitializeUI();

            // 이벤트 시스템 설정
            SetupEventSystem();

            // 초기 핸드 설정 (테스트용)
            SetupInitialHand();

            isInitialized = true;
            Log("✅ CardHandManager initialization completed");
        }

        /// <summary>
        /// 외부에서 주입받은 서비스 의존성 설정
        /// </summary>
        private void InjectDependencies(ITurnService iTurnService)
        {
            turnService = iTurnService;

            if (turnService != null)
            {
                Log("✅ TurnService dependency injected successfully");
            }
            else
            {
                Debug.LogError("[CardHandManager] TurnService is null");
            }
        }

        /// <summary>
        /// UI 컴포넌트 초기화
        /// </summary>
        private void InitializeUI()
        {
            // HandUI 부모 오브젝트 찾기 또는 생성
            if (handUIParent == null)
            {
                handUIParent = FindOrCreateHandUIParent();
            }

            // CardUI 프리팹 검증
            if (cardUIPrefab == null)
            {
                LogError("CardUI Prefab not assigned! Please assign CardUI prefab in inspector.");
            }

            Log("✅ UI components initialized");
        }

        /// <summary>
        /// 이벤트 시스템 설정
        /// </summary>
        private void SetupEventSystem()
        {
            // TurnService 이벤트 구독 (CardServiceManager에서 처리하지만 여기서도 추가 처리 가능)
            // Phase 3에서는 UI 상태 관리에 집중
            Log("✅ Event system setup completed");
        }

        /// <summary>
        /// 초기 핸드 설정 (테스트용)
        /// </summary>
        private void SetupInitialHand()
        {
            // TODO: Phase 4에서 실제 카드 드로우 로직으로 대체
            Log("🎴 Initial hand setup completed (placeholder)");
        }

        /// <summary>
        /// HandUI 부모 오브젝트 찾기 또는 생성
        /// </summary>
        private Transform FindOrCreateHandUIParent()
        {
            // Canvas 하위에서 "HandUI" 오브젝트 찾기
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var existingHandUI = canvas.transform.Find("HandUI");
                if (existingHandUI != null)
                {
                    return existingHandUI;
                }

                // 없으면 생성
                var handUIObject = new GameObject("HandUI");
                handUIObject.transform.SetParent(canvas.transform, false);
                
                // RectTransform 설정
                var rectTransform = handUIObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 0.3f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                Log("🖼️ HandUI parent created");
                return handUIObject.transform;
            }

            LogError("Canvas not found! HandUI cannot be created.");
            return null;
        }

        #endregion

        #region 플레이어 상호작용 (Phase 3 구현 완료)

        /// <summary>
        /// 플레이어의 소환 모드 활성화 (AllySummon 페이즈에서 호출)
        /// </summary>
        public void EnablePlayerSummonMode()
        {
            if (!isInitialized)
            {
                LogError("CardHandManager not initialized");
                return;
            }

            isPlayerSummonMode = true;
            enablePlayerInteraction = true;

            // 모든 카드 UI를 드래그 가능하게 설정
            foreach (var cardUI in cardUIComponents)
            {
                if (cardUI != null)
                {
                    cardUI.SetDraggable(true);
                }
            }

            // HandUI 활성화
            if (handUIParent != null)
            {
                handUIParent.gameObject.SetActive(true);
            }

            Log("🟢 Player summon mode enabled - Cards can be dragged");
        }

        /// <summary>
        /// 플레이어의 소환 모드 비활성화
        /// </summary>
        public void DisablePlayerSummonMode()
        {
            if (!isInitialized)
            {
                LogError("CardHandManager not initialized");
                return;
            }

            isPlayerSummonMode = false;
            enablePlayerInteraction = false;

            // 모든 카드 UI를 드래그 불가능하게 설정
            foreach (var cardUI in cardUIComponents)
            {
                if (cardUI != null)
                {
                    cardUI.SetDraggable(false);
                }
            }

            Log("🔴 Player summon mode disabled - Cards cannot be dragged");
        }

        #endregion

        #region 핸드 관리 (Phase 3 구현)

        /// <summary>
        /// 핸드에 카드 추가
        /// </summary>
        public bool AddCardToHand(CardData cardData)
        {
            if (!isInitialized)
            {
                LogError("CardHandManager not initialized");
                return false;
            }

            if (cardData == null)
            {
                LogError("Cannot add null card to hand");
                return false;
            }

            if (handCards.Count >= maxHandSize)
            {
                LogError($"Hand is full! Cannot add {cardData.CardName}");
                return false;
            }

            // 카드 데이터 추가
            handCards.Add(cardData);

            // UI 생성
            CreateCardUI(cardData);

            // 핸드 레이아웃 업데이트
            UpdateHandLayout();

            Log($"✅ Added {cardData.CardName} to hand ({handCards.Count}/{maxHandSize})");
            return true;
        }

        /// <summary>
        /// 핸드에서 카드 제거
        /// </summary>
        public bool RemoveCardFromHand(CardData cardData)
        {
            if (!isInitialized || cardData == null)
                return false;

            int cardIndex = handCards.IndexOf(cardData);
            if (cardIndex == -1)
                return false;

            // 카드 데이터 제거
            handCards.RemoveAt(cardIndex);

            // UI 제거
            if (cardIndex < cardUIComponents.Count)
            {
                var cardUI = cardUIComponents[cardIndex];
                if (cardUI != null)
                {
                    DestroyImmediate(cardUI.gameObject);
                }
                cardUIComponents.RemoveAt(cardIndex);
            }

            // 핸드 레이아웃 업데이트
            UpdateHandLayout();

            Log($"✅ Removed {cardData.CardName} from hand ({handCards.Count}/{maxHandSize})");
            return true;
        }

        /// <summary>
        /// 카드 UI 생성
        /// </summary>
        private void CreateCardUI(CardData cardData)
        {
            if (cardUIPrefab == null || handUIParent == null)
            {
                LogError("Cannot create CardUI - prefab or parent missing");
                return;
            }

            // CardUI 인스턴스 생성
            var cardUIObject = Instantiate(cardUIPrefab, handUIParent);
            var cardUI = cardUIObject.GetComponent<CardUI>();

            if (cardUI != null)
            {
                // 카드 데이터 설정
                cardUI.SetCardData(cardData);

                // 드래그 가능 여부 설정
                cardUI.SetDraggable(isPlayerSummonMode && enablePlayerInteraction);

                // 리스트에 추가
                cardUIComponents.Add(cardUI);

                Log($"🎴 Created CardUI for {cardData.CardName}");
            }
            else
            {
                LogError($"CardUI component not found on prefab for {cardData.CardName}");
                DestroyImmediate(cardUIObject);
            }
        }

        /// <summary>
        /// 핸드 레이아웃 업데이트
        /// </summary>
        private void UpdateHandLayout()
        {
            if (cardUIComponents.Count == 0) return;

            if (arrangeCardsInArc)
            {
                ArrangeCardsInArc();
            }
            else
            {
                ArrangeCardsInLine();
            }
        }

        /// <summary>
        /// 카드들을 호형으로 배열
        /// </summary>
        private void ArrangeCardsInArc()
        {
            int cardCount = cardUIComponents.Count;
            if (cardCount == 0) return;

            float totalAngle = Mathf.Min(60f, cardCount * 8f); // 최대 60도
            float startAngle = -totalAngle * 0.5f;
            float angleStep = cardCount > 1 ? totalAngle / (cardCount - 1) : 0f;

            for (int i = 0; i < cardCount; i++)
            {
                if (cardUIComponents[i] == null) continue;

                float angle = startAngle + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;

                // 호형 위치 계산
                float x = Mathf.Sin(rad) * arcRadius;
                float y = -Mathf.Cos(rad) * arcRadius * 0.1f; // 살짝 아래로 구부림

                var rectTransform = cardUIComponents[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(x, y);
                    rectTransform.rotation = Quaternion.Euler(0, 0, angle * 0.5f); // 살짝 회전
                }
            }
        }

        /// <summary>
        /// 카드들을 일직선으로 배열
        /// </summary>
        private void ArrangeCardsInLine()
        {
            int cardCount = cardUIComponents.Count;
            if (cardCount == 0) return;

            float totalWidth = (cardCount - 1) * cardSpacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < cardCount; i++)
            {
                if (cardUIComponents[i] == null) continue;

                var rectTransform = cardUIComponents[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(startX + i * cardSpacing, 0);
                    rectTransform.rotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// 핸드 초기화 (모든 카드 제거)
        /// </summary>
        public void ClearHand()
        {
            // UI 제거
            foreach (var cardUI in cardUIComponents)
            {
                if (cardUI != null)
                {
                    DestroyImmediate(cardUI.gameObject);
                }
            }

            // 리스트 초기화
            handCards.Clear();
            cardUIComponents.Clear();

            Log("🗑️ Hand cleared");
        }

        /// <summary>
        /// 랜덤 카드 드로우 (ScriptableObject 기반)
        /// CardServiceManager에서 호출됨
        /// availableCards 목록에서 랜덤 CardData를 선택하여 핸드에 추가
        /// </summary>
        public void DrawRandomCard()
        {
            if (!isInitialized)
            {
                LogError("CardHandManager not initialized");
                return;
            }

            if (IsHandFull())
            {
                LogError("Hand is full - cannot draw more cards");
                return;
            }

            Log("🃏 DrawRandomCard() called - Drawing a random card for player");

            // availableCards에서 랜덤 선택
            if (availableCards != null && availableCards.Count > 0)
            {
                // 유효한 카드 데이터만 필터링
                var validCards = availableCards.Where(card => card != null).ToList();

                if (validCards.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, validCards.Count);
                    var randomCardData = validCards[randomIndex];

                    Log($"🎯 Selected random card: {randomCardData.CardName} (index {randomIndex}/{validCards.Count})");

                    bool success = AddCardToHand(randomCardData);

                    if (success)
                    {
                        Log($"✨ Successfully drew random card: {randomCardData.CardName}");
                    }
                    else
                    {
                        LogError("Failed to add random card to hand");
                    }
                }
                else
                {
                    LogError("No valid CardData available in availableCards list");
                }
            }
            else
            {
                // availableCards가 비어있으면 기본 테스트 데이터 사용 (fallback)
                LogError("availableCards list is empty, using fallback test data");
                var testCards = CreateTestCardData();

                if (testCards.Count > 0)
                {
                    int randomCardIndex = UnityEngine.Random.Range(0, testCards.Count);
                    var randomCardData = testCards[randomCardIndex];

                    bool success = AddCardToHand(randomCardData);

                    if (success)
                    {
                        Log($"✨ Successfully drew fallback card: {randomCardData.CardName}");
                    }
                    else
                    {
                        LogError("Failed to add fallback card to hand");
                    }
                }
                else
                {
                    LogError("No card data available for random draw");
                }
            }
        }

        #endregion

        #region 로깅 시스템

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[CardHandManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CardHandManager] {message}");
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 핸드 매니저 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetStatus()
        {
            return $"CardHandManager Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Max Hand Size: {maxHandSize}\n" +
                   $"- Current Hand Size: {handCards.Count}\n" +
                   $"- Player Summon Mode: {isPlayerSummonMode}\n" +
                   $"- Player Interaction: {enablePlayerInteraction}\n" +
                   $"- Hand UI Parent: {(handUIParent != null ? "✅" : "❌")}\n" +
                   $"- Card UI Prefab: {(cardUIPrefab != null ? "✅" : "❌")}\n" +
                   $"- Available Cards: {(availableCards != null ? availableCards.Count : 0)}\n";
        }

        /// <summary>
        /// 핸드의 모든 카드 데이터 반환
        /// </summary>
        public List<CardData> GetHandCards()
        {
            return new List<CardData>(handCards);
        }

        /// <summary>
        /// 특정 인덱스의 카드 반환
        /// </summary>
        public CardData GetCardAt(int index)
        {
            if (index >= 0 && index < handCards.Count)
                return handCards[index];
            return null;
        }

        /// <summary>
        /// 핸드가 가득 찼는지 확인
        /// </summary>
        public bool IsHandFull()
        {
            return handCards.Count >= maxHandSize;
        }

        /// <summary>
        /// 특정 카드가 핸드에 있는지 확인
        /// </summary>
        public bool HasCard(CardData cardData)
        {
            return handCards.Contains(cardData);
        }

        #endregion

        #region 에디터용 디버깅

#if UNITY_EDITOR
        [Header("에디터 디버깅 도구")]
        [SerializeField] private bool showHandDebugInfo = false;
        [SerializeField] private CardData testCardData;
        [SerializeField] private bool generateTestCards = false;

        private void OnGUI()
        {
            if (!showHandDebugInfo || !Application.isPlaying) return;

            var rect = new Rect(Screen.width - 350, 200, 330, 300);
            GUILayout.BeginArea(rect);
            GUILayout.Box("CardHandManager Debug");

            GUILayout.Label($"Initialized: {(isInitialized ? "✅" : "❌")}");
            GUILayout.Label($"Hand Size: {handCards.Count}/{maxHandSize}");
            GUILayout.Label($"Summon Mode: {(isPlayerSummonMode ? "🟢" : "🔴")}");
            GUILayout.Label($"Player Interaction: {(enablePlayerInteraction ? "✅" : "❌")}");

            GUILayout.Space(10);

            if (GUILayout.Button("Enable Summon Mode"))
            {
                EnablePlayerSummonMode();
            }

            if (GUILayout.Button("Disable Summon Mode"))
            {
                DisablePlayerSummonMode();
            }

            if (GUILayout.Button("Add Test Card") && testCardData != null)
            {
                AddCardToHand(testCardData);
            }

            if (GUILayout.Button("Clear Hand"))
            {
                ClearHand();
            }

            if (GUILayout.Button("Toggle Arc Layout"))
            {
                arrangeCardsInArc = !arrangeCardsInArc;
                UpdateHandLayout();
            }

            GUILayout.Space(10);
            GUILayout.Label("Cards in Hand:");
            foreach (var card in handCards)
            {
                GUILayout.Label($"  • {card.CardName}");
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// 기본 테스트 카드 데이터 생성
        /// </summary>
        private List<CardData> CreateTestCardData()
        {
            var testCards = new List<CardData>();

            // 1. 파이어볼 (데미지 주문)
            var fireball = CardData.CreateSpellCard(
                "파이어볼",
                "적에게 화염 피해를 입힙니다",
                3,
                CardData.SpellType.Damage, 25, 5f, 2f
            );
            testCards.Add(fireball);

            // 2. 힐 (회복 주문)
            var heal = CardData.CreateSpellCard(
                "치유",
                "아군을 회복시킵니다",
                2,
                CardData.SpellType.Heal, 15, 3f, 1f
            );
            testCards.Add(heal);

            // 3. 실드 (보호 주문)
            var shield = CardData.CreateSpellCard(
                "마법 방패",
                "아군에게 보호막을 생성합니다",
                2,
                CardData.SpellType.Shield, 10, 4f, 3f
            );
            testCards.Add(shield);

            return testCards;
        }

        /// <summary>
        /// 다양한 주문 카드 생성
        /// </summary>
        private List<CardData> CreateSampleSpellCards()
        {
            var spellCards = new List<CardData>();

            // 공격 주문들
            spellCards.Add(CardData.CreateSpellCard(
                "번개 화살", "순간적으로 적을 타격합니다",
                1, CardData.SpellType.Damage, 12, 6f, 0f
            ));

            spellCards.Add(CardData.CreateSpellCard(
                "얼음 창", "적을 얼려 둔화시킵니다",
                2, CardData.SpellType.Debuff, 8, 4f, 1.5f
            ));

            spellCards.Add(CardData.CreateSpellCard(
                "메테오", "광역 화염 피해를 입힙니다",
                5, CardData.SpellType.Damage, 40, 8f, 5f
            ));

            // 보조 주문들
            spellCards.Add(CardData.CreateSpellCard(
                "신속", "아군을 강화합니다",
                1, CardData.SpellType.Buff, 5, 3f, 2f
            ));

            spellCards.Add(CardData.CreateSpellCard(
                "순간이동", "아군을 다른 위치로 이동시킵니다",
                2, CardData.SpellType.Teleport, 0, 7f, 3f
            ));

            return spellCards;
        }

        /// <summary>
        /// 샘플 유닛 카드 생성 (UnitData 없이)
        /// </summary>
        private List<CardData> CreateSampleUnitCards()
        {
            var unitCards = new List<CardData>();

            // UnitData가 없어도 테스트할 수 있도록 일반 카드로 생성
            var warrior = CardData.CreateSpellCard(
                "전사", "근접 전투 유닛입니다",
                2, CardData.SpellType.Summon, 1, 0f, 0f
            );
            unitCards.Add(warrior);

            var archer = CardData.CreateSpellCard(
                "궁수", "원거리 공격 유닛입니다",
                2, CardData.SpellType.Summon, 1, 0f, 0f
            );
            unitCards.Add(archer);

            var mage = CardData.CreateSpellCard(
                "마법사", "마법 공격 유닛입니다",
                3, CardData.SpellType.Summon, 1, 0f, 0f
            );
            unitCards.Add(mage);

            return unitCards;
        }
#endif

        #endregion
    }
}