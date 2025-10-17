using UnityEngine;
using System.Collections.Generic;
using Game.AI;
using Game.Interfaces;
using Game.Card.UI;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// 적군의 카드 핸드를 화면에 표시하는 뷰 컴포넌트 (View Only)
    /// 상호작용 없이 적군이 보유한 카드를 시각적으로만 표시
    /// </summary>
    public class EnemyCardHandView : MonoBehaviour, IEnemyCardHandView
    {
        [Header("UI 설정")]
        [SerializeField] private Transform enemyHandUIParent;
        [SerializeField] private GameObject cardUIPrefab;
        [SerializeField] private float cardSpacing = 130f;
        [SerializeField] private bool enableLogging = true;

        // 적군 AI 참조
        private EnemyAIController enemyAIController;

        // 카드 UI 관리
        private List<CardUI> enemyCardUIComponents = new List<CardUI>();
        private bool isInitialized = false;

        /// <summary>초기화 완료 여부</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>현재 표시 중인 적군 카드 수</summary>
        public int CardCount => enemyCardUIComponents.Count;

        #region Unity Lifecycle

        private void Awake()
        {
            // CardServiceManager에 의해 초기화되므로 여기서는 초기화하지 않음
        }

        #endregion

        #region 초기화

        /// <summary>
        /// CardServiceManager에 의해 호출되는 초기화 메서드
        /// </summary>
        /// <param name="enemyAI">적군 AI 컨트롤러</param>
        public void Init(EnemyAIController enemyAI)
        {
            if (isInitialized)
            {
                Debug.LogWarning($"[EnemyCardHandView] {gameObject.name} already initialized");
                return;
            }

            Log("👹 Initializing EnemyCardHandView...");

            // 의존성 설정
            enemyAIController = enemyAI;

            if (enemyAIController == null)
            {
                LogError("EnemyAIController is null!");
                return;
            }

            // UI 컴포넌트 초기화
            InitializeUI();

            isInitialized = true;
            Log("✅ EnemyCardHandView initialization completed");
        }

        /// <summary>
        /// UI 컴포넌트 초기화
        /// </summary>
        private void InitializeUI()
        {
            // EnemyHandUI 부모 오브젝트 찾기 또는 생성
            if (enemyHandUIParent == null)
            {
                enemyHandUIParent = FindOrCreateEnemyHandUIParent();
            }

            // CardUI 프리팹 검증
            if (cardUIPrefab == null)
            {
                LogError("CardUI Prefab not assigned! Please assign CardUI prefab in inspector.");
            }

            Log("✅ Enemy hand UI components initialized");
        }

        /// <summary>
        /// EnemyHandUI 부모 오브젝트 찾기 또는 생성
        /// 우측 상단에서 하단으로 배치
        /// </summary>
        private Transform FindOrCreateEnemyHandUIParent()
        {
            // Canvas 하위에서 "EnemyHandUI" 오브젝트 찾기
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var existingEnemyHandUI = canvas.transform.Find("EnemyHandUI");
                if (existingEnemyHandUI != null)
                {
                    return existingEnemyHandUI;
                }

                // 없으면 생성
                var enemyHandUIObject = new GameObject("EnemyHandUI");
                enemyHandUIObject.transform.SetParent(canvas.transform, false);

                // RectTransform 설정 - 우측 배치
                var rectTransform = enemyHandUIObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.8f, 0.3f);  // 우측 하단
                rectTransform.anchorMax = new Vector2(1.0f, 0.9f);  // 우측 상단
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                Log("🖼️ EnemyHandUI parent created on RIGHT side");
                return enemyHandUIObject.transform;
            }

            LogError("Canvas not found! EnemyHandUI cannot be created.");
            return null;
        }

        #endregion

        #region 적군 카드 동기화

        /// <summary>
        /// 적군 핸드 동기화 - EnemyAIController의 enemyHand와 UI를 동기화
        /// </summary>
        public void RefreshEnemyHand()
        {
            if (!isInitialized || enemyAIController == null)
            {
                LogError("Cannot refresh enemy hand - not initialized or AI controller missing");
                return;
            }

            // 기존 UI 모두 제거
            ClearAllCardUI();

            // 적군 AI의 핸드 정보 가져오기
            var handInfo = enemyAIController.GetHandInfo();
            Log($"Refreshing enemy hand: {handInfo}");

            // EnemyAIController의 EnemyHand 프로퍼티를 통해 실제 카드 데이터 가져오기
            var enemyCards = enemyAIController.EnemyHand;

            // 실제 카드 데이터를 사용하여 카드 UI 생성
            for (int i = 0; i < enemyCards.Count; i++)
            {
                CreateEnemyCardUI(enemyCards[i], i);
            }

            // 레이아웃 업데이트
            UpdateHandLayout();

            Log($"✅ Enemy hand refreshed: {enemyCards.Count} cards displayed with actual data");
        }

        /// <summary>
        /// 적군 카드 UI 생성 (View Only - 실제 카드 데이터 표시)
        /// </summary>
        /// <param name="cardData">표시할 카드 데이터</param>
        /// <param name="cardIndex">카드 인덱스</param>
        private void CreateEnemyCardUI(CardData cardData, int cardIndex)
        {
            if (cardUIPrefab == null || enemyHandUIParent == null)
            {
                LogError("Cannot create enemy CardUI - prefab or parent missing");
                return;
            }

            if (cardData == null)
            {
                LogError($"Cannot create enemy CardUI #{cardIndex} - CardData is null");
                return;
            }

            // CardUI 인스턴스 생성
            var cardUIObject = Instantiate(cardUIPrefab, enemyHandUIParent);
            var cardUI = cardUIObject.GetComponent<CardUI>();

            if (cardUI != null)
            {
                // 🎴 실제 카드 데이터 설정 (적군도 자신의 카드 정보를 볼 수 있음)
                cardUI.SetCardData(cardData);

                // 🔒 중요: 드래그 불가능 설정 (상호작용 완전 비활성화)
                cardUI.SetDraggable(false);

                // CanvasGroup으로 추가 상호작용 차단
                var canvasGroup = cardUIObject.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }

                // 리스트에 추가
                enemyCardUIComponents.Add(cardUI);

                Log($"🃏 Created enemy CardUI #{cardIndex}: {cardData.CardName} (View Only with Data)");
            }
            else
            {
                LogError($"CardUI component not found on prefab for enemy card #{cardIndex}");
                DestroyImmediate(cardUIObject);
            }
        }

        /// <summary>
        /// 핸드 레이아웃 업데이트 - 수직 배치 (위에서 아래로)
        /// </summary>
        private void UpdateHandLayout()
        {
            if (enemyCardUIComponents.Count == 0) return;

            ArrangeCardsVertically();
        }

        /// <summary>
        /// 카드들을 수직으로 배열 (위에서 아래로)
        /// </summary>
        private void ArrangeCardsVertically()
        {
            int cardCount = enemyCardUIComponents.Count;
            if (cardCount == 0) return;

            // 위에서 아래로 배치
            float startY = 0f;  // 상단 시작

            for (int i = 0; i < cardCount; i++)
            {
                if (enemyCardUIComponents[i] == null) continue;

                var rectTransform = enemyCardUIComponents[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 수직 배치 (X는 중앙, Y는 위에서 아래로)
                    rectTransform.anchoredPosition = new Vector2(0, startY - i * cardSpacing);
                    rectTransform.rotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// 모든 카드 UI 제거
        /// </summary>
        private void ClearAllCardUI()
        {
            foreach (var cardUI in enemyCardUIComponents)
            {
                if (cardUI != null)
                {
                    DestroyImmediate(cardUI.gameObject);
                }
            }

            enemyCardUIComponents.Clear();
            Log("🗑️ All enemy card UI cleared");
        }

        #endregion

        #region 로깅 시스템

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[EnemyCardHandView] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[EnemyCardHandView] {message}");
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 적군 카드 뷰 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetStatus()
        {
            return $"EnemyCardHandView Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Enemy AI Controller: {(enemyAIController != null ? "✅" : "❌")}\n" +
                   $"- Card Count: {CardCount}\n" +
                   $"- Enemy Hand UI Parent: {(enemyHandUIParent != null ? "✅" : "❌")}\n" +
                   $"- Card UI Prefab: {(cardUIPrefab != null ? "✅" : "❌")}\n";
        }

        #endregion

        #region 에디터용 디버깅

#if UNITY_EDITOR
        [Header("에디터 디버깅 도구")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            var rect = new Rect(Screen.width - 350, 500, 330, 200);
            GUILayout.BeginArea(rect);
            GUILayout.Box("EnemyCardHandView Debug");

            GUILayout.Label($"Initialized: {(isInitialized ? "✅" : "❌")}");
            GUILayout.Label($"Enemy AI: {(enemyAIController != null ? "✅" : "❌")}");
            GUILayout.Label($"Card Count: {CardCount}");

            GUILayout.Space(10);

            if (GUILayout.Button("Refresh Enemy Hand"))
            {
                RefreshEnemyHand();
            }

            if (GUILayout.Button("Clear All Cards"))
            {
                ClearAllCardUI();
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
