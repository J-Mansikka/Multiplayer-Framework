using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TestingArray : MnetVariableType<TestingNamedFlag[]>
{
    public override void Deserialize(Span<byte> receivedBytes)
    {
        throw new NotImplementedException();
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        throw new NotImplementedException();
    }

    public override void SetSize()
    {
        throw new NotImplementedException();
    }

    public override void Setup()
    {
    }
}
