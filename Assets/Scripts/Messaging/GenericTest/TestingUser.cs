using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TestingUser : TestingBase<string>, TestingInterface
{
    public byte[] Serialize()
    {
        throw new System.NotImplementedException();
    }

    public void Deserialize(byte[] bytes)
    {
        throw new System.NotImplementedException();
    }


}
