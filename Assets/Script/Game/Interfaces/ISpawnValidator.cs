using UnityEngine;
using Game.Data;
using Game.Services;
using System.Diagnostics;

namespace Game.Interfaces
{
    /// <summary>
    /// 소환 검증자 인터페이스
    /// 유닛 소환 및 주문 사용의 유효성을 검증
    /// </summary>
    public interface ISpawnValidator
    {
        /// <summary>검증자가 초기화되었는지 여부</summary>
        bool IsInitialized { get; }

        /// <summary>CardServiceManager에 의해 호출되는 초기화 메서드</summary>
        void Init(IGridController iGridController, ITurnService iTurnService, IResourceManager iResourceManager);

        bool CanUseCard(CardData cardData, Vector2Int targetPosition, bool isPlayerUnit);
    }
}