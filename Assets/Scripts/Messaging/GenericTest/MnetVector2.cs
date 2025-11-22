using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MnetVector2 : MnetVariableType<Vector2>
{
    public override void Deserialize(Span<byte> receivedBytes)
    {
        //SetReceivedValue(new Vector2(MnetTools.BytesToFloat(receivedBytes),MnetTools.BytesToFloat(receivedBytes.Slice(4))));
        ReadValue(new Vector2(MnetTools.BytesToFloat(receivedBytes.Slice(0,4)), MnetTools.BytesToFloat(receivedBytes.Slice(4,4))));
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        MnetTools.FloatToBytes(reservedBytes.Slice(0,4),Value.x);
        MnetTools.FloatToBytes(reservedBytes.Slice(4, 4), Value.y);
        //MnetTools.FloatToBytes(packet.Write(4),Value.x);//reservedBytes, Value.x);
        //MnetTools.FloatToBytes(packet.Write(4), Value.y);//reservedBytes.Slice(4,4), Value.y);
    }

    public override void SetSize()
    {

    }

    public override void Setup()
    {
        sizeCategory = VariableSize.Static;
        sizeInBytes = 8;
        _value = new Vector2(0, 0);
    }
}
