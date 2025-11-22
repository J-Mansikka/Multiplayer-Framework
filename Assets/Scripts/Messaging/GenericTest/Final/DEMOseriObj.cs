using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEMOseriObj : NewObject
{
    public MnetInt level;
    public MnetFloat dickLength;
    public MnetStringUnicode helloWorld;
    public MnetStringUnicode longTest;
    public MnetInt XavierTest;

    public override void UpdateState()
    {
        throw new System.NotImplementedException();
    }

    private void Awake()
    {
        Initialize();
    }
}
