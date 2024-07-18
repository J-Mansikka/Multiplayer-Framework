using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TestingArray : TestingBase<TestingNamedFlag[]>, TestingInterface
{
    public void Deserialize(byte[] bytes)
    {
        throw new System.NotImplementedException();
    }

    public byte[] Serialize()
    {
        throw new System.NotImplementedException();
    }
}
