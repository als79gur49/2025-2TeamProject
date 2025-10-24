using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using static Game.Interfaces.ITeamComponent;
using System.Collections;

namespace Game.Card.UI
{
    /// <summary>
    /// 타일 드롭 핸들러 - 타일 위에서의 드롭 이벤트를 처리하고 시각적 피드백을 제공
    /// Phase 3: UI 및 상호작용 구현의 핵심 컴포넌트
    /// </summary>
    public class TileDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("타일 정보")]
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private bool isInteractable = true;

        [Header("드롭 피드백")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = Color.yellow;
        [SerializeField] private Color validDropColor = Color.green;
        [SerializeField] private Color invalidDropColor = Color.red;
        [SerializeField] private float feedbackAlpha = 0.7f;

        [Header("시각적 효과")]
        [SerializeField] private GameObject dropPreviewPrefab;
        [SerializeField] private ParticleSystem dropEffect;
        [SerializeField] private AudioClip dropSuccessSound;
        [SerializeField] private AudioClip dropFailSound;

        // 상태 관리
        private bool isHighlighted = false;
        private bool isValidDrop = false;
        private GameObject currentPreview;
        
        // 서비스 참조
        private ICardSpawnService cardSpawnService;
        private ISpawnValidator spawnValidator;
        private IGridManager gridManager;
        private IGridRenderer gridRenderer;
        private AudioSource audioSource;

        #region Unity Lifecycle

        private void Awake()
        {
            // 컴포넌트 참조 설정 - 자식 오브젝트에서 Renderer 찾기
            if (tileRenderer == null)
                tileRenderer = GetComponentInChildren<Renderer>();

            if (tileRenderer == null)
                Debug.LogWarning($"[TileDropHandler] Renderer not found in children of {gameObject.name}");

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            // 서비스 의존성 주입
            InjectDependencies();
            
            // 그리드 위치 자동 감지 (설정되지 않은 경우)
            if (gridPosition == Vector2Int.zero)
            {
                AutoDetectGridPosition();
            }

            // 초기 색상 설정
            SetTileColor(normalColor);
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
                cardSpawnService = cardServiceManager.GetCardSpawnService();
                spawnValidator = cardServiceManager.GetSpawnValidator();

                gridManager = ServiceLocator.Get<IGridManager>();

                if (gridManager != null)
                {
                    gridRenderer = gridManager.GetGridRenderer();
                }

                if (cardSpawnService == null)
                    Debug.LogError("[TileDropHandler] CardSpawnService not found in ServiceLocator");

                if (spawnValidator == null)
                    Debug.LogError("[TileDropHandler] SpawnValidator not found in ServiceLocator");

                if (gridManager == null)
                    Debug.LogError("[TileDropHandler] GridManager not found in ServiceLocator");

                if (gridRenderer == null)
                    Debug.LogError("[TileDropHandler] GridRenderer not found in GridManager");
            }
        }

        #endregion

        #region 그리드 위치 관리

        /// <summary>
        /// 그리드 위치 설정
        /// </summary>
        public void SetGridPosition(Vector2Int position)
        {
            gridPosition = position;
            gameObject.name = $"Tile_{position.x}_{position.y}";
        }

        /// <summary>
        /// 그리드 위치 반환
        /// </summary>
        public Vector2Int GetGridPosition()
        {
            return gridPosition;
        }

        /// <summary>
        /// 타일 이름이나 위치를 기반으로 그리드 위치 자동 감지
        /// </summary>
        private void AutoDetectGridPosition()
        {
            // Tile 컴포넌트가 있는 경우
            var tile = GetComponent<Tile>();
            if (tile != null)
            {
                gridPosition = tile.GetGridPosition();
                return;
            }

            // 게임 오브젝트 이름에서 추출 시도 (예: "Tile_3_5")
            string objName = gameObject.name;
            if (objName.StartsWith("Tile_"))
            {
                string[] parts = objName.Split('_');
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                    {
                        gridPosition = new Vector2Int(x, y);
                        return;
                    }
                }
            }

            Debug.LogWarning($"[TileDropHandler] Could not auto-detect grid position for {gameObject.name}");
        }

        #endregion

        #region 드롭 이벤트 처리

        /// <summary>
        /// 카드 드롭 처리
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (!isInteractable) return;

            // 드래그된 오브젝트에서 CardUI 컴포넌트 찾기
            var cardUI = eventData.pointerDrag?.GetComponent<CardUI>();
            if (cardUI != null)
            {
                var cardData = cardUI.GetCardData();
                if (cardData != null)
                {
                    HandleCardDrop(cardData, cardUI);
                }
            }

            // 하이라이트 해제
            SetHighlight(false);
        }

        /// <summary>
        /// 카드 드롭 처리 (CardUI에서 직접 호출) - EffectData 기반 처리
        /// ✅ 시각적 프리뷰 먼저 표시 → 실행 → 짧은 효과 후 정리
        /// </summary>
        public bool HandleCardDrop(CardData cardData, CardUI cardUI)
        {
            if (!isInteractable || cardData == null) return false;

            Debug.Log($"[TileDropHandler] Attempting to drop {cardData.CardName} at position {gridPosition}");

            if (!cardData.IsEffectBasedCard)
            {
                Debug.LogWarning($"[TileDropHandler] Card {cardData.CardName} has no effects");
                PlayDropFailedFeedback();
                return false;
            }

            // ✅ 1. 프리뷰 표시 (최종 확인용)
            ShowCardPreview(cardData);

            // 2. 실제 카드 실행 (기존 방식 유지)
            bool success = false;

            // 효과 타입에 따른 처리 분기
            if (cardData.HasEffectType(Game.Card.Effects.EffectType.Summon))
            {
                success = HandleUnitCardDrop(cardData, cardUI);
            }
            else if (cardData.HasEffectType(Game.Card.Effects.EffectType.Damage) ||
                     cardData.HasEffectType(Game.Card.Effects.EffectType.Heal))
            {
                success = HandleSpellCardDrop(cardData, cardUI);
            }
            else
            {
                Debug.LogWarning($"[TileDropHandler] Unsupported effect types in card {cardData.CardName}");
                PlayDropFailedFeedback();
                ClearCardPreview(); // 실패 시 즉시 정리
                return false;
            }

            // ✅ 3. 짧은 시각 효과 후 프리뷰 정리
            if (success)
            {
                StartCoroutine(ClearPreviewAfterDelay(0.2f));
            }
            else
            {
                ClearCardPreview(); // 실패 시 즉시 정리
            }

            return success;
        }

        /// <summary>
        /// Unit 카드 드롭 처리
        /// </summary>
        private bool HandleUnitCardDrop(CardData cardData, CardUI cardUI)
        {
            // 유닛 소환 유효성 검사
            if (spawnValidator == null || !spawnValidator.CanSpawnUnit(cardData, gridPosition))
            {
                Debug.Log($"[TileDropHandler] Invalid spawn position for unit {cardData.CardName} at {gridPosition}");
                PlayDropFailedFeedback();
                return false;
            }

            // 카드 소환 서비스 확인
            if (cardSpawnService == null)
            {
                Debug.LogError("[TileDropHandler] CardSpawnService not available");
                PlayDropFailedFeedback();
                return false;
            }

            // 유닛 소환 시도 (TryExecuteCard 사용)
            bool spawnSuccess = cardSpawnService.TryExecuteCard(cardData, gridPosition, TeamType.Player);

            if (spawnSuccess)
            {
                Debug.Log($"[TileDropHandler] Successfully spawned unit {cardData.CardName} at {gridPosition}");
                PlayDropSuccessFeedback();
                cardUI?.OnCardUsed();
                return true;
            }
            else
            {
                Debug.Log($"[TileDropHandler] Failed to spawn unit {cardData.CardName} at {gridPosition}");
                PlayDropFailedFeedback();
                return false;
            }
        }

        /// <summary>
        /// Spell 카드 드롭 처리
        /// </summary>
        private bool HandleSpellCardDrop(CardData cardData, CardUI cardUI)
        {
            // 주문 사용 유효성 검사
            if (spawnValidator == null || !spawnValidator.CanUseSpell(cardData, gridPosition))
            {
                Debug.Log($"[TileDropHandler] Invalid target position for spell {cardData.CardName} at {gridPosition}");
                PlayDropFailedFeedback();
                return false;
            }

            // 카드 소환 서비스 확인
            if (cardSpawnService == null)
            {
                Debug.LogError("[TileDropHandler] CardSpawnService not available");
                PlayDropFailedFeedback();
                return false;
            }

            // 주문 발동 시도 (TryExecuteCard 사용)
            bool spellSuccess = cardSpawnService.TryExecuteCard(cardData, gridPosition, TeamType.Player);

            if (spellSuccess)
            {
                Debug.Log($"[TileDropHandler] Successfully activated spell {cardData.CardName} at {gridPosition}");
                PlayDropSuccessFeedback();
                cardUI?.OnCardUsed();
                return true;
            }
            else
            {
                Debug.Log($"[TileDropHandler] Failed to activate spell {cardData.CardName} at {gridPosition}");
                PlayDropFailedFeedback();
                return false;
            }
        }

        #endregion

        #region 마우스 이벤트 처리

        /// <summary>
        /// 마우스 포인터 진입
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;

            // 드래그 중인 카드가 있는지 확인
            var cardUI = eventData.pointerDrag?.GetComponent<CardUI>();
            if (cardUI != null && cardUI.IsDragging)
            {
                var cardData = cardUI.GetCardData();
                if (cardData != null)
                {
                    // ✅ 프리뷰 표시 추가 (SpellEffectExecutor 범위 계산 로직 사용)
                    ShowCardPreview(cardData);

                    // 카드 타입에 따른 유효성 검사
                    isValidDrop = ValidateCardDrop(cardData);
                    SetHighlight(true);
                    ShowDropPreview(cardData);
                }
            }
        }

        /// <summary>
        /// 마우스 포인터 이탈
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable) return;

            // ✅ 프리뷰 정리 추가
            ClearCardPreview();

            SetHighlight(false);
            HideDropPreview();
        }

        #endregion

        #region 카드 드롭 유효성 검사

        /// <summary>
        /// 카드 드롭 유효성 검사 (효과 타입에 따른 분기 처리)
        /// </summary>
        private bool ValidateCardDrop(CardData cardData)
        {
            if (spawnValidator == null) return false;

            if (!cardData.IsEffectBasedCard)
            {
                return false;
            }

            // 효과 타입에 따른 검증
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

        #region 시각적 피드백

        /// <summary>
        /// 타일 하이라이트 설정
        /// </summary>
        private void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;

            if (!highlight)
            {
                SetTileColor(normalColor);
                return;
            }

            // 하이라이트 색상 결정
            Color targetColor = isValidDrop ? validDropColor : invalidDropColor;
            SetTileColor(targetColor);
        }

        /// <summary>
        /// 타일 색상 설정
        /// </summary>
        private void SetTileColor(Color color)
        {
            if (tileRenderer != null && tileRenderer.material != null)
            {
                color.a = feedbackAlpha;
                tileRenderer.material.color = color;
            }
        }

        /// <summary>
        /// 드롭 프리뷰 표시
        /// </summary>
        private void ShowDropPreview(CardData cardData)
        {
            if (dropPreviewPrefab == null) return;

            // 기존 프리뷰 제거
            HideDropPreview();

            // 새 프리뷰 생성
            currentPreview = Instantiate(dropPreviewPrefab, transform.position, Quaternion.identity, transform);
            
            // 프리뷰 투명도 설정
            var previewRenderer = currentPreview.GetComponent<Renderer>();
            if (previewRenderer != null)
            {
                var material = previewRenderer.material;
                Color previewColor = isValidDrop ? validDropColor : invalidDropColor;
                previewColor.a = 0.5f;
                material.color = previewColor;
            }
        }

        /// <summary>
        /// 드롭 프리뷰 숨기기
        /// </summary>
        private void HideDropPreview()
        {
            if (currentPreview != null)
            {
                DestroyImmediate(currentPreview);
                currentPreview = null;
            }
        }

        /// <summary>
        /// 드롭 성공 피드백
        /// </summary>
        private void PlayDropSuccessFeedback()
        {
            // 파티클 효과
            if (dropEffect != null)
            {
                dropEffect.Play();
            }

            // 사운드 효과
            if (audioSource != null && dropSuccessSound != null)
            {
                audioSource.PlayOneShot(dropSuccessSound);
            }

            // 색상 변경 효과
            StartCoroutine(FlashColor(validDropColor));
        }

        /// <summary>
        /// 드롭 실패 피드백
        /// </summary>
        private void PlayDropFailedFeedback()
        {
            // 사운드 효과
            if (audioSource != null && dropFailSound != null)
            {
                audioSource.PlayOneShot(dropFailSound);
            }

            // 색상 변경 효과
            StartCoroutine(FlashColor(invalidDropColor));
        }

        /// <summary>
        /// 색상 플래시 효과
        /// </summary>
        private System.Collections.IEnumerator FlashColor(Color flashColor)
        {
            Color originalColor = (tileRenderer != null && tileRenderer.material != null)
                ? tileRenderer.material.color : Color.white;

            // 플래시 색상으로 변경
            SetTileColor(flashColor);
            yield return new WaitForSeconds(0.2f);

            // 원래 색상으로 복귀
            SetTileColor(originalColor);
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 상호작용 가능 여부 설정
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            
            if (!interactable)
            {
                SetHighlight(false);
                HideDropPreview();
            }
        }

        /// <summary>
        /// 상호작용 가능 여부 반환
        /// </summary>
        public bool IsInteractable => isInteractable;

        #endregion

        #region 카드 프리뷰 시스템

        /// <summary>
        /// 카드 프리뷰 표시
        /// SpellEffectExecutor의 실제 범위 계산 로직 사용
        /// </summary>
        private void ShowCardPreview(CardData cardData)
        {
            Debug.Log($"[TileDropHandler] Card preview shown");
            if (gridRenderer == null || gridManager == null || spawnValidator == null)
                return;

            // ✅ SpellEffectExecutor의 실제 범위 계산 로직 사용
            var (validPos, invalidPos) = CardPreviewHelper.ValidateAffectedPositions(
                cardData,
                gridPosition,
                gridManager,
                spawnValidator
            );

            // GridRenderer에 프리뷰 요청
            gridRenderer.ShowValidatedPreview(validPos, invalidPos);

            Debug.Log($"[TileDropHandler] Card preview shown: {validPos.Count} valid, {invalidPos.Count} invalid positions");
        }

        /// <summary>
        /// 프리뷰 정리
        /// </summary>
        private void ClearCardPreview()
        {
            gridRenderer?.ClearCardPreview();
        }

        /// <summary>
        /// 지연 후 프리뷰 정리 코루틴
        /// </summary>
        private System.Collections.IEnumerator ClearPreviewAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gridRenderer?.ClearCardPreview();
        }

        #endregion

        #region 에디터용 도구

