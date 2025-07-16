using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MnetInt : MnetVariableType<int>
{
    public override void Deserialize(MnetPacket packet)//Span<byte> receivedBytes)
    {
        //Value = BitConverter.ToInt32(receivedBytes);
        SetReceivedValue(BinaryPrimitives.ReadInt32LittleEndian(packet.Read(sizeInBytes)));//receivedBytes));
    }

    public override void Serialize(MnetPacket packet)//Span<byte> reservedBytes)
    {
        //BitConverter.TryWriteBytes(reservedBytes, Value);
        BinaryPrimitives.WriteInt32LittleEndian(packet.Write(sizeInBytes), Value); //reservedBytes, _value);
    }

    public override void SetSize()
    {
        throw new NotImplementedException();
    }

    public override void Setup()
    {
        sizeInBytes = 4;
        sizeCategory = VariableSize.Static;
    }
}
