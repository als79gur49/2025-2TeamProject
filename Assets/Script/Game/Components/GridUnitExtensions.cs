using UnityEngine;
using Game.Interfaces;

namespace Game.Components
{
    /// <summary>
    /// 호환성을 위한 확장 메서드
    /// </summary>
    public static class GridUnitExtensions 
    {
        public static IGridUnit AsGridUnit(this GameObject gameObject)
        {
            return new GameObjectGridUnit(gameObject);
        }
        
        public static IGridUnit AsGridUnit(this Unit unit)
        {
            return new UnitGridUnit(unit);
        }
    }

    /// <summary>
    /// GameObject용 그리드 유닛 어댑터
    /// </summary>
    internal class GameObjectGridUnit : IGridUnit
    {
        public GameObject GameObject { get; }
        public Vector2Int GridPosition { get; set; }
        
        public GameObjectGridUnit(GameObject obj) => GameObject = obj;
        public bool CanOccupyTile(Vector2Int position) => GameObject != null;
    }

    /// <summary>
    /// Unit 클래스용 그리드 유닛 어댑터
    /// </summary>
    internal class UnitGridUnit : IGridUnit
    {
        private readonly Unit unit;
        public GameObject GameObject => unit.gameObject;
        public Vector2Int GridPosition 
        { 
            get => new Vector2Int(unit.X, unit.Y); 
            set { unit.SetPosition(value.x, value.y); }
        }
        
        public UnitGridUnit(Unit unit) => this.unit = unit;
        public bool CanOccupyTile(Vector2Int position) => unit.CanMoveTo(position.x, position.y);
    }
}