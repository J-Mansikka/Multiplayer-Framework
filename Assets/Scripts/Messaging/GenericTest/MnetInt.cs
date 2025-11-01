using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MnetInt : MnetVariableType<int>
{
    public override void Deserialize(Span<byte> receivedBytes)
    {
        //Value = BitConverter.ToInt32(receivedBytes);
        ReadValue(BinaryPrimitives.ReadInt32LittleEndian(receivedBytes));//receivedBytes));
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        //BitConverter.TryWriteBytes(reservedBytes, Value);
        BinaryPrimitives.WriteInt32LittleEndian(reservedBytes,Value); //reservedBytes, _value);
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
