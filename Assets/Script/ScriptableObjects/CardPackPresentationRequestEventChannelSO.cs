using UnityEngine;

/// <summary>
/// 카드팩 보상 프레젠테이션 요청 이벤트 채널
/// RewardQueueService가 큐에서 꺼낸 보상을 UI 계층에 전달할 때 사용
/// </summary>
[CreateAssetMenu(
    fileName = "CardPackPresentationRequestEventChannel",
    menuName = "Events/Reward/Card Pack Presentation Request Channel")]
public class CardPackPresentationRequestEventChannelSO : GameEventChannelSO<CardPackPresentationData>
{
}
