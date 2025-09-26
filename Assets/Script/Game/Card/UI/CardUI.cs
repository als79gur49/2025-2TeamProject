using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using TMPro;

namespace Game.Card.UI
{
    /// <summary>
    /// 카드 UI 컴포넌트 - 드래그 앤 드롭 및 시각적 표현을 담당
    /// Phase 3: UI 및 상호작용 구현의 핵심 컴포넌트
    /// </summary>
    public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("카드 UI 설정")]
        [SerializeField] private Image cardImage;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("드래그 설정")]
        [SerializeField] private float dragAlpha = 0.6f;
        [SerializeField] private bool returnToOriginalPosition = true;
        [SerializeField] private float returnSpeed = 10f;

        [Header("시각적 피드백")]
        [SerializeField] private GameObject glowEffect;
        [SerializeField] private Color validDropColor = Color.green;
        [SerializeField] private Color invalidDropColor = Color.red;

        // 드래그 상태 관리
        private Vector3 originalPosition;
        private Transform originalParent;
        private Canvas parentCanvas;
        private GraphicRaycaster graphicRaycaster;
        
        // 카드 데이터
        private CardData cardData;
        private bool isDraggable = false;
        private bool isDragging = false;

        // 서비스 참조
        private ICardSpawnService cardSpawnService;
        private ISpawnValidator spawnValidator;

        #region Unity Lifecycle

        private void Awake()
        {
            // 컴포넌트 참조 설정
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 부모 캔버스 찾기
            parentCanvas = GetComponentInParent<Canvas>();
            graphicRaycaster = parentCanvas?.GetComponent<GraphicRaycaster>();
        }

        private void Start()
        {
            // 서비스 의존성 주입
            InjectDependencies();
            
            // 초기 상태 설정
            SetDraggable(false);
        }

        #endregion

        #region 의존성 주입

        /// <summary>
        /// 서비스 의존성 주입
        /// </summary>
        public void InjectDependencies()
        {
            if (ServiceLocator.IsInitialized)
            {
                var cardServiceManager = ServiceLocator.Get<ICardServiceManager>();
                cardSpawnService = cardServiceManager?.GetCardSpawnService();
                spawnValidator = cardServiceManager?.GetSpawnValidator();
                
                if (cardSpawnService == null)
                    Debug.LogError("[CardUI] CardSpawnService not found in ServiceLocator");
                
                if (spawnValidator == null)
                    Debug.LogError("[CardUI] SpawnValidator not found in ServiceLocator");
            }
        }

        #endregion

        #region 카드 데이터 설정

        /// <summary>
        /// 카드 데이터 설정 및 UI 업데이트
        /// </summary>
        public void SetCardData(CardData data)
        {
            cardData = data;
            UpdateCardUI();
        }

        /// <summary>
        /// 카드 UI 정보 업데이트
        /// </summary>
        private void UpdateCardUI()
        {
            if (cardData == null) return;

            // 카드 정보 표시
            if (cardNameText != null)
                cardNameText.text = cardData.CardName;
            
            if (costText != null)
                costText.text = cardData.ManaCost.ToString();
            
            if (descriptionText != null)
                descriptionText.text = cardData.Description;

            // 카드 이미지 설정 (있는 경우)
            if (cardImage != null && cardData.CardArt != null)
                cardImage.sprite = cardData.CardArt;
        }

        #endregion

        #region 드래그 상태 관리

        /// <summary>
        /// 드래그 가능 여부 설정
        /// </summary>
        public void SetDraggable(bool draggable)
        {
            isDraggable = draggable;
            
            // 시각적 피드백
            if (canvasGroup != null)
            {
                canvasGroup.alpha = draggable ? 1f : 0.7f;
                canvasGroup.interactable = draggable;
            }

            // 글로우 이펙트
            if (glowEffect != null)
                glowEffect.SetActive(draggable);
        }

        /// <summary>
        /// 드래그 가능 여부 반환
        /// </summary>
        public bool IsDraggable => isDraggable;

        /// <summary>
        /// 현재 드래그 중인지 여부
        /// </summary>
        public bool IsDragging => isDragging;

        #endregion

        #region 드래그 앤 드롭 이벤트

        /// <summary>
        /// 드래그 시작
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable || cardData == null) return;

            isDragging = true;
            
            // 원래 위치와 부모 저장
            originalPosition = transform.position;
            originalParent = transform.parent;

            // 드래그 중 시각적 변경
            if (canvasGroup != null)
                canvasGroup.alpha = dragAlpha;

            // 레이캐스팅 비활성화 (다른 UI와 간섭 방지)
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;

            // 최상위로 이동 (다른 UI 위에 표시)
            if (parentCanvas != null)
                transform.SetParent(parentCanvas.transform, true);

            Debug.Log($"[CardUI] Started dragging card: {cardData.CardName}");
        }

        /// <summary>
        /// 드래그 중
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // 마우스 위치로 카드 이동
            transform.position = eventData.position;

            // 드롭 가능한 위치인지 실시간 검사
            CheckDropValidation(eventData);
        }

        /// <summary>
        /// 드래그 종료
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;

            // 드롭 처리 (레이캐스팅이 비활성화된 상태에서 실행)
            bool dropSuccess = HandleDrop(eventData);

            // 레이캐스팅 재활성화 (드롭 처리 완료 후)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            // 드롭 실패 시 원래 위치로 복귀
            if (!dropSuccess && returnToOriginalPosition)
            {
                StartCoroutine(ReturnToOriginalPosition());
            }

            Debug.Log($"[CardUI] Ended dragging card: {cardData.CardName}, Drop success: {dropSuccess}");
        }

        #endregion

        #region 드롭 처리

        /// <summary>
        /// 실시간 드롭 유효성 검사 및 시각적 피드백
        /// </summary>
        private void CheckDropValidation(PointerEventData eventData)
        {
            // Physics Raycast로 3D 타일 검출
            Camera camera = Camera.main;
            if (camera == null)
            {
                UpdateDropFeedback(false);
                return;
            }

            Ray ray = camera.ScreenPointToRay(eventData.position);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            bool isValidDrop = false;

            foreach (var hit in hits)
            {
                var tileDropHandler = hit.collider.GetComponent<TileDropHandler>();
                if (tileDropHandler != null)
                {
                    // 카드 드롭 유효성 검사
                    Vector2Int gridPosition = tileDropHandler.GetGridPosition();
                    if (spawnValidator != null && cardData != null)
                    {
                        isValidDrop = ValidateCardDrop(cardData, gridPosition);
                    }
                    break;
                }
            }

            // 시각적 피드백 업데이트
            UpdateDropFeedback(isValidDrop);
        }

        /// <summary>
        /// 드롭 처리 실행
        /// </summary>
        private bool HandleDrop(PointerEventData eventData)
        {
            // Physics Raycast로 3D 타일 검출
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[CardUI] Main camera not found for physics raycast");
                return false;
            }

            Ray ray = camera.ScreenPointToRay(eventData.position);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            foreach (var hit in hits)
            {
                Debug.Log($"Physics Raycast hit: {hit.collider.gameObject.name}");
                var tileDropHandler = hit.collider.GetComponent<TileDropHandler>();
                if (tileDropHandler != null)
                {
                    // 드롭 처리 위임
                    return tileDropHandler.HandleCardDrop(cardData, this);
                }
            }

            return false; // 유효한 드롭 위치 없음
        }

        /// <summary>
        /// 드롭 피드백 시각적 업데이트
        /// </summary>
        private void UpdateDropFeedback(bool isValid)
        {
            if (cardImage != null)
            {
                cardImage.color = isValid ? validDropColor : invalidDropColor;
            }
        }

        /// <summary>
        /// 카드 타입에 따른 드롭 유효성 검사
        /// </summary>
        private bool ValidateCardDrop(CardData cardData, Vector2Int gridPosition)
        {
            if (spawnValidator == null) return false;

            switch (cardData.CardType)
            {
                case CardData.CardType.Unit:
                    return spawnValidator.CanSpawnUnit(cardData, gridPosition);

                case CardData.CardType.Spell:
                    return spawnValidator.CanUseSpell(cardData, gridPosition);

                default:
                    return false;
            }
        }

        #endregion

        #region 위치 복귀

        /// <summary>
        /// 원래 위치로 복귀하는 코루틴
        /// </summary>
        private System.Collections.IEnumerator ReturnToOriginalPosition()
        {
            // 원래 부모로 복귀
            if (originalParent != null)
                transform.SetParent(originalParent, true);

            // 부드러운 이동
            while (Vector3.Distance(transform.position, originalPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, originalPosition, returnSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = originalPosition;

            // 색상 복원
            if (cardImage != null)
                cardImage.color = Color.white;
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 카드 사용 완료 처리 (소환 성공 시 호출)
        /// </summary>
        public void OnCardUsed()
        {
            // 카드 UI 제거 또는 비활성화
            gameObject.SetActive(false);
            Debug.Log($"[CardUI] Card used: {cardData?.CardName}");
        }

        /// <summary>
        /// 카드 데이터 반환
        /// </summary>
        public CardData GetCardData()
        {
            return cardData;
        }

        #endregion

        #region 에디터용 디버깅

#if UNITY_EDITOR
        [Header("디버깅 도구")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            var rect = new Rect(Screen.width - 300, 10, 280, 150);
            GUILayout.BeginArea(rect);
            GUILayout.Box("CardUI Debug Info");

            GUILayout.Label($"Card: {cardData?.CardName ?? "None"}");
            GUILayout.Label($"Draggable: {isDraggable}");
            GUILayout.Label($"Dragging: {isDragging}");
            GUILayout.Label($"Services Connected: {(cardSpawnService != null && spawnValidator != null)}");

            if (GUILayout.Button("Toggle Draggable"))
            {
                SetDraggable(!isDraggable);
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}