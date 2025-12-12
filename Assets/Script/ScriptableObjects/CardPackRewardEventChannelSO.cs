using UnityEngine;

/// <summary>
/// 카드팩 보상 발생 이벤트 채널
/// 도메인 계층(상점, 스테이지 보상 등)에서 카드팩 개봉 결과를 발행할 때 사용
/// </summary>
[CreateAssetMenu(
    fileName = "CardPackRewardEventChannel",
    menuName = "Events/Reward/Card Pack Reward Event Channel")]
public class CardPackRewardEventChannelSO : GameEventChannelSO<CardPackPresentationData>
{
}
