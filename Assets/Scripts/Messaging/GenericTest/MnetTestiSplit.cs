using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class MnetTestiSplit : MnetVariableType<string>
{

    public MnetTestiSplit()
    {
        //Debug.Log("CONSTRUCTOR CALLED");
    }

    public override void Deserialize(Span<byte> receivedBytes)
    {
        Debug.Log("DESERIALIZING " + receivedBytes.Length);
        Value = Encoding.Unicode.GetString(receivedBytes);
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        Encoding.Unicode.GetBytes(Value, reservedBytes);
    }

    public override void SetSize()
    {
        // Size of the data and the extra space needed in the variable header
        // !!! On hullua pakotta useri lis‰‰m‰‰n t‰‰ p‰tk‰ jokaseen ni hoidetaa objekti puolella
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
        sizeCategory = VariableSize.Dividable;
    }
}
