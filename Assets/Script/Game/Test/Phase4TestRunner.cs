using UnityEngine;
using Game.Services;

namespace Game.Test
{
    /// <summary>
    /// Phase 4 테스트 실행 스크립트 - ValidationScript를 실행하고 결과를 확인합니다.
    /// </summary>
    public class Phase4TestRunner : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        
        private Phase4ValidationScript validationScript;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                RunPhase4Validation();
            }
        }
        
        /// <summary>
        /// Phase 4 검증 테스트 실행
        /// </summary>
        [ContextMenu("Run Phase 4 Validation")]
        public void RunPhase4Validation()
        {
            Debug.Log("🎯 [Phase4TestRunner] Starting Phase 4 validation tests...");
            
            // ValidationScript 컴포넌트 가져오기 또는 생성
            validationScript = GetComponent<Phase4ValidationScript>();
            if (validationScript == null)
            {
                validationScript = gameObject.AddComponent<Phase4ValidationScript>();
            }
            
            // 약간의 지연 후 검증 실행 (다른 컴포넌트들의 초기화를 위해)
            Invoke(nameof(ExecuteValidation), 0.5f);
        }
        
        private void ExecuteValidation()
        {
            if (validationScript != null)
            {
                validationScript.ValidateImplementation();
                
                // 결과 확인
                bool isValid = validationScript.GetValidationStatus();
                string report = validationScript.GetValidationReport();
                
                Debug.Log($"🔍 [Phase4TestRunner] Validation completed. Status: {(isValid ? "PASS" : "FAIL")}");
                Debug.Log($"📋 [Phase4TestRunner] Detailed Report:\n{report}");
                
                // 성공/실패에 따른 추가 처리
                if (isValid)
                {
                    OnValidationSuccess();
                }
                else
                {
                    OnValidationFailure();
                }
            }
        }
        
        private void OnValidationSuccess()
        {
            Debug.Log("✅ [Phase4TestRunner] All Phase 4 requirements validated successfully!");
            Debug.Log("🎉 [Phase4TestRunner] Phase 4 implementation is complete and ready for integration.");
        }
        
        private void OnValidationFailure()
        {
            Debug.LogWarning("⚠️ [Phase4TestRunner] Some Phase 4 requirements are not fully implemented.");
            Debug.LogWarning("🔧 [Phase4TestRunner] Please check the validation report and implement missing features.");
        }
        
        /// <summary>
        /// 카드 드래그 앤 드롭 워크플로우 테스트
        /// </summary>
        [ContextMenu("Test Card Workflow")]
        public void TestCardWorkflow()
        {
            Debug.Log("🃏 [Phase4TestRunner] Testing complete card workflow...");
            
            // TODO: 실제 카드 드로우부터 UnitService 등록까지의 워크플로우 테스트 구현
            Debug.Log("📝 [Phase4TestRunner] Card workflow test implementation pending.");
        }
        
        /// <summary>
        /// 주문 카드 로직 테스트
        /// </summary>
        [ContextMenu("Test Spell Cards")]
        public void TestSpellCards()
        {
            Debug.Log("🔮 [Phase4TestRunner] Testing spell card functionality...");
            
            // TODO: 주문 카드 발동 및 효과 테스트 구현
            Debug.Log("📝 [Phase4TestRunner] Spell card test implementation pending.");
        }
        
        private void Update()
        {
            // 키보드 단축키로 테스트 실행
            if (Input.GetKeyDown(KeyCode.F4))
            {
                RunPhase4Validation();
            }
            
            if (Input.GetKeyDown(KeyCode.F5))
            {
                TestCardWorkflow();
            }
            
            if (Input.GetKeyDown(KeyCode.F6))
            {
                TestSpellCards();
            }
        }
    }
}