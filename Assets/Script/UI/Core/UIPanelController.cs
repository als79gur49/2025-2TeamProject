using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanelController : MonoBehaviour
{
    // TODO: Button - Panel ���� ���� �߰� �ʿ�, Panel���� Regist�ϴ� �κ��� �ִ�. �Ƹ��� �������� ���ؼ� IUIPanel �̿��ϴ� �Լ��� ���
    // �̸� �� Ÿ�԰� �Լ��� �����Ͽ���, �̱��Ͽ��� ����ϴ� ��İ� ���� ��� ���� ��
    // �߰��� panelId�� ����ϴ� �� ������, ������ ��밡�������� ���ؼ� Ȯ��

    // Singleton ����
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
                    Debug.Log("UIPanelManager �ڵ� ������");
                }
            }
            return instance;
        }
    }
    private void Awake()
    {
        // Singleton ���� ����
        if (instance != null && instance != this)
        {
            Debug.LogWarning("UIPaneController �ߺ� �ν��Ͻ� ����");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void OpenSettingPanel()
    {
        UIPanelFacade.ShowGlobalPanel<SettingsPanel>();
    }
    public void CloseSettingPanel()
    {
        UIPanelFacade.HideGlobalPanel<SettingsPanel>();
    }
}
