using System;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 개선된 스탯 수정자 클래스 - 완전한 캡슐화와 안전한 수정
    /// </summary>
    [System.Serializable]
    public class StatModifier
    {
        /// <summary>
        /// 수정자 타입 열거형
        /// </summary>
        public enum ModifierType
        {
            Flat,           // 고정값 증감 (+10 공격력)
            Percentage,     // 백분율 증감 (+20% 공격력)
            Multiply        // 곱셈 (+100% = 2배)
        }

        // ✅ private 필드 + SerializeField로 Unity Inspector 지원하면서 캡슐화 유지
        [SerializeField] private ModifierType type = ModifierType.Flat;
        [SerializeField] private float value = 0f;
        [SerializeField] private float duration = -1f; // -1은 영구적
        [SerializeField] private int priority = 0; // 적용 우선순위
        [SerializeField] private string source = "Unknown"; // 수정자 출처
        [SerializeField] private bool isActive = true;

        // ✅ 읽기 전용 속성으로 안전한 외부 접근
        public ModifierType Type => type;
        public float Value => value;
        public float Duration => duration;
        public int Priority => priority;
        public string Source => source;
        public bool IsActive => isActive;
        public bool IsPermanent => duration < 0f;
        public bool IsExpired => !IsPermanent && duration <= 0f;

        // ✅ 기본 생성자
        public StatModifier() { }

        // ✅ 매개변수 생성자
        public StatModifier(ModifierType type, float value, float duration = -1f, int priority = 0, string source = "Unknown")
        {
            this.type = type;
            this.value = value;
            this.duration = duration;
            this.priority = priority;
            this.source = source ?? "Unknown";
            this.isActive = true;
        }

        // ✅ 복사 생성자
        public StatModifier(StatModifier original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            type = original.type;
            value = original.value;
            duration = original.duration;
            priority = original.priority;
            source = original.source;
            isActive = original.isActive;
        }

        // ✅ 안전한 지속시간 업데이트 메서드
        public bool UpdateDuration(float deltaTime)
        {
            if (IsPermanent || !isActive) 
                return false;

            duration = Mathf.Max(0f, duration - deltaTime);
            
            if (IsExpired)
            {
                isActive = false;
                return true; // 만료됨을 알림
            }
            
            return false;
        }

        // ✅ 수정자 활성화/비활성화
        public void SetActive(bool active)
        {
            isActive = active;
        }

        // ✅ 수정자 값 계산 메서드
        public float ApplyModifier(float baseValue)
        {
            if (!isActive || IsExpired)
                return baseValue;

            return type switch
            {
                ModifierType.Flat => baseValue + value,
                ModifierType.Percentage => baseValue * (1f + value / 100f),
                ModifierType.Multiply => baseValue * value,
                _ => baseValue
            };
        }

        // ✅ 수정자 값 안전 설정 (특별한 경우에만 사용)
        public void SetValue(float newValue)
        {
            // 타입별 유효성 검증
            switch (type)
            {
                case ModifierType.Flat:
                    value = newValue;
                    break;
                case ModifierType.Percentage:
                    value = Mathf.Max(-100f, newValue); // 최소 -100% (0이 되도록)
                    break;
                case ModifierType.Multiply:
                    value = Mathf.Max(0f, newValue); // 음수 곱셈 방지
                    break;
            }
        }

        // ✅ 지속시간 연장 메서드
        public void ExtendDuration(float additionalTime)
        {
            if (IsPermanent) return;
            
            duration = Mathf.Max(0f, duration + additionalTime);
            if (duration > 0f)
                isActive = true; // 지속시간이 있으면 다시 활성화
        }

        // ✅ 수정자 새로고침 (지속시간 리셋)
        public void Refresh(float newDuration = -1f)
        {
            if (newDuration >= 0f)
                duration = newDuration;
            else if (!IsPermanent)
                duration = Math.Abs(duration); // 원래 지속시간으로 리셋
            
            isActive = true;
        }

        // ✅ 수정자 비교 메서드 (중복 방지용)
        public bool IsSameAs(StatModifier other)
        {
            if (other == null) return false;
            
            return type == other.type &&
                   Mathf.Approximately(value, other.value) &&
                   source == other.source;
        }

        // ✅ 우선순위 비교 (정렬용)
        public int CompareTo(StatModifier other)
        {
            if (other == null) return 1;
            
            // 우선순위가 높은 것이 먼저 (내림차순)
            int priorityComparison = other.priority.CompareTo(priority);
            if (priorityComparison != 0) return priorityComparison;
            
            // 우선순위가 같으면 타입 순서로 (Flat -> Percentage -> Multiply)
            return type.CompareTo(other.type);
        }

        // ✅ 유효성 검증
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(source) && 
                   (IsPermanent || duration >= 0f);
        }

        // ✅ 디버깅을 위한 ToString 오버라이드
        public override string ToString()
        {
            string durationStr = IsPermanent ? "Permanent" : $"{duration:F1}s";
            string valueStr = type switch
            {
                ModifierType.Flat => $"{value:+0;-0}",
                ModifierType.Percentage => $"{value:+0;-0}%",
                ModifierType.Multiply => $"x{value:F2}",
                _ => value.ToString()
            };
            
            return $"StatModifier[{type}:{valueStr}, {durationStr}, {source}, Priority:{priority}, Active:{isActive}]";
        }

        // ✅ 수정자 복제 메서드
        public StatModifier Clone()
        {
            return new StatModifier(this);
        }
    }
}