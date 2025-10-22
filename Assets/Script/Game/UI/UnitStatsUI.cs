using UnityEngine;
using TMPro;
using Game.Components;
using Game.Interfaces;

namespace Game.UI
{
    /// <summary>
    /// Billboard 모드 - UI가 카메라를 바라보는 방식
    /// </summary>
    public enum BillboardMode
    {
        CameraRotation,      // 카메라 회전 복사 (기본, 가장 안정적)
        LookAtCamera,        // 카메라 위치를 직접 바라봄
        LookAtCameraYAxisOnly // Y축만 회전 (수평만)
    }

    /// <summary>
    /// 유닛의 스탯(체력, 공격력, 이동력)을 UI로 표시하는 컴포넌트
    /// 이벤트 구독 패턴을 사용하여 자동으로 UI 업데이트
    /// </summary>
    public class UnitStatsUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI movementText;

        [Header("Display Options")]
        [SerializeField] private bool showHealth = true;
        [SerializeField] private bool showAttack = true;
        [SerializeField] private bool showMovement = true;

        [Header("Text Formatting")]
        [SerializeField] private string healthFormat = "{0}";
        [SerializeField] private string attackFormat = "{0}";
        [SerializeField] private string movementFormat = "{0}";

        [Header("Static Colors")]
        [SerializeField] private Color healthColor = Color.green;
        [SerializeField] private Color attackColor = Color.red;
        [SerializeField] private Color movementColor = Color.magenta;

        [Header("Billboard Settings")]
        [SerializeField] private bool enableBillboard = true;
        [SerializeField] private BillboardMode billboardMode = BillboardMode.CameraRotation;
        [SerializeField] private bool smoothRotation = false;
        [SerializeField, Range(1f, 20f)] private float rotationSpeed = 10f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Component references
        private IHealthComponent healthComponent;
        private ICombatSystem combatComponent;
        private IMovementSystem movementComponent;

        // Camera reference for billboard
        private Camera mainCamera;

        private void Awake()
        {
            // Get component references from parent Unit
            InitializeComponentReferences();
        }

        private void Start()
        {
            // Cache camera reference for billboard
            mainCamera = Camera.main;
            if (mainCamera == null && enableBillboard)
            {
                Debug.LogWarning($"[UnitStatsUI] Main camera not found for billboard on {gameObject.name}");
            }

            // Subscribe to component events
            SubscribeToEvents();

            // Initial UI update
            UpdateAllUI();
        }

        private void LateUpdate()
        {
            // Update billboard after all other updates
            UpdateBillboard();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            UnsubscribeFromEvents();
        }

        #region Initialization

        /// <summary>
        /// 부모 GameObject에서 컴포넌트 참조 가져오기
        /// </summary>
        private void InitializeComponentReferences()
        {
            healthComponent = GetComponentInParent<IHealthComponent>();
            combatComponent = GetComponentInParent<ICombatSystem>();
            movementComponent = GetComponentInParent<IMovementSystem>();

            if (healthComponent == null && showDebugInfo)
                Debug.LogWarning($"[UnitStatsUI] HealthComponent not found on {gameObject.name}");

            if (combatComponent == null && showDebugInfo)
                Debug.LogWarning($"[UnitStatsUI] CombatComponent not found on {gameObject.name}");

            if (movementComponent == null && showDebugInfo)
                Debug.LogWarning($"[UnitStatsUI] MovementComponent not found on {gameObject.name}");
        }

        #endregion

        #region Event Subscription

        /// <summary>
        /// 컴포넌트 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            // HealthComponent 이벤트 구독
            if (healthComponent != null && showHealth)
            {
                healthComponent.OnHealthChanged += OnHealthChanged;
                healthComponent.OnDamageTaken += OnDamageTaken;
                healthComponent.OnHealed += OnHealed;
                healthComponent.OnDeath += OnUnitDeath;

                if (showDebugInfo)
                    Debug.Log($"[UnitStatsUI] Subscribed to HealthComponent events for {gameObject.name}");
            }

            // CombatComponent 이벤트 구독
            if (combatComponent != null && showAttack)
            {
                combatComponent.OnAttackPowerChanged += OnAttackPowerChanged;

                if (showDebugInfo)
                    Debug.Log($"[UnitStatsUI] Subscribed to CombatComponent events for {gameObject.name}");
            }

