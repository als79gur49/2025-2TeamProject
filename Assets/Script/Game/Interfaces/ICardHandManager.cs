using UnityEngine;
using System.Collections.Generic;
using Game.Data;
using Game.Services;

namespace Game.Interfaces
{
    /// <summary>
    /// 카드 핸드 매니저 인터페이스
    /// 플레이어의 카드 핸드 관리와 UI 상호작용을 담당
    /// Phase 3: UI 및 상호작용 구현 완료
    /// </summary>
    public interface ICardHandManager
    {
        /// <summary>핸드 매니저가 초기화되었는지 여부</summary>
        bool IsInitialized { get; }

        /// <summary>현재 핸드의 카드 수</summary>
        int HandSize { get; }

        /// <summary>플레이어 소환 모드인지 여부</summary>
        bool IsPlayerSummonMode { get; }

        /// <summary>CardServiceManager에 의해 호출되는 초기화 메서드</summary>
        void Init(ITurnService iTurnService);

        /// <summary>플레이어의 소환 모드 활성화</summary>
        void EnablePlayerSummonMode();

        /// <summary>플레이어의 소환 모드 비활성화</summary>
        void DisablePlayerSummonMode();

        /// <summary>핸드에 카드 추가</summary>
        bool AddCardToHand(CardData cardData);

        /// <summary>핸드에서 카드 제거</summary>
        bool RemoveCardFromHand(CardData cardData);

        /// <summary>핸드에서 카드 제거 (CardUIRefactored 참조와 함께)</summary>
        bool RemoveCardFromHand(CardData cardData, Game.Card.UI.Refactored.CardUIRefactored cardUI);

        /// <summary>핸드 초기화 (모든 카드 제거)</summary>
        void ClearHand();

        /// <summary>핸드의 모든 카드 데이터 반환</summary>
        List<CardData> GetHandCards();

        /// <summary>특정 인덱스의 카드 반환</summary>
        CardData GetCardAt(int index);

        /// <summary>핸드가 가득 찼는지 확인</summary>
        bool IsHandFull();

        /// <summary>특정 카드가 핸드에 있는지 확인</summary>
        bool HasCard(CardData cardData);

        /// <summary>랜덤 카드 드로우</summary>
        void DrawRandomCard();

        /// <summary>핸드 매니저 상태 정보 반환</summary>
        string GetStatus();

        /// <summary>핸드 레이아웃 재정렬 (외부 호출용)</summary>
        void RefreshHandLayout();

        /// <summary>덱 로드 및 셔플 (PlayerData의 lastUsedDeckName에서 로드된 덱 설정)</summary>
        void LoadDeck(Dictionary<CardData, int> deck);

        /// <summary>덱이 로드되었는지 여부 반환</summary>
        bool IsDeckLoaded();

        /// <summary>덱에 남은 총 카드 수 반환</summary>
        int GetRemainingDeckCount();
    }
}