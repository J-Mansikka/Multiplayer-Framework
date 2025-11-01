using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MnetVector3 : MnetVariableType<Vector3>
{
    public override void Deserialize(Span<byte> receivedBytes)
    {
        ReadValue(new Vector3(
            MnetTools.BytesToFloat(receivedBytes.Slice(0,4)),
            MnetTools.BytesToFloat(receivedBytes.Slice(4,4)),
            MnetTools.BytesToFloat(receivedBytes.Slice(8,4))
            ));
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        MnetTools.FloatToBytes(reservedBytes.Slice(0,4), Value.x);
        MnetTools.FloatToBytes(reservedBytes.Slice(4, 4), Value.y);
        MnetTools.FloatToBytes(reservedBytes.Slice(8, 4), Value.z);
    }

    public override void SetSize()
    {
        throw new NotImplementedException();
    }

    public override void Setup()
    {
        sizeCategory = VariableSize.Static;
        sizeInBytes = 12;
    }
}
