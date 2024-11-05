using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class MnetString : MnetVariableType<string>
{
    public override void Deserialize(Span<byte> receivedBytes)
    {
        Value = Encoding.Unicode.GetString(receivedBytes);
    }

    public override void Serialize(Span<byte> reservedBytes)
    {
        Encoding.Unicode.GetBytes(Value,reservedBytes);
    }

    public override void SetSize()
    {
        sizeInBytes = (short)(Value.Length * 2 + ServerSettings.bytesReservedForItemSizeValue);
        //Debug.Log("SIZE Was changed " + Value.Length);
    }

    public override void Setup()
    {
        varyingSize = true;
    }
}
