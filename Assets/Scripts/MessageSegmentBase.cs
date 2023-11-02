using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MessageSegmentBase
{
    public bool ItemUpdated { get; protected set; }
    public int MaxAllottedSize { get; protected set; }
    public bool VaryingSize { get; protected set; }
    public bool IncludeInMessage { get; protected set; }
    public object Item { get; protected set; }
    public byte[] AsBytes { get; protected set; }
    //public string Name { get; private set; }

    //public byte[] Bytes { get; protected set; }

    protected MessageSegmentBase()
    {
        //Name = name;
        VaryingSize = false;
        ItemUpdated = true;
    }
    
    public abstract void ToData(byte[] bytesToData, int readPos);


    public abstract bool ToBytes(byte[] dataToBytes, ref int writePos);
    

}
