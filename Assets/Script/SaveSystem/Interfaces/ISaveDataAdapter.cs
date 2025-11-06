using System.Collections.Generic;
using Game.Data;

namespace Game.SaveSystem
{
    /// <summary>
    /// SaveDataAdapter의 공개 인터페이스
    /// 외부에서는 이 인터페이스를 통해서만 저장 시스템 접근
    /// </summary>
    public interface ISaveDataAdapter
    {
        // 속성
        bool IsInitialized { get; }

        // 빠른 저장/로드
        void QuickSave();
        void QuickLoad();

        // 특정 데이터 저장/로드
        void SaveSpecific(SaveFileType fileType);
        void LoadSpecific(SaveFileType fileType);

        // 유틸리티
        bool HasSaveData();
        void DeleteAllSaveData();

        // 덱 관리 (Adapter를 통한 접근)
        bool SaveDeck(string deckName, Dictionary<CardData, int> deckCards);
        Dictionary<CardData, int> LoadDeck(string deckName);
        List<string> GetSavedDeckNames();
        bool DeleteDeck(string deckName);

        // 데이터 변환 메서드
        List<EnhancedCardData> ConvertDeckToEnhancedCards(Dictionary<CardData, int> deckCards);
        Dictionary<CardData, int> ConvertEnhancedCardsToDeck(List<EnhancedCardData> enhancedCards);
    }
}
