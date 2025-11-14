using UnityEngine;

/// <summary>
/// 적 유닛 처치 이벤트 채널
/// 적 유닛이 죽었을 때 발생하는 이벤트를 전달합니다.
///
/// 사용처:
/// - Unit: 적 유닛 사망 시 이벤트 발생
/// - GameSessionManager: 점수 추가
/// - (미래) 업적/퀘스트 시스템 등에서 구독 가능
/// </summary>
[CreateAssetMenu(fileName = "EnemyKilledEventChannel", menuName = "Events/Enemy Killed Event Channel")]
public class EnemyKilledEventChannelSO : VoidEventChannelSO
{
    // VoidEventChannelSO를 상속받아 파라미터 없는 이벤트 제공
    // 모든 적 유닛이 동일하게 처리되므로 추가 데이터 불필요
}
