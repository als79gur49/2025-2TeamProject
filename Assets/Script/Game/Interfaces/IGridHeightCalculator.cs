using UnityEngine;

namespace Game.Interfaces
{
    /// <summary>
    /// 그리드 높이 계산 인터페이스
    ///
    /// 책임: Base/Ground 위치에 따른 Y축 높이 계산
    /// 확장성: 향후 경사로, 계단, 다단계 층 시스템 지원 가능
    ///
    /// 설계 원칙:
    /// - 단일 책임: 높이 계산만 담당 (배치 오프셋과 분리)
    /// - 개방-폐쇄: 지형 타입 추가 시 GetGroundHeightAt() 확장
    /// - 의존성 역전: 추상 인터페이스에 의존
    /// </summary>
    public interface IGridHeightCalculator
    {
        /// <summary>
        /// Base 위치의 높이 (Inspector 설정 가능)
        /// 기본값: 1.0f
        /// </summary>
        float BaseHeight { get; set; }

        /// <summary>
        /// Ground 위치의 기본 높이 (Inspector 설정 가능)
        /// 기본값: 0.5f
        /// </summary>
        float GroundHeight { get; set; }

        /// <summary>
        /// 특정 그리드 위치의 지면 높이 반환
        ///
        /// 비즈니스 규칙:
        /// - Base 위치: BaseHeight 반환
        /// - Ground 위치: GroundHeight 반환
        /// - 향후 확장: 지형 타입별 높이 반환 가능
        /// </summary>
        /// <param name="gridPosition">그리드 좌표</param>
        /// <returns>Y축 높이 값</returns>
        float GetGroundHeightAt(Vector2Int gridPosition);

        /// <summary>
        /// 높이를 포함한 월드 좌표 계산 (Y축 포함)
        ///
        /// 계산 과정:
        /// 1. GridToWorldPosition() - Data Layer (Y=0 기본 좌표)
        /// 2. GetGroundHeightAt() - Business Logic (높이 규칙 적용)
        /// 3. Y축 합산 - 최종 월드 좌표 반환
        ///
        /// 주의: unitOffset은 포함하지 않음 (이동 시스템과 분리)
        /// </summary>
        /// <param name="gridPosition">그리드 좌표</param>
        /// <returns>높이가 반영된 월드 좌표 (Vector3)</returns>
        Vector3 CalculateWorldPositionWithHeight(Vector2Int gridPosition);
    }
}
