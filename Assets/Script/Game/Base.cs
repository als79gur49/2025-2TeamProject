using UnityEngine;
using System.Collections.Generic;
using Game.Components;
using Game.Interfaces;

namespace Game
{
    /// <summary>
    /// 플레이어 기지 - 여러 타일을 점유하는 고정 구조물
    /// TeamComponent 기반 팀 시스템 사용
    /// 범위 공격 시 중복 피해 방지를 위해 HealthComponent 인스턴스 단위로 관리
    /// </summary>
    public class Base : MonoBehaviour
    {
        [Header("Base Configuration")]
        [SerializeField] private Vector2Int baseSize = new Vector2Int(1, 3);  // 가로x세로 크기
        [SerializeField] private int maxHealth = 500;

        [Header("Visual Settings")]
        [SerializeField] private GameObject basePrefab;  // 기지 시각적 프리팹

        // ✅ 컴포넌트 참조
        private TeamComponent teamComponent;
        private HealthComponent healthComponent;

        // ✅ 타일 점유 정보
        private Vector2Int startPosition;  // 좌하단 시작 위치
        private readonly List<Tile> occupiedTiles = new List<Tile>();

        // ✅ 이벤트
        /// <summary>Base 파괴 시 발생하는 이벤트 (BaseManager가 구독)</summary>
        public event System.Action<GameObject> OnDeath;

        // ✅ 속성
        public TeamType Team => teamComponent?.Team ?? TeamType.None;
        public Vector2Int BaseSize => baseSize;
        public Vector2Int StartPosition => startPosition;
        public bool IsAlive => healthComponent != null && healthComponent.IsAlive;
        public HealthComponent HealthComponent => healthComponent;
        public IReadOnlyList<Tile> OccupiedTiles => occupiedTiles;

        private void Awake()
        {
            InitializeComponents();
        }

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            // TeamComponent 가져오기 또는 추가
            teamComponent = GetComponent<TeamComponent>();
            if (teamComponent == null)
            {
                teamComponent = gameObject.AddComponent<TeamComponent>();
                Debug.LogWarning($"[Base] TeamComponent가 없어 자동 추가됨: {gameObject.name}");
            }

            // HealthComponent 가져오기 또는 추가
            healthComponent = GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = gameObject.AddComponent<HealthComponent>();
            }

            // 기지 체력 설정
            healthComponent.SetMaxHealth(maxHealth);
            healthComponent.RestoreToFullHealth();

            // 사망 이벤트 구독
            healthComponent.OnDeath += OnBaseDestroyed;
        }

        /// <summary>
        /// Base 초기화 - 시작 위치와 팀 설정
        /// [Deprecated] BaseManager에서 3-parameter 버전 사용 권장
        /// </summary>
        [System.Obsolete("Use Initialize(Vector2Int, Vector2Int, TeamType) instead", false)]
        public void Initialize(Vector2Int startPos, TeamType team)
        {
            startPosition = startPos;

            if (teamComponent != null)
            {
                teamComponent.Team = team;
            }

            Debug.Log($"[Base] Initialized at {startPos} for team {team}, size {baseSize}");
        }

        /// <summary>
        /// Base 초기화 - 시작 위치, 크기, 팀 설정
        /// BaseManager에서 호출
        /// </summary>
        public void Initialize(Vector2Int startPos, Vector2Int size, TeamType team)
        {
            startPosition = startPos;
            baseSize = size;

            if (teamComponent != null)
            {
                teamComponent.Team = team;
            }

            Debug.Log($"[Base] Initialized at {startPos} for team {team}, size {baseSize}");
        }

        /// <summary>
        /// 타일 참조 추가
        /// </summary>
        public void AddOccupiedTile(Tile tile)
        {
            if (tile != null && !occupiedTiles.Contains(tile))
            {
                occupiedTiles.Add(tile);
            }
        }

        /// <summary>
        /// 타일 참조 제거
        /// </summary>
        public void RemoveOccupiedTile(Tile tile)
        {
            occupiedTiles.Remove(tile);
        }

        /// <summary>
        /// 특정 타일이 이 Base에 속하는지 확인
        /// </summary>
        public bool ContainsTile(Vector2Int tilePos)
        {
            return tilePos.x >= startPosition.x && tilePos.x < startPosition.x + baseSize.x &&
                   tilePos.y >= startPosition.y && tilePos.y < startPosition.y + baseSize.y;
        }

        /// <summary>
        /// Base가 점유하는 모든 타일 위치 반환
        /// </summary>
        public List<Vector2Int> GetOccupiedPositions()
        {
            var positions = new List<Vector2Int>();

            for (int x = 0; x < baseSize.x; x++)
            {
                for (int y = 0; y < baseSize.y; y++)
                {
                    positions.Add(new Vector2Int(startPosition.x + x, startPosition.y + y));
                }
            }

            return positions;
        }

        /// <summary>
        /// Base 파괴 시 처리
        /// </summary>
        private void OnBaseDestroyed()
        {
            Debug.Log($"[Base] Team {Team} base destroyed at {startPosition}!");

            // BaseManager에 사망 알림
            OnDeath?.Invoke(gameObject);

            // 점유 타일 정리
            foreach (var tile in occupiedTiles)
            {
                if (tile != null)
                {
                    tile.RemoveBase();
                }
            }
            occupiedTiles.Clear();

            // 오브젝트 파괴
            Destroy(gameObject, 1f);
        }

        private void OnDestroy()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDeath -= OnBaseDestroyed;
            }
        }

        /// <summary>
        /// 디버깅용 Gizmo
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (teamComponent != null)
            {
                Gizmos.color = teamComponent.TeamColor;
            }
            else
            {
                Gizmos.color = Color.cyan;
            }

            // Base 점유 영역 표시
            Vector3 center = transform.position;
            Vector3 size = new Vector3(baseSize.x, 1f, baseSize.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
