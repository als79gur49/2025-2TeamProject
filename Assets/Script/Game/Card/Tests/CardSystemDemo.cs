using UnityEngine;
using System.Collections.Generic;

public class CardSystemDemo : MonoBehaviour
{
    [Header("Demo Settings")]
    [SerializeField] private bool autoRunDemo = true;
    [SerializeField] private float demoInterval = 3f;
    
    private float lastDemoTime;
    private int demoStep = 0;
    private List<GameObject> createdObjects = new List<GameObject>();
    
    private void Start()
    {
        Debug.Log("=== TCG 카드 시스템 데모 시작 ===");
        PopupManager.Instance.PopupMessage("TCG 카드 시스템이 성공적으로 로드되었습니다!");
        
        if (autoRunDemo)
        {
            lastDemoTime = Time.time;
        }
    }
    
    private void Update()
    {
        if (autoRunDemo && Time.time - lastDemoTime > demoInterval)
        {
            RunNextDemoStep();
            lastDemoTime = Time.time;
        }
    }
    
    private void RunNextDemoStep()
    {
        switch (demoStep)
        {
            case 0:
                DemoUnitCard();
                break;
            case 1:
                DemoSpellCard();
                break;
            case 2:
                DemoCardInteractions();
                break;
            case 3:
                DemoCleanup();
                demoStep = -1; // 루프
                break;
        }
        
        demoStep++;
    }
    
    public void DemoUnitCard()
    {
        Debug.Log("--- 유닛 카드 데모 ---");
        PopupManager.Instance.PopupMessage("유닛 카드를 생성합니다...");
        
        GameObject unitCardObject = new GameObject("DemoWarriorCard");
        UnitCard unitCard = unitCardObject.AddComponent<UnitCard>();
        
        // 이벤트 구독
        UnitCard.OnUnitSummoned += OnDemoUnitSummoned;
        
        // 유닛 카드 사용
        if (unitCard.CanUse())
        {
            unitCard.Use();
            PopupManager.Instance.PopupMessage($"유닛 카드 '{unitCard.CardName}' 사용!");
        }
        
        createdObjects.Add(unitCardObject);
    }
    
    public void DemoSpellCard()
    {
        Debug.Log("--- 스펠 카드 데모 ---");
        PopupManager.Instance.PopupMessage("스펠 카드를 생성합니다...");
        
        // 데미지 스펠 생성
        GameObject damageSpellObject = new GameObject("DemoFireballSpell");
        Spell damageSpell = damageSpellObject.AddComponent<Spell>();
        
        // 힐 스펠 생성
        GameObject healSpellObject = new GameObject("DemoHealSpell");
        Spell healSpell = healSpellObject.AddComponent<Spell>();
        
        // 이벤트 구독
        Spell.OnSpellCast += OnDemoSpellCast;
        
        // 스펠 사용
        if (damageSpell.CanUse())
        {
            damageSpell.Use();
            PopupManager.Instance.PopupMessage($"데미지 스펠 '{damageSpell.CardName}' 시전!");
        }
        
        if (healSpell.CanUse())
        {
            healSpell.Use();
            PopupManager.Instance.PopupMessage($"힐 스펠 '{healSpell.CardName}' 시전!");
        }
        
        createdObjects.Add(damageSpellObject);
        createdObjects.Add(healSpellObject);
    }
    
    public void DemoCardInteractions()
    {
        Debug.Log("--- 카드 상호작용 데모 ---");
        PopupManager.Instance.PopupMessage("카드 시스템 상호작용을 테스트합니다...");
        
        // 유닛 생성 및 상호작용 테스트
        GameObject unitObject = new GameObject("DemoUnit");
        CardUnitController unitController = unitObject.AddComponent<CardUnitController>();
        
        UnitStats testStats = new UnitStats(100, 30, 3, 2);
        unitController.Initialize(testStats, "데모 전사", UnitRarity.Common);
        
        // 이벤트 구독
        unitController.OnHealthChanged += OnDemoHealthChanged;
        unitController.OnUnitDied += OnDemoUnitDied;
        
        // 데미지/힐 테스트
        unitController.TakeDamage(40);
        unitController.Heal(20);
        unitController.PrintStatus();
        
        createdObjects.Add(unitObject);
    }
    
    private void DemoCleanup()
    {
        Debug.Log("--- 데모 정리 ---");
        PopupManager.Instance.PopupMessage("데모 정리 중...");
        
        // 이벤트 구독 해제
        UnitCard.OnUnitSummoned -= OnDemoUnitSummoned;
        Spell.OnSpellCast -= OnDemoSpellCast;
        
        // 생성된 오브젝트들 정리
        foreach (GameObject obj in createdObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        createdObjects.Clear();
        PopupManager.Instance.PopupMessage("데모 정리 완료!");
    }
    
    // 이벤트 핸들러들
    private void OnDemoUnitSummoned(UnitCard unit, GameObject unitObject)
    {
        Debug.Log($"데모: 유닛 '{unit.CardName}' 소환됨!");
        PopupManager.Instance.PopupMessageWithColor($"유닛 소환: {unit.CardName}", Color.green);
    }
    
    private void OnDemoSpellCast(Spell spell, Vector3 position)
    {
        Debug.Log($"데모: 스펠 '{spell.CardName}' 시전됨!");
        PopupManager.Instance.PopupMessageWithColor($"스펠 시전: {spell.CardName}", Color.blue);
    }
    
    private void OnDemoHealthChanged(CardUnitController unit, int healthChange)
    {
        string changeType = healthChange > 0 ? "회복" : "피해";
        Debug.Log($"데모: {unit.UnitName}이(가) {Mathf.Abs(healthChange)} {changeType}를 받음");
        PopupManager.Instance.PopupMessage($"{unit.UnitName}: {changeType} {Mathf.Abs(healthChange)}");
    }
    
    private void OnDemoUnitDied(CardUnitController unit)
    {
        Debug.Log($"데모: {unit.UnitName}이(가) 사망함");
        PopupManager.Instance.PopupError($"{unit.UnitName} 사망!");
    }
    
    public void RunFullDemo()
    {
        Debug.Log("=== 전체 데모 실행 ===");
        DemoUnitCard();
        DemoSpellCard();
        DemoCardInteractions();
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 120, 300, 250));
        GUILayout.Label("=== TCG Card System Demo ===");
        GUILayout.Label($"Demo Step: {demoStep}");
        GUILayout.Label($"Auto Demo: {(autoRunDemo ? "ON" : "OFF")}");
        GUILayout.Label($"Created Objects: {createdObjects.Count}");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Run Full Demo"))
        {
            RunFullDemo();
        }
        
        if (GUILayout.Button("Demo Unit Card"))
        {
            DemoUnitCard();
        }
        
        if (GUILayout.Button("Demo Spell Card"))
        {
            DemoSpellCard();
        }
        
        if (GUILayout.Button("Demo Interactions"))
        {
            DemoCardInteractions();
        }
        
        if (GUILayout.Button("Cleanup Demo"))
        {
            DemoCleanup();
        }
        
        GUILayout.Space(10);
        
        autoRunDemo = GUILayout.Toggle(autoRunDemo, "Auto Run Demo");
        
        GUILayout.EndArea();
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnitCard.OnUnitSummoned -= OnDemoUnitSummoned;
        Spell.OnSpellCast -= OnDemoSpellCast;
        
        DemoCleanup();
    }
}