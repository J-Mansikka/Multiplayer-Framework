using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class MByte : MessageSegmentBase
{
    //private byte item;
    //private byte[] bytes;

    public new byte Item
    {
        get { return (byte)base.Item; }
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


    public MByte(byte newByte = default)
    {
        //Bytes = new byte[1];
        Item = newByte;
        MaxAllottedSize = 1;

        //base.Item = item;
        //base.Bytes = bytes;
    }

    public override void ToData(byte[] bytesToData, int readPos)
    {
        Item = bytesToData[readPos];
    }

    public override bool ToBytes(byte[] dataToBytes, ref int writePos)
    {
        bool setFlag = false;
        if (ItemUpdated)
        {
            dataToBytes[writePos] = Item;
            writePos++;
            ItemUpdated = false;
            setFlag = true;
        }

        return setFlag;
    }
}
