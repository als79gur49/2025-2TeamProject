using System.Collections.Generic;
using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// VFX 트리거 시점에 게임 로직으로 전달되는 동적 데이터
    /// TCG 게임 특성: 사전 결정된 타겟 유효성 검증 중심
    /// </summary>
    public class VFXTriggerData
    {
        #region Basic Trigger Info

        /// <summary>VFX 트리거가 발생한 월드 좌표</summary>
        public Vector3 TriggerWorldPosition { get; set; }

        /// <summary>VFX 재생 진행도 (0.0 ~ 1.0)</summary>
        public float NormalizedProgress { get; set; }

        /// <summary>트리거 발생 시간 (게임 시작 기준)</summary>
        public float TriggerTime { get; set; }

        #endregion

        #region TCG-Specific Target Validation

        /// <summary>타겟 공격 성공 여부 (TCG 핵심 정보)</summary>
        public bool AttackSuccess { get; set; }

        /// <summary>공격 대상 (사전 결정된 타겟)</summary>
        public GameObject PredeterminedTarget { get; set; }

        /// <summary>타겟의 그리드 좌표 (TCG 보드 기준)</summary>
        public Vector2Int? TargetGridPosition { get; set; }

        /// <summary>타겟 유효성 실패 이유 (디버깅용)</summary>
        public string ValidationFailureReason { get; set; }

        #endregion

        #region Multi-Target Support (AOE Effects)

        /// <summary>다중 타겟 리스트 (범위 효과용)</summary>
        public List<GameObject> ValidTargets { get; private set; }

        /// <summary>유효하지 않은 타겟 리스트 (디버깅/로그용)</summary>
        public List<GameObject> InvalidTargets { get; private set; }

        #endregion

        #region Custom Data (Future Extension)

        /// <summary>확장 가능한 커스텀 데이터 (파티클 카운트, 애니메이션 상태 등)</summary>
        public Dictionary<string, object> CustomData { get; private set; }

        #endregion

        #region Constructor

        public VFXTriggerData()
        {
            ValidTargets = new List<GameObject>();
            InvalidTargets = new List<GameObject>();
            CustomData = new Dictionary<string, object>();

            TriggerTime = Time.time;
            AttackSuccess = false; // Default: 실패 상태
        }

        #endregion

        #region Helper Methods

        /// <summary>타겟 유효성 검증 성공 설정</summary>
        public void SetTargetValid(GameObject target, Vector2Int gridPos)
        {
            AttackSuccess = true;
            PredeterminedTarget = target;
            TargetGridPosition = gridPos;
            ValidationFailureReason = null;

            if (!ValidTargets.Contains(target))
                ValidTargets.Add(target);
        }

        /// <summary>타겟 유효성 검증 실패 설정</summary>
        public void SetTargetInvalid(GameObject target, string reason)
        {
            AttackSuccess = false;
            PredeterminedTarget = target;
            ValidationFailureReason = reason;

            if (target != null && !InvalidTargets.Contains(target))
                InvalidTargets.Add(target);
        }

        /// <summary>커스텀 데이터 추가</summary>
        public void SetCustomData(string key, object value)
        {
            if (CustomData.ContainsKey(key))
                CustomData[key] = value;
            else
                CustomData.Add(key, value);
        }

        /// <summary>커스텀 데이터 조회</summary>
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (CustomData.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        #endregion
    }
}
