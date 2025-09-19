using Game.Core;
using Game.Interfaces;
using UnityEngine;

public class UnitTest : MonoBehaviour
{
    [SerializeField]
    private GridManager gridManager;
    [SerializeField] private GameObject unitPrefab;
    private Unit testUnit1;
    private Unit testUnit2;
    
    void Start()
    {
        if (unitPrefab == null)
        {
            CreateTestUnits();
        }
        else
        {
            InstantiateTestUnits();
        }
        
        TestUnitFunctionality();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && testUnit1 != null)
        {
            testUnit1.TakeDamage(20);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2) && testUnit2 != null)
        {
            testUnit2.TakeDamage(30);
        }

        if(Input.GetKeyDown(KeyCode.Alpha3) && testUnit1 != null)
        {
            testUnit1.OnTurnStart();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (testUnit1 != null) testUnit1.Heal(15);
            if (testUnit2 != null) testUnit2.Heal(15);
        }
    }
    
    private void CreateTestUnits()
    {
        
        GameObject unit1Obj = new GameObject("TestUnit1");
        //unit1Obj.transform.position = new Vector3(-2, 0, 0);
        //IGridController gridGridController = gridManager.GetGridServices().GridController;
        //gridGridController.MoveUnit(unit1Obj,new Vector2Int(4, 0));
        testUnit1 = unit1Obj.AddComponent<Unit>();
        


        GameObject unit2Obj = new GameObject("TestUnit2");
        testUnit2 = unit2Obj.AddComponent<Unit>();
        unit2Obj.transform.position = new Vector3(2, 0, 0);
        
        AddVisualCubes();
    }
    
    private void InstantiateTestUnits()
    {
        Debug.Log("-------------------TestUnitMove-----------------");
        GameObject unit1Obj = Instantiate(unitPrefab, new Vector3(-2, 0, 0), Quaternion.identity);
        unit1Obj.name = "TestUnit1";
        testUnit1 = unit1Obj.GetComponent<Unit>();

        IGridController gridGridController = gridManager.GetGridServices()?.GridController;
        //ServiceLocator.Get
        gridGridController?.MoveUnit(unit1Obj, new Vector2Int(4, 0));

        GameObject unit2Obj = Instantiate(unitPrefab, new Vector3(2, 0, 0), Quaternion.identity);
        unit2Obj.name = "TestUnit2";
        testUnit2 = unit2Obj.GetComponent<Unit>();
    }
    
    private void AddVisualCubes()
    {
        if (testUnit1 != null)
        {
            GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube1.transform.SetParent(testUnit1.transform);
            cube1.transform.localPosition = Vector3.zero;
            cube1.GetComponent<Renderer>().material.color = Color.blue;
        }
        
        if (testUnit2 != null)
        {
            GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube2.transform.SetParent(testUnit2.transform);
            cube2.transform.localPosition = Vector3.zero;
            cube2.GetComponent<Renderer>().material.color = Color.red;
        }
    }
    
    private void TestUnitFunctionality()
    {
        Debug.Log("=== Unit Test Started ===");
        
        if (testUnit1 != null)
        {
            Debug.Log($"Unit1 - Health: {testUnit1.Health}, Attack: {testUnit1.AttackPower}, Movement: {testUnit1.MovementRange}");
        }
        
        if (testUnit2 != null)
        {
            Debug.Log($"Unit2 - Health: {testUnit2.Health}, Attack: {testUnit2.AttackPower}, Movement: {testUnit2.MovementRange}");
        }
        
        Debug.Log("Press 1 to damage Unit1 (20 damage)");
        Debug.Log("Press 2 to damage Unit2 (30 damage)");
        Debug.Log("Press H to heal both units (15 heal)");
        Debug.Log("=== Unit Test Instructions Displayed ===");
    }
}