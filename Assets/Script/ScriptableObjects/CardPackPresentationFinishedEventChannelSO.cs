using UnityEngine;

/// <summary>
/// 카드팩 보상 프레젠테이션 완료 이벤트 채널
/// PackOpenPanel 등 UI 계층이 연출을 모두 끝냈을 때 발행
/// </summary>
[CreateAssetMenu(
    fileName = "CardPackPresentationFinishedEventChannel",
    menuName = "Events/Reward/Card Pack Presentation Finished Channel")]
public class CardPackPresentationFinishedEventChannelSO : VoidEventChannelSO
{
}

