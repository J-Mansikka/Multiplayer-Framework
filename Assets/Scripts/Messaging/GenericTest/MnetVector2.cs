using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MnetVector2 : MnetVariableType<Vector2>
{
    public override void Deserialize(MnetPacket packet)//Span<byte> receivedBytes)
    {
        //SetReceivedValue(new Vector2(MnetTools.BytesToFloat(receivedBytes),MnetTools.BytesToFloat(receivedBytes.Slice(4))));
        SetReceivedValue(new Vector2(MnetTools.BytesToFloat(packet.Read(4)), MnetTools.BytesToFloat(packet.Read(4))));
    }

    public override void Serialize(MnetPacket packet)// Span<byte> reservedBytes)
    {
        MnetTools.FloatToBytes(packet.Write(4),Value.x);//reservedBytes, Value.x);
        MnetTools.FloatToBytes(packet.Write(4), Value.y);//reservedBytes.Slice(4,4), Value.y);
    }

    public override void SetSize()
    {

    }

    public override void Setup()
    {
        sizeCategory = VariableSize.Static;
        sizeInBytes = 8;
    }
}
