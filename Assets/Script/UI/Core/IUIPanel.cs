using System;
using UnityEngine;

/// <summary>
/// UI 패널의 기본 인터페이스
/// 모든 UI 패널이 구현해야 하는 기본 기능 정의
/// </summary>
public interface IUIPanel
{
    /// <summary>
    /// 패널의 고유 식별자
    /// </summary>
    string PanelID { get; }
    
    /// <summary>
    /// 패널이 현재 활성화 상태인지 여부
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// 패널이 표시될 때 호출
    /// </summary>
    void OnShow();
    
    /// <summary>
    /// 패널이 숨겨질 때 호출
    /// </summary>
    void OnHide();
    
    /// <summary>
    /// 패널 초기화 (Awake/Start 대신 사용)
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 패널 정리 (OnDestroy 시 호출)
    /// </summary>
    void Cleanup();
    
    /// <summary>
    /// 패널 표시 이벤트
    /// </summary>
    event Action<IUIPanel> OnPanelShown;
    
    /// <summary>
    /// 패널 숨김 이벤트
    /// </summary>
    event Action<IUIPanel> OnPanelHidden;
}

/// <summary>
/// 데이터 바인딩을 지원하는 UI 패널 인터페이스
/// </summary>
/// <typeparam name="T">바인딩할 데이터 타입</typeparam>
public interface IDataBindable<T>
{
    /// <summary>
    /// 데이터를 패널에 바인딩
    /// </summary>
    /// <param name="data">바인딩할 데이터</param>
    void BindData(T data);
    
    /// <summary>
    /// 데이터 바인딩 해제
    /// </summary>
    void UnbindData();
    
    /// <summary>
    /// 현재 바인딩된 데이터
    /// </summary>
    T BoundData { get; }
}

/// <summary>
/// 애니메이션을 지원하는 UI 패널 인터페이스
/// </summary>
public interface IUIAnimatable
{
    /// <summary>
    /// 표시 애니메이션 재생
    /// </summary>
    /// <param name="onComplete">애니메이션 완료 콜백</param>
    void PlayShowAnimation(Action onComplete = null);
    
    /// <summary>
    /// 숨김 애니메이션 재생
    /// </summary>
    /// <param name="onComplete">애니메이션 완료 콜백</param>
    void PlayHideAnimation(Action onComplete = null);
    
    /// <summary>
    /// 애니메이션 진행 중인지 여부
    /// </summary>
    bool IsAnimating { get; }
}

/// <summary>
/// UI 패널 우선순위 열거형
/// </summary>
public enum UIPanelPriority
{
    Background = 0,     // 배경 UI
    Normal = 10,        // 일반 UI
    Important = 20,     // 중요한 UI (설정창 등)
    Critical = 30,      // 중요한 시스템 UI
    Modal = 40,         // 모달 다이얼로그
    Overlay = 50        // 최상위 오버레이
}

/// <summary>
/// UI 패널 상태 열거형
/// </summary>
public enum UIPanelState
{
    Inactive,      // 비활성화
    Initializing,  // 초기화 중
    Active,        // 활성화
    Hiding,        // 숨기는 중
    Showing        // 표시하는 중
}