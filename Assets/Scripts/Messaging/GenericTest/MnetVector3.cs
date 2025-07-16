using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MnetVector3 : MnetVariableType<Vector3>
{
    public override void Deserialize(MnetPacket packet)//Span<byte> receivedBytes)
    {
        SetReceivedValue(new Vector3(
            MnetTools.BytesToFloat(packet.Read(4)),
            MnetTools.BytesToFloat(packet.Read(4)),
            MnetTools.BytesToFloat(packet.Read(4))
            ));
    }

    public override void Serialize(MnetPacket packet)//Span<byte> reservedBytes)
    {
        MnetTools.FloatToBytes(packet.Write(4), Value.x);
        MnetTools.FloatToBytes(packet.Write(4), Value.y);
        MnetTools.FloatToBytes(packet.Write(4), Value.z);
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
