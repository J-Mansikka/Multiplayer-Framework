using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewInstanceAction : MnetVariableType<InstanceStruct>
{
    public override void Deserialize(Span<byte> receivedBytes)
    {
        int readPos = 0;
        InstanceStruct receivedIDs;
        receivedIDs.objectID = MnetTools.BytesToInteger(receivedBytes.Slice(readPos, Mnet.bytesReservedForObjectID));
        readPos += Mnet.bytesReservedForObjectID;
        receivedIDs.instanceID = MnetTools.BytesToInteger(receivedBytes.Slice(readPos, Mnet.bytesReservedForInstanceID));

        Debug.Log("RECEIVED ACTION " + Value.objectID + " " + Value.instanceID);
        ReadValue(receivedIDs);
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        int writePos = 0;
        MnetTools.IntegerToBytes(reservedBytes.Slice(writePos, Mnet.bytesReservedForObjectID), Value.objectID);
        writePos += Mnet.bytesReservedForObjectID;
        MnetTools.IntegerToBytes(reservedBytes.Slice(writePos, Mnet.bytesReservedForInstanceID), Value.instanceID);
    }

    public override void SetSize()
    {
        throw new NotImplementedException();
    }

    public override void Setup()
    {
        SetValue(new InstanceStruct());
        sizeCategory = VariableSize.Static;
        sizeInBytes = Mnet.bytesReservedForObjectID + Mnet.bytesReservedForInstanceID;
    }
}
