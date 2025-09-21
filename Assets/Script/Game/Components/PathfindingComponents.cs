using UnityEngine;

namespace Game.Components
{
    /// <summary>
    /// A* 알고리즘용 노드 클래스 - 통합된 버전
    /// GridController에서 사용되는 공통 A* 노드 클래스
    /// </summary>
    public class AStarNode
    {
        public Vector2Int Position { get; }
        public float G { get; set; } // 시작점으로부터의 실제 거리
        public float H { get; } // 목표점까지의 휴리스틱 거리
        public float F => G + H; // 총 비용
        public AStarNode Parent { get; set; }

        public AStarNode(Vector2Int position, float g, float h, AStarNode parent = null)
        {
            Position = position;
            G = g;
            H = h;
            Parent = parent;
        }
    }
}