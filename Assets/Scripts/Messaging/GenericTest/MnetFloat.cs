using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MnetFloat : MnetVariableType<float>
{
    public override void Deserialize(Span<byte> receivedBytes)
    {
        ReadValue(MnetTools.BytesToFloat(receivedBytes));
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        MnetTools.FloatToBytes(reservedBytes, Value);
    }

    public override void SetSize()
    {
        throw new NotImplementedException();
    }

    public override void Setup()
    {
        sizeCategory = VariableSize.Static;
        sizeInBytes = 4;
    }
}
