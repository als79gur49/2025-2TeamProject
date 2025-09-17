using UnityEngine;

public class CardUnitTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private GameObject testUnitPrefab;
    [SerializeField] private bool runTestsOnStart = true;
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            RunAllTests();
        }
    }
    
    public void RunAllTests()
    {
        Debug.Log("=== Card Unit Test 시작 ===");
        
        TestUnitCardCreation();
        TestUnitSummoning();
        TestUnitController();
        
        Debug.Log("=== Card Unit Test 완료 ===");
    }
    
    private void TestUnitCardCreation()
    {
        Debug.Log("--- Unit Card 생성 테스트 ---");
        
        GameObject cardObject = new GameObject("TestUnitCard");
        UnitCard unitCard = cardObject.AddComponent<UnitCard>();
        
        if (unitCard != null)
        {
            Debug.Log("✓ Unit Card 생성 성공");
            
            if (unitCard.CardType == CardType.Unit)
            {
                Debug.Log("✓ Card Type 설정 성공");
            }
            else
            {
                Debug.LogError("✗ Card Type 설정 실패");
            }
        }
        else
        {
            Debug.LogError("✗ Unit Card 생성 실패");
        }
        
        Destroy(cardObject);
    }
    
    private void TestUnitSummoning()
    {
        Debug.Log("--- Unit 소환 테스트 ---");
        
        if (testUnitPrefab == null)
        {
            Debug.LogWarning("⚠ Test Unit Prefab이 설정되지 않음");
            return;
        }
        
        GameObject cardObject = new GameObject("TestUnitCard");
        UnitCard unitCard = cardObject.AddComponent<UnitCard>();
        
        unitCard.GetComponent<UnitCard>().Use();
        
        Debug.Log("✓ Unit 소환 테스트 완료");
        
        Destroy(cardObject);
    }
    
    private void TestUnitController()
    {
        Debug.Log("--- CardUnitController 테스트 ---");
        
        GameObject unitObject = new GameObject("TestUnit");
        CardUnitController unitController = unitObject.AddComponent<CardUnitController>();
        
        UnitStats testStats = new UnitStats(100, 25, 3, 2);
        unitController.Initialize(testStats, "테스트 유닛", UnitRarity.Common);
        
        if (unitController.MaxHP == 100 && unitController.Attack == 25)
        {
            Debug.Log("✓ Unit 초기화 성공");
        }
        else
        {
            Debug.LogError("✗ Unit 초기화 실패");
        }
        
        bool damageResult = unitController.TakeDamage(30);
        if (damageResult && unitController.CurrentHP == 70)
        {
            Debug.Log("✓ 데미지 시스템 동작 성공");
        }
        else
        {
            Debug.LogError("✗ 데미지 시스템 동작 실패");
        }
        
        bool healResult = unitController.Heal(20);
        if (healResult && unitController.CurrentHP == 90)
        {
            Debug.Log("✓ 힐 시스템 동작 성공");
        }
        else
        {
            Debug.LogError("✗ 힐 시스템 동작 실패");
        }
        
        unitController.PrintStatus();
        
        Destroy(unitObject);
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        GUILayout.Label("=== Card Unit Test ===");
        
        if (GUILayout.Button("Run Unit Tests"))
        {
            RunAllTests();
        }
        
        GUILayout.EndArea();
    }
}