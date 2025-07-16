using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class MnetStringUnicode : MnetVariableType<string>
{

    public MnetStringUnicode()
    {
        //Debug.Log("CONSTRUCTOR CALLED");
    }

    public override void Deserialize(MnetPacket packet)//Span<byte> receivedBytes)
    {
            //Debug.Log("DESERIALIZING "+receivedBytes.Length);
        SetReceivedValue(Encoding.Unicode.GetString(packet.Read(sizeInBytes)));
    }

    public override void Serialize(MnetPacket packet)//Span<byte> reservedBytes)
    {
        Encoding.Unicode.GetBytes(Value,packet.Write(sizeInBytes));
    }

    public override void SetSize()
    {
        // Size of the data and the extra space needed in the variable header
        // !!! On hullua pakotta useri lis‰‰m‰‰n t‰‰ p‰tk‰ jokaseen ni hoidetaa objekti puolella
            //Debug.Log(variableName + " LENGTH " + Value.Length * 2);
        sizeInBytes = Value.Length * 2;// !!! T‰‰ huomioidaa objekti puolel ni ei tarvii ku data koko + MnetSettings.bytesReservedForSegmentSize;
    }

    public override void Setup()
    {

        if (Value == null)
        {
            Debug.Log("VALUE WAS NULL!");
            Value = "";
        }
        //
        if(sizeCategory == VariableSize.Static) sizeCategory = VariableSize.Limited;
    }
}
