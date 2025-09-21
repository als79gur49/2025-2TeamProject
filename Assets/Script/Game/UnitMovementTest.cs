using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using Game.Core;


/// <summary>
/// 유닛 움직임 테스트 스크립트
/// Unit.cs의 Act() 메서드를 통한 MoveForward() 기능 테스트
/// </summary>
public class UnitMovementTest : MonoBehaviour
{
    [Header("Test Setup")]
    public Unit testUnit;
    public GridManager gridManager;
    
    [Header("Test Parameters")]
    public Vector2Int startPosition = new Vector2Int(5, 2);
    public int testSteps = 3;
    
    [Header("Debug Info")]
    [SerializeField] private Vector2Int currentPosition;
    [SerializeField] private bool testInProgress = false;
    
    private void Start()
    {
        // 테스트 자동 시작 (필요시)
        if (testUnit != null && gridManager != null)
        {
            Debug.Log("[UnitMovementTest] Auto-starting movement test");
            StartCoroutine(AutoTestMovement());
        }
    }
    
    /// <summary>
    /// 수동 테스트 시작 버튼 (Inspector에서 호출 가능)
    /// </summary>
    [ContextMenu("Start Movement Test")]
    public void StartMovementTest()
    {
        if (testInProgress)
        {
            Debug.LogWarning("[UnitMovementTest] Test already in progress");
            return;
        }
        
        StartCoroutine(TestUnitMovement());
    }
    
    /// <summary>
    /// 단일 스텝 테스트 (즉시 실행)
    /// </summary>
    [ContextMenu("Test Single Step")]
    public void TestSingleStep()
    {
        if (testUnit == null)
        {
            Debug.LogError("[UnitMovementTest] No test unit assigned");
            return;
        }
        
        Debug.Log("=== 단일 스텝 테스트 시작 ===");
        
        // 현재 위치 기록
        Vector2Int beforePosition = new Vector2Int(testUnit.X, testUnit.Y);
        Debug.Log($"이동 전 위치: ({beforePosition.x}, {beforePosition.y})");
        
        // Act() 호출로 움직임 실행
        testUnit.OnTurnStart(); // Act() 메서드가 포함된 턴 시작
        
        // 결과 확인 (다음 프레임에서)
        StartCoroutine(CheckMovementResult(beforePosition));
    }
    
    /// <summary>
    /// 자동 테스트 코루틴
    /// </summary>
    private IEnumerator AutoTestMovement()
    {
        // 시스템 초기화 대기
        yield return new WaitForSeconds(1f);
        
        if (!ValidateTestSetup())
        {
            yield break;
        }
        
        Debug.Log("[UnitMovementTest] Starting automatic movement test");
        yield return TestUnitMovement();
    }
    
    /// <summary>
    /// 유닛 움직임 테스트 메인 코루틴
    /// </summary>
    private IEnumerator TestUnitMovement()
    {
        testInProgress = true;
        
        try
        {
            // 1. 테스트 설정 검증
            if (!ValidateTestSetup())
            {
                yield break;
            }
            
            // 2. 초기 위치 설정
            SetupInitialPosition();
            yield return new WaitForSeconds(0.5f);

            // 3. 연속 이동 테스트
            for (int step = 1; step <= testSteps; step++)
            {
                Debug.Log($"=== 스텝 {step}/{testSteps} 시작 ===");
                
                Vector2Int beforePos = new Vector2Int(testUnit.X, testUnit.Y);
                currentPosition = beforePos;
                
                Debug.Log($"스텝 {step} - 이동 전 위치: ({beforePos.x}, {beforePos.y})");
                
                // Act() 호출로 움직임 실행
                testUnit.OnTurnStart();
                
                // 이동 완료 대기
                yield return new WaitForSeconds(0.5f);
                
                // 결과 확인
                Vector2Int afterPos = new Vector2Int(testUnit.X, testUnit.Y);
                currentPosition = afterPos;
                
                Debug.Log($"스텝 {step} - 이동 후 위치: ({afterPos.x}, {afterPos.y})");
                
                // 움직임 검증
                bool moved = beforePos != afterPos;
                if (moved)
                {
                    Debug.Log($"✅ 스텝 {step} 성공: 유닛이 ({beforePos.x}, {beforePos.y}) → ({afterPos.x}, {afterPos.y})로 이동");
                    
                    // 예상 방향 확인
                    Vector2Int movement = afterPos - beforePos;
                    string direction = GetMovementDirection(movement);
                    Debug.Log($"📍 이동 방향: {direction}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ 스텝 {step} 주의: 유닛이 이동하지 않음 (장애물이나 경계?)");
                }
                
                // 다음 스텝 전 대기
                if (step < testSteps)
                {
                    yield return new WaitForSeconds(1f);
                }
            }
            
            Debug.Log("=== 🎉 움직임 테스트 완료 ===");
        }
        finally
        {
            testInProgress = false;
        }
    }
    
