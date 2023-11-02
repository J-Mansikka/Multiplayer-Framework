using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class MFloat : MessageSegmentBase
{
    //private byte item;
    //private byte[] bytes;

    public new float Item
    {
        get { return (float)base.Item; }
        set
        {
            base.Item = value;
            ItemUpdated = true;
            //base.Bytes[0] = value;
        }
    }

    /*
    public new byte[] Bytes
    {
        
        get
        {
            //Debug.Log("Dfsdfsd");
            //base.Bytes[0] = (byte)base.Item;
            return base.Bytes;
        }        
        set
        {
            base.Bytes = value;
            base.Item = value;
        }
    }

    */


    public MFloat(float newFloat = default)
    {
        Item = newFloat;
        MaxAllottedSize = 4;
        AsBytes = new byte[MaxAllottedSize];
    }

    public override void ToData(byte[] bytesToData, int readPos)
    {
        Item = BitConverter.ToSingle(bytesToData,readPos);
    }

    public override bool ToBytes(byte[] dataToBytes, ref int writePos)
    {
        bool setFlag = false;
        // SIIRRÄ BASEEN?
        if (ItemUpdated)
        {
            AsBytes = BitConverter.GetBytes(Item);
            for (int i = 0; i < MaxAllottedSize; i++)
            {
                dataToBytes[i + writePos] = AsBytes[i];
            }
            writePos += MaxAllottedSize;
            ItemUpdated = false;
            setFlag = true;
        }

        return setFlag;
    }
}
