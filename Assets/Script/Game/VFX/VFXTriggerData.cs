using System.Collections.Generic;
using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// VFX 트리거 데이터 - 타일 기반 설계
    /// 모든 타겟팅 정보를 타일 위치로 전달
    /// Phase 1.3: GameObject 기반 → 타일 좌표 기반으로 전환
    /// </summary>
    public class VFXTriggerData
    {
        #region Tile-Based Targeting (Phase 1.3)

        /// <summary>
        /// 타겟 타일의 그리드 좌표
        /// </summary>
        public Vector3Int TileGridPosition { get; set; }

        /// <summary>
        /// 타겟 타일의 월드 좌표 (VFX 재생 위치)
        /// </summary>
        public Vector3 TileWorldPosition { get; set; }

        /// <summary>
        /// VFX 트리거 시점의 검증 결과
        /// </summary>
        public bool AttackSuccess { get; set; }

        /// <summary>
        /// 검증 실패 사유 (디버깅용)
        /// </summary>
        public string ValidationFailureReason { get; set; }

        #endregion

        #region Legacy Support (Deprecated)

        /// <summary>
        /// [Deprecated] VFX 트리거가 발생한 월드 좌표
        /// → TileWorldPosition 사용 권장
        /// </summary>
        public Vector3 TriggerWorldPosition
        {
            get => TileWorldPosition;
            set => TileWorldPosition = value;
        }

        /// <summary>VFX 재생 진행도 (0.0 ~ 1.0)</summary>
        public float NormalizedProgress { get; set; }

        /// <summary>트리거 발생 시간 (게임 시작 기준)</summary>
        public float TriggerTime { get; set; }

        /// <summary>
        /// [Deprecated] 타겟의 그리드 좌표 (TCG 보드 기준)
        /// → TileGridPosition 사용 권장
        /// </summary>
        public Vector2Int? TargetGridPosition { get; set; }

        #endregion

        #region Custom Data (Future Extension)

        /// <summary>확장 가능한 커스텀 데이터 (파티클 카운트, 애니메이션 상태 등)</summary>
        public Dictionary<string, object> CustomData { get; private set; }

        #endregion

        #region Constructor

        public VFXTriggerData()
        {
            CustomData = new Dictionary<string, object>();

            TriggerTime = Time.time;
            AttackSuccess = false; // Default: 실패 상태
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 타일 기반 타겟 유효성 검증 성공 설정
        /// </summary>
        public void SetTileTargetValid(Vector3Int gridPos, Vector3 worldPos)
        {
            AttackSuccess = true;
            TileGridPosition = gridPos;
            TileWorldPosition = worldPos;
            ValidationFailureReason = null;
        }

        /// <summary>
        /// 타일 기반 타겟 유효성 검증 실패 설정
        /// </summary>
        public void SetTileTargetInvalid(Vector3Int gridPos, string reason)
        {
            AttackSuccess = false;
            TileGridPosition = gridPos;
            ValidationFailureReason = reason;
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
