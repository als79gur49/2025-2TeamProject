using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.AnimatedValues;
using UnityEngine;

public class _ConeAni : MonoBehaviour
{
    public bool ConeStart = false;
    public bool ConeLoop = false;
    public bool ConeDone = false;
    public bool ConeEnd = false;
    Animator ConeAni;
    // Start is called before the first frame update
    void Start()
    {
        ConeAni = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ConeStart == true)
        {
            ConeAni.SetBool("IsConeStart", true);
        }
        if (ConeLoop == true)
        {
            ConeAni.SetBool("IsConeLoop", true);
        }
        if (ConeDone == true)
        {
            ConeAni.SetBool("IsConeDone", true);
            ConeAni.SetBool("IsConeEnd", false);
        }
        if (ConeEnd == true)
        {
            ConeAni.SetBool("IsConeEnd", true);
            ConeAni.SetBool("IsConeStart", false);
            ConeAni.SetBool("IsConeLoop", false);
            ConeAni.SetBool("IsConeDone", false);
        }
    }
}