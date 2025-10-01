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
        /// Phase 3.16: 카드 UI 정보 업데이트 - 새로운 카드 구조 반영
        /// </summary>
        private void UpdateCardUI()
        {
            if (cardData == null) return;

            // 카드 기본 정보 표시
            if (cardNameText != null)
                cardNameText.text = cardData.CardName;

            // 비용 정보 (색상 포함)
            if (costText != null)
            {
                costText.text = cardData.ManaCost.ToString();
                // 비용에 따른 색상 적용
                costText.color = GetManaCostColor(cardData.ManaCost);
            }

            // Phase 3.16: 새로운 GetDetailedDescription() 메서드 사용
            if (descriptionText != null)
            {
                if (cardData.IsEffectBasedCard)
                {
                    // 새로운 EffectData 시스템 사용
                    descriptionText.text = cardData.GetDetailedDescription();
                }
                else
                {
                    // 레거시 시스템 또는 기본 설명
                    descriptionText.text = !string.IsNullOrEmpty(cardData.Description)
                        ? cardData.Description
                        : "효과 정보 없음";
                }
            }

            // 카드 이미지 설정
            if (cardImage != null && cardData.CardArt != null)
                cardImage.sprite = cardData.CardArt;

            // Phase 3.16: 카드 레어리티에 따른 테두리 색상 적용
            ApplyRarityVisualEffects();

            // Phase 3.16: EffectData 기반 추가 시각적 표현
            ApplyEffectTypeVisualCues();
        }

        /// <summary>
        /// Phase 3.16: 마나 비용에 따른 색상 반환
        /// </summary>
        private Color GetManaCostColor(int manaCost)
        {
            return manaCost switch
            {
                <= 1 => Color.white,
                <= 3 => Color.yellow,
                <= 5 => Color.cyan,
                <= 7 => Color.magenta,
                _ => Color.red
            };
        }

        /// <summary>
        /// Phase 3.16: 카드 레어리티에 따른 시각적 효과 적용
        /// </summary>
        private void ApplyRarityVisualEffects()
        {
            if (cardData == null) return;

            // 카드 테두리나 배경색 적용 (카드 이미지 컴포넌트 사용)
            if (cardImage != null)
            {
                var rarityColor = cardData.GetRarityColor();

                // 카드 이미지의 색조 조정 (미묘하게 적용)
                var imageColor = cardImage.color;
                imageColor = Color.Lerp(imageColor, rarityColor, 0.2f);
                cardImage.color = imageColor;
            }

            // 글로우 효과가 있다면 레어리티 색상으로 조정
            if (glowEffect != null)
            {
                var glowRenderer = glowEffect.GetComponent<Renderer>();
                if (glowRenderer != null)
                {
                    glowRenderer.material.color = cardData.GetRarityColor();
                }
            }
        }

        /// <summary>
        /// Phase 3.16: 효과 타입에 따른 시각적 단서 적용
        /// </summary>
        private void ApplyEffectTypeVisualCues()
        {
            if (cardData == null || !cardData.IsEffectBasedCard) return;

            // 주요 효과 타입 가져오기
            var primaryEffect = cardData.GetPrimaryEffectType();
            if (primaryEffect == null) return;

            // 카드 이름 텍스트에 효과 타입 아이콘 추가
            if (cardNameText != null)
            {
                string icon = primaryEffect switch
                {
                    Game.Card.Effects.EffectType.Damage => "⚔️",
                    Game.Card.Effects.EffectType.Heal => "💚",
                    Game.Card.Effects.EffectType.Summon => "🛡️",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(icon))
                {
                    cardNameText.text = $"{icon} {cardData.CardName}";
                }
            }
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
        /// Phase 3.16: 카드 드롭 유효성 검사 - 새로운 EffectData 시스템 적용
        /// </summary>
        private bool ValidateCardDrop(CardData cardData, Vector2Int gridPosition)
        {
            if (spawnValidator == null) return false;

            // Phase 3.16: 새로운 EffectData 시스템 우선 사용
            if (cardData.IsEffectBasedCard)
            {
                return ValidateEffectBasedCardDrop(cardData, gridPosition);
            }

            // 레거시 시스템 폴백
            return ValidateLegacyCardDrop(cardData, gridPosition);
        }

        /// <summary>
        /// Phase 3.16: EffectData 기반 카드 드롭 유효성 검사
        /// </summary>
        private bool ValidateEffectBasedCardDrop(CardData cardData, Vector2Int gridPosition)
        {
            if (!cardData.IsEffectBasedCard) return false;

            // 각 효과별로 드롭 유효성 검사
            var effects = cardData.EffectDataList;
            bool anyEffectValid = false;

            foreach (var effect in effects)
            {
                bool effectValid = effect.Type switch
                {
                    Game.Card.Effects.EffectType.Summon => ValidateSummonEffect(cardData, effect, gridPosition),
                    Game.Card.Effects.EffectType.Damage => ValidateDamageEffect(cardData, effect, gridPosition),
                    Game.Card.Effects.EffectType.Heal => ValidateHealEffect(cardData, effect, gridPosition),
                    _ => false
                };

                if (effectValid)
                {
                    anyEffectValid = true;
                    break; // 하나라도 유효하면 드롭 가능
                }
            }

            return anyEffectValid;
        }

        /// <summary>
        /// Phase 3.16: 소환 효과 드롭 유효성 검사
        /// </summary>
        private bool ValidateSummonEffect(CardData cardData, Game.Card.Effects.EffectData effect, Vector2Int gridPosition)
        {
            // 유닛 소환 위치 검증
            return spawnValidator.CanSpawnUnit(cardData, gridPosition);
        }

        /// <summary>
        /// Phase 3.16: 데미지 효과 드롭 유효성 검사
        /// </summary>
        private bool ValidateDamageEffect(CardData cardData, Game.Card.Effects.EffectData effect, Vector2Int gridPosition)
        {
            // 데미지 대상 검증 (적군 대상인지, 범위 내에 적이 있는지 등)
            if (effect.AffectedType == Game.Card.Effects.AffectedType.Enemy)
            {
                // 적군이 있는 위치이거나 범위 내에 적이 있어야 함
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }
            else if (effect.AffectedType == Game.Card.Effects.AffectedType.Any)
            {
                // 모든 유닛 대상이므로 유닛이 있는 곳이면 OK
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }

            return true; // 기타 경우는 배치 가능
        }

        /// <summary>
        /// Phase 3.16: 회복 효과 드롭 유효성 검사
        /// </summary>
        private bool ValidateHealEffect(CardData cardData, Game.Card.Effects.EffectData effect, Vector2Int gridPosition)
        {
            // 회복 대상 검증 (아군 대상인지 확인)
            if (effect.AffectedType == Game.Card.Effects.AffectedType.Ally)
            {
                // 아군이 있는 위치이거나 범위 내에 아군이 있어야 함
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }
            else if (effect.AffectedType == Game.Card.Effects.AffectedType.Any)
            {
                // 모든 유닛 대상이므로 유닛이 있는 곳이면 OK
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }

            return true; // 기타 경우는 배치 가능
        }

        /// <summary>
        /// Phase 3.16: 레거시 시스템 카드 드롭 유효성 검사 (폴백)
        /// </summary>
        private bool ValidateLegacyCardDrop(CardData cardData, Vector2Int gridPosition)
        {
            // 효과가 없는 카드는 배치 불가
            if (!cardData.IsEffectBasedCard)
            {
                return false;
            }

            // 효과 타입에 따라 기본적인 검증 수행
            if (cardData.HasEffectType(Game.Card.Effects.EffectType.Summon))
            {
                return spawnValidator.CanSpawnUnit(cardData, gridPosition);
            }
            else if (cardData.HasEffectType(Game.Card.Effects.EffectType.Damage) ||
                     cardData.HasEffectType(Game.Card.Effects.EffectType.Heal))
            {
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }

            return false;
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