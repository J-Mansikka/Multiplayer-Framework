using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MtestEvent : WANHAMnetObject
{
    public MnetInt numba;


    private void Awake()
    {
        Initialize();
    }

    public byte ActionTakeDamage()
    {
        Debug.Log("DRY RUN");
        return 0;
    }

    private byte ActionFuckup()
    {
        Debug.Log("Workin !");
        return 1;
    }
}
