using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MnetFloat : MnetVariableType<float>
{
    public override void Deserialize(MnetPacket packet) //Span<byte> receivedBytes)
    {
        SetReceivedValue(MnetTools.BytesToFloat(packet.Read(sizeInBytes)));
    }

    public override void Serialize(MnetPacket packet)//Span<byte> reservedBytes)
    {
        MnetTools.FloatToBytes(packet.Write(sizeInBytes), Value);
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
