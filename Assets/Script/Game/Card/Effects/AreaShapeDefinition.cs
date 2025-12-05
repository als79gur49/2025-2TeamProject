using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// 타일 기반 범위/형태를 정의하는 ScriptableObject 베이스
    /// 기존 AffectedRange 기반 맨해튼 범위를 일반화한 개념입니다.
    /// </summary>
    public abstract class AreaShapeDefinition : ScriptableObject
    {
        /// <summary>
        /// 중심 위치와 GridController를 기반으로, 이 Shape에 포함되는 모든 타일을 반환합니다.
        /// </summary>
        public abstract IEnumerable<Tile> GetTiles(Vector2Int center, IGridController gridController);

        /// <summary>
        /// 주어진 위치가 이 Shape의 범위 안에 포함되는지 여부를 반환합니다.
        /// 기본 구현은 GetTiles 결과를 순회하여 판단하며,
        /// 필요 시 성능 향상을 위해 파생 클래스에서 오버라이드할 수 있습니다.
        /// </summary>
        public virtual bool IsInArea(Vector2Int center, Vector2Int tilePos, IGridController gridController)
        {
            foreach (var tile in GetTiles(center, gridController))
            {
                if (tile == null) continue;
                if (tile.GetGridPosition() == tilePos)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 중심 좌표에 관계없이 항상 동일한 타일 집합을 반환하는지 여부
        /// GlobalAreaShape 등에서 true로 오버라이드합니다.
        /// 기본값은 false입니다.
        /// </summary>
        public virtual bool IsCenterIndependent => false;

        /// <summary>
        /// UI/설명용 범위 텍스트를 반환합니다.
        /// 예: "해당 타일만", "주변 2칸", "열 방향 모든 타일" 등
        /// </summary>
        public abstract string GetRangeDescription();
    }
}
