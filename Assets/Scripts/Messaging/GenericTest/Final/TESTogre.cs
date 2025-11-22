using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class TESTogre : NewObject

{

    public bool button;
    NewArray<MnetInt> arri;

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (button)
        {
            button = false;
            Test();
        }
    }

    public void Test()
    {

    }

    public override void UpdateState()
    {
        throw new System.NotImplementedException();
    }
}