#if UNITY_EDITOR
        [Header("에디터 디버깅")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            var rect = new Rect(10, Screen.height - 200, 300, 180);
            GUILayout.BeginArea(rect);
            GUILayout.Box("TileDropHandler Debug");

            GUILayout.Label($"Position: {gridPosition}");
            GUILayout.Label($"Interactable: {isInteractable}");
            GUILayout.Label($"Highlighted: {isHighlighted}");
            GUILayout.Label($"Valid Drop: {isValidDrop}");
            GUILayout.Label($"Services OK: {(cardSpawnService != null && spawnValidator != null)}");

            if (GUILayout.Button("Toggle Interactable"))
            {
                SetInteractable(!isInteractable);
            }

            if (GUILayout.Button("Test Drop Success"))
            {
                PlayDropSuccessFeedback();
            }

            if (GUILayout.Button("Test Drop Fail"))
            {
                PlayDropFailedFeedback();
            }

            GUILayout.EndArea();
        }

        [UnityEditor.MenuItem("GameObject/Game/Add TileDropHandler", false, 10)]
        private static void AddTileDropHandler()
        {
            var selected = UnityEditor.Selection.activeGameObject;
            if (selected != null)
            {
                selected.AddComponent<TileDropHandler>();
                Debug.Log($"TileDropHandler added to {selected.name}");
            }
        }
#endif

        #endregion
    }
}