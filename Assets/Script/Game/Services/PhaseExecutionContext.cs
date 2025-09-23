using System.Collections.Generic;
using UnityEngine;

namespace Game.Services
{
    /// <summary>
    /// 페이즈 실행에 필요한 컨텍스트 정보를 관리하는 클래스입니다.
    /// </summary>
    public class PhaseExecutionContext
    {
        /// <summary>
        /// 현재 실행 중인 턴 페이즈
        /// </summary>
        public TurnPhase Phase { get; set; }
        
        /// <summary>
        /// 현재 페이즈의 실행 상태
        /// </summary>
        public PhaseExecutionState State { get; set; }
        
        /// <summary>
        /// 처리해야 할 유닛들의 리스트
        /// </summary>
        public List<Unit> UnitsToProcess { get; set; }
        
        /// <summary>
        /// 현재 처리 중인 유닛의 인덱스
        /// </summary>
        public int CurrentUnitIndex { get; set; }
        
        /// <summary>
        /// 페이즈 실행이 시작된 시간
        /// </summary>
        public float StartTime { get; set; }
        
        /// <summary>
        /// 현재 실행 중인 코루틴 참조
        /// </summary>
        public Coroutine ExecutionCoroutine { get; set; }
        
        /// <summary>
        /// PhaseExecutionContext의 새 인스턴스를 초기화합니다.
        /// </summary>
        public PhaseExecutionContext()
        {
            UnitsToProcess = new List<Unit>();
            CurrentUnitIndex = 0;
            StartTime = 0f;
            ExecutionCoroutine = null;
        }
    }
}