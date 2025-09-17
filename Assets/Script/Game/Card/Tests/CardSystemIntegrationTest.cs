using UnityEngine;
using System.Collections.Generic;

public class CardSystemIntegrationTest : MonoBehaviour
{
    [Header("Integration Test Settings")]
    [SerializeField] private bool runTestsOnStart = true;
    [SerializeField] private GameObject testUnitPrefab;
    
    private List<ICard> testCards = new List<ICard>();
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            RunIntegrationTests();
        }
    }
    
    public void RunIntegrationTests()
    {
        Debug.Log("=== Card System Integration Test 시작 ===");
        
        TestPopupManagerIntegration();
        TestCardSystemWithAudio();
        TestCardSystemWithUI();
        TestMultipleCardsInteraction();
        
        Debug.Log("=== Card System Integration Test 완료 ===");
    }
    
    private void TestPopupManagerIntegration()
    {
        Debug.Log("--- PopupManager 통합 테스트 ---");
        
        if (PopupManager.Instance != null)
        {
            Debug.Log("✓ PopupManager 접근 성공");
            PopupManager.Instance.PopupMessage("카드 시스템 테스트 메시지");
            PopupManager.Instance.PopupWarning("카드 시스템 경고 메시지");
        }
        else
        {
            Debug.LogError("✗ PopupManager 접근 실패");
        }
    }
    
    private void TestCardSystemWithAudio()
    {
        Debug.Log("--- 오디오 시스템 통합 테스트 ---");
        
        var audioContainer = FindObjectOfType<AudioServiceContainer>();
        if (audioContainer != null)
        {
            Debug.Log("✓ AudioServiceContainer 발견");
            
            GameObject cardObject = new GameObject("TestCard");
            UnitCard unitCard = cardObject.AddComponent<UnitCard>();
            
            UnitCard.OnUnitSummoned += (unit, gameObject) =>
            {
                Debug.Log("유닛 소환 시 오디오 재생 가능");
            };
            
            Destroy(cardObject);
        }
        else
        {
            Debug.LogWarning("⚠ AudioServiceContainer를 찾을 수 없음");
        }
    }
    
    private void TestCardSystemWithUI()
    {
        Debug.Log("--- UI 시스템 통합 테스트 ---");
        
        var uiPanelManager = FindObjectOfType<UIPanelManager>();
        if (uiPanelManager != null)
        {
            Debug.Log("✓ UIPanelManager 발견");
            
            GameObject spellObject = new GameObject("TestSpell");
            Spell spellCard = spellObject.AddComponent<Spell>();
            
            Spell.OnSpellCast += (spell, position) =>
            {
                Debug.Log("스펠 시전 시 UI 업데이트 가능");
            };
            
            Destroy(spellObject);
        }
        else
        {
            Debug.LogWarning("⚠ UIPanelManager를 찾을 수 없음");
        }
    }
    
    private void TestMultipleCardsInteraction()
    {
        Debug.Log("--- 다중 카드 상호작용 테스트 ---");
        
        CreateTestCards();
        
        Debug.Log($"생성된 테스트 카드 수: {testCards.Count}");
        
        foreach (var card in testCards)
        {
            Debug.Log($"카드: {card.CardName}, 타입: {card.CardType}, 마나: {card.ManaCost}");
            
            if (card.CanUse())
            {
                card.Use();
            }
            else
            {
                Debug.Log($"카드 {card.CardName} 사용 불가");
            }
        }
        
        CleanupTestCards();
    }
    
    private void CreateTestCards()
    {
        GameObject unitCardObject = new GameObject("TestUnitCard");
        UnitCard unitCard = unitCardObject.AddComponent<UnitCard>();
        testCards.Add(unitCard);
        
        GameObject spellCardObject = new GameObject("TestSpellCard");
        Spell spellCard = spellCardObject.AddComponent<Spell>();
        testCards.Add(spellCard);
        
        Debug.Log("테스트 카드들 생성 완료");
    }
    
    private void CleanupTestCards()
    {
        foreach (var card in testCards)
        {
            if (card != null && card is MonoBehaviour cardMono)
            {
                Destroy(cardMono.gameObject);
            }
        }
        
        testCards.Clear();
        Debug.Log("테스트 카드들 정리 완료");
    }
    
    public void TestCardWithExistingGameSystems()
    {
        Debug.Log("--- 기존 게임 시스템과의 통합 테스트 ---");
        
        var gridManager = FindObjectOfType<GridManager>();
        var turnManager = FindObjectOfType<TurnManager>();
        var unitController = FindObjectOfType<UnitController>();
        
        if (gridManager != null)
        {
            Debug.Log("✓ GridManager 연동 가능");
        }
        
        if (turnManager != null)
        {
            Debug.Log("✓ TurnManager 연동 가능");
        }
        
        if (unitController != null)
        {
            Debug.Log("✓ UnitController 연동 가능");
        }
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(430, 10, 250, 200));
        GUILayout.Label("=== Card Integration Test ===");
        
        if (GUILayout.Button("Run Integration Tests"))
        {
            RunIntegrationTests();
        }
        
        if (GUILayout.Button("Test Game Systems"))
        {
            TestCardWithExistingGameSystems();
        }
        
        GUILayout.Space(10);
        GUILayout.Label($"Test Cards: {testCards.Count}");
        
        if (GUILayout.Button("Create Test Cards"))
        {
            CreateTestCards();
        }
        
        if (GUILayout.Button("Cleanup Test Cards"))
        {
            CleanupTestCards();
        }
        
        GUILayout.EndArea();
    }
}