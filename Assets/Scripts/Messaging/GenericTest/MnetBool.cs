using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;


[Serializable]
public class MnetBool : MnetVariableType<bool[]>
{
    public override void Deserialize(Span<byte> receivedBytes)
    {
        int bitFlag = 1;
        int value = 0;
        for (int i = 0; i < 8; i++)
        {
            value = bitFlag & receivedBytes[0] & bitFlag;
            if (value > 0)
            {
                Set(true,i);                
            }
            else
            {
                Set(false,i);
            }
            bitFlag = bitFlag << 1;
        }
    }
    public override void Serialize(Span<byte> reservedBytes)
    {
        int bitFlag = 1;
        for (int i = 0; i < 8; i++)
        {
            if (Value[i])
            {
                //packet[packet.currentLength] = (byte)(packet[packet.currentLength] | bitFlag);
                reservedBytes[0] = (byte)(reservedBytes[0] | bitFlag);
            }
            bitFlag = bitFlag << 1;
        }
    }

    public override void SetSize()
    {
        throw new NotImplementedException();
    }

    public override void Setup()
    {
        sizeInBytes = 1;
        if(Value == null)
        {
            Value = new bool[8];
        }
    }

    public void Set(bool boolean, int index = 0)
    {
        Value[index] = boolean;
        hasChanged = true;
    }

}
