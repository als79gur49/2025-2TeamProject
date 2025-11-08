using UnityEngine;
using Game.SaveSystem;

namespace Game.Data
{
    /// <summary>
    /// V2 시스템용 해금 조건 인터페이스
    /// StageProgressData를 사용
    /// </summary>
    public interface IUnlockCondition
    {
        /// <summary>
        /// V2 진행도 데이터로 조건 평가
        /// </summary>
        bool Evaluate(StageProgressData progressData);
        
        /// <summary>
        /// 조건 설명 텍스트
        /// </summary>
        string GetDescription();
        
        /// <summary>
        /// 조건 진행도 (0~1)
        /// </summary>
        float GetProgress(StageProgressData progressData);
    }

    /// <summary>
    /// V2 해금 조건 ScriptableObject 베이스 클래스
    /// </summary>
    public abstract class UnlockCondition : ScriptableObject, IUnlockCondition
    {
        [Header("Condition Settings")]
        [SerializeField]
        protected string conditionId = "";
        
        [SerializeField]
        [TextArea(2, 4)]
        protected string customDescription = "";
        
        [SerializeField]
        protected bool showProgress = true;

        // Properties
        public string ConditionId => string.IsNullOrEmpty(conditionId) ? name : conditionId;
        public bool ShowProgress => showProgress;

        /// <summary>
        /// 조건 평가 (파생 클래스에서 구현)
        /// </summary>
        public abstract bool Evaluate(StageProgressData progressData);
        
        /// <summary>
        /// 조건 진행도 계산 (파생 클래스에서 구현)
        /// </summary>
        public abstract float GetProgress(StageProgressData progressData);
        
        /// <summary>
        /// 조건 설명
        /// </summary>
        public virtual string GetDescription()
        {
            return string.IsNullOrEmpty(customDescription) 
                ? GetDefaultDescription() 
                : customDescription;
        }

        /// <summary>
        /// 진행도 포함 설명
        /// </summary>
        public virtual string GetDescriptionWithProgress(StageProgressData progressData)
        {
            string baseDesc = GetDescription();
            
            if (showProgress)
            {
                float progress = GetProgress(progressData);
                return $"{baseDesc} ({progress:P0})";
            }
            
            return baseDesc;
        }

        /// <summary>
        /// 기본 설명 (파생 클래스에서 오버라이드)
        /// </summary>
        protected abstract string GetDefaultDescription();

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(conditionId))
            {
                conditionId = System.Guid.NewGuid().ToString().Substring(0, 8);
            }
        }
#endif
    }
}
