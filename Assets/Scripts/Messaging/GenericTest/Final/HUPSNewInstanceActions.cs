using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUPSNewInstanceActions : MnetVariableType<Queue<InstanceStruct>>
{

    int actionSize;
    public override void Deserialize(Span<byte> receivedBytes)
    {
        int readPos = 0;

        while(readPos != receivedBytes.Length)
        {
            InstanceStruct receivedAction;

            receivedAction.objectID = MnetTools.BytesToInteger(receivedBytes.Slice(readPos, Mnet.bytesReservedForObjectID));
            readPos += Mnet.bytesReservedForObjectID;
            receivedAction.instanceID = MnetTools.BytesToInteger(receivedBytes.Slice(readPos, Mnet.bytesReservedForInstanceID));
            readPos += Mnet.bytesReservedForInstanceID;

            Value.Enqueue(receivedAction);
        }
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        InstanceStruct localAction;
        int writePos = 0;
        for (int i = 0; i < Value.Count; i++)
        {
            localAction = Value.Dequeue();
            MnetTools.IntegerToBytes(reservedBytes.Slice(writePos,Mnet.bytesReservedForObjectID), localAction.objectID);
            writePos += Mnet.bytesReservedForObjectID;
            MnetTools.IntegerToBytes(reservedBytes.Slice(writePos, Mnet.bytesReservedForInstanceID), localAction.instanceID);
            writePos += Mnet.bytesReservedForInstanceID;
        }
    }

    public override void SetSize()
    {
        sizeInBytes = Value.Count * actionSize;
    }

    public override void Setup()
    {
        SetValue(new Queue<InstanceStruct>());
        sizeCategory = VariableSize.Splittable;
        actionSize = 1 + Mnet.bytesReservedForObjectID + Mnet.bytesReservedForInstanceID;
    }
}
