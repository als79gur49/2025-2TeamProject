using System;
using System.Collections.Generic;

namespace Game.SaveSystem
{
    /// <summary>
    /// 데이터 쓰기 인터페이스
    /// </summary>
    public interface IDataWriter
    {
        void SaveToFile<T>(string fileName, T data, SaveFileType fileType);
        bool DeleteSaveFile(SaveFileType fileType);
        void DeleteAllSaveFiles();
    }

    /// <summary>
    /// 데이터 읽기 인터페이스
    /// </summary>
    public interface IDataReader
    {
        T LoadData<T>(SaveFileType fileType) where T : new();
        bool HasSaveFile(SaveFileType fileType);
    }

    /// <summary>
    /// 덱 관리 인터페이스
    /// </summary>
    public interface IDeckManager
    {
        bool SaveDeck(string deckName, List<EnhancedCardData> cards);
        DeckSaveData LoadDeck(string deckName);
        List<string> GetSavedDeckNames();
        bool DeleteDeck(string deckName);
    }

    /// <summary>
    /// SaveGameManager의 통합 인터페이스
    /// Interface Segregation Principle 적용
    /// </summary>
    public interface ISaveGameManager : IDataWriter, IDataReader, IDeckManager
    {
        // 기본 속성
        bool IsInitialized { get; }
        DateTime LastSaveTime { get; }

        // 이벤트
        event Action<SaveFileType> OnDataSaved;
        event Action<SaveFileType> OnDataLoaded;
        event Action<string> OnSaveError;
        event Action<string> OnLoadError;

        // 통합 저장/로드
        void SaveAllData();
        void LoadAllData();
        void SaveData(SaveFileType fileType);
    }
}
