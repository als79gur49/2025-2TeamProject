using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Interfaces;
using Game.Card.UI;
using Game.Data;
using Game.Card.Effects;

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

        /// <summary>
        /// 핸드 레이아웃 재정렬 (외부 호출용)
        /// CardUI에서 카드 사용 실패 시 원래 인덱스로 복귀한 후 호출됨
        /// </summary>
        public void RefreshHandLayout()
        {
            UpdateHandLayout();
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


            GUILayout.EndArea();
        }

        /// <summary>
        /// 기본 테스트 카드 데이터 생성 (EffectData 기반)
        /// </summary>
        private List<CardData> CreateTestCardData()
        {
            var testCards = new List<CardData>();

            // 1. 파이어볼 (데미지 효과)
            var fireball = ScriptableObject.CreateInstance<CardData>();
            fireball.name = "파이어볼";
            var fireballEffect = new EffectData(EffectType.Damage, 25, AffectedType.Enemy, 5);
            // Note: 실제로는 CardData의 effectDataList를 설정해야 하지만,
            // private 필드이므로 에디터에서 직접 설정 필요
            testCards.Add(fireball);

            // 2. 힐 (회복 효과)
            var heal = ScriptableObject.CreateInstance<CardData>();
            heal.name = "치유";
            var healEffect = new EffectData(EffectType.Heal, 15, AffectedType.Ally, 3);
            testCards.Add(heal);

            // 3. 소환 (소환 효과)
            var summon = ScriptableObject.CreateInstance<CardData>();
            summon.name = "유닛 소환";
            var summonEffect = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);
            testCards.Add(summon);

            return testCards;
        }

        /// <summary>
        /// 다양한 주문 카드 생성 (EffectData 기반)
        /// </summary>
        private List<CardData> CreateSampleSpellCards()
        {
            var spellCards = new List<CardData>();

            // 공격 주문들
            var lightningArrow = ScriptableObject.CreateInstance<CardData>();
            lightningArrow.name = "번개 화살";
            spellCards.Add(lightningArrow);

            var iceSpear = ScriptableObject.CreateInstance<CardData>();
            iceSpear.name = "얼음 창";
            spellCards.Add(iceSpear);

            var meteor = ScriptableObject.CreateInstance<CardData>();
            meteor.name = "메테오";
            spellCards.Add(meteor);

            // 보조 주문들
            var haste = ScriptableObject.CreateInstance<CardData>();
            haste.name = "신속";
            spellCards.Add(haste);

            var teleport = ScriptableObject.CreateInstance<CardData>();
            teleport.name = "순간이동";
            spellCards.Add(teleport);

            return spellCards;
        }

        /// <summary>
        /// 샘플 유닛 카드 생성 (EffectData 기반)
        /// </summary>
        private List<CardData> CreateSampleUnitCards()
        {
            var unitCards = new List<CardData>();

            // 소환 효과를 가진 카드들 생성
            var warrior = ScriptableObject.CreateInstance<CardData>();
            warrior.name = "전사";
            unitCards.Add(warrior);

            var archer = ScriptableObject.CreateInstance<CardData>();
            archer.name = "궁수";
            unitCards.Add(archer);

            var mage = ScriptableObject.CreateInstance<CardData>();
            mage.name = "마법사";
            unitCards.Add(mage);

            return unitCards;
        }
#endif

        #endregion
    }
}