using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanelController : MonoBehaviour
{
    // TODO: Button - Panel 연결 내용 추가 필요, Panel에서 Regist하는 부분은 있다. 아마도 안정성을 위해서 IUIPanel 이용하는 함수로 사용
    // 이를 각 타입과 함수를 매핑하여서, 싱글턴에서 사용하는 방식과 개별 방식 생각 중
    // 추가로 panelId를 사용하는 것 같은데, 실제로 사용가능한지에 대해서 확인

    // Singleton 패턴
    private static UIPanelController instance;
    public static UIPanelController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIPanelController>();

                if (instance == null)
                {
                    var controllerGO = new GameObject("UIPanelController");
                    instance = controllerGO.AddComponent<UIPanelController>();
                    DontDestroyOnLoad(controllerGO);
                    Debug.Log("UIPanelManager 자동 생성됨");
                }
            }
            return instance;
        }
    }
    private void Awake()
    {
        // Singleton 패턴 구현
        if (instance != null && instance != this)
        {
            Debug.LogWarning("UIPaneController 중복 인스턴스 제거");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void OpenSettingPanel()
    {
        UIPanelManager.Instance.ShowPanel<SettingsPanel>();
    }
    public void CloseSettingPanel()
    {
        UIPanelManager.Instance.HidePanel<SettingsPanel>();
    }
}
