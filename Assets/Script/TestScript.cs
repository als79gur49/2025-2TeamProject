using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class TestScript : MonoBehaviour
{
    int n = 0;

    void Start()
    {
        transform.
            DOMoveX(100, 2f).
            SetRelative(true);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log($"Test {n}");
        n++;
    }
}

public interface TestInterface
{
    event Action<int> TestActionInt;
}

public class TestSubClass : TestInterface
{
    public event Action<int> TestActionInt;
}