    /// <summary>
    /// 이동 결과 확인 코루틴
    /// </summary>
    private IEnumerator CheckMovementResult(Vector2Int beforePosition)
    {
        yield return new WaitForEndOfFrame();
        
        Vector2Int afterPosition = new Vector2Int(testUnit.X, testUnit.Y);
        currentPosition = afterPosition;
        
        if (beforePosition != afterPosition)
        {
            Vector2Int movement = afterPosition - beforePosition;
            string direction = GetMovementDirection(movement);
            Debug.Log($"✅ 이동 성공: ({beforePosition.x}, {beforePosition.y}) → ({afterPosition.x}, {afterPosition.y})");
            Debug.Log($"📍 이동 방향: {direction}");
        }
        else
        {
            Debug.LogWarning("⚠️ 유닛이 이동하지 않음");
        }
    }
    
    /// <summary>
    /// 테스트 설정 검증
    /// </summary>
    private bool ValidateTestSetup()
    {
        if (testUnit == null)
        {
            Debug.LogError("[UnitMovementTest] testUnit이 할당되지 않음");
            return false;
        }
        
        if (gridManager == null)
        {
            Debug.LogWarning("[UnitMovementTest] gridManager가 할당되지 않음 - 자동 검색 시도");
            gridManager = FindObjectOfType<GridManager>();
            
            if (gridManager == null)
            {
                Debug.LogError("[UnitMovementTest] GridManager를 찾을 수 없음");
                return false;
            }
        }
        
        Debug.Log("[UnitMovementTest] 테스트 설정 검증 완료");
        return true;
    }
    
    /// <summary>
    /// 초기 위치 설정
    /// </summary>
    private void SetupInitialPosition()
    {
        Debug.Log($"[UnitMovementTest] 초기 위치 설정: ({startPosition.x}, {startPosition.y})");
        
        // 유닛을 시작 위치로 설정
        testUnit.SetPosition(startPosition.x, startPosition.y);
        currentPosition = startPosition;
        
        Debug.Log($"유닛 '{testUnit.name}' 초기 위치: ({testUnit.X}, {testUnit.Y})");
        Debug.Log($"유닛 타입: {(testUnit.IsPlayerUnit ? "플레이어" : "적")}");
        Debug.Log($"이동 범위: {testUnit.MovementRange}");
    }
    
    /// <summary>
    /// 이동 방향 문자열 반환
    /// </summary>
    private string GetMovementDirection(Vector2Int movement)
    {
        if (movement.y > 0) return "위쪽(북)";
        if (movement.y < 0) return "아래쪽(남)";
        if (movement.x > 0) return "오른쪽(동)";
        if (movement.x < 0) return "왼쪽(서)";
        return "이동 없음";
    }
    
    /// <summary>
    /// 유닛 상태 정보 출력
    /// </summary>
    [ContextMenu("Log Unit Status")]
    public void LogUnitStatus()
    {
        if (testUnit == null)
        {
            Debug.LogWarning("[UnitMovementTest] No test unit assigned");
            return;
        }
        
        Debug.Log("=== 유닛 상태 정보 ===");
        Debug.Log($"이름: {testUnit.name}");
        Debug.Log($"위치: ({testUnit.X}, {testUnit.Y})");
        Debug.Log($"체력: {testUnit.Health}/{testUnit.MaxHealth}");
        Debug.Log($"공격력: {testUnit.AttackPower}");
        Debug.Log($"이동 범위: {testUnit.MovementRange}");
        Debug.Log($"팀: {(testUnit.IsPlayerUnit ? "플레이어" : "적")}");
        Debug.Log($"생존: {(testUnit.IsAlive ? "생존" : "사망")}");
        Debug.Log($"컴포넌트 시스템 사용: {testUnit.IsUsingComponentSystem()}");
    }
    
    
    // Update에서 실시간 정보 업데이트
    private void Update()
    {
        if (testUnit != null)
        {
            currentPosition = new Vector2Int(testUnit.X, testUnit.Y);
        }
    }
}