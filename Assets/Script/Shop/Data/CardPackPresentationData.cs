using UnityEngine;

/// <summary>
/// 카드팩 프레젠테이션에 필요한 데이터 컨테이너
/// - 실제 개봉 결과 (CardPackOpenResult)
/// - 닫힌 팩에 사용할 상자 스프라이트
/// </summary>
[System.Serializable]
public class CardPackPresentationData
{
    /// <summary>카드팩 개봉 결과 데이터</summary>
    public CardPackOpenResult result;

    /// <summary>팩 종류를 나타내는 상자 스프라이트 (나무 상자, 금 상자 등)</summary>
    public Sprite packSprite;
}

