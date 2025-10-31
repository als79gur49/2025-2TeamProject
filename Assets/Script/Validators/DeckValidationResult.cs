using System.Collections.Generic;

namespace Game.Validators
{
    /// <summary>
    /// 덱 유효성 검증 결과
    /// 검증 성공/실패 여부와 오류/경고 메시지를 포함
    /// </summary>
    public class DeckValidationResult
    {
        /// <summary>덱이 유효한지 여부</summary>
        public bool IsValid { get; set; } = true;

        /// <summary>유효성 검증 오류 목록 (치명적, 덱 사용 불가)</summary>
        public List<string> Errors { get; private set; } = new List<string>();

        /// <summary>유효성 검증 경고 목록 (비치명적, 덱 사용 가능)</summary>
        public List<string> Warnings { get; private set; } = new List<string>();

        /// <summary>
        /// 오류가 있는지 확인
        /// </summary>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// 경고가 있는지 확인
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// 모든 오류 메시지를 하나의 문자열로 결합
        /// </summary>
        public string GetErrorMessage()
        {
            if (!HasErrors)
                return string.Empty;

            return string.Join("\n", Errors);
        }

        /// <summary>
        /// 모든 경고 메시지를 하나의 문자열로 결합
        /// </summary>
        public string GetWarningMessage()
        {
            if (!HasWarnings)
                return string.Empty;

            return string.Join("\n", Warnings);
        }

        /// <summary>
        /// 검증 결과 요약 (디버그용)
        /// </summary>
        public override string ToString()
        {
            if (IsValid)
            {
                if (HasWarnings)
                    return $"Valid (with {Warnings.Count} warnings)";
                else
                    return "Valid";
            }
            else
            {
                return $"Invalid ({Errors.Count} errors)";
            }
        }

        /// <summary>
        /// 결과 초기화
        /// </summary>
        public void Clear()
        {
            IsValid = true;
            Errors.Clear();
            Warnings.Clear();
        }
    }
}
