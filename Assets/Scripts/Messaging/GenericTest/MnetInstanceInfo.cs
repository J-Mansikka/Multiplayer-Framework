using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MnetInstanceInfo : MnetVariableType<MnetInstanceMessageSegment>
{
    public override void Deserialize(MnetPacket packet)//Span<byte> receivedBytes)
    {
        SetReceivedValue(new MnetInstanceMessageSegment(
            (ObjectInstanceAction)packet.ReadSingleByte(),
            MnetTools.BytesToInteger(packet.Read(Mnet.bytesReservedForInstanceID)),//receivedBytes.Slice(1, Mnet.bytesReservedForInstanceID)),
            MnetTools.BytesToInteger(packet.Read(Mnet.bytesReservedForObjectID)// receivedBytes.Slice(1+Mnet.bytesReservedForInstanceID, Mnet.bytesReservedForObjectID))
            )));
    }

    // T‰‰ tulee objektin puolelta Writena ni currentlength kasvaa oikein. Nyt vaa pit‰‰ huomioida ett‰ toinen osa on + ekan pituus
    public override void Serialize(MnetPacket packet)// Span<byte> reservedBytes)
    {
        packet.WriteSingleByte((byte)Value.action);
        MnetTools.IntegerToBytes(packet.Write(Mnet.bytesReservedForInstanceID), Value.instanceID);//reservedBytes.Slice(1, Mnet.bytesReservedForInstanceID), Value.instanceID);
        MnetTools.IntegerToBytes(packet.Write(Mnet.bytesReservedForObjectID), Value.objectID); // reservedBytes.Slice(1+Mnet.bytesReservedForInstanceID, Mnet.bytesReservedForObjectID), Value.objectID);
    }

    public override void SetSize()
    {
        throw new NotImplementedException();
    }

    public override void Setup()
    {
        sizeCategory = VariableSize.Static;
        sizeInBytes = 1 + Mnet.bytesReservedForInstanceID + Mnet.bytesReservedForObjectID;
    }
}
