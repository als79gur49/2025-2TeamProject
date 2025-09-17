using UnityEngine;
using System;

public class PopupManager : MonoBehaviour
{
    private static PopupManager instance;
    
    public static PopupManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PopupManager>();
                
                if (instance == null)
                {
                    GameObject popupManagerObject = new GameObject("PopupManager");
                    instance = popupManagerObject.AddComponent<PopupManager>();
                    DontDestroyOnLoad(popupManagerObject);
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
    }
    
    public void PopupMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("PopupManager: 빈 메시지입니다.");
            return;
        }
        
        Debug.Log($"[POPUP] {message}");
    }
    
    public void PopupMessageWithColor(string message, Color color)
    {
        Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>[POPUP] {message}</color>");
    }
    
    public void PopupWarning(string message)
    {
        Debug.LogWarning($"[POPUP WARNING] {message}");
    }
    
    public void PopupError(string message)
    {
        Debug.LogError($"[POPUP ERROR] {message}");
    }
}