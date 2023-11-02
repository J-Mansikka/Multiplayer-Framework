using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MVector3 : MessageSegmentBase
{
    /*
    public new Vector3 Item
    {
        get { return (Vector3)base.Item; }
        set
        {
            base.Item = value;
            BitConverter.GetBytes(value.x).CopyTo(Bytes,0);
            BitConverter.GetBytes(value.y).CopyTo(Bytes, 4);
            BitConverter.GetBytes(value.x).CopyTo(Bytes, 8);
        }
    }

    public new byte[] Bytes
    {

        get
        {
            return base.Bytes;
        }
        set
        {
            base.Bytes = value;
            base.Item = new Vector3(BitConverter.ToSingle(Bytes, 0),BitConverter.ToSingle(Bytes,4),BitConverter.ToSingle(Bytes,8));
        }
    }



    public MVector3(Vector3 newValue = default)
    {
        Item = new Vector3();
        Bytes = new byte[12];
    }
    */
    public override bool ToBytes(byte[] dataToBytes, ref int writePos)
    {
        throw new NotImplementedException();
    }

    public override void ToData(byte[] bytesToData, int readPos)
    {
        throw new NotImplementedException();
    }
}
