using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MnetSmall : WANHAMnetObject
{
    public MnetBool booliTest;
    private void Awake()
    {
        //booliTest = new MnetBool();
        variables = new MnetVariable[1];
        variables[0] = booliTest;
        //Setup();
        booliTest.Set(true, 2);
        /*
        variables = new MnetInt[25];
        int intFill = 1;
        for (int i = 0; i < variables.Length; i++)
        {
            MnetInt intti = new MnetInt();
            variables[i] = intti;
        }
        Setup(this);
        for (int i = 0; i < variables.Length; i++)
        {
            MnetInt intti = (MnetInt)variables[i];
            intti.Value = intFill;
            intFill *= 2;
            variables[i] = intti;
        }
        */
    }
}
