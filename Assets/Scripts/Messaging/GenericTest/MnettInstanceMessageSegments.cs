using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

public class MnettInstanceMessageSegments : MnetVariableType<MnetInstanceMessageData[]>
{
    public int numberOfActions = 0;

    public MnettInstanceMessageSegments(MnetObject owner, int maxSize) : base(owner)
    {
        Value = new MnetInstanceMessageData[maxSize];
    }

    public override void Deserialize(Span<byte> receivedBytes)
    {
        numberOfActions = BinaryPrimitives.ReadInt16LittleEndian(receivedBytes);
        int segment;
        for (int i = 0; i < numberOfActions; i++)
        {
            segment = i * 5;
            Value[i].action = (ObjectInstanceAction)receivedBytes[segment];
            Value[i].objectID = BinaryPrimitives.ReadInt16LittleEndian(receivedBytes.Slice(segment + 1));
            Value[i].objectType = BinaryPrimitives.ReadInt16LittleEndian(receivedBytes.Slice(segment + 3));
        }
        GetAndSet();
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        int segment = 0;
        for (int i = 0; i < numberOfActions; i++)
        {
            segment = 5 * i;
            reservedBytes[segment] = (byte)Value[i].action;
            BinaryPrimitives.WriteInt16LittleEndian(reservedBytes.Slice(segment + 1), Value[i].objectID);
            BinaryPrimitives.WriteInt16LittleEndian(reservedBytes.Slice(segment + 3), Value[i].objectType);
        }
        numberOfActions = 0;
    }

    public override void SetSize()
    {
        sizeInBytes = (short)(numberOfActions * 5);
    }

    public override void Setup()
    {
        varyingSize = true;
    }
}
