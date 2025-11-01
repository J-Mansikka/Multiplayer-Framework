using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEMOseriObj : MnetObject
{
    public MnetInt level;
    public MnetFloat dickLength;
    public MnetStringUnicode helloWorld;
    public MnetStringUnicode longTest;
    public MnetInt XavierTest;

    private void Awake()
    {
        Setup(69);
        Initialize(13);
    }
}
