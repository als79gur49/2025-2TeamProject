using UnityEngine;
using Game.Core;
using System.Collections.Generic;

namespace Game.Interfaces
{
    public enum ActionType
    {
        Attack,
        Movement,
        Idle,
        Special
    }

    /// <summary>
    /// 액션 체인에서 다음 modifier로 진행할지 결정하는 동작 방식
    /// </summary>
    public enum ChainBehavior
    {
        /// <summary>
        /// 현재 액션이 실패했을 때만 다음 modifier로 진행 (fallback 패턴)
        /// </summary>
        FallbackOnFailure,

        /// <summary>
        /// 성공/실패 여부와 관계없이 항상 다음 modifier로 진행
        /// </summary>
        AlwaysContinue,

        /// <summary>
        /// 성공/실패 여부와 관계없이 항상 체인 종료
        /// </summary>
        AlwaysTerminate,

        /// <summary>
        /// 현재 액션이 성공했을 때만 다음 modifier로 진행
        /// </summary>
        ContinueOnSuccess
    }

    public interface IActionModifier
    {
        string ModifierName { get; }
        ActionType ActionType { get; }
        int Priority { get; }
        ChainBehavior ChainBehavior { get; }
        Unit Owner { get; }

        void SetNext(IActionModifier next);
        ActionResult Evaluate(ActionContext context);
        void Execute(ActionContext context);
    }

    public class ActionContext
    {
        public Vector2Int ActorPosition { get; set; }
        public ActionType CurrentActionType { get; set; }
        public List<Tile> AttackTiles { get; set; }
        public int AttackRange { get; set; }
        public bool HasClearRow { get; set; }
        public bool HasEnemyInRow { get; set; }
        public Vector2Int? TargetMovePosition { get; set; }
        public List<Vector2Int> MovePath { get; set; }
        public int MoveRange { get; set; }
        public List<IActionModifier> ExecutedActions { get; set; }

        public ActionContext(Vector2Int pos)
        {
            ActorPosition = pos;
            AttackTiles = new List<Tile>();
            MovePath = new List<Vector2Int>();
            ExecutedActions = new List<IActionModifier>();
        }
    }

    public class ActionResult
    {
        public bool IsSuccess { get; set; }
        public ActionType ActionType { get; set; }
        public ChainBehavior ChainBehavior { get; set; }
        public IActionModifier SelectedModifier { get; set; }
        public List<Tile> ValidTiles { get; set; }
        public Vector2Int? MoveDestination { get; set; }
        public List<Vector2Int> MovePath { get; set; }

        public ActionResult()
        {
            ValidTiles = new List<Tile>();
            MovePath = new List<Vector2Int>();
        }

        public bool ShouldContinueChain()
        {
            switch (ChainBehavior)
            {
                case ChainBehavior.FallbackOnFailure:
                    return !IsSuccess;
                case ChainBehavior.AlwaysContinue:
                    return true;
                case ChainBehavior.AlwaysTerminate:
                    return false;
                case ChainBehavior.ContinueOnSuccess:
                    return IsSuccess;
                default:
                    return false;
            }
        }
    }
}