            // MovementComponent 이벤트 구독
            if (movementComponent != null && showMovement)
            {
                movementComponent.OnMovementPointsChanged += OnMovementPointsChanged;
                movementComponent.OnMovementRefreshed += OnMovementRefreshed;

                if (showDebugInfo)
                    Debug.Log($"[UnitStatsUI] Subscribed to MovementComponent events for {gameObject.name}");
            }
        }

        /// <summary>
        /// 컴포넌트 이벤트 구독 해제 (메모리 누수 방지)
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            // HealthComponent 이벤트 구독 해제
            if (healthComponent != null)
            {
                healthComponent.OnHealthChanged -= OnHealthChanged;
                healthComponent.OnDamageTaken -= OnDamageTaken;
                healthComponent.OnHealed -= OnHealed;
                healthComponent.OnDeath -= OnUnitDeath;
            }

            // CombatComponent 이벤트 구독 해제
            if (combatComponent != null)
            {
                combatComponent.OnAttackPowerChanged -= OnAttackPowerChanged;
            }

            // MovementComponent 이벤트 구독 해제
            if (movementComponent != null)
            {
                movementComponent.OnMovementPointsChanged -= OnMovementPointsChanged;
                movementComponent.OnMovementRefreshed -= OnMovementRefreshed;
            }

            if (showDebugInfo)
                Debug.Log($"[UnitStatsUI] Unsubscribed from all events for {gameObject.name}");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 체력 변경 이벤트 핸들러
        /// </summary>
        private void OnHealthChanged(int newHealth)
        {
            UpdateHealthUI();

            if (showDebugInfo)
                Debug.Log($"[UnitStatsUI] Health changed to {newHealth}");
        }

        /// <summary>
        /// 피해 받음 이벤트 핸들러
        /// </summary>
        private void OnDamageTaken(int damage, int currentHealth)
        {
            UpdateHealthUI();

            if (showDebugInfo)
                Debug.Log($"[UnitStatsUI] Took {damage} damage, current health: {currentHealth}");
        }

        /// <summary>
        /// 회복 이벤트 핸들러
        /// </summary>
        private void OnHealed(int healAmount, int currentHealth)
        {
            UpdateHealthUI();

            if (showDebugInfo)
                Debug.Log($"[UnitStatsUI] Healed {healAmount}, current health: {currentHealth}");
        }

        /// <summary>
        /// 공격력 변경 이벤트 핸들러
        /// </summary>
        private void OnAttackPowerChanged(int newAttackPower)
        {
            UpdateAttackUI();

            if (showDebugInfo)
                Debug.Log($"[UnitStatsUI] Attack power changed to {newAttackPower}");
        }

        /// <summary>
        /// 이동력 변경 이벤트 핸들러
        /// </summary>
        private void OnMovementPointsChanged(int newMovementPoints)
        {
            UpdateMovementUI();

            if (showDebugInfo)
                Debug.Log($"[UnitStatsUI] Movement points changed to {newMovementPoints}");
        }

        /// <summary>
        /// 이동력 갱신 이벤트 핸들러 (턴 시작 등)
        /// </summary>
        private void OnMovementRefreshed()
        {
            UpdateMovementUI();

            if (showDebugInfo)
                Debug.Log($"[UnitStatsUI] Movement refreshed");
        }

        /// <summary>
        /// 유닛 사망 이벤트 핸들러 - UI 즉시 비활성화
        /// </summary>
        private void OnUnitDeath()
        {
            if (showDebugInfo)
                Debug.Log($"[UnitStatsUI] Unit died, hiding UI for {gameObject.name}");

            // 사망 애니메이션이 깔끔하게 보이도록 UI 즉시 비활성화
            gameObject.SetActive(false);
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// 모든 UI 업데이트
        /// </summary>
        public void UpdateAllUI()
        {
            UpdateHealthUI();
            UpdateAttackUI();
            UpdateMovementUI();
        }

        /// <summary>
        /// 체력 UI 업데이트
        /// </summary>
        private void UpdateHealthUI()
        {
            if (!showHealth || healthText == null || healthComponent == null)
                return;

            int currentHealth = healthComponent.CurrentHealth;

            // 텍스트 업데이트
            healthText.text = string.Format(healthFormat, currentHealth);

            // 고정 색상 적용
            healthText.color = healthColor;
        }

        /// <summary>
        /// 공격력 UI 업데이트
        /// </summary>
        private void UpdateAttackUI()
        {
            if (!showAttack || attackText == null || combatComponent == null)
                return;

            int attackPower = combatComponent.CurrentAttackPower;
            attackText.text = string.Format(attackFormat, attackPower);

            // 고정 색상 적용
            attackText.color = attackColor;
        }

        /// <summary>
        /// 이동력 UI 업데이트
        /// </summary>
        private void UpdateMovementUI()
        {
            if (!showMovement || movementText == null || movementComponent == null)
                return;

            int currentMovement = movementComponent.CurrentMovementPoints;

            // 텍스트 업데이트
            movementText.text = string.Format(movementFormat, currentMovement);

            // 고정 색상 적용
            movementText.color = movementColor;
        }

        #endregion

        #region Billboard

        /// <summary>
        /// 빌보드 업데이트 - UI가 카메라를 바라보도록 회전
        /// </summary>
        private void UpdateBillboard()
        {
            if (!enableBillboard || mainCamera == null) return;

            Quaternion targetRotation = CalculateBillboardRotation();

            if (smoothRotation)
            {
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed
                );
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }

        /// <summary>
        /// Billboard 모드에 따라 목표 회전 계산
        /// </summary>
        private Quaternion CalculateBillboardRotation()
        {
            switch (billboardMode)
            {
                case BillboardMode.CameraRotation:
                    // 카메라와 같은 방향으로 회전 (가장 안정적)
                    return mainCamera.transform.rotation;

                case BillboardMode.LookAtCamera:
                    // 카메라 위치를 직접 바라봄
                    Vector3 directionToCamera = mainCamera.transform.position - transform.position;
                    if (directionToCamera != Vector3.zero)
                        return Quaternion.LookRotation(-directionToCamera);
                    break;

                case BillboardMode.LookAtCameraYAxisOnly:
                    // Y축만 회전 (수평 회전만)
                    Vector3 direction = mainCamera.transform.position - transform.position;
                    direction.y = 0; // Y축 고정
                    if (direction != Vector3.zero)
                        return Quaternion.LookRotation(-direction);
                    break;
            }

            return transform.rotation;
        }

        #endregion

        #region Public API

        /// <summary>
        /// UI 표시 옵션 설정
        /// </summary>
        public void SetDisplayOptions(bool health, bool attack, bool movement)
        {
            showHealth = health;
            showAttack = attack;
            showMovement = movement;

            UpdateAllUI();
        }

        /// <summary>
        /// 각 스탯의 색상 설정
        /// </summary>
        public void SetStatColors(Color health, Color attack, Color movement)
        {
            healthColor = health;
            attackColor = attack;
            movementColor = movement;

            UpdateAllUI();
        }

        /// <summary>
        /// 빌보드 기능 활성화/비활성화
        /// </summary>
        public void SetBillboardEnabled(bool enabled)
        {
            enableBillboard = enabled;
        }

        /// <summary>
        /// 빌보드 모드 변경
        /// </summary>
        public void SetBillboardMode(BillboardMode mode)
        {
            billboardMode = mode;
        }

        /// <summary>
        /// 부드러운 회전 설정
        /// </summary>
        public void SetSmoothRotation(bool smooth, float speed = 10f)
        {
            smoothRotation = smooth;
            rotationSpeed = Mathf.Clamp(speed, 1f, 20f);
        }

        #endregion

        #region Editor Debugging

#if UNITY_EDITOR
        [ContextMenu("Force Update All UI")]
        private void ForceUpdateAllUI()
        {
            InitializeComponentReferences();
            UpdateAllUI();
            Debug.Log($"[UnitStatsUI] Force updated all UI for {gameObject.name}");
        }

        [ContextMenu("Log Component Status")]
        private void LogComponentStatus()
        {
            Debug.Log($"=== UnitStatsUI Status for {gameObject.name} ===");
            Debug.Log($"HealthComponent: {(healthComponent != null ? "Found" : "Missing")}");
            Debug.Log($"CombatComponent: {(combatComponent != null ? "Found" : "Missing")}");
            Debug.Log($"MovementComponent: {(movementComponent != null ? "Found" : "Missing")}");

            if (healthComponent != null)
                Debug.Log($"Current Health: {healthComponent.CurrentHealth}/{healthComponent.MaxHealth}");

            if (combatComponent != null)
                Debug.Log($"Current Attack: {combatComponent.CurrentAttackPower}");

            if (movementComponent != null)
                Debug.Log($"Current Movement: {movementComponent.CurrentMovementPoints}/{movementComponent.MaxMovementPoints}");
        }

        [ContextMenu("Test Event Subscription")]
        private void TestEventSubscription()
        {
            InitializeComponentReferences();
            UnsubscribeFromEvents();
            SubscribeToEvents();
            Debug.Log($"[UnitStatsUI] Re-subscribed to events for {gameObject.name}");
        }
#endif

        #endregion
    }
}
