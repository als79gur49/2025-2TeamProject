using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Services.Modifiers.Conditions
{
    /// <summary>
    /// 클리어 레인 조건
    /// 같은 행에 적이 없을 때만 통과 (스나이퍼용)
    /// </summary>
    public class ClearLaneCondition : IModifierCondition
    {
        private readonly IGridManager gridManager;

        public string Description => "같은 행에 적이 없어야 함";

        public ClearLaneCondition(IGridManager gridManager)
        {
            this.gridManager = gridManager;
        }

        public bool Evaluate(Unit owner, ActionContext context)
        {
            if (owner == null || context == null || gridManager == null) return false;

            var teamComponent = owner.GetComponent<ITeamComponent>();
            if (teamComponent == null || teamComponent.Team == TeamType.None) return false;

            var actorTeam = teamComponent.Team;

            // 행위자의 행(row) 번호 가져오기
            int actorRow = context.ActorPosition.x;

            // 그리드 너비 가져오기
            int gridWidth = gridManager.GridSize.y;

            // 같은 행의 모든 칸을 순회하며 적 유닛 확인
            for (int y = 0; y < gridWidth; y++)
            {
                var checkPosition = new Vector2Int(actorRow, y);

                // 행위자 자신의 위치는 건너뛰기
                if (checkPosition == context.ActorPosition) continue;

                // 해당 위치에 적 유닛이 있는지 확인 (행위자 기준 적군)
                if (gridManager.HasUnitWithRelation(checkPosition, actorTeam, TeamRelation.Enemy))
                {
                    // 같은 행에 적이 있으면 조건 실패
                    return false;
                }
            }

            // 같은 행에 적이 없으면 조건 통과
            return true;
        }
    }
}
